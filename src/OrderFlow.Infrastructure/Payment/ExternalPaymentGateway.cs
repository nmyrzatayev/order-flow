using Microsoft.Extensions.Logging;
using OrderFlow.Domain.Common;
using OrderFlow.Domain.Orders;
using OrderFlow.Domain.Shared;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;

namespace OrderFlow.Infrastructure.Payment;

public sealed class ExternalPaymentGateway : IPaymentGateway
{
    private readonly ResiliencePipeline _resiliencePipeline;
    private readonly ILogger<ExternalPaymentGateway> _logger;

    public ExternalPaymentGateway(ILogger<ExternalPaymentGateway> logger)
    {
        _logger = logger;

        // Build Polly v8 Resilience Pipeline
        _resiliencePipeline = new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                MaxRetryAttempts = 3,
                Delay = TimeSpan.FromMilliseconds(200),
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = true,
                OnRetry = args =>
                {
                    _logger.LogWarning(
                        "Payment attempt failed. Retrying in {RetryDelay}ms. Attempt: {AttemptNumber}",
                        args.RetryDelay.TotalMilliseconds,
                        args.AttemptNumber);
                    return ValueTask.CompletedTask;
                }
            })
            .AddCircuitBreaker(new CircuitBreakerStrategyOptions
            {
                FailureRatio = 0.5,
                SamplingDuration = TimeSpan.FromSeconds(10),
                MinimumThroughput = 5,
                BreakDuration = TimeSpan.FromSeconds(30),
                OnOpened = _ =>
                {
                    _logger.LogError("Payment gateway circuit breaker opened! Calls will fail fast.");
                    return ValueTask.CompletedTask;
                },
                OnClosed = _ =>
                {
                    _logger.LogInformation("Payment gateway circuit breaker reset to normal closed state.");
                    return ValueTask.CompletedTask;
                }
            })
            .AddTimeout(TimeSpan.FromSeconds(5))
            .Build();
    }

    public async Task<Result<Guid>> ProcessPaymentAsync(
        OrderId orderId,
        Money amount,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await _resiliencePipeline.ExecuteAsync(async token =>
            {
                _logger.LogInformation("Processing payment for order {OrderId} with amount {Amount}",
                    orderId.Value, amount.ToString());

                // Simulate external HTTP payment provider call
                await Task.Delay(100, token);

                // Simulation: Payments for amounts ending in 999.00 simulate temporary upstream failure
                if (amount.Amount == 999.00m)
                {
                    throw new HttpRequestException("Simulated upstream payment gateway 503 error");
                }

                var transactionId = Guid.NewGuid();
                _logger.LogInformation("Payment successful for order {OrderId}. Transaction: {TransactionId}",
                    orderId.Value, transactionId);

                return Result.Success(transactionId);
            }, cancellationToken);
        }
        catch (BrokenCircuitException)
        {
            _logger.LogError("Payment rejected due to open circuit breaker.");
            return Result.Failure<Guid>(
                Error.Failure("Payment.ServiceUnavailable", "Payment service is currently unavailable."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Payment failed after retries for order {OrderId}", orderId.Value);
            return Result.Failure<Guid>(
                Error.Failure("Payment.Failed", $"Payment processing failed: {ex.Message}"));
        }
    }
}

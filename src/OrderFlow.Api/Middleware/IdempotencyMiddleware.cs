using System.Text;
using Microsoft.Extensions.Caching.Hybrid;

namespace OrderFlow.Api.Middleware;

public sealed class IdempotencyMiddleware
{
    private const string IdempotencyHeader = "Idempotency-Key";
    private readonly RequestDelegate _next;
    private readonly HybridCache _cache;
    private readonly ILogger<IdempotencyMiddleware> _logger;

    public IdempotencyMiddleware(
        RequestDelegate next,
        HybridCache cache,
        ILogger<IdempotencyMiddleware> logger)
    {
        _next = next;
        _cache = cache;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Only enforce idempotency for POST / PUT / PATCH mutating requests
        if (!HttpMethods.IsPost(context.Request.Method) &&
            !HttpMethods.IsPut(context.Request.Method) &&
            !HttpMethods.IsPatch(context.Request.Method))
        {
            await _next(context);
            return;
        }

        if (!context.Request.Headers.TryGetValue(IdempotencyHeader, out var keyValues) ||
            string.IsNullOrWhiteSpace(keyValues.ToString()))
        {
            await _next(context);
            return;
        }

        var idempotencyKey = keyValues.ToString();
        var cacheKey = $"idempotency:{idempotencyKey}";

        var cachedResponse = await _cache.GetOrCreateAsync<CachedHttpResponse?>(
            cacheKey,
            async _ => null,
            cancellationToken: context.RequestAborted);

        if (cachedResponse is not null)
        {
            _logger.LogInformation("Returning cached response for idempotency key {Key}", idempotencyKey);

            context.Response.StatusCode = cachedResponse.StatusCode;
            context.Response.ContentType = cachedResponse.ContentType;
            context.Response.Headers[IdempotencyHeader] = idempotencyKey;

            await context.Response.WriteAsync(cachedResponse.Body, context.RequestAborted);
            return;
        }

        // Intercept response stream
        var originalResponseBodyStream = context.Response.Body;
        using var memoryStream = new MemoryStream();
        context.Response.Body = memoryStream;

        try
        {
            await _next(context);

            memoryStream.Seek(0, SeekOrigin.Begin);
            var responseBody = await new StreamReader(memoryStream).ReadToEndAsync(context.RequestAborted);
            memoryStream.Seek(0, SeekOrigin.Begin);

            // If response is successful, cache it for 24 hours
            if (context.Response.StatusCode is >= 200 and < 300)
            {
                var entry = new CachedHttpResponse(
                    context.Response.StatusCode,
                    context.Response.ContentType ?? "application/json",
                    responseBody);

                await _cache.SetAsync(cacheKey, entry, cancellationToken: context.RequestAborted);
            }

            await memoryStream.CopyToAsync(originalResponseBodyStream, context.RequestAborted);
        }
        finally
        {
            context.Response.Body = originalResponseBodyStream;
        }
    }

    private sealed record CachedHttpResponse(int StatusCode, string ContentType, string Body);
}

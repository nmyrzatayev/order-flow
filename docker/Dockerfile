# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0-preview AS build
WORKDIR /src

# Copy project files and restore dependencies (caching layers)
COPY Directory.Build.props ./
COPY src/OrderFlow.Domain/OrderFlow.Domain.csproj src/OrderFlow.Domain/
COPY src/OrderFlow.Application/OrderFlow.Application.csproj src/OrderFlow.Application/
COPY src/OrderFlow.Infrastructure/OrderFlow.Infrastructure.csproj src/OrderFlow.Infrastructure/
COPY src/OrderFlow.Api/OrderFlow.Api.csproj src/OrderFlow.Api/

RUN dotnet restore src/OrderFlow.Api/OrderFlow.Api.csproj

# Copy source code and publish
COPY src/ ./src/
WORKDIR /src/src/OrderFlow.Api
RUN dotnet publish -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0-preview AS runtime
WORKDIR /app

# Run as non-root user for security
USER $APP_UID

COPY --from=build /app/publish ./
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "OrderFlow.Api.dll"]

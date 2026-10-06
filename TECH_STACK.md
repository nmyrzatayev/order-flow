# Современный стек технологий Senior .NET Developer (2024–2026)

Этот документ описывает актуальный технологический стек, библиотеки, инструменты и концепции, владение которыми ожидается от Senior .NET разработчика на технических собеседованиях и в реальном production.

---

## 1. Платформа, Язык и CLR Internals

* **.NET Runtime**: .NET 8 (LTS) / .NET 9+.
* **C#**: C# 12 / C# 13 (Primary constructors, Collection expressions, ref struct & ref fields, Pattern matching, Interceptors).
* **CLR Internals & Memory Management**:
  * Сборщик мусора (GC): Gen 0, 1, 2, LOH (Large Object Heap), POH (Pinned Object Heap), Server GC vs Workstation GC, Background GC.
  * Аллокации памяти: стек vs куча, boxing/unboxing, `Span<T>`, `ReadOnlySpan<T>`, `Memory<T>`, `ArrayPool<T>`, `ref struct`.
  * Многопоточность и асинхронность: `async`/`await`, `ValueTask<T>` vs `Task<T>`, `SynchronizationContext`, `ThreadPool` starvation, `Channel<T>`, `AsyncLocal<T>`, lock-free примитивы (`Interlocked`, `SpinLock`, `SemaphoreSlim`).

---

## 2. Архитектурные стили и паттерны

* **Архитектурные подходы**:
  * **Modular Monolith** (Модульный монолит с изолированными Bounded Contexts) — золотой стандарт для большинства современных enterprise-систем перед переходом в микросервисы.
  * **Clean Architecture / Hexagonal Architecture (Ports & Adapters)**.
  * **Vertical Slice Architecture** — группировка кода по фичам (Features), а не техническим слоям (Controllers, Services, Repositories).
  * **Domain-Driven Design (DDD)**: Entities, Value Objects, Aggregates, Domain Events, Ubiquitous Language.
* **Паттерны проектирования и распределенных систем**:
  * **CQRS** (Command Query Responsibility Segregation).
  * **Transactional Outbox / Inbox Pattern** (гарантия At-least-once доставки сообщений без 2PC распределенных транзакций).
  * **Saga Pattern** (Orchestration vs Choreography) для распределенных транзакций и компенсаций.
  * **Idempotency**: идемпотентные потребители сообщений и API (`Idempotency-Key`).
  * **Specification Pattern** и **Result Pattern** (замена исключений в бизнес-логике).

---

## 3. Web API и Коммуникации

* **REST / HTTP API**:
  * Minimal APIs vs Controller-based APIs, FastEndpoints.
  * Content Negotiation, RFC 7807 (`ProblemDetails` для ошибок).
  * Версионирование API (`Asp.Versioning.Http`).
  * Rate Limiting (встроенный в .NET middleware: Token Bucket, Concurrency, Sliding Window).
* **RPC & Streaming**:
  * **gRPC** (Protobuf, HTTP/2, contract-first, streaming) для высоконагруженного межсервисного взаимодействия.
  * **SignalR** / WebSockets для real-time уведомлений.

---

## 4. Хранение данных, ORM и Кэширование

* **Реляционные БД**: PostgreSQL, SQL Server.
  * Оптимизация запросов: `EXPLAIN ANALYZE`, индексы (B-Tree, GIN, составные, покрывающие), денормализация, блокировки и уровни изоляции транзакций (Read Committed, Repeatable Read, Snapshot, Serializable).
* **ORM & Micro-ORM**:
  * **Entity Framework Core**:
    * Change Tracker (и влияние `AsNoTracking()` / `AsNoTrackingWithIdentityResolution()`).
    * Split queries (`AsSplitQuery`), Compiled Queries, Interceptors, Global Query Filters.
    * Concurrency Tokens / Оптимистичные блокировки (`xmin` в Postgres, RowVersion).
    * Миграции в production (Idempotent SQL bundles, zero-downtime миграции / Expand-Contract pattern).
  * **Dapper**: точечные высокопроизводительные выборки для Query-стороны CQRS.
* **Кэширование**:
  * **Redis**: Cache-Aside, Write-Through, distributed locks (`RedLock` / Lua-скрипты), Key expiry, Thundering Herd problem (решение через SingleFlight / `HybridCache` в .NET 9).

---

## 5. Асинхронные сообщения и брокеры

* **Message Brokers**: RabbitMQ, Apache Kafka.
* **Библиотеки/Абстракции**:
  * **MassTransit** или **Wolverine** (out-of-the-box Outbox, Retry, Circuit Breaker, Dead Letter Queue, Saga state machines).
* **Концепции**:
  * Семантики доставки: At-most-once, At-least-once, Exactly-once (эффективно At-least-once + Idempotency).
  * Обработка ошибок: Transient error retry, Backoff policies, Poison pill messages, DLQ (Dead Letter Queue).

---

## 6. Надежность и отказоустойчивость (Resilience)

* **Polly v8** (Resilience Pipelines):
  * Retry (со случайным джиттером / Exponential Backoff).
  * Circuit Breaker (защита от каскадных падений).
  * Rate Limiter, Timeout, Fallback.
  * Интеграция с `IHttpClientFactory`.

---

## 7. Безопасность и Авторизация

* **Аутентификация & Авторизация**:
  * OAuth 2.0 / OpenID Connect (OIDC).
  * JWT (Access & Refresh tokens, claims, validation, signing keys, revocation).
  * Policy-based authorization, Resource-based authorization, Permission-based RBAC/PBAC.
* **Security Best Practices**:
  * Data protection (`IDataProtectionProvider`), секреты в Kubernetes/Azure KeyVault/Vault, защита от OWASP Top 10 (SQL Injection, XSS, CSRF, IDOR).

---

## 8. Observability & Мониторинг (Телеметрия)

* **OpenTelemetry (OTel)** — современный стандарт индустрии:
  * **Tracing**: Distributed tracing с W3C TraceContext (Propagation заголовков `traceparent`, `tracestate`).
  * **Metrics**: `System.Diagnostics.Metrics` (`Meter`, `Counter`, `Histogram`), экспорт в Prometheus / Grafana.
  * **Logs**: Структурированное логирование (Serilog / OpenTelemetry Logger), Correlation ID.
* **Health Checks**:
  * `Microsoft.Extensions.Diagnostics.HealthChecks` (Liveness, Readiness probes для K8s).

---

## 9. Тестирование и Benchmarking

* **Unit Testing**: xUnit / NUnit, FluentAssertions, NSubstitute / Moq.
* **Integration Testing**:
  * `WebApplicationFactory<Program>`.
  * **Testcontainers** (поднятие реального PostgreSQL, Redis, RabbitMQ в Docker на время тестов).
* **Architecture Testing**: NetArchTest / ArchUnitNET (тестирование соблюдения границ слоев и зависимостей в коде).
* **Performance Testing & Profiling**:
  * **BenchmarkDotNet** (микро-бенчмарки памяти и CPU).
  * Профайлеры: `dotnet-trace`, `dotnet-dump`, `dotnet-gcdump`, PerfView, JetBrains dotTrace / dotMemory.

---

## 10. DevOps, Контейнеризация и Cloud-Native

* **Docker**: Многоэтапная сборка (Multi-stage builds), Non-root user, distroless / chiseled images для минимизации размера и уязвимостей.
* **Docker Compose**: оркестрация локального окружения (App + Postgres + Redis + RabbitMQ + Jaeger + Prometheus + Grafana).
* **CI/CD**: GitHub Actions (линтер, тесты, сборка образов, security scan).
* **Kubernetes (базовое понимание)**: Pod, Deployment, Service, Ingress, ConfigMap, Secret, probes (liveness, readiness).

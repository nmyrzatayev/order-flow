# 🚀 Interview Flow: Подготовка к Senior .NET Developer

Репозиторий предназначен для практической подготовки к техническим собеседованиям на позицию **Senior .NET Software Engineer / Tech Lead**. 

Здесь собран современный стек, матрица сеньорных компетенций с разбором каверзных вопросов, а также реализован эталонный пет-проект **OrderFlow** (Order & Payment Processing Engine).

---

## 📚 Навигация по документации

| Документ | Описание |
| :--- | :--- |
| 📖 **[TECH_STACK.md](./TECH_STACK.md)** | Актуальный технологический стек (2024–2026): .NET 9/10, C# 12/13, EF Core, Dapper, Redis, RabbitMQ, Polly v8, OpenTelemetry, Docker. |
| 🗺️ **[SENIOR_ROADMAP.md](./SENIOR_ROADMAP.md)** | Матрица компетенций Senior vs Middle: CLR & GC internals, многопоточность, архитектурные компромиссы, безопасность и вопросы на собеседованиях. |
| 🏗️ **[PET_PROJECT_SPEC.md](./PET_PROJECT_SPEC.md)** | Архитектурная спецификация и пошаговый дизайн системы **OrderFlow**. |

---

## 🏛️ Что реализовано в проекте OrderFlow

Проект спроектирован так, чтобы лаконично, но на 100% продемонстрировать все главные инженерные и сеньорные паттерны:

```
                            HTTP Request (Idempotency-Key)
                                        │
                                        ▼
                      ┌────────────────────────────────────┐
                      │          OrderFlow.Api             │
                      │  - IdempotencyMiddleware           │
                      │  - GlobalExceptionHandler (RFC7807)│
                      │  - OpenTelemetry Tracing & Metrics │
                      │  - Minimal APIs + Scalar Docs      │
                      └─────────────────┬──────────────────┘
                                        │
                  (Command)             ▼             (Query)
         ┌──────────────────────────────┴──────────────────────────────┐
         ▼                                                             ▼
┌─────────────────────────────────┐                   ┌─────────────────────────────────┐
│     OrderFlow.Application       │                   │     OrderFlow.Infrastructure    │
│  - MediatR + Pipeline Behaviors │                   │  - Dapper (Zero tracking cost)  │
│  - FluentValidation (no throws) │                   │  - HybridCache (L1 + L2 Redis)  │
│  - Result Pattern (Error type)  │                   │                                 │
└────────────────┬────────────────┘                   └─────────────────────────────────┘
                 │
                 ▼
┌─────────────────────────────────┐
│        OrderFlow.Domain         │
│  - Rich Domain Model (DDD)      │
│  - Aggregates (Order, Inventory)│
│  - Optimistic Concurrency Token │
│  - Invariants & Domain Events   │
└────────────────┬────────────────┘
                 │
                 ▼
┌────────────────────────────────────────────────────────────────────────┐
│                        OrderFlow.Infrastructure                        │
│  - EF Core (Npgsql PostgreSQL)                                         │
│  - InsertOutboxMessagesInterceptor (Transactional Outbox в 1 транзакции)│
│  - OutboxProcessorBackgroundService (Гарантия доставки в RabbitMQ)     │
│  - ExternalPaymentGateway с Polly v8 (Jittered Retry + Circuit Breaker)│
│  - MassTransit Consumer (Saga Compensation: отмена заказа & возврат)   │
└────────────────────────────────────────────────────────────────────────┘
```

### 1. Чистая архитектура и DDD (Domain Layer)
* **Zero external dependencies** в `OrderFlow.Domain`.
* **Rich Domain Model**: инварианты инкапсулированы внутри агрегатов (`Order`, `Inventory`, `OrderItem`).
* **Value Objects**: `Money`, `Currency` со структурным равенством.
* **Strongly-typed IDs**: `OrderId`, `CustomerId`, `ProductId`, `InventoryId` на `readonly record struct` (zero allocations).
* **Result Pattern**: класс `Result<T>` и типизированные `Error` вместо выбрасывания медленных `Exception` на валидациях.

### 2. CQRS с разделением моделей чтения и записи
* **Write Side**: команды через MediatR с пайплайнами логирования и валидации (`ValidationBehavior`, `LoggingBehavior`), агрегаты EF Core с проверкой инвариантов.
* **Read Side**: Dapper выполняет прямой оптимизированный SQL-запрос, минуя оверхед Change Tracker EF Core, с двухуровневым кэшированием **`HybridCache`** (память + Redis).

### 3. Распределенная надежность и Transactional Outbox
* **Решение Dual-Write Problem**: интерцептор `InsertOutboxMessagesInterceptor` перехватывает доменные события сущностей и сохраняет их в таблицу `outbox_messages` **в той же транзакции базы данных**.
* **Outbox Processor**: фоновый воркер `OutboxProcessorBackgroundService` надежно считывает и публикует события в брокер сообщений (**MassTransit / RabbitMQ**).
* **Saga Compensation**: если платежный шлюз отклоняет оплату, consumer автоматически отменяет заказ и возвращает зарезервированный товар обратно на склад.

### 4. API Idempotency & Concurrency Control
* **`IdempotencyMiddleware`**: перехват заголовка `Idempotency-Key` с кэшированием ответов (защита от повторного списания средств и двойных заказов при ретраях клиента).
* **Optimistic Locking**: поле `Version` (concurrency token) в таблице `inventories` предотвращает перезапись остатков при конкурентных заказах.

### 5. Отказоустойчивость с Polly v8
* `ExternalPaymentGateway` настроен через новейший `ResiliencePipeline`:
  * **Exponential Backoff с случайным Jitter** (защита от шторма повторных запросов).
  * **Circuit Breaker** (быстрый отказ при падении внешнего платежного провайдера, предотвращение исчерпания потоков).
  * **Timeout**.

### 6. Observability & Стандарты
* **OpenTelemetry**: Сквозной Distributed Tracing по стандарту W3C TraceContext (ASP.NET Core, HttpClient, Npgsql, OTLP экспортер в Jaeger).
* **Serilog**: Структурированные логи с выводом `TraceId`.
* **RFC 7807**: Единый формат ошибок `ProblemDetails`.

---

## 🧪 Запуск тестов

В проекте настроены 3 уровня тестирования:
1. **Unit Tests** (`OrderFlow.Domain.UnitTests`): проверка инвариантов агрегатов и расчетов.
2. **Architecture Tests** (`OrderFlow.ArchitectureTests`): правила `NetArchTest` для автоматического контроля изоляции слоев (Domain не имеет внешних ссылок).
3. **Integration Tests** (`OrderFlow.IntegrationTests`): запуск **реального PostgreSQL в Docker через Testcontainers** и тестирование сквозных сценариев (сидирование, оформление, идемпотентность, чтение через Dapper).

Запустить все тесты:
```bash
dotnet test
```

---

## 🏎️ Запуск микро-бенчмарков (BenchmarkDotNet)

Для демонстрации навыков низкоуровневой оптимизации подготовлен проект `OrderFlow.Benchmarks`:
```bash
dotnet run -c Release --project benchmarks/OrderFlow.Benchmarks
```

---

## 🐳 Запуск через Docker Compose

Для локального запуска всего окружения (API, Postgres, RabbitMQ с веб-интерфейсом, Redis, Jaeger UI):

```bash
docker compose -f docker/docker-compose.yml up --build -d
```

* **API & OpenAPI (Scalar UI)**: `http://localhost:5000/scalar/v1`
* **Jaeger Tracing UI**: `http://localhost:16686`
* **RabbitMQ Management**: `http://localhost:15672` (guest / guest)
* **PostgreSQL**: `localhost:5432`

---

## 💡 Как презентовать этот проект на собеседовании

1. **"Расскажите о своем подходе к архитектуре"**:
   > *"В проекте применен Modular Monolith с элементами Clean и Vertical Slice Architecture. Доменный слой полностью изолирован (что подтверждается архитектурными тестами на NetArchTest). Модели чтения и записи разделены по CQRS: команды идут через богатую доменную модель и EF Core, а чтение — через легковесный Dapper и HybridCache."*

2. **"Как вы решаете проблему Dual-Write и распределенной консистентности?"**:
   > *"Я не публикую события в брокер напрямую из HTTP-хендлера, так как сбой сети или падение сервиса привели бы к потере консистентности. Вместо этого применен паттерн Transactional Outbox: интерцептор EF Core сериализует доменные события в таблицу `outbox_messages` в рамках той же транзакции БД. Фоновый процессор гарантирует At-least-once доставку в RabbitMQ, а консьюмеры и API защищены идемпотентностью."*

3. **"Как защищаетесь от каскадных падений сторонних интеграций?"**:
   > *"Интеграция с внешним сервисом изолирована за Polly v8 Resilience Pipeline: настроен Exponential Retry со случайным джиттером, чтобы избежать Thundering Herd, и Circuit Breaker, который размыкает цепь и сбрасывает нагрузку при превышении порога ошибок."*

# Спецификация пет-проекта: OrderFlow (Order & Payment Processing Engine)

Эталонный сеньорный проект, цель которого — продемонстрировать глубокое понимание архитектурных паттернов, отказоустойчивости, распределенной консистентности и современных подходов в .NET экосистеме.

---

## 🎯 Цели проекта

1. Показать **архитектурную зрелость** (Modular Monolith / Clean Architecture / Vertical Slice, DDD, CQRS).
2. Решить реальные **распределенные проблемы**:
   * Dual-write problem через **Transactional Outbox**.
   * Защита от повторных списаний/заказов через **API & Consumer Idempotency**.
   * Конкурентный заказ ограниченного инвентаря (**Optimistic Concurrency / Distributed Locks**).
   * Отказоустойчивость вызова внешних сервисов (**Polly v8 Pipelines**).
3. Обеспечить **Production-Grade Observability** (OpenTelemetry: Traces, Metrics, Logs с сквозным `TraceId`).
4. Написать надежные **интеграционные тесты с Testcontainers** (PostgreSQL, RabbitMQ, Redis).
5. Показать культуру производительности (**BenchmarkDotNet** на критическом пути).

---

## 🏢 Предметная область: Order & Reservation Engine

### Бизнес-сценарий:
1. **Создание заказа (Order Placement)**:
   * Клиент отправляет запрос с `Idempotency-Key`.
   * Система проверяет доступность товара на складе (защита от race conditions через оптимистичную блокировку).
   * Заказ сохраняется со статусом `Pending`.
   * В той же транзакции базы данных создается запись в таблице `OutboxMessages` (`OrderCreatedDomainEvent`).
2. **Асинхронная обработка (Outbox & Messaging)**:
   * Фоновый воркер (Outbox Processor) считывает непосланные события, публикует их в **RabbitMQ** и помечает обработанными.
3. **Оплата (Payment Processing with Resilience)**:
   * Consumer слушает событие `OrderCreated`.
   * Вызывается внешний платежный провайдер (симуляция нестабильного API):
     * Применяется **Polly v8**: Retry с экспоненциальным джиттером, Circuit Breaker, Timeout.
   * При успехе: статус заказа обновляется на `Paid`, создается событие `OrderPaid`.
   * При критическом отказе: статус заказа обновляется на `Cancelled`, бронь товара возвращается на склад (компенсация).
4. **Чтение данных (CQRS Queries)**:
   * Запросы на просмотр заказов и аналитики выполняются в обход агрегатов Domain, напрямую через оптимизированные запросы (Dapper / EF NoTracking).
   * Каталог товаров и частые выборки кэшируются через **Redis / HybridCache** с инвалидацией при обновлении.

---

## 🏗️ Архитектура системы

```
                              ┌─────────────────────────────────────────┐
                              │             HTTP Client / UI            │
                              └────────────────────┬────────────────────┘
                                                   │
                                                   ▼
┌────────────────────────────────────────────────────────────────────────────────────────┐
│                                   OrderFlow API Host                                   │
│                                                                                        │
│   ┌────────────────────────────────────────────────────────────────────────────────┐   │
│   │                        API Layer (Minimal APIs / Endpoints)                    │   │
│   │   - Idempotency Middleware         - Authentication / Authorization (JWT)      │   │
│   │   - Global Exception Handling (RFC 7807 ProblemDetails)                        │   │
│   │   - Rate Limiting Middleware       - OpenTelemetry HTTP Tracing               │   │
│   └───────────────────────┬────────────────────────────────┬───────────────────────┘   │
│                           │                                │                           │
│           (Command)       ▼                                ▼       (Query)             │
│   ┌───────────────────────────────────┐    ┌───────────────────────────────────┐       │
│   │   Application Layer (Commands)    │    │    Application Layer (Queries)    │       │
│   │   - MediatR Command Handlers      │    │   - Dapper / Fast EF Queries      │       │
│   │   - FluentValidation Pipelines    │    │   - HybridCache / Redis Cache     │       │
│   │   - Unit of Work & Transaction    │    │                                   │       │
│   └─────────────────┬─────────────────┘    └─────────────────┬─────────────────┘       │
│                     │                                        │                         │
│                     ▼                                        │                         │
│   ┌───────────────────────────────────┐                      │                         │
│   │        Domain Layer (DDD)         │                      │                         │
│   │   - Aggregates (Order, Inventory) │                      │                         │
│   │   - Domain Events                 │                      │                         │
│   │   - Invariants & Business Rules   │                      │                         │
│   └─────────────────┬─────────────────┘                      │                         │
│                     │                                        │                         │
│                     ▼                                        │                         │
│   ┌──────────────────────────────────────────────────────────┴─────────────────────┐   │
│   │                     Infrastructure Layer (PostgreSQL)                          │   │
│   │   - EF Core (Orders, Inventory, Outbox Table)                                  │   │
│   │   - Interceptors (Audit, Domain Events -> Outbox)                              │   │
│   └─────────────────┬──────────────────────────────────────────────────────────────┘   │
│                     │                                                                  │
│                     ▼ (Background Worker / MassTransit Outbox)                         │
│   ┌───────────────────────────────────┐                                                │
│   │     Outbox Processor Service      │                                                │
│   └─────────────────┬─────────────────┘                                                │
└─────────────────────┼──────────────────────────────────────────────────────────────────┘
                      │ Publish Messages
                      ▼
         ┌─────────────────────────┐
         │    RabbitMQ (Broker)    │
         └────────────┬────────────┘
                      │ Consume
                      ▼
┌────────────────────────────────────────────────────────────────────────────────────────┐
│                              OrderFlow Background Workers                              │
│                                                                                        │
│   ┌────────────────────────────────────────────────────────────────────────────────┐   │
│   │   Consumers (Idempotent Message Handlers)                                      │   │
│   │   - Payment Consumer (External Payment Gateway via Polly v8 Resilience)        │   │
│   │   - Notification Consumer                                                      │   │
│   │   - OpenTelemetry Activity Source & Trace Propagation                          │   │
│   └────────────────────────────────────────────────────────────────────────────────┘   │
└────────────────────────────────────────────────────────────────────────────────────────┘
```

---

## 🛠️ Состав технологического стека проекта

| Компонент | Технология | Обоснование на интервью |
| :--- | :--- | :--- |
| **Фреймворк** | .NET 8 / 9 + C# 12/13 | Актуальный LTS, производительные коллекции, Primary Constructors, Minimal APIs. |
| **БД** | PostgreSQL | Индустриальный стандарт для микросервисов и enterprise-приложений. |
| **ORM / Data Access** | EF Core 8/9 + Dapper | EF Core для сложной бизнес-логики записи, Dapper для высокопроизводительного чтения. |
| **Кэш** | Redis + .NET HybridCache | Быстрый L1+L2 кэш с защитой от Stampede/Thundering Herd. |
| **Брокер сообщений** | RabbitMQ + MassTransit | Готовая реализация Transactional Outbox, Retry, DLQ, Idempotency. |
| **Отказоустойчивость** | Polly v8 | Защита от сбоев интеграции с платежной системой (Circuit Breaker, Jittered Retry). |
| **Наблюдаемость** | OpenTelemetry, Prometheus, Jaeger / Grafana | Единый сквозной Distributed Tracing по W3C стандарту + метрики. |
| **Тесты** | xUnit, FluentAssertions, Testcontainers, NetArchTest | Запуск реальных Postgres, RabbitMQ и Redis в тестах без моков БД. |
| **Бенчмаркинг** | BenchmarkDotNet | Микро-бенчмарки сериализации и аллокаций. |
| **Инфраструктура** | Docker, Docker Compose | Запуск всего стека одной командой. |

---

## 📦 Модульная структура репозитория

```
interview-flow/
├── src/
│   ├── OrderFlow.Domain/              # Чистая бизнес-логика (Entities, Aggregates, Events, Exceptions)
│   ├── OrderFlow.Application/         # CQRS команды, запросы, валидаторы, пайплайны
│   ├── OrderFlow.Infrastructure/      # Реализация EF Core, Outbox, Dapper, внешние API
│   ├── OrderFlow.Contracts/           # DTO, события для интеграции
│   └── OrderFlow.Api/                 # Minimal API эндпоинты, middleware, Program.cs
├── tests/
│   ├── OrderFlow.Domain.UnitTests/    # Тестирование бизнес-инвариантов
│   ├── OrderFlow.ArchitectureTests/   # NetArchTest (проверка зависимостей слоев)
│   └── OrderFlow.IntegrationTests/    # Testcontainers (Postgres, RabbitMQ, Redis)
├── benchmarks/
│   └── OrderFlow.Benchmarks/          # BenchmarkDotNet
├── docker/
│   ├── docker-compose.yml             # Postgres, Redis, RabbitMQ, Jaeger, Prometheus
│   └── Dockerfile                     # Multi-stage production build
├── TECH_STACK.md
├── SENIOR_ROADMAP.md
├── PET_PROJECT_SPEC.md
└── README.md
```

---

## 🚀 Пошаговый план разработки

1. **Шаг 1: Документация и концепт** (Текущий этап: стек, роадмап, спецификация).
2. **Шаг 2: Инициализация солюшена и базовых проектов** (`dotnet new sln`, структура проектов, сборка).
3. **Шаг 3: Domain Layer** (Агрегат `Order`, `OrderItem`, Value Objects `Money`, доменные события, инварианты).
4. **Шаг 4: Infrastructure & Persistence** (EF Core DbContext, Fluent Configurations, PostgreSQL, миграции).
5. **Шаг 5: Application Layer & CQRS** (MediatR/Wolverine, FluentValidation, обработчики команд и запросов).
6. **Шаг 6: Transactional Outbox & RabbitMQ** (Сохранение доменных событий в Outbox, фоновый воркер или MassTransit).
7. **Шаг 7: Payment & Resilience** (Потребитель сообщений, симуляция платежного шлюза с Polly v8).
8. **Шаг 8: Caching & Query Optimization** (Dapper для быстрого чтения, Redis / HybridCache).
9. **Шаг 9: Observability** (OpenTelemetry distributed tracing, Jaeger, Prometheus метрики).
10. **Шаг 10: Тестирование** (Unit, NetArchTest, Testcontainers integration tests).
11. **Шаг 11: Бенчмарки и Docker Compose** (Полная оркестрация одной командой).

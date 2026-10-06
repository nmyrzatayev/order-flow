# Senior .NET Developer Roadmap & Interview Matrix

Этот роадмап структурирует темы, концептуальные знания и практические навыки, необходимые для уверенного прохождения собеседований на позицию Senior .NET Software Engineer / Tech Lead.

---

## 🧭 Уровневая матрица: Middle vs Senior

| Направление | Middle .NET Developer | Senior .NET Developer |
| :--- | :--- | :--- |
| **Код и реализация** | Умеет написать фичу по ТЗ, знает паттерны GoF, использует EF Core / ASP.NET. | Понимает влияние аллокаций на GC, умеет профилировать код, пишет zero-allocation critical path при необходимости. |
| **Архитектура** | Знает слои Clean Architecture / 3-tier, применяет MediatR по шаблону. | Оценивает трейдоффы: Монолит vs Микросервисы, CQRS с разделением моделей, Event-driven vs REST/gRPC. |
| **Базы данных** | Пишет LINQ, умеет создавать миграции, понимает базовые внешние ключи и индексы. | Разбирается в уровнях изоляции, понимает блокировки (Shared/Exclusive, Deadlocks), оптимизирует сложные планы выполнения, планирует миграции без простоя (Zero-Downtime). |
| **Асинхронность & Распределенность** | Ставит `await` перед Task-ами, знает базовый `async`/`await`. | Понимает семантики доставки брокеров (At-least-once), решает проблему распределенной согласованности (Outbox, Sagas), борется с Race Conditions. |
| **Надежность и мониторинг** | Логирует `ILogger.LogError`, пишет try-catch. | Проектирует метрики, распределенный трейсинг (W3C TraceContext), настраивает Circuit Breaker/Rate Limiting, анализирует дампы памяти при OOM. |
| **Инженерия и качество** | Пишет Unit-тесты для сервисов с Moq. | Проектирует интеграционные тесты на Testcontainers, тестирует архитектурные правила (ArchUnit), внедряет CI/CD пайплайны. |

---

## 📌 Модуль 1: .NET Runtime, CLR & Low-Level Mechanics

### Ключевые темы для глубокого изучения
1. **Garbage Collection (GC)**:
   * Фазы сборки: Mark, Sweep, Compact.
   * Поколения (Gen 0, 1, 2) и почему старые объекты собирать дороже.
   * LOH (порог 85 000 байт), POH (буферы ввода/вывода, `fixed`).
   * Server GC (отдельный heap на ядро) vs Workstation GC.
   * Background GC и Non-concurrent GC.
   * Алгоритм остановки мира (Stop-The-World).
2. **Память и Value/Reference Types**:
   * Размещение в стеке и куче. В каких случаях `struct` попадает в кучу (boxing, замыкания, поля классов).
   * `ref struct`, `in`, `ref readonly`, `scoped`.
   * `Span<T>` и `Memory<T>`: как работают срезы памяти без аллокаций.
   * `ArrayPool<T>`: повторное использование буферов.
3. **Многопоточность и асинхронность**:
   * `Task` vs `ValueTask`: когда `ValueTask` действительно оправдан.
   * `SynchronizationContext` и `ConfigureAwait(false)`: роль в ASP.NET Core vs Desktop.
   * `ThreadPool`: Starvation (голодание пула), почему нельзя вызывать `.Result` или `.Wait()`.
   * Примитивы синхронизации: `Monitor` (`lock`), `SemaphoreSlim`, `ReaderWriterLockSlim`, `Interlocked`.
   * `Channel<T>`: высокопроизводительный потокобезопасный Producer-Consumer.

### ❓ Типичные сеньорные вопросы на собеседовании:
* *Что происходит при вызове `await Task.Delay(1000)` на уровне потоков ОС?*
* *Почему ThreadPool starvation может положить высоконагруженный веб-сервер и как диагностировать эту проблему?*
* *В чем разница между `Memory<T>` и `Span<T>` и почему `Span<T>` не может быть полем обычного класса?*
* *Как работает `IDisposable` и `IAsyncDisposable`, зачем нужен паттерн Dispose с финализатором (`GC.SuppressFinalize`)?*

---

## 📌 Модуль 2: Проектирование архитектуры и распределенных систем

### Ключевые темы
1. **Architectural Styles**:
   * **Modular Monolith**: организация модулей через контракты, единая БД с изолированными схемами или логическими границами, коммуникация in-memory через MediatR/Channel vs через брокер.
   * **Vertical Slice Architecture**: разделение по фичам (Features), отказ от лишней абстракции ради абстракции.
   * **Domain-Driven Design (DDD)**: Ubiquitous Language, Bounded Contexts, Aggregates, Invariants, Domain Events.
2. **Паттерны распределенных систем**:
   * **Dual-Write Problem**: почему нельзя в одном HTTP-запросе записать в Postgres и затем отправить событие в RabbitMQ.
   * **Transactional Outbox & Outbox Processor**: реализация сохранения событий в той же транзакции БД и их фоновая публикация.
   * **Idempotency**: проектирование идемпотентных API (заголовок `Idempotency-Key`) и идемпотентных обработчиков сообщений (Inbox pattern / дедупликация).
   * **Saga Pattern**: Orchestrated (State Machine) vs Choreographed (событийно-ориентированная), компенсационные действия.
   * **CQRS**: разделение модели записи (инварианты, агрегаты) и модели чтения (оптимизированные денормализованные представления / Dapper).

### ❓ Типичные сеньорные вопросы на собеседовании:
* *Как гарантировать надежную доставку события в брокер сообщений при сбое приложения или сети?*
* *Расскажите о различиях между оркестрируемой сагой и хореографией. В каких случаях вы выберете оркестрацию?*
* *Как реализовать идемпотентный POST запрос на оформление заказа или списание средств?*
* *Какие минусы у классической Clean Architecture и как Vertical Slice решает проблему чрезмерной связности слоев?*

---

## 📌 Модуль 3: Базы данных, Консистентность и EF Core

### Ключевые темы
1. **Реляционные базы данных (PostgreSQL)**:
   * Индексы: B-Tree, Composite, Included columns (`INCLUDE`), GIN/GiST. Селективность индекса.
   * Анализ производительности: чтение плана `EXPLAIN (ANALYZE, BUFFERS)`. Seq Scan vs Index Scan vs Bitmap Index Scan.
   * Транзакции: ACID, уровни изоляции (Read Uncommitted, Read Committed, Repeatable Read, Serializable). Аномалии: Dirty Read, Non-repeatable Read, Phantom Read, Write Skew.
   * Блокировки: Оптимистичные (Concurrency Tokens) vs Пессимистичные (`SELECT ... FOR UPDATE`).
2. **EF Core Internals**:
   * Change Tracker: как работает Snapshot Tracking, затраты памяти при трекинге сотен объектов.
   * Запросы: `AsNoTracking()`, `AsSplitQuery()` (борьба с декартовым произведением Cartesian Explosion), `Select()`-проекции.
   * Батчинг команд и интерцепторы (`SaveChangesInterceptor`, `DbCommandInterceptor`).
   * Миграции: миграции больших таблиц без даунтайма, добавление столбцов с default values, Expand-Contract pattern.
3. **Кэширование**:
   * Паттерны: Cache-Aside, Write-Through.
   * Проблемы кэширования: Cache Stampede / Thundering Herd, Cache Penetration, Cache Breakdown.
   * `HybridCache` в .NET 9 (двухуровневый кэш L1 Memory + L2 Redis со встроенной защитой от thundering herd).

### ❓ Типичные сеньорные вопросы на собеседовании:
* *Что такое Cartesian Explosion в EF Core при использовании нескольких `.Include()` и как с этим бороться?*
* *Чем отличается уровень изоляции Repeatable Read от Snapshot Isolation в PostgreSQL?*
* *Как в распределенной системе обновить запись в БД, защитившись от Lost Update (потери одновременного обновления двумя сервисами)?*

---

## 📌 Модуль 4: Observability, Resilience & Security

### Ключевые темы
1. **OpenTelemetry и три столпа наблюдаемости**:
   * **Tracing**: W3C TraceContext (`traceparent`, `tracestate`), спаны, propagation между микросервисами по HTTP и через брокер сообщений.
   * **Metrics**: системные и кастомные метрики через `System.Diagnostics.Metrics` (`Counter`, `Histogram`).
   * **Logs**: Структурированные JSON-логи с CorrelationId / TraceId для сквозного поиска в Grafana Loki / ELK.
2. **Resilience с Polly v8**:
   * Стратегии: Retry с Exponential Backoff + Jitter, Circuit Breaker, Timeout, Rate Limiter.
   * Fallback-стратегии при отказе сторонних сервисов.
3. **Безопасность**:
   * Архитектура JWT: заголовок, payload, подпись (RS256 vs HS256). Ротация ключей, валидация `iss`, `aud`, `exp`.
   * RBAC (Role-Based) vs PBAC/ABAC (Policy/Attribute-Based Access Control).

### ❓ Типичные сеньорные вопросы на собеседовании:
* *Как устроен Distributed Tracing и как передать контекст трейса через очередь сообщений в RabbitMQ?*
* *Что такое Jitter при настройке Retry-политики и почему Retry без джиттера опасен для падающей системы?*
* *Как организовать безопасную инвалидацию JWT токена до истечения его срока жизни (`exp`)?*

---

## 📌 Модуль 5: Тестирование, CI/CD и Профилирование

### Ключевые темы
1. **Интеграционное тестирование**:
   * Использование `WebApplicationFactory` для тестирования полного пайплайна HTTP-запроса.
   * **Testcontainers**: запуск реальных изолированных контейнеров (PostgreSQL, Redis, RabbitMQ) в тестах.
2. **Диагностика и профилирование на проде**:
   * Диагностика утечек памяти и высокой нагрузки CPU в Linux-контейнерах: сбор дампов (`dotnet-dump`), трассировок (`dotnet-trace`).
   * Микрооптимизация: `BenchmarkDotNet` для сравнения алгоритмов и аллокаций.
3. **CI/CD и контейнеризация**:
   * Оптимизация Dockerfile: многоэтапная сборка, кеширование nuget пакетов, Chiseled / Distroless образы, Non-root security.

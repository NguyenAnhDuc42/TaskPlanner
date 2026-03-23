# Feature Implementation Guide (The TaskPlanner Master Blueprint)

This document is the **Definitive Source of Truth**. It represents the ultimate production standard we are building towards, mapping both our **current infrastructure** and our **future roadmap**.

---

## 💎 THE MASTER VISUAL (The North Star)

```text
FEATURE SLICE: CreateTask (Representative Flow)

┌─────────────────────────────────────────────────────────────┐
│ COMMAND LAYER                                               │
├─────────────────────────────────────────────────────────────┤
│ CreateTaskCommand
│     ↓
│ Decorator: ValidationDecorator (FluentValidator)
│     ↓
│ Decorator: CachingDecorator (cache invalidation strategy)
│     ↓
│ Decorator: DistributedLockDecorator (Redlock for concurrent writes)
│     ↓
│ Decorator: CircuitBreakerDecorator (Polly)
│     ↓
│ CreateTaskCommandHandler (actual logic)
│     ├─ UnitOfWork.Begin()
│     ├─ Domain logic (TaskAggregate.Create)
│     ├─ Publish DomainEvent (TaskCreated)
│     └─ UnitOfWork.Commit()
└─────────────────────────────────────────────────────────────┘
                         ↓
┌─────────────────────────────────────────────────────────────┐
│ EVENT PUBLISHING (Outbox Pattern - guaranteed delivery)     │
├─────────────────────────────────────────────────────────────┤
│ TaskCreatedDomainEvent
│     ↓ (Outbox table persisted with transaction)
│ EventPublisher (MediatR INotificationHandler)
│     ├─ IN-PROCESS Handlers (synchronous, same transaction)
│     │   ├─ GridOccupancyHandler
│     │   │   └─ Update Dashboard aggregate
│     │   │       └─ Invalidate Redis cache key: "dashboard:grid:{workspaceId}"
│     │   │
│     │   ├─ UpdateTaskCountHandler
│     │   │   └─ Increment Redis counter: "tasks:count:{userId}:{status}"
│     │   │
│     │   └─ PublishIntegrationEventHandler
│     │       └─ Write to OutboxEvents table
│     │
│     └─ ASYNC/DISTRIBUTED (separate transaction, Outbox worker)
│         ├─ IntegrationEvent published to MassTransit
│         └─ Outbox Relay Worker (polls OutboxEvents → publishes to RabbitMQ/AWS SQS)
└─────────────────────────────────────────────────────────────┘
                         ↓
┌─────────────────────────────────────────────────────────────┐
│ CACHE LAYER (Redis)                                         │
├─────────────────────────────────────────────────────────────┤
│ ├─ Cache invalidation: "dashboard:grid:{workspaceId}"
│ ├─ Cache invalidation: "tasks:user:{userId}"
│ ├─ Cache invalidation: "task:list:{workspaceId}:*"
│ ├─ Warm cache: Pre-fetch frequently accessed keys (background job)
│ └─ TTL strategy: different TTLs per data type
│     ├─ Hot data (task status): 5 min
│     ├─ Cold data (workspace settings): 1 hour
│     └─ Semi-hot (user preferences): 15 min
└─────────────────────────────────────────────────────────────┘
                         ↓
┌─────────────────────────────────────────────────────────────┐
│ REAL-TIME NOTIFICATION (SignalR Hub)                        │
├─────────────────────────────────────────────────────────────┤
│ TaskCreatedIntegrationEvent
│     ↓
│ SignalRNotificationHandler
│     ├─ Resolve connected users: "task_updated_{workspaceId}"
│     ├─ Send to hub: hubContext.Clients.Group(groupName)
│     │   .SendAsync("TaskCreated", taskDTO)
│     ├─ Broadcast via Redis pub/sub for multi-server scaling
│     │   └─ SignalR backplane (StackExchange.Redis)
│     └─ Rate limit: sliding window (1000 msgs/sec per connection)
└─────────────────────────────────────────────────────────────┘
                         ↓
┌─────────────────────────────────────────────────────────────┐
│ READ MODEL PROJECTION (Event-driven)                        │
├─────────────────────────────────────────────────────────────┐
│ TaskCreatedIntegrationEvent
│     ↓
│ ProjectionHandler (eventually consistent)
│     ├─ Read from EventStore (Event Sourcing) OR
│     ├─ Read from MassTransit message (event-streaming)
│     ├─ Update read model:
│     │   ├─ TaskListProjection (PostgreSQL materialized view)
│     │   ├─ TaskSearchProjection (Elasticsearch index)
│     │   └─ TaskAnalyticsProjection (TimescaleDB)
│     └─ Invalidate related caches
└─────────────────────────────────────────────────────────────┘
                         ↓
┌─────────────────────────────────────────────────────────────┐
│ EXTERNAL INTEGRATIONS (Distributed Handlers)                │
├─────────────────────────────────────────────────────────────┤
│ TaskCreatedIntegrationEvent routed to:
│     ├─ NotificationSlice Consumer
│     │   ├─ Email service (with retry: Polly exponential backoff)
│     │   └─ SMS service (with rate limiting: sliding window token bucket)
│     │
│     ├─ SlackIntegrationSlice Consumer
│     │   ├─ Webhook post (circuit breaker + timeout)
│     │   └─ Fallback: queue for batch processing
│     │
│     ├─ ElasticsearchIndexingConsumer
│     │   ├─ Bulk indexing (batch size: 1000 docs)
│     │   ├─ Retry policy: dead letter queue after 3 failures
│     │   └─ Monitor: Elastic APM instrumentation
│     │
│     ├─ DataWarehouseConsumer (analytics)
│     │   ├─ Kafka streaming to data lake
│     │   └─ ETL pipeline to Snowflake/BigQuery
│     │
│     └─ WebhookConsumer (customer integrations)
│         ├─ Retry with exponential backoff
│         ├─ Signature verification (HMAC-SHA256)
│         └─ Rate limit per customer: 100 req/sec
└─────────────────────────────────────────────────────────────┘
                         ↓
│ MONITORING & RESILIENCE                                     │
├─────────────────────────────────────────────────────────────┐
│ ├─ Structured logging (Serilog + Seq)
│ │   └─ Log all events + timings for tracing
│ ├─ Distributed tracing (OpenTelemetry + Jaeger)
│ │   └─ Track event flow across services
│ ├─ Health checks
│ │   ├─ Redis connection
│ │   ├─ RabbitMQ connection
│ │   ├─ Elasticsearch cluster
│ │   └─ Database connection pool
│ ├─ Metrics (Prometheus)
│ │   ├─ Handler execution time (histogram)
│ │   ├─ Event publish latency (gauge)
│ │   ├─ Cache hit rate (counter)
│ │   └─ Failed event count (counter)
│ └─ Dead Letter Queue handling
│     └─ Poison pill detection + manual intervention
└─────────────────────────────────────────────────────────────┘
```

---

## 🛠️ THE PRAGMATIC MAPPING (The "Shit We Have Now")

This section maps the **Gold Standard** (above) directly to the **TaskPlanner Codebase**.

### 1. The Command Pipeline (`Application.Pipeline`)
- **Status**: ✅ **Implemented**
- **Mapping**: 
    - `ValidationBehavior.cs` -> FluentValidation.
    - `TransactionBehavior.cs` -> **Managed persistence**.
- **RULE**: Handlers MUST NOT call `SaveChangesAsync()`. It is the Pipeline's job.

### 2. Caching & State Sync (`Application.Features`)
- **Status**: ✅ **Implemented** (Phase 1)
- **Mapping**: 
    - **`HybridCache`** -> Our primary Redis-backed cache layer. 
    - **Keys**: Centralized in `[Feature]CacheKeys.cs`.
    - **Invalidation**: Tag-based (e.g., `_cache.RemoveByTagAsync`).

### 3. Real-time Notification
- **Status**: ✅ **Implemented**
- **Mapping**: 
    - **`IRealtimeService`** -> Abstraction over SignalR. 
    - **Standard**: Broadcast at the end of every Command handler.

---

## 🚧 ROADMAP (The "Missing Shit" We're Building Toward)

- [ ] **Distributed Locks**: Redlock implementation for concurrent write safety.
- [ ] **Outbox Pattern**: Moving persistence of events to a separate table for guaranteed delivery.
- [ ] **MassTransit/RabbitMQ**: For distributed consumers (Notifications, Indexing).
- [ ] **Elasticsearch**: Dedicated read-model projection for search.
- [ ] **OpenTelemetry**: Integrated distributed tracing.

---

## 🏗️ Implementation Standards

### 1. The Handler Pattern
- Use **Clean, focused private methods**. No `// Block 1` comments.
- **NEVER** call `SaveChangesAsync()`.
- Broadcast state changes via `IRealtimeService` as the final handler step.

### 2. High-Performance Reads
- Use LINQ `from...join...select` syntax.
- **Always** include `.AsNoTracking()`.
- Define DTOs inside the same `.cs` file as the Query.
- Casing: **C# Properties = PascalCase**, **JSON Wire = camelCase**.

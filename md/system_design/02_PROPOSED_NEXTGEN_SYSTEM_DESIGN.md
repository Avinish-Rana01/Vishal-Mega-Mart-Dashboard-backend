# 🚀 02. Proposed Next-Gen System Design Specification
## Cloud-Native, Event-Driven RFID Retail & Real-Time Analytics Platform

> **Document Version**: 2.0 (Greenfield Vision & Next-Gen Architecture)  
> **Design Philosophy**: High-throughput event streaming, CQRS polyglot persistence, Change Data Capture (CDC), horizontally scalable real-time push, and edge RFID processing.  
> **Target Scale**: 1,000+ Retail Stores, 50 Distribution Centers, 100,000+ concurrent users, and 10,000,000+ physical RFID scans per hour.

---

## 📑 Table of Contents
1. [Architectural Goals & Principles](#1-architectural-goals--principles)
2. [Next-Gen System Topology Diagram](#2-next-gen-system-topology-diagram)
3. [Edge Layer: High-Speed RFID Stream Ingestion](#3-edge-layer-high-speed-rfid-stream-ingestion)
   - [3.1 Edge Micro-Daemons & Reader Filtering](#31-edge-micro-daemons--reader-filtering)
   - [3.2 Event Streaming Pipeline (Kafka / Redpanda)](#32-event-streaming-pipeline-kafka--redpanda)
4. [Backend Architecture (Event-Driven CQRS Micro-Services)](#4-backend-architecture-event-driven-cqrs-micro-services)
   - [4.1 Command vs. Query Responsibility Segregation (CQRS)](#41-command-vs-query-responsibility-segregation-cqrs)
   - [4.2 Polyglot Persistence: Right Database for the Right Job](#42-polyglot-persistence-right-database-for-the-right-job)
   - [4.3 Zero-Poll Change Data Capture (CDC via Debezium)](#43-zero-poll-change-data-capture-cdc-via-debezium)
   - [4.4 Distributed Cache & Pub/Sub (Redis / Dragonfly)](#44-distributed-cache--pubsub-redis--dragonfly)
   - [4.5 Scalable Real-Time Gateway (SignalR + Redis Backplane)](#45-scalable-real-time-gateway-signalr--redis-backplane)
5. [Frontend Architecture (React 19 + TanStack Query + Virtualization)](#5-frontend-architecture-react-19--tanstack-query--virtualization)
   - [5.1 TanStack Query & Optimistic State Mutations](#51-tanstack-query--optimistic-state-mutations)
   - [5.2 Web Workers for Client-Side Stream Processing](#52-web-workers-for-client-side-stream-processing)
   - [5.3 Virtualized Data Grids for 100K+ Articles](#53-virtualized-data-grids-for-100k-articles)
6. [Identity, Security & Governance (OAuth2 / OIDC + ABAC)](#6-identity-security--governance-oauth2--oidc--abac)
7. [Observability & Resilience (OpenTelemetry + Prometheus + Grafana)](#7-observability--resilience-opentelemetry--prometheus--grafana)

---

## 1. Architectural Goals & Principles

If designed from scratch without legacy database constraints, the platform should achieve:
1. **Zero Database Polling**: State changes are driven exclusively by **event streams and Change Data Capture (CDC)**, completely eliminating periodic SQL polling queries.
2. **Sub-5ms Hot Read Latency**: Pre-computed inventory projections in distributed in-memory clusters (Redis/Dragonfly).
3. **Massive Stream Ingestion Capacity**: Capable of ingesting **50,000 RFID reads/second** without locking transactional tables.
4. **Horizontal Scalability (Stateless Application Tier)**: Any number of backend instances can be spun up behind a load balancer without cache synchronization issues.
5. **Resilient Offline-First Edge Operation**: Retail store scanners continue to operate during network outages and sync seamlessly upon reconnection.

---

## 2. Next-Gen System Topology Diagram

```mermaid
flowchart TB
    subgraph EdgeLayer["Edge Layer (Stores & DC RFID Readers)"]
        FixedGate["RFID Gate Reader\n(Impinj / Zebra)"]
        Handheld["Handheld Sled\n(Zebra TC26)"]
        POS["Store POS Checkout"]
        EdgeDaemon["Edge Rust/Go Micro-Daemon\n(Smoothing & De-duplication)"]
    end

    subgraph Ingestion["High-Throughput Ingestion Pipeline"]
        Kafka["Apache Kafka / Redpanda\nTopics: rfid.raw.reads, pos.sales, grc.inward"]
        FlinkWorker["Stream Processor (Apache Flink / .NET Channels)\nSliding Window Tag Directionality"]
    end

    subgraph CoreBackend["Distributed Backend Tier (Kubernetes Cluster)"]
        IngestionSvc["RFID Ingestion Service (gRPC)"]
        CommandSvc["Inventory Command API (.NET 10 Web API)"]
        QuerySvc["Real-Time Query API (.NET 10 Minimal APIs)"]
        SignalRCluster["SignalR Real-Time Cluster\n(Distributed WebSocket Nodes)"]
    end

    subgraph DataPlane["Polyglot Persistence Data Plane"]
        Postgres[("Transactional DB (PostgreSQL)\nOrders, Audits, Users")]
        ClickHouse[("Analytical DB (ClickHouse)\nTime-Series Tag Scans & Hourly Metrics")]
        RedisCluster[("Distributed Cache (Dragonfly / Redis)\nReal-Time Store Inventory Projections")]
        Debezium["Debezium CDC\nTransaction Log Change Capture"]
    end

    subgraph ClientTier["Modern Frontend SPA (React 19 + TanStack)"]
        WebSPA["Next.js / Vite React 19 SPA\nTanStack Query + Virtual Grid"]
        Worker["Web Worker (Bulk Tag Processing)"]
    end

    %% Edge Ingestion Flow
    FixedGate --> EdgeDaemon
    Handheld --> EdgeDaemon
    POS --> EdgeDaemon
    EdgeDaemon -->|gRPC Stream (mTLS)| IngestionSvc
    IngestionSvc --> Kafka
    Kafka --> FlinkWorker
    FlinkWorker -->|Bulk Append| ClickHouse
    FlinkWorker -->|Update Projections| RedisCluster

    %% Command / Write Flow
    WebSPA -->|REST / GraphQL Mutation| CommandSvc
    CommandSvc --> Postgres
    Postgres -->|Write-Ahead Log (WAL)| Debezium
    Debezium --> Kafka

    %% Read Flow
    WebSPA -->|Instant Query (<5ms)| QuerySvc
    QuerySvc <-->|Fast Lookups| RedisCluster
    QuerySvc <-->|Complex Aggregations| ClickHouse

    %% Real-time Push Flow
    Kafka -->|CDC Event Trigger| SignalRCluster
    RedisCluster -.->|Redis Pub/Sub Backplane| SignalRCluster
    SignalRCluster -->|Multiplexed WebSocket Stream| WebSPA
    WebSPA --> Worker
```

---

## 3. Edge Layer: High-Speed RFID Stream Ingestion

### 3.1 Edge Micro-Daemons & Reader Filtering
In retail stores, physical RFID readers can report the same tag 50 times per second while an item sits on a shelf. Passing all 50 reads over the WAN to the cloud database wastes bandwidth and exhausts database threads.

**The Solution**: An ultra-lightweight **Rust or Go edge micro-daemon** installed on the local store server:
- **RSSI (Signal Strength) Smoothing**: Discards weak reflection signals.
- **De-duplication Buffer**: Employs an in-memory sliding window filter (100ms) to ensure each unique EPC (Electronic Product Code) is reported once per scan event.
- **Offline Journaling**: Stores scans in local SQLite embedded database if WAN connectivity drops, streaming backlog upon reconnection.

### 3.2 Event Streaming Pipeline (Kafka / Redpanda)
Edge daemons stream compacted tag events to an **Apache Kafka / Redpanda** cluster over gRPC with mTLS:
- Topic `rfid.raw.scans`: Partitioned by `StoreCode` to guarantee strict chronological ordering per store.
- Throughput: Capable of handling over 1,000,000 events/minute with sub-10ms broker latency.

---

## 4. Backend Architecture (Event-Driven CQRS Micro-Services)

### 4.1 Command vs. Query Responsibility Segregation (CQRS)
Instead of forcing stored procedures to handle both transactional writes and complex dashboard reads, we decouple the paths:

```
          ┌───────────────────────────────────────────────┐
          │                  Client SPA                   │
          └───────────────┬───────────────────────────────┘
                          │
            ┌─────────────┴─────────────┐
            ▼                           ▼
  [Command Path (Writes)]     [Query Path (Reads)]
            │                           │
   Inventory Command API        Real-Time Query API
            │                           │
  Transactional PostgreSQL    Redis & ClickHouse Projections
```

- **Command Path**: Processes validations, dispatches, and cycle counts. Highly normalized, ACID-compliant PostgreSQL with row-level locks.
- **Query Path**: Reads directly from pre-computed, de-normalized read models in Redis or ClickHouse in **under 3 ms**.

### 4.2 Polyglot Persistence: Right Database for the Right Job

| Storage Technology | Purpose in System | Why Chosen |
| :--- | :--- | :--- |
| **PostgreSQL 16** | Core Business Data (Users, Permissions, Audit Masters) | Bulletproof ACID transactions, JSONB flexibility, rich relational constraints. |
| **ClickHouse** | Time-Series Tag Scans & Analytical Reports | Columnar storage capable of aggregating 100,000,000 tag movements in milliseconds. |
| **Redis / Dragonfly** | Real-Time Store Inventory & Active Sessions | In-memory key-value engine delivering sub-millisecond query responses. |

### 4.3 Zero-Poll Change Data Capture (CDC via Debezium)
Instead of background timers executing SQL queries every 2 or 4 seconds, **Debezium captures database mutations directly from the PostgreSQL Write-Ahead Log (WAL)**:

1. When a cashier completes a sale, PostgreSQL writes to the WAL.
2. Debezium streams a `InventoryChangedEvent` into Kafka within **5 milliseconds**.
3. A lightweight consumer updates the Redis store projection.
4. The SignalR gateway pushes the delta patch to connected clients.
5. **Zero overhead on the primary database engine!**

### 4.4 Distributed Cache & Pub/Sub (Redis / Dragonfly)
In place of local in-process `IMemoryCache` (which cannot share state across multiple backend nodes), the Next-Gen design uses a **Redis/Dragonfly cluster**:
- **Store Projections**: Hash sets `store:{storeCode}:stock` storing current `rfid_qty`, `sap_qty`, and `variance`.
- **Atomic Operations**: Uses `HINCRBY` to adjust stock counts atomically without locks.

### 4.5 Scalable Real-Time Gateway (SignalR + Redis Backplane)
Multiple backend API pods connect to a shared Redis Pub/Sub backplane. When an update occurs on Node A, Redis broadcasts the patch to Nodes B, C, and D, ensuring all connected browsers receive instant updates regardless of which server holds their WebSocket connection.

---

## 5. Frontend Architecture (React 19 + TanStack Query + Virtualization)

### 5.1 TanStack Query & Optimistic State Mutations
The frontend replaces manual `fetch()` calls and custom caching with **TanStack Query (React Query)**:
- **Automatic Background Synchronization**: Window refocus and network reconnection auto-revalidate queries.
- **Optimistic Updates**: When a cashier or auditor marks an item counted, the UI updates **instantly (0ms)** while the mutation promise executes in the background.

```javascript
// Next-Gen Frontend: Optimistic UI Mutation
const mutation = useMutation({
  mutationFn: updateCycleCountScan,
  onMutate: async (newScan) => {
    await queryClient.cancelQueries(['cycleCount', activeAuditId]);
    const previousState = queryClient.getQueryData(['cycleCount', activeAuditId]);
    
    // Optimistically update UI before server responds!
    queryClient.setQueryData(['cycleCount', activeAuditId], old => ({
      ...old,
      scannedQty: old.scannedQty + newScan.qty
    }));
    return { previousState };
  },
  onError: (err, newScan, context) => {
    // Rollback if network or server validation fails
    queryClient.setQueryData(['cycleCount', activeAuditId], context.previousState);
  }
});
```

### 5.2 Web Workers for Client-Side Stream Processing
When physical handheld sleds stream 500 tags/second during a full-store cycle count, running JSON parsing and array diffing on the main UI thread freezes animations.
- **Dedicated Web Worker**: Heavy array filtering, barcode translation, and EPC string parsing execute in a background browser worker thread.
- **Main Thread**: Remains locked at a silky-smooth **60 FPS**.

### 5.3 Virtualized Data Grids for 100K+ Articles
Rather than rendering 10,000 DOM `<tr>` nodes (which bloats DOM memory to 500MB), the UI utilizes **TanStack Virtual**:
- Only the 25 rows currently visible in the user's viewport are rendered.
- Memory usage remains under **15 MB** regardless of how large the dataset is.

---

## 6. Identity, Security & Governance (OAuth2 / OIDC + ABAC)

### Modern Identity Architecture
- **Identity Provider**: Centralized OIDC identity server (Keycloak, Auth0, or Microsoft Entra ID).
- **Security Protocol**: PKCE-enhanced OAuth2 authorization code flow (eliminates stored passwords in DB).
- **Authorization**: **Attribute-Based Access Control (ABAC)** powered by Open Policy Agent (OPA). Fine-grained policies evaluate contextual attributes:
  ```rego
  # OPA Policy Rule
  allow {
      input.user.role == "StoreAdmin"
      input.action == "view_inventory"
      input.resource.storeCode == input.user.assignedStore
  }
  ```

---

## 7. Observability & Resilience (OpenTelemetry + Prometheus + Grafana)

The entire distributed stack implements full OpenTelemetry distributed tracing:
- **Tracing**: Every tag scan event carries a `traceparent` header from Edge Reader $\rightarrow$ Kafka $\rightarrow$ .NET Service $\rightarrow$ SignalR $\rightarrow$ Browser.
- **Metrics**: Real-time Prometheus dashboards track:
  - WebSocket active connection count.
  - Kafka consumer group lag.
  - Redis cache hit ratio.
  - Tag scan ingestion velocity (tags/sec).
- **Self-Healing Circuit Breakers**: Built with **Polly v8** in .NET, automatically degrading to local cache if upstream analytical databases become unresponsive.

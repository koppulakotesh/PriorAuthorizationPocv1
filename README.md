# Prior Authorization FHIR API — Learning POC

A simple .NET 9 proof of concept that shows how an **asynchronous Prior Authorization** flow works:

```
Consumer
   ↓
FHIR API
   ↓
Validation
   ↓
PAS Service
   ↓
SQL Server
   ↓
Queue  (System.Threading.Channels)
   ↓
Background Worker
   ↓
FHIR Mapper
   ↓
QAR Mock API
   ↓
ServiceNow Mock API
   ↓
Update PAS DB
   ↓
Audit
```

This is a **learning project**. It is intentionally small. There are no microservices, Docker, Kubernetes, Azure Service Bus, Redis, or API gateways.

---

## 1. Architecture

The API does **not** call QAR. It validates, saves, queues, and returns `202 Accepted`. A background worker does the slow external work.

```
                    +------------------+
   POST JSON        |  PriorAuth API   |
 -----------------> |  (thin controller|
                    |   + middleware)  |
                    +--------+---------+
                             |
                             v
                    +------------------+
                    | Application      |
                    | Service          |
                    |  - validate      |
                    |  - save PA       |
                    |  - write audit   |
                    |  - publish queue |
                    +--------+---------+
                             |
              +--------------+---------------+
              |                              |
              v                              v
     +----------------+              +----------------+
     | SQL Server     |              | In-memory      |
     | PriorAuths     |              | Channel queue  |
     | PAAudits       |              +--------+-------+
     +----------------+                       |
                                              v
                                     +----------------+
                                     | PAProcessing   |
                                     | Worker         |
                                     +--------+-------+
                                              |
                                              v
                                     +----------------+
                                     | Processor      |
                                     |  Received ->   |
                                     |  Processing    |
                                     +--------+-------+
                                              |
                                              v
                                     +----------------+
                                     | FHIR Mapper    |
                                     +--------+-------+
                                              |
                                              v
                                     +----------------+
                                     | Mock QAR       |
                                     | (1s delay,     |
                                     |  3 retries)    |
                                     +--------+-------+
                                              |
                                              v
                                     +----------------+
                                     | Mock ServiceNow|
                                     +--------+-------+
                                              |
                                              v
                                     +----------------+
                                     | Update DB      |
                                     | Status=Submitted
                                     | + Audit        |
                                     +----------------+
```

**Why 202 Accepted?**  
Submit, update, and cancel are accepted immediately. The consumer gets an id and a correlation id. Inquiry (`GET`) is synchronous and reads SQL.

**Important for this POC:** the worker is hosted **inside the API process** so the in-memory Channel is shared. That is how `POST` then `GET` can show `Submitted` after a couple of seconds. The `PriorAuthorization.Worker` project is the same worker as a standalone host. Use the standalone worker later, when you replace the Channel with a real message broker.

---

## 2. Project structure

```
PriorAuthorizationPOC/
├── PriorAuthorizationPOC.sln
├── README.md
├── PriorAuthorization.Api              HTTP endpoints, Swagger, middleware
│   ├── Controllers/
│   │   └── PriorAuthorizationController.cs
│   ├── Middleware/
│   │   ├── CorrelationIdMiddleware.cs
│   │   └── ExceptionHandlingMiddleware.cs
│   ├── Program.cs
│   └── appsettings.json
├── PriorAuthorization.Application      Use cases, validation, processor
│   ├── Dtos/
│   ├── Exceptions/
│   ├── Interfaces/
│   ├── Services/
│   └── Validators/
├── PriorAuthorization.Domain           Entities and enums only
│   ├── Entities/
│   ├── Enums/
│   └── Models/
├── PriorAuthorization.Infrastructure   EF Core, queue, mapper, mocks
│   ├── Data/
│   ├── External/
│   ├── Mapping/
│   ├── Queue/
│   ├── Repositories/
│   └── DependencyInjection.cs
└── PriorAuthorization.Worker           BackgroundService host
    ├── PAProcessingWorker.cs
    └── Program.cs
```

Layer direction:

```
Api  --------->  Application  --------->  Domain
  |                  ^
  |                  |
  +--> Infrastructure (implements Application interfaces)
  |
  +--> Worker (hosts PAProcessingWorker)

Worker ------> Application
     +-------> Infrastructure
```

---

## 3. Configure SQL Server

1. Install SQL Server (Developer / Express / LocalDB) and make sure it is running.
2. Open `PriorAuthorization.Api/appsettings.json` (and the Worker copy if you use it):

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=PriorAuthorizationPOC;Trusted_Connection=True;TrustServerCertificate=True;"
}
```

If you use a full SQL Server instance instead of LocalDB:

```json
"Server=localhost;Database=PriorAuthorizationPOC;Trusted_Connection=True;TrustServerCertificate=True;"
```

Other examples:

- `Server=.\\SQLEXPRESS`
- `Server=(localdb)\\mssqllocaldb`

The database `PriorAuthorizationPOC` is created automatically on first run (`Database.Migrate()` in `Program.cs`).

---

## 4. Run migrations

The API applies migrations at startup. To create or add migrations yourself:

```bash
dotnet tool install --global dotnet-ef

cd PriorAuthorizationPOC

dotnet ef migrations add InitialCreate ^
  --project PriorAuthorization.Infrastructure ^
  --startup-project PriorAuthorization.Api

dotnet ef database update ^
  --project PriorAuthorization.Infrastructure ^
  --startup-project PriorAuthorization.Api
```

Tables:

- `PriorAuthorizations`
- `PAAudits`

---

## 5. Run the API

From the solution folder:

```bash
dotnet run --project PriorAuthorization.Api
```

Then open Swagger:

http://localhost:5080/swagger

The API process also starts `PAProcessingWorker`. You do **not** need a second terminal for the happy-path demo.

---

## 6. Run the Worker (optional)

```bash
dotnet run --project PriorAuthorization.Worker
```

Do this only after you replace `InMemoryPAQueue` with a real broker. Two processes **cannot** share `System.Threading.Channels`. For this POC, run the API only.

---

## 7. Test Submit

`POST /api/prior-authorizations`

```bash
curl -X POST http://localhost:5080/api/prior-authorizations ^
  -H "Content-Type: application/json" ^
  -H "X-Correlation-Id: demo-001" ^
  -d "{ \"resourceType\": \"PriorAuthorization\", \"patient\": { \"id\": \"PAT001\", \"name\": \"John Doe\" }, \"provider\": { \"id\": \"PROV001\", \"name\": \"ABC Hospital\" }, \"insurance\": { \"id\": \"INS001\", \"name\": \"ABC Insurance\" }, \"procedure\": { \"code\": \"MRI001\", \"description\": \"MRI Scan\" } }"
```

Expected: **202 Accepted**

```json
{
  "id": 1,
  "correlationId": "demo-001",
  "status": "Received",
  "message": "Prior authorization request received"
}
```

Wait about 1–2 seconds for the worker (mock QAR has a 1 second delay).

---

## 8. Test Inquiry

`GET /api/prior-authorizations/{id}`

This is **synchronous**. No queue.

```bash
curl http://localhost:5080/api/prior-authorizations/1
```

Expected after the worker finishes:

```json
{
  "resourceType": "PriorAuthorization",
  "id": 1,
  "status": "Submitted",
  "correlationId": "demo-001",
  "externalReferenceId": "QAR-10001",
  "patient": { "id": "PAT001", "name": "John Doe" },
  "provider": { "id": "PROV001", "name": "ABC Hospital" },
  "insurance": { "id": "INS001", "name": "ABC Insurance" },
  "procedure": { "code": "MRI001", "description": "MRI Scan" }
}
```

---

## 9. Test Update

`PUT /api/prior-authorizations/{id}`

Validates, updates SQL, queues again, returns **202**.

```bash
curl -X PUT http://localhost:5080/api/prior-authorizations/1 ^
  -H "Content-Type: application/json" ^
  -d "{ \"resourceType\": \"PriorAuthorization\", \"patient\": { \"id\": \"PAT001\", \"name\": \"John Doe\" }, \"provider\": { \"id\": \"PROV001\", \"name\": \"ABC Hospital\" }, \"insurance\": { \"id\": \"INS001\", \"name\": \"ABC Insurance\" }, \"procedure\": { \"code\": \"CT001\", \"description\": \"CT Scan\" } }"
```

---

## 10. Test Cancel

`POST /api/prior-authorizations/{id}/cancel`

Returns **202**. The worker calls mock QAR cancel, then sets status to `Cancelled`.

```bash
curl -X POST http://localhost:5080/api/prior-authorizations/1/cancel
```

---

## 11. How the queue works

Interface: `IPAQueue`  
Implementation: `InMemoryPAQueue` (`System.Threading.Channels`)

Message:

```csharp
public class PAQueueMessage
{
    public long PriorAuthorizationId { get; set; }
    public string CorrelationId { get; set; }
    public string Action { get; set; }   // Submit | Update | Cancel
}
```

Flow:

```
API service  ->  IPAQueue.PublishAsync()
                      |
                      v
              Channel<PAQueueMessage>
                      |
                      v
Worker  ->  IPAQueue.ReadAllAsync()
```

To use Azure Service Bus later, implement `IPAQueue` with a Service Bus sender/receiver. Callers do not change.

---

## 12. How the worker works

Class: `PAProcessingWorker` (`BackgroundService`)

1. Reads a message from the channel.
2. Creates a **DI scope** (so it can use EF Core `DbContext`).
3. Resolves `IPriorAuthorizationProcessor`.
4. Calls `ProcessAsync`.

The worker does **not** contain QAR or SQL logic.

Processor steps for Submit/Update:

1. Load PA from SQL.
2. Status `Received` → `Processing` + audit.
3. Map entity → canonical model (`FhirMapper`).
4. Call mock QAR (up to 3 attempts).
5. On success: save `ExternalReferenceId`, status → `Submitted` + audit.
6. Call mock ServiceNow + audit.
7. On 3 failures: status → `Failed`, store `LastError` + audit.

---

## 13. How the QAR mock works

`IQarClient` → `MockQarClient`

- Injects `HttpClient` to show the production shape.
- Does **not** call a real system.
- Waits 1 second (`Task.Delay(1000)`).
- Returns:

```json
{
  "success": true,
  "externalReferenceId": "QAR-10001",
  "status": "Submitted"
}
```

QAR is retried 3 times in `PriorAuthorizationProcessor`.

---

## 14. How the ServiceNow mock works

`IServiceNowClient` → `MockServiceNowClient`

- Same idea as QAR: `HttpClient` is injected, no real HTTP call.
- Returns:

```json
{
  "success": true,
  "ticketNumber": "INC0010001"
}
```

---

## 15. How correlation ID works

Middleware: `CorrelationIdMiddleware`

- If the request has `X-Correlation-Id`, that value is used.
- Otherwise a GUID is generated.
- The same value is returned on the response header `X-Correlation-Id`.
- It is stored on the PA row, the queue message, QAR/ServiceNow logs, and audit rows.

`ICorrelationContext` uses `AsyncLocal<string>` so API and worker each see the id for their current flow.

---

## 16. How audit works

`IPAAuditService` → `PAAuditService` writes `PAAudits`.

Typical events:

| From        | To          | When                         |
|-------------|-------------|------------------------------|
| (none)      | Received    | API saved the request        |
| Received    | Processing  | Worker picked up the message |
| Processing  | Submitted   | Mock QAR succeeded           |
| Processing  | Cancelled   | Cancel succeeded             |
| Processing  | Failed      | QAR failed 3 times           |

Every important status change is a new audit row with `CorrelationId`.

---

## Demonstration flow

1. `POST /api/prior-authorizations` → **202**, status **Received**, message in Channel.
2. Worker reads the message → status **Processing**.
3. FHIR mapper builds the canonical model.
4. Mock QAR returns `QAR-10001`.
5. Status **Submitted**, `ExternalReferenceId` saved.
6. Mock ServiceNow returns `INC0010001`.
7. Audit rows saved.
8. `GET /api/prior-authorizations/{id}` → **Submitted**.

---

## Swagger endpoints

| Method | Path | Result |
|--------|------|--------|
| POST | `/api/prior-authorizations` | 202 Accepted |
| GET | `/api/prior-authorizations/{id}` | 200 FHIR-like body |
| PUT | `/api/prior-authorizations/{id}` | 202 Accepted |
| DELETE | `/api/prior-authorizations/{id}` | 204 No Content |
| POST | `/api/prior-authorizations/{id}/cancel` | 202 Accepted |

---

## What each project does

| Project | Responsibility |
|---------|----------------|
| **Api** | HTTP, Swagger, correlation and error middleware. Controllers only call the application service. |
| **Application** | Use cases: submit, get, update, cancel, process, validate, audit orchestration. |
| **Domain** | `PriorAuthorization`, `PAAudit`, `PAStatus`, canonical model. No EF, no HTTP. |
| **Infrastructure** | EF Core, SQL, Channel queue, FHIR mapper, mock QAR, mock ServiceNow, DI registration. |
| **Worker** | `BackgroundService` that reads the queue and calls the processor. |

---

## Production-ready improvements (not in this POC)

1. Replace `System.Threading.Channels` with Azure Service Bus / queue with persistence, competing consumers, and poison messages.
2. Run the worker as a separate process or Azure Function.
3. Use real FHIR R4 `Claim` / PAS (`PASInquiryResponse`) profiles instead of a toy JSON shape.
4. Replace mocks with real QAR and ServiceNow HTTP APIs, timeouts, and auth.
5. Use Polly (or similar) for retries, circuit breaker, and jitter — not a `for` loop.
6. Add authentication / authorization (OAuth2 / SMART on FHIR).
7. Encrypt or tokenize patient identifiers; never log names.
8. Add idempotency keys so duplicate POSTs do not create two PAs.
9. Outbox pattern so “save SQL + publish queue” is atomic.
10. Health checks, metrics, distributed tracing (`Activity` / Application Insights).
11. Proper FHIR validation (profiles, terminologies).
12. Concurrency tokens on the PA row.
13. Integration tests and contract tests for QAR.
14. Secrets in a vault, not `appsettings.json`.
15. Structured logging with a logging scope for `CorrelationId`.

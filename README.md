# MyToDo – Workflow Engine + APS Scheduler

A .NET 10 ASP.NET Core API that combines a lightweight **workflow engine** (inspired by Elsa Workflow) with an **APS (Advanced Planning and Scheduling)** scheduler and a **work-order** management system.

---

## Architecture overview

| Layer | Key types |
|---|---|
| **Domain entities** | `WorkOrder`, `WorkflowInstance`, `WorkflowExecutionToken`, `WorkflowNodeInstance`, `WorkflowBookmark`, `SchedulableTask`, `ScheduleResult` |
| **Workflow runtime** | `IWorkflowRuntime` / `WorkflowRuntime` – token-based execution loop |
| **Node executors** | `IWorkflowNodeExecutor` / `WorkflowNodeExecutorRegistry` – pluggable per-node logic (`StartNodeExecutor`, `EndNodeExecutor`, `ScheduleTaskNodeExecutor`, `WorkstationTaskNodeExecutor`) |
| **Bookmark service** | `IWorkflowBookmarkService` / `WorkflowBookmarkService` – suspend/resume points |
| **Workstation gateway** | `IWorkstationGateway` / `FakeWorkstationGateway` – device dispatch abstraction |
| **APS scheduler** | `IApsScheduler` / `ApsScheduler` – priority + earliest-available-resource scheduler |
| **Background service** | `ApsSchedulerBackgroundService` – runs the scheduling cycle every 30 s |
| **Persistence** | EF Core 10 + SQLite (`MyToDoContext`) with `EnsureCreated` on startup |

---

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)

---

## Running locally

```bash
cd MyToDo.Api
dotnet run
```

The API starts on `http://localhost:5000` (or the port shown in the console).
The SQLite database file `mytodo.db` is created automatically on first run via `EnsureCreated()`.

SQLite connection string (`MyToDo.Api/appsettings.json`):

```json
"ConnectionStrings": {
  "ToDoConnection": "Data Source=mytodo.db"
}
```

---

## Testing the Start → WorkstationTask → End flow (fake gateway)

### 1. Bootstrap – create workflow definition + work order

```http
POST http://localhost:5000/api/WorkflowEngine/bootstrap
Content-Type: application/json

{
  "WorkflowName": "Demo Workflow",
  "WorkOrderNo": "WO-DEMO-001",
  "Priority": 5,
  "RequiredResourceType": "Workstation",
  "EstimatedDurationMinutes": 30
}
```

Note the returned `WorkOrderId` and `WorkflowVersionId`.

The bootstrap endpoint creates a `Start → ScheduleTask → WorkstationTask → End` graph and a matching scheduling resource.

### 2. Start the workflow instance

```http
POST http://localhost:5000/api/WorkflowEngine/start
Content-Type: application/json

{
  "WorkOrderId": "<WorkOrderId from step 1>",
  "WorkflowVersionId": "<WorkflowVersionId from step 1>"
}
```

Note the returned `WorkflowInstanceId`. The engine automatically passes through `Start` and suspends at `ScheduleTask`.

### 3. Run APS scheduling (advances past ScheduleTask → WorkstationTask)

```http
POST http://localhost:5000/api/WorkflowEngine/schedule
```

The scheduler allocates a resource, creates a `ScheduleResult`, and internally resumes the `ScheduleTaskScheduled` bookmark.
The `FakeWorkstationGateway` is then called, returning a `DeviceJobId` (e.g. `dev-abc123…`), and execution suspends at `WorkstationTask`.

> The `ApsSchedulerBackgroundService` also triggers this automatically every **30 seconds**.

### 4. Find the active WorkstationTask bookmark

```http
GET http://localhost:5000/api/WorkflowEngine/instances/<WorkflowInstanceId>
```

Look for a bookmark with `"BookmarkType": "WorkstationTaskCompleted"` and note its `BookmarkKey` (the `DeviceJobId`).

### 5. Simulate device completion (resume)

```http
POST http://localhost:5000/api/WorkflowEngine/resume
Content-Type: application/json

{
  "BookmarkType": "WorkstationTaskCompleted",
  "BookmarkKey": "<DeviceJobId from step 4>"
}
```

### 6. Verify completion

```http
GET http://localhost:5000/api/WorkflowEngine/instances/<WorkflowInstanceId>
```

`"status"` should now be `"Completed"`.

---

## Running tests

```bash
dotnet test MyToDo.Api.Tests/MyToDo.Api.Tests.csproj
```

The test suite (`WorkflowRuntimeApsTests`) exercises the full `Start → ScheduleTask → WorkstationTask → End` flow end-to-end using an in-memory database and `FakeWorkstationGateway`.

---

## Swapping the fake gateway for a real device

1. Implement `IWorkstationGateway` in a new class (e.g. `HttpWorkstationGateway`).
2. Replace the registration in `Program.cs`:
   ```csharp
   // builder.Services.AddScoped<IWorkstationGateway, FakeWorkstationGateway>();
   builder.Services.AddScoped<IWorkstationGateway, HttpWorkstationGateway>();
   ```
3. No other code changes are required.

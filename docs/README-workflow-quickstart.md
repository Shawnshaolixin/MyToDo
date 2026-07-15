# Workflow Runtime Quick-Start Guide

> **Scope:** This guide explains how to run the MyToDo workflow engine locally using SQLite,
> seed the minimum required data, and exercise the `Start → WorkstationTask → End` flow
> end-to-end using `curl`.

---

## Prerequisites

| Tool | Version |
|------|---------|
| .NET SDK | 8 or later (tested on 10) |
| SQLite (optional) | any — for inspecting the DB |
| curl / Postman | for API calls |

---

## 1. Run Locally

```bash
# Clone & enter the repo
git clone https://github.com/Shawnshaolixin/MyToDo.git
cd MyToDo

# Start the API (SQLite DB created automatically in development)
dotnet run --project MyToDo.Api
```

The API listens on `http://localhost:5000` (or the port shown in the console).

On first start, `db.Database.EnsureCreated()` creates `mytodo.db` in the working
directory with all tables.  **You do not need to run migrations for local dev.**

> **EnsureCreated vs Migrations:**
> - `EnsureCreated` — instant, zero-config, perfect for demos.  Does **not** apply
>   incremental schema changes; delete `mytodo.db` and restart if the model changes.
> - `Migrations` — required for production; run `dotnet ef migrations add <Name>`
>   followed by `dotnet ef database update` whenever the model changes.

---

## 2. Seed Minimal Workflow Data

The `/api/workflow-engine/bootstrap` endpoint creates the minimal data in one call:

```bash
# Step 1: Bootstrap workflow + work order + resource
curl -s -X POST http://localhost:5000/api/workflow-engine/bootstrap \
  -H "Content-Type: application/json" \
  -d '{
    "workflowName": "Demo Workflow",
    "workOrderNo": "WO-DEMO-001",
    "priority": 5,
    "requiredResourceType": "Workstation",
    "estimatedDurationMinutes": 30
  }' | python3 -m json.tool
```

Save the returned `workOrderId` and `workflowVersionId` — you'll need them next.

Alternatively, insert directly with SQL (useful for scripting):

```sql
-- workstation (required for WorkstationTask node)
INSERT INTO Workstations (Id, Code, Name, IsActive, CreatedAt)
VALUES ('11111111-0000-0000-0000-000000000001', 'WS001', 'Lab Workstation A', 1, datetime('now'));
```

---

## 3. Start the Workflow

```bash
# Replace the GUIDs with those returned by /bootstrap
curl -s -X POST http://localhost:5000/api/workflow-engine/start \
  -H "Content-Type: application/json" \
  -d '{
    "workOrderId":        "<workOrderId>",
    "workflowVersionId":  "<workflowVersionId>"
  }' | python3 -m json.tool
```

The runtime:
1. Creates a `WorkflowInstance` and `WorkflowExecutionToken` pointing at the Start node.
2. Executes Start (immediate Done) → advances to ScheduleTask node.
3. Creates a `SchedulableTask` and a `WorkflowBookmark` (`ScheduleTaskScheduled`), then suspends.

---

## 4. Run the Scheduler

The APS background service runs automatically every 30 seconds.  To trigger it
immediately via API:

```bash
curl -s -X POST http://localhost:5000/api/workflow-engine/schedule \
  | python3 -m json.tool
```

This calls `IApsScheduler.ScheduleAsync`, assigns the first matching resource,
writes a `ScheduleResult`, and resumes the `ScheduleTaskScheduled` bookmark.
The token advances to the WorkstationTask node, a `WorkstationTaskInstance` is
created in `PendingConfig` stage, and a new `WorkstationTaskCompleted` bookmark is
created.

---

## 5. Configure & Start the Experiment

```bash
# a) Get the WorkflowNodeInstanceId from the instance status
curl -s http://localhost:5000/api/workflow-engine/instances/<workflowInstanceId> \
  | python3 -m json.tool

# b) Browse available experiments (FakeWorkstationGateway returns deterministic list)
curl -s http://localhost:5000/api/workstation-tasks/<workflowNodeInstanceId>/experiments \
  | python3 -m json.tool

# c) Save experiment config
curl -s -X POST \
  http://localhost:5000/api/workstation-tasks/<workflowNodeInstanceId>/config \
  -H "Content-Type: application/json" \
  -d '{
    "experimentDefinitionId": "EXP-001",
    "parametersJson": "{\"sampleId\":\"S-100\",\"temperature\":37}"
  }' | python3 -m json.tool

# d) Start the experiment on the device
curl -s -X POST \
  http://localhost:5000/api/workstation-tasks/<workflowNodeInstanceId>/start \
  | python3 -m json.tool
```

The `FakeWorkstationGateway` returns `{ Success: true, DeviceJobId: "<guid>" }`.
Save the `DeviceJobId`.

---

## 6. Simulate Device Completion

Post an `ExperimentCompleted` event (what a real device would send):

```bash
curl -s -X POST http://localhost:5000/api/workstations/events \
  -H "Content-Type: application/json" \
  -d '{
    "workstationCode": "WS001",
    "deviceJobId":     "<DeviceJobId from step 5d>",
    "eventType":       "ExperimentCompleted",
    "payloadJson":     "{\"result\":\"OK\"}"
  }' | python3 -m json.tool
```

`WorkstationEventAppService`:
1. Persists the `WorkstationEvent`.
2. Updates `WorkstationTaskInstance.Stage → Completed`.
3. Calls `IWorkflowRuntime.ResumeAsync(WorkstationTaskCompleted, …)`.

The runtime consumes the bookmark, the token advances to the End node,
`WorkflowInstance.Status → Completed`, `WorkOrder.Status → Completed`.

---

## 7. Verify Completion

```bash
curl -s http://localhost:5000/api/workflow-engine/instances/<workflowInstanceId> \
  | python3 -m json.tool
# Expect: "status": "Completed"
```

---

## 8. Simulate a Prompt (Optional)

To exercise the prompt flow, post a `PromptRaised` event:

```bash
curl -s -X POST http://localhost:5000/api/workstations/events \
  -H "Content-Type: application/json" \
  -d '{
    "workstationCode": "WS001",
    "deviceJobId":     "<DeviceJobId>",
    "eventType":       "PromptRaised",
    "payloadJson":     "{\"promptCode\":\"ConfirmSample\",\"message\":\"Is sample loaded?\"}"
  }'

# Then resolve the prompt (find promptId in DB or via API)
curl -s -X POST http://localhost:5000/api/workstation-tasks/prompts/<promptId>/resolve \
  -H "Content-Type: application/json" \
  -d '{"resolution": "Yes"}'
```

---

## Architecture Overview

```
WorkOrder
  └─ WorkflowInstance
       ├─ WorkflowExecutionToken  (tracks current node)
       ├─ WorkflowNodeInstance[]  (one per executed node)
       └─ WorkflowBookmark[]      (suspension points)

Node executors (IWorkflowNodeExecutor):
  ├─ StartNodeExecutor     → Done (immediate)
  ├─ EndNodeExecutor       → Done (immediate, closes instance)
  ├─ ScheduleTask          → Waiting("ScheduleTaskScheduled", taskId)
  └─ WorkstationTaskExecutor → Waiting("WorkstationTaskCompleted", instanceId:nodeId)

APS Scheduler (IApsScheduler / ApsScheduler):
  ReadyForScheduling tasks → sort by priority desc, earliestStart asc
  → match first resource of RequiredResourceType → write ScheduleResult
  → resume bookmark

Background services:
  ApsSchedulingBackgroundService    — runs ScheduleAsync every 30 s
  ScheduleReleaseBackgroundService  — resumes elapsed schedule slots every 60 s
```

---

## Known Limitations

| Area | Limitation |
|------|-----------|
| **FakeWorkstationGateway** | Returns hard-coded experiment list and a new random `DeviceJobId` per call.  No actual device communication. |
| **APS algorithm** | Simplified greedy heuristic: sort by priority/earliest-start, pick first matching resource.  Does not handle resource calendars, sequence-dependent setup times, or multi-resource tasks. |
| **EnsureCreated** | Schema is recreated from scratch when the DB file is deleted.  Not suitable for production — use EF Migrations. |
| **Concurrency** | `WorkflowBookmarkService.ConsumeAsync` uses an optimistic Active-check, sufficient for SQLite's serialised writes.  Add `RowVersion` / `SELECT FOR UPDATE` for multi-instance deployments. |
| **No authentication** | All endpoints are unauthenticated.  Add JWT / API-key middleware before exposing publicly. |
| **Single token** | The runtime currently uses a single execution token per workflow instance (no parallel branches / split-join). |

---

## Running the Tests

```bash
dotnet test MyToDo.Api.Tests/MyToDo.Api.Tests.csproj
# Expected: all tests pass (Passed: 49+)
```

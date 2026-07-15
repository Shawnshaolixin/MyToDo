# MyToDo

## Workflow + APS 最小可运行说明（SQLite）

### 本地运行

```bash
cd MyToDo.Api
dotnet run
```

默认连接字符串：

```json
"ConnectionStrings": {
  "DefaultConnection": "Data Source=mytodo.db",
  "ToDoConnection": "Data Source=mytodo.db"
}
```

开发环境启动时会执行 `EnsureCreated()` 自动建表。

### 示例 SQL（可选）

```sql
-- 查看启动后是否已建表
SELECT name FROM sqlite_master WHERE type='table' ORDER BY name;
```

### curl 验证（Start -> ScheduleTask -> WorkstationTask -> End）

1) 初始化示例流程/工单/资源：

```bash
curl -s -X POST "http://localhost:5000/api/WorkflowEngine/bootstrap" \
  -H "Content-Type: application/json" \
  -d '{"workflowName":"Demo Workflow","workOrderNo":"WO-DEMO-001","priority":10,"requiredResourceType":"Workstation","estimatedDurationMinutes":30}'
```

2) 启动流程（会先跑到 `ScheduleTask` 并挂起）：

```bash
curl -s -X POST "http://localhost:5000/api/WorkflowEngine/start" \
  -H "Content-Type: application/json" \
  -d '{"workOrderId":"<WorkOrderId>","workflowVersionId":"<WorkflowVersionId>"}'
```

3) 执行 APS 调度并自动恢复 `ScheduleTask`：

```bash
curl -s -X POST "http://localhost:5000/api/WorkflowEngine/schedule"
```

4) 查询实例，拿到 `WorkstationTaskCompleted` 的 bookmark key（Fake gateway 返回 GUID）：

```bash
curl -s "http://localhost:5000/api/WorkflowEngine/instances/<WorkflowInstanceId>"
```

5) 恢复 `WorkstationTask`，流程推进到 `End` 并完成：

```bash
curl -s -X POST "http://localhost:5000/api/WorkflowEngine/resume" \
  -H "Content-Type: application/json" \
  -d '{"bookmarkType":"WorkstationTaskCompleted","bookmarkKey":"<DeviceJobId>"}'
```

### 测试

```bash
dotnet test MyToDo.Api.Tests/MyToDo.Api.Tests.csproj
```

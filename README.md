# MyToDo

## Workflow + APS 本地最小运行说明（SQLite）

本仓库已提供可运行的最小链路：`Start -> ScheduleTask -> WorkstationTask -> End`。

### 1) 本地运行与数据库初始化

```bash
cd MyToDo.Api
dotnet run
```

默认连接串位于 `MyToDo.Api/appsettings.json`：

```json
"ConnectionStrings": {
  "ToDoConnection": "Data Source=mytodo.db"
}
```

开发环境下会调用 `EnsureCreated()` 自动建表（便于本地快速验证）；生产环境建议使用 EF Migrations 管理数据库版本。

### 2) 示例 curl：跑通 Start -> WorkstationTask -> End

#### 2.1 初始化示例流程与工单

```bash
curl -s -X POST "<baseUrl>/api/WorkflowEngine/bootstrap" \
  -H "Content-Type: application/json" \
  -d '{
    "workflowName":"Demo Workflow",
    "requiredResourceType":"Workstation",
    "estimatedDurationMinutes":30,
    "priority":10
  }'
```

响应里会有 `workflowVersionId` 和 `workOrderId`。

#### 2.2 启动流程

```bash
curl -s -X POST "<baseUrl>/api/WorkflowEngine/start" \
  -H "Content-Type: application/json" \
  -d '{
    "workOrderId":"<workOrderId>",
    "workflowVersionId":"<workflowVersionId>"
  }'
```

响应里会有 `workflowInstanceId`。

#### 2.3 执行 APS 调度（恢复 ScheduleTask 书签）

```bash
curl -s -X POST "<baseUrl>/api/WorkflowEngine/schedule"
```

#### 2.4 触发工位事件（恢复 WorkstationTask 书签，推进到 End）

先查询实例状态拿到工位节点 Id（可从 bootstrap 记录的 workflow 节点中找到 workstation 节点）：

```bash
curl -s "<baseUrl>/api/WorkflowEngine/instances/<workflowInstanceId>"
```

再调用：

```bash
curl -s -X POST "<baseUrl>/api/WorkflowEngine/workstation/complete" \
  -H "Content-Type: application/json" \
  -d '{
    "workflowInstanceId":"<workflowInstanceId>",
    "workflowNodeId":"<workstationNodeId>",
    "deviceJobId":"11111111-1111-1111-1111-111111111111",
    "eventCode":"ExperimentCompleted"
  }'
```

### 3) Fake 网关说明

- 位置：`MyToDo.Api/Services/Workflow/FakeWorkstationGateway.cs`
- 作用：提供本地可重复的假数据（`StartExperimentAsync` 会基于输入返回确定性的 GUID `DeviceJobId`），便于调试与测试。

### 4) 运行测试

```bash
dotnet test MyToDo.Api.Tests/MyToDo.Api.Tests.csproj
```

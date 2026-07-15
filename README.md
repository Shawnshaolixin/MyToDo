# MyToDo

## Workflow + APS 最小可运行验证（SQLite）

本仓库 `MyToDo.Api` 已包含一个最小可运行链路：

1. 创建工单并绑定流程版本
2. 启动工作流运行时（`StartAsync`）
3. 流程到达 `ScheduleTask` 并创建 `SchedulableTask` + Bookmark
4. 运行 APS 调度器（按优先级、最早开始时间、资源类型分配）
5. 调度完成后恢复流程（`ResumeAsync`）
6. 流程到达 `WorkstationTask`，恢复后继续推进到 `End`
7. 流程实例与工单状态标记为 `Completed`

### 本地运行

```bash
cd MyToDo.Api
dotnet run
```

默认 SQLite 连接：`MyToDo.Api/appsettings.json`

```json
"ConnectionStrings": {
  "ToDoConnection": "Data Source=mytodo.db"
}
```

应用启动时会自动执行 `EnsureCreated()` 初始化数据库表结构。

### 最小验证 API

- `POST /api/WorkflowEngine/bootstrap`：初始化一条示例 WorkflowVersion + WorkOrder + Resource
- `POST /api/WorkflowEngine/start`：启动流程运行时
- `POST /api/WorkflowEngine/schedule`：执行 APS 调度并恢复 `ScheduleTask` 书签
- `POST /api/WorkflowEngine/resume`：手动恢复书签（如 `WorkstationTaskCompleted`）
- `GET /api/WorkflowEngine/instances/{instanceId}`：查看流程实例、书签、排程任务状态

### 测试

```bash
dotnet test MyToDo.Api.Tests/MyToDo.Api.Tests.csproj
```

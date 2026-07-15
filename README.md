# MyToDo

## Workflow + APS 最小可运行验证（SQLite）

本仓库 `MyToDo.Api` 实现了一个完整的工作流引擎，包含：

- **WorkflowBookmarkService** — 管理工作流书签（暂停/恢复点）
- **WorkflowNodeExecutorRegistry** — 按节点类型路由到对应执行器
- **StartNodeExecutor / EndNodeExecutor** — 开始/结束节点执行器
- **ScheduleTaskExecutor** — 调度任务节点执行器，创建 SchedulableTask + 书签
- **WorkstationTaskExecutor** — 工作站任务节点执行器，调用 IWorkstationGateway
- **FakeWorkstationGateway** — 本地测试用伪网关，无需真实设备
- **ApsScheduler (SimpleApsScheduler)** — 贪心单趟 APS 调度器（优先级 + 最早开始时间规则）
- **WorkflowRuntime** — 核心执行引擎，驱动书签流与令牌推进

执行流程：

1. 创建工单并绑定流程版本
2. 启动工作流运行时（`StartAsync`）
3. 流程到达 `ScheduleTask` 并创建 `SchedulableTask` + Bookmark
4. 运行 APS 调度器（按优先级、最早开始时间、资源类型分配）
5. 调度完成后恢复流程（`ResumeAsync`）
6. 流程到达 `WorkstationTask`，FakeWorkstationGateway 分配 DeviceJobId，创建 Bookmark
7. 手动触发设备完成事件（`ResumeAsync`），流程推进到 `End`
8. 流程实例与工单状态标记为 `Completed`

---

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

> **注意（EnsureCreated vs Migrations）：**  
> `EnsureCreated()` 适合开发/演示，无需手动建表；但若模型变更需删除 `.db` 文件重建。  
> 生产环境请改用 `db.Database.Migrate()` 搭配 EF Core 迁移文件。

---

### FakeWorkstationGateway 说明

`FakeWorkstationGateway` 注册于 DI 容器（`Program.cs`），工作流执行 `WorkstationTask` 节点时自动调用。  
它返回一个随机 GUID 作为 `DeviceJobId`，**无需真实设备**即可完整跑通流程。

要替换为真实网关：在 `Program.cs` 中将

```csharp
builder.Services.AddScoped<IWorkstationGateway, FakeWorkstationGateway>();
```

替换为你的生产实现（如 `HttpWorkstationGateway`）。

---

### 完整 curl 演示（Start → WorkstationTask → End）

#### 1. 初始化示例数据

```bash
curl -X POST http://localhost:5000/api/WorkflowEngine/bootstrap \
  -H "Content-Type: application/json" \
  -d '{
    "workflowName": "Demo Workflow",
    "workOrderNo": "WO-001",
    "priority": 5,
    "requiredResourceType": "Workstation",
    "estimatedDurationMinutes": 60
  }'
# 响应中获取 workflowVersionId 和 workOrderId
```

#### 2. 启动工作流

```bash
curl -X POST http://localhost:5000/api/WorkflowEngine/start \
  -H "Content-Type: application/json" \
  -d '{
    "workOrderId": "<workOrderId from step 1>",
    "workflowVersionId": "<workflowVersionId from step 1>"
  }'
# 响应中获取 workflowInstanceId
```

#### 3. 查看实例状态（含活跃书签）

```bash
curl http://localhost:5000/api/WorkflowEngine/instances/<workflowInstanceId>
# 应看到 BookmarkType=ScheduleTaskScheduled 的活跃书签
```

#### 4. 运行 APS 调度器（分配资源并恢复 ScheduleTask 书签）

```bash
curl -X POST http://localhost:5000/api/WorkflowEngine/schedule
# 调度器自动分配资源并恢复 ScheduleTask 书签，流程推进到 WorkstationTask
```

#### 5. 查看 WorkstationTask 书签

```bash
curl http://localhost:5000/api/WorkflowEngine/instances/<workflowInstanceId>
# 应看到 BookmarkType=WorkstationTaskCompleted 的活跃书签，记录 bookmarkKey
```

#### 6. 模拟设备完成事件（触发 WorkstationTask → End）

```bash
curl -X POST http://localhost:5000/api/WorkflowEngine/resume \
  -H "Content-Type: application/json" \
  -d '{
    "bookmarkType": "WorkstationTaskCompleted",
    "bookmarkKey": "<bookmarkKey from step 5>"
  }'
```

#### 7. 验证流程完成

```bash
curl http://localhost:5000/api/WorkflowEngine/instances/<workflowInstanceId>
# Status 应为 Completed
```

---

### 测试

```bash
dotnet test MyToDo.Api.Tests/MyToDo.Api.Tests.csproj
```


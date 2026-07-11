# MVP 实现说明

## 核心流程

`GameRoot` 是唯一组合根：加载 ScriptableObject 配置，创建 `GameState` 和 `GameSession`，绑定 UI，并在 `Update` 调用 `Tick`。业务系统均为普通 C# 类。

```text
GameRoot -> GameSession -> GameLoop
                       -> DaySystem
                       -> EventSystem -> ResourceSystem
                       -> DispatchSystem -> WorkerSystem
                       -> ReportSystem
GameSession -> C# events -> GameUIController -> reusable UI views
```

事件以 `Pending` 生成；派工按阶段、事件、员工和预算顺序校验。成功派工后进入 `Handling`，员工进入 `Working`。倒计时结束应用成功影响并释放员工；Pending 超时则应用失败影响。日计时结束后暂停在 `DaySettlement`，生成报告；第 5 天生成 `GameResult`。

## 配置字段

- `GameBalanceConfig`：初始资源、60 秒日长、5 日上限、事件数量/间隔、每日收入和失败阈值。
- `EventConfig`：事件/设施/紧急度、等待与处理时长、成本、推荐员工、成功和失败资源变化。
- `WorkerConfig`：员工标识、显示名、类型及匹配/不匹配时长倍率。

运行时状态只保存在 `GameState`、`GameEventRuntime` 和 `WorkerRuntime`，不会写回配置资产。

## 内容扩展

新增事件：在 Unity 中创建 `Neighborhood Manager/Event` 资产，填写唯一 `EventId` 和数值，然后将资产加入场景 `GameRoot` 的事件列表。若希望默认 Setup 自动创建，还需在 `CommunityManagerMvpSetup.CreateEventConfigs` 中增加一项。

新增员工：创建 `Neighborhood Manager/Worker` 资产，填写唯一 `WorkerId`、类型和倍率，再加入 `GameRoot` 员工列表；默认内容同样可加入 Setup 工具。

调整游戏时长或事件频率：修改 `GameBalanceConfig.DayLengthSeconds`、`MaxDayCount`、`MinEventSpawnInterval` 和 `MaxEventSpawnInterval`。完整默认流程约 5 分钟。

## 平台扩展

`IPlatformService`/`DefaultPlatformService` 隔离平台判断。Android 与 Web 应在 `Scripts/Platform/` 增加实现，在 Input System 中补充触摸拖动、双指缩放，并为窄屏创建独立 Canvas 布局。核心系统不得依赖平台宏、云服务或原生插件。

## Editor 生成边界

场景、配置、材质、字体和引用由 `Tools/Community Manager/Setup MVP Project` 通过 Editor API 创建。工具按固定路径检查资产并保持幂等，不应手工编辑 `.unity` 或 ScriptableObject YAML。

# 小区经理（Neighborhood Manager）

《小区经理》是一个社区运维题材的轻量模拟经营 MVP。玩家在五个游戏日内处理电梯、停车、投诉、快递柜和摄像头事件，调度维修工、保安与客服，并管理预算、满意度、投诉量和设备健康度。

## 环境与依赖

- Unity `6000.3.19f1`
- Universal Render Pipeline `17.3.0`
- Input System `1.19.0`
- uGUI/TextMeshPro `2.0.0`
- Unity Test Framework `1.6.0`

不要升级 Unity 版本。通过 Unity Hub 添加现有项目目录并使用上述 Editor 打开。

## 初始化与运行

1. 等待 Unity 完成首次脚本编译。
2. 选择 **Tools > Community Manager > Setup MVP Project**。
3. 选择 **Tools > Community Manager > Validate MVP Setup**，确认 Console 验证通过。
4. 打开 `Assets/_Project/Scenes/Game_MVP.unity`，点击 Play。

玩家先选择一个待处理事件，再点击空闲员工的“派工”按钮。员工匹配时处理更快；事件成功或超时会改变资源。每 60 秒显示日结，第 5 天结束后显示最终结果。

## 测试与 Windows 构建

在 **Window > General > Test Runner > EditMode** 中运行 `NeighborhoodManager.EditModeTests`。命令行示例：

```powershell
& $Unity -batchmode -projectPath . -runTests -testPlatform EditMode -testResults Logs/editmode-results.xml -logFile Logs/editmode-tests.log
```

Windows 构建：打开 **File > Build Profiles**，选择 Windows，确认 `Game_MVP` 已启用，点击 **Build**。当前没有自动化 Player 构建方法。

## 目录结构

- `Assets/_Project/Scripts/`：运行时 Model、Config、系统、UI 与平台层。
- `Assets/_Project/Editor/`：幂等初始化和验证工具。
- `Assets/_Project/Configs/`、`Scenes/`、`Art/`：由 Setup 菜单生成的资产。
- `Assets/_Project/Tests/EditMode/`：不依赖场景的核心测试。
- `Docs/MVP_IMPLEMENTATION.md`：架构和扩展说明。

## 完成范围与后续

已完成本地五日闭环、事件/员工调度、资源和报告系统、基础 UI、俯视相机、Editor 初始化与核心测试。未实现存档、联网、Steam、NPC、NavMesh、正式移动 UI 或 Web 构建。

平台差异集中在 `Scripts/Platform/`；后续 Android/Web 输入适配应扩展 Input System 与 UI 布局，不要把平台宏散落到核心系统。

## 常见问题

- **空引用**：重新运行 Setup，再运行 Validate；已有配置不会被覆盖。
- **中文方框/缺字**：Setup 会尝试创建动态“Microsoft YaHei”字体。若系统没有该字体，请导入可商用 CJK 字体并替换 Canvas 和场景标签的 TMP Font Asset。
- **按钮无响应**：确认场景只有一个 EventSystem，且使用 `InputSystemUIInputModule`。

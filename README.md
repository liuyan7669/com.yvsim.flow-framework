# Flow Framework

Flow Framework 是一个基于状态模式实现的 Unity 轻量同步流程框架。它保留简单明确的状态切换语义，同时提供实验和仿真流程常用的运行历史、结果、备注、时间统计、共享上下文与快照能力。

## 功能

- 使用 `Enter`、`Update`、`LateUpdate`、`FixedUpdate` 和 `Exit` 描述状态生命周期。
- 状态切换立即同步完成，不引入队列、重试或隐式调度。
- 通过 `FlowContext` 查询当前状态、上一状态和已运行状态。
- 每次进入状态都会生成独立的 `StateRunRecord`。
- 记录状态标识、显示名称、开始时间、结束时间、结果和备注。
- 同时统计实际用时与排除业务暂停、后台暂停及特定失焦时间后的有效用时。
- 支持清除历史、完成事件和 JSON 快照导出。
- 提供只读的 Unity Editor 实时调试窗口。

## 安装

当前源码版本为 `1.0.0`，最低 Unity 2021.3。

在 **Window > Package Manager > + > Add package from disk...** 中选择本包根目录的 `package.json`。安装本包前，先用相同方式安装本地 `com.yvsim.inspector-groups` `1.0.0`，或将两者一起作为 embedded package 放入项目 `Packages/` 目录。

也可把完整包目录（含 `.meta`）放入项目 `Packages/com.yvsim.flow-framework`，由 Unity 作为 embedded package 加载。

本包提供本地源码、Git 和 OpenUPM 安装方式；固定版本使用 `1.0.0`，具体步骤见下文。

## 快速开始

通过 Hierarchy 右键菜单 `Yvsim > 框架 > 创建 Flow 系统` 创建名为 `Flow`、已挂载 `FlowManager` 的场景根物体（支持撤销，场景已有 FlowManager 时禁用）。也可以手动创建 GameObject 并挂载 `FlowManager`。框架会在 `Awake` 中初始化，并先进入不计入业务历史的 `NullState`。

```csharp
using Yvsim.FlowFramework;
using UnityEngine;

public sealed class PrepareState : StateBase
{
    public override string StateId => "prepare";
    public override string DisplayName => "准备步骤";

    public override void Enter()
    {
        Debug.Log("开始准备");
    }

    public override void Exit()
    {
        Debug.Log("准备结束");
    }
}

public sealed class ExperimentState : StateBase
{
    public override string StateId => "experiment";
    public override string DisplayName => "实验步骤";
}
```

立即切换状态：

```csharp
FlowManager.ChangeState<PrepareState>();
FlowManager.ChangeState<ExperimentState>(
    StateRunResult.Succeeded,
    "准备检查通过");
```

第二次切换传入的结果和备注描述即将退出的 `PrepareState`。方法返回时，旧状态的 `Exit` 和新状态的 `Enter` 都已经执行完成。

最后一个业务状态结束时：

```csharp
FlowManager.EndCurrentState(
    StateRunResult.Succeeded,
    "实验流程完成");
```

该方法会执行最后状态的 `Exit`、封存运行记录并回到 `NullState`。

## 初始化与场景切换

`FlowManager` 使用较早的脚本执行顺序在 `Awake` 中完成初始化。首次初始化后会通过 `DontDestroyOnLoad` 跨场景保留；后续场景中如果再次出现 `FlowManager`，重复实例会自行销毁，原实例和 `FlowContext` 会继续使用。

场景切换本身不会自动结束当前业务状态。需要把场景边界视为流程边界时，应在加载场景前显式调用 `ChangeState` 或 `EndCurrentState`，这样状态的 `Exit`、结果和结束时间才有明确含义。

## 状态参数

需要参数时直接通过构造函数传入状态实例：

```csharp
public sealed class TrialState : StateBase
{
    private readonly int trialIndex;

    public TrialState(int trialIndex)
    {
        this.trialIndex = trialIndex;
    }
}

FlowManager.ChangeState(new TrialState(3));
```

框架不缓存旧状态实例，也不限制参数类型。

## 运行上下文

```csharp
FlowContext context = FlowManager.FlowContext;

bool isExperiment = context.IsCurrent<ExperimentState>();
bool hasPrepared = context.HasRun<PrepareState>();
int prepareCount = context.GetRunCount<PrepareState>();
StateRunRecord lastPrepare = context.GetLastRecord<PrepareState>();
```

`StateHistory` 和 `StateRunHistory` 都是只读视图。`NullState` 不会进入业务历史。

状态切换和运行完成事件：

```csharp
context.StateChanged += (previous, current) =>
{
    Debug.Log($"{previous?.GetType().Name} -> {current?.GetType().Name}");
};

context.StateRunCompleted += record =>
{
    Debug.Log($"{record.DisplayName}: {record.ActiveDuration.TotalSeconds:F3} 秒");
};
```

## 计时

`Duration` 是状态开始到结束的实际经过时间。`ActiveDuration` 会排除业务暂停和应用后台暂停时间。应用失焦时是否继续累计有效用时，遵循 Player Settings 中的 `Run In Background`：勾选时继续累计，未勾选时暂停累计。

```csharp
FlowManager.PauseTiming();
FlowManager.ResumeTiming();
```

如果恢复业务计时时应用仍在后台，或应用失焦且未勾选 `Run In Background`，有效用时仍会保持暂停。

## 快照

```csharp
FlowSnapshot snapshot = FlowManager.FlowContext.CreateSnapshot();
string json = snapshot.ToJson();
```

快照是创建时刻的独立数据，可用于实验报告、问题复现、存档或上传。框架只生成 JSON 内容，不决定文件路径、数据库或网络上传方式。

## 调试窗口

在 Unity 菜单中选择：

```text
Yvsim > 调试 > Flow 系统调试
```

进入 Play Mode 后可实时查看当前状态、完整状态类名、上一状态、计时状态、实际/有效用时以及已经结束的运行记录。FlowManager 的 Inspector 也会同步显示当前步骤的实时信息。两处界面均为只读，不会触发状态切换。

## 示例

通过 Package Manager 导入 `Flow Framework Examples`，打开其中的 `Flow Framework` 场景。示例提供设备检查、受试者登记、基线校准、实验执行和数据保存五个步骤，可在 Game 视图中逐步推进，并配合 Flow Debugger 观察实时计时和已经结束的运行记录。

## 测试

在 Unity Test Runner 的 EditMode 页签中运行 `Yvsim.FlowFramework.Tests`。测试覆盖状态结果、重复进入、暂停计时、历史只读性、快照独立性和最后状态结束等关键行为。

## 环境要求

- Unity 2021.3 或更高版本。

## 许可证

本包使用 MIT License，详情参见 [LICENSE.md](LICENSE.md)。

示例自带约 16 MB 的完整 Noto Sans CJK SC 字体，不依赖 Noto TMP 包；字体遵循 SIL Open Font License 1.1，详见 `Samples~/Flow Framework/Fonts/OFL-Noto-CJK.txt`。字体不适用上述代码许可。

## Inspector 统一依赖

本包依赖 `com.yvsim.inspector-groups` `1.0.0`，只声明字段、状态与业务操作。Inspector 的布局、样式和控件绘制由 Inspector Groups 提供；包内分组使用框架样式，用户新增分组默认使用普通样式。核心包不依赖其他 Yvsim 业务包，Samples 的明确可选组合除外。

## 从 GitHub 安装

仓库：[com.yvsim.flow-framework](https://github.com/liuyan7669/com.yvsim.flow-framework)。仓库名与 UPM 包名一致。

先通过 Package Manager 的 Add package from git URL 安装以下 Yvsim 依赖，再安装本包：

- `https://github.com/liuyan7669/com.yvsim.inspector-groups.git#main`（1.0.0）

在 Unity Package Manager 选择 Add package from git URL，输入：

```text
https://github.com/liuyan7669/com.yvsim.flow-framework.git#main
```

此地址跟随 main 分支。需要固定 1.0.0 源码时，将 `#main` 改为 `#v1.0.0`；Unity 官方依赖仍按 package.json 解析。

## 从 OpenUPM 安装

在 Unity 的 Project Settings > Package Manager 中添加 Scoped Registry：

- Name：OpenUPM
- URL：`https://package.openupm.com`
- Scope：`com.yvsim`

然后在 Package Manager 中通过 Add package by name 输入 `com.yvsim.flow-framework`，版本填 `1.0.0`。也可从 My Registries 中选择本包。声明的 Yvsim 依赖由 OpenUPM 解析，Unity 官方依赖仍由 Unity 注册表提供。

如果已安装 OpenUPM CLI，可在工程根目录执行：

```sh
openupm add com.yvsim.flow-framework@1.0.0
```

[OpenUPM 包页面](https://openupm.com/packages/com.yvsim.flow-framework/)

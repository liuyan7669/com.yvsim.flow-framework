# Flow Framework Examples

打开 `Flow Framework` 场景并进入 Play Mode，示例会自动开始一个五步仿真实验流程：

1. 设备检查
2. 受试者登记
3. 基线校准
4. 实验执行
5. 数据保存

Game 视图中的操作面板可以完成当前步骤、暂停或恢复有效计时、记录失败、重新开始以及输出 JSON 快照。

同时打开 `Yvsim > 调试 > Flow 系统调试`，可以观察当前步骤的实时计时，以及每次切换后已经冻结的完成记录。暂停期间“实际用时”继续增加，“有效用时”停止增加。

`BasicFlowExample.cs` 同时演示了无参状态切换、构造函数参数、状态结果与备注、最后状态结束、历史查询和快照导出。

## 中文字体与许可

`Fonts/NotoSansCJKsc-Regular.otf` 为示例自带的完整 Noto Sans CJK SC 字体，避免 WebGL 等环境依赖操作系统字体回退而出现中文方框。面板绘制只临时切换 GUI 字体，并在绘制结束后恢复；示例组件的 Sample Font 留空时沿用当前默认字体。

每个示例独立携带约 16 MB 字体，是独立导入时保持中文覆盖的体积代价；不依赖 Noto TMP 或其他 Yvsim 业务包。字体按 SIL Open Font License 1.1 分发，许可和版权说明保留在 `Fonts/OFL-Noto-CJK.txt`，不适用示例代码的 MIT 许可。

using System;
using UnityEditor;
using UnityEngine;

namespace Yvsim.FlowFramework.Editor
{
    /// <summary>
    /// 在 Unity Editor 中只读查看 Flow Framework 当前状态和运行历史。
    /// 此窗口位于 Editor 程序集，不会被打入运行时构建。
    /// </summary>
    public class FlowDebugWindow : EditorWindow
    {
        private Vector2 scrollPosition;

        /// <summary>
        /// 打开 Flow Framework 运行调试窗口。
        /// </summary>
        [MenuItem("Yvsim/调试/Flow 系统调试")]
        public static void Open()
        {
            GetWindow<FlowDebugWindow>("Flow Debugger");
        }

        void OnEnable()
        {
            EditorApplication.update += RefreshWhilePlaying;
        }

        void OnDisable()
        {
            EditorApplication.update -= RefreshWhilePlaying;
        }

        private void RefreshWhilePlaying()
        {
            if (EditorApplication.isPlaying && !EditorApplication.isPaused)
            {
                Repaint();
            }
        }

        void OnGUI()
        {
            // 不在未初始化时访问静态 FlowContext，避免调试窗口产生无意义报错。
            if (!EditorApplication.isPlaying)
            {
                EditorGUILayout.HelpBox(
                    "进入 Play Mode 后可查看 Flow Framework 的实时运行信息。",
                    MessageType.Info);
                return;
            }

            FlowManager manager = FindObjectOfType<FlowManager>();
            if (manager == null)
            {
                EditorGUILayout.HelpBox(
                    "当前场景中没有已初始化的 FlowManager。",
                    MessageType.Warning);
                return;
            }

            FlowContext context = FlowManager.FlowContext;
            if (context == null)
            {
                return;
            }

            DrawCurrentState(context);
            EditorGUILayout.Space();
            DrawHistory(context);
        }

        /// <summary>
        /// 绘制当前步骤的实时运行信息。
        /// 此方法仅用于调试窗口；Inspector 通过 GetCurrentStateValues 复用数据。
        /// </summary>
        internal static void DrawCurrentState(FlowContext context)
        {
            EditorGUILayout.LabelField("当前步骤（实时）", EditorStyles.boldLabel);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                DrawCurrentStateValues(context);
        }

        /// <summary>为窗口与 Inspector 提供相同的状态数据。</summary>
        internal static System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<string, string>> GetCurrentStateValues(FlowContext context)
        {
            return new[]
            {
                new System.Collections.Generic.KeyValuePair<string, string>("当前状态", GetStateName(context.CurrentState)),
                new System.Collections.Generic.KeyValuePair<string, string>("状态类", GetStateTypeName(context.CurrentState)),
                new System.Collections.Generic.KeyValuePair<string, string>("上一状态", GetStateName(context.PreviousState)),
                new System.Collections.Generic.KeyValuePair<string, string>("计时状态", context.IsTimingPaused ? "已暂停" : "运行中"),
                new System.Collections.Generic.KeyValuePair<string, string>("开始时间", FormatTime(context.CurrentStateStartTime)),
                new System.Collections.Generic.KeyValuePair<string, string>("实际用时", FormatDuration(context.CurrentStateElapsedTime)),
                new System.Collections.Generic.KeyValuePair<string, string>("有效用时", FormatDuration(context.CurrentStateActiveElapsedTime))
            };
        }

        private static void DrawCurrentStateValues(FlowContext context)
        {
            foreach (var value in GetCurrentStateValues(context)) DrawValue(value.Key, value.Value);
        }

        private void DrawHistory(FlowContext context)
        {
            int completedCount = 0;
            foreach (StateRunRecord record in context.StateRunHistory)
            {
                if (record.IsCompleted)
                {
                    completedCount++;
                }
            }

            EditorGUILayout.LabelField($"已完成记录 ({completedCount})", EditorStyles.boldLabel);

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            for (int i = context.StateRunHistory.Count - 1; i >= 0; i--)
            {
                StateRunRecord record = context.StateRunHistory[i];
                if (record.IsCompleted)
                {
                    DrawRecord(i, record);
                }
            }
            EditorGUILayout.EndScrollView();
        }

        private static void DrawRecord(int index, StateRunRecord record)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                string title = string.IsNullOrEmpty(record.DisplayName)
                    ? record.StateType.Name
                    : record.DisplayName;
                EditorGUILayout.LabelField($"#{index + 1}  {title}", EditorStyles.boldLabel);

                DrawValue("状态类型", record.StateType.FullName);
                DrawValue("状态标识", record.StateId);
                DrawValue("运行结果", record.Result.ToString());
                DrawValue("开始时间", FormatTime(record.StartTime));
                DrawValue("结束时间", FormatTime(record.EndTime));
                DrawValue("实际用时", FormatDuration(record.Duration));
                DrawValue("有效用时", FormatDuration(record.ActiveDuration));

                if (!string.IsNullOrEmpty(record.Note))
                {
                    DrawValue("备注", record.Note);
                }
            }
        }

        private static string GetStateName(IState state)
        {
            if (state == null)
            {
                return "无";
            }

            IStateInfo stateInfo = state as IStateInfo;
            return stateInfo?.DisplayName ?? state.GetType().Name;
        }

        private static string GetStateTypeName(IState state)
        {
            if (state == null || state is NullState)
            {
                return "-";
            }

            return state.GetType().FullName;
        }

        private static string FormatTime(DateTime? time)
        {
            return time?.ToString("yyyy-MM-dd HH:mm:ss.fff") ?? "-";
        }

        private static string FormatDuration(TimeSpan duration)
        {
            return $"{duration.TotalSeconds:F3} 秒";
        }

        /// <summary>为 Inspector 与调试窗口中的状态字段提供相同的说明。</summary>
        internal static string GetValueTooltip(string label)
        {
            switch (label)
            {
                case "当前状态": return "当前正在运行的步骤显示名称；未提供名称时显示状态类名。";
                case "状态类":
                case "状态类型": return "实现该步骤的 C# 状态类完整名称，用于定位具体业务逻辑。";
                case "上一状态": return "切换到当前步骤之前运行的状态显示名称。";
                case "计时状态": return "有效用时是否正在累计；业务暂停或应用暂停等条件会暂停累计，实际用时仍会继续。";
                case "开始时间": return "进入该步骤时记录的本地日期和时间。";
                case "结束时间": return "结束该步骤时记录的本地日期和时间；尚未结束时显示横线。";
                case "实际用时": return "从步骤开始到结束或当前时刻的总经过时间，包含暂停期间，单位为秒。";
                case "有效用时": return "该步骤扣除计时暂停时段后的累计用时，单位为秒。";
                case "状态标识": return "状态提供的稳定标识，用于区分步骤；默认使用状态类完整名称。";
                case "运行结果": return "结束该步骤时记录的结果，例如成功、失败或取消，由业务流程指定。";
                case "备注": return "结束或切换步骤时由业务代码附加的说明。";
                default: return string.Empty;
            }
        }

        private static void DrawValue(string label, string value)
        {
            EditorGUILayout.LabelField(new GUIContent(label, GetValueTooltip(label)), new GUIContent(value ?? "-"));
        }
    }
}

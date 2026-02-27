using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace CombatEditor
{
    /// <summary>
    /// 条件系统初始化器，在编辑器启动时和运行时都会自动注册条件
    /// </summary>
    public static class ConditionSystemInitializer
    {
#if UNITY_EDITOR
        [InitializeOnLoad]
        public static class EditorInitializer
        {
            static EditorInitializer()
            {
                // 在编辑器启动时自动注册条件
                InitializeConditionSystem();
            }
        }
#endif

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void RuntimeInitialize()
        {
            // 在运行时启动时自动注册条件
            InitializeConditionSystem();
        }
        
        /// <summary>
        /// 初始化条件系统
        /// </summary>
        public static void InitializeConditionSystem()
        {
            try
            {
                // 确保条件管理器已经初始化（这会自动注册内置条件）
                var manager = EventConditionManager.Instance;
                
                // 注册示例条件
                RegisterExampleConditions();
                
                // 自动发现并注册所有条件
                manager.AutoRegisterConditions();
                
                Debug.Log("✓ 条件系统初始化完成");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"条件系统初始化失败: {e.Message}");
            }
        }
        
        /// <summary>
        /// 注册示例条件
        /// </summary>
        private static void RegisterExampleConditions()
        {
            var manager = EventConditionManager.Instance;
            
            // 注册示例条件
            manager.RegisterCondition<ExampleConditions.HealthPercentageCondition>();
            manager.RegisterCondition<ExampleConditions.InAirCondition>();
            manager.RegisterCondition<ExampleConditions.OnGroundCondition>();
            manager.RegisterCondition<ExampleConditions.HasTargetCondition>();
            manager.RegisterCondition<ExampleConditions.DistanceCondition>();
            manager.RegisterCondition<ExampleConditions.HasBuffCondition>();
            manager.RegisterCondition<ExampleConditions.StateCondition>();
            manager.RegisterCondition<ExampleConditions.AttackPhaseCondition>();
            manager.RegisterCondition<ExampleConditions.ComboCountCondition>();
            manager.RegisterCondition<ExampleConditions.TimeCondition>();
            
            Debug.Log("✓ 示例条件注册完成");
        }
        
#if UNITY_EDITOR
        /// <summary>
        /// 获取条件系统信息
        /// </summary>
        [MenuItem("CombatEditor/条件系统/显示系统信息")]
        public static void ShowSystemInfo()
        {
            var manager = EventConditionManager.Instance;
            var allConditions = manager.GetAllConditions();
            
            string info = $"条件系统信息:\n\n";
            info += $"已注册条件数量: {allConditions.Count}\n\n";
            info += "已注册的条件:\n";
            
            foreach (var condition in allConditions)
            {
                info += $"- {condition.DisplayName} (ID: {condition.ConditionId})\n";
            }
            
            EditorUtility.DisplayDialog("条件系统信息", info, "确定");
        }
        
        /// <summary>
        /// 重新初始化条件系统
        /// </summary>
        [MenuItem("CombatEditor/条件系统/重新初始化")]
        public static void ReinitializeSystem()
        {
            var manager = EventConditionManager.Instance;
            manager.ClearAllConditions();
            
            InitializeConditionSystem();
            
            EditorUtility.DisplayDialog("重新初始化", "条件系统已重新初始化完成", "确定");
        }
        
        /// <summary>
        /// 清除所有条件
        /// </summary>
        [MenuItem("CombatEditor/条件系统/清除所有条件")]
        public static void ClearAllConditions()
        {
            var manager = EventConditionManager.Instance;
            manager.ClearAllConditions();
            
            EditorUtility.DisplayDialog("清除完成", "所有条件已清除", "确定");
        }
#endif
    }
} 
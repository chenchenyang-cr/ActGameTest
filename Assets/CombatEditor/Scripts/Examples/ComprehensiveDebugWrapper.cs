using UnityEngine;
using CombatEditor;
using System.Linq;
using System.Collections.Generic;

namespace CombatEditor
{
    /// <summary>
    /// 全面的运行时参数系统调试器
    /// 深度诊断所有可能导致GetAction和GetActionByGroupLabel失败的问题
    /// </summary>
    public class ComprehensiveDebugWrapper : MonoBehaviour
    {
        [Header("目标")]
        public CombatController combatController;
        
        [Header("调试设置")]
        public bool enableDetailedLogging = true;
        public bool autoFixProblems = true;
        
        private RuntimeParameterWrapper wrapper;
        private RuntimeParameterManager manager;
        
        void Start()
        {
            if (combatController == null)
                combatController = GetComponent<CombatController>();
            
            // 延迟执行完整诊断
            Invoke("RunComprehensiveDiagnostic", 0.2f);
        }
        
        void Update()
        {
            if (Input.GetKeyDown(KeyCode.F1))
            {
                RunComprehensiveDiagnostic();
            }
            
            if (Input.GetKeyDown(KeyCode.F2))
            {
                TestAllGetActionMethods();
            }
            
            if (Input.GetKeyDown(KeyCode.F3))
            {
                TryFixProblems();
            }
        }
        
        /// <summary>
        /// 运行全面诊断
        /// </summary>
        [ContextMenu("运行全面诊断")]
        public void RunComprehensiveDiagnostic()
        {
            Debug.Log("=== 🔍 开始全面诊断 ===");
            
            // 第1步：检查CombatController基础状态
            if (!DiagnoseCombatController())
            {
                Debug.LogError("❌ CombatController基础诊断失败，停止后续检查");
                return;
            }
            
            // 第2步：检查CombatDatas结构
            if (!DiagnoseCombatDatas())
            {
                Debug.LogError("❌ CombatDatas结构诊断失败");
                if (autoFixProblems) TryFixCombatDatas();
                return;
            }
            
            // 第3步：检查RuntimeParameterManager
            if (!DiagnoseRuntimeParameterManager())
            {
                Debug.LogError("❌ RuntimeParameterManager诊断失败");
                if (autoFixProblems) TryFixRuntimeParameterManager();
                return;
            }
            
            // 第4步：检查RuntimeParameterWrapper
            if (!DiagnoseRuntimeParameterWrapper())
            {
                Debug.LogError("❌ RuntimeParameterWrapper诊断失败");
                if (autoFixProblems) TryFixRuntimeParameterWrapper();
                return;
            }
            
            // 第5步：测试所有获取方法
            TestAllGetActionMethods();
            
            Debug.Log("✅ 全面诊断完成");
        }
        
        /// <summary>
        /// 诊断CombatController基础状态
        /// </summary>
        bool DiagnoseCombatController()
        {
            Debug.Log("--- 📋 诊断CombatController基础状态 ---");
            
            if (combatController == null)
            {
                Debug.LogError("❌ CombatController为空！");
                return false;
            }
            
            Debug.Log($"✅ CombatController存在: {combatController.name}");
            
            if (combatController._animator == null)
            {
                Debug.LogWarning("⚠️ CombatController._animator为空");
            }
            else
            {
                Debug.Log($"✅ Animator存在: {combatController._animator.name}");
            }
            
            return true;
        }
        
        /// <summary>
        /// 诊断CombatDatas结构
        /// </summary>
        bool DiagnoseCombatDatas()
        {
            Debug.Log("--- 📊 诊断CombatDatas结构 ---");
            
            if (combatController.CombatDatas == null)
            {
                Debug.LogError("❌ CombatDatas为空！");
                return false;
            }
            
            Debug.Log($"📁 CombatDatas组数量: {combatController.CombatDatas.Count}");
            
            if (combatController.CombatDatas.Count == 0)
            {
                Debug.LogWarning("⚠️ CombatDatas没有任何组");
                return false;
            }
            
            bool hasValidData = false;
            int totalActions = 0;
            
            for (int groupIndex = 0; groupIndex < combatController.CombatDatas.Count; groupIndex++)
            {
                var group = combatController.CombatDatas[groupIndex];
                
                if (group == null)
                {
                    Debug.LogError($"❌ 组 {groupIndex} 为空！");
                    continue;
                }
                
                string groupLabel = group.Label ?? "null";
                int actionCount = group.CombatObjs?.Count ?? 0;
                
                Debug.Log($"📁 组 {groupIndex}: Label='{groupLabel}' Actions={actionCount}");
                
                if (group.CombatObjs == null)
                {
                    Debug.LogWarning($"⚠️ 组 {groupIndex} 的CombatObjs为空");
                    continue;
                }
                
                for (int abilityIndex = 0; abilityIndex < group.CombatObjs.Count; abilityIndex++)
                {
                    var ability = group.CombatObjs[abilityIndex];
                    if (ability != null)
                    {
                        Debug.Log($"  🎬 动作 {abilityIndex}: '{ability.name}' 事件数={ability.events?.Count ?? 0}");
                        hasValidData = true;
                        totalActions++;
                    }
                    else
                    {
                        Debug.LogWarning($"  ⚠️ 动作 {abilityIndex}: null");
                    }
                }
            }
            
            Debug.Log($"📈 总有效动作数量: {totalActions}");
            return hasValidData;
        }
        
        /// <summary>
        /// 诊断RuntimeParameterManager
        /// </summary>
        bool DiagnoseRuntimeParameterManager()
        {
            Debug.Log("--- 🔧 诊断RuntimeParameterManager ---");
            
            try
            {
                manager = combatController.GetOrAddRuntimeParameterManager();
                if (manager == null)
                {
                    Debug.LogError("❌ 无法获取RuntimeParameterManager");
                    return false;
                }
                
                Debug.Log($"✅ RuntimeParameterManager存在: {manager.GetType().Name}");
                
                // 强制初始化
                var initMethod = manager.GetType().GetMethod("Initialize");
                if (initMethod != null)
                {
                    initMethod.Invoke(manager, null);
                    Debug.Log("✅ RuntimeParameterManager初始化完成");
                }
                
                return true;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"❌ RuntimeParameterManager诊断异常: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// 诊断RuntimeParameterWrapper
        /// </summary>
        bool DiagnoseRuntimeParameterWrapper()
        {
            Debug.Log("--- 🎁 诊断RuntimeParameterWrapper ---");
            
            try
            {
                wrapper = combatController.GetRuntimeParameterWrapper();
                if (wrapper == null)
                {
                    Debug.LogError("❌ 无法创建RuntimeParameterWrapper");
                    return false;
                }
                
                Debug.Log("✅ RuntimeParameterWrapper创建成功");
                
                // 强制刷新动作
                wrapper.RefreshActions();
                
                var actions = wrapper.GetActions();
                if (actions == null)
                {
                    Debug.LogError("❌ Wrapper.GetActions()返回null");
                    return false;
                }
                
                Debug.Log($"📋 Wrapper中的动作数量: {actions.Count}");
                
                if (actions.Count == 0)
                {
                    Debug.LogWarning("⚠️ Wrapper中没有动作");
                    return false;
                }
                
                return true;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"❌ RuntimeParameterWrapper诊断异常: {ex.Message}");
                Debug.LogError($"堆栈: {ex.StackTrace}");
                return false;
            }
        }
        
        /// <summary>
        /// 测试所有GetAction方法
        /// </summary>
        [ContextMenu("测试所有GetAction方法")]
        public void TestAllGetActionMethods()
        {
            Debug.Log("--- 🧪 测试所有GetAction方法 ---");
            
            if (wrapper == null)
            {
                wrapper = combatController?.GetRuntimeParameterWrapper();
            }
            
            if (wrapper == null)
            {
                Debug.LogError("❌ Wrapper为空，无法测试");
                return;
            }
            
            // 显示所有可用的Group Labels
            var groupLabels = wrapper.GetAllGroupLabels();
            Debug.Log($"📁 所有Group Labels ({groupLabels.Count}个):");
            foreach (var label in groupLabels)
            {
                Debug.Log($"  - '{label}'");
            }
            
            // 显示所有动作的ScriptableObject名称
            var allActions = wrapper.GetActions();
            Debug.Log($"\n📝 所有动作的ScriptableObject名称 ({allActions.Count}个):");
            foreach (var action in allActions)
            {
                Debug.Log($"  - '{action.Name}' (组{action.GroupIndex}, 索引{action.AbilityIndex})");
            }
            
            // 测试常见名称
            string[] testNames = { "Denglong", "denglong", "Player", "LaiAttack", "Attack1", "Attack", "Idle", "Run" };
            
            Debug.Log("\n🔍 测试常见动作名称:");
            foreach (string testName in testNames)
            {
                Debug.Log($"\n--- 测试名称: '{testName}' ---");
                
                // 测试GetAction
                var action = wrapper.GetAction(testName);
                if (action != null)
                {
                    Debug.Log($"✅ GetAction('{testName}') 成功: '{action.Name}' (组{action.GroupIndex})");
                }
                else
                {
                    Debug.Log($"❌ GetAction('{testName}') 失败");
                }
                
                // 测试GetActionByGroupLabel
                var actionByGroup = wrapper.GetActionByGroupLabel(testName);
                if (actionByGroup != null)
                {
                    Debug.Log($"✅ GetActionByGroupLabel('{testName}') 成功: '{actionByGroup.Name}' (组{actionByGroup.GroupIndex})");
                }
                else
                {
                    Debug.Log($"❌ GetActionByGroupLabel('{testName}') 失败");
                }
                
                // 测试GetActionsByGroupLabel
                var actionsByGroup = wrapper.GetActionsByGroupLabel(testName);
                if (actionsByGroup.Count > 0)
                {
                    Debug.Log($"✅ GetActionsByGroupLabel('{testName}') 成功，找到{actionsByGroup.Count}个动作");
                }
                else
                {
                    Debug.Log($"❌ GetActionsByGroupLabel('{testName}') 失败");
                }
            }
        }
        
        /// <summary>
        /// 尝试修复问题
        /// </summary>
        [ContextMenu("尝试修复问题")]
        public void TryFixProblems()
        {
            Debug.Log("--- 🔧 尝试修复问题 ---");
            
            TryFixCombatDatas();
            TryFixRuntimeParameterManager();
            TryFixRuntimeParameterWrapper();
        }
        
        void TryFixCombatDatas()
        {
            Debug.Log("🔧 尝试修复CombatDatas...");
            
            if (combatController.CombatDatas == null)
            {
                combatController.CombatDatas = new List<CombatGroup>();
                Debug.Log("✅ 创建了新的CombatDatas列表");
            }
            
            // 检查每个组的Label
            for (int i = 0; i < combatController.CombatDatas.Count; i++)
            {
                var group = combatController.CombatDatas[i];
                if (group != null && string.IsNullOrEmpty(group.Label))
                {
                    group.Label = $"Group_{i}";
                    Debug.Log($"✅ 修复组{i}的Label为: {group.Label}");
                }
                
                if (group != null && group.CombatObjs == null)
                {
                    group.CombatObjs = new List<AbilityScriptableObject>();
                    Debug.Log($"✅ 修复组{i}的CombatObjs列表");
                }
            }
        }
        
        void TryFixRuntimeParameterManager()
        {
            Debug.Log("🔧 尝试修复RuntimeParameterManager...");
            
            // 强制重新获取和初始化
            var existingManager = combatController.GetComponent<RuntimeParameterManager>();
            if (existingManager != null)
            {
                DestroyImmediate(existingManager);
                Debug.Log("✅ 删除了旧的RuntimeParameterManager");
            }
            
            manager = combatController.GetOrAddRuntimeParameterManager();
            Debug.Log("✅ 创建了新的RuntimeParameterManager");
        }
        
        void TryFixRuntimeParameterWrapper()
        {
            Debug.Log("🔧 尝试修复RuntimeParameterWrapper...");
            
            // 重新创建wrapper
            wrapper = new RuntimeParameterWrapper(combatController);
            wrapper.RefreshActions();
            
            Debug.Log("✅ 重新创建了RuntimeParameterWrapper");
        }
        
        /// <summary>
        /// 创建测试用的CombatData
        /// </summary>
        [ContextMenu("创建测试数据")]
        public void CreateTestData()
        {
            Debug.Log("--- 🧪 创建测试数据 ---");
            
            if (combatController.CombatDatas == null)
            {
                combatController.CombatDatas = new List<CombatGroup>();
            }
            
            // 创建测试组
            var testGroup = new CombatGroup();
            testGroup.Label = "Denglong";
            testGroup.CombatObjs = new List<AbilityScriptableObject>();
            testGroup.IsFolded = true;
            
            // 创建测试ScriptableObject
            var testAbility = ScriptableObject.CreateInstance<AbilityScriptableObject>();
            testAbility.name = "TestDenglongAbility";
            testAbility.events = new List<AbilityEvent>();
            
            testGroup.CombatObjs.Add(testAbility);
            combatController.CombatDatas.Add(testGroup);
            
            Debug.Log("✅ 创建了测试数据: Denglong组包含1个测试动作");
            
            // 重新初始化
            TryFixRuntimeParameterWrapper();
        }
        
        /// <summary>
        /// 详细检查特定名称
        /// </summary>
        public void DetailedCheckForName(string targetName)
        {
            Debug.Log($"--- 🔍 详细检查名称: '{targetName}' ---");
            
            if (wrapper == null)
            {
                wrapper = combatController?.GetRuntimeParameterWrapper();
            }
            
            if (wrapper == null)
            {
                Debug.LogError("❌ Wrapper为空");
                return;
            }
            
            // 检查Group Labels
            var groupLabels = wrapper.GetAllGroupLabels();
            bool foundInGroupLabels = false;
            for (int i = 0; i < groupLabels.Count; i++)
            {
                string label = groupLabels[i];
                Debug.Log($"检查Group Label {i}: '{label}' vs '{targetName}'");
                if (string.Equals(label, targetName, System.StringComparison.OrdinalIgnoreCase))
                {
                    foundInGroupLabels = true;
                    Debug.Log($"✅ 在Group Labels中找到匹配: '{label}'");
                    break;
                }
            }
            
            if (!foundInGroupLabels)
            {
                Debug.Log($"❌ 在Group Labels中没有找到'{targetName}'");
            }
            
            // 检查ScriptableObject名称
            var actions = wrapper.GetActions();
            bool foundInActions = false;
            foreach (var action in actions)
            {
                Debug.Log($"检查Action名称: '{action.Name}' vs '{targetName}'");
                if (string.Equals(action.Name, targetName, System.StringComparison.OrdinalIgnoreCase))
                {
                    foundInActions = true;
                    Debug.Log($"✅ 在Action名称中找到匹配: '{action.Name}'");
                    break;
                }
            }
            
            if (!foundInActions)
            {
                Debug.Log($"❌ 在Action名称中没有找到'{targetName}'");
            }
        }
        
        /// <summary>
        /// 提供快速测试按钮
        /// </summary>
        void OnGUI()
        {
            if (!Application.isPlaying) return;
            
            GUILayout.BeginArea(new Rect(10, 10, 300, 200));
            GUILayout.Label("CombatController 调试器", GUI.skin.box);
            
            if (GUILayout.Button("F1: 全面诊断"))
            {
                RunComprehensiveDiagnostic();
            }
            
            if (GUILayout.Button("F2: 测试GetAction方法"))
            {
                TestAllGetActionMethods();
            }
            
            if (GUILayout.Button("F3: 尝试修复问题"))
            {
                TryFixProblems();
            }
            
            if (GUILayout.Button("创建测试数据"))
            {
                CreateTestData();
            }
            
            if (GUILayout.Button("检查'Denglong'"))
            {
                DetailedCheckForName("Denglong");
            }
            
            GUILayout.EndArea();
        }
    }
} 
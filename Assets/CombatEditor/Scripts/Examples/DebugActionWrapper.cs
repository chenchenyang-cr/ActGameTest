using UnityEngine;
using CombatEditor;
using System.Linq;

namespace CombatEditor
{
    /// <summary>
    /// 调试ActionWrapper的脚本，帮助找出动作名称不匹配的问题
    /// </summary>
    public class DebugActionWrapper : MonoBehaviour
    {
        [Header("目标")]
        public CombatController combatController;
        
        private RuntimeParameterWrapper wrapper;
        
        void Start()
        {
            // 延迟执行，确保CombatController完全初始化
            Invoke("DelayedDebug", 0.1f);
        }
        
        void DelayedDebug()
        {
            if (combatController == null)
                combatController = GetComponent<CombatController>();
            
            if (combatController != null)
            {
                DebugCombatController();
            }
        }
        
        void Update()
        {
            if (Input.GetKeyDown(KeyCode.D))
            {
                DebugCombatController();
            }
            
            if (Input.GetKeyDown(KeyCode.F))
            {
                FindDenglongAction();
            }
        }
        
        /// <summary>
        /// 调试CombatController和所有动作
        /// </summary>
        [ContextMenu("调试CombatController")]
        public void DebugCombatController()
        {
            if (combatController == null)
            {
                Debug.LogError("❌ CombatController为空！");
                return;
            }
            
            Debug.Log("=== CombatController 调试信息 ===");
            
            // 检查CombatDatas
            if (combatController.CombatDatas == null)
            {
                Debug.LogError("❌ CombatDatas为空！");
                return;
            }
            
            Debug.Log($"📊 CombatDatas组数量: {combatController.CombatDatas.Count}");
            
            // 遍历所有组和动作
            int totalActions = 0;
            for (int groupIndex = 0; groupIndex < combatController.CombatDatas.Count; groupIndex++)
            {
                var group = combatController.CombatDatas[groupIndex];
                Debug.Log($"📁 组 {groupIndex}: {group.Label} (动作数量: {group.CombatObjs?.Count ?? 0})");
                
                if (group.CombatObjs != null)
                {
                    for (int abilityIndex = 0; abilityIndex < group.CombatObjs.Count; abilityIndex++)
                    {
                        var ability = group.CombatObjs[abilityIndex];
                        if (ability != null)
                        {
                            Debug.Log($"  🎬 动作 {abilityIndex}: '{ability.name}' (事件数量: {ability.events?.Count ?? 0})");
                            totalActions++;
                        }
                        else
                        {
                            Debug.LogWarning($"  ⚠️ 动作 {abilityIndex}: null");
                        }
                    }
                }
            }
            
            Debug.Log($"📈 总动作数量: {totalActions}");
            
            // 测试RuntimeParameterWrapper
            DebugRuntimeParameterWrapper();
        }
        
        /// <summary>
        /// 调试RuntimeParameterWrapper
        /// </summary>
        public void DebugRuntimeParameterWrapper()
        {
            Debug.Log("\n=== RuntimeParameterWrapper 调试信息 ===");
            
            try
            {
                wrapper = combatController.GetRuntimeParameterWrapper();
                if (wrapper == null)
                {
                    Debug.LogError("❌ RuntimeParameterWrapper为空！");
                    return;
                }
                
                var actions = wrapper.GetActions();
                Debug.Log($"📋 Wrapper中的动作数量: {actions?.Count ?? 0}");
                
                if (actions == null || actions.Count == 0)
                {
                    Debug.LogError("❌ Wrapper中没有动作！");
                    return;
                }
                
                // 列出所有动作名称
                Debug.Log("\n📝 所有动作名称:");
                for (int i = 0; i < actions.Count; i++)
                {
                    var action = actions[i];
                    if (action != null)
                    {
                        Debug.Log($"  {i}: '{action.Name}' (组{action.GroupIndex}, 索引{action.AbilityIndex})");
                        
                        // 检查是否有Motion轨道
                        var motionTracks = action.GetTracks<AbilityEventObj_Motion>();
                        if (motionTracks.Count > 0)
                        {
                            Debug.Log($"    🏃 包含 {motionTracks.Count} 个Motion轨道");
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"  {i}: null动作");
                    }
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"❌ RuntimeParameterWrapper调试失败: {ex.Message}");
                Debug.LogError($"堆栈跟踪: {ex.StackTrace}");
            }
        }
        
        /// <summary>
        /// 专门查找Denglong动作
        /// </summary>
        [ContextMenu("查找Denglong动作")]
        public void FindDenglongAction()
        {
            if (wrapper == null)
            {
                wrapper = combatController?.GetRuntimeParameterWrapper();
            }
            
            if (wrapper == null)
            {
                Debug.LogError("❌ Wrapper为空，无法查找动作");
                return;
            }
            
            Debug.Log("=== 查找Denglong动作 ===");
            
            // 现在GetAction会自动优先使用Group Label！
            var action = wrapper.GetAction("Denglong");
            if (action != null)
            {
                Debug.Log($"✅ 找到动作: '{action.Name}' (组: {action.GroupIndex})");
                Debug.Log($"📍 这个动作是通过界面显示的Group Label找到的！");
                TestMotionAccess(action);
                return;
            }
            
            // 如果没找到，显示调试信息
            Debug.LogWarning("⚠️ 没有找到名为'Denglong'的Group Label或ScriptableObject");
            Debug.Log("🔍 让我们看看都有哪些可用的选项...");
            ShowDebugInfo();
        }
        
        /// <summary>
        /// 显示所有Group Labels和动作信息
        /// </summary>
        [ContextMenu("显示所有Group Labels")]
        public void ShowDebugInfo()
        {
            if (wrapper == null)
            {
                wrapper = combatController?.GetRuntimeParameterWrapper();
            }
            
            if (wrapper == null)
            {
                Debug.LogError("❌ Wrapper为空");
                return;
            }
            
            Debug.Log("=== 所有Group Labels和动作信息 ===");
            
            // 显示所有Group Labels
            var groupLabels = wrapper.GetAllGroupLabels();
            Debug.Log($"📁 所有Group Labels ({groupLabels.Count}个):");
            foreach (var label in groupLabels)
            {
                Debug.Log($"  - '{label}'");
                
                // 显示该组的所有动作
                var actionsInGroup = wrapper.GetActionsByGroupLabel(label);
                foreach (var action in actionsInGroup)
                {
                    Debug.Log($"    └─ 动作: '{action.Name}' (ScriptableObject名称)");
                }
            }
            
            Debug.Log("\n📋 所有动作的ScriptableObject名称:");
            var allActions = wrapper.GetActions();
            foreach (var action in allActions)
            {
                if (action != null)
                {
                    Debug.Log($"  - '{action.Name}' (组{action.GroupIndex}, 索引{action.AbilityIndex})");
                }
            }
        }
        
        /// <summary>
        /// 测试Motion访问
        /// </summary>
        private void TestMotionAccess(RuntimeParameterWrapper.RuntimeAction action)
        {
            Debug.Log($"\n=== 测试动作 '{action.Name}' 的Motion访问 ===");
            
            var motionTracks = action.GetTracks<AbilityEventObj_Motion>();
            Debug.Log($"🏃 Motion轨道数量: {motionTracks.Count}");
            
            if (motionTracks.Count == 0)
            {
                Debug.LogWarning("⚠️ 该动作没有Motion轨道");
                
                // 显示所有轨道类型
                var allTracks = action.GetTracks();
                Debug.Log($"📋 该动作包含的所有轨道类型:");
                foreach (var track in allTracks)
                {
                    Debug.Log($"  - {track.TypeName}: {track.Name}");
                }
                return;
            }
            
            // 测试第一个Motion轨道
            var motionTrack = motionTracks[0];
            var motion = motionTrack.EventObj as AbilityEventObj_Motion;
            
            if (motion?.target != null)
            {
                Vector3 originalOffset = motion.target.Offset;
                Debug.Log($"✅ 成功访问Motion轨道");
                Debug.Log($"📍 当前Offset: {originalOffset}");
                
                // 测试修改
                motion.target.Offset += new Vector3(0, 0, 100);
                Debug.Log($"🔄 修改后Offset: {motion.target.Offset}");
                
                // 恢复原值
                motion.target.Offset = originalOffset;
                Debug.Log($"🔙 恢复原值: {motion.target.Offset}");
            }
            else
            {
                Debug.LogError("❌ Motion轨道的target为空");
            }
        }
        
        /// <summary>
        /// 安全的动作获取方法
        /// </summary>
        [ContextMenu("安全获取Denglong动作")]
        public void SafeGetDenglongAction()
        {
            Debug.Log("=== 安全获取Denglong动作 ===");
            
            // 确保初始化
            if (combatController == null)
                combatController = GetComponent<CombatController>();
            
            if (combatController == null)
            {
                Debug.LogError("❌ 找不到CombatController组件");
                return;
            }
            
            // 等待一帧确保完全初始化
            StartCoroutine(SafeGetActionCoroutine());
        }
        
        private System.Collections.IEnumerator SafeGetActionCoroutine()
        {
            yield return null; // 等待一帧
            
            try
            {
                wrapper = combatController.GetRuntimeParameterWrapper();
                
                if (wrapper == null)
                {
                    Debug.LogError("❌ 无法创建RuntimeParameterWrapper");
                    yield break;
                }
                
                // 刷新actions
                wrapper.RefreshActions();
                
                var action = wrapper.GetAction("Denglong");
                if (action == null)
                {
                    Debug.LogWarning("⚠️ 找不到'Denglong'动作，尝试其他可能的名称...");
                    
                    // 尝试不同的大小写组合
                    string[] possibleNames = { "denglong", "DengLong", "DENGLONG", "dengLong" };
                    
                    foreach (string name in possibleNames)
                    {
                        action = wrapper.GetAction(name);
                        if (action != null)
                        {
                            Debug.Log($"✅ 找到动作: '{name}' -> '{action.Name}'");
                            break;
                        }
                    }
                }
                
                if (action != null)
                {
                    TestMotionAccess(action);
                }
                else
                {
                    Debug.LogError("❌ 仍然找不到Denglong动作");
                    DebugRuntimeParameterWrapper(); // 显示所有可用动作
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"❌ 安全获取动作失败: {ex.Message}");
            }
        }
    }
} 
using UnityEngine;
using CombatEditor;

namespace CombatEditor
{
    /// <summary>
    /// 简化的动作获取示例
    /// 展示GetAction现在直接支持CombatEditor界面显示的Group Label
    /// </summary>
    public class SimpleActionExample : MonoBehaviour
    {
        [Header("目标")]
        public CombatController combatController;
        
        void Start()
        {
            if (combatController == null)
                combatController = GetComponent<CombatController>();
            
            // 延迟初始化
            Invoke("TestSimpleUsage", 0.2f);
        }
        
        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                TestSimpleUsage();
            }
        }
        
        /// <summary>
        /// 测试简化的使用方式
        /// </summary>
        [ContextMenu("测试简化用法")]
        public void TestSimpleUsage()
        {
            if (combatController == null)
            {
                Debug.LogError("❌ CombatController为空！");
                return;
            }
            
            Debug.Log("=== 🎉 新的简化使用方式测试 ===");
            
            // ✨ 现在这么简单就可以了！
            var wrapper = combatController.GetRuntimeParameterWrapper();
            
            // 🎯 直接使用CombatEditor界面上看到的名称
            var action = wrapper.GetAction("Denglong");  // 就是界面上显示的名称！
            
            if (action != null)
            {
                Debug.Log($"✅ 成功获取动作: '{action.Name}'");
                Debug.Log($"📍 这是通过界面显示的Group Label找到的！");
                
                // 获取Motion轨道并修改offset
                var motionTrack = action.GetTrack<AbilityEventObj_Motion>();
                if (motionTrack != null)
                {
                    var motion = motionTrack.EventObj as AbilityEventObj_Motion;
                    if (motion?.target != null)
                    {
                        Vector3 originalOffset = motion.target.Offset;
                        Debug.Log($"📍 原始Offset: {originalOffset}");
                        
                        // 修改offset
                        motion.target.Offset += new Vector3(0, 0, 100);
                        Debug.Log($"🔄 修改后Offset: {motion.target.Offset}");
                        Debug.Log("🎉 修改成功！现在执行该技能会看到新的移动效果！");
                    }
                    else
                    {
                        Debug.LogWarning("⚠️ Motion轨道没有target");
                    }
                }
                else
                {
                    Debug.LogWarning("⚠️ 该动作没有Motion轨道");
                    
                    // 显示该动作包含的轨道类型
                    var tracks = action.GetTracks();
                    Debug.Log($"📋 该动作包含以下轨道:");
                    foreach (var track in tracks)
                    {
                        Debug.Log($"  - {track.TypeName}");
                    }
                }
            }
            else
            {
                Debug.LogError("❌ 找不到名为'Denglong'的动作");
                Debug.Log("🔍 让我查看所有可用的Group Labels...");
                
                // 显示所有可用的Group Labels
                var groupLabels = wrapper.GetAllGroupLabels();
                Debug.Log($"📁 可用的Group Labels ({groupLabels.Count}个):");
                foreach (var label in groupLabels)
                {
                    Debug.Log($"  - '{label}'");
                }
            }
        }
        
        /// <summary>
        /// 展示完整的工作流程
        /// </summary>
        [ContextMenu("完整工作流程示例")]
        public void CompleteWorkflowExample()
        {
            Debug.Log("=== 🚀 完整的动作修改工作流程 ===");
            
            // 第1步：获取wrapper
            var wrapper = combatController.GetRuntimeParameterWrapper();
            
            // 第2步：直接使用界面名称获取动作
            var action = wrapper.GetAction("Denglong");  // 界面上的名称
            
            if (action == null)
            {
                Debug.LogError("❌ 找不到动作，请检查名称是否正确");
                return;
            }
            
            // 第3步：获取Motion轨道
            var motionTrack = action.GetTrack<AbilityEventObj_Motion>();
            
            if (motionTrack == null)
            {
                Debug.LogError("❌ 该动作没有Motion轨道");
                return;
            }
            
            // 第4步：获取Motion对象
            var motion = motionTrack.EventObj as AbilityEventObj_Motion;
            
            if (motion?.target == null)
            {
                Debug.LogError("❌ Motion轨道的target为空");
                return;
            }
            
            // 第5步：修改参数
            Vector3 originalOffset = motion.target.Offset;
            motion.target.Offset += new Vector3(0, 0, 100);
            
            Debug.Log($"✅ 成功修改Motion参数！");
            Debug.Log($"📍 原始值: {originalOffset}");
            Debug.Log($"🔄 新值: {motion.target.Offset}");
            Debug.Log($"🎮 现在执行该技能会看到修改后的效果！");
        }
    }
} 
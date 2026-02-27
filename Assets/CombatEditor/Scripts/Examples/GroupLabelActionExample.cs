using UnityEngine;
using CombatEditor;

namespace CombatEditor
{
    /// <summary>
    /// 通过Group Label获取动作的示例
    /// 演示如何使用CombatEditor左侧显示的组标签来获取动作
    /// </summary>
    public class GroupLabelActionExample : MonoBehaviour
    {
        [Header("目标")]
        public CombatController combatController;
        
        [Header("要获取的Group Label")]
        public string targetGroupLabel = "Denglong";
        
        private RuntimeParameterWrapper wrapper;
        
        void Start()
        {
            if (combatController == null)
                combatController = GetComponent<CombatController>();
            
            if (combatController != null)
            {
                // 延迟初始化确保CombatController完全加载
                Invoke("InitializeWrapper", 0.1f);
            }
        }
        
        void InitializeWrapper()
        {
            wrapper = combatController.GetRuntimeParameterWrapper();
            
            if (wrapper != null)
            {
                Debug.Log("✅ RuntimeParameterWrapper初始化成功");
                ShowAllGroupLabels();
            }
        }
        
        void Update()
        {
            if (Input.GetKeyDown(KeyCode.G))
            {
                GetActionByGroupLabel();
            }
            
            if (Input.GetKeyDown(KeyCode.H))
            {
                ShowAllGroupLabels();
            }
            
            if (Input.GetKeyDown(KeyCode.J))
            {
                ModifyMotionByGroupLabel();
            }
        }
        
        /// <summary>
        /// 通过Group Label获取动作
        /// </summary>
        [ContextMenu("通过Group Label获取动作")]
        public void GetActionByGroupLabel()
        {
            if (wrapper == null)
            {
                Debug.LogError("❌ Wrapper未初始化");
                return;
            }
            
            Debug.Log($"=== 通过Group Label '{targetGroupLabel}' 获取动作 ===");
            
            // 方法1：获取该组的第一个动作
            var firstAction = wrapper.GetActionByGroupLabel(targetGroupLabel);
            if (firstAction != null)
            {
                Debug.Log($"✅ 找到第一个动作: '{firstAction.Name}' (组{firstAction.GroupIndex}, 索引{firstAction.AbilityIndex})");
                
                // 显示该动作的所有轨道
                ShowActionDetails(firstAction);
            }
            else
            {
                Debug.LogWarning($"⚠️ 找不到Group Label为 '{targetGroupLabel}' 的动作");
            }
            
            // 方法2：获取该组的所有动作
            var allActionsInGroup = wrapper.GetActionsByGroupLabel(targetGroupLabel);
            Debug.Log($"📋 Group '{targetGroupLabel}' 包含 {allActionsInGroup.Count} 个动作:");
            
            for (int i = 0; i < allActionsInGroup.Count; i++)
            {
                var action = allActionsInGroup[i];
                Debug.Log($"  {i}: '{action.Name}' (组{action.GroupIndex}, 索引{action.AbilityIndex})");
            }
        }
        
        /// <summary>
        /// 显示所有Group Labels
        /// </summary>
        [ContextMenu("显示所有Group Labels")]
        public void ShowAllGroupLabels()
        {
            if (wrapper == null)
            {
                Debug.LogError("❌ Wrapper未初始化");
                return;
            }
            
            Debug.Log("=== 所有可用的Group Labels ===");
            
            var groupLabels = wrapper.GetAllGroupLabels();
            Debug.Log($"📁 总共 {groupLabels.Count} 个Group:");
            
            for (int i = 0; i < groupLabels.Count; i++)
            {
                var label = groupLabels[i];
                var actionsInGroup = wrapper.GetActionsByGroupLabel(label);
                
                Debug.Log($"  {i}: '{label}' ({actionsInGroup.Count} 个动作)");
                
                foreach (var action in actionsInGroup)
                {
                    Debug.Log($"    └─ '{action.Name}'");
                }
            }
        }
        
        /// <summary>
        /// 通过Group Label修改Motion参数
        /// </summary>
        [ContextMenu("通过Group Label修改Motion")]
        public void ModifyMotionByGroupLabel()
        {
            if (wrapper == null)
            {
                Debug.LogError("❌ Wrapper未初始化");
                return;
            }
            
            Debug.Log($"=== 通过Group Label '{targetGroupLabel}' 修改Motion ===");
            
            // 获取指定组的所有动作
            var actionsInGroup = wrapper.GetActionsByGroupLabel(targetGroupLabel);
            
            if (actionsInGroup.Count == 0)
            {
                Debug.LogWarning($"⚠️ Group '{targetGroupLabel}' 中没有动作");
                return;
            }
            
            foreach (var action in actionsInGroup)
            {
                Debug.Log($"🔍 检查动作: '{action.Name}'");
                
                // 获取Motion轨道
                var motionTracks = action.GetTracks<AbilityEventObj_Motion>();
                
                if (motionTracks.Count > 0)
                {
                    Debug.Log($"  🏃 找到 {motionTracks.Count} 个Motion轨道");
                    
                    foreach (var motionTrack in motionTracks)
                    {
                        var motion = motionTrack.EventObj as AbilityEventObj_Motion;
                        if (motion?.target != null)
                        {
                            Vector3 originalOffset = motion.target.Offset;
                            Debug.Log($"  📍 原始Offset: {originalOffset}");
                            
                            // 修改offset
                            motion.target.Offset += new Vector3(0, 0, 100);
                            Debug.Log($"  🔄 修改后Offset: {motion.target.Offset}");
                            
                            // 显示其他相关信息
                            Debug.Log($"  📊 坐标类型: {(motion.target.UseAbsoluteCoordinates ? "绝对坐标" : "本地坐标")}");
                            Debug.Log($"  ⏰ 时间控制: {motion.target.timeControlMode}");
                        }
                    }
                }
                else
                {
                    Debug.Log($"  ⚠️ 动作 '{action.Name}' 没有Motion轨道");
                }
            }
        }
        
        /// <summary>
        /// 显示动作的详细信息
        /// </summary>
        private void ShowActionDetails(RuntimeParameterWrapper.RuntimeAction action)
        {
            Debug.Log($"\n=== 动作 '{action.Name}' 详细信息 ===");
            Debug.Log($"📍 位置: 组{action.GroupIndex}, 索引{action.AbilityIndex}");
            
            var tracks = action.GetTracks();
            Debug.Log($"🎬 轨道数量: {tracks.Count}");
            
            foreach (var track in tracks)
            {
                Debug.Log($"  - {track.TypeName}: {track.Name}");
            }
        }
        
        /// <summary>
        /// 按索引获取特定组中的动作
        /// </summary>
        [ContextMenu("按索引获取组中的动作")]
        public void GetActionByGroupLabelAndIndex()
        {
            if (wrapper == null)
            {
                Debug.LogError("❌ Wrapper未初始化");
                return;
            }
            
            Debug.Log($"=== 获取Group '{targetGroupLabel}' 中的第0个动作 ===");
            
            // 通过Group Label和索引获取特定动作
            var action = wrapper.GetActionByGroupLabel(targetGroupLabel, 0);
            
            if (action != null)
            {
                Debug.Log($"✅ 找到动作: '{action.Name}'");
                ShowActionDetails(action);
            }
            else
            {
                Debug.LogWarning($"⚠️ Group '{targetGroupLabel}' 中没有索引为0的动作");
            }
        }
        
        /// <summary>
        /// 完整的使用示例（现在GetAction直接支持Group Label了！）
        /// </summary>
        [ContextMenu("完整示例")]
        public void CompleteExample()
        {
            if (wrapper == null)
            {
                Debug.LogError("❌ Wrapper未初始化");
                return;
            }
            
            Debug.Log("=== 🎉 新的简化使用方式 ===");
            
            // 🎯 现在GetAction直接支持CombatEditor界面显示的名称！
            combatController = GetComponent<CombatController>();
            wrapper = combatController.GetRuntimeParameterWrapper();
            
            // ✨ 直接使用GetAction，它会自动优先查找Group Label！
            var action = wrapper.GetAction("Denglong");  // 就这么简单！
            
            if (action != null)
            {
                var motionTrack = action.GetTrack<AbilityEventObj_Motion>();
                if (motionTrack != null)
                {
                    var motion = motionTrack.EventObj as AbilityEventObj_Motion;
                    if (motion?.target != null)
                    {
                        Debug.Log($"✅ 成功！直接通过GetAction获取到Motion");
                        Debug.Log($"📍 当前Offset: {motion.target.Offset}");
                        
                        // 修改offset
                        motion.target.Offset += new Vector3(0, 0, 100);
                        Debug.Log($"🔄 修改后Offset: {motion.target.Offset}");
                        
                        Debug.Log("🎉 现在你可以直接使用界面上看到的名称了！");
                    }
                }
            }
            else
            {
                Debug.LogError("❌ 找不到指定的Group");
            }
        }
    }
} 
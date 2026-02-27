using UnityEngine;
using CombatEditor;
using System.Reflection;

namespace CombatEditor
{
    /// <summary>
    /// Motion轨道Offset访问示例
    /// 演示如何获取和修改Motion轨道的offset值
    /// </summary>
    public class MotionOffsetAccessExample : MonoBehaviour
    {
        [Header("目标")]
        public CombatController combatController;
        
        [Header("测试设置")]
        public Vector3 newOffset = new Vector3(0, 0, 20);
        
        private RuntimeParameterWrapper _wrapper;
        
        void Start()
        {
            if (combatController == null)
                combatController = GetComponent<CombatController>();
            
            if (combatController != null)
            {
                _wrapper = combatController.GetRuntimeParameterWrapper();
            }
        }
        
        void Update()
        {
            if (Input.GetKeyDown(KeyCode.M))
            {
                TestMotionOffsetAccess();
            }
            
            if (Input.GetKeyDown(KeyCode.N))
            {
                ModifyMotionOffset();
            }
            
            if (Input.GetKeyDown(KeyCode.B))
            {
                RestoreMotionOffset();
            }
        }
        
        /// <summary>
        /// 测试Motion轨道offset的访问
        /// </summary>
        [ContextMenu("测试Motion Offset访问")]
        public void TestMotionOffsetAccess()
        {
            if (_wrapper == null) return;
            
            Debug.Log("=== Motion Offset 访问测试 ===");
            
            var actions = _wrapper.GetActions();
            foreach (var action in actions)
            {
                var motionTracks = action.GetTracks<AbilityEventObj_Motion>();
                
                foreach (var track in motionTracks)
                {
                    Debug.Log($"🔍 检查动作: {action.Name}, 轨道: {track.Name}");
                    
                    // 方法1：直接访问Motion对象的target.Offset
                    var motionObj = track.EventObj as AbilityEventObj_Motion;
                    if (motionObj != null && motionObj.target != null)
                    {
                        Vector3 currentOffset = motionObj.target.Offset;
                        bool useAbsolute = motionObj.target.UseAbsoluteCoordinates;
                        var timeMode = motionObj.target.timeControlMode;
                        
                        Debug.Log($"📍 当前Offset: {currentOffset}");
                        Debug.Log($"📍 坐标类型: {(useAbsolute ? "绝对坐标" : "本地坐标")}");
                        Debug.Log($"📍 时间控制: {timeMode}");
                    }
                    
                    // 方法2：通过反射访问嵌套字段
                    Vector3? reflectionOffset = GetMotionOffsetByReflection(track);
                    if (reflectionOffset.HasValue)
                    {
                        Debug.Log($"🔧 反射获取的Offset: {reflectionOffset.Value}");
                    }
                }
            }
        }
        
        /// <summary>
        /// 修改Motion轨道的offset值
        /// </summary>
        [ContextMenu("修改Motion Offset")]
        public void ModifyMotionOffset()
        {
            if (_wrapper == null) return;
            
            Debug.Log("=== 修改Motion Offset ===");
            
            var actions = _wrapper.GetActions();
            foreach (var action in actions)
            {
                var motionTracks = action.GetTracks<AbilityEventObj_Motion>();
                
                foreach (var track in motionTracks)
                {
                    var motionObj = track.EventObj as AbilityEventObj_Motion;
                    if (motionObj != null && motionObj.target != null)
                    {
                        Vector3 originalOffset = motionObj.target.Offset;
                        Debug.Log($"🔄 动作: {action.Name}, 原始Offset: {originalOffset} → 新Offset: {newOffset}");
                        
                        // 直接修改Motion对象的target.Offset
                        motionObj.target.Offset = newOffset;
                        
                        // 验证修改是否生效
                        Vector3 currentOffset = motionObj.target.Offset;
                        Debug.Log($"✅ 修改后的Offset: {currentOffset}");
                    }
                }
            }
            
            Debug.Log("Motion Offset修改完成！现在执行相关技能会看到新的移动效果。");
        }
        
        /// <summary>
        /// 恢复Motion轨道的offset值（这里只是演示，实际需要事先保存原始值）
        /// </summary>
        [ContextMenu("恢复Motion Offset")]
        public void RestoreMotionOffset()
        {
            if (_wrapper == null) return;
            
            Debug.Log("=== 恢复Motion Offset ===");
            
            var actions = _wrapper.GetActions();
            foreach (var action in actions)
            {
                var motionTracks = action.GetTracks<AbilityEventObj_Motion>();
                
                foreach (var track in motionTracks)
                {
                    var motionObj = track.EventObj as AbilityEventObj_Motion;
                    if (motionObj != null && motionObj.target != null)
                    {
                        // 这里演示恢复到原始值（实际应用中需要事先保存）
                        Vector3 restoredOffset = new Vector3(0, 0, 17); // 示例原始值
                        Debug.Log($"🔄 动作: {action.Name}, 恢复Offset到: {restoredOffset}");
                        
                        motionObj.target.Offset = restoredOffset;
                    }
                }
            }
            
            Debug.Log("Motion Offset已恢复到原始值！");
        }
        
        /// <summary>
        /// 通过反射获取Motion轨道的Offset值
        /// </summary>
        private Vector3? GetMotionOffsetByReflection(RuntimeParameterWrapper.RuntimeTrack track)
        {
            try
            {
                var motionObj = track.EventObj as AbilityEventObj_Motion;
                if (motionObj == null) return null;
                
                // 获取target字段
                var targetField = typeof(AbilityEventObj_Motion).GetField("target", BindingFlags.Public | BindingFlags.Instance);
                if (targetField == null) return null;
                
                var targetObj = targetField.GetValue(motionObj);
                if (targetObj == null) return null;
                
                // 获取Offset字段
                var offsetField = targetObj.GetType().GetField("Offset", BindingFlags.Public | BindingFlags.Instance);
                if (offsetField == null) return null;
                
                return (Vector3)offsetField.GetValue(targetObj);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"反射获取Offset失败: {ex.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// 通过反射设置Motion轨道的Offset值
        /// </summary>
        private bool SetMotionOffsetByReflection(RuntimeParameterWrapper.RuntimeTrack track, Vector3 newOffset)
        {
            try
            {
                var motionObj = track.EventObj as AbilityEventObj_Motion;
                if (motionObj == null) return false;
                
                // 获取target字段
                var targetField = typeof(AbilityEventObj_Motion).GetField("target", BindingFlags.Public | BindingFlags.Instance);
                if (targetField == null) return false;
                
                var targetObj = targetField.GetValue(motionObj);
                if (targetObj == null) return false;
                
                // 获取并设置Offset字段
                var offsetField = targetObj.GetType().GetField("Offset", BindingFlags.Public | BindingFlags.Instance);
                if (offsetField == null) return false;
                
                offsetField.SetValue(targetObj, newOffset);
                return true;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"反射设置Offset失败: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// 创建完整的Motion参数备份和覆盖系统
        /// </summary>
        [ContextMenu("高级：创建Motion参数备份系统")]
        public void CreateMotionParameterBackupSystem()
        {
            if (_wrapper == null) return;
            
            Debug.Log("=== 创建Motion参数备份系统 ===");
            
            var actions = _wrapper.GetActions();
            foreach (var action in actions)
            {
                var motionTracks = action.GetTracks<AbilityEventObj_Motion>();
                
                foreach (var track in motionTracks)
                {
                    var motionObj = track.EventObj as AbilityEventObj_Motion;
                    if (motionObj != null && motionObj.target != null)
                    {
                        // 备份所有Motion相关参数
                        Vector3 originalOffset = motionObj.target.Offset;
                        bool originalUseAbsolute = motionObj.target.UseAbsoluteCoordinates;
                        TimeControlMode originalTimeMode = motionObj.target.timeControlMode;
                        
                        Debug.Log($"💾 备份Motion参数 - 动作: {action.Name}");
                        Debug.Log($"   Offset: {originalOffset}");
                        Debug.Log($"   UseAbsoluteCoordinates: {originalUseAbsolute}");
                        Debug.Log($"   TimeControlMode: {originalTimeMode}");
                        
                        // 可以将这些值存储到Dictionary或自定义的备份系统中
                        // 用于后续恢复
                    }
                }
            }
        }
    }
} 
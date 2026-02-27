using UnityEngine;
using System.Collections.Generic;

namespace CombatEditor.Examples
{
    /// <summary>
    /// 运行时Motion参数覆盖系统
    /// 只在运行时生效，不修改原始ScriptableObject数据
    /// </summary>
    public class RuntimeMotionOverride : MonoBehaviour
    {
        [Header("测试用例")]
        public RuntimeParameterWrapper wrapper;
        
        // 运行时覆盖存储
        private static Dictionary<AbilityEventObj_Motion, Vector3> runtimeOffsetOverrides = 
            new Dictionary<AbilityEventObj_Motion, Vector3>();
        private static Dictionary<AbilityEventObj_Motion, Vector3> originalOffsets = 
            new Dictionary<AbilityEventObj_Motion, Vector3>();
        
        void Start()
        {
            // 注册Motion执行钩子
            RegisterMotionExecutionHook();
        }
        
        /// <summary>
        /// 设置运行时Offset覆盖（不修改原始数据）
        /// </summary>
        public void SetRuntimeOffsetOverride(string actionName, Vector3 offsetOverride)
        {
            var action = wrapper.GetAction(actionName);
            if (action == null)
            {
                Debug.LogError($"Action '{actionName}' not found!");
                return;
            }
            
            var motionTrack = action.GetTrack<AbilityEventObj_Motion>();
            if (motionTrack == null)
            {
                Debug.LogError($"Motion track not found in action '{actionName}'!");
                return;
            }
            
            var motion = motionTrack.EventObj as AbilityEventObj_Motion;
            if (motion == null)
            {
                Debug.LogError($"Motion event object not found!");
                return;
            }
            
            // 保存原始值（如果还没保存过）
            if (!originalOffsets.ContainsKey(motion))
            {
                originalOffsets[motion] = motion.target.Offset;
                Debug.Log($"保存原始Offset: {motion.target.Offset}");
            }
            
            // 设置运行时覆盖
            runtimeOffsetOverrides[motion] = offsetOverride;
            Debug.Log($"设置运行时覆盖Offset: {offsetOverride}");
        }
        
        /// <summary>
        /// 清除指定Motion的运行时覆盖
        /// </summary>
        public void ClearRuntimeOffsetOverride(string actionName)
        {
            var action = wrapper.GetAction(actionName);
            if (action == null) return;
            
            var motionTrack = action.GetTrack<AbilityEventObj_Motion>();
            if (motionTrack == null) return;
            
            var motion = motionTrack.EventObj as AbilityEventObj_Motion;
            if (motion == null) return;
            
            runtimeOffsetOverrides.Remove(motion);
            Debug.Log($"清除运行时覆盖，恢复原始值");
        }
        
        /// <summary>
        /// 清除所有运行时覆盖
        /// </summary>
        public void ClearAllRuntimeOverrides()
        {
            runtimeOffsetOverrides.Clear();
            originalOffsets.Clear();
            Debug.Log("清除所有运行时覆盖");
        }
        
        /// <summary>
        /// 获取当前生效的Offset值（考虑覆盖）
        /// </summary>
        public Vector3 GetEffectiveOffset(AbilityEventObj_Motion motion)
        {
            if (runtimeOffsetOverrides.ContainsKey(motion))
            {
                return runtimeOffsetOverrides[motion];
            }
            return motion.target.Offset;
        }
        
        /// <summary>
        /// 注册Motion执行钩子，在执行时应用覆盖
        /// </summary>
        private void RegisterMotionExecutionHook()
        {
            // 这里需要根据你的执行系统来实现
            // 示例：在Motion执行前临时应用覆盖，执行后恢复
        }
        
        /// <summary>
        /// 在Motion执行前调用，临时应用覆盖
        /// </summary>
        public static void ApplyRuntimeOverrideBeforeExecution(AbilityEventObj_Motion motion)
        {
            if (runtimeOffsetOverrides.ContainsKey(motion))
            {
                // 确保原始值已保存
                if (!originalOffsets.ContainsKey(motion))
                {
                    originalOffsets[motion] = motion.target.Offset;
                }
                
                // 临时应用覆盖
                motion.target.Offset = runtimeOffsetOverrides[motion];
                Debug.Log($"临时应用覆盖Offset: {motion.target.Offset}");
            }
        }
        
        /// <summary>
        /// 在Motion执行后调用，恢复原始值
        /// </summary>
        public static void RestoreOriginalAfterExecution(AbilityEventObj_Motion motion)
        {
            if (originalOffsets.ContainsKey(motion))
            {
                // 恢复原始值
                motion.target.Offset = originalOffsets[motion];
                Debug.Log($"恢复原始Offset: {motion.target.Offset}");
            }
        }
        
        // 测试方法
        [ContextMenu("Test Runtime Override")]
        public void TestRuntimeOverride()
        {
            // 设置运行时覆盖
            SetRuntimeOffsetOverride("LaiAttack", new Vector3(0, 0, 100));
            
            // 验证原始数据没有被修改
            var action = wrapper.GetAction("LaiAttack");
            var motionTrack = action.GetTrack<AbilityEventObj_Motion>();
            var motion = motionTrack.EventObj as AbilityEventObj_Motion;
            
            Debug.Log($"原始ScriptableObject Offset: {motion.target.Offset}");
            Debug.Log($"运行时生效Offset: {GetEffectiveOffset(motion)}");
        }
        
        [ContextMenu("Clear Override")]
        public void TestClearOverride()
        {
            ClearRuntimeOffsetOverride("LaiAttack");
        }
        
        void OnDestroy()
        {
            // 清理运行时数据
            ClearAllRuntimeOverrides();
        }
    }
} 
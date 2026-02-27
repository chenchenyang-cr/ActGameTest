using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CombatEditor
{
    public class RootMotionReceiver : MonoBehaviour
    {
        Animator _animator;
        
        [Header("Root Motion 设置")]
        [Tooltip("是否由战斗系统手动处理Root Motion（推荐开启）")]
        public bool manualRootMotionControl = true;
        
        [Tooltip("是否应用位置移动")]
        public bool applyRootPosition = true;
        
        [Tooltip("是否应用旋转（现在默认自动应用）")]
        public bool applyRootRotation = true;
        
        [Tooltip("自动检测是否需要应用Root Rotation")]
        public bool autoDetectRootRotation = true;
        
        [Header("简单旋转控制")]
        [Tooltip("旋转精度阈值（度） - 小于此值的旋转会被忽略")]
        public float rotationThreshold = 0.1f;
        
        [Header("Root Motion 信息")]
        [ReadOnly] public Vector3 CurrentRootMotion;
        [ReadOnly] public Quaternion CurrentRootRotation;
        [ReadOnly] public Vector3 AccumulatedRootMotion;
        [ReadOnly] public bool HasSignificantRootRotation;
        [ReadOnly] public float CurrentRotationAngle;
        
        // 记录原始的applyRootMotion设置
        private bool _originalApplyRootMotion;
        
        // Root rotation检测阈值
        private const float ROTATION_DETECTION_THRESHOLD = 0.1f;
        
        private void Awake()
        {
            _animator = GetComponent<Animator>();
            
            // 记录原始设置
            _originalApplyRootMotion = _animator.applyRootMotion;
            
            // 如果启用手动控制，则禁用Unity的自动root motion
            if (manualRootMotionControl)
            {
                _animator.applyRootMotion = false;
            }
        }
        
        private void OnDestroy()
        {
            // 恢复原始设置
            if (_animator != null)
            {
                _animator.applyRootMotion = _originalApplyRootMotion;
            }
        }
        
        private void OnAnimatorMove()
        {
            // 获取当前帧的root motion数据
            Vector3 deltaPosition = _animator.deltaPosition;
            Quaternion deltaRotation = _animator.deltaRotation;
            
            // 更新当前帧的root motion值
            CurrentRootMotion = deltaPosition;
            CurrentRootRotation = deltaRotation;
            
            // 检测是否有明显的root rotation
            float rotationAngle = Quaternion.Angle(deltaRotation, Quaternion.identity);
            CurrentRotationAngle = rotationAngle;
            HasSignificantRootRotation = rotationAngle > ROTATION_DETECTION_THRESHOLD;
            
            // 如果启用手动控制，则由我们来应用root motion
            if (manualRootMotionControl)
            {
                // 应用位置变化
                if (applyRootPosition && deltaPosition.magnitude > 0.001f)
                {
                    transform.position += deltaPosition;
                    AccumulatedRootMotion += deltaPosition;
                }
                
                // 应用旋转变化 - 最简单的方式
                bool shouldApplyRotation = applyRootRotation;
                if (autoDetectRootRotation && HasSignificantRootRotation)
                {
                    shouldApplyRotation = true;
                }
                
                if (shouldApplyRotation && rotationAngle > rotationThreshold)
                {
                    // 直接应用旋转，不做任何复杂处理
                    transform.rotation = transform.rotation * deltaRotation;
                }
            }
            else
            {
                // 如果不是手动控制，让Unity自动处理
                // 但仍然记录数据用于调试和其他用途
                AccumulatedRootMotion += deltaPosition;
            }
        }
        
        /// <summary>
        /// 重置累积的root motion数据
        /// </summary>
        public void ResetAccumulatedRootMotion()
        {
            AccumulatedRootMotion = Vector3.zero;
        }
        
        /// <summary>
        /// 获取自上次调用以来的累积位移
        /// </summary>
        public Vector3 GetAccumulatedDelta()
        {
            Vector3 delta = AccumulatedRootMotion;
            AccumulatedRootMotion = Vector3.zero;
            return delta;
        }
        
        /// <summary>
        /// 设置是否启用手动root motion控制
        /// </summary>
        public void SetManualRootMotionControl(bool enable)
        {
            manualRootMotionControl = enable;
            _animator.applyRootMotion = !enable;
        }
        
        /// <summary>
        /// 临时禁用root motion（比如在被击飞或特殊状态时）
        /// </summary>
        public void SetRootMotionEnabled(bool enabled)
        {
            if (manualRootMotionControl)
            {
                applyRootPosition = enabled;
                applyRootRotation = enabled;
            }
            else
            {
                _animator.applyRootMotion = enabled;
            }
        }
        
        /// <summary>
        /// 强制重置旋转到指定角度
        /// </summary>
        public void ForceSetRotation(Quaternion rotation)
        {
            transform.rotation = rotation;
            Debug.Log($"强制设置旋转到: {rotation.eulerAngles}");
        }
        
        /// <summary>
        /// 获取调试信息
        /// </summary>
        public string GetDebugInfo()
        {
            return $"手动控制: {manualRootMotionControl}, 应用位置: {applyRootPosition}, 应用旋转: {applyRootRotation}, " +
                   $"当前旋转角度: {CurrentRotationAngle:F2}°, 有明显旋转: {HasSignificantRootRotation}";
        }
    }
}

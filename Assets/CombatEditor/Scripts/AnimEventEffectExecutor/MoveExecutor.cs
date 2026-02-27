using System.Collections;
using System.Collections.Generic;
using UnityEngine;

 namespace CombatEditor
{	
	public class MoveExecutor
	{
	    public CombatController _combatController;
        RootMotionReceiver receiver;
        
        [Header("移动执行器设置")]
        [Tooltip("当Motion事件活跃时是否禁用root motion")]
        public bool disableRootMotionDuringMotionEvents = true;
        
        // 记录是否有Motion事件正在运行
        private bool _hasActiveMotionEvents = false;
        private bool _originalRootMotionState = true;
        
        public MoveExecutor(CombatController _controller)
        {
            _combatController = _controller;
            receiver = _combatController._animator.gameObject.AddComponent<RootMotionReceiver>();
            
            // 记录原始的root motion状态
            _originalRootMotionState = receiver.applyRootPosition;
        }
        
	    public void Execute()
	    {
	        
	    }
	    
        /// <summary>
        /// 通知移动执行器Motion事件开始
        /// </summary>
        public void OnMotionEventStart()
        {
            if (disableRootMotionDuringMotionEvents && !_hasActiveMotionEvents)
            {
                Debug.Log("🚫 Motion事件开始，临时禁用Root Motion以避免冲突");
                _hasActiveMotionEvents = true;
                receiver.SetRootMotionEnabled(false);
            }
        }
        
        /// <summary>
        /// 通知移动执行器Motion事件结束
        /// </summary>
        public void OnMotionEventEnd()
        {
            if (disableRootMotionDuringMotionEvents && _hasActiveMotionEvents)
            {
                Debug.Log("✅ Motion事件结束，恢复Root Motion");
                _hasActiveMotionEvents = false;
                receiver.SetRootMotionEnabled(_originalRootMotionState);
            }
        }
        
        /// <summary>
        /// Remember to change this to the physics you desire.
        /// 用于Motion事件的移动，会考虑与root motion的冲突
        /// </summary>
        /// <param name="DeltaMove"></param>
	    public void Move(Vector3 DeltaMove)
	    {
            // 如果有Motion事件在运行，直接应用移动
            if (_hasActiveMotionEvents)
            {
                _combatController.transform.Translate(DeltaMove, Space.World);
            }
            else
            {
                // 如果没有Motion事件，检查是否与root motion冲突
                Vector3 currentRootMotion = GetCurrentRootMotion();
                
                // 如果root motion的移动量很小，可以安全应用Motion移动
                if (currentRootMotion.magnitude < 0.001f)
                {
                    _combatController.transform.Translate(DeltaMove, Space.World);
                }
                else
                {
                    // 有明显的root motion，给出警告并选择性应用
                    Debug.LogWarning($"⚠️ 检测到Root Motion冲突: Root Motion = {currentRootMotion}, Motion Event = {DeltaMove}");
                    
                    // 可以选择混合或者优先使用其中一个
                    // 这里优先使用Motion事件的移动
                    _combatController.transform.Translate(DeltaMove, Space.World);
                }
            }
        }
        
        /// <summary>
        /// 用于非Motion事件的移动（如被击退等），不会禁用root motion
        /// </summary>
        /// <param name="DeltaMove"></param>
        public void MoveOverride(Vector3 DeltaMove)
        {
            _combatController.transform.Translate(DeltaMove, Space.World);
        }
        
	    public Vector3 GetCurrentRootMotion()
	    {
	        return receiver.CurrentRootMotion;
	    }
	    
	    /// <summary>
	    /// 获取累积的root motion并重置
	    /// </summary>
	    public Vector3 GetAndResetAccumulatedRootMotion()
	    {
	        return receiver.GetAccumulatedDelta();
	    }
	    
	    /// <summary>
	    /// 重置root motion累积数据
	    /// </summary>
	    public void ResetRootMotion()
	    {
	        receiver.ResetAccumulatedRootMotion();
	    }
	    
	    /// <summary>
	    /// 设置是否启用root motion
	    /// </summary>
	    public void SetRootMotionEnabled(bool enabled)
	    {
	        _originalRootMotionState = enabled;
	        if (!_hasActiveMotionEvents)
	        {
	            receiver.SetRootMotionEnabled(enabled);
	        }
	    }
	
	}
}

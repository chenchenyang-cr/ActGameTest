using UnityEngine;

namespace CombatEditor {	
	[System.Serializable]
	public class MotionTarget
	{
	    [Header("移动设置")]
	    public Vector3 Offset;

        [Header("移动模式")]
        [Tooltip("选择移动的实现方式")]
        public MovementMode movementMode = MovementMode.TransformBased;
	    
	        [Header("坐标类型")]
    [Tooltip("如果启用，使用绝对坐标增量值（不受角色朝向影响）；如果禁用，使用本地坐标系（受角色朝向影响）")]
    public bool UseAbsoluteCoordinates = false;
    
    [Header("精度设置")]
    [Tooltip("启用高精度移动模式可以减少累积误差，确保准确到达目的地。建议保持启用。")]
    public bool UseHighPrecisionMovement = true;
    
    [Header("时间控制")]
    [Tooltip("选择Motion事件的时间控制模式")]
    public TimeControlMode timeControlMode = TimeControlMode.AnimationTime;
    
    // 保持向后兼容性的旧字段
    [System.Obsolete("使用timeControlMode代替")]
    [HideInInspector]
    public bool UseRealTimePlayback = false;
	    
	    public GameObject CreateObject(CombatController controller)
	    {
	        var _obj = new GameObject("TargetPoint");
	        return _obj;
	    }
	    
	        /// <summary>
    /// 计算目标位置（在Motion事件开始时调用一次）
    /// </summary>
    /// <param name="controller">CombatController实例</param>
    /// <param name="startPosition">开始位置</param>
    /// <returns>目标绝对位置</returns>
    public Vector3 CalculateTargetPosition(CombatController controller, Vector3 startPosition)
    {
        if (UseAbsoluteCoordinates)
        {
            // 绝对坐标增量：直接加到起始位置上，不受角色朝向影响
            return startPosition + Offset;
        }
        else
        {
            // 本地坐标系：根据开始时的角色朝向计算目标位置
            return startPosition + controller._animator.transform.rotation * Offset;
        }
    }
    
    /// <summary>
    /// 计算当前帧的移动向量（朝向锁定的目标位置）
    /// </summary>
    /// <param name="currentPosition">当前位置</param>
    /// <param name="targetPosition">目标位置</param>
    /// <param name="deltaDistance">移动距离百分比</param>
    /// <returns>实际的移动向量</returns>
    public Vector3 CalculateMovement(Vector3 currentPosition, Vector3 targetPosition, float deltaDistance)
    {
        // 计算从起始位置到目标位置的总向量
        Vector3 totalMovement = targetPosition - currentPosition;
        // 根据deltaDistance计算当前帧应该移动的向量
        return totalMovement * deltaDistance;
	    }
	}
	
	[AbilityEvent]
	[CreateAssetMenu(menuName = "AbilityEvents / Motion")]
	public class AbilityEventObj_Motion : AbilityEventObj
	{
	    public MotionTarget target;
	    [ReadOnly]
	    public float MotionTime;
	    [MyAnimationCurve]
	    public AnimationCurve TimeToDis;
	
	
	    public override EventTimeType GetEventTimeType()
	    {
	        return EventTimeType.EventRange;
	    }
	    public override AbilityEventEffect Initialize()
	    {
	        return new AbilityEventEffect_Motion(this);
	    }
	    public override AbilityEventPreview InitializePreview()
	    {
	        return new AbilityEventPreview_Motion(this);
	    }
	}
	public partial class AbilityEventEffect_Motion : AbilityEventEffect
	{
	    float CurrentSpeed;
    Vector3 startPosition; // 保存开始位置
    Vector3 targetPosition; // 锁定的目标位置
    
    // 实际时间跟踪相关字段
    float _realTimeStartTime; // 事件开始的实际时间
    float _eventDuration; // 事件持续时间（秒）
    
    // HitStop感知时间跟踪相关字段
    float _hitStopAwareTimeStart; // HitStop感知时间开始
    float _accumulatedHitStopAwareTime; // 累积的HitStop感知时间
    
    // 精度修复相关字段
    Vector3 _lastCalculatedPosition; // 上一帧计算出的绝对位置
    bool _hasForcePositioned = false; // 是否已经强制归位

	    public override void StartEffect()
	    {
	        // 通知移动执行器Motion事件开始
	        _combatController._moveExecutor.OnMotionEventStart();
        
        // 保存开始位置
        startPosition = _combatController.transform.position;
        
        // 计算并锁定目标位置（只在开始时计算一次）
        targetPosition = TargetObj.target.CalculateTargetPosition(_combatController, startPosition);
        
        // 初始化精度修复字段
        _lastCalculatedPosition = startPosition;
        _hasForcePositioned = false;
        
        // 处理向后兼容性
        var timeMode = TargetObj.target.timeControlMode;
        if (TargetObj.target.UseRealTimePlayback)
        {
            timeMode = TimeControlMode.RealTime;
        }
        
        // 初始化时间跟踪
        switch (timeMode)
        {
            case TimeControlMode.RealTime:
                _realTimeStartTime = Time.time;
                _eventDuration = (eve.GetEventEndTime() - eve.GetEventStartTime()) * AnimObj.Clip.length;
                break;
                
            case TimeControlMode.HitStopAwareTime:
                _hitStopAwareTimeStart = Time.time;
                _accumulatedHitStopAwareTime = 0f;
                _eventDuration = (eve.GetEventEndTime() - eve.GetEventStartTime()) * AnimObj.Clip.length;
                break;
        }
	        
	        base.StartEffect();
	    }
	    float LastFrameDistance = 0;
	    public override void EffectRunning(float CurrentTimePercentage)
	    {
	        base.EffectRunning(CurrentTimePercentage);

        float timePercentageFloat = GetTimePercentage(CurrentTimePercentage);

        // Evaluate the distance with the calculated time percentage
        float targetDistance = TargetObj.TimeToDis.Evaluate(timePercentageFloat);
        
        if (TargetObj.target.UseHighPrecisionMovement)
        {
            // 高精度移动模式：计算当前应该在的绝对位置
            Vector3 totalMovement = targetPosition - startPosition;
            Vector3 currentTargetPosition = startPosition + totalMovement * targetDistance;
            
            // 计算从当前实际位置到目标位置的移动向量
            Vector3 motion = currentTargetPosition - _combatController.transform.position;
            
            // 应用移动
            if (motion.magnitude > 0.0001f) // 只有在需要移动时才应用
            {
                if (TargetObj.target.movementMode == MovementMode.TransformBased)
                {
                    _combatController.SimpleMoveRG(motion);
                }
                else
                {
                    // 物理移动模式 - 添加调试信息
                    Debug.Log($"Motion Event: Using PhysicsMove mode, motion={motion}, timePercentage={timePercentageFloat}");
                    _combatController.PhysicsMove(motion);
                }
            }
            
            // 更新记录的位置
            _lastCalculatedPosition = currentTargetPosition;
        }
        else
        {
            // 原有的增量移动模式（保持向后兼容）
            float deltaDistance = targetDistance - LastFrameDistance;
            LastFrameDistance = targetDistance;
            
            // 计算朝向锁定目标位置的移动向量
            Vector3 totalMovement = targetPosition - startPosition;
            Vector3 motion = totalMovement * deltaDistance;
            
            // 应用移动
            if (TargetObj.target.movementMode == MovementMode.TransformBased)
            {
                _combatController.SimpleMoveRG(motion);
            }
            else
            {
                // 物理移动模式（原有增量模式） - 添加调试信息
                Debug.Log($"Motion Event (Legacy): Using PhysicsMove mode, motion={motion}, deltaDistance={deltaDistance}");
                _combatController.PhysicsMove(motion);
            }
        }
        
        // 检测90%完成时强制归位到目标位置
        if (timePercentageFloat >= 0.9f && !_hasForcePositioned && TargetObj.target.UseHighPrecisionMovement)
        {
            // 强制归位到精确的目标位置
            _combatController.transform.position = targetPosition;
            _hasForcePositioned = true;
            Debug.Log($"Motion强制归位到目标位置: {targetPosition}，timePercentage: {timePercentageFloat}");
        }
        
        // 原有逻辑的兼容处理
        if (!TargetObj.target.UseHighPrecisionMovement && timePercentageFloat >= 1.0f)
        {
            LastFrameDistance = 1.0f;
        }
	    }
    
    /// <summary>
    /// 根据时间控制模式计算时间百分比
    /// </summary>
    private float GetTimePercentage(float currentTimePercentage)
    {
        // 处理向后兼容性
        var timeMode = TargetObj.target.timeControlMode;
        if (TargetObj.target.UseRealTimePlayback)
        {
            timeMode = TimeControlMode.RealTime;
        }
        
        switch (timeMode)
        {
            case TimeControlMode.RealTime:
                // 使用实际时间计算：不受动画速度和hitstop影响
                float realTimeElapsed = Time.time - _realTimeStartTime;
                return Mathf.Clamp01(realTimeElapsed / _eventDuration);
                
            case TimeControlMode.HitStopAwareTime:
                // 使用HitStop感知时间：不受AnimSpeed影响，但受hitstop影响
                return GetHitStopAwareTimePercentage();
                
            default: // AnimationTime
                // 使用动画时间计算：受动画速度和hitstop影响（原有逻辑）
                double startTime = eve.GetEventStartTime();
                double endTime = eve.GetEventEndTime();
                double currentTime = currentTimePercentage;
                
                double timePercentage = (currentTime - startTime) / (endTime - startTime);
                timePercentage = System.Math.Min(1.0, System.Math.Max(0.0, timePercentage));
                
                return (float)timePercentage;
        }
    }
    
    /// <summary>
    /// 计算HitStop感知时间百分比
    /// </summary>
    private float GetHitStopAwareTimePercentage()
    {
        // 累积时间，但在hitstop期间根据hitstop速度调整
        float deltaTime = Time.deltaTime;
        
        if (_combatController.isInHitStop)
        {
            // 在hitstop期间，根据当前的动画速度调整时间流逝
            float hitStopSpeed = _combatController._animator.speed;
            deltaTime *= hitStopSpeed;
        }
        // 注意：不在hitstop期间时，不受AnimSpeed影响，使用正常的deltaTime
        
        _accumulatedHitStopAwareTime += deltaTime;
        
        return Mathf.Clamp01(_accumulatedHitStopAwareTime / _eventDuration);
	    }
    
	    public override void EndEffect()
	    {
	        LastFrameDistance = 0;
	        
	                // 重置精度修复字段
        _lastCalculatedPosition = Vector3.zero;
        _hasForcePositioned = false;
	        
	        // 通知移动执行器Motion事件结束
	        _combatController._moveExecutor.OnMotionEventEnd();
	        
	        base.EndEffect();
	    }
	}
	public partial class AbilityEventEffect_Motion : AbilityEventEffect
	{
	    AbilityEventObj_Motion TargetObj => (AbilityEventObj_Motion)_EventObj;
	    public AbilityEventEffect_Motion(AbilityEventObj InitObj) : base(InitObj)
	    {
	        _EventObj = InitObj;
	    }
	}
}

/// <summary>
/// 时间控制模式枚举
/// </summary>
[System.Serializable]
public enum TimeControlMode
{
    [Tooltip("跟随动画时间播放，受AnimSpeed和hitstop影响")]
    AnimationTime = 0,
    
    [Tooltip("基于实际时间播放，不受AnimSpeed和hitstop影响")]
    RealTime = 1,
    
    [Tooltip("不受AnimSpeed影响，但受hitstop影响")]
    HitStopAwareTime = 2
}

/// <summary>
/// 移动模式枚举
/// </summary>
[System.Serializable]
public enum MovementMode
{
    [Tooltip("基于Transform的移动，会忽略物理碰撞")]
    TransformBased = 0,
    
    [Tooltip("基于物理的移动，会响应碰撞和物理效果")]
    PhysicsBased = 1,
}

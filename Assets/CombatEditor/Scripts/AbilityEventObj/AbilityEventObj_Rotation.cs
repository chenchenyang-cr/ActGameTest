using UnityEngine;

namespace CombatEditor {	
	[System.Serializable]
	public class RotationTarget
	{
	    [Header("旋转设置")]
	    public Vector3 EulerRotation;
	    
	    [Header("时间控制")]
	    [Tooltip("选择Rotation事件的时间控制模式")]
	    public TimeControlMode timeControlMode = TimeControlMode.AnimationTime;
	    
	    // 保持向后兼容性的旧字段
	    [System.Obsolete("使用timeControlMode代替")]
	    [HideInInspector]
	    public bool UseRealTimePlayback = false;
	    
	    [Header("目标对象")]
	    [Tooltip("如果不指定对象，将旋转CombatController本身")]
	    public GameObject TargetObject;
	    
	    [Tooltip("是否使用自定义对象名称查找")]
	    public bool UseCustomObjectName;
	    
	    [Tooltip("要旋转的对象的名称")]
	    public string CustomObjectName;
	    
	    public GameObject CreateObject(CombatController controller)
	    {
	        var _obj = new GameObject("RotationPoint");
	        return _obj;
	    }
	    
	    /// <summary>
	    /// 获取要旋转的Transform
	    /// </summary>
	    /// <param name="controller">CombatController实例</param>
	    /// <returns>要旋转的Transform，如果找不到则返回null</returns>
	    public Transform GetTargetTransform(CombatController controller)
	    {
	        // 如果指定了具体的游戏对象
	        if (TargetObject != null)
	        {
	            return TargetObject.transform;
	        }
	        
	        // 如果使用自定义名称查找
	        if (UseCustomObjectName && !string.IsNullOrEmpty(CustomObjectName))
	        {
	            // 首先在controller的子对象中查找
	            Transform childTransform = controller.transform.Find(CustomObjectName);
	            if (childTransform != null)
	            {
	                return childTransform;
	            }
	            
	            // 如果在子对象中找不到，尝试在整个场景中查找
	            GameObject foundObject = GameObject.Find(CustomObjectName);
	            if (foundObject != null)
	            {
	                return foundObject.transform;
	            }
	            
	            Debug.LogWarning($"找不到名为 '{CustomObjectName}' 的游戏对象");
	            return null;
	        }
	        
	        // 默认返回CombatController的transform
	        return controller.transform;
	    }
	}
	
	[AbilityEvent]
	[CreateAssetMenu(menuName = "AbilityEvents / Rotation")]
	public class AbilityEventObj_Rotation : AbilityEventObj
	{
	    public RotationTarget target;
	    [ReadOnly]
	    public float RotationTime;
	    [MyAnimationCurve]
	    public AnimationCurve TimeToRotation;
	
	    public override EventTimeType GetEventTimeType()
	    {
	        return EventTimeType.EventRange;
	    }
	    public override AbilityEventEffect Initialize()
	    {
	        return new AbilityEventEffect_Rotation(this);
	    }
	    public override AbilityEventPreview InitializePreview()
	    {
	        return new AbilityEventPreview_Rotation(this);
	    }
	}
	
	public partial class AbilityEventEffect_Rotation : AbilityEventEffect
	{
	    float CurrentSpeed;
	    Transform targetTransform; // 缓存目标Transform
	    
	    // 实际时间跟踪相关字段
	    float _realTimeStartTime; // 事件开始的实际时间
	    float _eventDuration; // 事件持续时间（秒）
	    
	    // HitStop感知时间跟踪相关字段
	    float _hitStopAwareTimeStart; // HitStop感知时间开始
	    float _accumulatedHitStopAwareTime; // 累积的HitStop感知时间
	    
	    public override void StartEffect()
	    {
	        base.StartEffect();
	        LastRotationPercentage = 0f;
	        
	        // 获取目标Transform并缓存
	        targetTransform = TargetObj.target.GetTargetTransform(_combatController);
	        if (targetTransform == null)
	        {
	            Debug.LogError("无法找到要旋转的目标对象，将使用CombatController");
	            targetTransform = _combatController.transform;
	        }
	        
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
	    }
	    
	    float LastRotationPercentage = 0;
	    public override void EffectRunning(float CurrentTimePercentage)
	    {
	        base.EffectRunning(CurrentTimePercentage);

	        float timePercentageFloat = GetTimePercentage(CurrentTimePercentage);
            
	        // Evaluate the rotation amount with the calculated time percentage
            float rotationPercentage = TargetObj.TimeToRotation.Evaluate(timePercentageFloat);
	        
	        // Calculate delta rotation to apply
	        float deltaRotation = rotationPercentage - LastRotationPercentage;
	        LastRotationPercentage = rotationPercentage;
	        
	        // Apply rotation delta to current rotation
        if (Mathf.Abs(deltaRotation) > 0.001f && targetTransform != null)
	        {
	            // Calculate rotation based on target euler angles and interpolation value
	            Vector3 rotationAmount = new Vector3(
	                TargetObj.target.EulerRotation.x * deltaRotation,
	                TargetObj.target.EulerRotation.y * deltaRotation,
	                TargetObj.target.EulerRotation.z * deltaRotation
	            );
                
            // Apply the rotation to the target transform
            targetTransform.Rotate(rotationAmount, Space.Self);
        }
        
        // 如果使用非动画时间模式且已完成，提前结束
        if ((TargetObj.target.timeControlMode == TimeControlMode.RealTime || 
             TargetObj.target.timeControlMode == TimeControlMode.HitStopAwareTime) && 
            timePercentageFloat >= 1.0f)
        {
            // 确保完成完整的旋转
            if (LastRotationPercentage < 1.0f)
            {
                float remainingRotation = 1.0f - LastRotationPercentage;
                Vector3 finalRotationAmount = new Vector3(
                    TargetObj.target.EulerRotation.x * remainingRotation,
                    TargetObj.target.EulerRotation.y * remainingRotation,
                    TargetObj.target.EulerRotation.z * remainingRotation
                );
                targetTransform.Rotate(finalRotationAmount, Space.Self);
                LastRotationPercentage = 1.0f;
            }
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
	        LastRotationPercentage = 0;
        targetTransform = null; // 清除缓存
	        base.EndEffect();
	    }
	}
	
	public partial class AbilityEventEffect_Rotation : AbilityEventEffect
	{
	    AbilityEventObj_Rotation TargetObj => (AbilityEventObj_Rotation)_EventObj;
	    public AbilityEventEffect_Rotation(AbilityEventObj InitObj) : base(InitObj)
	    {
	        _EventObj = InitObj;
	    }
	}
} 
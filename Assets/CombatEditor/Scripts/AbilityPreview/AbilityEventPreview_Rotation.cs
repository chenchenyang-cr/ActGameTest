using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CombatEditor
{	
	public class AbilityEventPreview_Rotation : AbilityEventPreview
	{
	    public AbilityEventObj_Rotation Obj => (AbilityEventObj_Rotation)_EventObj;
	    Transform targetTransform; // 缓存目标Transform
	    Quaternion originalRotation; // 保存原始旋转
	    
	    // 实际时间跟踪相关字段（用于预览）
	    float _previewRealTimeStart; // 预览开始的实际时间
	    float _previewEventDuration; // 预览事件持续时间
	    
	    public AbilityEventPreview_Rotation(AbilityEventObj Obj) : base(Obj)
	    {
	        _EventObj = Obj;
	    }
	
#if UNITY_EDITOR
	    public override void InitPreview()
	    {
	        base.InitPreview();
	        
	        // 获取目标Transform
	        targetTransform = Obj.target.GetTargetTransform(_combatController);
	        if (targetTransform == null)
	        {
	            Debug.LogWarning("无法找到要旋转的目标对象，将使用CombatController");
	            targetTransform = _combatController.transform;
	        }
	        
	        // 保存原始旋转
	        originalRotation = targetTransform.rotation;
	        
	        // 初始化实际时间跟踪（用于预览）
	        if (Obj.target.UseRealTimePlayback)
	        {
	            _previewRealTimeStart = Time.realtimeSinceStartup;
	            _previewEventDuration = (EndTimePercentage - StartTimePercentage) * AnimObj.Clip.length;
	        }
	        
	        CreateRotationTarget();
	        CreateRotationHandles();
	    }
	    
	    public override bool NeedStartFrameValue()
	    {
	        return true;
	    }
	    
	    public GameObject PreviewTarget;
	    public void CreateRotationTarget()
	    {
	        PreviewTarget = new GameObject("Preview_Rotation");
	        PreviewTarget.transform.SetParent(previewGroup.transform);
	    }
	    
	    PreviewRotationHandle handle;
	    public void CreateRotationHandles()
	    {
	        handle = previewGroup.AddComponent<PreviewRotationHandle>();
	        handle.StartRotation = _combatController.transform.rotation;
	        
	        handle.TargetTrans = PreviewTarget.transform;
	        handle.Init();
	        handle.target = Obj.target;
	        handle._combatController = _combatController;
	        handle._preview = this;
	    }
	    
	    public override void PreviewRunning(float CurrentTimePercentage)
	    {
	        base.PreviewRunning(CurrentTimePercentage);
	        Obj.RotationTime = (EndTimePercentage - StartTimePercentage) * AnimObj.Clip.length;
	        
	        if (targetTransform == null) return;
	        
	        // 如果使用实际时间播放模式，需要特殊处理预览
	        if (Obj.target.timeControlMode == TimeControlMode.RealTime || 
	            (Obj.target.UseRealTimePlayback && Obj.target.timeControlMode == TimeControlMode.AnimationTime))
	        {
	            // 检查是否应该重新初始化实际时间跟踪（当进入事件范围时）
	            if (PreviewInRange(CurrentTimePercentage) && _previewRealTimeStart == 0)
	            {
	                _previewRealTimeStart = Time.realtimeSinceStartup;
	            }
	            
	            // 如果超出事件范围，重置实际时间跟踪
	            if (!PreviewInRange(CurrentTimePercentage) && CurrentTimePercentage <= EndTimePercentage)
	            {
	                _previewRealTimeStart = 0;
	            }
	        }
	        else if (Obj.target.timeControlMode == TimeControlMode.HitStopAwareTime)
	        {
	            // HitStop感知时间模式的预览处理
	            if (PreviewInRange(CurrentTimePercentage) && _previewRealTimeStart == 0)
	            {
	                _previewRealTimeStart = Time.realtimeSinceStartup;
	            }
	            
	            if (!PreviewInRange(CurrentTimePercentage) && CurrentTimePercentage <= EndTimePercentage)
	            {
	                _previewRealTimeStart = 0;
	            }
	        }
	        
	        if (PreviewInRange(CurrentTimePercentage) || CurrentTimePercentage > EndTimePercentage)
	        {
	            // 如果旋转的是CombatController本身，使用原来的逻辑
	            if (targetTransform == _combatController.transform)
	        {
	            // Save current position before rotating
	            Vector3 currentPosition = _combatController.transform.position;
	            
	            // Apply rotation
	            Quaternion targetRotation = CombatGlobalEditorValue.CharacterRotBeforePreview * GetRotationAtCurrentFrame(CurrentTimePercentage);
	            _combatController.transform.rotation = targetRotation;
	            
	            // Restore position (rotation might have affected it)
	            _combatController.transform.position = currentPosition;
	        }
	        else
	            {
	                // 对于其他对象，应用旋转
	                Quaternion rotationDelta = GetRotationAtCurrentFrame(CurrentTimePercentage);
	                targetTransform.rotation = originalRotation * rotationDelta;
	            }
	        }
	        else
	        {
	            // 恢复原始旋转
	            if (targetTransform == _combatController.transform)
	        {
	            _combatController.transform.rotation = CombatGlobalEditorValue.CharacterRotBeforePreview;
	            }
	            else
	            {
	                targetTransform.rotation = originalRotation;
	            }
	        }
	    }
	    
	    public Quaternion GetRotationAtCurrentFrame(float CurrentTimePercentage)
	    {
	        Obj.RotationTime = (EndTimePercentage - StartTimePercentage) * AnimObj.Clip.length;
	
	        if (PreviewInRange(CurrentTimePercentage) || CurrentTimePercentage > EndTimePercentage)
	        {
            float timePercentageFloat = GetPreviewTimePercentage(CurrentTimePercentage);
            
	
	            float rotationPercentage = 0;
	            if (Obj.TimeToRotation != null)
	            {
	                rotationPercentage = Obj.TimeToRotation.Evaluate(timePercentageFloat);
	            }
	            else
	            {
	                Obj.TimeToRotation = new AnimationCurve();
	                Obj.TimeToRotation.AddKey(0, 0);
	                Obj.TimeToRotation.AddKey(1, 1);
	            }
	            
	            // Calculate the rotation with minimized floating point errors
                Vector3 targetRotation = Obj.target.EulerRotation;
                
                // Calculate rotation for the current percentage
                Vector3 scaledRotation = new Vector3(
                    targetRotation.x * rotationPercentage,
                    targetRotation.y * rotationPercentage,
                    targetRotation.z * rotationPercentage
                );
                
                // Convert to quaternion
                Quaternion rotation = Quaternion.Euler(scaledRotation);
                
                return rotation;
	        }
	        return Quaternion.identity;
	    }
	
	    public override void DestroyPreview()
	    {
	        // 恢复目标对象的原始旋转
	        if (targetTransform != null)
	        {
	            if (targetTransform == _combatController.transform)
	    {
	        _combatController.transform.rotation = CombatGlobalEditorValue.CharacterRotBeforePreview;
	            }
	            else
	            {
	                targetTransform.rotation = originalRotation;
	            }
	        }
	        
	        base.DestroyPreview();
	    }
	    
	    public override void GetStartFrameDataBeforePreview()
	    {
	        base.GetStartFrameDataBeforePreview();
	        FetchDataAtStartFrame();
	    }
	    
	    public Vector3 NodePosAtStartFrame = Vector3.zero;
	    public Quaternion NodeRotAtStartFrame = Quaternion.identity;
	    public Quaternion AnimatorRotAtStartFrame = Quaternion.identity;
	    
	    public void FetchDataAtStartFrame()
	    {
	        AnimatorRotAtStartFrame = _combatController.GetNodeTranform(CharacterNode.NodeType.Animator).rotation;
	        NodePosAtStartFrame = _combatController.transform.position;
	        NodeRotAtStartFrame = _combatController.transform.rotation;
	        
	        handle.SetStartFrameRotation(NodeRotAtStartFrame, AnimatorRotAtStartFrame);
	    }
    
    /// <summary>
    /// 根据时间控制模式计算预览时间百分比
    /// </summary>
    private float GetPreviewTimePercentage(float currentTimePercentage)
    {
        // 处理向后兼容性
        var timeMode = Obj.target.timeControlMode;
        if (Obj.target.UseRealTimePlayback)
        {
            timeMode = TimeControlMode.RealTime;
        }
        
        switch (timeMode)
        {
            case TimeControlMode.RealTime:
                // 使用实际时间计算（用于预览）
                float realTimeElapsed = Time.realtimeSinceStartup - _previewRealTimeStart;
                return Mathf.Clamp01(realTimeElapsed / _previewEventDuration);
                
            case TimeControlMode.HitStopAwareTime:
                // HitStop感知时间模式（预览时简化为实际时间）
                // 在编辑器预览中，我们无法完全模拟hitstop状态，所以简化为实际时间
                float hitStopAwareTimeElapsed = Time.realtimeSinceStartup - _previewRealTimeStart;
                return Mathf.Clamp01(hitStopAwareTimeElapsed / _previewEventDuration);
                
            default: // AnimationTime
                // 使用动画时间计算（原有逻辑）
                double startTime = StartTimePercentage;
                double endTime = EndTimePercentage;
                double currentTime = currentTimePercentage;
                
                double timePercentage = (currentTime - startTime) / (endTime - startTime);
                timePercentage = System.Math.Min(1.0, System.Math.Max(0.0, timePercentage));
                
                return (float)timePercentage;
        }
    }
#endif
	}
} 
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CombatEditor
{	
	public class AbilityEventPreview_Motion : AbilityEventPreview
	{
	    public AbilityEventObj_Motion Obj => (AbilityEventObj_Motion)_EventObj;
	    Vector3 previewStartPosition; // 预览开始位置
	    Vector3 previewTargetPosition; // 预览目标位置
	    
	    // 实际时间跟踪相关字段（用于预览）
	    float _previewRealTimeStart; // 预览开始的实际时间
	    float _previewEventDuration; // 预览事件持续时间
	    
	    public AbilityEventPreview_Motion(AbilityEventObj Obj) :base(Obj)
	    {
	        _EventObj = Obj;
	    }
	
#if UNITY_EDITOR
	    public override void InitPreview()
	    {
	        base.InitPreview();
	        
	        // 初始化预览位置
	        previewStartPosition = CombatGlobalEditorValue.CharacterTransPosBeforePreview;
	        previewTargetPosition = Obj.target.CalculateTargetPosition(_combatController, previewStartPosition);
	        
	        // 初始化实际时间跟踪（用于预览）
	        if (Obj.target.UseRealTimePlayback)
	        {
	            _previewRealTimeStart = Time.realtimeSinceStartup;
	            _previewEventDuration = (EndTimePercentage - StartTimePercentage) * AnimObj.Clip.length;
	        }
	        
	        CreateMotionTarget();
	        CreateMotionHandles();
	    }
	    public override bool NeedStartFrameValue()
	    {
	        return true;
	    }
	    public GameObject PreviewTarget;
	    public void CreateMotionTarget()
	    {
	        PreviewTarget = new GameObject("Preview_Motion");
	        PreviewTarget.transform.SetParent(previewGroup.transform);
	    }
	    PreviewMotionHandle handle;
	    public void CreateMotionHandles()
	    {
	        handle = previewGroup.AddComponent<PreviewMotionHandle>();
	        handle.StartPosition = _combatController.transform.position;
	
	        handle.TargetTrans = PreviewTarget.transform;
	        //handle = PreviewTarget.AddComponent<PreviewMotionHandle>();
	        handle.Init();
	        handle.target = Obj.target;
	        handle._combatController = _combatController;
	        handle._preview = this;
	        //AddMotionHandles
	    }
	    public override void PreviewRunning(float CurrentTimePercentage)
	    {
	        base.PreviewRunning(CurrentTimePercentage);
	        Obj.MotionTime = (EndTimePercentage - StartTimePercentage) * AnimObj.Clip.length;
	        
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
	            _combatController.transform.position = CombatGlobalEditorValue.CharacterTransPosBeforePreview + GetOffsetAtCurrentFrame(CurrentTimePercentage);
	            
	        }
	        else
	        {
	            _combatController.transform.position = CombatGlobalEditorValue.CharacterTransPosBeforePreview;
	        }
	    }
	    
	    public Vector3 GetOffsetAtCurrentFrame(float CurrentTimePercentage)
	    {
	        Obj.MotionTime = (EndTimePercentage - StartTimePercentage) * AnimObj.Clip.length;
	
	        if (PreviewInRange(CurrentTimePercentage) || CurrentTimePercentage > EndTimePercentage)
	        {
            float timePercentageFloat = GetPreviewTimePercentage(CurrentTimePercentage);
	
	            float distancePercentage = 0;
	            if (Obj.TimeToDis != null)
	            {
	                distancePercentage = Obj.TimeToDis.Evaluate(timePercentageFloat);
	            }
	            else
	            {
	                Obj.TimeToDis = new AnimationCurve();
	                Obj.TimeToDis.AddKey(0, 0);
	                Obj.TimeToDis.AddKey(1, 1);
	            }
	            
            // 使用锁定的目标位置计算移动偏移量
            Vector3 motion = Obj.target.CalculateMovement(previewStartPosition, previewTargetPosition, distancePercentage);
            return motion;
	        }
	        return Vector3.zero;
	    }
	
	    public override void DestroyPreview()
	    {
	        _combatController.transform.position = CombatGlobalEditorValue.CharacterTransPosBeforePreview;
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
	    public Vector3 ControllerStartPosition;
	
	    public void FetchDataAtStartFrame()
	    {
	        ControllerStartPosition = handle.StartPosition + _combatController._animator.transform.rotation * CombatGlobalEditorValue.CurrentMotionTAtGround;
	        AnimatorRotAtStartFrame = _combatController.GetNodeTranform(CharacterNode.NodeType.Animator).rotation;
	
	        NodePosAtStartFrame = ControllerStartPosition;
	        NodeRotAtStartFrame = AnimatorRotAtStartFrame;
	
	        handle.SetStartFramePos(NodePosAtStartFrame, NodeRotAtStartFrame, AnimatorRotAtStartFrame);
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

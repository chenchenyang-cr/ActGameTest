using UnityEngine;
using UnityEditor;

namespace CombatEditor
{
    [AbilityEvent]
    [CreateAssetMenu(menuName = "AbilityEvents/HitStop")]
    public class AbilityEventObj_HitStop : AbilityEventObj
    {
        [Tooltip("顿帧持续的帧数")]
        public int hitStopFrames = 10;
        
        [Tooltip("顿帧期间的动画速度，0表示完全停止")]
        [Range(0f, 0.5f)]
        public float animationSpeed = 0f;
        
        [Tooltip("是否启用精确帧定位（防止跳过目标帧）")]
        public bool enablePreciseFramePosition = true;

        [Header("速度恢复设置")]
        [Tooltip("是否启用顿帧结束后的速度恢复功能")]
        public bool enableSpeedRecovery = false;

        [Tooltip("速度恢复所需的时间（秒）")]
        public float recoveryDuration = 0.5f;

        [Tooltip("最终恢复到的动画速度")]
        public float recoveryTargetSpeed = 1f;

        [Tooltip("速度恢复的插值曲线")]
        public AnimationCurve recoveryCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        
        public override EventTimeType GetEventTimeType()
        {
            return EventTimeType.EventTime;
        }
        
        public override AbilityEventEffect Initialize()
        {
            return new AbilityEventEffect_HitStop(this);
        }
        
        public override AbilityEventPreview InitializePreview()
        {
            return new AbilityEventPreview_HitStop(this);
        }
        
        public override bool PreviewExist()
        {
            return true;
        }
    }

    // Runtime effect implementation
    public partial class AbilityEventEffect_HitStop : AbilityEventEffect
    {
        private bool hasStartedHitStop = false;
        private float targetFrameTime = -1f;
        private System.Action<float> frameAdjustmentCallback = null;
        
        public override void StartEffect()
        {
            base.StartEffect();
            AbilityEventObj_HitStop eventObj = (AbilityEventObj_HitStop)_EventObj;
            
            // 应用Sword的HitStopMultiplier倍数到顿帧帧数
            int finalHitStopFrames = Mathf.RoundToInt(eventObj.hitStopFrames *1);
            
            if (eventObj.enablePreciseFramePosition && eve != null)
            {
                // 获取当前的精确时间点
                float exactTime = eve.EventTime;
                
                // 设置精确帧回调
                EnablePreciseTiming(exactTime, OnFrameAdjustmentNeeded);
                
                // 开始顿帧，并传递事件对象和精确帧位置，使用计算后的帧数
                _combatController.StartHitStop(
                    finalHitStopFrames, 
                    eventObj.animationSpeed, 
                    eventObj, 
                    exactTime);
            }
            else
            {
                // 使用常规顿帧，使用计算后的帧数
                _combatController.StartHitStop(
                    finalHitStopFrames, 
                    eventObj.animationSpeed, 
                    eventObj);
            }
            
            hasStartedHitStop = true;
        }
        
        // 启用精确帧定位
        private void EnablePreciseTiming(float time, System.Action<float> callback)
        {
            targetFrameTime = time;
            frameAdjustmentCallback = callback;
        }
        
        // 完成帧调整
        private void FrameAdjustmentComplete()
        {
            targetFrameTime = -1f;
        }
        
        // 帧调整回调，确保精确顿帧位置
        private void OnFrameAdjustmentNeeded(float targetTime)
        {
            if (_combatController != null && hasStartedHitStop)
            {
                // 请求精确帧调整
                _combatController.RequestFrameAdjustment(targetTime);
                
                // 重置调整状态
                FrameAdjustmentComplete();
            }
        }
        
        public override void EffectRunning()
        {
            base.EffectRunning();
            
            // 检查是否需要精确帧调整
            if (targetFrameTime > 0 && frameAdjustmentCallback != null)
            {
                // 可以在这里添加额外的检查逻辑，判断是否需要触发帧调整回调
                frameAdjustmentCallback(targetFrameTime);
            }
        }
        
        public override void EndEffect()
        {
            // 如果事件结束时顿帧仍在进行，则结束顿帧
            // 注意：通常顿帧会根据帧数自动结束，但如果事件被提前终止，需要手动结束顿帧
            if (hasStartedHitStop)
            {
                _combatController.EndHitStop();
                hasStartedHitStop = false;
            }
            
            base.EndEffect();
        }
    }

    // Constructor and helper
    public partial class AbilityEventEffect_HitStop : AbilityEventEffect
    {
        AbilityEventObj_HitStop EventObj => (AbilityEventObj_HitStop)_EventObj;
        
        public AbilityEventEffect_HitStop(AbilityEventObj InitObj) : base(InitObj)
        {
            _EventObj = InitObj;
        }
    }

    // Editor preview implementation
    public class AbilityEventPreview_HitStop : AbilityEventPreview
    {
        AbilityEventObj_HitStop EventObj => (AbilityEventObj_HitStop)_EventObj;
        
        public AbilityEventPreview_HitStop(AbilityEventObj Obj) : base(Obj)
        {
        }
        
        public override void InitPreview()
        {
            base.InitPreview();
        }
        
        public override void PassStartFrame()
        {
            base.PassStartFrame();
            Debug.Log("[Editor Preview] HitStop: " + EventObj.hitStopFrames + " frames");
        }
        
        public override void DestroyPreview()
        {
            base.DestroyPreview();
        }
    }
} 
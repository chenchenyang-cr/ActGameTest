using UnityEngine;

namespace CombatEditor
{
    [AbilityEvent]
    [CreateAssetMenu(menuName = "AbilityEvents/SetAttackPhase")]
    public class AbilityEventObj_SetAttackPhase : AbilityEventObj
    {
        [Tooltip("要设置的攻击阶段")]
        public int targetPhase = 0;
        
        [Tooltip("攻击阶段描述（方便在编辑器中识别）")]
        public string phaseDescription = "";
        
        public override EventTimeType GetEventTimeType()
        {
            return EventTimeType.EventTime;
        }
        
        public override AbilityEventEffect Initialize()
        {
            return new AbilityEventEffect_SetAttackPhase(this);
        }
        
        public override AbilityEventPreview InitializePreview()
        {
            return new AbilityEventPreview_SetAttackPhase(this);
        }
        
        public override bool PreviewExist()
        {
            return true;
        }
    }
    
    // 运行时效果实现
    public partial class AbilityEventEffect_SetAttackPhase : AbilityEventEffect
    {
        private int previousPhase = 0;
        private bool hasExecuted = false;
        
        public override void StartEffect()
        {
            base.StartEffect();
            
            if (_combatController != null)
            {
                // 记录之前的阶段
                previousPhase = _combatController.GetAttackPhase();
                
                // 设置新的攻击阶段
                AbilityEventObj_SetAttackPhase eventObj = (AbilityEventObj_SetAttackPhase)_EventObj;
                _combatController.SetAttackPhase(eventObj.targetPhase);
                
                hasExecuted = true;
                Debug.Log($"设置攻击阶段: {eventObj.targetPhase} {(string.IsNullOrEmpty(eventObj.phaseDescription) ? "" : "- " + eventObj.phaseDescription)}");
            }
        }
        
        public override void EffectRunning()
        {
            base.EffectRunning();
        }
        
        public override void EndEffect()
        {
            base.EndEffect();
        }
    }
    
    // 构造函数和辅助方法
    public partial class AbilityEventEffect_SetAttackPhase : AbilityEventEffect
    {
        AbilityEventObj_SetAttackPhase EventObj => (AbilityEventObj_SetAttackPhase)_EventObj;
        
        public AbilityEventEffect_SetAttackPhase(AbilityEventObj InitObj) : base(InitObj)
        {
            _EventObj = InitObj;
        }
    }
    
    // 编辑器预览实现
    public class AbilityEventPreview_SetAttackPhase : AbilityEventPreview
    {
        AbilityEventObj_SetAttackPhase EventObj => (AbilityEventObj_SetAttackPhase)_EventObj;
        
        public AbilityEventPreview_SetAttackPhase(AbilityEventObj Obj) : base(Obj)
        {
        }
        
        public override void InitPreview()
        {
            base.InitPreview();
        }
        
        public override void PreviewRunning(float CurrentTimePercentage)
        {
            base.PreviewRunning(CurrentTimePercentage);
            
            if (CurrentTimePercentage >= StartTimePercentage && CurrentTimePercentage < StartTimePercentage + 0.01f)
            {
                // 在预览模式下，当时间点接近事件触发点时，在编辑器中显示提示
                Debug.Log($"[预览] 攻击阶段变更: {EventObj.targetPhase} {(string.IsNullOrEmpty(EventObj.phaseDescription) ? "" : "- " + EventObj.phaseDescription)}");
            }
        }
        
        public override void DestroyPreview()
        {
            base.DestroyPreview();
        }
    }
} 
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

 namespace CombatEditor {	
	
	[AbilityEvent]
	[CreateAssetMenu(menuName = "AbilityEvents/ AnimSpeed")]
	public class AbilityEventObj_AnimSpeed : AbilityEventObj
	{
	    [Header("Basic Speed")]
	    public float Speed = 1;
	    
	    [Header("Curve Lerp (Optional)")]
	    [Tooltip("Enable this to use curve-based speed interpolation instead of constant speed")]
	    public bool UseCurveLerp = false;
	    
	    [Tooltip("Curve to interpolate between StartSpeed and EndSpeed over the event duration")]
	    public TweenCurve SpeedCurve = new TweenCurve();
	    
	    public override EventTimeType GetEventTimeType()
	    {
	        return EventTimeType.EventRange;
	    }
	    public override AbilityEventEffect Initialize()
	    {
	        return new AbilityEventEffect_AnimSpeed(this);
	    }
	    public override AbilityEventPreview InitializePreview()
	    {
	        return new AbilityEventPreview_AnimSpeed(this);
	    }
	}
	
	public class AbilityEventEffect_AnimSpeed : AbilityEventEffect
	{
	    CharacterAnimSpeedModifier modifier;
	    AbilityEventObj_AnimSpeed EventObj => (AbilityEventObj_AnimSpeed)_EventObj;
	    
	    public AbilityEventEffect_AnimSpeed(AbilityEventObj Obj) : base(Obj)
	    {
	        _EventObj = Obj;
	    }
	    
	    public override void StartEffect()
	    {
	        base.StartEffect();
	        
	        // Get initial speed value
	        float initialSpeed = EventObj.UseCurveLerp ? 
	            EventObj.SpeedCurve.StartValue : EventObj.Speed;
	            
	        modifier = _combatController._animSpeedExecutor.AddAnimSpeedModifier(initialSpeed);
	    }
	    
	    public override void EffectRunning(float currentTimePercentage)
	    {
	        base.EffectRunning(currentTimePercentage);
	        
	        if (modifier != null)
	        {
	            float currentSpeed;
	            
	            if (EventObj.UseCurveLerp)
	            {
	                // Use curve to interpolate speed
	                currentSpeed = EventObj.SpeedCurve.GetCurveValue(
	                    eve.GetEventStartTime(), 
	                    eve.GetEventEndTime(), 
	                    currentTimePercentage);
	            }
	            else
	            {
	                // Use constant speed
	                currentSpeed = EventObj.Speed;
	            }
	            
	            // Update the modifier's speed
	            modifier.SpeedScale = currentSpeed;
	        }
	    }
	    
	    public override void EndEffect()
	    {
	        base.EndEffect();
	        if (modifier != null)
	        {
	            _combatController._animSpeedExecutor.RemoveAnimSpeedModifier(modifier);
	        }
	    }
	}
}

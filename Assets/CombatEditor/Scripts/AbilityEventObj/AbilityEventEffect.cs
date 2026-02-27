using CombatEditor;
using UnityEngine;

public class AbilityEventEffect
{
    public AbilityEvent eve;
    public AbilityEventObj _EventObj;
    public CombatController _combatController;
    public AbilityScriptableObject AnimObj;
    public bool IsRunning;
    float _LastNormalizedTime;
    
    // 精确顿帧相关变量
    protected float _targetNormalizedTime = -1f; // 用于精确顿帧的目标时间点
    protected bool _preciseTiming = false; // 是否需要精确时机检测
    protected bool _frameAdjustmentRequired = false; // 是否需要调整到精确帧
    
    // 回调方法，用于通知需要重置动画位置
    public delegate void FrameAdjustmentCallback(float targetTime);
    public FrameAdjustmentCallback OnFrameAdjustmentNeeded;
    
    public AbilityEventEffect(AbilityEventObj InitObj)
    {
        _EventObj = InitObj;
    }

    public virtual void StartEffect()
    {
        IsRunning = true;
        FetchCurrentValues();
    }

    public virtual void EndEffect()
    {
        IsRunning = false;
    }

    public virtual void EffectRunning() { }
    public virtual void EffectRunning(float percentage) { }
    public virtual void EffectRunningFixedUpdate(float percentage) { }
    
    // 用于启用精确顿帧检测和回调
    public virtual void EnablePreciseTiming(float normalizedTargetTime, FrameAdjustmentCallback callback = null)
    {
        _preciseTiming = true;
        _targetNormalizedTime = normalizedTargetTime;
        _frameAdjustmentRequired = false;
        
        // 注册回调
        if (callback != null)
        {
            OnFrameAdjustmentNeeded = callback;
        }
    }
    
    // 检查是否达到精确时间点，并处理跳帧情况
    public virtual bool HasReachedPreciseTime(float currentNormalizedTime)
    {
        if (!_preciseTiming || _targetNormalizedTime < 0)
            return false;
        
        // 计算当前帧和上一帧之间的时间间隔
        float deltaTime = Mathf.Abs(currentNormalizedTime - _LastNormalizedTime);
        
        // 判断是否达到或越过目标时间点
        bool crossed = false;
        bool needsAdjustment = false;
        
        // 常规检测：如果从上一帧到当前帧跨过了目标时间点
        if ((_LastNormalizedTime <= _targetNormalizedTime && currentNormalizedTime >= _targetNormalizedTime) ||
            // 处理循环情况
            (_LastNormalizedTime > currentNormalizedTime && 
            (_LastNormalizedTime > _targetNormalizedTime || currentNormalizedTime > _targetNormalizedTime)))
        {
            crossed = true;
            
            // 检查是否需要精确调整
            float distanceFromTarget = Mathf.Abs(currentNormalizedTime - _targetNormalizedTime);
            if (distanceFromTarget > 0.01f) // 如果距离目标帧超过1%，则需要调整
            {
                needsAdjustment = true;
            }
        }
        
        // 高速预测检测：如果时间间隔过大，检查是否可能跳过了目标帧
        else if (deltaTime > 0.05f) 
        {
            // 检查目标时间点是否在当前区间内
            float minTime = Mathf.Min(_LastNormalizedTime, currentNormalizedTime);
            float maxTime = Mathf.Max(_LastNormalizedTime, currentNormalizedTime);
            
            // 处理循环情况(当时间从接近1跳到接近0时)
            if (_LastNormalizedTime > currentNormalizedTime && maxTime - minTime > 0.5f)
            {
                // 在循环情况下检查：时间点是否处于[minTime,1]或[0,maxTime]区间
                if ((_targetNormalizedTime >= minTime && _targetNormalizedTime <= 1.0f) ||
                    (_targetNormalizedTime >= 0.0f && _targetNormalizedTime <= maxTime))
                {
                    crossed = true;
                    needsAdjustment = true; // 循环情况下一定需要调整
                }
            }
            // 常规情况
            else if (_targetNormalizedTime >= minTime && _targetNormalizedTime <= maxTime)
            {
                crossed = true;
                needsAdjustment = true; // 跨帧过大时一定需要调整
            }
        }
        
        // 如果需要精确调整且回调已注册，则通知需要调整
        if (crossed && needsAdjustment && !_frameAdjustmentRequired)
        {
            _frameAdjustmentRequired = true;
            
            // 调用回调通知需要调整帧
            if (OnFrameAdjustmentNeeded != null)
            {
                OnFrameAdjustmentNeeded(_targetNormalizedTime);
            }
        }
        
        // 更新上一帧时间
        _LastNormalizedTime = currentNormalizedTime;
        return crossed;
    }
    
    // 重置帧调整状态，在完成帧调整后调用
    public void FrameAdjustmentComplete()
    {
        _frameAdjustmentRequired = false;
    }

    public void FetchCurrentValues()
    {
        if (_combatController != null && eve != null && eve.GetEventTimeType() != AbilityEventObj.EventTimeType.Null)
        {
            // 修复：不要尝试将AbilityEventObj转换为AbilityScriptableObject
            // AnimObj应该由调用者传入
            
            if (AnimObj != null)
            {
                _LastNormalizedTime = 0f;
            }
        }
    }
} 
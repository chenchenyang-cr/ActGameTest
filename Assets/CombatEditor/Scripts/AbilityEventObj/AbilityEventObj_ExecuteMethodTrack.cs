using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace CombatEditor
{
    [AbilityEvent]
    [CreateAssetMenu(menuName = "AbilityEvents / Execute Method Track")]
    public class AbilityEventObj_ExecuteMethodTrack : AbilityEventObj_ExecuteMethod
    {
        [Header("轨道执行设置")]
        [Tooltip("每帧执行间隔（秒），0表示每帧都执行")]
        public float executionInterval = 0f;
        
        [Tooltip("是否在轨道开始时执行一次")]
        public bool executeOnStart = true;
        
        [Tooltip("是否在轨道结束时执行一次")]
        public bool executeOnEnd = false;

        public override EventTimeType GetEventTimeType()
        {
            return EventTimeType.EventRange;
        }

        public override AbilityEventEffect Initialize()
        {
            return new AbilityEventEffect_ExecuteMethodTrack(this);
        }

#if UNITY_EDITOR
        public override AbilityEventPreview InitializePreview()
        {
            return new AbilityEventPreview_ExecuteMethodTrack(this);
        }
#endif
    }

    public class AbilityEventEffect_ExecuteMethodTrack : AbilityEventEffect_ExecuteMethod
    {
        AbilityEventObj_ExecuteMethodTrack TrackEventObj => (AbilityEventObj_ExecuteMethodTrack)_EventObj;
        
        private float _lastExecutionTime;
        private bool _hasExecutedOnStart;

        public AbilityEventEffect_ExecuteMethodTrack(AbilityEventObj InitObj) : base(InitObj)
        {
            _EventObj = InitObj;
        }

        public override void StartEffect()
        {
            // 调用基类AbilityEventEffect的StartEffect，但不调用ExecuteMethod的StartEffect
            // 因为我们要控制方法的执行时机
            if (_combatController == null || eve == null || AnimObj == null)
            {
                // 如果基础数据没有初始化，先初始化
                base.StartEffect();
                return;
            }
            
            // 初始化轨道状态
            _lastExecutionTime = 0f;
            _hasExecutedOnStart = false;
            
            // 在轨道开始时执行一次（如果启用）
            if (TrackEventObj.executeOnStart)
            {
                ExecuteMethodSafe();
                _hasExecutedOnStart = true;
            }
        }

        public override void EffectRunning(float CurrentTimePercentage)
        {
            base.EffectRunning(CurrentTimePercentage);
            
            // 计算当前轨道的实际时间
            float currentTrackTime = GetCurrentTrackTime(CurrentTimePercentage);
            
            // 检查是否需要执行
            bool shouldExecute = false;
            
            if (TrackEventObj.executionInterval <= 0f)
            {
                // 每帧都执行（除了开始帧，如果已经在开始时执行过）
                if (!_hasExecutedOnStart || currentTrackTime > 0f)
                {
                    shouldExecute = true;
                }
            }
            else
            {
                // 按间隔时间执行
                if (currentTrackTime - _lastExecutionTime >= TrackEventObj.executionInterval)
                {
                    shouldExecute = true;
                }
            }
            
            if (shouldExecute)
            {
                ExecuteMethodSafe();
                _lastExecutionTime = currentTrackTime;
            }
        }

        public override void EndEffect()
        {
            // 在轨道结束时执行一次（如果启用）
            if (TrackEventObj.executeOnEnd)
            {
                ExecuteMethodSafe();
            }
            
            base.EndEffect();
        }

        /// <summary>
        /// 安全地执行方法，包含错误处理
        /// </summary>
        private void ExecuteMethodSafe()
        {
            try
            {
                ExecuteMethod();
            }
            catch (Exception e)
            {
                Debug.LogError($"ExecuteMethodTrack执行出错: {e.Message}");
            }
        }

        /// <summary>
        /// 获取当前轨道的实际时间
        /// </summary>
        private float GetCurrentTrackTime(float timePercentage)
        {
            double startTime = eve.GetEventStartTime();
            double endTime = eve.GetEventEndTime();
            double duration = endTime - startTime;
            
            // 计算当前时间百分比在轨道范围内的实际时间
            double normalizedTime = (timePercentage - startTime) / duration;
            normalizedTime = Math.Max(0.0, Math.Min(1.0, normalizedTime));
            
            return (float)(normalizedTime * duration * AnimObj.Clip.length);
        }
    }

#if UNITY_EDITOR
    public class AbilityEventPreview_ExecuteMethodTrack : AbilityEventPreview_ExecuteMethod
    {
        AbilityEventObj_ExecuteMethodTrack TrackEventObj => (AbilityEventObj_ExecuteMethodTrack)_EventObj;

        public AbilityEventPreview_ExecuteMethodTrack(AbilityEventObj Obj) : base(Obj)
        {
            _EventObj = Obj;
        }

        public override void InitPreview()
        {
            base.InitPreview();
            
            string intervalText = TrackEventObj.executionInterval <= 0f ? "每帧" : $"每{TrackEventObj.executionInterval}秒";
            Debug.Log($"预览模式: 轨道执行 {TrackEventObj.targetClassName}.{TrackEventObj.methodName} - {intervalText}执行一次");
        }

        public override void PreviewRunning(float CurrentTimePercentage)
        {
            base.PreviewRunning(CurrentTimePercentage);
            // 预览模式下不执行实际方法，只在InitPreview中显示信息
        }
    }
#endif
} 
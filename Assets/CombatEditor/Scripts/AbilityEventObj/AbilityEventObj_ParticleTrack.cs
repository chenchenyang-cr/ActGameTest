using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace CombatEditor
{
    /// <summary>
    /// 粒子轨道速度控制模式
    /// </summary>
    public enum SpeedControlMode
    {
        [InspectorName("基础速度")]
        BaseSpeed,
        [InspectorName("曲线速度")]
        CurveSpeed
    }
    /// <summary>
    /// 粒子轨道数据配置
    /// </summary>
    [System.Serializable]
    public class ParticleTrackData
    {
        [Header("粒子对象设置")]
        [Tooltip("要创建的粒子系统预制体")]
        public GameObject particlePrefab;
        
        [Header("轨道播放控制")]
        [Tooltip("是否在轨道开始时立即播放")]
        public bool playOnStart = true;
        [Tooltip("是否在轨道结束时停止播放")]
        public bool stopOnEnd = true;
        [Tooltip("轨道结束后是否自动销毁（在粒子播放完毕后）")]
        public bool destroyWhenFinished = false;
        [Tooltip("轨道结束后延迟销毁时间（秒）")]
        public float destroyDelay = 0f;
        [Tooltip("是否在退出轨道时立即清除粒子")]
        public bool clearParticlesOnExit = false;
        
        [Header("轨道时间控制")]
        [Tooltip("速度控制模式选择")]
        public SpeedControlMode speedMode = SpeedControlMode.BaseSpeed;
        
        [Header("基础速度模式")]
        [Tooltip("粒子播放速度倍率")]
        [Range(0.1f, 5f)]
        public float playbackSpeed = 1f;
        
        [Header("曲线速度模式")]
        [Tooltip("速度变化曲线")]
        public TweenCurve speedCurve = new TweenCurve();
        
        [Header("通用设置")]
        [Tooltip("是否循环播放粒子")]
        public bool loopParticles = false;
        [Tooltip("轨道进度映射曲线")]
        public AnimationCurve progressCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
        
        [Header("位置绑定")]
        [Tooltip("绑定的节点类型")]
        public CharacterNode.NodeType targetNode = CharacterNode.NodeType.BodyCenter;
        [Tooltip("位置偏移")]
        public Vector3 positionOffset = Vector3.zero;
        [Tooltip("旋转偏移")]
        public Vector3 rotationOffset = Vector3.zero;
        [Tooltip("是否跟随节点移动")]
        public bool followNodeMovement = true;
        [Tooltip("是否跟随节点旋转")]
        public bool followNodeRotation = false;
        
        [Header("高级设置")]
        [Tooltip("使用自定义目标对象")]
        public bool useCustomTarget = false;
        [Tooltip("自定义目标对象名称")]
        public string customTargetName = "";
        [Tooltip("自定义父级变换")]
        public Transform customParent;
        
        [Header("粒子强度控制")]
        [Tooltip("是否使用强度曲线控制")]
        public bool useIntensityCurve = false;
        [Tooltip("粒子强度曲线（控制emission rate）")]
        public AnimationCurve intensityCurve = AnimationCurve.Constant(0f, 1f, 1f);
        [Tooltip("最大强度倍率")]
        [Range(0f, 10f)]
        public float maxIntensityMultiplier = 1f;
    }

    /// <summary>
    /// 粒子轨道事件 - 提供完整的轨道控制功能
    /// </summary>
    [AbilityEvent]
    [CreateAssetMenu(menuName = "AbilityEvents / ParticleTrack")]
    public class AbilityEventObj_ParticleTrack : AbilityEventObj
    {
        [Header("粒子轨道配置")]
        public ParticleTrackData particleData = new ParticleTrackData();
        
        [Header("调试信息")]
        [Tooltip("显示详细的调试日志")]
        public bool enableDebugLog = false;
        
        public override EventTimeType GetEventTimeType()
        {
            return EventTimeType.EventRange;
        }
        
        public override AbilityEventEffect Initialize()
        {
            return new AbilityEventEffect_ParticleTrack(this);
        }
        
#if UNITY_EDITOR
        public override AbilityEventPreview InitializePreview()
        {
            return new AbilityEventPreview_ParticleTrack(this);
        }
#endif
    }
    
    /// <summary>
    /// 粒子轨道运行时效果
    /// </summary>
    public class AbilityEventEffect_ParticleTrack : AbilityEventEffect
    {
        private AbilityEventObj_ParticleTrack EventObj => (AbilityEventObj_ParticleTrack)_EventObj;
        
        private GameObject particleInstance;
        private ParticleSystem[] particleSystems;
        private ParticleSystem.EmissionModule[] emissionModules;
        private float[] originalEmissionRates;
        private Transform targetTransform;
        private NodeFollower nodeFollower;
        private float trackDuration;
        private float trackStartTime;
        private bool isTrackActive = false;
        
        public AbilityEventEffect_ParticleTrack(AbilityEventObj obj) : base(obj)
        {
            _EventObj = obj;
        }
        
        public override void StartEffect()
        {
            base.StartEffect();
            
            if (EventObj.enableDebugLog)
                Debug.Log($"[ParticleTrack] 开始轨道效果: {_EventObj.name}");
            
            CreateParticleInstance();
            SetupParticleSystems();
            SetupNodeBinding();
            CalculateTrackTiming();
            
            isTrackActive = true;
            
            if (EventObj.particleData.playOnStart)
            {
                PlayParticles();
            }
        }
        
        public override void EffectRunning(float normalizedTime)
        {
            base.EffectRunning(normalizedTime);
            
            if (!isTrackActive || particleInstance == null) return;
            
            UpdateParticleSpeed(normalizedTime);
            UpdateParticleIntensity(normalizedTime);
            UpdateNodeFollowing();
        }
        
        public override void EndEffect()
        {
            if (EventObj.enableDebugLog)
                Debug.Log($"[ParticleTrack] 结束轨道效果: {_EventObj.name}");
            
            isTrackActive = false;
            
            if (EventObj.particleData.stopOnEnd)
            {
                StopParticles();
            }
            
            if (EventObj.particleData.clearParticlesOnExit)
            {
                ClearParticles();
            }
            
            // 延迟销毁或立即销毁
            if (particleInstance != null)
            {
                if (EventObj.particleData.destroyWhenFinished)
                {
                    // 添加自动销毁组件，让粒子播放完毕后自动销毁
                    particleInstance.AddComponent<ParticleSystemAutoDestroy>();
                }
                else if (EventObj.particleData.destroyDelay > 0)
                {
                    Object.Destroy(particleInstance, EventObj.particleData.destroyDelay);
                }
                else
                {
                    Object.Destroy(particleInstance);
                }
            }
            
            base.EndEffect();
        }
        
        private void CreateParticleInstance()
        {
            if (EventObj.particleData.particlePrefab == null)
            {
                Debug.LogWarning($"[ParticleTrack] 粒子预制体为空: {_EventObj.name}");
                return;
            }
            
            particleInstance = Object.Instantiate(EventObj.particleData.particlePrefab);
            particleInstance.name = $"ParticleTrack_{_EventObj.name}";
            
            // 设置父级
            if (EventObj.particleData.customParent != null)
            {
                particleInstance.transform.SetParent(EventObj.particleData.customParent);
            }
            else if (_combatController != null)
            {
                particleInstance.transform.SetParent(_combatController.transform);
            }
        }
        
        private void SetupParticleSystems()
        {
            if (particleInstance == null) return;
            
            particleSystems = particleInstance.GetComponentsInChildren<ParticleSystem>();
            emissionModules = new ParticleSystem.EmissionModule[particleSystems.Length];
            originalEmissionRates = new float[particleSystems.Length];
            
            for (int i = 0; i < particleSystems.Length; i++)
            {
                emissionModules[i] = particleSystems[i].emission;
                originalEmissionRates[i] = emissionModules[i].rateOverTime.constant;
                
                // 根据速度控制模式设置初始播放速度
                var main = particleSystems[i].main;
                float initialSpeed = GetCurrentSpeed(0f); // 获取初始速度
                main.simulationSpeed = initialSpeed;
                
                // 设置循环
                main.loop = EventObj.particleData.loopParticles;
                
                // 初始时停止播放
                particleSystems[i].Stop();
            }
        }
        
        private void SetupNodeBinding()
        {
            if (particleInstance == null || _combatController == null) return;
            
            // 确定目标变换
            if (EventObj.particleData.useCustomTarget && !string.IsNullOrEmpty(EventObj.particleData.customTargetName))
            {
                GameObject customTarget = GameObject.Find(EventObj.particleData.customTargetName);
                targetTransform = customTarget?.transform;
            }
            else
            {
                targetTransform = _combatController.GetNodeTranform(EventObj.particleData.targetNode);
            }
            
            if (targetTransform != null && EventObj.particleData.followNodeMovement)
            {
                nodeFollower = particleInstance.AddComponent<NodeFollower>();
                nodeFollower.Init(
                    targetTransform,
                    EventObj.particleData.positionOffset,
                    Quaternion.Euler(EventObj.particleData.rotationOffset),
                    EventObj.particleData.followNodeMovement,
                    EventObj.particleData.followNodeRotation,
                    _combatController
                );
            }
            else
            {
                // 静态位置设置
                if (targetTransform != null)
                {
                    particleInstance.transform.position = targetTransform.position + EventObj.particleData.positionOffset;
                    particleInstance.transform.rotation = targetTransform.rotation * Quaternion.Euler(EventObj.particleData.rotationOffset);
                }
            }
        }
        
        private void CalculateTrackTiming()
        {
            if (eve != null && AnimObj?.Clip != null)
            {
                trackStartTime = eve.GetEventStartTime() * AnimObj.Clip.length;
                trackDuration = (eve.GetEventEndTime() - eve.GetEventStartTime()) * AnimObj.Clip.length;
            }
        }
        
        private void PlayParticles()
        {
            if (particleSystems == null) return;
            
            foreach (var ps in particleSystems)
            {
                if (ps != null)
                {
                    ps.Play();
                }
            }
            
            if (EventObj.enableDebugLog)
                Debug.Log($"[ParticleTrack] 播放粒子: {particleSystems.Length} 个粒子系统");
        }
        
        private void StopParticles()
        {
            if (particleSystems == null) return;
            
            foreach (var ps in particleSystems)
            {
                if (ps != null)
                {
                    ps.Stop();
                }
            }
        }
        
        private void ClearParticles()
        {
            if (particleSystems == null) return;
            
            foreach (var ps in particleSystems)
            {
                if (ps != null)
                {
                    ps.Clear();
                }
            }
        }
        
        private void UpdateParticleIntensity(float normalizedTime)
        {
            if (!EventObj.particleData.useIntensityCurve || emissionModules == null) return;
            
            float curveTime = EventObj.particleData.progressCurve.Evaluate(normalizedTime);
            float intensity = EventObj.particleData.intensityCurve.Evaluate(curveTime);
            intensity *= EventObj.particleData.maxIntensityMultiplier;
            
            for (int i = 0; i < emissionModules.Length; i++)
            {
                var emission = emissionModules[i];
                emission.rateOverTime = originalEmissionRates[i] * intensity;
            }
        }
        
        private void UpdateNodeFollowing()
        {
            // NodeFollower 组件会自动处理跟随逻辑
            // 这里可以添加额外的更新逻辑
        }
        
        /// <summary>
        /// 根据当前模式和时间百分比获取当前速度
        /// </summary>
        private float GetCurrentSpeed(float normalizedTime)
        {
            switch (EventObj.particleData.speedMode)
            {
                case SpeedControlMode.BaseSpeed:
                    return EventObj.particleData.playbackSpeed;
                    
                case SpeedControlMode.CurveSpeed:
                    if (eve != null && AnimObj?.Clip != null)
                    {
                        return EventObj.particleData.speedCurve.GetCurveValue(
                            eve.GetEventStartTime(),
                            eve.GetEventEndTime(),
                            normalizedTime);
                    }
                    return 1f; // 默认速度
                    
                default:
                    return 1f;
            }
        }
        
        /// <summary>
        /// 更新粒子播放速度
        /// </summary>
        private void UpdateParticleSpeed(float normalizedTime)
        {
            if (particleSystems == null) return;
            
            float currentSpeed = GetCurrentSpeed(normalizedTime);
            
            // 防止速度为0或负数导致粒子系统停止工作
            if (currentSpeed <= 0f)
            {
                if (EventObj.enableDebugLog)
                    Debug.LogWarning($"[ParticleTrack] 粒子速度为 {currentSpeed}，将使用最小速度 0.01f");
                currentSpeed = 0.01f; // 使用一个很小的正数而不是0
            }
            
            foreach (var ps in particleSystems)
            {
                if (ps != null)
                {
                    var main = ps.main;
                    main.simulationSpeed = currentSpeed;
                }
            }
            
            if (EventObj.enableDebugLog && EventObj.particleData.speedMode == SpeedControlMode.CurveSpeed)
            {
                Debug.Log($"[ParticleTrack] 曲线速度更新: {currentSpeed:F2} (时间: {normalizedTime:F2})");
            }
        }
    }
} 
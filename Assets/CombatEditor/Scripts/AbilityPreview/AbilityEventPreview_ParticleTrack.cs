using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace CombatEditor
{
#if UNITY_EDITOR
    /// <summary>
    /// 粒子轨道编辑器预览类
    /// 实现精确的轨道控制和实时预览效果
    /// </summary>
    public class AbilityEventPreview_ParticleTrack : AbilityEventPreview
    {
        private AbilityEventObj_ParticleTrack EventObj => (AbilityEventObj_ParticleTrack)_EventObj;
        
        // 预览对象管理
        private GameObject previewParticleInstance;
        private ParticleSystem[] previewParticleSystems;
        private ParticleSystem.EmissionModule[] previewEmissionModules;
        private float[] originalEmissionRates;
        private float[] originalSimulationSpeeds;
        
        // 轨道控制相关
        private Transform targetTransform;
        private NodeFollower nodeFollower;
        private bool isInTrackRange = false;
        private float lastSimulatedTime = -1f;
        private float trackStartTimeInSeconds;
        private float trackDurationInSeconds;
        
        // 预览状态管理
        private bool hasInitializedParticles = false;
        private Vector3 previewStartPosition;
        private Quaternion previewStartRotation;
        private float previewStartFrame = -1f;
        
        public AbilityEventPreview_ParticleTrack(AbilityEventObj obj) : base(obj)
        {
            _EventObj = obj;
        }
        
        public override void InitPreview()
        {
            base.InitPreview();
            
            if (EventObj.enableDebugLog)
                Debug.Log($"[ParticleTrack Preview] 初始化预览: {_EventObj.name}");
            
            CalculateTrackTiming();
            CreatePreviewParticleInstance();
            SetupPreviewParticleSystems();
            SetupPreviewNodeBinding();
            
            hasInitializedParticles = true;
            
            // 保存预览开始时的状态
            SavePreviewStartState();
        }
        
        public override void PreviewRunning(float currentTimePercentage)
        {
            base.PreviewRunning(currentTimePercentage);
            
            if (!hasInitializedParticles || previewParticleInstance == null) return;
            
            // 实时检查配置变化并更新
            CheckAndUpdateConfiguration();
            
            UpdateTrackRangeStatus(currentTimePercentage);
            UpdatePreviewTransform();
            UpdatePreviewParticleSpeed(currentTimePercentage);
            UpdateParticleSimulation(currentTimePercentage);
            UpdateParticleIntensity(currentTimePercentage);
        }
        
        public override void PreviewRunningInScale(float scaledPercentage)
        {
            base.PreviewRunningInScale(scaledPercentage);
            
            if (!hasInitializedParticles || previewParticleSystems == null) return;
            
            // 使用缩放后的时间来精确控制粒子系统
            SimulateParticlesWithScaledTime(scaledPercentage);
        }
        
        public override bool NeedStartFrameValue()
        {
            return true;
        }
        
        public override void GetStartFrameDataBeforePreview()
        {
            base.GetStartFrameDataBeforePreview();
            SavePreviewStartState();
        }
        
        public override void DestroyPreview()
        {
            if (EventObj.enableDebugLog)
                Debug.Log($"[ParticleTrack Preview] 销毁预览: {_EventObj.name}");
            
            if (previewParticleInstance != null)
            {
                Object.DestroyImmediate(previewParticleInstance);
                previewParticleInstance = null;
            }
            
            hasInitializedParticles = false;
            base.DestroyPreview();
        }
        
        public override void BackToStart()
        {
            base.BackToStart();
            
            if (previewParticleSystems != null)
            {
                foreach (var ps in previewParticleSystems)
                {
                    if (ps != null)
                    {
                        ps.Stop();
                        ps.Clear();
                        ps.Simulate(0f, true, true);
                    }
                }
            }
            
            lastSimulatedTime = -1f;
            isInTrackRange = false;
        }
        
        /// <summary>
        /// 计算轨道时间相关参数
        /// </summary>
        private void CalculateTrackTiming()
        {
            if (eve != null && AnimObj?.Clip != null)
            {
                trackStartTimeInSeconds = StartTimePercentage * AnimObj.Clip.length;
                trackDurationInSeconds = (EndTimePercentage - StartTimePercentage) * AnimObj.Clip.length;
            }
        }
        
        /// <summary>
        /// 创建预览用的粒子实例
        /// </summary>
        private void CreatePreviewParticleInstance()
        {
            if (EventObj.particleData.particlePrefab == null)
            {
                Debug.LogWarning($"[ParticleTrack Preview] 粒子预制体为空: {_EventObj.name}");
                return;
            }
            
            previewParticleInstance = Object.Instantiate(EventObj.particleData.particlePrefab);
            previewParticleInstance.name = $"Preview_ParticleTrack_{_EventObj.name}";
            previewParticleInstance.hideFlags = HideFlags.DontSaveInEditor;
            
            // 设置父级到预览组
            if (previewGroup != null)
            {
                previewParticleInstance.transform.SetParent(previewGroup.transform);
            }
        }
        
        /// <summary>
        /// 设置预览粒子系统
        /// </summary>
        private void SetupPreviewParticleSystems()
        {
            if (previewParticleInstance == null) return;
            
            previewParticleSystems = previewParticleInstance.GetComponentsInChildren<ParticleSystem>();
            previewEmissionModules = new ParticleSystem.EmissionModule[previewParticleSystems.Length];
            originalEmissionRates = new float[previewParticleSystems.Length];
            originalSimulationSpeeds = new float[previewParticleSystems.Length];
            
            for (int i = 0; i < previewParticleSystems.Length; i++)
            {
                var ps = previewParticleSystems[i];
                if (ps == null) continue;
                
                // 保存原始设置
                previewEmissionModules[i] = ps.emission;
                originalEmissionRates[i] = previewEmissionModules[i].rateOverTime.constant;
                originalSimulationSpeeds[i] = ps.main.simulationSpeed;
                
                // 配置预览模式设置
                var main = ps.main;
                main.simulationSpeed = GetPreviewSpeed(0f); // 获取初始预览速度
                main.loop = EventObj.particleData.loopParticles;
                main.playOnAwake = false;
                
                // 禁用自动随机种子以获得一致的预览效果
                ps.useAutoRandomSeed = false;
                ps.randomSeed = 1;
                
                // 初始时停止
                ps.Stop();
                ps.Clear();
            }
        }
        
        /// <summary>
        /// 设置预览节点绑定
        /// </summary>
        private void SetupPreviewNodeBinding()
        {
            if (previewParticleInstance == null || _combatController == null) return;
            
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
                nodeFollower = previewParticleInstance.AddComponent<NodeFollower>();
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
                UpdateStaticTransform();
            }
        }
        
        /// <summary>
        /// 更新轨道范围状态
        /// </summary>
        private void UpdateTrackRangeStatus(float currentTimePercentage)
        {
            bool wasInRange = isInTrackRange;
            isInTrackRange = PreviewInRange(currentTimePercentage);
            
            // 进入轨道范围
            if (isInTrackRange && !wasInRange)
            {
                OnEnterTrackRange();
            }
            // 退出轨道范围
            else if (!isInTrackRange && wasInRange)
            {
                OnExitTrackRange();
            }
        }
        
        /// <summary>
        /// 进入轨道范围时的处理
        /// </summary>
        private void OnEnterTrackRange()
        {
            if (EventObj.enableDebugLog)
                Debug.Log($"[ParticleTrack Preview] 进入轨道范围: {_EventObj.name}");
            
            if (EventObj.particleData.playOnStart && previewParticleSystems != null)
            {
                foreach (var ps in previewParticleSystems)
                {
                    if (ps != null)
                    {
                        ps.Play();
                    }
                }
            }
        }
        
        /// <summary>
        /// 退出轨道范围时的处理
        /// </summary>
        private void OnExitTrackRange()
        {
            if (EventObj.enableDebugLog)
                Debug.Log($"[ParticleTrack Preview] 退出轨道范围: {_EventObj.name}");
            
            if (EventObj.particleData.stopOnEnd && previewParticleSystems != null)
            {
                foreach (var ps in previewParticleSystems)
                {
                    if (ps != null)
                    {
                        ps.Stop();
                    }
                }
            }
            
            if (EventObj.particleData.clearParticlesOnExit && previewParticleSystems != null)
            {
                foreach (var ps in previewParticleSystems)
                {
                    if (ps != null)
                    {
                        ps.Clear();
                    }
                }
            }
        }
        
        /// <summary>
        /// 检查配置变化并实时更新预览
        /// </summary>
        private void CheckAndUpdateConfiguration()
        {
            // 检查节点绑定是否发生变化
            Transform newTargetTransform = null;
            if (EventObj.particleData.useCustomTarget && !string.IsNullOrEmpty(EventObj.particleData.customTargetName))
            {
                GameObject customTarget = GameObject.Find(EventObj.particleData.customTargetName);
                newTargetTransform = customTarget?.transform;
            }
            else
            {
                newTargetTransform = _combatController?.GetNodeTranform(EventObj.particleData.targetNode);
            }
            
            // 如果目标变换发生变化，更新绑定
            if (newTargetTransform != targetTransform)
            {
                targetTransform = newTargetTransform;
                UpdateNodeBinding();
            }
            
            // 更新NodeFollower配置（如果存在）
            if (nodeFollower != null)
            {
                UpdateNodeFollowerConfiguration();
            }
            
            // 实时更新粒子系统配置
            UpdateParticleSystemConfiguration();
        }
        
        /// <summary>
        /// 更新NodeFollower的配置
        /// </summary>
        private void UpdateNodeFollowerConfiguration()
        {
            if (nodeFollower == null) return;
            
            // 重新初始化NodeFollower以应用最新设置
            nodeFollower.Init(
                targetTransform,
                EventObj.particleData.positionOffset,
                Quaternion.Euler(EventObj.particleData.rotationOffset),
                EventObj.particleData.followNodeMovement,
                EventObj.particleData.followNodeRotation,
                _combatController
            );
        }
        
        /// <summary>
        /// 更新粒子系统配置
        /// </summary>
        private void UpdateParticleSystemConfiguration()
        {
            if (previewParticleSystems == null) return;
            
            for (int i = 0; i < previewParticleSystems.Length; i++)
            {
                var ps = previewParticleSystems[i];
                if (ps == null) continue;
                
                // 更新播放速度 - 使用当前的预览时间
                var main = ps.main;
                float currentSpeed = GetPreviewSpeed(0f); // 配置更新时使用初始速度
                main.simulationSpeed = currentSpeed;
                main.loop = EventObj.particleData.loopParticles;
            }
        }
        
        /// <summary>
        /// 更新节点绑定
        /// </summary>
        private void UpdateNodeBinding()
        {
            if (previewParticleInstance == null) return;
            
            // 销毁旧的NodeFollower
            if (nodeFollower != null)
            {
                Object.DestroyImmediate(nodeFollower);
                nodeFollower = null;
            }
            
            // 重新设置绑定
            if (targetTransform != null && EventObj.particleData.followNodeMovement)
            {
                nodeFollower = previewParticleInstance.AddComponent<NodeFollower>();
                nodeFollower.Init(
                    targetTransform,
                    EventObj.particleData.positionOffset,
                    Quaternion.Euler(EventObj.particleData.rotationOffset),
                    EventObj.particleData.followNodeMovement,
                    EventObj.particleData.followNodeRotation,
                    _combatController
                );
            }
        }
        
        /// <summary>
        /// 更新预览变换
        /// </summary>
        private void UpdatePreviewTransform()
        {
            if (nodeFollower == null && targetTransform != null)
            {
                UpdateStaticTransform();
            }
        }
        
        /// <summary>
        /// 更新静态变换
        /// </summary>
        private void UpdateStaticTransform()
        {
            if (previewParticleInstance == null || targetTransform == null) return;
            
            // 实时应用位置偏移
            Vector3 worldOffset = targetTransform.rotation * EventObj.particleData.positionOffset;
            previewParticleInstance.transform.position = targetTransform.position + worldOffset;
            
            // 实时应用旋转
            if (EventObj.particleData.followNodeRotation)
            {
                previewParticleInstance.transform.rotation = targetTransform.rotation * Quaternion.Euler(EventObj.particleData.rotationOffset);
            }
            else
            {
                // 如果不跟随节点旋转，只应用旋转偏移
                previewParticleInstance.transform.rotation = Quaternion.Euler(EventObj.particleData.rotationOffset);
            }
        }
        
        /// <summary>
        /// 更新粒子模拟
        /// </summary>
        private void UpdateParticleSimulation(float currentTimePercentage)
        {
            if (!isInTrackRange || previewParticleSystems == null) return;
            
            // 计算轨道内的相对时间
            float normalizedTrackTime = (currentTimePercentage - StartTimePercentage) / (EndTimePercentage - StartTimePercentage);
            normalizedTrackTime = Mathf.Clamp01(normalizedTrackTime);
            
            // 应用进度曲线
            float curveTime = EventObj.particleData.progressCurve.Evaluate(normalizedTrackTime);
            float simulationTime = curveTime * trackDurationInSeconds;
            
            // 避免重复模拟相同时间
            if (Mathf.Abs(simulationTime - lastSimulatedTime) > 0.001f)
            {
                foreach (var ps in previewParticleSystems)
                {
                    if (ps != null)
                    {
                        ps.Simulate(simulationTime, true, true);
                    }
                }
                
                lastSimulatedTime = simulationTime;
            }
        }
        
        /// <summary>
        /// 使用缩放时间精确模拟粒子
        /// </summary>
        private void SimulateParticlesWithScaledTime(float scaledPercentage)
        {
            if (!isInTrackRange || previewParticleSystems == null) return;
            
            // 计算实际的模拟时间
            float simulationTime = (scaledPercentage - StartTimeScaledPercentage) * AnimLength;
            simulationTime = Mathf.Max(0f, simulationTime);
            
            // 需要额外的启动时间来确保粒子系统正确初始化
            float particleStartupTime = 1f / 60f; // 1帧的时间
            float totalSimulationTime = particleStartupTime + simulationTime;
            
            foreach (var ps in previewParticleSystems)
            {
                if (ps != null)
                {
                    ps.Simulate(totalSimulationTime, true, true);
                }
            }
            
            // 强制刷新Scene视图
            SceneView.RepaintAll();
        }
        
        /// <summary>
        /// 更新粒子强度
        /// </summary>
        private void UpdateParticleIntensity(float currentTimePercentage)
        {
            if (!isInTrackRange || previewEmissionModules == null) return;
            
            if (EventObj.particleData.useIntensityCurve)
            {
                float normalizedTrackTime = (currentTimePercentage - StartTimePercentage) / (EndTimePercentage - StartTimePercentage);
                normalizedTrackTime = Mathf.Clamp01(normalizedTrackTime);
                
                float curveTime = EventObj.particleData.progressCurve.Evaluate(normalizedTrackTime);
                float intensity = EventObj.particleData.intensityCurve.Evaluate(curveTime);
                intensity *= EventObj.particleData.maxIntensityMultiplier;
                
                for (int i = 0; i < previewEmissionModules.Length; i++)
                {
                    var emission = previewEmissionModules[i];
                    emission.rateOverTime = originalEmissionRates[i] * intensity;
                }
            }
            else
            {
                // 没有启用强度曲线时，确保使用原始发射率
                for (int i = 0; i < previewEmissionModules.Length; i++)
                {
                    var emission = previewEmissionModules[i];
                    emission.rateOverTime = originalEmissionRates[i];
                }
            }
        }
        
        /// <summary>
        /// 保存预览开始状态
        /// </summary>
        private void SavePreviewStartState()
        {
            if (_combatController != null)
            {
                previewStartPosition = _combatController.transform.position;
                previewStartRotation = _combatController.transform.rotation;
            }
        }
        
        /// <summary>
        /// 检查是否在预览范围内
        /// </summary>
        private bool PreviewInRange(float currentTimePercentage)
        {
            return currentTimePercentage >= StartTimePercentage && currentTimePercentage <= EndTimePercentage;
        }
        
        /// <summary>
        /// 根据当前模式和时间百分比获取预览速度
        /// </summary>
        private float GetPreviewSpeed(float currentTimePercentage)
        {
            switch (EventObj.particleData.speedMode)
            {
                case SpeedControlMode.BaseSpeed:
                    return EventObj.particleData.playbackSpeed;
                    
                case SpeedControlMode.CurveSpeed:
                    if (eve != null && AnimObj?.Clip != null)
                    {
                        return EventObj.particleData.speedCurve.GetCurveValue(
                            StartTimePercentage,
                            EndTimePercentage,
                            currentTimePercentage);
                    }
                    return 1f; // 默认速度
                    
                default:
                    return 1f;
            }
        }
        
        /// <summary>
        /// 更新预览粒子播放速度
        /// </summary>
        private void UpdatePreviewParticleSpeed(float currentTimePercentage)
        {
            if (!isInTrackRange || previewParticleSystems == null) return;
            
            float currentSpeed = GetPreviewSpeed(currentTimePercentage);
            
            // 防止速度为0或负数导致粒子系统停止工作
            if (currentSpeed <= 0f)
            {
                if (EventObj.enableDebugLog)
                    Debug.LogWarning($"[ParticleTrack Preview] 预览粒子速度为 {currentSpeed}，将使用最小速度 0.01f");
                currentSpeed = 0.01f; // 使用一个很小的正数而不是0
            }
            
            foreach (var ps in previewParticleSystems)
            {
                if (ps != null)
                {
                    var main = ps.main;
                    main.simulationSpeed = currentSpeed;
                }
            }
            
            if (EventObj.enableDebugLog && EventObj.particleData.speedMode == SpeedControlMode.CurveSpeed)
            {
                Debug.Log($"[ParticleTrack Preview] 曲线速度更新: {currentSpeed:F2} (时间: {currentTimePercentage:F2})");
            }
        }
    }
#endif
} 
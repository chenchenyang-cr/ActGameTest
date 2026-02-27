using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace CombatEditor
{

    public class RecordedClip2Time
    {
        public AnimationClip _clip;
        public double time;
        public RecordedClip2Time(AnimationClip clip,double time)
        {
            _clip = clip;
            this.time = time;
        }
    }

    [System.Serializable]
	public class CombatGroup
	{
	    public bool IsFolded;
	    public string Label;
	    public List<AbilityScriptableObject> CombatObjs;
	    public List<AbilityObjWithEffect> eves = new List<AbilityObjWithEffect>();
	}
	public class AbilityObjWithEffect
	{
	    public AbilityScriptableObject Obj;
	    public int Index;
	    public List<AbilityEventEffect> EventEffects = new List<AbilityEventEffect>();
	}
	
	[System.Serializable]
	public class CharacterNode
	{
	    public enum NodeType { Animator, BottomCenter, BodyCenter, Head, Spine, Hand, RHand, LHand, Foot ,LFoot, RFoot, Weapon , WeaponBase, WeaponTip}
	    public NodeType type;
	    public Transform NodeTrans;
	}
	
	
	public class AbilityEventWithEffects
	{
	    public AbilityEvent eve;
	    public AbilityEventEffect effect;
	}



    public class CombatController : MonoBehaviour
    {
        public Rigidbody _rigidbody;
        public Animator _animator;
        public AbilityScriptableObject SelectedAbility;
        public List<CombatGroup> CombatDatas = new List<CombatGroup>();
        public AnimationClip clip;

        CombatEventReceiver receiver;
        
        // 动画循环检测用的时间记录
        private Dictionary<int, float> lastNormalizedTimes = new Dictionary<int, float>();

        public AnimSpeedExecutor _animSpeedExecutor;
        public MoveExecutor _moveExecutor;


        public List<CharacterNode> Nodes = new List<CharacterNode>();

        public List<RecordedClip2Time> _recordedSelfTransClips = new List<RecordedClip2Time>();

        public Dictionary<int, List<AbilityEventWithEffects>> ClipID_To_EventEffects;

        public List<AbilityEventEffect_States> RunningStates = new List<AbilityEventEffect_States>();
        
        // 是否执行动画速度控制器
        private bool _executeAnimSpeedController = true;

        // 用于条件检查的相关属性
        [Header("条件检查相关")]
        public List<string> currentBuffs = new List<string>(); // 当前拥有的buff列表
        public float currentHealth = 100f;                    // 当前生命值
        public float maxHealth = 100f;                        // 最大生命值
        public bool isInAir = false;                          // 是否在空中
        public Transform currentTarget;                       // 当前目标
        public bool hasHitTarget = false;                     // 是否击中目标
        public bool hasBeenHit = false;                       // 是否被击中
        public bool isInHitStop = false;                      // 是否在顿帧中
        public bool isHitChecked = false;                     // 是否判定受击
        
        [Header("攻击阶段设置")]
        [Tooltip("最大攻击阶段数量")]
        [Range(5, 100)]
        public int maxAttackPhases = 30;                      // 最大攻击阶段数量
        public int attackPhase = 0;                           // 当前攻击阶段
        
        [Header("连击系统")]
        [Tooltip("当前连击数")]
        public int comboCount = 0;                            // 当前连击数
        [Tooltip("连击重置时间")]
        public float comboResetTime = 2f;                     // 连击重置时间
        private float lastComboTime = 0f;                     // 上次连击时间
        private float originalAnimationSpeed = 1f;            // 原始动画速度
        private float hitStopTimer = 0f;                      // 顿帧计时器
        private int hitStopFrames = 0;                        // 顿帧持续帧数
        private AbilityEventObj _currentHitStopEventObj;      // 当前触发顿帧的事件对象
        private bool _pendingFrameAdjustment = false;         // 是否有待处理的帧调整
        private float _targetFramePosition = 0f;              // 目标帧位置
        
        // --- 新增代码: 速度恢复相关 ---
        private Coroutine _speedRecoveryCoroutine;
        private AbilityEventObj_HitStop _lastHitStopEventForRecovery;
        // --- 新增代码结束 ---
        
        // 事件监听器列表
        private List<ICombatEventListener> eventListeners = new List<ICombatEventListener>();
        
        // 为每个事件创建唯一的key用于触发状态跟踪
        private Dictionary<int, Dictionary<int, bool>> clipEventTriggeredStates = new Dictionary<int, Dictionary<int, bool>>();
        
        private void Awake()
        {
            // 订阅条件状态变更事件
            ConditionStateNotifier.OnConditionStateChanged += HandleConditionStateChanged;
        }

        private void OnDestroy()
        {
            // 取消订阅，防止内存泄漏
            ConditionStateNotifier.OnConditionStateChanged -= HandleConditionStateChanged;
        }
        
        private void Start()
        {
            receiver = GetComponent<CombatEventReceiver>();

            // 自动初始化 Rigidbody 组件（如果未手动设置）
            if (_rigidbody == null)
            {
                _rigidbody = GetComponent<Rigidbody>();
                if (_rigidbody == null)
                {
                    Debug.LogWarning($"CombatController on {gameObject.name}: No Rigidbody component found. PhysicsMove will not work. Please add a Rigidbody component or set _rigidbody field manually.");
                }
            }

            ClipID_To_EventEffects = new Dictionary<int, List<AbilityEventWithEffects>>();
            ClearNullReference();
            InitClipsOnRunningLayers();
            InitAnimEffects();
            _animSpeedExecutor = new AnimSpeedExecutor(this);
            _moveExecutor = new MoveExecutor(this);
            
            // 自动查找并注册所有实现了ICombatEventListener接口的组件
            AutoRegisterEventListeners();
        }

        /// <summary>
        /// 自动查找并注册所有实现了ICombatEventListener接口的组件
        /// </summary>
        private void AutoRegisterEventListeners()
        {
            // 查找游戏对象中所有实现了ICombatEventListener接口的组件
            ICombatEventListener[] listeners = GetComponentsInChildren<ICombatEventListener>(true);
            
            foreach (var listener in listeners)
            {
                // 如果不是自身，则注册
                if (listener != null)
                {
                    RegisterEventListener(listener);
                    
                    // 获取组件所在游戏对象的名称，用于调试输出
                    string componentName = "未知组件";
                    if (listener is MonoBehaviour mb)
                    {
                        componentName = mb.name + ":" + mb.GetType().Name;
                    }
                    
                }
            }
        }
        
        /// <summary>
        /// 在场景中查找实现了ICombatEventListener接口的组件并注册
        /// 如果提供了targetObject参数，则只在该对象及其子对象中查找
        /// </summary>
        public void FindAndRegisterEventListeners(GameObject targetObject = null)
        {
            ICombatEventListener[] listeners;
            
            if (targetObject != null)
            {
                // 如果指定了目标对象，只在该对象中查找
                listeners = targetObject.GetComponentsInChildren<ICombatEventListener>(true);
            }
            else
            {
                // 否则在场景中的所有活动对象中查找
                listeners = GameObject.FindObjectsOfType<MonoBehaviour>().OfType<ICombatEventListener>().ToArray();
            }
            
            foreach (var listener in listeners)
            {
                RegisterEventListener(listener);
                
                string componentName = "未知组件";
                if (listener is MonoBehaviour mb)
                {
                    componentName = mb.name + ":" + mb.GetType().Name;
                }
                
                Debug.Log($"查找并注册事件监听器: {componentName}");
            }
        }

        public void InitClipsOnRunningLayers()
        {
            LayerActiveClipIDs = new List<int[]>();
            for (int i = 0; i < _animator.layerCount; i++)
            {
                LayerActiveClipIDs.Add(null);
            }
        }
        public void ClearNullReference()
        {
            for (int i = 0; i < CombatDatas.Count; i++)
            {
                for (int j = 0; j < CombatDatas[i].CombatObjs.Count; j++)
                {
                    if (CombatDatas[i].CombatObjs[j] == null)
                    {
                        CombatDatas[i].CombatObjs.RemoveAt(j);
                        j--;
                        continue;
                    }
                    else
                    {
                        for (int k = 0; k < CombatDatas[i].CombatObjs[j].events.Count; k++)
                        {
                            if (CombatDatas[i].CombatObjs[j].events[k].Obj == null)
                            {
                                CombatDatas[i].CombatObjs[j].events.RemoveAt(k);
                                k--;
                                continue;
                            }
                        }

                    }
                }
            }
        }


        private void Update()
        {
            // 更新顿帧
            UpdateHitStop();
         
            // 只有在非顿帧状态下才运行普通效果
            if (!isInHitStop)
            {
                RunEffects(0);
                
                // 只有在非顿帧状态下才执行动画速度控制器
                if (_executeAnimSpeedController)
                {
                    _animSpeedExecutor.Execute();
                }
            }
            else if (_currentHitStopEventObj != null && _currentHitStopEventObj is AbilityEventObj_HitStop)
            {
                // 在顿帧状态下，确保动画速度保持设定值
                AbilityEventObj_HitStop hitStopObj = (AbilityEventObj_HitStop)_currentHitStopEventObj;
                _animator.speed = hitStopObj.animationSpeed;
            }
        }

        private void FixedUpdate()
        {
            RunEffects(1);
        }

        public Vector3 GetCurrentRootMotion()
        {
            return _moveExecutor.GetCurrentRootMotion();
        }
        
        /// <summary>
        /// 获取并重置累积的root motion
        /// </summary>
        public Vector3 GetAndResetAccumulatedRootMotion()
        {
            return _moveExecutor.GetAndResetAccumulatedRootMotion();
        }
        
        /// <summary>
        /// 设置是否启用root motion
        /// </summary>
        /// <param name="enabled">是否启用</param>
        /// <param name="reason">设置原因（用于调试）</param>
        public void SetRootMotionEnabled(bool enabled, string reason = "")
        {
            if (!string.IsNullOrEmpty(reason))
            {
                Debug.Log($"🦵 Root Motion状态变更: {(enabled ? "启用" : "禁用")} - 原因: {reason}");
            }
            _moveExecutor.SetRootMotionEnabled(enabled);
        }
        
        /// <summary>
        /// 重置root motion数据（用于技能开始时重置状态）
        /// </summary>
        public void ResetRootMotion()
        {
            _moveExecutor.ResetRootMotion();
        }
        
        /// <summary>
        /// 获取root motion接收器的状态信息（用于调试）
        /// </summary>
        public string GetRootMotionDebugInfo()
        {
            var receiver = _animator.GetComponent<RootMotionReceiver>();
            if (receiver != null)
            {
                return $"手动控制: {receiver.manualRootMotionControl}, " +
                       $"应用位置: {receiver.applyRootPosition}, " +
                       $"当前Root Motion: {receiver.CurrentRootMotion}, " +
                       $"累积Root Motion: {receiver.AccumulatedRootMotion}";
            }
            return "Root Motion接收器未找到";
        }
        
        public List<int[]> LayerActiveClipIDs = new List<int[]>();
        /// <summary>
        /// Fetch states and clips in animator.
        /// UpdateMode : 0:Update 1:FixedUpdate.
        /// </summary>
        public void RunEffects( int UpdateMode = 0 )
        {
            // 如果在顿帧状态，且是非FixedUpdate调用，则减少对动画的干扰
            if (isInHitStop && UpdateMode == 0)
            {
                return;
            }
            
            for (int i = 0; i < _animator.layerCount; i++)
            {
                var LayerIndex = i;
                if (!_animator.IsInTransition(LayerIndex))
                {
                    var CurrentAnimState = _animator.GetCurrentAnimatorStateInfo(LayerIndex);
                    var RunningClips = _animator.GetCurrentAnimatorClipInfo(LayerIndex);

                    int[] runningclipsID = new int[RunningClips.Length];
                    for (int j = 0; j < RunningClips.Length; j++)
                    {
                        runningclipsID[j] = RunningClips[j].clip.GetInstanceID();
                    }
                    UpdateLayerActiveClips(LayerIndex, runningclipsID);

                    for (int j = 0; j < RunningClips.Length; j++)
                    {
                        var CurrentClipID = RunningClips[j].clip.GetInstanceID();

                        if (!ClipID_To_EventEffects.ContainsKey(CurrentClipID)) { continue; }


                        RunningEventsOnClip(CurrentClipID, CurrentAnimState.normalizedTime, LayerIndex, UpdateMode);
                    }
                }

                if (_animator.IsInTransition(LayerIndex))
                {
                    var NextAnimState = _animator.GetNextAnimatorStateInfo(LayerIndex);
                    var NextRunningClips = _animator.GetNextAnimatorClipInfo(LayerIndex);

                    int[] runningClipsID = new int[NextRunningClips.Length];
                    for (int j = 0; j < NextRunningClips.Length; j++)
                    {
                        runningClipsID[j] = NextRunningClips[j].clip.GetInstanceID();
                    }
                    UpdateLayerActiveClips(LayerIndex, runningClipsID);

                    for (int j = 0; j < NextRunningClips.Length; j++)
                    {
                        var CurrentClip = NextRunningClips[j].clip.GetInstanceID();
                        if (!ClipID_To_EventEffects.ContainsKey(CurrentClip)) { continue; }
                        RunningEventsOnClip(CurrentClip, NextAnimState.normalizedTime, LayerIndex ,UpdateMode);
                    }
                }

            }
        }
        /// <summary>
        //  Running the target effects on animation clip.
        /// </summary>
        /// <param name="clip"></param>
        /// <param name="NormalizedTime"></param>
        /// <param name="LayerIndex"></param>
        /// <param name="UpdateMode"> 0 : Update 1:FixedUpdate </param>
        public void RunningEventsOnClip(int clipID, float NormalizedTime, int LayerIndex ,int UpdateMode = 0)
        {
            List<AbilityEventWithEffects> abilityEventWithEffects = ClipID_To_EventEffects[clipID];
            
            // 初始化clipID的事件触发状态字典
            if (!clipEventTriggeredStates.ContainsKey(clipID))
            {
                clipEventTriggeredStates[clipID] = new Dictionary<int, bool>();
            }
            
            // 简化的循环检测：如果时间减少了，并且不是很小的减少（可能是帧间的微小波动），则认为是循环
            bool animationLooped = false;
            
            if (lastNormalizedTimes.ContainsKey(clipID))
            {
                float lastTime = lastNormalizedTimes[clipID];
                
                // 检测时间倒退，且倒退幅度大于0.1（避免误判小的波动）
                if (lastTime > NormalizedTime && (lastTime - NormalizedTime) > 0.1f)
                {
                    animationLooped = true;
                    Debug.Log($"[循环检测] ⭕ Clip {clipID} 动画循环检测到: {lastTime:F3} -> {NormalizedTime:F3} (倒退: {(lastTime - NormalizedTime):F3})");
                    
                    // 强制重置所有事件状态
                    for (int i = 0; i < abilityEventWithEffects.Count; i++)
                    {
                        var eve = abilityEventWithEffects[i];
                        if (eve.effect.IsRunning)
                        {
                            Debug.Log($"[循环重置] 🔄 强制重置事件 {eve.effect._EventObj.name} (之前运行状态: {eve.effect.IsRunning})");
                            eve.effect.EndEffect();
                        }
                    }
                    
                    // 清除所有事件的触发状态
                    clipEventTriggeredStates[clipID].Clear();
                    Debug.Log($"[循环重置] 🔄 清除Clip {clipID}的所有事件触发状态");
                }
                
                // 调试信息：在关键时刻打印时间变化
                if (UpdateMode == 0)
                {
                    float timeDiff = NormalizedTime - lastTime;
                    if (Mathf.Abs(timeDiff) > 0.05f || lastTime > 0.9f || NormalizedTime < 0.1f || timeDiff < 0)
                    {
                    }
                }
            }
            
            // 更新时间记录
            lastNormalizedTimes[clipID] = NormalizedTime;
            
            for (int j = 0; j < abilityEventWithEffects.Count; j++)
            {
                var eve = abilityEventWithEffects[j];
                
                // 检查条件是否满足，不满足则跳过该事件
                if (!eve.eve.CheckCondition(this))
                {
                    continue;
                }
                
                var StartTime = eve.eve.GetEventStartTime();
                var EndTime = eve.eve.GetEventEndTime();
                var EveTimeType = eve.eve.GetEventTimeType();

                if (EveTimeType == AbilityEventObj.EventTimeType.EventTime && UpdateMode == 0)
                {
                    // 为每个事件创建唯一的key用于触发状态跟踪
                    int eventKey = j; // 使用事件在列表中的索引作为key
                    
                    // 初始化事件触发状态
                    if (!clipEventTriggeredStates[clipID].ContainsKey(eventKey))
                    {
                        clipEventTriggeredStates[clipID][eventKey] = false;
                    }
                    
                    bool hasTriggeredThisCycle = clipEventTriggeredStates[clipID][eventKey];
                    bool shouldTrigger = false;
                    
                    // 检查是否应该触发事件
                    if (NormalizedTime >= StartTime && !hasTriggeredThisCycle)
                    {
                        shouldTrigger = true;
                        clipEventTriggeredStates[clipID][eventKey] = true; // 标记为已触发
                    }
                    
                    // 重置触发状态：当时间回到StartTime之前时
                    if (NormalizedTime < StartTime && hasTriggeredThisCycle)
                    {
                        clipEventTriggeredStates[clipID][eventKey] = false;
                    }
                    
                    // 触发事件
                    if (shouldTrigger && eve.effect._EventObj.IsActive && !eve.effect.IsRunning)
                    {
                        eve.effect.StartEffect();
                    }
                    
                    // 运行中的事件继续更新
                    if (eve.effect._EventObj.IsActive && eve.effect.IsRunning)
                    {
                        eve.effect.EffectRunning();
                    }
                    
                    // 如果时间在StartTime之前，确保事件结束
                    if (NormalizedTime < StartTime && eve.effect.IsRunning)
                    {
                        eve.effect.EndEffect();
                    }
                }
                if (EveTimeType == AbilityEventObj.EventTimeType.EventRange || EveTimeType == AbilityEventObj.EventTimeType.EventMultiRange)
                {
                    if (NormalizedTime < EndTime && NormalizedTime >= StartTime)
                    {
                        if (!eve.effect.IsRunning && eve.effect._EventObj.IsActive && UpdateMode == 0)
                        {
                            eve.effect.StartEffect();
                        }

                        if(eve.effect.IsRunning)
                        {
                            if (UpdateMode == 0)
                            {
                                eve.effect.EffectRunning();
                                eve.effect.EffectRunning(NormalizedTime);
                            }
                            if(UpdateMode == 1)
                            {
                                eve.effect.EffectRunningFixedUpdate(NormalizedTime);
                            }
                        }
                    }
                    else if (NormalizedTime >= EndTime || NormalizedTime < StartTime && UpdateMode == 0)
                    {
                        if (eve.effect._EventObj.IsActive)
                        {
                            if (eve.effect.IsRunning)
                            {
                            }
                            eve.effect.EndEffect();
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Update Active Clips on target Layer, End last frame events if needed.
        /// </summary>
        /// <param name="LayerIndex"></param>
        /// <param name="clips"></param>
        public void UpdateLayerActiveClips(int LayerIndex, int[] clipsID)
        {
            bool RunningClipsChangedInLayer = false;
            if (LayerActiveClipIDs[LayerIndex] != null)
            {
                if (LayerActiveClipIDs[LayerIndex].Length != clipsID.Length)
                {
                    RunningClipsChangedInLayer = true;
                }
                else
                {
                    for (int i = 0; i < LayerActiveClipIDs[LayerIndex].Length; i++)
                    {
                        if (LayerActiveClipIDs[LayerIndex][i] != clipsID[i])
                        {
                            RunningClipsChangedInLayer = true;
                        }
                    }

                }
            }
            else RunningClipsChangedInLayer = true;

            if (RunningClipsChangedInLayer)
            {
                if(LayerActiveClipIDs[LayerIndex] != null)
                {
                    for (int i = 0; i < LayerActiveClipIDs[LayerIndex].Length; i++)
                    {
                        var clip = LayerActiveClipIDs[LayerIndex][i];
                        if (ClipID_To_EventEffects.ContainsKey(clip))
                        {
                            for (int j = 0; j < ClipID_To_EventEffects[clip].Count; j++)
                            {
                                ClipID_To_EventEffects[clip][j].effect.EndEffect();
                            }
                        }
                        
                        // 清理不再使用的clip的事件触发状态
                        bool isStillActive = false;
                        for (int k = 0; k < clipsID.Length; k++)
                        {
                            if (clipsID[k] == clip)
                            {
                                isStillActive = true;
                                break;
                            }
                        }
                        
                        if (!isStillActive)
                        {
                            ClearEventTriggeredStates(clip);
                        }
                    }
                }
                LayerActiveClipIDs[LayerIndex] = clipsID;
            }
        }
        
	    public Transform GetNodeTranform(CharacterNode.NodeType type)
	    {
	        if(type == CharacterNode.NodeType.Animator)
	        {
	            if (_animator != null)
	            {
	                return _animator.transform;
	            }
	            return transform;
	        }
	
	        for(int i  =0;i<Nodes.Count;i++)
	        {
	            if(Nodes[i].type == type)
	            {
	                if(Nodes[i].NodeTrans == null)
	                {
	                    return _animator.transform;
	                }
	                return Nodes[i].NodeTrans;
	            }
	        }
	        return _animator.transform;
	    }
	
	    public void SimpleMoveRG(Vector3 deltaMove)
	    {
            _moveExecutor.Move(deltaMove);
        }

        public void PhysicsMove(Vector3 deltaMove)
        {
            if (_rigidbody != null)
            {
                Vector3 newPosition = _rigidbody.position + deltaMove;
                _rigidbody.MovePosition(newPosition);
                
                // 调试信息：记录物理移动
                if (deltaMove.magnitude > 0.001f)
                {
                    Debug.Log($"PhysicsMove: Moving from {_rigidbody.position} by {deltaMove} to {newPosition}");
                }
            }
            else
            {
                Debug.LogError($"CombatController on {gameObject.name}: Rigidbody not assigned! PhysicsMove cannot work. " +
                               $"Please ensure a Rigidbody component is attached to this GameObject or manually assign the _rigidbody field.");
                
                // 作为备选方案，使用 Transform 移动
                Debug.LogWarning("Falling back to Transform-based movement as emergency fallback.");
                SimpleMoveRG(deltaMove);
            }
        }
	
	    public void InitAnimReceiver()
	    {
	        receiver = _animator.gameObject.AddComponent<CombatEventReceiver>();
	        receiver.controller = this;
	        receiver.CombatDatasID = new List<string>();
	        for (int i =0;i<CombatDatas.Count;i++)
	        {
	            var Group = CombatDatas[i];
	            for (int j = 0; j < Group.CombatObjs.Count; j++)
	            {
	                receiver.CombatDatasID.Add(Group.CombatObjs[j].GetInstanceID().ToString());
	            }
	        }
	    }
	    public void StartEvent(int GroupIndex, int ObjIndex, int EventIndex)
	    {
	        // 先检查条件是否满足
	        if (CombatDatas[GroupIndex].CombatObjs[ObjIndex].events[EventIndex].CheckCondition(this) == false)
	        {
	            // 不满足条件，不触发事件
	            return;
	        }
	        
	        // 满足条件，继续原有逻辑
	        if (CombatDatas[GroupIndex].eves[ObjIndex].Obj.events[EventIndex].Obj.IsActive)
	        {
	            CombatDatas[GroupIndex].eves[ObjIndex].EventEffects[EventIndex].eve = CombatDatas[GroupIndex].CombatObjs[ObjIndex].events[EventIndex];
	            CombatDatas[GroupIndex].eves[ObjIndex].EventEffects[EventIndex].StartEffect();
	        }
	    }
	    public void EndEvent(int GroupIndex, int ObjIndex, int EventIndex)
	    {
	        if (CombatDatas[GroupIndex].eves[ObjIndex].Obj.events[EventIndex].Obj.IsActive)
	        {
	            CombatDatas[GroupIndex].eves[ObjIndex].EventEffects[EventIndex].EndEffect();
	        }
	    }
	
	
	
	    public void InitAnimEffects()
	    {
	        for (int i = 0; i < CombatDatas.Count; i++)
	        {
	            var Group = CombatDatas[i];
	            for (int j = 0; j < Group.CombatObjs.Count; j++)
	            {
	                //Caution: The Number of CombatObj and EventEffects must sync
	                var CombatObj = Group.CombatObjs[j];
	                AbilityObjWithEffect ae = new AbilityObjWithEffect();
	                ae.Obj = CombatObj;
	                for (int k = 0; k < CombatObj.events.Count; k++)
	                {
	                    var EventEffect = AddEventEffects( CombatObj.Clip.GetInstanceID(), CombatObj.events[k]);
	                    EventEffect.AnimObj = CombatObj;
	                    ae.EventEffects.Add(EventEffect);
	                }
	                Group.eves.Add(ae);
	            }
	        }
	    }
	
	    List<AbilityEventEffect> _abilityEventEffects = new List<AbilityEventEffect>();
	    public AbilityEventEffect AddEventEffects( int clipID, AbilityEvent eve)
	    {
	        AbilityEventObj EffectObj = eve.Obj;
	        AbilityEventEffect _abilityEventEffect = EffectObj.Initialize();
	        _abilityEventEffect.eve = eve;
	        _abilityEventEffect._combatController = this;
	        _abilityEventEffects.Add(_abilityEventEffect);
	
	        AbilityEventWithEffects eveWithEffects = new AbilityEventWithEffects();
	        eveWithEffects.eve = eve;
	        eveWithEffects.effect = _abilityEventEffect;
	
	        //Save all animationEvents to dictionary
	        if(ClipID_To_EventEffects.ContainsKey(clipID))
	        {
                ClipID_To_EventEffects[clipID].Add(eveWithEffects);
	        }
	        else
	        {
	            List<AbilityEventWithEffects> list = new List<AbilityEventWithEffects>();
	            list.Add(eveWithEffects);
                ClipID_To_EventEffects.Add(clipID, list);
	        }
	
	
	        return _abilityEventEffect;
	    }
	
	
        public bool IsInState(string Name)
        {
            for(int i =0;i<RunningStates.Count;i++)
            {
                if (RunningStates[i].CurrentStateName == Name)
                {
                    return true;
                }
            }
            return false;
        }
	
	
        // 检查是否有指定的Buff
        public bool HasBuff(string buffID)
        {
            return string.IsNullOrEmpty(buffID) || currentBuffs.Contains(buffID);
        }
        
        // 获取当前生命值百分比
        public float GetHealthPercentage()
        {
            return maxHealth > 0 ? currentHealth / maxHealth : 0;
        }
        
        // 检查是否在空中
        public bool IsInAir()
        {
            return isInAir;
        }
        
        // 检查是否在地面
        public bool IsOnGround()
        {
            return !isInAir;
        }
        
        // 检查是否有目标
        public bool HasTarget()
        {
            return currentTarget != null;
        }
        
        // 获取与目标的距离
        public float GetTargetDistance()
        {
            if (currentTarget == null)
                return float.MaxValue;
                
            return Vector3.Distance(transform.position, currentTarget.position);
        }
        
        // 简化的条件API
        
        /// <summary>
        /// 设置"击中目标"条件状态
        /// </summary>
        /// <param name="value">条件值</param>
        public void SetHitTargetCondition(bool value, CombatController target = null)
        {
            // 状态变化时才通知，避免不必要的性能开销
            if (hasHitTarget == value) return;

            hasHitTarget = value;
            
            if (value)
            {
                if (target != null) TriggerHasHitTargetEvent(target);
                // 使用新的通知系统
                ConditionStateNotifier.Notify(this, "has_hit");
            }
        }
        
        /// <summary>
        /// 设置"被击中"条件状态
        /// </summary>
        /// <param name="value">条件值</param>
        public void SetBeenHitCondition(bool value, CombatController attacker = null)
        {
            // 状态变化时才通知
            if (hasBeenHit == value) return;

            hasBeenHit = value;

            if (value)
            {
                if (attacker != null) TriggerBeenHitEvent(attacker);
                // 使用新的通知系统
                ConditionStateNotifier.Notify(this, "been_hit");
            }
        }
        
        /// <summary>
        /// 延迟重置被击中条件，避免短时间内多次命中时条件被过早重置
        /// </summary>
        /// <param name="delay">延迟时间（秒）</param>
        public void DelayedResetBeenHitCondition(float delay)
        {
            // 取消之前可能正在执行的重置协程
            if (_beenHitResetCoroutine != null)
            {
                StopCoroutine(_beenHitResetCoroutine);
            }
            
            // 启动新的重置协程
            _beenHitResetCoroutine = StartCoroutine(ResetBeenHitAfterDelay(delay));
        }
        
        // 记录当前正在执行的重置协程
        private Coroutine _beenHitResetCoroutine = null;
        
        /// <summary>
        /// 延迟重置被击中条件的协程
        /// </summary>
        private System.Collections.IEnumerator ResetBeenHitAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            
            // 重置条件
            hasBeenHit = false;
            
            // 清除协程引用
            _beenHitResetCoroutine = null;
        }
        
        /// <summary>
        /// 检查是否在顿帧中
        /// </summary>
        public bool IsInHitStop()
        {
            return isInHitStop;
        }
        
        /// <summary>
        /// 检查是否判定受击
        /// </summary>
        public bool IsHitChecked()
        {
            return isHitChecked;
        }
        
        /// <summary>
        /// 设置"判定受击"条件状态
        /// </summary>
        /// <param name="value">条件值</param>
        public void SetHitCheckedCondition(bool value)
        {
            // 状态变化时才通知
            if (isHitChecked == value) return;

            isHitChecked = value;

            if (value)
            {
                TriggerHitCheckedEvent();
                // 使用新的通知系统
                ConditionStateNotifier.Notify(this, "hit_checked");
            }
        }
        
        /// <summary>
        /// 开始顿帧
        /// </summary>
        /// <param name="frames">顿帧持续的帧数</param>
        /// <param name="speed">顿帧期间的动画速度（默认为0）</param>
        /// <param name="eventObj">触发顿帧的事件对象</param>
        public void StartHitStop(int frames, float speed = 0f, AbilityEventObj eventObj = null)
        {
            StartHitStop(frames, speed, eventObj, -1f);
        }
        
        /// <summary>
        /// 开始顿帧，支持精确帧定位
        /// </summary>
        /// <param name="frames">顿帧持续的帧数</param>
        /// <param name="speed">顿帧期间的动画速度（默认为0）</param>
        /// <param name="eventObj">触发顿帧的事件对象</param>
        /// <param name="exactFramePosition">精确帧位置（0-1之间，表示动画的归一化时间）</param>
        public void StartHitStop(int frames, float speed, AbilityEventObj eventObj, float exactFramePosition)
        {
            // --- 新增代码: 中断正在进行的速度恢复 ---
            if (_speedRecoveryCoroutine != null)
            {
                StopCoroutine(_speedRecoveryCoroutine);
                _speedRecoveryCoroutine = null;
            }
            // --- 新增代码结束 ---
            
            // 如果已经在顿帧中，先结束当前顿帧
            if (isInHitStop)
            {
                EndHitStop();
            }
            
            _currentHitStopEventObj = eventObj;
            originalAnimationSpeed = _animator.speed;
            
            if (_animSpeedExecutor != null)
            {
                _executeAnimSpeedController = false;
            }
            
            if (exactFramePosition >= 0f && exactFramePosition <= 1f)
            {
                SetAnimationExactFrame(exactFramePosition);
            }
            
            _animator.speed = speed;
            hitStopFrames = frames;
            hitStopTimer = 0f;
            isInHitStop = true;
            
            TriggerEnterHitStopEvent();

            // 使用新的通知系统
            ConditionStateNotifier.Notify(this, "in_hit_stop");
            
            _animator.Update(0);
        }
        
        /// <summary>
        /// 设置动画精确帧位置
        /// </summary>
        /// <param name="normalizedTime">归一化的时间位置(0-1)</param>
        public void SetAnimationExactFrame(float normalizedTime)
        {
            if (normalizedTime < 0f || normalizedTime > 1f || _animator == null)
                return;
                
            // 找到当前正在播放的动画
            int layerIndex = 0; // 使用主层
            AnimatorStateInfo stateInfo = _animator.GetCurrentAnimatorStateInfo(layerIndex);
            
            // 设置动画状态到指定时间点
            _animator.Play(stateInfo.fullPathHash, layerIndex, normalizedTime);
            
            // 强制更新Animator，确保变更立即生效
            _animator.Update(0);
            
        }
        
        /// <summary>
        /// 请求调整到精确帧位置
        /// </summary>
        /// <param name="normalizedTime">归一化的时间位置(0-1)</param>
        public void RequestFrameAdjustment(float normalizedTime)
        {
            _pendingFrameAdjustment = true;
            _targetFramePosition = normalizedTime;
        }
        
        /// <summary>
        /// 结束顿帧
        /// </summary>
        public void EndHitStop()
        {
            // --- 新增代码: 处理速度恢复 ---
            var hitStopEvent = _currentHitStopEventObj as AbilityEventObj_HitStop;
            if (hitStopEvent != null && hitStopEvent.enableSpeedRecovery)
            {
                // 如果启用了速度恢复，启动协程
                if (_speedRecoveryCoroutine != null) StopCoroutine(_speedRecoveryCoroutine);
                _speedRecoveryCoroutine = StartCoroutine(SpeedRecoveryCoroutine(hitStopEvent));
            }
            else
            {
                // 否则，立即恢复原始速度
                _animator.speed = originalAnimationSpeed;
            }
            // --- 新增代码结束 ---
            
            if (_animSpeedExecutor != null)
            {
                _executeAnimSpeedController = true;
            }
            
            hitStopTimer = 0f;
            hitStopFrames = 0;
            _currentHitStopEventObj = null;
            
            if (isInHitStop)
            {
                isInHitStop = false;
                TriggerExitHitStopEvent();
                
                // 状态变回false，也需要通知，以便依赖“不在顿帧中”的条件可以触发
                ConditionStateNotifier.Notify(this, "in_hit_stop");
            }
            else
            {
                isInHitStop = false;
            }
            
            _animator.Update(0);
        }
        
        // --- 新增代码: 速度恢复协程 ---
        private IEnumerator SpeedRecoveryCoroutine(AbilityEventObj_HitStop hitStopEvent)
        {
            float timeElapsed = 0f;
            float startSpeed = _animator.speed;
            float endSpeed = hitStopEvent.recoveryTargetSpeed;
            float duration = hitStopEvent.recoveryDuration;
            AnimationCurve curve = hitStopEvent.recoveryCurve;

            while (timeElapsed < duration)
            {
                timeElapsed += Time.deltaTime;
                float progress = timeElapsed / duration;
                float curveValue = curve.Evaluate(progress);
                _animator.speed = Mathf.Lerp(startSpeed, endSpeed, curveValue);
                yield return null;
            }

            // 确保最终速度被精确设置
            _animator.speed = endSpeed;
            _speedRecoveryCoroutine = null;
        }
        // --- 新增代码结束 ---
        
        /// <summary>
        /// 更新顿帧计时
        /// </summary>
        private void UpdateHitStop()
        {
            // 处理帧精确定位请求
            if (_pendingFrameAdjustment)
            {
                SetAnimationExactFrame(_targetFramePosition);
                _pendingFrameAdjustment = false;
            }
            
            if (isInHitStop)
            {
                hitStopTimer += Time.deltaTime * 60f; // 转换为大致帧数
                
                if (hitStopTimer >= hitStopFrames)
                {
                    EndHitStop();
                }
            }
        }
        
        /// <summary>
        /// 重置所有条件
        /// </summary>
        public void ResetAllConditions()
        {
            hasHitTarget = false;
            hasBeenHit = false;
            isInHitStop = false;
            isHitChecked = false;
            
            // 取消正在执行的重置协程
            if (_beenHitResetCoroutine != null)
            {
                StopCoroutine(_beenHitResetCoroutine);
                _beenHitResetCoroutine = null;
            }
        }
        
        // 检查是否击中目标
        public bool HasHitTarget()
        {
            return hasHitTarget;
        }
        
        // 检查是否被击中
        public bool HasBeenHit()
        {
            return hasBeenHit;
        }

        // 注册事件监听器
        public void RegisterEventListener(ICombatEventListener listener)
        {
            if (!eventListeners.Contains(listener))
            {
                eventListeners.Add(listener);
            }
        }
        
        // 移除事件监听器
        public void UnregisterEventListener(ICombatEventListener listener)
        {
            if (eventListeners.Contains(listener))
            {
                eventListeners.Remove(listener);
            }
        }
        
        /// <summary>
        /// 获取所有已注册的事件监听器
        /// </summary>
        public List<ICombatEventListener> GetEventListeners()
        {
            return eventListeners;
        }
        
        // 触发击中目标事件
        private void TriggerHasHitTargetEvent(CombatController target)
        {
            foreach (var listener in eventListeners)
            {
                listener.OnHasHitTarget(this, target);
            }
        }
        
        // 触发被击中事件
        private void TriggerBeenHitEvent(CombatController attacker)
        {
            foreach (var listener in eventListeners)
            {
                listener.OnBeenHit(this, attacker);
            }
        }
        
        // 触发命中判定事件
        private void TriggerHitCheckedEvent()
        {
            foreach (var listener in eventListeners)
            {
                listener.OnHitChecked(this);
            }
        }
        
        // 触发进入顿帧事件
        private void TriggerEnterHitStopEvent()
        {
            foreach (var listener in eventListeners)
            {
                listener.OnEnterHitStop(this);
            }
        }
        
        // 触发退出顿帧事件
        private void TriggerExitHitStopEvent()
        {
            foreach (var listener in eventListeners)
            {
                listener.OnExitHitStop(this);
            }
        }

        /// <summary>
        /// 获取当前攻击阶段
        /// </summary>
        public int GetAttackPhase()
        {
            return attackPhase;
        }
        
        /// <summary>
        /// 设置攻击阶段
        /// </summary>
        /// <param name="phase">新的攻击阶段</param>
        public void SetAttackPhase(int phase)
        {
            // 不再只在状态改变时触发事件，每次设置都触发
            int oldPhase = attackPhase;
            attackPhase = phase;
            
            // 即使新旧阶段相同，也触发事件
            TriggerAttackPhaseChangedEvent(oldPhase, attackPhase);
        }
        
        // 触发攻击阶段变更事件
        private void TriggerAttackPhaseChangedEvent(int oldPhase, int newPhase)
        {
            foreach (var listener in eventListeners)
            {
                listener.OnAttackPhaseChanged(this, oldPhase, newPhase);
            }
        }

        /// <summary>
        /// 清理指定clipID的事件触发状态
        /// </summary>
        /// <param name="clipID">要清理的clip ID</param>
        public void ClearEventTriggeredStates(int clipID)
        {
            if (clipEventTriggeredStates.ContainsKey(clipID))
            {
                clipEventTriggeredStates[clipID].Clear();
            }
            if (lastNormalizedTimes.ContainsKey(clipID))
            {
                lastNormalizedTimes.Remove(clipID);
            }
        }
        
        /// <summary>
        /// 清理所有事件触发状态
        /// </summary>
        public void ClearAllEventTriggeredStates()
        {
            clipEventTriggeredStates.Clear();
            lastNormalizedTimes.Clear();
        }
        
        /// <summary>
        /// 获取当前连击数
        /// </summary>
        /// <returns>当前连击数</returns>
        public int GetComboCount()
        {
            // 检查是否需要重置连击
            if (Time.time - lastComboTime > comboResetTime)
            {
                comboCount = 0;
            }
            
            return comboCount;
        }
        
        /// <summary>
        /// 增加连击数
        /// </summary>
        /// <param name="increment">增加的连击数，默认为1</param>
        public void AddCombo(int increment = 1)
        {
            comboCount += increment;
            lastComboTime = Time.time;
        }
        
        /// <summary>
        /// 重置连击数
        /// </summary>
        public void ResetCombo()
        {
            comboCount = 0;
            lastComboTime = 0f;
        }
        
        /// <summary>
        /// 设置连击数
        /// </summary>
        /// <param name="count">连击数</param>
        public void SetComboCount(int count)
        {
            comboCount = count;
            lastComboTime = Time.time;
        }
        
        /// <summary>
        /// 处理条件状态变更通知
        /// </summary>
        private void HandleConditionStateChanged(CombatController controller, string conditionId)
        {
            // 确保是自身的事件
            if (controller != this) return;
            
            //Debug.Log($"[CombatController] ⚡ Handling condition change: '{conditionId}'");

            // 遍历所有当前活动的clip
            for (int i = 0; i < _animator.layerCount; i++)
            {
                // 忽略未激活的layer
                if (_animator.GetLayerWeight(i) <= 0) continue;

                // 处理当前状态
                if (!_animator.IsInTransition(i))
                {
                    var stateInfo = _animator.GetCurrentAnimatorStateInfo(i);
                    var clips = _animator.GetCurrentAnimatorClipInfo(i);
                    foreach (var clipInfo in clips)
                    {
                        CheckAndTriggerEventsForClip(clipInfo.clip.GetInstanceID(), stateInfo.normalizedTime, conditionId);
                    }
                }

                // 处理过渡状态
                if (_animator.IsInTransition(i))
                {
                    var nextStateInfo = _animator.GetNextAnimatorStateInfo(i);
                    var nextClips = _animator.GetNextAnimatorClipInfo(i);
                    foreach (var clipInfo in nextClips)
                    {
                        CheckAndTriggerEventsForClip(clipInfo.clip.GetInstanceID(), nextStateInfo.normalizedTime, conditionId);
                    }
                }
            }
        }

        /// <summary>
        /// 检查并触发指定clip中与特定条件ID相关的事件
        /// </summary>
        private void CheckAndTriggerEventsForClip(int clipID, float normalizedTime, string relevantConditionId)
        {
            if (!ClipID_To_EventEffects.ContainsKey(clipID)) return;

            var eventWithEffects = ClipID_To_EventEffects[clipID];
            for (int j = 0; j < eventWithEffects.Count; j++)
            {
                var eve = eventWithEffects[j];
                var eveCondition = eve.eve.condition;

                // 如果事件没有条件，或者其条件ID与当前变化的ID不符，则跳过
                if (!eveCondition.hasCondition || eveCondition.interfaceConditionId != relevantConditionId)
                {
                    // 兼容旧版枚举，如果通知的是旧版条件，也需要检查
                    if (eveCondition.conditionMode != EventCondition.ConditionMode.LegacyEnum || 
                        eveCondition.GetLegacyConditionId() != relevantConditionId)
                    {
                        continue;
                    }
                }
                
                // 检查事件是否在激活的时间窗口内
                var startTime = eve.eve.GetEventStartTime();
                if (normalizedTime < startTime) continue;
                
                // 检查条件是否真的满足
                if (!eve.eve.CheckCondition(this)) continue;

                // 处理单点触发事件
                if (eve.eve.GetEventTimeType() == AbilityEventObj.EventTimeType.EventTime)
                {
                    int eventKey = j;
                    if (!clipEventTriggeredStates.ContainsKey(clipID))
                    {
                        clipEventTriggeredStates[clipID] = new Dictionary<int, bool>();
                    }
                    if (!clipEventTriggeredStates[clipID].ContainsKey(eventKey))
                    {
                        clipEventTriggeredStates[clipID][eventKey] = false;
                    }

                    if (!clipEventTriggeredStates[clipID][eventKey] && eve.effect._EventObj.IsActive && !eve.effect.IsRunning)
                    {
                        eve.effect.StartEffect();
                        clipEventTriggeredStates[clipID][eventKey] = true; // 标记为已触发
                        Debug.Log($"[立即触发] ⚡ 条件 '{relevantConditionId}' 满足，触发事件: {eve.effect._EventObj.name}");
                    }
                }
                // 处理范围触发事件
                else if (eve.eve.GetEventTimeType() == AbilityEventObj.EventTimeType.EventRange || eve.eve.GetEventTimeType() == AbilityEventObj.EventTimeType.EventMultiRange)
                {
                    var endTime = eve.eve.GetEventEndTime();
                    if (normalizedTime < endTime && !eve.effect.IsRunning && eve.effect._EventObj.IsActive)
                    {
                        eve.effect.StartEffect();
                        Debug.Log($"[立即触发] ⚡ 范围事件条件 '{relevantConditionId}' 满足，启动: {eve.effect._EventObj.name}");
                    }
                }
            }
        }
	}
}

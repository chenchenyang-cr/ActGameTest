using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CombatEditor
{
    [AbilityEvent]
    [CreateAssetMenu(menuName = "AbilityEvents / AnimatorVariable")]
    public class AbilityEventObj_AnimatorVariable : AbilityEventObj
    {
        [System.Serializable]
        public class AnimatorVariableData
        {
            [Header("变量设置")]
            [Tooltip("变量名称")]
            public string variableName = "";
            [Tooltip("变量类型")]
            public AnimatorControllerParameterType variableType = AnimatorControllerParameterType.Bool;
            
            [Header("布尔值设置")]
            [Tooltip("设置的布尔值")]
            public bool boolValue = false;
            
            [Header("整数值设置")]
            [Tooltip("设置的整数值")]
            public int intValue = 0;
            
            [Header("浮点值设置")]
            [Tooltip("设置的浮点值")]
            public float floatValue = 0f;
            
            [Header("触发器设置")]
            [Tooltip("对于触发器类型，是否激活触发器")]
            public bool activateTrigger = true;
            
            [Header("操作设置")]
            [Tooltip("是否恢复到原始值（仅在事件结束时）")]
            public bool restoreOriginalValue = false;
            
            // 公共属性，用于外部访问
            public string VariableName => variableName;
            public AnimatorControllerParameterType VariableType => variableType;
            
            // 构造函数
            public AnimatorVariableData(string name, AnimatorControllerParameterType type)
            {
                variableName = name;
                variableType = type;
            }
            
            // 默认构造函数
            public AnimatorVariableData() { }
        }
        
        [Header("动画机引用")]
        [Tooltip("目标动画机（如果为空，将从CombatController中自动获取）")]
        public Animator targetAnimator;
        
        [Header("动画机变量设置")]
        [Tooltip("要控制的动画机变量列表")]
        public List<AnimatorVariableData> animatorVariables = new List<AnimatorVariableData>();
        
        [Header("事件类型设置")]
        [Tooltip("是否为持续性事件（EventRange）还是瞬时事件（EventTime）")]
        public bool isRangeEvent = false;
        
        public override EventTimeType GetEventTimeType()
        {
            return isRangeEvent ? EventTimeType.EventRange : EventTimeType.EventTime;
        }
        
        public override AbilityEventEffect Initialize()
        {
            return new AbilityEventEffect_AnimatorVariable(this);
        }
        
        #if UNITY_EDITOR
        public override AbilityEventPreview InitializePreview()
        {
            return new AbilityEventPreview_AnimatorVariable(this);
        }
        #endif
        
        /// <summary>
        /// 获取动画机中的所有可用变量名称列表
        /// </summary>
        public List<string> GetAvailableVariableNames()
        {
            var animator = GetBestAvailableAnimator();
            if (animator == null || animator.runtimeAnimatorController == null)
            {
                return new List<string>();
            }
            
            var variableNames = new List<string>();
            foreach (var parameter in animator.parameters)
            {
                variableNames.Add(parameter.name);
            }
            return variableNames;
        }
        
        /// <summary>
        /// 获取动画机中指定变量的类型
        /// </summary>
        public AnimatorControllerParameterType? GetVariableType(string variableName)
        {
            var animator = GetBestAvailableAnimator();
            if (animator == null || animator.runtimeAnimatorController == null)
            {
                return null;
            }
            
            foreach (var parameter in animator.parameters)
            {
                if (parameter.name == variableName)
                {
                    return parameter.type;
                }
            }
            return null;
        }
        
        /// <summary>
        /// 添加新的动画机变量控制
        /// </summary>
        public void AddAnimatorVariable(string variableName = "", AnimatorControllerParameterType variableType = AnimatorControllerParameterType.Bool)
        {
            var newVariable = new AnimatorVariableData(variableName, variableType);
            
            // 如果变量名不为空，尝试从动画机获取默认值
            if (!string.IsNullOrEmpty(variableName))
            {
                SetDefaultValueFromAnimator(newVariable);
            }
            
            animatorVariables.Add(newVariable);
        }
        
        /// <summary>
        /// 从动画机中获取变量的当前值作为默认值
        /// </summary>
        private void SetDefaultValueFromAnimator(AnimatorVariableData variableData)
        {
            var animator = GetBestAvailableAnimator();
            if (animator == null || string.IsNullOrEmpty(variableData.variableName))
                return;
                
            switch (variableData.VariableType)
            {
                case AnimatorControllerParameterType.Bool:
                    variableData.boolValue = animator.GetBool(variableData.VariableName);
                    break;
                case AnimatorControllerParameterType.Int:
                    variableData.intValue = animator.GetInteger(variableData.VariableName);
                    break;
                case AnimatorControllerParameterType.Float:
                    variableData.floatValue = animator.GetFloat(variableData.VariableName);
                    break;
                case AnimatorControllerParameterType.Trigger:
                    variableData.activateTrigger = true; // 触发器默认为激活
                    break;
            }
        }
        
        /// <summary>
        /// 获取最佳可用的动画机
        /// 优先级：手动指定 > CombatController动画机 > 场景中的CombatController > 场景中的任意动画机
        /// </summary>
        public Animator GetBestAvailableAnimator()
        {
            // 1. 优先使用手动指定的动画机
            if (targetAnimator != null)
            {
                Debug.Log($"使用手动指定的动画机: {targetAnimator.name}");
                return targetAnimator;
            }
            
            #if UNITY_EDITOR
            // 2. 尝试从当前选中的对象或其父对象中查找CombatController
            var selection = UnityEditor.Selection.activeGameObject;
            if (selection != null)
            {
                var combatController = selection.GetComponentInParent<CombatController>();
                if (combatController != null && combatController._animator != null)
                {
                    targetAnimator = combatController._animator; // 自动设置为目标动画机
                    Debug.Log($"自动检测到选中对象的CombatController动画机: {combatController._animator.name}");
                    return combatController._animator;
                }
            }
            #endif
            
            // 3. 在场景中查找CombatController
            var combatControllers = UnityEngine.Object.FindObjectsOfType<CombatController>();
            foreach (var controller in combatControllers)
            {
                if (controller._animator != null)
                {
                    targetAnimator = controller._animator; // 自动设置为目标动画机
                    Debug.Log($"自动检测到场景中的CombatController动画机: {controller._animator.name} (来自 {controller.name})");
                    return controller._animator;
                }
            }
            
            // 4. 最后尝试查找场景中的任意动画机
            var animators = UnityEngine.Object.FindObjectsOfType<Animator>();
            foreach (var animator in animators)
            {
                if (animator.runtimeAnimatorController != null)
                {
                    targetAnimator = animator; // 自动设置为目标动画机
                    Debug.Log($"自动检测到场景中的动画机: {animator.name}");
                    return animator;
                }
            }
            
            return null;
        }
    }

    public class AbilityEventEffect_AnimatorVariable : AbilityEventEffect
    {
        AbilityEventObj_AnimatorVariable EventObj => (AbilityEventObj_AnimatorVariable)_EventObj;
        
        // 用于存储原始值，以便在事件结束时恢复
        private Dictionary<string, object> originalValues = new Dictionary<string, object>();
        
        public AbilityEventEffect_AnimatorVariable(AbilityEventObj InitObj) : base(InitObj)
        {
            _EventObj = InitObj;
        }
        
        public override void StartEffect()
        {
            base.StartEffect();
            
            // 获取动画机引用
            Animator animator = GetTargetAnimator();
            if (animator == null)
            {
                Debug.LogWarning($"AbilityEventEffect_AnimatorVariable: 未找到可用的Animator组件！");
                return;
            }
            
            // 检查是否有变量需要控制
            if (EventObj.animatorVariables.Count == 0)
            {
                Debug.LogWarning("AbilityEventEffect_AnimatorVariable: 没有设置要控制的动画机变量！");
                return;
            }
            
            // 存储原始值并设置新值
            foreach (var varData in EventObj.animatorVariables)
            {
                if (string.IsNullOrEmpty(varData.VariableName))
                    continue;
                    
                StoreOriginalValue(varData);
                SetAnimatorVariable(varData);
            }
        }
        
        public override void EffectRunning()
        {
            base.EffectRunning();
            // 对于持续性事件，可以在这里添加持续更新的逻辑
        }
        
        public override void EndEffect()
        {
            base.EndEffect();
            
            Animator animator = GetTargetAnimator();
            if (animator == null)
                return;
                
            // 如果需要恢复原始值
            foreach (var varData in EventObj.animatorVariables)
            {
                if (varData.restoreOriginalValue && originalValues.ContainsKey(varData.VariableName))
                {
                    RestoreOriginalValue(varData);
                }
            }
        }
        
        /// <summary>
        /// 获取目标动画机
        /// </summary>
        private Animator GetTargetAnimator()
        {
            // 优先使用指定的目标动画机
            if (EventObj.targetAnimator != null)
                return EventObj.targetAnimator;
                
            // 否则使用CombatController的动画机
            if (_combatController?._animator != null)
                return _combatController._animator;
                
            return null;
        }
        
        private void StoreOriginalValue(AbilityEventObj_AnimatorVariable.AnimatorVariableData varData)
        {
            if (!varData.restoreOriginalValue)
                return;
                
            var animator = GetTargetAnimator();
            if (animator == null)
                return;
            
            switch (varData.VariableType)
            {
                case AnimatorControllerParameterType.Bool:
                    if (HasParameter(varData.VariableName, AnimatorControllerParameterType.Bool))
                        originalValues[varData.VariableName] = animator.GetBool(varData.VariableName);
                    break;
                    
                case AnimatorControllerParameterType.Int:
                    if (HasParameter(varData.VariableName, AnimatorControllerParameterType.Int))
                        originalValues[varData.VariableName] = animator.GetInteger(varData.VariableName);
                    break;
                    
                case AnimatorControllerParameterType.Float:
                    if (HasParameter(varData.VariableName, AnimatorControllerParameterType.Float))
                        originalValues[varData.VariableName] = animator.GetFloat(varData.VariableName);
                    break;
                    
                case AnimatorControllerParameterType.Trigger:
                    // 触发器没有原始值概念，不需要存储
                    break;
            }
        }
        
        private void SetAnimatorVariable(AbilityEventObj_AnimatorVariable.AnimatorVariableData varData)
        {
            var animator = GetTargetAnimator();
            if (animator == null)
                return;
                
            object oldValue = null;
            object newValue = null;
            
            switch (varData.VariableType)
            {
                case AnimatorControllerParameterType.Bool:
                    if (HasParameter(varData.VariableName, AnimatorControllerParameterType.Bool))
                    {
                        oldValue = animator.GetBool(varData.VariableName);
                        animator.SetBool(varData.VariableName, varData.boolValue);
                        newValue = varData.boolValue;
                        
                        // 触发事件
                        foreach (var listener in _combatController.GetEventListeners())
                        {
                            listener.OnAnimatorBoolSet(_combatController, varData.VariableName, varData.boolValue);
                        }
                    }
                    break;
                    
                case AnimatorControllerParameterType.Int:
                    if (HasParameter(varData.VariableName, AnimatorControllerParameterType.Int))
                    {
                        oldValue = animator.GetInteger(varData.VariableName);
                        animator.SetInteger(varData.VariableName, varData.intValue);
                        newValue = varData.intValue;
                        
                        // 触发事件
                        foreach (var listener in _combatController.GetEventListeners())
                        {
                            listener.OnAnimatorIntSet(_combatController, varData.VariableName, varData.intValue);
                        }
                    }
                    break;
                    
                case AnimatorControllerParameterType.Float:
                    if (HasParameter(varData.VariableName, AnimatorControllerParameterType.Float))
                    {
                        oldValue = animator.GetFloat(varData.VariableName);
                        animator.SetFloat(varData.VariableName, varData.floatValue);
                        newValue = varData.floatValue;
                        
                        // 触发事件
                        foreach (var listener in _combatController.GetEventListeners())
                        {
                            listener.OnAnimatorFloatSet(_combatController, varData.VariableName, varData.floatValue);
                        }
                    }
                    break;
                    
                case AnimatorControllerParameterType.Trigger:
                    if (HasParameter(varData.VariableName, AnimatorControllerParameterType.Trigger))
                    {
                        if (varData.activateTrigger)
                        {
                            animator.SetTrigger(varData.VariableName);
                            
                            // 触发事件
                            foreach (var listener in _combatController.GetEventListeners())
                            {
                                listener.OnAnimatorTriggerSet(_combatController, varData.VariableName);
                            }
                        }
                        else
                        {
                            animator.ResetTrigger(varData.VariableName);
                        }
                    }
                    break;
            }
            
            // 触发通用变量变更事件
            if (oldValue != null && newValue != null)
            {
                foreach (var listener in _combatController.GetEventListeners())
                {
                    listener.OnAnimatorVariableChanged(_combatController, varData.VariableName, oldValue, newValue);
                }
            }
        }
        
        private void RestoreOriginalValue(AbilityEventObj_AnimatorVariable.AnimatorVariableData varData)
        {
            if (!originalValues.ContainsKey(varData.VariableName))
                return;
                
            var animator = GetTargetAnimator();
            if (animator == null)
                return;
                
            var originalValue = originalValues[varData.VariableName];
            
            switch (varData.VariableType)
            {
                case AnimatorControllerParameterType.Bool:
                    animator.SetBool(varData.VariableName, (bool)originalValue);
                    break;
                    
                case AnimatorControllerParameterType.Int:
                    animator.SetInteger(varData.VariableName, (int)originalValue);
                    break;
                    
                case AnimatorControllerParameterType.Float:
                    animator.SetFloat(varData.VariableName, (float)originalValue);
                    break;
            }
        }
        
        private bool HasParameter(string parameterName, AnimatorControllerParameterType type)
        {
            var animator = GetTargetAnimator();
            if (animator == null)
                return false;
            
            foreach (var parameter in animator.parameters)
            {
                if (parameter.name == parameterName && parameter.type == type)
                    return true;
            }
            
            Debug.LogWarning($"动画机中找不到名为 '{parameterName}' 的 {type} 类型参数！");
            return false;
        }
    }

    #if UNITY_EDITOR
    public class AbilityEventPreview_AnimatorVariable : AbilityEventPreview
    {
        AbilityEventObj_AnimatorVariable EventObj => (AbilityEventObj_AnimatorVariable)_EventObj;
        
        public AbilityEventPreview_AnimatorVariable(AbilityEventObj Obj) : base(Obj)
        {
        }
        
        public override void InitPreview()
        {
            base.InitPreview();
        }
        
        public override void PreviewUpdateFrame(float CurrentTimePercentage)
        {
            base.PreviewUpdateFrame(CurrentTimePercentage);
            
            // 在预览模式下可以显示将要设置的动画机变量信息
            if (IsOnStartFrame && _combatController?._animator != null)
            {
                foreach (var varData in EventObj.animatorVariables)
                {
                    if (!string.IsNullOrEmpty(varData.VariableName))
                    {
                        Debug.Log($"预览: 将设置动画机变量 {varData.VariableName} ({varData.VariableType}) = {GetVariableValueString(varData)}");
                    }
                }
            }
        }
        
        /// <summary>
        /// 获取变量值的字符串表示
        /// </summary>
        private string GetVariableValueString(AbilityEventObj_AnimatorVariable.AnimatorVariableData varData)
        {
            switch (varData.VariableType)
            {
                case AnimatorControllerParameterType.Bool:
                    return varData.boolValue.ToString();
                case AnimatorControllerParameterType.Int:
                    return varData.intValue.ToString();
                case AnimatorControllerParameterType.Float:
                    return varData.floatValue.ToString("F2");
                case AnimatorControllerParameterType.Trigger:
                    return varData.activateTrigger ? "激活" : "重置";
                default:
                    return "未知";
            }
        }
        
        public override void BackToStart()
        {
            base.BackToStart();
        }
    }
    #endif
} 
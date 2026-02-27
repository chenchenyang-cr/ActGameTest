using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace CombatEditor
{
    [Serializable]
    public class MethodParameter
    {
        public enum ParameterType
        {
            Int,
            Float,
            Bool,
            String,
            Vector3,
            GameObject,
            Transform,
            Color,
            AnimationCurve,
            Enum
        }

        public ParameterType parameterType;
        public string parameterName;

        public int intValue;
        public float floatValue;
        public bool boolValue;
        public string stringValue;
        public Vector3 vector3Value;
        public GameObject gameObjectValue;
        public Transform transformValue;
        public Color colorValue = Color.white;
        public AnimationCurve animationCurveValue = AnimationCurve.Linear(0f, 0f, 1f, 1f);

        public string enumTypeName;
        public int enumSelectedIndex;

        public object GetValue()
        {
            switch (parameterType)
            {
                case ParameterType.Int:
                    return intValue;
                case ParameterType.Float:
                    return floatValue;
                case ParameterType.Bool:
                    return boolValue;
                case ParameterType.String:
                    return stringValue;
                case ParameterType.Vector3:
                    return vector3Value;
                case ParameterType.GameObject:
                    return gameObjectValue;
                case ParameterType.Transform:
                    return transformValue;
                case ParameterType.Color:
                    return colorValue;
                case ParameterType.AnimationCurve:
                    return animationCurveValue;
                case ParameterType.Enum:
                    if (string.IsNullOrEmpty(enumTypeName)) return null;
                    var enumType = Type.GetType(enumTypeName);
                    if (enumType == null || !enumType.IsEnum) return null;
                    var enumValues = Enum.GetValues(enumType);
                    if (enumSelectedIndex < 0 || enumSelectedIndex >= enumValues.Length) return null;
                    return enumValues.GetValue(enumSelectedIndex);
                default:
                    return null;
            }
        }

        public Type GetParameterType()
        {
            switch (parameterType)
            {
                case ParameterType.Int:
                    return typeof(int);
                case ParameterType.Float:
                    return typeof(float);
                case ParameterType.Bool:
                    return typeof(bool);
                case ParameterType.String:
                    return typeof(string);
                case ParameterType.Vector3:
                    return typeof(Vector3);
                case ParameterType.GameObject:
                    return typeof(GameObject);
                case ParameterType.Transform:
                    return typeof(Transform);
                case ParameterType.Color:
                    return typeof(Color);
                case ParameterType.AnimationCurve:
                    return typeof(AnimationCurve);
                case ParameterType.Enum:
                    return string.IsNullOrEmpty(enumTypeName) ? typeof(Enum) : Type.GetType(enumTypeName) ?? typeof(Enum);
                default:
                    return typeof(object);
            }
        }
    }

    [AbilityEvent]
    [CreateAssetMenu(menuName = "AbilityEvents / Execute Method")]
    public class AbilityEventObj_ExecuteMethod : AbilityEventObj
    {
        [Header("Target")]
        public TargetSelectionMode targetMode = TargetSelectionMode.CurrentCombatController;
        public GameObject specificTarget;

        [Header("Method")]
        public string targetClassName = "";
        public string methodName = "";
        public List<MethodParameter> parameters = new List<MethodParameter>();

        [Header("Explicit Binding")]
        [Tooltip("Relative path from target root object. Empty means root object itself.")]
        public string boundObjectPath = "";
        [Tooltip("Assembly qualified or full type name of selected component.")]
        public string boundComponentTypeName = "";
        [Tooltip("Index among components with the same type on the bound object.")]
        public int boundComponentIndex = 0;

        [Serializable]
        public enum TargetSelectionMode
        {
            CurrentCombatController,
            SpecificTarget,
            FindByTag,
            FindByName
        }

        [Header("Find Settings")]
        public string searchValue = "";

        public override EventTimeType GetEventTimeType()
        {
            return EventTimeType.EventTime;
        }

        public override AbilityEventEffect Initialize()
        {
            return new AbilityEventEffect_ExecuteMethod(this);
        }

#if UNITY_EDITOR
        public override AbilityEventPreview InitializePreview()
        {
            return new AbilityEventPreview_ExecuteMethod(this);
        }
#endif
    }

    public class AbilityEventEffect_ExecuteMethod : AbilityEventEffect
    {
        private AbilityEventObj_ExecuteMethod EventObj => (AbilityEventObj_ExecuteMethod)_EventObj;

        public AbilityEventEffect_ExecuteMethod(AbilityEventObj initObj) : base(initObj)
        {
            _EventObj = initObj;
        }

        public override void StartEffect()
        {
            base.StartEffect();
            ExecuteMethod();
        }

        protected void ExecuteMethod()
        {
            if (string.IsNullOrEmpty(EventObj.targetClassName) || string.IsNullOrEmpty(EventObj.methodName))
            {
                Debug.LogWarning("[ExecuteMethod] Missing targetClassName or methodName.");
                return;
            }

            var rootTarget = FindTargetGameObject();
            if (rootTarget == null)
            {
                Debug.LogWarning($"[ExecuteMethod] Cannot resolve target root, mode={EventObj.targetMode}.");
                return;
            }

            var targetObject = ResolveBoundTargetObject(rootTarget);
            if (targetObject == null)
            {
                Debug.LogWarning($"[ExecuteMethod] Bound target object not found or inactive. path='{EventObj.boundObjectPath}' root='{rootTarget.name}'.");
                return;
            }

            var targetComponent = ResolveBoundComponent(rootTarget, targetObject);
            if (targetComponent == null)
            {
                Debug.LogWarning($"[ExecuteMethod] Target component not found. class='{EventObj.targetClassName}', boundType='{EventObj.boundComponentTypeName}'.");
                return;
            }

            var method = ResolveMethod(targetComponent.GetType());
            if (method == null)
            {
                Debug.LogWarning($"[ExecuteMethod] Method not found: {EventObj.methodName} on {targetComponent.GetType().FullName}.");
                return;
            }

            var args = BuildArguments(method);
            if (args == null)
            {
                Debug.LogWarning($"[ExecuteMethod] Parameter mismatch for method {method.Name}.");
                return;
            }

            try
            {
                method.Invoke(targetComponent, args);
            }
            catch (TargetInvocationException tie)
            {
                var inner = tie.InnerException ?? tie;
                Debug.LogError($"[ExecuteMethod] Invoke failed: {inner.GetType().Name} - {inner.Message}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[ExecuteMethod] Invoke failed: {e.GetType().Name} - {e.Message}");
            }
        }

        private MethodInfo ResolveMethod(Type targetType)
        {
            string cleanMethodName = EventObj.methodName;
            int index = cleanMethodName.IndexOf('(');
            if (index >= 0)
            {
                cleanMethodName = cleanMethodName.Substring(0, index);
            }

            var candidates = targetType.GetMethods(BindingFlags.Public | BindingFlags.Instance);
            foreach (var method in candidates)
            {
                if (!string.Equals(method.Name, cleanMethodName, StringComparison.Ordinal))
                {
                    continue;
                }

                var methodParams = method.GetParameters();
                if (methodParams.Length != EventObj.parameters.Count)
                {
                    continue;
                }

                bool ok = true;
                for (int i = 0; i < methodParams.Length; i++)
                {
                    var expected = methodParams[i].ParameterType;
                    var provided = EventObj.parameters[i].GetParameterType();
                    if (!IsParameterTypeCompatible(expected, provided))
                    {
                        ok = false;
                        break;
                    }
                }

                if (ok)
                {
                    return method;
                }
            }

            return null;
        }

        private object[] BuildArguments(MethodInfo method)
        {
            var methodParams = method.GetParameters();
            if (methodParams.Length != EventObj.parameters.Count)
            {
                return null;
            }

            var args = new object[EventObj.parameters.Count];
            for (int i = 0; i < EventObj.parameters.Count; i++)
            {
                object value = EventObj.parameters[i].GetValue();
                Type expected = methodParams[i].ParameterType;

                if (value == null)
                {
                    if (expected.IsValueType && Nullable.GetUnderlyingType(expected) == null)
                    {
                        return null;
                    }

                    args[i] = null;
                    continue;
                }

                if (!expected.IsInstanceOfType(value))
                {
                    return null;
                }

                args[i] = value;
            }

            return args;
        }

        private static bool IsParameterTypeCompatible(Type expected, Type provided)
        {
            if (expected == provided)
            {
                return true;
            }

            if (provided == null)
            {
                return false;
            }

            if (expected.IsAssignableFrom(provided))
            {
                return true;
            }

            return false;
        }

        private Component ResolveBoundComponent(GameObject rootTarget, GameObject resolvedObject)
        {
            if (!string.IsNullOrEmpty(EventObj.boundComponentTypeName))
            {
                var bound = GetComponentByTypeName(resolvedObject, EventObj.boundComponentTypeName, EventObj.boundComponentIndex);
                if (bound != null)
                {
                    return bound;
                }
            }

            if (!string.IsNullOrEmpty(EventObj.targetClassName))
            {
                var onResolved = GetComponentByTypeName(resolvedObject, EventObj.targetClassName, 0);
                if (onResolved != null)
                {
                    return onResolved;
                }

                if (string.IsNullOrEmpty(EventObj.boundObjectPath))
                {
                    foreach (var go in EnumerateRootAndDirectChildren(rootTarget))
                    {
                        var found = GetComponentByTypeName(go, EventObj.targetClassName, 0);
                        if (found != null)
                        {
                            return found;
                        }
                    }
                }
            }

            return null;
        }

        private static Component GetComponentByTypeName(GameObject go, string typeName, int matchIndex)
        {
            if (go == null || string.IsNullOrEmpty(typeName))
            {
                return null;
            }

            var components = go.GetComponents<MonoBehaviour>();
            int currentMatch = 0;
            foreach (var component in components)
            {
                if (component == null) continue;

                Type type = component.GetType();
                if (type.AssemblyQualifiedName == typeName || type.FullName == typeName || type.Name == typeName)
                {
                    if (currentMatch == Mathf.Max(0, matchIndex))
                    {
                        return component;
                    }

                    currentMatch++;
                }
            }

            return null;
        }

        private GameObject ResolveBoundTargetObject(GameObject rootTarget)
        {
            if (rootTarget == null || !rootTarget.activeInHierarchy)
            {
                return null;
            }

            if (string.IsNullOrEmpty(EventObj.boundObjectPath))
            {
                return rootTarget;
            }

            string[] segments = EventObj.boundObjectPath.Split('/');
            string firstNonEmpty = null;
            int segmentCount = 0;
            foreach (var raw in segments)
            {
                string segment = raw.Trim();
                if (string.IsNullOrEmpty(segment))
                {
                    continue;
                }

                segmentCount++;
                if (firstNonEmpty == null)
                {
                    firstNonEmpty = segment;
                }
            }

            // Only support root/self or direct child binding.
            if (segmentCount > 1)
            {
                return null;
            }

            if (segmentCount == 0)
            {
                return rootTarget;
            }

            Transform root = rootTarget.transform;
            for (int i = 0; i < root.childCount; i++)
            {
                Transform child = root.GetChild(i);
                if (!child.gameObject.activeInHierarchy) continue;
                if (child.name == firstNonEmpty)
                {
                    return child.gameObject;
                }
            }

            return null;
        }

        private static IEnumerable<GameObject> EnumerateRootAndDirectChildren(GameObject root)
        {
            if (root == null || !root.activeInHierarchy)
            {
                yield break;
            }

            yield return root;
            Transform rootTransform = root.transform;
            for (int i = 0; i < rootTransform.childCount; i++)
            {
                Transform child = rootTransform.GetChild(i);
                if (child.gameObject.activeInHierarchy)
                {
                    yield return child.gameObject;
                }
            }
        }

        private GameObject FindTargetGameObject()
        {
            switch (EventObj.targetMode)
            {
                case AbilityEventObj_ExecuteMethod.TargetSelectionMode.CurrentCombatController:
                    return _combatController != null ? _combatController.gameObject : null;

                case AbilityEventObj_ExecuteMethod.TargetSelectionMode.SpecificTarget:
                    return EventObj.specificTarget;

                case AbilityEventObj_ExecuteMethod.TargetSelectionMode.FindByTag:
                    if (string.IsNullOrEmpty(EventObj.searchValue)) return null;
                    return GameObject.FindGameObjectWithTag(EventObj.searchValue);

                case AbilityEventObj_ExecuteMethod.TargetSelectionMode.FindByName:
                    if (string.IsNullOrEmpty(EventObj.searchValue)) return null;
                    return GameObject.Find(EventObj.searchValue);

                default:
                    return _combatController != null ? _combatController.gameObject : null;
            }
        }
    }

#if UNITY_EDITOR
    public class AbilityEventPreview_ExecuteMethod : AbilityEventPreview
    {
        private AbilityEventObj_ExecuteMethod EventObj => (AbilityEventObj_ExecuteMethod)_EventObj;

        public AbilityEventPreview_ExecuteMethod(AbilityEventObj obj) : base(obj)
        {
            _EventObj = obj;
        }

        public override void InitPreview()
        {
            base.InitPreview();
            Debug.Log($"[ExecuteMethod Preview] {EventObj.targetClassName}.{EventObj.methodName}");
        }
    }
#endif
}

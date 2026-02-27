using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace CombatEditor
{
    [CustomEditor(typeof(AbilityEventObj_ExecuteMethod))]
    public class AbilityEventObj_ExecuteMethodEditor : Editor
    {
        private SerializedProperty targetModeProperty;
        private SerializedProperty specificTargetProperty;
        private SerializedProperty searchValueProperty;

        private SerializedProperty targetClassNameProperty;
        private SerializedProperty methodNameProperty;
        private SerializedProperty parametersProperty;
        private SerializedProperty boundObjectPathProperty;
        private SerializedProperty boundComponentTypeNameProperty;
        private SerializedProperty boundComponentIndexProperty;
        private SerializedProperty nameProperty;

        private readonly List<string> _objectPaths = new List<string>();
        private readonly List<string> _objectLabels = new List<string>();
        private readonly List<Component> _components = new List<Component>();
        private readonly List<string> _componentLabels = new List<string>();
        private readonly List<MethodInfo> _methods = new List<MethodInfo>();
        private readonly List<string> _methodLabels = new List<string>();

        private void OnEnable()
        {
            targetModeProperty = serializedObject.FindProperty("targetMode");
            specificTargetProperty = serializedObject.FindProperty("specificTarget");
            searchValueProperty = serializedObject.FindProperty("searchValue");

            targetClassNameProperty = serializedObject.FindProperty("targetClassName");
            methodNameProperty = serializedObject.FindProperty("methodName");
            parametersProperty = serializedObject.FindProperty("parameters");
            boundObjectPathProperty = serializedObject.FindProperty("boundObjectPath");
            boundComponentTypeNameProperty = serializedObject.FindProperty("boundComponentTypeName");
            boundComponentIndexProperty = serializedObject.FindProperty("boundComponentIndex");
            nameProperty = serializedObject.FindProperty("m_Name");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawTargetSection();
            EditorGUILayout.Space();
            DrawBindingSection();
            EditorGUILayout.Space();
            DrawMethodSection();

            SyncNameWithMethod();
            serializedObject.ApplyModifiedProperties();
        }

        private void DrawTargetSection()
        {
            EditorGUILayout.LabelField("Target", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(targetModeProperty);

            var mode = (AbilityEventObj_ExecuteMethod.TargetSelectionMode)targetModeProperty.enumValueIndex;
            switch (mode)
            {
                case AbilityEventObj_ExecuteMethod.TargetSelectionMode.SpecificTarget:
                    EditorGUILayout.PropertyField(specificTargetProperty);
                    break;
                case AbilityEventObj_ExecuteMethod.TargetSelectionMode.FindByTag:
                    EditorGUILayout.PropertyField(searchValueProperty, new GUIContent("Tag"));
                    break;
                case AbilityEventObj_ExecuteMethod.TargetSelectionMode.FindByName:
                    EditorGUILayout.PropertyField(searchValueProperty, new GUIContent("Name"));
                    break;
            }
        }

        private void DrawBindingSection()
        {
            EditorGUILayout.LabelField("Binding", EditorStyles.boldLabel);

            GameObject root = ResolveRootTargetObject();
            if (root == null)
            {
                EditorGUILayout.HelpBox("Cannot resolve target object in editor. You can still type values manually.", MessageType.Info);
                EditorGUILayout.PropertyField(boundObjectPathProperty, new GUIContent("Bound Object Path"));
                EditorGUILayout.PropertyField(boundComponentTypeNameProperty, new GUIContent("Bound Component Type"));
                if (boundComponentIndexProperty != null) EditorGUILayout.PropertyField(boundComponentIndexProperty, new GUIContent("Bound Component Index"));
                EditorGUILayout.PropertyField(targetClassNameProperty, new GUIContent("Target Class"));
                return;
            }

            RefreshObjectCandidates(root);
            int objectIndex = Mathf.Max(0, _objectPaths.IndexOf(boundObjectPathProperty.stringValue));
            int newObjectIndex = EditorGUILayout.Popup("Target Object", objectIndex, _objectLabels.ToArray());
            if (newObjectIndex != objectIndex)
            {
                boundObjectPathProperty.stringValue = _objectPaths[newObjectIndex];
                boundComponentTypeNameProperty.stringValue = string.Empty;
                if (boundComponentIndexProperty != null) boundComponentIndexProperty.intValue = 0;
                targetClassNameProperty.stringValue = string.Empty;
                methodNameProperty.stringValue = string.Empty;
                parametersProperty.ClearArray();
            }

            GameObject selectedObject = ResolveBoundObject(root, boundObjectPathProperty.stringValue);
            if (selectedObject == null)
            {
                EditorGUILayout.HelpBox("Selected bound object is inactive or missing.", MessageType.Warning);
                return;
            }

            RefreshComponentCandidates(selectedObject);
            int componentIndex = FindComponentIndex(boundComponentTypeNameProperty.stringValue, boundComponentIndexProperty != null ? boundComponentIndexProperty.intValue : 0);
            int newComponentIndex = EditorGUILayout.Popup("Target Component", componentIndex, _componentLabels.ToArray());
            if (newComponentIndex != componentIndex && _components.Count > 0)
            {
                var compType = _components[newComponentIndex].GetType();
                boundComponentTypeNameProperty.stringValue = compType.AssemblyQualifiedName;
                targetClassNameProperty.stringValue = compType.FullName;
                if (boundComponentIndexProperty != null) boundComponentIndexProperty.intValue = GetComponentMatchIndex(newComponentIndex);
                methodNameProperty.stringValue = string.Empty;
                parametersProperty.ClearArray();
            }

            if (!string.IsNullOrEmpty(boundComponentTypeNameProperty.stringValue) &&
                string.IsNullOrEmpty(targetClassNameProperty.stringValue))
            {
                var type = ResolveType(boundComponentTypeNameProperty.stringValue);
                if (type != null)
                {
                    targetClassNameProperty.stringValue = type.FullName;
                }
            }

            EditorGUILayout.LabelField("Target Class", targetClassNameProperty.stringValue);
            EditorGUILayout.LabelField("Bound Path", string.IsNullOrEmpty(boundObjectPathProperty.stringValue) ? "(Self)" : boundObjectPathProperty.stringValue);
        }

        private void DrawMethodSection()
        {
            EditorGUILayout.LabelField("Method", EditorStyles.boldLabel);

            GameObject root = ResolveRootTargetObject();
            GameObject selectedObject = root == null ? null : ResolveBoundObject(root, boundObjectPathProperty.stringValue);
            Component selectedComponent = selectedObject == null ? null : ResolveSelectedComponent(selectedObject);

            if (selectedComponent == null)
            {
                EditorGUILayout.HelpBox("Select a valid target object and component first. Method is auto-discovered only.", MessageType.Info);
                return;
            }

            RefreshMethodCandidates(selectedComponent.GetType());
            int currentMethodIndex = _methodLabels.IndexOf(methodNameProperty.stringValue);
            if (currentMethodIndex < 0) currentMethodIndex = 0;

            int newMethodIndex = EditorGUILayout.Popup("Method", currentMethodIndex, _methodLabels.Count == 0 ? new[] { "(No Methods)" } : _methodLabels.ToArray());
            if (_methodLabels.Count > 0 && newMethodIndex != currentMethodIndex)
            {
                methodNameProperty.stringValue = _methodLabels[newMethodIndex];
                SetupParametersForMethod(_methods[newMethodIndex]);
                SyncNameWithMethod();
            }

            if (_methods.Count > 0 && string.IsNullOrEmpty(methodNameProperty.stringValue))
            {
                methodNameProperty.stringValue = _methodLabels[0];
                SetupParametersForMethod(_methods[0]);
                SyncNameWithMethod();
            }

            DrawParameters();
        }

        private void DrawParameters()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Parameters", EditorStyles.boldLabel);

            for (int i = 0; i < parametersProperty.arraySize; i++)
            {
                SerializedProperty param = parametersProperty.GetArrayElementAtIndex(i);
                DrawParameterField(param, i);
            }
        }

        private void DrawParameterField(SerializedProperty parameter, int index)
        {
            EditorGUILayout.BeginVertical("box");
            SerializedProperty parameterType = parameter.FindPropertyRelative("parameterType");
            SerializedProperty parameterName = parameter.FindPropertyRelative("parameterName");
            EditorGUILayout.LabelField($"{index + 1}. {parameterName.stringValue}", EditorStyles.boldLabel);

            MethodParameter.ParameterType type = (MethodParameter.ParameterType)parameterType.enumValueIndex;
            switch (type)
            {
                case MethodParameter.ParameterType.Int:
                    EditorGUILayout.PropertyField(parameter.FindPropertyRelative("intValue"), new GUIContent("Value"));
                    break;
                case MethodParameter.ParameterType.Float:
                    EditorGUILayout.PropertyField(parameter.FindPropertyRelative("floatValue"), new GUIContent("Value"));
                    break;
                case MethodParameter.ParameterType.Bool:
                    EditorGUILayout.PropertyField(parameter.FindPropertyRelative("boolValue"), new GUIContent("Value"));
                    break;
                case MethodParameter.ParameterType.String:
                    EditorGUILayout.PropertyField(parameter.FindPropertyRelative("stringValue"), new GUIContent("Value"));
                    break;
                case MethodParameter.ParameterType.Vector3:
                    EditorGUILayout.PropertyField(parameter.FindPropertyRelative("vector3Value"), new GUIContent("Value"));
                    break;
                case MethodParameter.ParameterType.GameObject:
                    EditorGUILayout.PropertyField(parameter.FindPropertyRelative("gameObjectValue"), new GUIContent("Value"));
                    break;
                case MethodParameter.ParameterType.Transform:
                    EditorGUILayout.PropertyField(parameter.FindPropertyRelative("transformValue"), new GUIContent("Value"));
                    break;
                case MethodParameter.ParameterType.Color:
                    EditorGUILayout.PropertyField(parameter.FindPropertyRelative("colorValue"), new GUIContent("Value"));
                    break;
                case MethodParameter.ParameterType.AnimationCurve:
                    EditorGUILayout.PropertyField(parameter.FindPropertyRelative("animationCurveValue"), new GUIContent("Value"));
                    break;
                case MethodParameter.ParameterType.Enum:
                    DrawEnumParameterField(parameter);
                    break;
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawEnumParameterField(SerializedProperty parameter)
        {
            SerializedProperty enumTypeNameProp = parameter.FindPropertyRelative("enumTypeName");
            SerializedProperty enumSelectedIndexProp = parameter.FindPropertyRelative("enumSelectedIndex");

            if (string.IsNullOrEmpty(enumTypeNameProp.stringValue))
            {
                EditorGUILayout.HelpBox("Enum type not set.", MessageType.Warning);
                return;
            }

            Type enumType = ResolveType(enumTypeNameProp.stringValue);
            if (enumType == null || !enumType.IsEnum)
            {
                EditorGUILayout.HelpBox($"Cannot resolve enum type: {enumTypeNameProp.stringValue}", MessageType.Error);
                return;
            }

            string[] enumNames = Enum.GetNames(enumType);
            int currentIndex = Mathf.Clamp(enumSelectedIndexProp.intValue, 0, Mathf.Max(0, enumNames.Length - 1));
            int newIndex = EditorGUILayout.Popup("Value", currentIndex, enumNames);
            enumSelectedIndexProp.intValue = newIndex;
        }

        private void RefreshObjectCandidates(GameObject root)
        {
            _objectPaths.Clear();
            _objectLabels.Clear();
            if (root == null || !root.activeInHierarchy) return;

            _objectPaths.Add(string.Empty);
            _objectLabels.Add($"{root.name} (Self)");

            Transform rt = root.transform;
            for (int i = 0; i < rt.childCount; i++)
            {
                Transform child = rt.GetChild(i);
                if (!child.gameObject.activeInHierarchy) continue;
                _objectPaths.Add(child.name);
                _objectLabels.Add(child.name);
            }
        }

        private void RefreshComponentCandidates(GameObject targetObject)
        {
            _components.Clear();
            _componentLabels.Clear();

            var comps = targetObject.GetComponents<MonoBehaviour>();
            foreach (var comp in comps)
            {
                if (comp == null) continue;
                _components.Add(comp);
                int sameTypeIndex = _components.Count(c => c != null && c.GetType() == comp.GetType()) - 1;
                _componentLabels.Add($"{comp.GetType().FullName}  [#{sameTypeIndex}]");
            }

            if (_componentLabels.Count == 0)
            {
                _componentLabels.Add("(No MonoBehaviour)");
            }
        }

        private int FindComponentIndex(string typeName, int typeMatchIndex)
        {
            if (_components.Count == 0) return 0;
            int currentMatch = 0;
            for (int i = 0; i < _components.Count; i++)
            {
                var t = _components[i].GetType();
                if (t.AssemblyQualifiedName == typeName || t.FullName == typeName || t.Name == typeName)
                {
                    if (currentMatch == Mathf.Max(0, typeMatchIndex)) return i;
                    currentMatch++;
                }
            }
            return 0;
        }

        private int GetComponentMatchIndex(int selectedGlobalIndex)
        {
            if (selectedGlobalIndex < 0 || selectedGlobalIndex >= _components.Count) return 0;
            Type targetType = _components[selectedGlobalIndex].GetType();
            int match = 0;
            for (int i = 0; i < selectedGlobalIndex; i++)
            {
                if (_components[i] != null && _components[i].GetType() == targetType)
                {
                    match++;
                }
            }
            return match;
        }

        private void RefreshMethodCandidates(Type componentType)
        {
            _methods.Clear();
            _methodLabels.Clear();

            var methods = componentType
                .GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Where(IsAllowedMethod)
                .OrderBy(m => m.Name)
                .ThenBy(m => m.GetParameters().Length)
                .ToList();

            foreach (var method in methods)
            {
                _methods.Add(method);
                _methodLabels.Add(GetMethodSignature(method));
            }
        }

        private static bool IsAllowedMethod(MethodInfo method)
        {
            if (method.IsSpecialName || method.IsGenericMethod) return false;
            if (method.DeclaringType == typeof(MonoBehaviour) || method.DeclaringType == typeof(Component) || method.DeclaringType == typeof(UnityEngine.Object) || method.DeclaringType == typeof(object)) return false;

            string n = method.Name;
            if (n == "Awake" || n == "Start" || n == "Update" || n == "LateUpdate" || n == "FixedUpdate" || n == "OnEnable" || n == "OnDisable" || n == "OnDestroy" ||
                n == "GetType" || n == "ToString" || n == "Equals" || n == "GetHashCode")
            {
                return false;
            }

            return true;
        }

        private void SetupParametersForMethod(MethodInfo method)
        {
            parametersProperty.ClearArray();

            var parameters = method.GetParameters();
            for (int i = 0; i < parameters.Length; i++)
            {
                parametersProperty.InsertArrayElementAtIndex(i);
                SerializedProperty p = parametersProperty.GetArrayElementAtIndex(i);
                p.FindPropertyRelative("parameterName").stringValue = parameters[i].Name;

                MethodParameter.ParameterType paramType = GetParameterTypeFromType(parameters[i].ParameterType);
                p.FindPropertyRelative("parameterType").enumValueIndex = (int)paramType;

                if (paramType == MethodParameter.ParameterType.Enum)
                {
                    p.FindPropertyRelative("enumTypeName").stringValue = parameters[i].ParameterType.AssemblyQualifiedName;
                    p.FindPropertyRelative("enumSelectedIndex").intValue = 0;
                }
            }
        }

        private static MethodParameter.ParameterType GetParameterTypeFromType(Type type)
        {
            if (type == typeof(int)) return MethodParameter.ParameterType.Int;
            if (type == typeof(float)) return MethodParameter.ParameterType.Float;
            if (type == typeof(bool)) return MethodParameter.ParameterType.Bool;
            if (type == typeof(string)) return MethodParameter.ParameterType.String;
            if (type == typeof(Vector3)) return MethodParameter.ParameterType.Vector3;
            if (type == typeof(GameObject)) return MethodParameter.ParameterType.GameObject;
            if (type == typeof(Transform)) return MethodParameter.ParameterType.Transform;
            if (type == typeof(Color)) return MethodParameter.ParameterType.Color;
            if (type == typeof(AnimationCurve)) return MethodParameter.ParameterType.AnimationCurve;
            if (type.IsEnum) return MethodParameter.ParameterType.Enum;
            return MethodParameter.ParameterType.String;
        }

        private static string GetMethodSignature(MethodInfo method)
        {
            ParameterInfo[] ps = method.GetParameters();
            if (ps.Length == 0) return method.Name + "()";
            return method.Name + "(" + string.Join(", ", ps.Select(p => GetSimpleTypeName(p.ParameterType))) + ")";
        }

        private static string GetSimpleTypeName(Type type)
        {
            if (type == typeof(int)) return "int";
            if (type == typeof(float)) return "float";
            if (type == typeof(bool)) return "bool";
            if (type == typeof(string)) return "string";
            if (type == typeof(Vector3)) return "Vector3";
            if (type == typeof(GameObject)) return "GameObject";
            if (type == typeof(Transform)) return "Transform";
            if (type == typeof(Color)) return "Color";
            if (type == typeof(AnimationCurve)) return "AnimationCurve";
            return type.Name;
        }

        private Component ResolveSelectedComponent(GameObject selectedObject)
        {
            if (selectedObject == null) return null;
            string typeName = boundComponentTypeNameProperty.stringValue;
            int wantedMatchIndex = boundComponentIndexProperty != null ? boundComponentIndexProperty.intValue : 0;
            if (string.IsNullOrEmpty(typeName)) return null;

            var comps = selectedObject.GetComponents<MonoBehaviour>();
            int currentMatch = 0;
            foreach (var comp in comps)
            {
                if (comp == null) continue;
                var t = comp.GetType();
                if (t.AssemblyQualifiedName == typeName || t.FullName == typeName || t.Name == typeName)
                {
                    if (currentMatch == Mathf.Max(0, wantedMatchIndex)) return comp;
                    currentMatch++;
                }
            }

            return null;
        }

        private static Type ResolveType(string typeName)
        {
            if (string.IsNullOrEmpty(typeName)) return null;
            Type t = Type.GetType(typeName);
            if (t != null) return t;

            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                t = asm.GetType(typeName);
                if (t != null) return t;
            }

            return null;
        }

        private static GameObject ResolveBoundObject(GameObject root, string path)
        {
            if (root == null || !root.activeInHierarchy) return null;
            if (string.IsNullOrEmpty(path)) return root;

            if (path.Contains("/")) return null;
            Transform rt = root.transform;
            for (int i = 0; i < rt.childCount; i++)
            {
                Transform child = rt.GetChild(i);
                if (!child.gameObject.activeInHierarchy) continue;
                if (child.name == path) return child.gameObject;
            }
            return null;
        }

        private GameObject ResolveRootTargetObject()
        {
            var mode = (AbilityEventObj_ExecuteMethod.TargetSelectionMode)targetModeProperty.enumValueIndex;
            switch (mode)
            {
                case AbilityEventObj_ExecuteMethod.TargetSelectionMode.CurrentCombatController:
                    {
                        var controller = GetCurrentSelectedCombatController();
                        return controller != null ? controller.gameObject : null;
                    }
                case AbilityEventObj_ExecuteMethod.TargetSelectionMode.SpecificTarget:
                    return specificTargetProperty.objectReferenceValue as GameObject;
                case AbilityEventObj_ExecuteMethod.TargetSelectionMode.FindByTag:
                    if (string.IsNullOrEmpty(searchValueProperty.stringValue)) return null;
                    return GameObject.FindGameObjectWithTag(searchValueProperty.stringValue);
                case AbilityEventObj_ExecuteMethod.TargetSelectionMode.FindByName:
                    if (string.IsNullOrEmpty(searchValueProperty.stringValue)) return null;
                    return GameObject.Find(searchValueProperty.stringValue);
                default:
                    return null;
            }
        }

        private static CombatController GetCurrentSelectedCombatController()
        {
            if (CombatEditorUtility.EditorExist())
            {
                var editor = CombatEditorUtility.GetCurrentEditor();
                if (editor != null && editor.SelectedController != null)
                {
                    return editor.SelectedController;
                }
            }

            return FindObjectOfType<CombatController>();
        }

        private void SyncNameWithMethod()
        {
            if (nameProperty == null || string.IsNullOrEmpty(methodNameProperty.stringValue))
            {
                return;
            }

            string displayName = ExtractMethodDisplayName(methodNameProperty.stringValue);
            if (nameProperty.stringValue != displayName)
            {
                nameProperty.stringValue = displayName;
            }
        }

        private static string ExtractMethodDisplayName(string methodSignature)
        {
            if (string.IsNullOrEmpty(methodSignature))
            {
                return string.Empty;
            }

            int idx = methodSignature.IndexOf('(');
            return idx > 0 ? methodSignature.Substring(0, idx) : methodSignature;
        }
    }
}

using UnityEngine;
using UnityEditor;

namespace CombatEditor
{
    [CustomEditor(typeof(AbilityEventObj_CreateObjWithHandle))]
    public class AbilityEventObj_CreateObjWithHandleEditor : Editor
    {
        private SerializedProperty objDataProperty;
        private SerializedProperty destroyOnEndProperty;
        private SerializedProperty destroyDelayProperty;

        private void OnEnable()
        {
            objDataProperty = serializedObject.FindProperty("ObjData");
            destroyOnEndProperty = serializedObject.FindProperty("DestroyOnEnd");
            destroyDelayProperty = serializedObject.FindProperty("DestroyDelay");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // 开始检查是否有任何GUI变化
            EditorGUI.BeginChangeCheck();

            // Draw the custom ObjData with enhanced parent selection
            DrawObjDataWithParentSelection();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Destruction Settings", EditorStyles.boldLabel);
            
            EditorGUILayout.PropertyField(destroyOnEndProperty, new GUIContent("Destroy On End", "Whether to destroy the created object when the effect ends"));
            
            if (destroyOnEndProperty.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(destroyDelayProperty, new GUIContent("Destroy Delay", "Time delay before destroying the object (0 for immediate)"));
                EditorGUI.indentLevel--;
            }

            // 如果有任何变化，应用修改
            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(serializedObject.targetObject);
                
                // 在CombatInspector环境中强制刷新
                UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
                
                // 确保资产被保存
                AssetDatabase.SaveAssets();
            }
        }

        private void DrawObjDataWithParentSelection()
        {
            EditorGUILayout.LabelField("Object Creation Settings", EditorStyles.boldLabel);
            
            // Draw basic ObjData properties
            var targetObjProp = objDataProperty.FindPropertyRelative("TargetObj");
            var controlTypeProp = objDataProperty.FindPropertyRelative("controlType");
            var offsetProp = objDataProperty.FindPropertyRelative("Offset");
            var rotProp = objDataProperty.FindPropertyRelative("Rot");
            var targetNodeProp = objDataProperty.FindPropertyRelative("TargetNode");
            var followNodeProp = objDataProperty.FindPropertyRelative("FollowNode");
            var rotateByNodeProp = objDataProperty.FindPropertyRelative("RotateByNode");
            var useCustomTargetProp = objDataProperty.FindPropertyRelative("UseCustomTarget");
            var customTargetNameProp = objDataProperty.FindPropertyRelative("CustomTargetName");
            
            // Parent settings properties
            var useCustomParentProp = objDataProperty.FindPropertyRelative("UseCustomParent");
            var customParentTransformProp = objDataProperty.FindPropertyRelative("CustomParentTransform");

            EditorGUILayout.PropertyField(targetObjProp, new GUIContent("Target Object", "The prefab to instantiate"));
            EditorGUILayout.PropertyField(controlTypeProp, new GUIContent("Control Type", "How the handle behaves in the scene view"));
            EditorGUILayout.PropertyField(offsetProp, new GUIContent("Offset", "Position offset from the target"));
            EditorGUILayout.PropertyField(rotProp, new GUIContent("Rotation", "Rotation of the instantiated object"));
            EditorGUILayout.PropertyField(targetNodeProp, new GUIContent("Target Node", "Which node to attach to"));
            EditorGUILayout.PropertyField(followNodeProp, new GUIContent("Follow Node", "Whether to follow the node's movement"));
            EditorGUILayout.PropertyField(rotateByNodeProp, new GUIContent("Rotate By Node", "Whether to rotate with the node"));
            
            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(useCustomTargetProp, new GUIContent("Use Custom Target", "Use a custom target by name instead of character node"));
            if (useCustomTargetProp.boolValue)
            {
                EditorGUI.indentLevel++;
                
                // Simple string input for target name
                EditorGUILayout.PropertyField(customTargetNameProp, new GUIContent("Target Name", "Name of the target object in the scene"));
                
                // Show help box based on target name
                if (string.IsNullOrEmpty(customTargetNameProp.stringValue))
                {
                    EditorGUILayout.HelpBox("Please enter the name of the target object in the scene.", MessageType.Warning);
                }
                else
                {
                    EditorGUILayout.HelpBox($"Will look for object named: {customTargetNameProp.stringValue}", MessageType.Info);
                }
                
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Parent Object Settings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(useCustomParentProp, new GUIContent("Use Custom Parent", "Set a custom parent for the instantiated object"));
            
            if (useCustomParentProp.boolValue)
            {
                EditorGUI.indentLevel++;
                
                EditorGUILayout.BeginHorizontal();
                
                // Use ObjectField to allow dragging from scene
                Transform currentParent = (Transform)customParentTransformProp.objectReferenceValue;
                
                EditorGUI.BeginChangeCheck();
                Transform newParent = (Transform)EditorGUILayout.ObjectField(
                    new GUIContent("Parent Transform", "The parent transform for the created object"), 
                    currentParent, 
                    typeof(Transform), 
                    true  // Allow scene objects
                );
                
                // Update property if changed
                if (EditorGUI.EndChangeCheck())
                {
                    Debug.Log($"CustomParent changed from {(currentParent != null ? currentParent.name : "null")} to {(newParent != null ? newParent.name : "null")}");
                    customParentTransformProp.objectReferenceValue = newParent;
                    serializedObject.ApplyModifiedProperties();
                    EditorUtility.SetDirty(serializedObject.targetObject);
                    
                    // 强制重绘所有视图，确保在CombatInspector中也能更新
                    UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
                    
                    // 确保资产被保存
                    AssetDatabase.SaveAssets();
                }
                
                // Add a button to select from scene
                if (GUILayout.Button("Select from Scene", GUILayout.Width(120)))
                {
                    ShowParentSelectionWindow(customParentTransformProp);
                }
                EditorGUILayout.EndHorizontal();
                
                // Show current parent info
                if (customParentTransformProp.objectReferenceValue != null)
                {
                    Transform parent = (Transform)customParentTransformProp.objectReferenceValue;
                    EditorGUILayout.HelpBox($"Selected Parent: {parent.name}\nPath: {GetTransformPath(parent)}", MessageType.Info);
                }
                else
                {
                    EditorGUILayout.HelpBox("No parent selected. Object will be created at root level.", MessageType.Warning);
                }
                
                EditorGUI.indentLevel--;
            }
        }

        private void ShowParentSelectionWindow(SerializedProperty parentProperty)
        {
            if (parentProperty == null || serializedObject == null)
            {
                Debug.LogError("ShowParentSelectionWindow: Invalid parameters!");
                return;
            }
            
            Debug.Log($"ShowParentSelectionWindow: Opening window for property {parentProperty.name}");
            // Create a popup window to select from scene objects
            SceneObjectSelectionWindow.ShowWindow(parentProperty, serializedObject, "Select Parent Object");
        }

        private string GetTransformPath(Transform transform)
        {
            if (transform == null) return "";
            
            string path = transform.name;
            Transform parent = transform.parent;
            
            while (parent != null)
            {
                path = parent.name + "/" + path;
                parent = parent.parent;
            }
            
            return path;
        }
    }

    // Generic popup window for selecting Transform objects from the scene
    public class SceneObjectSelectionWindow : EditorWindow
    {
        private SerializedProperty targetProperty;
        private SerializedObject serializedObject;
        private Vector2 scrollPosition;
        private string searchFilter = "";
        private Transform[] sceneTransforms;
        private string windowTitle = "Select Object";

        public static void ShowWindow(SerializedProperty prop, SerializedObject serializedObj, string title = "Select Object")
        {
            if (prop == null || serializedObj == null)
            {
                Debug.LogError("SceneObjectSelectionWindow: Property or SerializedObject is null!");
                return;
            }

            SceneObjectSelectionWindow window = GetWindow<SceneObjectSelectionWindow>(true, title, true);
            window.targetProperty = prop;
            window.serializedObject = serializedObj;
            window.windowTitle = title;
            window.RefreshSceneTransforms();
            window.Show();
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField($"{windowTitle} from Scene", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            // Search filter
            string newSearchFilter = EditorGUILayout.TextField("Search:", searchFilter);
            if (newSearchFilter != searchFilter)
            {
                searchFilter = newSearchFilter;
                RefreshSceneTransforms();
            }

            // Refresh button
            if (GUILayout.Button("Refresh Scene Objects"))
            {
                RefreshSceneTransforms();
            }

            EditorGUILayout.Space();

            // Scroll view for scene objects
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            if (sceneTransforms != null)
            {
                foreach (Transform transform in sceneTransforms)
                {
                    if (transform == null) continue;

                    if (string.IsNullOrEmpty(searchFilter) || 
                        transform.name.ToLower().Contains(searchFilter.ToLower()))
                    {
                        EditorGUILayout.BeginHorizontal();
                        
                        // Object icon and name with hierarchy path
                        string objectPath = GetTransformPath(transform);
                        EditorGUILayout.LabelField($"{transform.name}", GUILayout.ExpandWidth(true));
                        if (!string.IsNullOrEmpty(objectPath) && objectPath != transform.name)
                        {
                            EditorGUILayout.LabelField($"({objectPath})", EditorStyles.miniLabel, GUILayout.ExpandWidth(true));
                        }
                        
                        if (GUILayout.Button("Select", GUILayout.Width(60)))
                        {
                            try
                            {
                                // 验证对象是否有效
                                if (targetProperty == null)
                                {
                                    Debug.LogError("SceneObjectSelectionWindow: targetProperty is null!");
                                    return;
                                }
                                
                                if (serializedObject == null)
                                {
                                    Debug.LogError("SceneObjectSelectionWindow: serializedObject is null!");
                                    return;
                                }
                                
                                if (transform == null)
                                {
                                    Debug.LogError("SceneObjectSelectionWindow: selected transform is null!");
                                    return;
                                }
                                
                                Debug.Log($"SceneObjectSelectionWindow: Selecting {transform.name} for property {targetProperty.name}");
                                
                                // 确保序列化对象是最新的
                                serializedObject.Update();
                                
                                // 设置属性值
                                targetProperty.objectReferenceValue = transform;
                                
                                // 立即应用修改并标记脏状态
                                bool applied = serializedObject.ApplyModifiedProperties();
                                Debug.Log($"SceneObjectSelectionWindow: ApplyModifiedProperties result: {applied}");
                                
                                EditorUtility.SetDirty(serializedObject.targetObject);
                                
                                // 关闭窗口
                                Close();
                                
                                // 强制重绘Inspector
                                UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
                            }
                            catch (System.Exception e)
                            {
                                Debug.LogError($"SceneObjectSelectionWindow: Error during selection: {e.Message}");
                            }
                        }
                        
                        EditorGUILayout.EndHorizontal();
                    }
                }
            }

            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space();
            if (GUILayout.Button("Cancel"))
            {
                Close();
            }
        }

        private void RefreshSceneTransforms()
        {
            // Get all transforms in the scene
            sceneTransforms = FindObjectsOfType<Transform>();
            
            // Filter by search if needed
            if (!string.IsNullOrEmpty(searchFilter))
            {
                System.Array.Sort(sceneTransforms, (a, b) => {
                    bool aContains = a.name.ToLower().Contains(searchFilter.ToLower());
                    bool bContains = b.name.ToLower().Contains(searchFilter.ToLower());
                    
                    if (aContains && !bContains) return -1;
                    if (!aContains && bContains) return 1;
                    return string.Compare(a.name, b.name);
                });
            }
            else
            {
                // Sort alphabetically
                System.Array.Sort(sceneTransforms, (a, b) => string.Compare(a.name, b.name));
            }
        }

        private string GetTransformPath(Transform transform)
        {
            if (transform == null) return "";
            
            string path = transform.name;
            Transform parent = transform.parent;
            
            while (parent != null)
            {
                path = parent.name + "/" + path;
                parent = parent.parent;
            }
            
            return path;
        }
    }
} 
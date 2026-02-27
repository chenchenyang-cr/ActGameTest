using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditorInternal;
#endif
using System.Collections.Generic;
using System;
using System.Linq;

namespace CombatEditor
{
    [AbilityEvent]
    [CreateAssetMenu(menuName = "AbilityEvents / ToggleGameObject")]
    public class AbilityEventObj_ToggleScript : AbilityEventObj
    {
        // 旧的单一脚本名称字段（保留但隐藏，用于向后兼容）
        [HideInInspector]
        public string scriptName;
        
        // 存储路径信息，用于在运行时查找场景中的GameObject
        [SerializeField, HideInInspector]
        private List<string> gameObjectPaths = new List<string>();
        
        // 存储GameObject实例ID，用于编辑器中识别场景物体
        [SerializeField, HideInInspector]
        private List<int> gameObjectInstanceIDs = new List<int>();
        
        // 在编辑器中显示的GameObject引用
        public List<GameObject> gameObjectReferences = new List<GameObject>();
        
        [Tooltip("Whether to enable or disable the GameObjects")]
        public bool enable = true;
        
        public override EventTimeType GetEventTimeType()
        {
            return EventTimeType.EventTime;
        }
        
        public override AbilityEventEffect Initialize()
        {
            return new AbilityEventEffect_ToggleScript(this);
        }
        
        public override AbilityEventPreview InitializePreview()
        {
#if UNITY_EDITOR
            return new AbilityEventPreview_ToggleScript(this);
#else
            return null;
#endif
        }
        
        public override bool PreviewExist()
        {
#if UNITY_EDITOR
            return true;
#else
            return false;
#endif
        }
    }

    // Runtime effect implementation
    public partial class AbilityEventEffect_ToggleScript : AbilityEventEffect
    {
        // 用于存储在运行时找到的场景GameObject
        private List<GameObject> runtimeGameObjects = new List<GameObject>();
        private bool hasInitialized = false;
        
        // 使用路径信息在运行时查找GameObject
        private void InitializeGameObjectReferences()
        {
            if (hasInitialized) return;
            
            AbilityEventObj_ToggleScript eventObj = (AbilityEventObj_ToggleScript)_EventObj;
            
            // 清空列表
            runtimeGameObjects.Clear();
            
            // 1. 首先通过名称在场景中查找GameObject
            var gameObjectPaths = eventObj.GetType().GetField("gameObjectPaths", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                
            if (gameObjectPaths != null)
            {
                List<string> paths = gameObjectPaths.GetValue(eventObj) as List<string>;
                if (paths != null)
                {
                    foreach (string path in paths)
                    {
                        if (!string.IsNullOrEmpty(path))
                        {
                            // 尝试使用路径查找GameObject
                            GameObject foundObj = GameObject.Find(path);
                            if (foundObj != null)
                            {
                                runtimeGameObjects.Add(foundObj);
                            }
                        }
                    }
                }
            }
            
            // 2. 如果有预制体引用，也添加进来
            if (eventObj.gameObjectReferences != null)
            {
                foreach (var go in eventObj.gameObjectReferences)
                {
                    if (go != null && !runtimeGameObjects.Contains(go))
                    {
                        runtimeGameObjects.Add(go);
                    }
                }
            }
            
            hasInitialized = true;
        }
        
        public override void StartEffect()
        {
            base.StartEffect();
            AbilityEventObj_ToggleScript eventObj = (AbilityEventObj_ToggleScript)_EventObj;
            
            // 初始化GameObject引用
            InitializeGameObjectReferences();
            
            // 向后兼容：检查旧的单一脚本名称
            if (runtimeGameObjects.Count == 0 && !string.IsNullOrEmpty(eventObj.scriptName))
            {
                ToggleScriptByName(eventObj.scriptName, eventObj.enable);
                return;
            }
            
            // 处理GameObject引用列表
            if (runtimeGameObjects.Count == 0)
            {
                Debug.LogWarning("No GameObjects found. Cannot toggle GameObjects.");
                return;
            }
            
            // 循环切换所有GameObject
            foreach (var go in runtimeGameObjects)
            {
                if (go != null)
                {
                    // 切换GameObject
                    go.SetActive(eventObj.enable);
                }
            }
        }
        
        // 通过脚本名称切换脚本状态（保留向后兼容）
        private void ToggleScriptByName(string scriptName, bool enable)
        {
            if (string.IsNullOrEmpty(scriptName) || _combatController == null)
                return;
                
            // Get the component at the same level as CombatController
            MonoBehaviour[] scripts = _combatController.GetComponents<MonoBehaviour>();
            
            // Find the script by name
            foreach (MonoBehaviour script in scripts)
            {
                if (script.GetType().Name == scriptName)
                {
                    script.enabled = enable;
                    Debug.Log($"[Runtime] {(enable ? "Enabled" : "Disabled")} {scriptName} script.");
                    return;
                }
            }
            
            Debug.LogWarning($"Script {scriptName} not found on the CombatController GameObject.");
        }
        
        public override void EffectRunning()
        {
            base.EffectRunning();
        }
        
        public override void EndEffect()
        {
            base.EndEffect();
        }
    }

    // Constructor and helper
    public partial class AbilityEventEffect_ToggleScript : AbilityEventEffect
    {
        AbilityEventObj_ToggleScript EventObj => (AbilityEventObj_ToggleScript)_EventObj;
        
        public AbilityEventEffect_ToggleScript(AbilityEventObj InitObj) : base(InitObj)
        {
            _EventObj = InitObj;
        }
    }

#if UNITY_EDITOR
    // Editor preview implementation
    public class AbilityEventPreview_ToggleScript : AbilityEventPreview
    {
        AbilityEventObj_ToggleScript EventObj => (AbilityEventObj_ToggleScript)_EventObj;
        
        public AbilityEventPreview_ToggleScript(AbilityEventObj Obj) : base(Obj)
        {
        }
        
        public override void InitPreview()
        {
            base.InitPreview();
        }
        
        public override void PassStartFrame()
        {
            base.PassStartFrame();
            
            if (EventObj.gameObjectReferences.Count > 0)
            {
                string goNames = string.Join(", ", EventObj.gameObjectReferences
                    .Where(go => go != null)
                    .Select(go => go.name));
                    
                Debug.Log($"[Editor Preview] {(EventObj.enable ? "Enable" : "Disable")} GameObjects: {goNames}");
            }
            else if (!string.IsNullOrEmpty(EventObj.scriptName))
            {
                Debug.Log($"[Editor Preview] {(EventObj.enable ? "Enable" : "Disable")} {EventObj.scriptName} script.");
            }
        }
        
        public override void DestroyPreview()
        {
            base.DestroyPreview();
        }
    }
#endif

#if UNITY_EDITOR
    // Custom editor for the AbilityEventObj_ToggleScript
    [CustomEditor(typeof(AbilityEventObj_ToggleScript))]
    public class AbilityEventObj_ToggleScriptEditor : Editor
    {
        private AbilityEventObj_ToggleScript targetObj;
        private SerializedProperty gameObjectReferencesProp;
        private SerializedProperty gameObjectPathsProp;
        private SerializedProperty gameObjectInstanceIDsProp;
        private ReorderableList gameObjectReferencesList;
        
        // 场景中的对象引用
        private List<GameObject> sceneGameObjects = new List<GameObject>();
        
        private void OnEnable()
        {
            targetObj = (AbilityEventObj_ToggleScript)target;
            gameObjectReferencesProp = serializedObject.FindProperty("gameObjectReferences");
            gameObjectPathsProp = serializedObject.FindProperty("gameObjectPaths");
            gameObjectInstanceIDsProp = serializedObject.FindProperty("gameObjectInstanceIDs");
            
            // 恢复场景对象引用
            RestoreSceneReferences();
            
            SetupReorderableList();
        }
        
        // 恢复场景对象引用
        private void RestoreSceneReferences()
        {
            sceneGameObjects.Clear();
            
            // 通过实例ID恢复场景引用
            var instanceIDs = GetPrivateFieldValue<List<int>>(targetObj, "gameObjectInstanceIDs");
            if (instanceIDs != null)
            {
                foreach (int instanceID in instanceIDs)
                {
                    if (instanceID != 0)
                    {
                        var obj = EditorUtility.InstanceIDToObject(instanceID) as GameObject;
                        if (obj != null)
                        {
                            sceneGameObjects.Add(obj);
                        }
                    }
                }
            }
        }
        
        // 工具方法：获取私有字段值
        private T GetPrivateFieldValue<T>(object obj, string fieldName)
        {
            var field = obj.GetType().GetField(fieldName, 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (field != null)
            {
                return (T)field.GetValue(obj);
            }
            
            return default(T);
        }
        
        private void SetupReorderableList()
        {
            gameObjectReferencesList = new ReorderableList(
                sceneGameObjects,
                typeof(GameObject),
                true, // 可拖动
                true, // 显示标题
                true, // 可添加元素
                true  // 可移除元素
            );
            
            // 设置标题
            gameObjectReferencesList.drawHeaderCallback = (Rect rect) => {
                EditorGUI.LabelField(rect, "拖拽场景中的GameObject到此处");
            };
            
            // 设置元素绘制
            gameObjectReferencesList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) => {
                rect.y += 2;
                rect.height = EditorGUIUtility.singleLineHeight;
                
                EditorGUI.BeginChangeCheck();
                sceneGameObjects[index] = EditorGUI.ObjectField(rect, sceneGameObjects[index], typeof(GameObject), true) as GameObject;
                if (EditorGUI.EndChangeCheck())
                {
                    SaveSceneReferences();
                }
            };
            
            // 添加元素回调
            gameObjectReferencesList.onAddCallback = (ReorderableList list) => {
                sceneGameObjects.Add(null);
                SaveSceneReferences();
            };
            
            // 移除元素回调
            gameObjectReferencesList.onRemoveCallback = (ReorderableList list) => {
                if (list.index >= 0 && list.index < sceneGameObjects.Count)
                {
                    sceneGameObjects.RemoveAt(list.index);
                    SaveSceneReferences();
                }
            };
            
            // 列表变化回调
            gameObjectReferencesList.onReorderCallback = (ReorderableList list) => {
                SaveSceneReferences();
            };
        }
        
        // 保存场景引用到序列化字段
        private void SaveSceneReferences()
        {
            serializedObject.Update();
            
            // 清空现有数据
            gameObjectPathsProp.ClearArray();
            gameObjectInstanceIDsProp.ClearArray();
            
            // 添加新数据
            for (int i = 0; i < sceneGameObjects.Count; i++)
            {
                var go = sceneGameObjects[i];
                if (go != null)
                {
                    // 保存路径
                    gameObjectPathsProp.arraySize++;
                    gameObjectPathsProp.GetArrayElementAtIndex(gameObjectPathsProp.arraySize - 1).stringValue = GetGameObjectPath(go);
                    
                    // 保存实例ID
                    gameObjectInstanceIDsProp.arraySize++;
                    gameObjectInstanceIDsProp.GetArrayElementAtIndex(gameObjectInstanceIDsProp.arraySize - 1).intValue = go.GetInstanceID();
                }
                else
                {
                    // 空项也添加，保持索引一致
                    gameObjectPathsProp.arraySize++;
                    gameObjectPathsProp.GetArrayElementAtIndex(gameObjectPathsProp.arraySize - 1).stringValue = "";
                    
                    gameObjectInstanceIDsProp.arraySize++;
                    gameObjectInstanceIDsProp.GetArrayElementAtIndex(gameObjectInstanceIDsProp.arraySize - 1).intValue = 0;
                }
            }
            
            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(targetObj);
        }
        
        // 获取GameObject的完整路径
        private string GetGameObjectPath(GameObject obj)
        {
            if (obj == null) return "";
            
            string path = obj.name;
            Transform parent = obj.transform.parent;
            
            while (parent != null)
            {
                path = parent.name + "/" + path;
                parent = parent.parent;
            }
            
            return path;
        }
        
        // 批量选择多个GameObject
        private void SelectMultipleGameObjects()
        {
            // 使用对话框选择多个GameObject
            GameObject[] selection = Selection.gameObjects;
            
            if (selection != null && selection.Length > 0)
            {
                // 清空现有列表
                sceneGameObjects.Clear();
                
                // 添加选中的所有GameObject
                foreach (var go in selection)
                {
                    if (go != null)
                    {
                        sceneGameObjects.Add(go);
                    }
                }
                
                // 保存引用
                SaveSceneReferences();
            }
        }
        
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            
            EditorGUI.BeginChangeCheck();
            
            // 检查是否在空列表状态下提供批量选择选项
            if (sceneGameObjects.Count == 0)
            {
                EditorGUILayout.HelpBox("列表为空，您可以直接使用当前在Hierarchy中选中的GameObject", MessageType.Info);
                
                EditorGUILayout.BeginHorizontal();
                
                if (GUILayout.Button("使用当前选中的GameObject", GUILayout.Height(30)))
                {
                    SelectMultipleGameObjects();
                }
                
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.Space(5);
                
                EditorGUILayout.HelpBox("或者从下方一个一个添加GameObject", MessageType.Info);
            }
            
            EditorGUILayout.HelpBox("直接将场景中的GameObject拖拽到下方列表中，这些GameObject将被统一开关", MessageType.Info);
            
            // 绘制可重排列表
            gameObjectReferencesList.DoLayoutList();
            
            // 如果列表不为空，提供批量添加按钮
            if (sceneGameObjects.Count > 0)
            {
                if (GUILayout.Button("添加当前在Hierarchy中选中的GameObject"))
                {
                    // 获取当前选中的对象
                    GameObject[] selection = Selection.gameObjects;
                    
                    if (selection != null && selection.Length > 0)
                    {
                        foreach (var go in selection)
                        {
                            if (go != null && !sceneGameObjects.Contains(go))
                            {
                                sceneGameObjects.Add(go);
                            }
                        }
                        
                        SaveSceneReferences();
                    }
                }
            }
            
            // Enable/disable toggle
            targetObj.enable = EditorGUILayout.Toggle("开关GameObject", targetObj.enable);
            
            // 显示当前选中的GameObject数量
            int validGOCount = sceneGameObjects.Count(go => go != null);
            if (validGOCount > 0)
            {
                EditorGUILayout.HelpBox(
                    $"已选择 {validGOCount} 个GameObject，这些对象将在事件触发时被{(targetObj.enable ? "激活" : "禁用")}",
                    MessageType.Info);
            }
            
            // 添加"清空列表"按钮
            if (validGOCount > 0 && GUILayout.Button("清空列表"))
            {
                sceneGameObjects.Clear();
                SaveSceneReferences();
            }
            
            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(targetObj);
            }
        }
    }
#endif
} 
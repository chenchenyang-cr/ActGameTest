using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace CombatEditor
{
    public class EventConditionEditorWindow : EditorWindow
    {
        private AbilityEvent targetEvent;
        private AbilityScriptableObject parentObj;
        private Vector2 scrollPosition;
        private string[] conditionTypeNames = { "无条件", "击中目标", "被击中", "在顿帧中", "判定受击" };
        private string[] conditionModeNames = { "传统模式", "接口模式" };
        private int trackIndex = -1; // 添加轨道索引字段，用于标识当前编辑的是哪个轨道
        private string windowTitle = "事件条件编辑";
        private Color titleColor = Color.white;

        // 添加实例跟踪
        private static EventConditionEditorWindow _currentWindow;
        
        public static void ShowWindow(AbilityEvent evt, AbilityScriptableObject parent, int index = -1)
        {
            // 强制关闭已有的条件编辑窗口，避免多个窗口同时存在导致混乱
            if (_currentWindow != null)
            {
                _currentWindow.Close();
                _currentWindow = null;
            }
            
            // 安全检查，确保索引有效
            if (evt == null || parent == null || index < 0 || index >= parent.events.Count)
            {
                Debug.LogError($"无法编辑事件条件：无效的参数 (index={index}, events.Count={parent?.events?.Count ?? 0})");
                EditorUtility.DisplayDialog("错误", "无法编辑事件条件，选中的轨道无效。", "确定");
                return;
            }

            // 确保传入的事件对象与指定索引的事件对象相同
            if (!ReferenceEquals(evt, parent.events[index]))
            {
                Debug.LogError($"轨道索引不匹配：索引 {index} 指向的事件对象与传入的事件对象不同");
                EditorUtility.DisplayDialog("错误", "轨道索引不匹配，请重新选择轨道。", "确定");
                return;
            }

            // 创建窗口并设置特殊标题以标识轨道
            _currentWindow = GetWindow<EventConditionEditorWindow>(true, $"编辑轨道 #{index + 1} 条件", true);
            EventConditionEditorWindow window = _currentWindow;
            
            // 安全地设置目标事件对象的引用
            window.targetEvent = evt;
            window.parentObj = parent;
            window.trackIndex = index; // 保存轨道索引
            
            // 设置窗口标题颜色，使其更醒目
            string eventName = evt.Obj != null ? evt.Obj.name : "未命名事件";
            string eventType = evt.Obj != null ? GetEventTypeName(evt.Obj) : "未知类型";
            window.windowTitle = $"轨道 #{index + 1}: [{eventType}] {eventName}";
            
            // 根据条件类型设置标题颜色
            if (evt.condition.hasCondition)
            {
                switch (evt.condition.conditionType)
                {
                    case EventCondition.ConditionType.HasHit:
                        window.titleColor = new Color(1f, 0.6f, 0.2f); // 橙色
                        break;
                    case EventCondition.ConditionType.BeenHit:
                        window.titleColor = new Color(1f, 0.2f, 0.2f); // 红色
                        break;
                    case EventCondition.ConditionType.InHitStop:
                        window.titleColor = new Color(0.4f, 0.4f, 1f); // 蓝色
                        break;
                    case EventCondition.ConditionType.HitChecked:
                        window.titleColor = new Color(0.2f, 0.8f, 0.2f); // 绿色
                        break;
                    default:
                        window.titleColor = Color.white;
                        break;
                }
            }
            
            window.Init();
            window.minSize = new Vector2(350, 250);
            window.Show();
            
            // 记录打开窗口的时间戳和轨道信息，用于验证窗口状态
            EditorPrefs.SetString("EventConditionWindow_LastOpenTime", System.DateTime.Now.ToString());
            EditorPrefs.SetInt("EventConditionWindow_TrackIndex", index);
            EditorPrefs.SetString("EventConditionWindow_EventName", eventName);
            EditorPrefs.SetString("EventConditionWindow_EventType", eventType);
        }

        private void Init()
        {
            // 验证窗口状态
            ValidateWindowState();
        }
        
        private void ValidateWindowState()
        {
            // 确保目标事件和轨道索引始终有效
            if (parentObj != null && trackIndex >= 0 && trackIndex < parentObj.events.Count)
            {
                // 如果存储的事件引用已失效，重新获取正确的引用
                if (targetEvent == null || !ReferenceEquals(targetEvent, parentObj.events[trackIndex]))
                {
                    targetEvent = parentObj.events[trackIndex];
                    Debug.Log($"已恢复轨道 #{trackIndex + 1} 的事件引用");
                }
            }
        }

        private void OnEnable()
        {
            // 在窗口启用时验证状态
            ValidateWindowState();
            
            // 登记为当前窗口
            _currentWindow = this;
        }
        
        private void OnDisable()
        {
            // 窗口关闭时，清除全局引用
            if (_currentWindow == this)
            {
                _currentWindow = null;
            }
            
            // 通知CombatEditor禁用轨道保护
            DisableTrackProtectionInCombatEditor();
        }
        
        private void OnDestroy()
        {
            // 窗口销毁时也确保禁用轨道保护
            DisableTrackProtectionInCombatEditor();
        }
        
        /// <summary>
        /// 通知CombatEditor禁用轨道保护
        /// </summary>
        private void DisableTrackProtectionInCombatEditor()
        {
            try
            {
                // 查找CombatEditor窗口
                var combatEditorWindows = Resources.FindObjectsOfTypeAll<EditorWindow>()
                    .Where(w => w.GetType().Name == "CombatEditor")
                    .ToArray();

                if (combatEditorWindows.Length > 0)
                {
                    var combatEditor = combatEditorWindows[0];
                    var combatEditorType = combatEditor.GetType();
                    
                    // 优先尝试调用新的事件保护禁用方法
                    var disableEventProtectionMethod = combatEditorType.GetMethod("DisableEventProtection");
                    if (disableEventProtectionMethod != null)
                    {
                        disableEventProtectionMethod.Invoke(combatEditor, null);
                        Debug.Log("✓ 已禁用事件保护机制");
                        return;
                    }
                    
                    // 如果新方法不存在，则尝试旧方法（向后兼容）
                    var disableTrackProtectionMethod = combatEditorType.GetMethod("DisableTrackProtection");
                    if (disableTrackProtectionMethod != null)
                    {
                        disableTrackProtectionMethod.Invoke(combatEditor, null);
                        Debug.Log("✓ 已禁用轨道保护机制（兼容模式）");
                    }
                    else
                    {
                        Debug.LogWarning("❌ 无法找到保护禁用方法");
                    }
                }
                else
                {
                    Debug.LogWarning("❌ 无法找到CombatEditor窗口");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"禁用保护机制时发生错误: {ex.Message}");
            }
        }

        private void OnGUI()
        {
            // 绘制标题
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel);
                titleStyle.normal.textColor = titleColor;
                titleStyle.fontSize = 12;
                
                GUILayout.FlexibleSpace();
                GUILayout.Label(windowTitle, titleStyle);
                GUILayout.FlexibleSpace();
            }
            
            EditorGUILayout.Space();
            
            // 再次验证窗口状态
            ValidateWindowState();
            
            if (targetEvent == null || parentObj == null || trackIndex < 0 || trackIndex >= parentObj.events.Count)
            {
                EditorGUILayout.HelpBox("无效的事件或轨道索引，请关闭窗口并重新选择轨道。", MessageType.Error);
                if (GUILayout.Button("关闭窗口"))
                {
                    Close();
                }
                return;
            }
            
            // 确保引用正确
            if (!ReferenceEquals(targetEvent, parentObj.events[trackIndex]))
            {
                EditorGUILayout.HelpBox("轨道引用不匹配，将自动修复。", MessageType.Warning);
                targetEvent = parentObj.events[trackIndex];
            }

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            // 显示当前编辑的轨道信息
            string eventName = targetEvent.Obj != null ? targetEvent.Obj.name : "未命名事件";
            string eventType = targetEvent.Obj != null ? GetEventTypeName(targetEvent.Obj) : "未知类型";
            
            EditorGUILayout.LabelField($"编辑轨道 #{trackIndex + 1}: {eventName}", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"事件类型: {eventType}", EditorStyles.miniLabel);
            
            EditorGUILayout.Space();

            // 使用滚动视图
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            // 启用条件复选框
            EditorGUI.BeginChangeCheck();
            bool hasCondition = EditorGUILayout.Toggle("启用条件", targetEvent.condition.hasCondition);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(parentObj, "修改事件条件");
                targetEvent.condition.hasCondition = hasCondition;
                EditorUtility.SetDirty(parentObj);
            }

            if (targetEvent.condition.hasCondition)
            {
                // 条件模式选择
                EditorGUI.BeginChangeCheck();
                int selectedMode = EditorGUILayout.Popup("条件模式", (int)targetEvent.condition.conditionMode, conditionModeNames);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(parentObj, "修改事件条件模式");
                    targetEvent.condition.conditionMode = (EventCondition.ConditionMode)selectedMode;
                    EditorUtility.SetDirty(parentObj);
                }

                EditorGUILayout.Space();

                // 根据条件模式显示不同的UI
                if (targetEvent.condition.conditionMode == EventCondition.ConditionMode.LegacyEnum)
                {
                    // 传统条件类型选择
                    EditorGUI.BeginChangeCheck();
                    int selectedIndex = EditorGUILayout.Popup("条件类型", (int)targetEvent.condition.conditionType, conditionTypeNames);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(parentObj, "修改事件条件类型");
                    targetEvent.condition.conditionType = (EventCondition.ConditionType)selectedIndex;
                    EditorUtility.SetDirty(parentObj);
                }

                EditorGUILayout.Space();

                // 添加条件说明
                switch (targetEvent.condition.conditionType)
                {
                    case EventCondition.ConditionType.HasHit:
                        EditorGUILayout.HelpBox("当角色击中目标时触发事件\n\n在代码中使用 controller.SetHitTargetCondition(true/false) 设置条件", MessageType.Info);
                        break;

                    case EventCondition.ConditionType.BeenHit:
                        EditorGUILayout.HelpBox("当角色被击中时触发事件\n\n在代码中使用 controller.SetBeenHitCondition(true/false) 设置条件", MessageType.Info);
                        break;
                        
                    case EventCondition.ConditionType.InHitStop:
                        EditorGUILayout.HelpBox("当角色处于顿帧状态时触发事件\n\n此条件在顿帧开始时自动设置为true，顿帧结束时自动设置为false", MessageType.Info);
                        break;
                        
                    case EventCondition.ConditionType.HitChecked:
                        EditorGUILayout.HelpBox("当判定受击时触发事件\n\n在代码中使用 controller.SetHitCheckedCondition(true/false) 设置条件", MessageType.Info);
                        break;
                        
                    default:
                        EditorGUILayout.HelpBox("请选择条件类型", MessageType.Info);
                        break;
                    }
                }
                else // 接口模式
                {
                    // 获取所有可用的接口条件
                    var conditionManager = EventConditionManager.Instance;
                    var allConditions = conditionManager.GetAllConditions();
                    var conditionDisplayNames = new string[allConditions.Count];
                    var conditionIds = new string[allConditions.Count];
                    
                    for (int i = 0; i < allConditions.Count; i++)
                    {
                        conditionDisplayNames[i] = allConditions[i].DisplayName;
                        conditionIds[i] = allConditions[i].ConditionId;
                    }
                    
                    // 找到当前选中的条件索引
                    int selectedIndex = 0;
                    for (int i = 0; i < conditionIds.Length; i++)
                    {
                        if (conditionIds[i] == targetEvent.condition.interfaceConditionId)
                        {
                            selectedIndex = i;
                            break;
                        }
                    }
                    
                    // 显示条件选择下拉菜单
                    EditorGUI.BeginChangeCheck();
                    int newSelectedIndex = EditorGUILayout.Popup("接口条件", selectedIndex, conditionDisplayNames);
                    if (EditorGUI.EndChangeCheck())
                    {
                        if (newSelectedIndex >= 0 && newSelectedIndex < conditionIds.Length)
                        {
                            Undo.RecordObject(parentObj, "修改接口条件");
                            targetEvent.condition.interfaceConditionId = conditionIds[newSelectedIndex];
                            EditorUtility.SetDirty(parentObj);
                        }
                    }

                    EditorGUILayout.Space();

                    // 显示选中条件的描述
                    if (selectedIndex >= 0 && selectedIndex < allConditions.Count)
                    {
                        var selectedCondition = allConditions[selectedIndex];
                        EditorGUILayout.HelpBox(selectedCondition.Description, MessageType.Info);
                    }
                    
                    // 添加自动发现按钮
                    if (GUILayout.Button("自动发现新条件"))
                    {
                        conditionManager.AutoRegisterConditions();
                        EditorUtility.DisplayDialog("自动发现", "已完成自动发现新条件，请检查控制台输出。", "确定");
                    }
                }
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.Space();

            // 应用按钮
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("应用并关闭", GUILayout.Width(120)))
            {
                // 应用之前再次确认引用正确
                if (targetEvent != null && parentObj != null && 
                    trackIndex >= 0 && trackIndex < parentObj.events.Count &&
                    ReferenceEquals(targetEvent, parentObj.events[trackIndex]))
                {
                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();
                    
                    // 清除EditorPrefs中的窗口状态记录
                    CleanupEditorPrefs();
                    
                    // 禁用轨道保护
                    DisableTrackProtectionInCombatEditor();
                    
                    Close();
                }
                else
                {
                    Debug.LogError("无法应用条件更改：事件引用无效");
                    EditorUtility.DisplayDialog("错误", "无法应用条件更改，事件引用无效。", "确定");
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
            
            // 显示验证信息（仅在调试模式下）
            if (Event.current.shift)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("验证信息", EditorStyles.boldLabel);
                EditorGUILayout.LabelField($"轨道索引: {trackIndex}");
                EditorGUILayout.LabelField($"父对象: {parentObj?.name ?? "无"}");
                EditorGUILayout.LabelField($"事件总数: {parentObj?.events?.Count.ToString() ?? "0"}");
                EditorGUILayout.LabelField($"对象引用匹配: {ReferenceEquals(targetEvent, parentObj?.events[trackIndex])}");
                EditorGUILayout.LabelField($"最后记录的轨道索引: {EditorPrefs.GetInt("EventConditionWindow_TrackIndex", -1)}");
                EditorGUILayout.LabelField($"最后记录的事件名称: {EditorPrefs.GetString("EventConditionWindow_EventName", "无")}");
            }
        }
        
        private void CleanupEditorPrefs()
        {
            // 清除所有与窗口状态相关的EditorPrefs项
            if (EditorPrefs.HasKey("EventConditionWindow_LastOpenTime"))
                EditorPrefs.DeleteKey("EventConditionWindow_LastOpenTime");
                
            if (EditorPrefs.HasKey("EventConditionWindow_TrackIndex"))
                EditorPrefs.DeleteKey("EventConditionWindow_TrackIndex");
                
            if (EditorPrefs.HasKey("EventConditionWindow_EventName"))
                EditorPrefs.DeleteKey("EventConditionWindow_EventName");
                
            if (EditorPrefs.HasKey("EventConditionWindow_EventType"))
                EditorPrefs.DeleteKey("EventConditionWindow_EventType");
        }

        // 获取事件类型名称
        private static string GetEventTypeName(AbilityEventObj eventObj)
        {
            if (eventObj == null) return "未知";
            
            string typeName = eventObj.GetType().Name;
            // 移除前缀
            if (typeName.StartsWith("AbilityEventObj_"))
            {
                typeName = typeName.Substring("AbilityEventObj_".Length);
            }
            
            return typeName;
        }
    }
} 
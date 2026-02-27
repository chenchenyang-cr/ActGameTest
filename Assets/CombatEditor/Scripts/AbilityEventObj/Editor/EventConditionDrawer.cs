using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace CombatEditor
{
    [CustomPropertyDrawer(typeof(EventCondition))]
    public class EventConditionDrawer : PropertyDrawer
    {
        private bool showDetails = false;
        private Dictionary<string, bool> foldoutStates = new Dictionary<string, bool>();
        private string[] conditionTypeNames = { "无条件", "击中目标", "被击中", "在顿帧中", "判定受击" };
        private string[] conditionModeNames = { "传统模式", "接口模式" };

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (!property.FindPropertyRelative("hasCondition").boolValue)
                return EditorGUIUtility.singleLineHeight;

            string key = property.propertyPath;
            if (!foldoutStates.ContainsKey(key))
                foldoutStates[key] = false;

            bool expanded = foldoutStates[key];

            if (!expanded)
                return EditorGUIUtility.singleLineHeight * 2 + 4;

            // 根据条件模式计算高度
            SerializedProperty conditionModeProp = property.FindPropertyRelative("conditionMode");
            int conditionMode = conditionModeProp.enumValueIndex;
            
            if (conditionMode == 0) // 传统模式
            {
                return EditorGUIUtility.singleLineHeight * 4 + 12;
            }
            else // 接口模式
            {
                return EditorGUIUtility.singleLineHeight * 4 + 12;
            }
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            Rect hasConditionRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            SerializedProperty hasConditionProp = property.FindPropertyRelative("hasCondition");
            
            hasConditionProp.boolValue = EditorGUI.ToggleLeft(hasConditionRect, "启用条件", hasConditionProp.boolValue);

            if (hasConditionProp.boolValue)
            {
                string key = property.propertyPath;
                if (!foldoutStates.ContainsKey(key))
                    foldoutStates[key] = false;

                Rect foldoutRect = new Rect(position.x, position.y + EditorGUIUtility.singleLineHeight + 2, position.width, EditorGUIUtility.singleLineHeight);
                foldoutStates[key] = EditorGUI.Foldout(foldoutRect, foldoutStates[key], "条件设置");

                if (foldoutStates[key])
                {
                    // 条件模式选择
                    SerializedProperty conditionModeProp = property.FindPropertyRelative("conditionMode");
                    Rect modeRect = new Rect(position.x, position.y + EditorGUIUtility.singleLineHeight * 2 + 4, position.width, EditorGUIUtility.singleLineHeight);
                    
                    int selectedMode = EditorGUI.Popup(modeRect, "条件模式", conditionModeProp.enumValueIndex, conditionModeNames);
                    if (selectedMode != conditionModeProp.enumValueIndex)
                    {
                        conditionModeProp.enumValueIndex = selectedMode;
                    }
                    
                    // 根据条件模式显示不同的UI
                    if (conditionModeProp.enumValueIndex == 0) // 传统模式
                {
                    SerializedProperty conditionTypeProp = property.FindPropertyRelative("conditionType");
                        Rect typeRect = new Rect(position.x, position.y + EditorGUIUtility.singleLineHeight * 3 + 8, position.width, EditorGUIUtility.singleLineHeight);

                    // 使用自定义中文名称数组
                    int selectedType = EditorGUI.Popup(typeRect, "条件类型", conditionTypeProp.enumValueIndex, conditionTypeNames);
                    if (selectedType != conditionTypeProp.enumValueIndex)
                    {
                        conditionTypeProp.enumValueIndex = selectedType;
                    }
                    }
                    else // 接口模式
                    {
                        SerializedProperty interfaceConditionIdProp = property.FindPropertyRelative("interfaceConditionId");
                        Rect interfaceRect = new Rect(position.x, position.y + EditorGUIUtility.singleLineHeight * 3 + 8, position.width, EditorGUIUtility.singleLineHeight);
                        
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
                            if (conditionIds[i] == interfaceConditionIdProp.stringValue)
                            {
                                selectedIndex = i;
                                break;
                            }
                        }
                        
                        // 显示条件选择下拉菜单
                        int newSelectedIndex = EditorGUI.Popup(interfaceRect, "接口条件", selectedIndex, conditionDisplayNames);
                        if (newSelectedIndex != selectedIndex && newSelectedIndex >= 0 && newSelectedIndex < conditionIds.Length)
                        {
                            interfaceConditionIdProp.stringValue = conditionIds[newSelectedIndex];
                        }
                    }
                }
            }

            EditorGUI.EndProperty();
        }
    }
} 
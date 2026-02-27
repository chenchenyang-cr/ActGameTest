using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

 namespace CombatEditor
{
    [CustomEditor(typeof(AbilityScriptableObject))]
	public class AbilityScriptableObjectEditor : Editor
	{
	    public float LabelWidth = 140;
        private bool showEvents = true;
        private Dictionary<int, bool> eventFoldouts = new Dictionary<int, bool>();
        
	    public override void OnInspectorGUI()
	    {
            serializedObject.Update();

            AbilityScriptableObject abilityObj = (AbilityScriptableObject)target;

            // 绘制默认属性
            DrawPropertiesExcluding(serializedObject, "events");

            // 绘制事件列表
            EditorGUILayout.Space();
            showEvents = EditorGUILayout.Foldout(showEvents, "事件列表", true);
            
            if (showEvents)
            {
                SerializedProperty eventsProp = serializedObject.FindProperty("events");
                
                EditorGUILayout.BeginVertical(GUI.skin.box);
                
                for (int i = 0; i < eventsProp.arraySize; i++)
                {
                    SerializedProperty eventProp = eventsProp.GetArrayElementAtIndex(i);
                    SerializedProperty objProp = eventProp.FindPropertyRelative("Obj");
                    
                    if (!eventFoldouts.ContainsKey(i))
                    {
                        eventFoldouts[i] = false;
                    }
                    
                    EditorGUILayout.BeginHorizontal();
                    
                    string eventName = "事件 " + i;
                    if (objProp.objectReferenceValue != null)
                    {
                        eventName = objProp.objectReferenceValue.name;
                    }
                    
                    eventFoldouts[i] = EditorGUILayout.Foldout(eventFoldouts[i], eventName, true);
                    
                    if (GUILayout.Button("移除", GUILayout.Width(60)))
                    {
                        eventsProp.DeleteArrayElementAtIndex(i);
                        serializedObject.ApplyModifiedProperties();
                        return;
                    }
                    
                    EditorGUILayout.EndHorizontal();
                    
                    if (eventFoldouts[i])
                    {
                        EditorGUI.indentLevel++;
                        
                        // 绘制事件属性
                        EditorGUILayout.PropertyField(objProp);
                        
                        SerializedProperty timeTypeProp = null;
                        if (objProp.objectReferenceValue != null)
                        {
                            AbilityEventObj.EventTimeType timeType = ((AbilityEventObj)objProp.objectReferenceValue).GetEventTimeType();
                            
                            if (timeType == AbilityEventObj.EventTimeType.EventTime)
                            {
                                SerializedProperty timeProp = eventProp.FindPropertyRelative("EventTime");
                                EditorGUILayout.PropertyField(timeProp, new GUIContent("事件时间"));
                            }
                            else if (timeType == AbilityEventObj.EventTimeType.EventRange || 
                                    timeType == AbilityEventObj.EventTimeType.EventMultiRange)
                            {
                                SerializedProperty rangeProp = eventProp.FindPropertyRelative("EventRange");
                                EditorGUILayout.PropertyField(rangeProp, new GUIContent("事件范围"));
                            }
                        }
                        
                        // 绘制条件设置
                        SerializedProperty conditionProp = eventProp.FindPropertyRelative("condition");
                        if (conditionProp != null)
                        {
                            EditorGUILayout.PropertyField(conditionProp, new GUIContent("事件条件"));
                        }
                        
                        EditorGUI.indentLevel--;
                    }
                    
                    if (i < eventsProp.arraySize - 1)
                    {
                        EditorGUILayout.Space();
                    }
                }
                
                EditorGUILayout.EndVertical();
                
                EditorGUILayout.Space();
                
                if (GUILayout.Button("添加事件"))
                {
                    eventsProp.arraySize++;
                    SerializedProperty newEventProp = eventsProp.GetArrayElementAtIndex(eventsProp.arraySize - 1);
                    newEventProp.FindPropertyRelative("Obj").objectReferenceValue = null;
                    newEventProp.FindPropertyRelative("EventTime").floatValue = 0f;
                    newEventProp.FindPropertyRelative("EventRange").vector2Value = new Vector2(0f, 1f);
                    
                    SerializedProperty conditionProp = newEventProp.FindPropertyRelative("condition");
                    if (conditionProp != null)
                    {
                        conditionProp.FindPropertyRelative("hasCondition").boolValue = false;
                    }
                    
                    serializedObject.ApplyModifiedProperties();
                }
            }
            
            serializedObject.ApplyModifiedProperties();
	    }
	}
}

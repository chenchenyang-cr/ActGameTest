using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace CombatEditor
{
    [CustomEditor(typeof(AbilityEventObj_Rotation))]
    public class AbilityEventObj_RotationEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            AbilityEventObj_Rotation rotationObj = (AbilityEventObj_Rotation)target;
            
            EditorGUILayout.LabelField("Rotation事件设置", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            
            EditorGUI.BeginChangeCheck();
            
            // 绘制默认属性
            DrawDefaultInspector();
            
            // 获取时间控制模式属性
            SerializedObject serializedObject = new SerializedObject(rotationObj);
            SerializedProperty targetProp = serializedObject.FindProperty("target");
            SerializedProperty timeControlModeProp = targetProp.FindPropertyRelative("timeControlMode");
            SerializedProperty useRealTimePlaybackProp = targetProp.FindPropertyRelative("UseRealTimePlayback");
            
            // 处理向后兼容性
            if (useRealTimePlaybackProp.boolValue && timeControlModeProp.intValue == 0)
            {
                timeControlModeProp.intValue = 1; // 设置为RealTime模式
                useRealTimePlaybackProp.boolValue = false;
            }
            
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("使用说明", EditorStyles.boldLabel);
            
            // 显示时间控制说明
            TimeControlMode timeMode = (TimeControlMode)timeControlModeProp.intValue;
            switch (timeMode)
            {
                case TimeControlMode.RealTime:
                    EditorGUILayout.HelpBox("实际时间播放模式：Rotation事件将基于实际经过的时间播放，不受动画速度修改器和hitstop影响。\n" +
                                          "适用于需要固定时间完成的旋转，如定时旋转、固定时长的转向等。", MessageType.Info);
                    break;
                    
                case TimeControlMode.HitStopAwareTime:
                    EditorGUILayout.HelpBox("HitStop感知时间模式：Rotation事件不受AnimSpeed事件影响，但受hitstop影响。\n" +
                                          "在hitstop期间，旋转会根据hitstop的动画速度进行调整。\n" +
                                          "适用于需要与hitstop同步但不受AnimSpeed影响的旋转效果。", MessageType.Info);
                    break;
                    
                default: // AnimationTime
                    EditorGUILayout.HelpBox("动画时间播放模式（默认）：Rotation事件将跟随动画播放速度。\n" +
                                          "当动画速度变慢时，Rotation事件也会变慢；当动画速度加快时，Rotation事件也会加快。\n" +
                                          "同时也会受到hitstop的影响。适用于需要与动画同步的旋转效果。", MessageType.Info);
                    break;
            }
            
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("使用建议", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("• 动画时间播放：适用于需要与动画完全同步的旋转效果\n" +
                                  "• 实际时间播放：适用于需要固定时间完成的旋转（如定时转向、固定时长旋转）\n" +
                                  "• HitStop感知时间：适用于需要与hitstop同步但不受AnimSpeed影响的旋转\n" +
                                  "• 可以与Motion事件配合使用，实现复杂的移动+旋转效果", MessageType.None);
            
            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(rotationObj);
            }
        }

        private void DrawRotationTargetSettings()
        {
            SerializedProperty targetProperty = serializedObject.FindProperty("target");
            
            EditorGUILayout.LabelField("旋转目标设置", EditorStyles.boldLabel);
            
            // 绘制旋转角度
            SerializedProperty eulerRotationProp = targetProperty.FindPropertyRelative("EulerRotation");
            EditorGUILayout.PropertyField(eulerRotationProp, new GUIContent("旋转角度", "要应用的欧拉角旋转"));
            
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("目标对象选择", EditorStyles.boldLabel);
            
            // 绘制目标对象字段
            SerializedProperty targetObjectProp = targetProperty.FindPropertyRelative("TargetObject");
            SerializedProperty useCustomNameProp = targetProperty.FindPropertyRelative("UseCustomObjectName");
            SerializedProperty customNameProp = targetProperty.FindPropertyRelative("CustomObjectName");
            
            EditorGUILayout.PropertyField(targetObjectProp, new GUIContent("目标对象", "要旋转的游戏对象，如果为空则旋转CombatController"));
            
            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(useCustomNameProp, new GUIContent("使用对象名称查找", "通过名称在场景中查找要旋转的对象"));
            
            if (useCustomNameProp.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(customNameProp, new GUIContent("对象名称", "要查找的游戏对象名称"));
                
                // 显示帮助信息
                if (string.IsNullOrEmpty(customNameProp.stringValue))
                {
                    EditorGUILayout.HelpBox("请输入要查找的游戏对象名称", MessageType.Warning);
                }
                else
                {
                    EditorGUILayout.HelpBox($"将查找名为 '{customNameProp.stringValue}' 的对象\n首先在CombatController的子对象中查找，然后在整个场景中查找", MessageType.Info);
                }
                
                EditorGUI.indentLevel--;
            }
            
            // 显示优先级说明
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox("目标对象优先级：\n1. 直接指定的目标对象\n2. 通过名称查找的对象\n3. CombatController本身", MessageType.Info);
            
            // 绘制时间控制选项
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("时间控制", EditorStyles.boldLabel);
            
            SerializedProperty useRealTimePlaybackProp = targetProperty.FindPropertyRelative("UseRealTimePlayback");
            EditorGUILayout.PropertyField(useRealTimePlaybackProp, new GUIContent("使用实际时间播放", "如果启用，rotation事件将基于实际时间播放，不受动画速度影响；如果禁用，rotation事件将跟随动画速度"));
            
            // 显示时间控制说明
            if (useRealTimePlaybackProp.boolValue)
            {
                EditorGUILayout.HelpBox("实际时间播放模式：Rotation事件将基于实际经过的时间播放，不受动画速度修改器影响。\n" +
                                      "适用于需要固定时间完成的旋转，如定时旋转、固定时长的转向等。", MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox("动画时间播放模式（默认）：Rotation事件将跟随动画播放速度。\n" +
                                      "当动画速度变慢时，Rotation事件也会变慢；当动画速度加快时，Rotation事件也会加快。\n" +
                                      "适用于需要与动画同步的旋转效果。", MessageType.Info);
            }
            
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("使用建议", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("• 实际时间播放：适用于需要固定时间完成的旋转（如定时转向、固定时长旋转）\n" +
                                  "• 动画时间播放：适用于需要与动画同步的旋转效果\n" +
                                  "• 可以与Motion事件配合使用，实现复杂的移动+旋转效果", MessageType.None);
        }
    }
} 
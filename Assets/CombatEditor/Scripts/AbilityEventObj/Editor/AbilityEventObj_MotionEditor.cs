using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace CombatEditor
{
    [CustomEditor(typeof(AbilityEventObj_Motion))]
    public class AbilityEventObj_MotionEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            AbilityEventObj_Motion motionObj = (AbilityEventObj_Motion)target;
            
            EditorGUILayout.LabelField("Motion事件设置", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            
            EditorGUI.BeginChangeCheck();
            
            // 绘制默认属性
            DrawDefaultInspector();
            
            // 获取时间控制模式属性
            SerializedObject serializedObject = new SerializedObject(motionObj);
            SerializedProperty targetProp = serializedObject.FindProperty("target");
            SerializedProperty timeControlModeProp = targetProp.FindPropertyRelative("timeControlMode");
            SerializedProperty useRealTimePlaybackProp = targetProp.FindPropertyRelative("UseRealTimePlayback");
            SerializedProperty useAbsoluteCoordinatesProp = targetProp.FindPropertyRelative("UseAbsoluteCoordinates");
            
            // 处理向后兼容性
            if (useRealTimePlaybackProp.boolValue && timeControlModeProp.intValue == 0)
            {
                timeControlModeProp.intValue = 1; // 设置为RealTime模式
                useRealTimePlaybackProp.boolValue = false;
            }
            
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("使用说明", EditorStyles.boldLabel);
            
            // 显示坐标系统说明
            if (useAbsoluteCoordinatesProp.boolValue)
            {
                EditorGUILayout.HelpBox("绝对坐标增量模式：在事件开始时根据增量值锁定目标位置，然后朝向该位置移动。\n" +
                                      "目标位置 = 起始位置 + 增量值，不受角色朝向影响。\n" +
                                      "例如：增量(1,0,0)始终向世界坐标X轴正方向移动1个单位", MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox("本地坐标系模式（默认）：在事件开始时根据角色朝向和增量值锁定目标位置，然后朝向该位置移动。\n" +
                                      "目标位置 = 起始位置 + 角色朝向 * 增量值，移动期间目标位置不会改变。\n" +
                                      "例如：增量(0,0,1)向角色面朝方向移动1个单位", MessageType.Info);
            }
            
            EditorGUILayout.Space();
            
            // 显示时间控制说明
            TimeControlMode timeMode = (TimeControlMode)timeControlModeProp.intValue;
            switch (timeMode)
            {
                case TimeControlMode.RealTime:
                    EditorGUILayout.HelpBox("实际时间播放模式：Motion事件将基于实际经过的时间播放，不受动画速度修改器和hitstop影响。\n" +
                                          "适用于需要固定时间完成的移动，如定时闪避、固定时长的位移等。", MessageType.Info);
                    break;
                    
                case TimeControlMode.HitStopAwareTime:
                    EditorGUILayout.HelpBox("HitStop感知时间模式：Motion事件不受AnimSpeed事件影响，但受hitstop影响。\n" +
                                          "在hitstop期间，Movement会根据hitstop的动画速度进行调整。\n" +
                                          "适用于需要与hitstop同步但不受AnimSpeed影响的移动效果。", MessageType.Info);
                    break;
                    
                default: // AnimationTime
                    EditorGUILayout.HelpBox("动画时间播放模式（默认）：Motion事件将跟随动画播放速度。\n" +
                                          "当动画速度变慢时，Motion事件也会变慢；当动画速度加快时，Motion事件也会加快。\n" +
                                          "同时也会受到hitstop的影响。适用于需要与动画同步的移动效果。", MessageType.Info);
                    break;
            }
            
            // 显示使用建议
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("使用建议", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("• 动画时间播放：适用于需要与动画完全同步的移动效果\n" +
                                  "• 实际时间播放：适用于需要固定时间完成的移动（如定时闪避、固定时长位移）\n" +
                                  "• HitStop感知时间：适用于需要与hitstop同步但不受AnimSpeed影响的移动\n" +
                                  "• 可以与Rotation事件配合使用，实现复杂的移动+旋转效果", MessageType.None);
            
            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(motionObj);
            }
        }
    }
} 
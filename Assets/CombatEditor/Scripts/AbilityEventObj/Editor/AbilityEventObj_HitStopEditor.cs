using UnityEngine;
using UnityEditor;

namespace CombatEditor
{
    [CustomEditor(typeof(AbilityEventObj_HitStop))]
    public class AbilityEventObj_HitStopEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            AbilityEventObj_HitStop hitStopObj = (AbilityEventObj_HitStop)target;
            
            EditorGUILayout.LabelField("顿帧设置", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            
            EditorGUI.BeginChangeCheck();
            
            // 顿帧帧数
            int frames = EditorGUILayout.IntSlider(new GUIContent("顿帧帧数", "顿帧持续的帧数"), hitStopObj.hitStopFrames, 1, 60);
            
            // 顿帧期间的动画速度
            float speed = EditorGUILayout.Slider(new GUIContent("动画速度", "顿帧期间的动画速度，0表示完全停止"), hitStopObj.animationSpeed, 0f, 0.5f);
            
            // 精确帧定位
            bool enablePreciseFramePosition = EditorGUILayout.Toggle(
                new GUIContent("启用精确帧定位", "防止在高速动画中跳过目标帧"), 
                hitStopObj.enablePreciseFramePosition);
            
            EditorGUILayout.Space();
            
            // --- 新增代码: 绘制速度恢复设置 ---
            EditorGUILayout.LabelField("速度恢复设置", EditorStyles.boldLabel);
            bool enableRecovery = EditorGUILayout.Toggle(
                new GUIContent("启用速度恢复", "是否在顿帧结束后启用平滑的速度恢复功能"),
                hitStopObj.enableSpeedRecovery);

            float recoveryDuration = hitStopObj.recoveryDuration;
            float recoveryTargetSpeed = hitStopObj.recoveryTargetSpeed;
            AnimationCurve recoveryCurve = hitStopObj.recoveryCurve;

            if (enableRecovery)
            {
                EditorGUI.indentLevel++;
                recoveryDuration = EditorGUILayout.FloatField(
                    new GUIContent("恢复时长 (秒)", "速度恢复所需的时间"),
                    hitStopObj.recoveryDuration);
                
                recoveryTargetSpeed = EditorGUILayout.FloatField(
                    new GUIContent("目标速度", "最终恢复到的动画速度"),
                    hitStopObj.recoveryTargetSpeed);

                recoveryCurve = EditorGUILayout.CurveField(
                    new GUIContent("恢复曲线", "速度恢复的插值曲线"),
                    hitStopObj.recoveryCurve);
                EditorGUI.indentLevel--;
            }
            // --- 新增代码结束 ---

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(hitStopObj, "Modified HitStop Settings");
                
                hitStopObj.hitStopFrames = frames;
                hitStopObj.animationSpeed = speed;
                hitStopObj.enablePreciseFramePosition = enablePreciseFramePosition;
                
                // --- 新增代码: 保存速度恢复设置 ---
                hitStopObj.enableSpeedRecovery = enableRecovery;
                hitStopObj.recoveryDuration = recoveryDuration;
                hitStopObj.recoveryTargetSpeed = recoveryTargetSpeed;
                hitStopObj.recoveryCurve = recoveryCurve;
                // --- 新增代码结束 ---
                
                EditorUtility.SetDirty(hitStopObj);
            }
            
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox("此事件会在指定时间点将动画速度设置为指定值，并持续指定帧数。\n在顿帧期间，'在顿帧中'条件会自动设置为true。", MessageType.Info);
        }
    }
} 
using UnityEngine;
using UnityEditor;

namespace CombatEditor
{
    [CustomEditor(typeof(AnimationDebugger))]
    public class AnimationDebuggerEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            
            EditorGUILayout.Space();
            
            AnimationDebugger debugger = (AnimationDebugger)target;
            
            if (GUILayout.Button("检查所有动画循环设置"))
            {
                debugger.CheckAllAnimationLoopSettings();
            }
            
            if (Application.isPlaying)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("=== 运行时信息 ===", EditorStyles.boldLabel);
                
                if (debugger.animator != null)
                {
                    for (int i = 0; i < debugger.animator.layerCount; i++)
                    {
                        if (!debugger.animator.IsInTransition(i))
                        {
                            var stateInfo = debugger.animator.GetCurrentAnimatorStateInfo(i);
                            var clipInfo = debugger.animator.GetCurrentAnimatorClipInfo(i);
                            
                            if (clipInfo.Length > 0)
                            {
                                var clip = clipInfo[0].clip;
                                EditorGUILayout.LabelField($"层{i}: {clip.name}");
                                EditorGUILayout.LabelField($"  循环: {clip.isLooping}, 时间: {stateInfo.normalizedTime:F3}");
                                EditorGUILayout.LabelField($"  长度: {clip.length:F2}s, 速度: {debugger.animator.speed:F2}");
                            }
                        }
                        else
                        {
                            EditorGUILayout.LabelField($"层{i}: 正在过渡中");
                        }
                    }
                }
                
                if (GUILayout.Button("刷新显示"))
                {
                    Repaint();
                }
            }
        }
    }
} 
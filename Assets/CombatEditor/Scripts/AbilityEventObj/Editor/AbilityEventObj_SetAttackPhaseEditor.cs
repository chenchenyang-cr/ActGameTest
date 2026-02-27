using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace CombatEditor
{
    [CustomEditor(typeof(AbilityEventObj_SetAttackPhase))]
    public class AbilityEventObj_SetAttackPhaseEditor : Editor
    {
        // 基础攻击阶段描述
        private readonly string[] BasicPhaseDescriptions = {
            "阶段 0 - 初始/未激活",
            "阶段 1 - 准备阶段",
            "阶段 2 - 攻击阶段",
            "阶段 3 - 后摇阶段"
        };
        
        public override void OnInspectorGUI()
        {
            AbilityEventObj_SetAttackPhase phaseObj = (AbilityEventObj_SetAttackPhase)target;
            
            EditorGUILayout.LabelField("攻击阶段设置", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            
            EditorGUI.BeginChangeCheck();
            
            // 获取当前选择的CombatController的最大阶段数
            int maxPhases = 30; // 默认值
            
            // 尝试从当前场景查找最大阶段数
            CombatController[] controllers = GameObject.FindObjectsOfType<CombatController>();
            if (controllers != null && controllers.Length > 0)
            {
                // 使用找到的第一个控制器的最大阶段数
                maxPhases = controllers[0].maxAttackPhases;
            }
            
            // 显示说明，指示最大阶段数的来源
            EditorGUILayout.HelpBox(
                $"当前最大阶段数: {maxPhases}\n" +
                "最大阶段数取自场景中的第一个CombatController组件设置。\n" +
                "如果场景中没有CombatController，则使用默认值30。", 
                MessageType.Info);
            
            // 选择攻击阶段
            int phase = EditorGUILayout.IntSlider(
                new GUIContent("目标阶段", $"设置的攻击阶段（0-{maxPhases}）"),
                phaseObj.targetPhase, 0, maxPhases);
            
            // 显示当前阶段的默认描述
            string defaultDescription;
            if (phase < BasicPhaseDescriptions.Length)
            {
                defaultDescription = BasicPhaseDescriptions[phase];
            }
            else
            {
                defaultDescription = $"阶段 {phase} - 自定义阶段";
            }
            
            EditorGUILayout.LabelField("默认描述", defaultDescription);
            
            // 自定义描述输入
            string description = EditorGUILayout.TextField(
                new GUIContent("自定义描述", "可选的自定义描述，用于在编辑器中更好地识别此阶段"),
                phaseObj.phaseDescription);
            
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(phaseObj, "Modified Attack Phase Settings");
                
                phaseObj.targetPhase = phase;
                phaseObj.phaseDescription = description;
                
                EditorUtility.SetDirty(phaseObj);
            }
            
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(
                "此事件将在指定时间点将角色的攻击阶段设置为指定值。\n" +
                "攻击阶段可以用于控制攻击连招、不同阶段的特效、音效等。", 
                MessageType.Info);
                
            // 显示当前配置预览
            if (!string.IsNullOrEmpty(phaseObj.phaseDescription))
            {
                EditorGUILayout.LabelField($"配置预览: 阶段 {phaseObj.targetPhase} - {phaseObj.phaseDescription}");
            }
            else
            {
                EditorGUILayout.LabelField($"配置预览: {defaultDescription}");
            }
        }
    }
} 
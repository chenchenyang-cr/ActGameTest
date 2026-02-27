#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using CombatEditor;

[CustomEditor(typeof(AbilityEventObj_AnimatorVariable))]
public class AbilityEventObj_AnimatorVariableEditor : Editor
{
    private AbilityEventObj_AnimatorVariable targetEvent;
    private SerializedProperty targetAnimatorProp;
    private SerializedProperty animatorVariablesProp;
    private SerializedProperty isRangeEventProp;
    
    // 用于添加变量的临时数据
    private string newVariableName = "";
    private int selectedVariableIndex = 0;
    private List<string> availableVariableNames = new List<string>();
    
    private void OnEnable()
    {
        targetEvent = (AbilityEventObj_AnimatorVariable)target;
        
        targetAnimatorProp = serializedObject.FindProperty("targetAnimator");
        animatorVariablesProp = serializedObject.FindProperty("animatorVariables");
        isRangeEventProp = serializedObject.FindProperty("isRangeEvent");
        
        RefreshAvailableVariables();
    }
    
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        
        EditorGUILayout.Space(5);
        
        // 动画机引用
        EditorGUILayout.LabelField("动画机设置", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(targetAnimatorProp, new GUIContent("目标动画机 (可选)", "手动指定要控制的动画机。如果为空，系统会自动检测：1)选中对象的CombatController 2)场景中的CombatController 3)场景中的任意动画机"));
        
        // 显示当前检测到的动画机
        var detectedAnimator = targetEvent.GetBestAvailableAnimator();
        if (detectedAnimator != null && detectedAnimator != targetEvent.targetAnimator)
        {
            var combatController = detectedAnimator.GetComponent<CombatController>();
            string sourceInfo = combatController != null ? $"来自CombatController: {combatController.name}" : "场景中的动画机";
            EditorGUILayout.HelpBox($"已自动检测到动画机: {detectedAnimator.name}\n({sourceInfo})", MessageType.Info);
        }
        
        EditorGUILayout.Space(10);
        
        // 添加变量设置
        EditorGUILayout.LabelField("添加动画机变量", EditorStyles.boldLabel);
        
        // 刷新可用变量按钮
        if (GUILayout.Button("🔄 刷新可用变量列表", GUILayout.Height(25)))
        {
            RefreshAvailableVariables();
        }
        
        EditorGUILayout.Space(5);
        
        // 添加变量界面
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("添加新变量", EditorStyles.boldLabel);
        
        if (availableVariableNames.Count > 0)
        {
            // 从下拉列表选择
            EditorGUILayout.LabelField("从动画机变量中选择:");
            selectedVariableIndex = EditorGUILayout.Popup("可用变量", selectedVariableIndex, availableVariableNames.ToArray());
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("添加选中的变量", GUILayout.Height(25)))
            {
                if (selectedVariableIndex >= 0 && selectedVariableIndex < availableVariableNames.Count)
                {
                    AddVariableFromList(availableVariableNames[selectedVariableIndex]);
                }
            }
            
            if (GUILayout.Button("添加所有变量", GUILayout.Width(100), GUILayout.Height(25)))
            {
                if (EditorUtility.DisplayDialog("确认", "确定要添加所有动画机变量吗？", "确定", "取消"))
                {
                    AddAllVariables();
                }
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("或者手动输入变量名:", EditorStyles.label);
        }
        else
        {
            EditorGUILayout.HelpBox("未检测到动画机变量。请确保指定了有效的动画机。", MessageType.Warning);
            EditorGUILayout.LabelField("手动输入变量名:", EditorStyles.label);
        }
        
        // 手动输入变量名
        newVariableName = EditorGUILayout.TextField("变量名", newVariableName);
        
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("添加自定义变量", GUILayout.Height(25)))
        {
            if (!string.IsNullOrEmpty(newVariableName))
            {
                AddCustomVariable(newVariableName);
                newVariableName = "";
            }
        }
        
        if (GUILayout.Button("清空所有变量", GUILayout.Width(100), GUILayout.Height(25)))
        {
            if (EditorUtility.DisplayDialog("确认", "确定要清空所有变量吗？", "确定", "取消"))
            {
                ClearAllVariables();
            }
        }
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.EndVertical();
        
        EditorGUILayout.Space(10);
        
        // 事件类型设置
        EditorGUILayout.LabelField("事件类型设置", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(isRangeEventProp, new GUIContent("持续性事件", "true=持续事件(EventRange), false=瞬时事件(EventTime)"));
        
        EditorGUILayout.Space(10);
        
        // 动画机变量列表
        DrawAnimatorVariablesList();
        
        // 状态信息
        DrawStatusInfo();
        
        serializedObject.ApplyModifiedProperties();
    }
    
    private void RefreshAvailableVariables()
    {
        availableVariableNames = targetEvent.GetAvailableVariableNames();
        
        if (availableVariableNames.Count > 0)
        {
            selectedVariableIndex = 0; // 重置选择索引
            Debug.Log($"检测到 {availableVariableNames.Count} 个可用的动画机变量");
        }
        else
        {
            Debug.LogWarning("未检测到动画机变量，请确保指定了有效的动画机");
        }
    }
    
    private void AddVariableFromList(string variableName)
    {
        // 检查是否已经存在
        if (targetEvent.animatorVariables.Any(v => v.variableName == variableName))
        {
            EditorUtility.DisplayDialog("提示", $"变量 '{variableName}' 已经存在！", "确定");
            return;
        }
        
        // 获取变量类型
        var variableType = targetEvent.GetVariableType(variableName);
        if (variableType.HasValue)
        {
            targetEvent.AddAnimatorVariable(variableName, variableType.Value);
            EditorUtility.SetDirty(targetEvent);
            Debug.Log($"添加了动画机变量: {variableName} ({variableType.Value})");
        }
        else
        {
            EditorUtility.DisplayDialog("错误", $"无法获取变量 '{variableName}' 的类型！", "确定");
        }
    }
    
    private void AddCustomVariable(string variableName)
    {
        // 检查是否已经存在
        if (targetEvent.animatorVariables.Any(v => v.variableName == variableName))
        {
            EditorUtility.DisplayDialog("提示", $"变量 '{variableName}' 已经存在！", "确定");
            return;
        }
        
        targetEvent.AddAnimatorVariable(variableName);
        EditorUtility.SetDirty(targetEvent);
        Debug.Log($"添加了自定义变量: {variableName}");
    }
    
    private void AddAllVariables()
    {
        int addedCount = 0;
        foreach (var variableName in availableVariableNames)
        {
            // 检查是否已经存在
            if (!targetEvent.animatorVariables.Any(v => v.variableName == variableName))
            {
                var variableType = targetEvent.GetVariableType(variableName);
                if (variableType.HasValue)
                {
                    targetEvent.AddAnimatorVariable(variableName, variableType.Value);
                    addedCount++;
                }
            }
        }
        
        if (addedCount > 0)
        {
            EditorUtility.SetDirty(targetEvent);
            Debug.Log($"批量添加了 {addedCount} 个动画机变量");
            EditorUtility.DisplayDialog("成功", $"成功添加了 {addedCount} 个变量！", "确定");
        }
        else
        {
            EditorUtility.DisplayDialog("提示", "没有新的变量可以添加（可能都已存在）", "确定");
        }
    }
    
    private void ClearAllVariables()
    {
        targetEvent.animatorVariables.Clear();
        EditorUtility.SetDirty(targetEvent);
    }
    
    private void DrawAnimatorVariablesList()
    {
        EditorGUILayout.LabelField($"已添加的动画机变量 ({targetEvent.animatorVariables.Count})", EditorStyles.boldLabel);
        
        if (targetEvent.animatorVariables.Count == 0)
        {
            EditorGUILayout.HelpBox("还没有添加任何动画机变量。\n请使用上方的添加功能来添加要控制的变量。", MessageType.Info);
            return;
        }
        
        EditorGUILayout.Space(5);
        
        // 显示变量列表
        for (int i = 0; i < targetEvent.animatorVariables.Count; i++)
        {
            DrawAnimatorVariable(i);
        }
    }
    
    private void DrawAnimatorVariable(int index)
    {
        var varData = targetEvent.animatorVariables[index];
        
        EditorGUILayout.BeginVertical("box");
        
        // 变量头部信息
        EditorGUILayout.BeginHorizontal();
        
        // 变量名和类型设置
        EditorGUILayout.BeginVertical();
        varData.variableName = EditorGUILayout.TextField("变量名", varData.variableName);
        varData.variableType = (AnimatorControllerParameterType)EditorGUILayout.EnumPopup("变量类型", varData.variableType);
        EditorGUILayout.EndVertical();
        
        // 删除按钮
        if (GUILayout.Button("×", GUILayout.Width(25), GUILayout.Height(35)))
        {
            if (EditorUtility.DisplayDialog("确认", $"确定要删除变量 '{varData.variableName}' 吗？", "确定", "取消"))
            {
                targetEvent.animatorVariables.RemoveAt(index);
                EditorUtility.SetDirty(targetEvent);
                return;
            }
        }
        
        EditorGUILayout.EndHorizontal();
        
        EditorGUI.indentLevel++;
        
        // 根据变量类型显示对应的设置
        switch (varData.VariableType)
        {
            case AnimatorControllerParameterType.Bool:
                varData.boolValue = EditorGUILayout.Toggle("设置值", varData.boolValue);
                break;
                
            case AnimatorControllerParameterType.Int:
                varData.intValue = EditorGUILayout.IntField("设置值", varData.intValue);
                break;
                
            case AnimatorControllerParameterType.Float:
                varData.floatValue = EditorGUILayout.FloatField("设置值", varData.floatValue);
                break;
                
            case AnimatorControllerParameterType.Trigger:
                varData.activateTrigger = EditorGUILayout.Toggle("激活触发器", varData.activateTrigger);
                break;
        }
        
        // 恢复原值选项（触发器除外）
        if (varData.VariableType != AnimatorControllerParameterType.Trigger)
        {
            varData.restoreOriginalValue = EditorGUILayout.Toggle("事件结束时恢复原值", varData.restoreOriginalValue);
        }
        
        EditorGUI.indentLevel--;
        
        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(2);
    }
    
    private void DrawStatusInfo()
    {
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("状态信息", EditorStyles.boldLabel);
        
        int totalVariables = targetEvent.animatorVariables.Count;
        int availableVariables = availableVariableNames.Count;
        
        EditorGUILayout.LabelField($"已添加变量数: {totalVariables}");
        EditorGUILayout.LabelField($"可用变量数: {availableVariables}");
        
        // 显示实际使用的动画机
        var actualAnimator = targetEvent.GetBestAvailableAnimator();
        if (actualAnimator != null)
        {
            string animatorInfo = actualAnimator.name;
            if (targetEvent.targetAnimator == null)
            {
                // 自动检测的情况
                var combatController = actualAnimator.GetComponent<CombatController>();
                if (combatController != null)
                {
                    animatorInfo += $" (自动检测: 来自CombatController {combatController.name})";
                }
                else
                {
                    animatorInfo += " (自动检测: 场景中的动画机)";
                }
            }
            else
            {
                animatorInfo += " (手动指定)";
            }
            EditorGUILayout.LabelField($"使用的动画机: {animatorInfo}");
        }
        else
        {
            EditorGUILayout.LabelField("使用的动画机: 未找到可用的动画机", EditorStyles.label);
            EditorGUILayout.HelpBox("请指定目标动画机或确保场景中有CombatController", MessageType.Warning);
        }
        
        EditorGUILayout.LabelField($"事件类型: {(targetEvent.isRangeEvent ? "持续事件" : "瞬时事件")}");
        
        if (totalVariables == 0)
        {
            EditorGUILayout.HelpBox("还没有添加任何变量！请添加要控制的动画机变量。", MessageType.Warning);
        }
    }
}
#endif 
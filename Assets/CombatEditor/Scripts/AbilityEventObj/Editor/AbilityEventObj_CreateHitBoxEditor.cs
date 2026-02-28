using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace CombatEditor
{
    [CustomEditor(typeof(AbilityEventObj_CreateHitBox))]
    public class AbilityEventObj_CreateHitBoxEditor : Editor
    {
        // 分类折叠状态
        private bool showTransformBinding = true;
        private bool showBasicSettings = true;
        private bool showShapeSettings = true;
        private bool showTagSettings = true;
        private bool showAdvancedSettings = false;
        
        // 常用标签快速选择
        private List<string> commonTags = new List<string> { "Player", "Enemy", "Neutral", "Destructible" };
        
        public override void OnInspectorGUI()
        {
            AbilityEventObj_CreateHitBox hitBoxObj = (AbilityEventObj_CreateHitBox)target;
            
            EditorGUI.BeginChangeCheck();
            
            // Transform绑定设置 - 新增部分
            showTransformBinding = EditorGUILayout.BeginFoldoutHeaderGroup(showTransformBinding, "Transform绑定设置");
            if (showTransformBinding)
            {
                EditorGUI.indentLevel++;
                
                EditorGUILayout.HelpBox("设置HitBox要绑定到哪个Transform上。留空表示绑定到角色根Transform。", MessageType.Info);
                
                // 绑定Transform名称
                hitBoxObj.bindTransformName = EditorGUILayout.TextField(
                    new GUIContent("绑定Transform名称", "要绑定的Transform名称，例如：RightHand, LeftFoot, Sword等"),
                    hitBoxObj.bindTransformName);
                
                // 自动搜索选项
                hitBoxObj.autoSearchBindTransform = EditorGUILayout.Toggle(
                    new GUIContent("自动搜索Transform", "是否自动在角色子物体中搜索指定名称的Transform"),
                    hitBoxObj.autoSearchBindTransform);
                
                // 显示一些常用绑定点的快速按钮
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("常用绑定点：");
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("右手"))
                    hitBoxObj.bindTransformName = "RightHand";
                if (GUILayout.Button("左手"))
                    hitBoxObj.bindTransformName = "LeftHand";
                if (GUILayout.Button("右脚"))
                    hitBoxObj.bindTransformName = "RightFoot";
                if (GUILayout.Button("左脚"))
                    hitBoxObj.bindTransformName = "LeftFoot";
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("头部"))
                    hitBoxObj.bindTransformName = "Head";
                if (GUILayout.Button("武器"))
                    hitBoxObj.bindTransformName = "Weapon";
                if (GUILayout.Button("清空"))
                    hitBoxObj.bindTransformName = "";
                EditorGUILayout.EndHorizontal();
                
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
            
            // 基本设置
            showBasicSettings = EditorGUILayout.BeginFoldoutHeaderGroup(showBasicSettings, "基本设置");
            if (showBasicSettings)
            {
                EditorGUI.indentLevel++;
                
                // 持续时间提示
                EditorGUILayout.HelpBox("HitBox持续时间基于轨道长度自动计算，无需单独设置", MessageType.Info);
                
                // 偏移量
                hitBoxObj.hitBoxOffset = EditorGUILayout.Vector3Field(new GUIContent("位置偏移", 
                    "相对于绑定Transform位置的偏移"), hitBoxObj.hitBoxOffset);
                
                // 颜色
                hitBoxObj.hitBoxColor = EditorGUILayout.ColorField(new GUIContent("可视化颜色", 
                    "编辑器中HitBox的显示颜色"), hitBoxObj.hitBoxColor);
                
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
            
            // 形状设置
            showShapeSettings = EditorGUILayout.BeginFoldoutHeaderGroup(showShapeSettings, "形状设置");
            if (showShapeSettings)
            {
                EditorGUI.indentLevel++;
                
                // 形状类型
                hitBoxObj.hitBoxShape = (HitBox.HitBoxShape)EditorGUILayout.EnumPopup(
                    new GUIContent("形状类型", "HitBox的几何形状"), hitBoxObj.hitBoxShape);
                
                // 根据不同形状显示不同参数
                switch (hitBoxObj.hitBoxShape)
                {
                    case HitBox.HitBoxShape.Box:
                        hitBoxObj.hitBoxSize = EditorGUILayout.Vector3Field(
                            new GUIContent("盒体大小", "盒形HitBox的尺寸"), hitBoxObj.hitBoxSize);
                        break;
                        
                    case HitBox.HitBoxShape.Sphere:
                        hitBoxObj.radius = EditorGUILayout.Slider(
                            new GUIContent("半径", "球形HitBox的半径"), hitBoxObj.radius, 0.1f, 5f);
                        break;
                        
                    case HitBox.HitBoxShape.Capsule:
                        hitBoxObj.radius = EditorGUILayout.Slider(
                            new GUIContent("半径", "胶囊体HitBox的半径"), hitBoxObj.radius, 0.1f, 3f);
                        hitBoxObj.height = EditorGUILayout.Slider(
                            new GUIContent("高度", "胶囊体HitBox的高度"), hitBoxObj.height, 0.1f, 5f);
                        break;
                }
                
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
            
            // 标签设置
            showTagSettings = EditorGUILayout.BeginFoldoutHeaderGroup(showTagSettings, "判定标签设置");
            if (showTagSettings)
            {
                EditorGUI.indentLevel++;
                
                EditorGUILayout.LabelField("判定标签（仅命中带有这些标签的对象）");
                
                // 显示当前标签
                if (hitBoxObj.hitTags != null && hitBoxObj.hitTags.Length > 0)
                {
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    
                    List<string> tagList = new List<string>(hitBoxObj.hitTags);
                    List<string> tagsToRemove = new List<string>();
                    
                    foreach (string tag in tagList)
                    {
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField(tag, EditorStyles.boldLabel);
                        if (GUILayout.Button("移除", GUILayout.Width(60)))
                        {
                            tagsToRemove.Add(tag);
                        }
                        EditorGUILayout.EndHorizontal();
                    }
                    
                    // 移除标记的标签
                    foreach (string tag in tagsToRemove)
                    {
                        tagList.Remove(tag);
                    }
                    
                    if (tagsToRemove.Count > 0)
                    {
                        hitBoxObj.hitTags = tagList.ToArray();
                    }
                    
                    EditorGUILayout.EndVertical();
                }
                else
                {
                    EditorGUILayout.HelpBox("未设置判定标签，将无法命中任何对象", MessageType.Warning);
                }
                
                // 添加常用标签的快速选择
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("添加常用标签：");
                
                EditorGUILayout.BeginHorizontal();
                foreach (string tag in commonTags)
                {
                    if (GUILayout.Button(tag))
                    {
                        // 检查是否已包含此标签
                        bool tagExists = false;
                        if (hitBoxObj.hitTags != null)
                        {
                            foreach (string existingTag in hitBoxObj.hitTags)
                            {
                                if (existingTag == tag)
                                {
                                    tagExists = true;
                                    break;
                                }
                            }
                        }
                        
                        // 如果标签不存在，添加它
                        if (!tagExists)
                        {
                            List<string> newTags = new List<string>();
                            if (hitBoxObj.hitTags != null)
                            {
                                newTags.AddRange(hitBoxObj.hitTags);
                            }
                            newTags.Add(tag);
                            hitBoxObj.hitTags = newTags.ToArray();
                        }
                    }
                }
                EditorGUILayout.EndHorizontal();
                
                // 添加自定义标签
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("添加自定义标签：");
                
                EditorGUILayout.BeginHorizontal();
                customTag = EditorGUILayout.TextField(customTag);
                if (GUILayout.Button("添加", GUILayout.Width(60)) && !string.IsNullOrEmpty(customTag))
                {
                    // 检查是否已包含此标签
                    bool tagExists = false;
                    if (hitBoxObj.hitTags != null)
                    {
                        foreach (string existingTag in hitBoxObj.hitTags)
                        {
                            if (existingTag == customTag)
                            {
                                tagExists = true;
                                break;
                            }
                        }
                    }
                    
                    // 如果标签不存在，添加它
                    if (!tagExists)
                    {
                        List<string> newTags = new List<string>();
                        if (hitBoxObj.hitTags != null)
                        {
                            newTags.AddRange(hitBoxObj.hitTags);
                        }
                        newTags.Add(customTag);
                        hitBoxObj.hitTags = newTags.ToArray();
                        customTag = ""; // 清空自定义标签输入
                    }
                }
                EditorGUILayout.EndHorizontal();
                
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
            
            // 高级设置
            showAdvancedSettings = EditorGUILayout.BeginFoldoutHeaderGroup(showAdvancedSettings, "高级设置");
            if (showAdvancedSettings)
            {
                EditorGUI.indentLevel++;
                
                // 是否在命中后销毁
                hitBoxObj.destroyOnHit = EditorGUILayout.Toggle(new GUIContent("命中后销毁", 
                    "若启用，HitBox将在命中目标后立即销毁"), hitBoxObj.destroyOnHit);
                
                // 最大命中次数
                hitBoxObj.maxHits = EditorGUILayout.IntField(new GUIContent("最大命中次数", 
                    "HitBox可以命中的最大目标数量（0表示无限制）"), hitBoxObj.maxHits);
                if (hitBoxObj.maxHits < 0)
                    hitBoxObj.maxHits = 0;
                
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
            
            // 预览说明
            EditorGUILayout.Space(10);
            EditorGUILayout.HelpBox(
                "HitBox将基于非碰撞检测进行判定，以自定义逻辑实现更精确的判定。\n" +
                "命中判定会按设定的持续时间进行，命中后会设置角色的'HasHit'和'HitChecked'条件。", 
                MessageType.Info);
            
            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(hitBoxObj);

                // 参数变化后主动刷新 CombatEditor 预览与场景视图，
                // 避免出现“Inspector 数值已改但 Scene 可视化没跟上”
                if (CombatEditorUtility.EditorExist())
                {
                    CombatEditor editor = CombatEditorUtility.GetCurrentEditor();
                    if (editor != null)
                    {
                        editor.RequirePreviewReload();
                        editor.Repaint();
                    }
                }

                SceneView.RepaintAll();
            }
        }
        
        // 自定义标签输入
        private string customTag = "";
    }
} 

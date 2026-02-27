using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

namespace CombatEditor
{
    /// <summary>
    /// ScriptableObject文件名同步工具窗口
    /// </summary>
    public class ScriptableObjectSyncWindow : EditorWindow
    {
        private Vector2 scrollPosition;
        private CombatController selectedController;
        private SyncStatusReport lastReport;
        private bool autoRefresh = true;
        private bool showSyncedObjects = false;
        private bool showUnsyncedOnly = true;
        private float lastRefreshTime = 0f;
        private const float AUTO_REFRESH_INTERVAL = 2f;
        
        // 样式
        private GUIStyle headerStyle;
        private GUIStyle statusStyle;
        private GUIStyle buttonStyle;
        
        [MenuItem("CombatEditor/ScriptableObject文件名同步工具", false, 100)]
        public static void ShowWindow()
        {
            var window = GetWindow<ScriptableObjectSyncWindow>("ScriptableObject同步工具");
            window.minSize = new Vector2(500, 400);
            window.Show();
        }
        
        void OnEnable()
        {
            // 尝试找到场景中的CombatController
            if (selectedController == null)
            {
                selectedController = FindObjectOfType<CombatController>();
            }
            
            RefreshReport();
        }
        
        void OnGUI()
        {
            InitializeStyles();
            
            EditorGUILayout.BeginVertical("box");
            
            // 标题
            EditorGUILayout.LabelField("ScriptableObject文件名同步工具", headerStyle);
            EditorGUILayout.Space();
            
            // CombatController选择
            DrawControllerSelection();
            
            EditorGUILayout.Space();
            
            // 控制按钮区域
            DrawControlButtons();
            
            EditorGUILayout.Space();
            
            // 状态报告
            DrawStatusReport();
            
            EditorGUILayout.Space();
            
            // 详细列表
            DrawDetailedList();
            
            EditorGUILayout.EndVertical();
            
            // 自动刷新
            HandleAutoRefresh();
        }
        
        void InitializeStyles()
        {
            if (headerStyle == null)
            {
                headerStyle = new GUIStyle(EditorStyles.boldLabel);
                headerStyle.fontSize = 16;
                headerStyle.alignment = TextAnchor.MiddleCenter;
            }
            
            if (statusStyle == null)
            {
                statusStyle = new GUIStyle(EditorStyles.helpBox);
                statusStyle.fontSize = 12;
            }
            
            if (buttonStyle == null)
            {
                buttonStyle = new GUIStyle(GUI.skin.button);
                buttonStyle.fontSize = 12;
            }
        }
        
        void DrawControllerSelection()
        {
            EditorGUILayout.LabelField("目标CombatController", EditorStyles.boldLabel);
            
            EditorGUI.BeginChangeCheck();
            selectedController = (CombatController)EditorGUILayout.ObjectField(
                "CombatController", 
                selectedController, 
                typeof(CombatController), 
                true
            );
            
            if (EditorGUI.EndChangeCheck())
            {
                RefreshReport();
            }
            
            if (selectedController == null)
            {
                EditorGUILayout.HelpBox("请选择一个CombatController来查看同步状态", MessageType.Info);
            }
        }
        
        void DrawControlButtons()
        {
            EditorGUILayout.LabelField("操作", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            
            // 刷新按钮
            if (GUILayout.Button("🔄 刷新状态", buttonStyle))
            {
                RefreshReport();
            }
            
            // 自动刷新开关
            autoRefresh = GUILayout.Toggle(autoRefresh, "自动刷新", GUILayout.Width(80));
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space();
            
            if (selectedController != null)
            {
                EditorGUILayout.BeginHorizontal();
                
                // 同步当前Controller的所有对象
                GUI.backgroundColor = Color.green;
                if (GUILayout.Button("🔄 同步当前Controller的所有对象", buttonStyle))
                {
                    int synced = ScriptableObjectSyncUtility.SyncAllScriptableObjects(selectedController, false);
                    EditorUtility.DisplayDialog("同步完成", $"成功同步了 {synced} 个ScriptableObject文件", "确定");
                    RefreshReport();
                }
                
                // 强制同步（即使名称相同）
                GUI.backgroundColor = Color.yellow;
                if (GUILayout.Button("⚡ 强制同步所有", buttonStyle))
                {
                    bool confirmed = EditorUtility.DisplayDialog(
                        "强制同步确认", 
                        "这将强制重命名所有ScriptableObject文件，即使它们的名称已经匹配。\n\n确定要继续吗？", 
                        "确定", 
                        "取消"
                    );
                    
                    if (confirmed)
                    {
                        int synced = ScriptableObjectSyncUtility.SyncAllScriptableObjects(selectedController, true);
                        EditorUtility.DisplayDialog("强制同步完成", $"强制同步了 {synced} 个ScriptableObject文件", "确定");
                        RefreshReport();
                    }
                }
                
                GUI.backgroundColor = Color.white;
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.Space();
                
                // 项目范围操作
                EditorGUILayout.LabelField("项目范围操作", EditorStyles.boldLabel);
                EditorGUILayout.BeginHorizontal();
                
                GUI.backgroundColor = Color.cyan;
                if (GUILayout.Button("🌐 同步项目中所有ScriptableObject", buttonStyle))
                {
                    bool confirmed = EditorUtility.DisplayDialog(
                        "项目范围同步", 
                        "这将同步项目中所有的AbilityScriptableObject文件名。\n\n确定要继续吗？", 
                        "确定", 
                        "取消"
                    );
                    
                    if (confirmed)
                    {
                        int synced = ScriptableObjectSyncUtility.SyncAllInProject(false);
                        EditorUtility.DisplayDialog("项目同步完成", $"项目范围内成功同步了 {synced} 个ScriptableObject文件", "确定");
                        RefreshReport();
                    }
                }
                
                GUI.backgroundColor = Color.white;
                EditorGUILayout.EndHorizontal();
            }
        }
        
        void DrawStatusReport()
        {
            if (lastReport == null || selectedController == null)
                return;
            
            EditorGUILayout.LabelField("同步状态", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginVertical(statusStyle);
            
            EditorGUILayout.LabelField($"📊 总计: {lastReport.TotalCount} 个ScriptableObject");
            
            if (lastReport.TotalCount > 0)
            {
                EditorGUILayout.LabelField($"✅ 已同步: {lastReport.SyncedCount} 个 ({lastReport.SyncedPercentage:F1}%)");
                EditorGUILayout.LabelField($"⚠️ 未同步: {lastReport.UnsyncedCount} 个");
                EditorGUILayout.LabelField($"❌ 无Clip: {lastReport.NoClipCount} 个");
                
                // 进度条
                Rect progressRect = EditorGUILayout.GetControlRect(false, 20);
                EditorGUI.ProgressBar(progressRect, lastReport.SyncedPercentage / 100f, $"同步进度: {lastReport.SyncedPercentage:F1}%");
            }
            
            EditorGUILayout.EndVertical();
        }
        
        void DrawDetailedList()
        {
            if (lastReport == null || selectedController == null)
                return;
            
            EditorGUILayout.LabelField("详细信息", EditorStyles.boldLabel);
            
            // 显示选项
            EditorGUILayout.BeginHorizontal();
            showUnsyncedOnly = GUILayout.Toggle(showUnsyncedOnly, "只显示未同步对象", GUILayout.Width(150));
            showSyncedObjects = GUILayout.Toggle(showSyncedObjects, "显示已同步对象", GUILayout.Width(150));
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space();
            
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, "box");
            
            // 显示未同步对象
            if (lastReport.UnsyncedObjects.Count > 0)
            {
                EditorGUILayout.LabelField("⚠️ 未同步对象:", EditorStyles.boldLabel);
                
                foreach (var info in lastReport.UnsyncedObjects)
                {
                    if (info.ScriptableObject == null) continue;
                    
                    EditorGUILayout.BeginVertical("box");
                    
                    EditorGUILayout.BeginHorizontal();
                    
                    // 对象信息
                    EditorGUILayout.BeginVertical();
                    EditorGUILayout.LabelField($"组: {info.GroupLabel}", EditorStyles.miniLabel);
                    EditorGUILayout.LabelField($"文件名: {info.CurrentName}", EditorStyles.boldLabel);
                    EditorGUILayout.LabelField($"Clip名: {info.ClipName}", EditorStyles.miniLabel);
                    EditorGUILayout.EndVertical();
                    
                    // 操作按钮
                    EditorGUILayout.BeginVertical(GUILayout.Width(100));
                    
                    if (GUILayout.Button("🔄 同步", GUILayout.Height(30)))
                    {
                        ScriptableObjectSyncUtility.SyncScriptableObjectName(info.ScriptableObject, true);
                        RefreshReport();
                    }
                    
                    if (GUILayout.Button("📂 定位", GUILayout.Height(20)))
                    {
                        EditorGUIUtility.PingObject(info.ScriptableObject);
                        Selection.activeObject = info.ScriptableObject;
                    }
                    
                    EditorGUILayout.EndVertical();
                    
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.EndVertical();
                    
                    EditorGUILayout.Space();
                }
            }
            
            // 显示已同步对象（可选）
            if (showSyncedObjects && selectedController.CombatDatas != null)
            {
                EditorGUILayout.LabelField("✅ 已同步对象:", EditorStyles.boldLabel);
                
                foreach (var group in selectedController.CombatDatas)
                {
                    if (group?.CombatObjs == null) continue;
                    
                    foreach (var obj in group.CombatObjs)
                    {
                        if (obj == null || obj.Clip == null) continue;
                        if (showUnsyncedOnly && obj.name != obj.Clip.name) continue;
                        if (obj.name == obj.Clip.name)
                        {
                            EditorGUILayout.BeginHorizontal("box");
                            EditorGUILayout.LabelField($"✅ {obj.name} (组: {group.Label})", EditorStyles.miniLabel);
                            
                            if (GUILayout.Button("📂", GUILayout.Width(30), GUILayout.Height(18)))
                            {
                                EditorGUIUtility.PingObject(obj);
                                Selection.activeObject = obj;
                            }
                            
                            EditorGUILayout.EndHorizontal();
                        }
                    }
                }
            }
            
            EditorGUILayout.EndScrollView();
        }
        
        void RefreshReport()
        {
            if (selectedController != null)
            {
                lastReport = ScriptableObjectSyncUtility.GetSyncStatusReport(selectedController);
                lastRefreshTime = Time.realtimeSinceStartup;
                Repaint();
            }
        }
        
        void HandleAutoRefresh()
        {
            if (autoRefresh && selectedController != null && 
                Time.realtimeSinceStartup - lastRefreshTime > AUTO_REFRESH_INTERVAL)
            {
                RefreshReport();
            }
        }
        
        void OnInspectorUpdate()
        {
            if (autoRefresh)
            {
                Repaint();
            }
        }
    }
} 
using UnityEngine;
using UnityEditor;

namespace CombatEditor
{
    public class RootMotionDebugWindow : EditorWindow
    {
        private CombatController _selectedController;
        private Vector2 _scrollPosition;
        private bool _autoRefresh = true;
        private float _refreshInterval = 0.1f;
        private double _lastRefreshTime;

        [MenuItem("Combat Editor/Root Motion Debug")]
        public static void ShowWindow()
        {
            var window = GetWindow<RootMotionDebugWindow>("Root Motion Debug");
            window.minSize = new Vector2(400, 300);
        }

        private void OnEnable()
        {
            _lastRefreshTime = EditorApplication.timeSinceStartup;
        }

        private void OnGUI()
        {
            DrawHeader();
            DrawControllerSelection();
            
            if (_selectedController != null)
            {
                DrawRootMotionInfo();
                DrawControlsPanel();
            }
            else
            {
                EditorGUILayout.HelpBox("请选择一个CombatController来监控Root Motion状态", MessageType.Info);
            }

            // 自动刷新
            if (_autoRefresh && EditorApplication.timeSinceStartup - _lastRefreshTime > _refreshInterval)
            {
                _lastRefreshTime = EditorApplication.timeSinceStartup;
                Repaint();
            }
        }

        private void DrawHeader()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Label("Root Motion 调试工具", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            
            _autoRefresh = GUILayout.Toggle(_autoRefresh, "自动刷新", EditorStyles.toolbarButton);
            
            if (GUILayout.Button("手动刷新", EditorStyles.toolbarButton))
            {
                Repaint();
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space();
        }

        private void DrawControllerSelection()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("目标Controller:", GUILayout.Width(100));
            
            var newController = EditorGUILayout.ObjectField(_selectedController, typeof(CombatController), true) as CombatController;
            if (newController != _selectedController)
            {
                _selectedController = newController;
                Repaint();
            }
            
            if (GUILayout.Button("选择场景中的", GUILayout.Width(100)))
            {
                var controllers = FindObjectsOfType<CombatController>();
                if (controllers.Length > 0)
                {
                    _selectedController = controllers[0];
                    Selection.activeGameObject = _selectedController.gameObject;
                }
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space();
        }

        private void DrawRootMotionInfo()
        {
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
            
            // 基本信息
            EditorGUILayout.LabelField("基本信息", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            EditorGUILayout.LabelField("Controller名称:", _selectedController.name);
            EditorGUILayout.LabelField("Animator状态:", _selectedController._animator != null ? "已连接" : "未连接");
            
            if (_selectedController._animator != null)
            {
                EditorGUILayout.LabelField("Unity Root Motion:", _selectedController._animator.applyRootMotion ? "启用" : "禁用");
            }
            
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();

            // Root Motion接收器信息
            var receiver = _selectedController._animator?.GetComponent<RootMotionReceiver>();
            if (receiver != null)
            {
                EditorGUILayout.LabelField("Root Motion 接收器", EditorStyles.boldLabel);
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                
                EditorGUILayout.LabelField("手动控制:", receiver.manualRootMotionControl ? "是" : "否");
                EditorGUILayout.LabelField("应用位置:", receiver.applyRootPosition ? "是" : "否");
                EditorGUILayout.LabelField("应用旋转:", receiver.applyRootRotation ? "是" : "否");
                EditorGUILayout.LabelField("自动检测旋转:", receiver.autoDetectRootRotation ? "是" : "否");
                
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("旋转阈值:", $"{receiver.rotationThreshold:F3}°");
                
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("当前Root Motion:", receiver.CurrentRootMotion.ToString("F4"));
                EditorGUILayout.LabelField("当前Root旋转:", receiver.CurrentRootRotation.ToString("F4"));
                EditorGUILayout.LabelField("累积Root Motion:", receiver.AccumulatedRootMotion.ToString("F4"));
                
                // 显示是否检测到明显的root rotation
                var rotationColor = receiver.HasSignificantRootRotation ? Color.yellow : Color.gray;
                var oldColor = GUI.color;
                GUI.color = rotationColor;
                EditorGUILayout.LabelField("检测到明显旋转:", receiver.HasSignificantRootRotation ? "是" : "否");
                GUI.color = oldColor;
                
                // 显示当前旋转角度
                EditorGUILayout.LabelField("当前旋转角度:", $"{receiver.CurrentRotationAngle:F3}°");
                
                // 可视化Root Motion大小
                float motionMagnitude = receiver.CurrentRootMotion.magnitude;
                EditorGUILayout.LabelField("Root Motion强度:", motionMagnitude.ToString("F4"));
                
                Rect rect = GUILayoutUtility.GetRect(200, 20);
                EditorGUI.ProgressBar(rect, Mathf.Clamp01(motionMagnitude * 10), $"强度: {motionMagnitude:F4}");
                
                // 可视化Root Rotation大小
                float rotationAngle = receiver.CurrentRotationAngle;
                EditorGUILayout.LabelField("Root Rotation角度:", rotationAngle.ToString("F2") + "°");
                
                Rect rotRect = GUILayoutUtility.GetRect(200, 20);
                EditorGUI.ProgressBar(rotRect, Mathf.Clamp01(rotationAngle / 180f), $"旋转角度: {rotationAngle:F2}°");
                
                EditorGUILayout.EndVertical();
                EditorGUILayout.Space();
            }
            else
            {
                EditorGUILayout.HelpBox("Root Motion接收器未找到", MessageType.Warning);
            }

            // 移动执行器信息
            if (_selectedController._moveExecutor != null)
            {
                EditorGUILayout.LabelField("移动执行器", EditorStyles.boldLabel);
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                
                EditorGUILayout.LabelField("调试信息:", _selectedController.GetRootMotionDebugInfo());
                
                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawControlsPanel()
        {
            EditorGUILayout.LabelField("控制面板", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("启用Root Motion"))
            {
                _selectedController.SetRootMotionEnabled(true, "手动启用");
            }
            if (GUILayout.Button("禁用Root Motion"))
            {
                _selectedController.SetRootMotionEnabled(false, "手动禁用");
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("重置Root Motion数据"))
            {
                _selectedController.ResetRootMotion();
            }
            if (GUILayout.Button("获取累积Root Motion"))
            {
                Vector3 accumulated = _selectedController.GetAndResetAccumulatedRootMotion();
                Debug.Log($"累积Root Motion: {accumulated}");
            }
            EditorGUILayout.EndHorizontal();
            
            // 简化的旋转控制
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("旋转控制:", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            
            var receiver = _selectedController._animator?.GetComponent<RootMotionReceiver>();
            if (receiver != null)
            {
                if (GUILayout.Button("重置旋转到身份四元数"))
                {
                    receiver.ForceSetRotation(Quaternion.identity);
                    Debug.Log("已重置旋转到(0,0,0)");
                }
                
                if (GUILayout.Button("重置累积数据"))
                {
                    receiver.ResetAccumulatedRootMotion();
                    Debug.Log("已重置累积Root Motion数据");
                }
                
                // 显示调试信息按钮
                if (GUILayout.Button("打印调试信息"))
                {
                    Debug.Log($"RootMotionReceiver调试信息: {receiver.GetDebugInfo()}");
                }
            }
            else
            {
                EditorGUILayout.HelpBox("未找到RootMotionReceiver组件", MessageType.Warning);
            }
            
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("刷新设置:");
            _refreshInterval = EditorGUILayout.Slider("刷新间隔 (秒)", _refreshInterval, 0.05f, 1f);

            EditorGUILayout.EndVertical();
        }
    }
} 
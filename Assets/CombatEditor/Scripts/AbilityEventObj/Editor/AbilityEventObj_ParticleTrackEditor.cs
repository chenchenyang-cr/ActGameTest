#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace CombatEditor
{
    /// <summary>
    /// 粒子轨道事件的自定义编辑器
    /// 提供直观的配置界面和实时预览功能
    /// </summary>
    [CustomEditor(typeof(AbilityEventObj_ParticleTrack))]
    public class AbilityEventObj_ParticleTrackEditor : Editor
    {
        private AbilityEventObj_ParticleTrack particleTrack;
        private bool showPlaybackSettings = true;
        private bool showTimeControlSettings = true;
        private bool showPositionSettings = true;
        private bool showAdvancedSettings = false;
        private bool showIntensitySettings = false;
        private bool showDebugSettings = false;
        private bool showPreviewSettings = true;
        
        // 预览相关
        private bool isRealTimePreview = false;
        private float previewProgress = 0f;
        private bool showTrackVisualization = true;
        
        // GUI样式
        private GUIStyle headerStyle;
        private GUIStyle boxStyle;
        private GUIStyle buttonStyle;
        
        void OnEnable()
        {
            particleTrack = (AbilityEventObj_ParticleTrack)target;
            InitializeGUIStyles();
            
            // 开始监听编辑器更新
            EditorApplication.update += OnEditorUpdate;
        }
        
        void OnDisable()
        {
            // 停止监听编辑器更新
            EditorApplication.update -= OnEditorUpdate;
        }
        
        private float lastUpdateTime = 0f;
        void OnEditorUpdate()
        {
            // 限制更新频率，避免过于频繁的更新
            if (EditorApplication.timeSinceStartup - lastUpdateTime < 0.1f) return;
            lastUpdateTime = (float)EditorApplication.timeSinceStartup;
            
            // 只有在不播放且CombatEditor存在时才进行实时更新
            if (!Application.isPlaying && CombatEditorUtility.EditorExist())
            {
                var editor = CombatEditorUtility.GetCurrentEditor();
                if (editor != null && editor.SelectedAbilityObj != null)
                {
                    // 检查当前选中的事件是否是我们的粒子轨道事件
                    bool isCurrentEventSelected = false;
                    if (editor.SelectedTrackIndex > 0 && editor.SelectedTrackIndex <= editor.SelectedAbilityObj.events.Count)
                    {
                        var selectedEvent = editor.SelectedAbilityObj.events[editor.SelectedTrackIndex - 1];
                        isCurrentEventSelected = (selectedEvent.Obj == particleTrack);
                    }
                    
                    if (isCurrentEventSelected)
                    {
                        // 重绘Scene视图以确保实时更新
                        SceneView.RepaintAll();
                    }
                }
            }
        }
        
        void InitializeGUIStyles()
        {
            if (headerStyle == null)
            {
                headerStyle = new GUIStyle(EditorStyles.boldLabel);
                headerStyle.fontSize = 12;
                headerStyle.normal.textColor = new Color(0.8f, 0.9f, 1f);
            }
            
            if (boxStyle == null)
            {
                boxStyle = new GUIStyle("Box");
                boxStyle.padding = new RectOffset(10, 10, 5, 5);
            }
            
            if (buttonStyle == null)
            {
                buttonStyle = new GUIStyle("Button");
                buttonStyle.fixedHeight = 25;
            }
        }
        
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            
            DrawHeader();
            EditorGUILayout.Space();
            
            DrawParticlePrefabSection();
            EditorGUILayout.Space();
            
            DrawPlaybackSettingsSection();
            EditorGUILayout.Space();
            
            DrawTimeControlSection();
            EditorGUILayout.Space();
            
            DrawPositionSettingsSection();
            EditorGUILayout.Space();
            
            DrawIntensityControlSection();
            EditorGUILayout.Space();
            
            DrawAdvancedSettingsSection();
            EditorGUILayout.Space();
            
            DrawPreviewSection();
            EditorGUILayout.Space();
            
            DrawDebugSection();
            EditorGUILayout.Space();
            
            DrawTrackInfoSection();
            
            serializedObject.ApplyModifiedProperties();
            
            // 检测变化并更新预览
            if (GUI.changed && !Application.isPlaying)
            {
                UpdatePreview();
                
                // 立即重绘Inspector以反映变化
                Repaint();
            }
        }
        
        void DrawHeader()
        {
            EditorGUILayout.BeginVertical(boxStyle);
            
            GUILayout.Label("🎆 粒子轨道事件", headerStyle);
            EditorGUILayout.LabelField("为技能创建完全跟随轨道进度的粒子效果", EditorStyles.miniLabel);
            
            EditorGUILayout.EndVertical();
        }
        
        void DrawParticlePrefabSection()
        {
            EditorGUILayout.BeginVertical(boxStyle);
            
            GUILayout.Label("🎨 粒子对象设置", headerStyle);
            
            SerializedProperty particlePrefabProp = serializedObject.FindProperty("particleData.particlePrefab");
            EditorGUILayout.PropertyField(particlePrefabProp, new GUIContent("粒子预制体", "要在轨道中播放的粒子系统预制体"));
            
            if (particlePrefabProp.objectReferenceValue == null)
            {
                EditorGUILayout.HelpBox("⚠️ 请指定一个包含ParticleSystem组件的预制体", MessageType.Warning);
            }
            else
            {
                GameObject prefab = particlePrefabProp.objectReferenceValue as GameObject;
                ParticleSystem[] particleSystems = prefab.GetComponentsInChildren<ParticleSystem>();
                
                if (particleSystems.Length == 0)
                {
                    EditorGUILayout.HelpBox("❌ 选中的预制体不包含ParticleSystem组件", MessageType.Error);
                }
                else
                {
                    EditorGUILayout.HelpBox($"✅ 检测到 {particleSystems.Length} 个粒子系统", MessageType.Info);
                }
            }
            
            EditorGUILayout.EndVertical();
        }
        
        void DrawPlaybackSettingsSection()
        {
            EditorGUILayout.BeginVertical(boxStyle);
            
            showPlaybackSettings = EditorGUILayout.Foldout(showPlaybackSettings, "⏯️ 轨道播放控制", true);
            
            if (showPlaybackSettings)
            {
                EditorGUI.indentLevel++;
                
                SerializedProperty playOnStartProp = serializedObject.FindProperty("particleData.playOnStart");
                SerializedProperty stopOnEndProp = serializedObject.FindProperty("particleData.stopOnEnd");
                SerializedProperty clearOnExitProp = serializedObject.FindProperty("particleData.clearParticlesOnExit");
                SerializedProperty destroyDelayProp = serializedObject.FindProperty("particleData.destroyDelay");
                
                EditorGUILayout.PropertyField(playOnStartProp, new GUIContent("进入轨道时播放", "是否在时间轴进入轨道范围时自动开始播放粒子"));
                EditorGUILayout.PropertyField(stopOnEndProp, new GUIContent("退出轨道时停止", "是否在时间轴退出轨道范围时自动停止粒子"));
                EditorGUILayout.PropertyField(clearOnExitProp, new GUIContent("退出时清除粒子", "是否在退出轨道时立即清除所有粒子"));
                EditorGUILayout.PropertyField(destroyDelayProp, new GUIContent("销毁延迟 (秒)", "轨道结束后延迟多长时间销毁粒子对象"));
                
                EditorGUI.indentLevel--;
            }
            
            EditorGUILayout.EndVertical();
        }
        
        void DrawTimeControlSection()
        {
            EditorGUILayout.BeginVertical(boxStyle);
            
            showTimeControlSettings = EditorGUILayout.Foldout(showTimeControlSettings, "⏰ 轨道时间控制", true);
            
            if (showTimeControlSettings)
            {
                EditorGUI.indentLevel++;
                
                SerializedProperty speedModeProp = serializedObject.FindProperty("particleData.speedMode");
                SerializedProperty playbackSpeedProp = serializedObject.FindProperty("particleData.playbackSpeed");
                SerializedProperty speedCurveProp = serializedObject.FindProperty("particleData.speedCurve");
                SerializedProperty loopParticlesProp = serializedObject.FindProperty("particleData.loopParticles");
                SerializedProperty progressCurveProp = serializedObject.FindProperty("particleData.progressCurve");
                
                // 速度控制模式选择
                EditorGUILayout.LabelField("速度控制模式", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(speedModeProp, new GUIContent("控制模式", "选择速度控制的方式"));
                
                EditorGUILayout.Space();
                
                // 根据选择的模式显示对应的设置
                SpeedControlMode currentMode = (SpeedControlMode)speedModeProp.enumValueIndex;
                
                if (currentMode == SpeedControlMode.BaseSpeed)
                {
                    EditorGUILayout.LabelField("基础速度设置", EditorStyles.boldLabel);
                    EditorGUILayout.PropertyField(playbackSpeedProp, new GUIContent("播放速度倍率", "粒子系统的播放速度倍率，1.0为正常速度"));
                    
                    if (playbackSpeedProp.floatValue <= 0f)
                    {
                        EditorGUILayout.HelpBox("⚠️ 播放速度不能小于等于0，否则粒子将无法播放", MessageType.Warning);
                    }
                    else
                    {
                        EditorGUILayout.HelpBox("✅ 使用固定的播放速度", MessageType.Info);
                    }
                }
                else if (currentMode == SpeedControlMode.CurveSpeed)
                {
                    EditorGUILayout.LabelField("曲线速度设置", EditorStyles.boldLabel);
                    EditorGUILayout.PropertyField(speedCurveProp, new GUIContent("速度变化曲线", "控制粒子播放速度随时间的变化"));
                    
                    EditorGUILayout.HelpBox("💡 使用曲线可以创建变速效果，如加速、减速、停顿等", MessageType.Info);
                    
                    // 显示曲线的开始和结束值
                    SerializedProperty startValueProp = speedCurveProp.FindPropertyRelative("StartValue");
                    SerializedProperty endValueProp = speedCurveProp.FindPropertyRelative("EndValue");
                    
                    if (startValueProp != null && endValueProp != null)
                    {
                        float startValue = startValueProp.floatValue;
                        float endValue = endValueProp.floatValue;
                        
                        EditorGUILayout.LabelField($"起始速度: {startValue:F2}, 结束速度: {endValue:F2}");
                        
                        if (startValue <= 0f || endValue <= 0f)
                        {
                            EditorGUILayout.HelpBox("⚠️ 曲线中包含小于等于0的速度值，可能导致粒子播放异常", MessageType.Warning);
                        }
                    }
                }
                
                EditorGUILayout.Space();
                
                // 通用设置
                EditorGUILayout.LabelField("通用设置", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(loopParticlesProp, new GUIContent("循环播放", "是否循环播放粒子效果"));
                
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("进度映射曲线", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(progressCurveProp, new GUIContent("", "控制粒子播放进度与轨道时间的映射关系"));
                EditorGUILayout.HelpBox("💡 进度曲线控制粒子播放的时间映射，与速度控制是独立的", MessageType.Info);
                
                EditorGUI.indentLevel--;
            }
            
            EditorGUILayout.EndVertical();
        }
        
        void DrawPositionSettingsSection()
        {
            EditorGUILayout.BeginVertical(boxStyle);
            
            showPositionSettings = EditorGUILayout.Foldout(showPositionSettings, "📍 位置绑定设置", true);
            
            if (showPositionSettings)
            {
                EditorGUI.indentLevel++;
                
                SerializedProperty targetNodeProp = serializedObject.FindProperty("particleData.targetNode");
                SerializedProperty positionOffsetProp = serializedObject.FindProperty("particleData.positionOffset");
                SerializedProperty rotationOffsetProp = serializedObject.FindProperty("particleData.rotationOffset");
                SerializedProperty followMovementProp = serializedObject.FindProperty("particleData.followNodeMovement");
                SerializedProperty followRotationProp = serializedObject.FindProperty("particleData.followNodeRotation");
                
                SerializedProperty useCustomTargetProp = serializedObject.FindProperty("particleData.useCustomTarget");
                SerializedProperty customTargetNameProp = serializedObject.FindProperty("particleData.customTargetName");
                SerializedProperty customParentProp = serializedObject.FindProperty("particleData.customParent");
                
                EditorGUILayout.PropertyField(targetNodeProp, new GUIContent("绑定节点", "粒子效果绑定到角色的哪个节点"));
                EditorGUILayout.PropertyField(positionOffsetProp, new GUIContent("位置偏移", "相对于绑定节点的位置偏移"));
                EditorGUILayout.PropertyField(rotationOffsetProp, new GUIContent("旋转偏移", "相对于绑定节点的旋转偏移（欧拉角）"));
                
                EditorGUILayout.Space();
                EditorGUILayout.PropertyField(followMovementProp, new GUIContent("跟随移动", "是否实时跟随节点的位置变化"));
                EditorGUILayout.PropertyField(followRotationProp, new GUIContent("跟随旋转", "是否实时跟随节点的旋转变化"));
                
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("高级绑定选项", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(useCustomTargetProp, new GUIContent("使用自定义目标", "是否使用场景中的自定义对象作为绑定目标"));
                
                if (useCustomTargetProp.boolValue)
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(customTargetNameProp, new GUIContent("目标对象名称", "场景中目标对象的名称"));
                    EditorGUI.indentLevel--;
                }
                
                EditorGUILayout.PropertyField(customParentProp, new GUIContent("自定义父级", "为粒子对象指定自定义的父级变换"));
                
                EditorGUI.indentLevel--;
            }
            
            EditorGUILayout.EndVertical();
        }
        
        void DrawIntensityControlSection()
        {
            EditorGUILayout.BeginVertical(boxStyle);
            
            showIntensitySettings = EditorGUILayout.Foldout(showIntensitySettings, "💫 粒子强度控制", true);
            
            if (showIntensitySettings)
            {
                EditorGUI.indentLevel++;
                
                SerializedProperty useIntensityCurveProp = serializedObject.FindProperty("particleData.useIntensityCurve");
                SerializedProperty intensityCurveProp = serializedObject.FindProperty("particleData.intensityCurve");
                SerializedProperty maxIntensityProp = serializedObject.FindProperty("particleData.maxIntensityMultiplier");
                
                EditorGUILayout.PropertyField(useIntensityCurveProp, new GUIContent("启用强度控制", "是否使用曲线控制粒子发射强度"));
                
                if (useIntensityCurveProp.boolValue)
                {
                    EditorGUILayout.PropertyField(intensityCurveProp, new GUIContent("强度曲线", "控制粒子发射率随轨道时间的变化"));
                    EditorGUILayout.PropertyField(maxIntensityProp, new GUIContent("最大强度倍率", "强度曲线的最大值对应的倍率"));
                    
                    EditorGUILayout.HelpBox("💡 强度曲线可以创建渐强、渐弱、爆发等效果", MessageType.Info);
                }
                
                EditorGUI.indentLevel--;
            }
            
            EditorGUILayout.EndVertical();
        }
        
        void DrawAdvancedSettingsSection()
        {
            EditorGUILayout.BeginVertical(boxStyle);
            
            showAdvancedSettings = EditorGUILayout.Foldout(showAdvancedSettings, "⚙️ 高级设置", true);
            
            if (showAdvancedSettings)
            {
                EditorGUI.indentLevel++;
                
                EditorGUILayout.LabelField("性能优化", EditorStyles.boldLabel);
                EditorGUILayout.HelpBox("• 避免同时播放过多粒子轨道\n• 合理设置销毁延迟时间\n• 使用LOD系统控制远距离粒子", MessageType.Info);
                
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("兼容性说明", EditorStyles.boldLabel);
                EditorGUILayout.HelpBox("• 支持所有标准的Unity粒子系统特性\n• 兼容现有的CombatEditor轨道系统\n• 支持多个粒子系统的复合效果", MessageType.Info);
                
                EditorGUI.indentLevel--;
            }
            
            EditorGUILayout.EndVertical();
        }
        
        void DrawPreviewSection()
        {
            EditorGUILayout.BeginVertical(boxStyle);
            
            showPreviewSettings = EditorGUILayout.Foldout(showPreviewSettings, "👁️ 预览控制", true);
            
            if (showPreviewSettings)
            {
                EditorGUI.indentLevel++;
                
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("🔄 刷新预览", buttonStyle))
                {
                    UpdatePreview();
                }
                
                if (GUILayout.Button("🧹 清除预览", buttonStyle))
                {
                    ClearPreview();
                }
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.Space();
                
                showTrackVisualization = EditorGUILayout.Toggle("显示轨道可视化", showTrackVisualization);
                
                if (!Application.isPlaying)
                {
                    EditorGUILayout.LabelField("预览进度", EditorStyles.boldLabel);
                    previewProgress = EditorGUILayout.Slider(previewProgress, 0f, 1f);
                    
                    if (GUILayout.Button("模拟轨道进度", buttonStyle))
                    {
                        SimulateTrackProgress(previewProgress);
                    }
                }
                else
                {
                    EditorGUILayout.HelpBox("运行时预览请使用CombatEditor", MessageType.Info);
                }
                
                EditorGUI.indentLevel--;
            }
            
            EditorGUILayout.EndVertical();
        }
        
        void DrawDebugSection()
        {
            EditorGUILayout.BeginVertical(boxStyle);
            
            showDebugSettings = EditorGUILayout.Foldout(showDebugSettings, "🐛 调试设置", true);
            
            if (showDebugSettings)
            {
                EditorGUI.indentLevel++;
                
                SerializedProperty enableDebugLogProp = serializedObject.FindProperty("enableDebugLog");
                EditorGUILayout.PropertyField(enableDebugLogProp, new GUIContent("启用调试日志", "在控制台输出详细的调试信息"));
                
                if (enableDebugLogProp.boolValue)
                {
                    EditorGUILayout.HelpBox("📝 调试日志将显示轨道状态变化、粒子控制等详细信息", MessageType.Info);
                }
                
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("快速诊断", EditorStyles.boldLabel);
                
                if (GUILayout.Button("🔍 检查配置", buttonStyle))
                {
                    DiagnoseConfiguration();
                }
                
                EditorGUI.indentLevel--;
            }
            
            EditorGUILayout.EndVertical();
        }
        
        void DrawTrackInfoSection()
        {
            EditorGUILayout.BeginVertical(boxStyle);
            
            GUILayout.Label("📊 轨道信息", headerStyle);
            
            EditorGUILayout.LabelField("事件类型", "EventRange (轨道事件)");
            EditorGUILayout.LabelField("支持预览", "是");
            EditorGUILayout.LabelField("运行时性能", "中等");
            
            if (particleTrack.particleData.particlePrefab != null)
            {
                GameObject prefab = particleTrack.particleData.particlePrefab;
                ParticleSystem[] systems = prefab.GetComponentsInChildren<ParticleSystem>();
                EditorGUILayout.LabelField("粒子系统数量", systems.Length.ToString());
            }
            
            EditorGUILayout.EndVertical();
        }
        
        void UpdatePreview()
        {
            // 通知CombatEditor更新预览
            if (CombatEditorUtility.EditorExist())
            {
                CombatEditorUtility.ReloadAnimEvents();
            }
            
            // 强制更新当前帧的预览
            var editor = CombatEditorUtility.GetCurrentEditor();
            if (editor != null && editor._previewer != null)
            {
                // 强制重新计算当前帧的预览
                editor.HardResetPreviewToCurrentFrame();
            }
            
            // 重绘Scene视图
            SceneView.RepaintAll();
        }
        
        void ClearPreview()
        {
            // 清除预览对象
            GameObject previewGroup = GameObject.Find(CombatGlobalEditorValue.PreviewGroupName);
            if (previewGroup != null)
            {
                var particleTrackPreviews = previewGroup.GetComponentsInChildren<Transform>();
                foreach (var preview in particleTrackPreviews)
                {
                    if (preview.name.Contains("Preview_ParticleTrack"))
                    {
                        DestroyImmediate(preview.gameObject);
                    }
                }
            }
            
            SceneView.RepaintAll();
        }
        
        void SimulateTrackProgress(float progress)
        {
            // 这里可以添加轨道进度模拟逻辑
            Debug.Log($"模拟粒子轨道进度: {progress * 100:F1}%");
        }
        
        void DiagnoseConfiguration()
        {
            List<string> issues = new List<string>();
            List<string> suggestions = new List<string>();
            
            // 检查粒子预制体
            if (particleTrack.particleData.particlePrefab == null)
            {
                issues.Add("❌ 未指定粒子预制体");
                suggestions.Add("请在'粒子对象设置'中指定一个包含ParticleSystem的预制体");
            }
            else
            {
                GameObject prefab = particleTrack.particleData.particlePrefab;
                ParticleSystem[] systems = prefab.GetComponentsInChildren<ParticleSystem>();
                
                if (systems.Length == 0)
                {
                    issues.Add("❌ 粒子预制体不包含ParticleSystem组件");
                    suggestions.Add("确保预制体或其子对象包含ParticleSystem组件");
                }
                else
                {
                    suggestions.Add($"✅ 检测到 {systems.Length} 个粒子系统");
                }
            }
            
            // 检查速度设置
            if (particleTrack.particleData.speedMode == SpeedControlMode.BaseSpeed)
            {
                if (particleTrack.particleData.playbackSpeed <= 0)
                {
                    issues.Add("⚠️ 基础速度小于等于0");
                    suggestions.Add("基础播放速度应该大于0，建议设置为0.1-5.0之间");
                }
                else
                {
                    suggestions.Add($"✅ 基础速度设置正常: {particleTrack.particleData.playbackSpeed:F2}");
                }
            }
            else if (particleTrack.particleData.speedMode == SpeedControlMode.CurveSpeed)
            {
                if (particleTrack.particleData.speedCurve != null)
                {
                    var curve = particleTrack.particleData.speedCurve;
                    if (curve.StartValue <= 0f || curve.EndValue <= 0f)
                    {
                        issues.Add("⚠️ 速度曲线包含小于等于0的值");
                        suggestions.Add("速度曲线的所有值都应该大于0，建议最小值设为0.1");
                    }
                    else
                    {
                        suggestions.Add($"✅ 速度曲线设置正常: {curve.StartValue:F2} → {curve.EndValue:F2}");
                    }
                }
                else
                {
                    issues.Add("❌ 曲线速度模式下未设置速度曲线");
                    suggestions.Add("请为曲线速度模式设置一个有效的TweenCurve");
                }
            }
            
            // 检查强度设置
            if (particleTrack.particleData.useIntensityCurve && particleTrack.particleData.maxIntensityMultiplier <= 0)
            {
                issues.Add("⚠️ 启用了强度控制但最大强度为0");
                suggestions.Add("请设置大于0的最大强度倍率");
            }
            
            // 显示诊断结果
            string message = "";
            if (issues.Count > 0)
            {
                message += "发现以下问题:\n" + string.Join("\n", issues) + "\n\n";
            }
            if (suggestions.Count > 0)
            {
                message += "建议:\n" + string.Join("\n", suggestions);
            }
            
            if (string.IsNullOrEmpty(message))
            {
                message = "✅ 配置检查通过，没有发现问题！";
            }
            
            EditorUtility.DisplayDialog("配置诊断", message, "确定");
        }
        
        /// <summary>
        /// Scene视图中的实时预览更新
        /// </summary>
        void OnSceneGUI()
        {
            if (particleTrack == null) return;
            
            // 检查是否有CombatEditor在运行
            if (!CombatEditorUtility.EditorExist()) return;
            
            var editor = CombatEditorUtility.GetCurrentEditor();
            if (editor == null || editor._previewer == null) return;
            
            // 在Scene视图中显示位置偏移的可视化辅助
            if (particleTrack.particleData.particlePrefab != null)
            {
                DrawPositionOffsetHandle();
            }
        }
        
        /// <summary>
        /// 绘制位置偏移的可视化手柄
        /// </summary>
        void DrawPositionOffsetHandle()
        {
            var editor = CombatEditorUtility.GetCurrentEditor();
            if (editor == null || editor.SelectedController == null) return;
            
            // 获取目标变换
            Transform targetTransform = null;
            if (particleTrack.particleData.useCustomTarget && !string.IsNullOrEmpty(particleTrack.particleData.customTargetName))
            {
                GameObject customTarget = GameObject.Find(particleTrack.particleData.customTargetName);
                targetTransform = customTarget?.transform;
            }
            else
            {
                targetTransform = editor.SelectedController.GetNodeTranform(particleTrack.particleData.targetNode);
            }
            
            if (targetTransform == null) return;
            
            // 计算世界空间的偏移位置
            Vector3 worldOffset = targetTransform.rotation * particleTrack.particleData.positionOffset;
            Vector3 offsetPosition = targetTransform.position + worldOffset;
            
            // 绘制连接线
            Handles.color = Color.cyan;
            Handles.DrawDottedLine(targetTransform.position, offsetPosition, 5f);
            
            // 绘制位置手柄
            EditorGUI.BeginChangeCheck();
            Vector3 newOffsetPosition = Handles.PositionHandle(offsetPosition, targetTransform.rotation);
            
            if (EditorGUI.EndChangeCheck())
            {
                // 计算新的本地偏移
                Vector3 newWorldOffset = newOffsetPosition - targetTransform.position;
                Vector3 newLocalOffset = Quaternion.Inverse(targetTransform.rotation) * newWorldOffset;
                
                // 记录撤销操作
                Undo.RecordObject(particleTrack, "修改粒子位置偏移");
                
                // 应用新的偏移
                particleTrack.particleData.positionOffset = newLocalOffset;
                
                // 标记对象为已修改
                EditorUtility.SetDirty(particleTrack);
                
                // 立即更新预览
                UpdatePreview();
            }
            
            // 绘制标签
            Handles.Label(offsetPosition, $"Particle Offset\n{particleTrack.particleData.positionOffset.ToString("F2")}");
        }
    }
}
#endif 
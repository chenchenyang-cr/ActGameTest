using System.Collections.Generic;
using UnityEngine;
using CombatEditor;

namespace CombatEditor
{
    /// <summary>
    /// 运行时参数包装器的使用示例
    /// 展示如何通过面向对象的方式访问和修改参数：CombatController -> Actions -> Tracks -> Parameters
    /// </summary>
    public class RuntimeParameterWrapperExample : MonoBehaviour
    {
        [Header("目标CombatController")]
        public CombatController targetCombatController;
        
        [Header("示例参数")]
        [SerializeField] private float speedMultiplier = 1.5f;
        [SerializeField] private Vector3 hitBoxSizeMultiplier = new Vector3(1.2f, 1.2f, 1.2f);
        [SerializeField] private bool enableDebugMode = true;
        
        private RuntimeParameterWrapper _parameterWrapper;
        
        void Start()
        {
            // 获取运行时参数包装器
            if (targetCombatController == null)
            {
                targetCombatController = GetComponent<CombatController>();
            }
            
            if (targetCombatController != null)
            {
                _parameterWrapper = targetCombatController.GetRuntimeParameterWrapper();
                
                if (enableDebugMode)
                {
                    Debug.Log($"运行时参数包装器初始化完成，动作数量: {_parameterWrapper.GetActions().Count}");
                    LogAllActionsInfo();
                }
            }
        }
        
        void Update()
        {
            // 示例：按键控制参数覆盖
            if (Input.GetKeyDown(KeyCode.Q))
            {
                ModifySpeedByActionName();
            }
            
            if (Input.GetKeyDown(KeyCode.W))
            {
                ModifyHitBoxByTrackType();
            }
            
            if (Input.GetKeyDown(KeyCode.E))
            {
                ModifyParametersByCondition();
            }
            
            if (Input.GetKeyDown(KeyCode.R))
            {
                ShowParameterInfo();
            }
            
            if (Input.GetKeyDown(KeyCode.T))
            {
                ClearSpecificActionOverrides();
            }
            
            if (Input.GetKeyDown(KeyCode.Y))
            {
                ModifyMultipleParametersOnSameTrack();
            }
            
            if (Input.GetKeyDown(KeyCode.U))
            {
                ModifyParametersByTrackName();
            }
            
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                ClearAllOverrides();
            }
        }
        
        /// <summary>
        /// 示例1：通过动作名称修改参数
        /// </summary>
        [ContextMenu("通过动作名称修改速度")]
        public void ModifySpeedByActionName()
        {
            if (_parameterWrapper == null) return;
            
            Debug.Log("=== 通过动作名称修改速度 ===");
            
            // 获取指定动作
            var attackAction = _parameterWrapper.GetAction("Attack1");
            if (attackAction != null)
            {
                Debug.Log($"找到动作: {attackAction.Name}");
                
                // 获取该动作的所有轨道
                var tracks = attackAction.GetTracks();
                Debug.Log($"轨道数量: {tracks.Count}");
                
                // 修改动画速度轨道的参数
                foreach (var track in tracks)
                {
                    if (track.TypeName == "AbilityEventObj_AnimSpeed")
                    {
                        float originalSpeed = track.GetOriginalParameter<float>("Speed");
                        float newSpeed = originalSpeed * speedMultiplier;
                        
                        track.SetParameter("Speed", newSpeed);
                        
                        Debug.Log($"轨道 {track.Name}: 速度从 {originalSpeed} 修改为 {newSpeed}");
                    }
                }
            }
            else
            {
                Debug.LogWarning("未找到名为 'Attack1' 的动作");
            }
        }
        
        /// <summary>
        /// 示例2：通过轨道类型修改参数
        /// </summary>
        [ContextMenu("通过轨道类型修改碰撞盒")]
        public void ModifyHitBoxByTrackType()
        {
            if (_parameterWrapper == null) return;
            
            Debug.Log("=== 通过轨道类型修改碰撞盒 ===");
            
            // 获取所有动作
            var actions = _parameterWrapper.GetActions();
            
            foreach (var action in actions)
            {
                // 获取指定类型的轨道
                var hitBoxTracks = action.GetTracks<AbilityEventObj_CreateHitBox>();
                
                foreach (var track in hitBoxTracks)
                {
                    Vector3 originalSize = track.GetOriginalParameter<Vector3>("hitBoxSize");
                    Vector3 newSize = Vector3.Scale(originalSize, hitBoxSizeMultiplier);
                    
                    track.SetParameter("hitBoxSize", newSize);
                    
                    Debug.Log($"动作 {action.Name} 的轨道 {track.Name}: 碰撞盒大小从 {originalSize} 修改为 {newSize}");
                }
            }
        }
        
        /// <summary>
        /// 示例3：条件性修改参数
        /// </summary>
        [ContextMenu("条件性修改参数")]
        public void ModifyParametersByCondition()
        {
            if (_parameterWrapper == null) return;
            
            Debug.Log("=== 条件性修改参数 ===");
            
            // 获取包含"Attack"的动作
            var attackActions = _parameterWrapper.GetActionsContaining("Attack");
            
            foreach (var action in attackActions)
            {
                Debug.Log($"处理攻击动作: {action.Name}");
                
                var tracks = action.GetTracks();
                foreach (var track in tracks)
                {
                    // 如果是移动轨道，缩短移动时间
                    if (track.TypeName == "AbilityEventObj_Motion")
                    {
                        float originalTime = track.GetOriginalParameter<float>("MotionTime");
                        float newTime = originalTime * 0.5f;
                        
                        track.SetParameter("MotionTime", newTime);
                        
                        Debug.Log($"轨道 {track.Name}: 移动时间从 {originalTime} 修改为 {newTime}");
                    }
                    
                    // 如果是事件范围，调整时间范围
                    if (track.GetParameter("EventRange") != null)
                    {
                        Vector2 originalRange = track.GetOriginalParameter<Vector2>("EventRange");
                        Vector2 newRange = new Vector2(originalRange.x * 0.8f, originalRange.y * 1.2f);
                        
                        track.SetParameter("EventRange", newRange);
                        
                        Debug.Log($"轨道 {track.Name}: 事件范围从 {originalRange} 修改为 {newRange}");
                    }
                }
            }
        }
        
        /// <summary>
        /// 示例4：显示参数信息
        /// </summary>
        [ContextMenu("显示参数信息")]
        public void ShowParameterInfo()
        {
            if (_parameterWrapper == null) return;
            
            Debug.Log("=== 参数信息 ===");
            
            var actions = _parameterWrapper.GetActions();
            foreach (var action in actions)
            {
                Debug.Log($"动作: {action.Name} (覆盖参数: {action.GetOverrideCount()})");
                
                var tracks = action.GetTracks();
                foreach (var track in tracks)
                {
                    Debug.Log($"  轨道: {track.Name} ({track.TypeName}) (覆盖参数: {track.GetOverrideCount()})");
                    
                    var parameters = track.GetParameters();
                    foreach (var parameter in parameters)
                    {
                        if (parameter.HasOverride())
                        {
                            var info = parameter.GetInfo();
                            Debug.Log($"    参数: {info}");
                        }
                    }
                }
            }
            
            // 显示总体覆盖信息
            var overrideInfo = _parameterWrapper.GetOverrideInfo();
            Debug.Log($"总体覆盖状态: {overrideInfo}");
        }
        
        /// <summary>
        /// 示例5：清除特定动作的覆盖
        /// </summary>
        [ContextMenu("清除特定动作的覆盖")]
        public void ClearSpecificActionOverrides()
        {
            if (_parameterWrapper == null) return;
            
            Debug.Log("=== 清除特定动作的覆盖 ===");
            
            var attackAction = _parameterWrapper.GetAction("Attack1");
            if (attackAction != null)
            {
                int beforeCount = attackAction.GetOverrideCount();
                attackAction.ClearOverrides();
                
                Debug.Log($"动作 {attackAction.Name}: 清除了 {beforeCount} 个参数覆盖");
            }
        }
        
        /// <summary>
        /// 示例6：在同一轨道上修改多个参数
        /// </summary>
        [ContextMenu("在同一轨道上修改多个参数")]
        public void ModifyMultipleParametersOnSameTrack()
        {
            if (_parameterWrapper == null) return;
            
            Debug.Log("=== 在同一轨道上修改多个参数 ===");
            
            var actions = _parameterWrapper.GetActions();
            foreach (var action in actions)
            {
                var hitBoxTracks = action.GetTracks<AbilityEventObj_CreateHitBox>();
                
                foreach (var track in hitBoxTracks)
                {
                    Debug.Log($"修改轨道 {track.Name} 的多个参数:");
                    
                    // 修改碰撞盒大小
                    Vector3 originalSize = track.GetOriginalParameter<Vector3>("hitBoxSize");
                    Vector3 newSize = Vector3.Scale(originalSize, hitBoxSizeMultiplier);
                    track.SetParameter("hitBoxSize", newSize);
                    Debug.Log($"  碰撞盒大小: {originalSize} -> {newSize}");
                    
                    // 修改碰撞盒偏移
                    Vector3 originalOffset = track.GetOriginalParameter<Vector3>("hitBoxOffset");
                    Vector3 newOffset = originalOffset + Vector3.up * 0.1f;
                    track.SetParameter("hitBoxOffset", newOffset);
                    Debug.Log($"  碰撞盒偏移: {originalOffset} -> {newOffset}");
                    
                    // 修改最大命中次数
                    int originalMaxHits = track.GetOriginalParameter<int>("maxHits");
                    int newMaxHits = originalMaxHits + 2;
                    track.SetParameter("maxHits", newMaxHits);
                    Debug.Log($"  最大命中次数: {originalMaxHits} -> {newMaxHits}");
                    
                    // 修改命中后是否销毁
                    bool originalDestroyOnHit = track.GetOriginalParameter<bool>("destroyOnHit");
                    bool newDestroyOnHit = !originalDestroyOnHit;
                    track.SetParameter("destroyOnHit", newDestroyOnHit);
                    Debug.Log($"  命中后销毁: {originalDestroyOnHit} -> {newDestroyOnHit}");
                }
            }
        }
        
        /// <summary>
        /// 示例7：通过轨道名称修改参数
        /// </summary>
        [ContextMenu("通过轨道名称修改参数")]
        public void ModifyParametersByTrackName()
        {
            if (_parameterWrapper == null) return;
            
            Debug.Log("=== 通过轨道名称修改参数 ===");
            
            var actions = _parameterWrapper.GetActions();
            foreach (var action in actions)
            {
                // 假设我们要找名为"Speed"的轨道
                var speedTrack = action.GetTrackByName("Speed");
                if (speedTrack != null)
                {
                    // 检查是否有Speed参数
                    if (speedTrack.GetParameter("Speed") != null)
                    {
                        float originalSpeed = speedTrack.GetOriginalParameter<float>("Speed");
                        float newSpeed = originalSpeed * speedMultiplier;
                        
                        speedTrack.SetParameter("Speed", newSpeed);
                        
                        Debug.Log($"动作 {action.Name} 的轨道 {speedTrack.Name}: 速度从 {originalSpeed} 修改为 {newSpeed}");
                    }
                }
            }
        }
        
        /// <summary>
        /// 示例8：参数类型检查和安全修改
        /// </summary>
        [ContextMenu("参数类型检查和安全修改")]
        public void SafeParameterModification()
        {
            if (_parameterWrapper == null) return;
            
            Debug.Log("=== 参数类型检查和安全修改 ===");
            
            var actions = _parameterWrapper.GetActions();
            foreach (var action in actions)
            {
                var tracks = action.GetTracks();
                foreach (var track in tracks)
                {
                    // 获取参数类型信息
                    var parameterTypes = track.GetParameterTypes();
                    
                    foreach (var kvp in parameterTypes)
                    {
                        string paramName = kvp.Key;
                        System.Type paramType = kvp.Value;
                        
                        // 根据参数类型进行安全修改
                        if (paramType == typeof(float))
                        {
                            float originalValue = track.GetOriginalParameter<float>(paramName);
                            if (originalValue > 0) // 只修改正值
                            {
                                float newValue = originalValue * 1.1f;
                                track.SetParameter(paramName, newValue);
                                Debug.Log($"修改float参数 {paramName}: {originalValue} -> {newValue}");
                            }
                        }
                        else if (paramType == typeof(int))
                        {
                            int originalValue = track.GetOriginalParameter<int>(paramName);
                            if (originalValue > 0) // 只修改正值
                            {
                                int newValue = originalValue + 1;
                                track.SetParameter(paramName, newValue);
                                Debug.Log($"修改int参数 {paramName}: {originalValue} -> {newValue}");
                            }
                        }
                        else if (paramType == typeof(bool))
                        {
                            bool originalValue = track.GetOriginalParameter<bool>(paramName);
                            bool newValue = !originalValue;
                            track.SetParameter(paramName, newValue);
                            Debug.Log($"修改bool参数 {paramName}: {originalValue} -> {newValue}");
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// 清除所有参数覆盖
        /// </summary>
        [ContextMenu("清除所有覆盖")]
        public void ClearAllOverrides()
        {
            if (_parameterWrapper == null) return;
            
            var overrideInfo = _parameterWrapper.GetOverrideInfo();
            int beforeCount = overrideInfo.OverrideCount;
            
            _parameterWrapper.ClearAllOverrides();
            
            Debug.Log($"清除了 {beforeCount} 个参数覆盖");
        }
        
        /// <summary>
        /// 记录所有动作信息
        /// </summary>
        private void LogAllActionsInfo()
        {
            Debug.Log("=== 所有动作信息 ===");
            
            var actions = _parameterWrapper.GetActions();
            foreach (var action in actions)
            {
                Debug.Log($"动作: {action.Name} (组 {action.GroupIndex}, 索引 {action.AbilityIndex})");
                
                var tracks = action.GetTracks();
                foreach (var track in tracks)
                {
                    Debug.Log($"  轨道: {track.Name} ({track.TypeName}) - 参数数量: {track.GetParameters().Count}");
                    
                    // 列出所有参数
                    var parameters = track.GetParameters();
                    foreach (var param in parameters)
                    {
                        Debug.Log($"    参数: {param.Name} ({param.Type.Name})");
                    }
                }
            }
        }
        
        /// <summary>
        /// 实时参数调整演示
        /// </summary>
        [ContextMenu("开始实时参数调整")]
        public void StartRealTimeAdjustment()
        {
            if (_parameterWrapper == null) return;
            
            StartCoroutine(RealTimeAdjustmentCoroutine());
        }
        
        private System.Collections.IEnumerator RealTimeAdjustmentCoroutine()
        {
            Debug.Log("开始实时参数调整");
            
            float elapsedTime = 0f;
            float duration = 10f; // 持续10秒
            
            var actions = _parameterWrapper.GetActions();
            
            while (elapsedTime < duration)
            {
                // 根据时间动态调整速度
                float speedValue = 1f + Mathf.Sin(elapsedTime * 2f) * 0.5f; // 在0.5到1.5之间振荡
                
                // 应用到所有动画速度轨道
                foreach (var action in actions)
                {
                    var speedTracks = action.GetTracks<AbilityEventObj_AnimSpeed>();
                    foreach (var track in speedTracks)
                    {
                        track.SetParameter("Speed", speedValue);
                    }
                }
                
                elapsedTime += Time.deltaTime;
                yield return null;
            }
            
            // 恢复原始值
            ClearAllOverrides();
            Debug.Log("实时参数调整结束");
        }
        
        void OnGUI()
        {
            if (_parameterWrapper == null) return;
            
            // 显示简单的状态信息
            var overrideInfo = _parameterWrapper.GetOverrideInfo();
            
            GUI.Box(new Rect(10, 10, 350, 180), "运行时参数包装器状态");
            
            GUI.Label(new Rect(20, 30, 330, 20), $"动作数量: {_parameterWrapper.GetActions().Count}");
            GUI.Label(new Rect(20, 50, 330, 20), $"总参数数量: {overrideInfo.TotalParameterCount}");
            GUI.Label(new Rect(20, 70, 330, 20), $"覆盖数量: {overrideInfo.OverrideCount}");
            
            GUI.Label(new Rect(20, 90, 330, 20), "按键操作:");
            GUI.Label(new Rect(20, 110, 330, 20), "Q-动作名称修改 W-轨道类型修改 E-条件修改");
            GUI.Label(new Rect(20, 130, 330, 20), "R-显示信息 T-清除特定 Y-多参数 U-轨道名称");
            GUI.Label(new Rect(20, 150, 330, 20), "Esc-清除所有覆盖");
            
            // 添加按钮
            if (GUI.Button(new Rect(20, 200, 100, 30), "速度修改"))
            {
                ModifySpeedByActionName();
            }
            
            if (GUI.Button(new Rect(130, 200, 100, 30), "碰撞盒修改"))
            {
                ModifyHitBoxByTrackType();
            }
            
            if (GUI.Button(new Rect(240, 200, 100, 30), "显示信息"))
            {
                ShowParameterInfo();
            }
            
            if (GUI.Button(new Rect(350, 200, 100, 30), "清除覆盖"))
            {
                ClearAllOverrides();
            }
        }
    }
} 
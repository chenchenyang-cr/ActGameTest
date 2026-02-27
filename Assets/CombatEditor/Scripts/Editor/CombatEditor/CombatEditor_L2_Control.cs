using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

namespace CombatEditor
{
    public partial class CombatEditor
    {
        /// <summary>
        /// HandlePosition to SwapPosition;
        /// </summary>
        /// 
        public void HandleDraggingEvents()
        {
            Event e = Event.current;

            // 处理拖拽
            if (e.type == EventType.MouseDrag && e.button == 0)
            {
                var index = Mathf.RoundToInt(((e.mousePosition.y - L3TrackAvailableRect.y) - LineHeight) / LineHeight);
                index = Mathf.Clamp(index, 0, SelectedAbilityObj.events.Count);

                L2DragEndIndex = index;

                var rect = new Rect(L2Rect.x, L3TrackAvailableRect.y + (index + 1) * LineHeight - DragIndicatorHeight / 2, L2Rect.width, DragIndicatorHeight);
                L2DragRect = rect;
                DragHandleRequired = true;
                Repaint();
            }

            // 🎯 完全重构的右键检测系统
            if (e.type == EventType.MouseDown && e.button == 1)
            {
                Vector2 mousePos = e.mousePosition;
                Debug.Log($"🖱️ 原始右键点击位置: {mousePos}");
                
                bool foundTrackHit = HandleRightClickWithPreciseDetection(mousePos, e);
                
                if (foundTrackHit)
                {
                    e.Use(); // 只有在成功处理时才消费事件
                }
            }
        }

        /// <summary>
        /// 完全重构的精确右键检测系统
        /// 解决滚动偏移和坐标系统问题
        /// </summary>
        private bool HandleRightClickWithPreciseDetection(Vector2 mousePosition, Event e)
        {
            if (SelectedAbilityObj?.events == null || SelectedAbilityObj.events.Count == 0)
            {
                Debug.Log("❌ 没有可用的轨道事件");
                return false;
            }

            List<AbilityEvent> eves = SelectedAbilityObj.events;
            
            // 🎯 关键修复：考虑滚动偏移量
            // TrackRect 是滚动视图，需要计算实际的滚动偏移
            Vector2 scrollOffset = Scroll_Fields;
            Vector2 adjustedMousePos = new Vector2(mousePosition.x, mousePosition.y + scrollOffset.y);
            
            Debug.Log($"📏 滚动偏移: {scrollOffset}, 调整后鼠标位置: {adjustedMousePos}");
            Debug.Log($"📐 TrackRect区域: {TrackRect}");
            Debug.Log($"📐 L3TrackAvailableRect区域: {L3TrackAvailableRect}");
            
            // 方法1：基于相对Y坐标计算轨道索引
            int detectedTrackIndex = CalculateTrackIndexFromMousePosition(adjustedMousePos);
            
            if (detectedTrackIndex >= 0 && detectedTrackIndex < eves.Count)
            {
                AbilityEvent targetEvent = eves[detectedTrackIndex];
                if (targetEvent?.Obj != null)
                {
                    Debug.Log($"🎯 方法1检测成功 - 轨道 #{detectedTrackIndex + 1}: {targetEvent.Obj.name}");
                    ShowRightClickMenuForEvent(targetEvent, detectedTrackIndex);
                    return true;
                }
            }
            
            // 方法2：精确几何检测（后备方案）
            int geometricDetectedIndex = CalculateTrackIndexUsingGeometry(mousePosition);
            
            if (geometricDetectedIndex >= 0 && geometricDetectedIndex < eves.Count)
            {
                AbilityEvent targetEvent = eves[geometricDetectedIndex];
                if (targetEvent?.Obj != null)
                {
                    Debug.Log($"🎯 方法2检测成功 - 轨道 #{geometricDetectedIndex + 1}: {targetEvent.Obj.name}");
                    ShowRightClickMenuForEvent(targetEvent, geometricDetectedIndex);
                    return true;
                }
            }
            
            // 方法3：暴力枚举检测（最后的后备方案）
            int bruteForceIndex = CalculateTrackIndexBruteForce(mousePosition);
            
            if (bruteForceIndex >= 0 && bruteForceIndex < eves.Count)
            {
                AbilityEvent targetEvent = eves[bruteForceIndex];
                if (targetEvent?.Obj != null)
                {
                    Debug.Log($"🎯 方法3检测成功 - 轨道 #{bruteForceIndex + 1}: {targetEvent.Obj.name}");
                    ShowRightClickMenuForEvent(targetEvent, bruteForceIndex);
                    return true;
                }
            }
            
            // 如果所有方法都失败，显示详细调试信息
            ShowDetailedDebugInfo(mousePosition, adjustedMousePos, scrollOffset);
            return false;
        }
        
        /// <summary>
        /// 方法1：基于相对Y坐标计算轨道索引
        /// </summary>
        private int CalculateTrackIndexFromMousePosition(Vector2 adjustedMousePos)
        {
            // 计算相对于轨道区域起始位置的Y偏移
            float relativeY = adjustedMousePos.y - L3TrackAvailableRect.y;
            
            // 计算轨道索引（注意：第一个轨道从索引1开始，所以需要减1）
            int trackIndex = Mathf.FloorToInt(relativeY / LineHeight) - 1;
            
            Debug.Log($"🔢 方法1计算: relativeY={relativeY:F1}, LineHeight={LineHeight}, 计算得轨道索引={trackIndex}");
            
            // 确保索引在有效范围内
            if (trackIndex >= 0 && trackIndex < SelectedAbilityObj.events.Count)
            {
                return trackIndex;
            }
            
            return -1;
        }
        
        /// <summary>
        /// 方法2：精确几何检测
        /// </summary>
        private int CalculateTrackIndexUsingGeometry(Vector2 mousePosition)
        {
            List<AbilityEvent> eves = SelectedAbilityObj.events;
            
            for (int i = 0; i < eves.Count; i++)
            {
                // 计算每个轨道的精确几何区域，不考虑滚动
                float trackY = L3TrackAvailableRect.y + (i + 1) * LineHeight;
                Rect trackGeometry = new Rect(0, trackY, position.width, LineHeight);
                
                // 使用GUI坐标系检测
                if (mousePosition.y >= trackY && mousePosition.y <= trackY + LineHeight)
                {
                    Debug.Log($"🔢 方法2检测: 鼠标Y={mousePosition.y:F1}, 轨道Y范围=[{trackY:F1}, {trackY + LineHeight:F1}] → 轨道#{i + 1}");
                    return i;
                }
            }
            
            return -1;
        }
        
        /// <summary>
        /// 方法3：暴力枚举检测
        /// </summary>
        private int CalculateTrackIndexBruteForce(Vector2 mousePosition)
        {
            List<AbilityEvent> eves = SelectedAbilityObj.events;
            
            // 计算最可能的轨道索引，基于简单的数学计算
            float startY = L3TrackAvailableRect.y + LineHeight; // 第一个轨道的Y位置
            float relativeY = mousePosition.y - startY;
            int possibleIndex = Mathf.RoundToInt(relativeY / LineHeight);
            
            Debug.Log($"🔢 方法3计算: startY={startY:F1}, relativeY={relativeY:F1}, possibleIndex={possibleIndex}");
            
            // 检查可能的索引及其邻近索引
            for (int offset = 0; offset <= 2; offset++)
            {
                int checkIndex = possibleIndex - offset;
                if (checkIndex >= 0 && checkIndex < eves.Count)
                {
                    Debug.Log($"🔍 方法3检查索引: {checkIndex}");
                    return checkIndex;
                }
                
                if (offset > 0)
                {
                    checkIndex = possibleIndex + offset;
                    if (checkIndex >= 0 && checkIndex < eves.Count)
                    {
                        Debug.Log($"🔍 方法3检查索引: {checkIndex}");
                        return checkIndex;
                    }
                }
            }
            
            return -1;
        }
        
        /// <summary>
        /// 显示右键菜单
        /// </summary>
        private void ShowRightClickMenuForEvent(AbilityEvent targetEvent, int trackIndex)
        {
            string eventName = targetEvent.Obj?.name ?? "未命名";
            Debug.Log($"✅ 成功检测到轨道 #{trackIndex + 1}: {eventName}");
            
            // 立即选中这个轨道
            SelectedTrackIndex = trackIndex + 1;
            FocusOnEvent(targetEvent);
            
            // 创建右键菜单
            GenericMenu menu = new GenericMenu();
            
            // 使用事件对象引用
            AbilityEvent capturedEvent = targetEvent;
            
            menu.AddItem(new GUIContent("删除事件"), false, () => {
                DeleteAbilityEventByReference(capturedEvent);
            });
            
            menu.AddItem(new GUIContent("编辑条件"), false, () => {
                Debug.Log($"🔧 准备编辑事件: {capturedEvent.Obj?.name ?? "未命名"} 的条件");
                ShowEventConditionDialogByReference(capturedEvent);
            });
            
            // 显示菜单
            menu.ShowAsContext();
            
            // 强制重绘
            Repaint();
        }
        
        /// <summary>
        /// 显示详细调试信息
        /// </summary>
        private void ShowDetailedDebugInfo(Vector2 mousePosition, Vector2 adjustedMousePos, Vector2 scrollOffset)
        {
            Debug.Log($"❌ 所有检测方法都失败了");
            Debug.Log($"🐛 调试信息:");
            Debug.Log($"   - 原始鼠标位置: {mousePosition}");
            Debug.Log($"   - 调整后位置: {adjustedMousePos}"); 
            Debug.Log($"   - 滚动偏移: {scrollOffset}");
            Debug.Log($"   - L3TrackAvailableRect: {L3TrackAvailableRect}");
            Debug.Log($"   - LineHeight: {LineHeight}");
            Debug.Log($"   - 轨道数量: {SelectedAbilityObj.events.Count}");
            
            // 显示所有轨道的理论位置
            for (int i = 0; i < SelectedAbilityObj.events.Count; i++)
            {
                float trackY = L3TrackAvailableRect.y + (i + 1) * LineHeight;
                string eventName = SelectedAbilityObj.events[i].Obj?.name ?? "未命名";
                Debug.Log($"   - 轨道 #{i + 1} ({eventName}): Y={trackY:F1} 范围=[{trackY:F1}, {trackY + LineHeight:F1}]");
            }
        }

        // 基于对象引用的删除事件方法
        private void DeleteAbilityEventByReference(AbilityEvent targetEvent)
        {
            if (targetEvent == null || targetEvent.Obj == null || SelectedAbilityObj?.events == null)
            {
                Debug.LogError("无法删除事件：事件对象为空或无效");
                return;
            }
            
            List<AbilityEvent> eves = SelectedAbilityObj.events;
            int eventIndex = eves.IndexOf(targetEvent);
            
            if (eventIndex >= 0)
            {
                Debug.Log($"🗑️ 删除事件: {targetEvent.Obj.name} (轨道 #{eventIndex + 1})");
                
                Undo.RecordObject(SelectedAbilityObj, "Delete Ability Event");
                
                // 删除资源
                Object[] assets = AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(SelectedAbilityObj));
                if (assets.Contains(targetEvent.Obj))
                {
                    Undo.DestroyObjectImmediate(targetEvent.Obj);
                    AssetDatabase.SaveAssets();
                }
                
                // 从列表中移除
                eves.RemoveAt(eventIndex);
                
                EditorUtility.SetDirty(SelectedAbilityObj);
                OnAnimEventChanges();
                AssetDatabase.SaveAssets();
            }
            else
            {
                Debug.LogError($"无法删除事件：在事件列表中找不到事件 {targetEvent.Obj.name}");
            }
        }

        // 轨道选择保护相关的静态变量
        private static int _protectedTrackIndex = -1;
        private static string _protectedTrackName = "";
        private static bool _isTrackProtected = false;
        private static double _protectionStartTime = 0;
        private static int _protectedAbilityHashCode = -1;

        /// <summary>
        /// 启用轨道选择保护机制
        /// </summary>
        private void EnableTrackProtection(int trackIndex, string trackName)
        {
            _protectedTrackIndex = trackIndex;
            _protectedTrackName = trackName;
            _isTrackProtected = true;
            _protectionStartTime = EditorApplication.timeSinceStartup;
            _protectedAbilityHashCode = SelectedAbilityObj?.GetHashCode() ?? -1;
            
            Debug.Log($"🛡️ 启用轨道保护：轨道 #{trackIndex} ({trackName})");
        }

        /// <summary>
        /// 禁用轨道选择保护机制
        /// </summary>
        public void DisableTrackProtection()
        {
            if (_isTrackProtected)
            {
                Debug.Log($"🛡️ 禁用轨道保护：轨道 #{_protectedTrackIndex} ({_protectedTrackName})");
                _protectedTrackIndex = -1;
                _protectedTrackName = "";
                _isTrackProtected = false;
                _protectionStartTime = 0;
                _protectedAbilityHashCode = -1;
                
                // 清除EditorPrefs
                if (EditorPrefs.HasKey("ConditionEditingTrackIndex"))
                    EditorPrefs.DeleteKey("ConditionEditingTrackIndex");
                if (EditorPrefs.HasKey("ConditionEditingTrackName"))
                    EditorPrefs.DeleteKey("ConditionEditingTrackName");
            }
        }

        /// <summary>
        /// 检查是否应该恢复被保护的轨道选择
        /// </summary>
        private bool ShouldRestoreProtectedTrack()
        {
            if (!_isTrackProtected || _protectedTrackIndex <= 0) return false;
            
            // 检查是否是同一个AbilityObj
            if (SelectedAbilityObj?.GetHashCode() != _protectedAbilityHashCode) return false;
            
            // 检查当前轨道选择是否与被保护的不同
            if (SelectedTrackIndex == _protectedTrackIndex) return false;
            
            // 检查被保护的轨道是否仍然有效
            if (SelectedAbilityObj?.events == null || _protectedTrackIndex > SelectedAbilityObj.events.Count) return false;
            
            var protectedEvent = SelectedAbilityObj.events[_protectedTrackIndex - 1];
            if (protectedEvent?.Obj == null || protectedEvent.Obj.name != _protectedTrackName) return false;
            
            return true;
        }

        /// <summary>
        /// 强制恢复被保护的轨道选择
        /// </summary>
        private void RestoreProtectedTrack()
        {
            if (ShouldRestoreProtectedTrack())
            {
                Debug.Log($"🔄 恢复被保护的轨道：从 #{SelectedTrackIndex} 恢复到 #{_protectedTrackIndex} ({_protectedTrackName})");
                
                var oldIndex = SelectedTrackIndex;
                SelectedTrackIndex = _protectedTrackIndex;
                
                // 确保UI状态同步
                var protectedEvent = SelectedAbilityObj.events[_protectedTrackIndex - 1];
                FocusOnEvent(protectedEvent);
                EnsureCreateObjWithHandleVisible();
            }
        }

        /// <summary>
        /// 增强版轨道选择验证，更强的保护机制
        /// </summary>
        public void ValidateTrackSelection()
        {
            // 首先检查新的事件保护机制
            if (_isEventProtected)
            {
                // 检查保护是否超时（10秒）
                if (EditorApplication.timeSinceStartup - _eventProtectionStartTime > 10.0)
                {
                    Debug.Log("⏰ 事件保护超时，自动禁用保护");
                    DisableEventProtection();
                    return;
                }
                
                // 检查条件编辑窗口是否还存在
                bool conditionWindowExists = false;
                var allWindows = Resources.FindObjectsOfTypeAll<EditorWindow>();
                foreach (var window in allWindows)
                {
                    if (window.GetType().Name == "EventConditionEditorWindow")
                    {
                        conditionWindowExists = true;
                        break;
                    }
                }
                
                if (!conditionWindowExists)
                {
                    Debug.Log("🪟 条件编辑窗口已关闭，禁用事件保护");
                    DisableEventProtection();
                    return;
                }
                
                // 如果需要，恢复被保护的事件
                RestoreProtectedEvent();
                return; // 使用新机制时，不再检查旧机制
            }
            
            // 兼容旧的轨道保护机制（逐步淘汰）
            if (_isTrackProtected)
            {
                // 检查保护是否超时（10秒）
                if (EditorApplication.timeSinceStartup - _protectionStartTime > 10.0)
                {
                    Debug.Log("⏰ 轨道保护超时，自动禁用保护");
                    DisableTrackProtection();
                    return;
                }
                
                // 检查条件编辑窗口是否还存在
                bool conditionWindowExists = false;
                var allWindows = Resources.FindObjectsOfTypeAll<EditorWindow>();
                foreach (var window in allWindows)
                {
                    if (window.GetType().Name == "EventConditionEditorWindow")
                    {
                        conditionWindowExists = true;
                        break;
                    }
                }
                
                if (!conditionWindowExists)
                {
                    Debug.Log("🪟 条件编辑窗口已关闭，禁用轨道保护");
                    DisableTrackProtection();
                    return;
                }
                
                // 如果需要，恢复被保护的轨道
                RestoreProtectedTrack();
            }
        }

        // 事件保护相关的静态变量（基于对象引用）
        private static AbilityEvent _protectedEvent = null;
        private static bool _isEventProtected = false;
        private static double _eventProtectionStartTime = 0;
        private static int _protectedEventAbilityHashCode = -1;

        /// <summary>
        /// 启用事件保护机制（基于对象引用）
        /// </summary>
        private void EnableEventProtection(AbilityEvent targetEvent)
        {
            _protectedEvent = targetEvent;
            _isEventProtected = true;
            _eventProtectionStartTime = EditorApplication.timeSinceStartup;
            _protectedEventAbilityHashCode = SelectedAbilityObj?.GetHashCode() ?? -1;
            
            string eventName = targetEvent?.Obj?.name ?? "未命名";
            Debug.Log($"🛡️ 启用事件保护: {eventName}");
        }

        /// <summary>
        /// 禁用事件保护机制
        /// </summary>
        public void DisableEventProtection()
        {
            if (_isEventProtected && _protectedEvent != null)
            {
                string eventName = _protectedEvent?.Obj?.name ?? "未命名";
                Debug.Log($"🛡️ 禁用事件保护: {eventName}");
            }
            
            _protectedEvent = null;
            _isEventProtected = false;
            _eventProtectionStartTime = 0;
            _protectedEventAbilityHashCode = -1;
        }

        /// <summary>
        /// 检查是否应该恢复被保护的事件选择
        /// </summary>
        private bool ShouldRestoreProtectedEvent()
        {
            if (!_isEventProtected || _protectedEvent == null || _protectedEvent.Obj == null) return false;
            
            // 检查是否是同一个AbilityObj
            if (SelectedAbilityObj?.GetHashCode() != _protectedEventAbilityHashCode) return false;
            
            // 检查被保护的事件是否仍在事件列表中
            if (SelectedAbilityObj?.events == null || !SelectedAbilityObj.events.Contains(_protectedEvent)) return false;
            
            // 检查当前选中的轨道是否与被保护的事件对应的轨道不同
            int protectedEventIndex = SelectedAbilityObj.events.IndexOf(_protectedEvent);
            if (protectedEventIndex < 0) return false;
            
            int protectedTrackIndex = protectedEventIndex + 1;
            return SelectedTrackIndex != protectedTrackIndex;
        }

        /// <summary>
        /// 强制恢复被保护的事件选择
        /// </summary>
        private void RestoreProtectedEvent()
        {
            if (ShouldRestoreProtectedEvent())
            {
                int protectedEventIndex = SelectedAbilityObj.events.IndexOf(_protectedEvent);
                int protectedTrackIndex = protectedEventIndex + 1;
                
                Debug.Log($"🔄 恢复被保护的事件：从轨道 #{SelectedTrackIndex} 恢复到轨道 #{protectedTrackIndex} ({_protectedEvent.Obj.name})");
                
                SelectedTrackIndex = protectedTrackIndex;
                FocusOnEvent(_protectedEvent);
                EnsureCreateObjWithHandleVisible();
            }
        }

        // 基于对象引用的条件编辑方法
        private void ShowEventConditionDialogByReference(AbilityEvent targetEvent)
        {
            if (targetEvent == null || targetEvent.Obj == null)
            {
                Debug.LogError("无法编辑条件：事件对象为空或无效");
                EditorUtility.DisplayDialog("错误", "无法编辑条件，事件对象无效。", "确定");
                return;
            }
            
            List<AbilityEvent> eves = SelectedAbilityObj.events;
            int eventIndex = eves.IndexOf(targetEvent);
            
            if (eventIndex < 0)
            {
                Debug.LogError($"无法编辑条件：在事件列表中找不到事件 {targetEvent.Obj.name}");
                EditorUtility.DisplayDialog("错误", "无法编辑条件，事件在列表中不存在。", "确定");
                return;
            }
            
            // 确保轨道选择状态正确设置
            int targetTrackIndex = eventIndex + 1;
            int oldSelectedTrackIndex = SelectedTrackIndex;
            SelectedTrackIndex = targetTrackIndex;
            
            // 如果轨道选择发生了变化，记录并更新UI状态
            if (oldSelectedTrackIndex != SelectedTrackIndex)
            {
                Debug.Log($"条件编辑：轨道选择从 #{oldSelectedTrackIndex} 更新为 #{SelectedTrackIndex}");
                FocusOnEvent(targetEvent);
                EnsureCreateObjWithHandleVisible();
            }
            
            // 启用事件保护机制
            EnableEventProtection(targetEvent);
            
            // 记录当前被编辑的事件信息，用于调试
            string eventName = targetEvent.Obj?.name ?? "未命名";
            string eventType = targetEvent.Obj?.GetType().Name ?? "未知类型";
            
            // 记录操作，以便撤销
            RegisterUndo(SelectedAbilityObj, "Edit Event Condition");
            
            // 显示详细日志，帮助调试
            Debug.Log($"✓ 打开事件条件编辑窗口: [{eventType}] {eventName} (轨道 #{targetTrackIndex})");
            Debug.Log($"  - 已启用事件保护机制");
            
            // 显示条件编辑窗口
            EventConditionEditorWindow.ShowWindow(targetEvent, SelectedAbilityObj, eventIndex);
            
            // 强制更新UI
            Repaint();
        }

        public void OnSwapAnimEvents(int indexBefore, int indexAfter)
        {
            // Register undo before modifying the events list
            RegisterUndo(SelectedAbilityObj, "Swap Animation Events");
            
            SelectedAbilityObj.events = SwapList<AbilityEvent>(SelectedAbilityObj.events, indexBefore, indexAfter);

            if (indexBefore < indexAfter)
            {
                SelectedTrackIndex = indexAfter;
            }
            else
            {
                SelectedTrackIndex = indexAfter + 1;
            }
            LoadL3();
            Repaint();
        }

        public List<T> SwapList<T>(List<T> list, int indexBefore, int indexAfter)
        {
            var obj = list[indexBefore];
            list.Insert(indexAfter, obj);
            if (indexBefore < indexAfter)
            {
                list.RemoveAt(indexBefore);
            }
            else
            {
                list.RemoveAt(indexBefore + 1);
            }
            return list;
        }

        /// <summary>
        /// 确保只有选中轨道的CreateObjWithHandle事件的Handle可见，隐藏其他的
        /// </summary>
        private void EnsureCreateObjWithHandleVisible()
        {
            if (SelectedAbilityObj != null && SelectedAbilityObj.events != null)
            {
                for (int i = 0; i < SelectedAbilityObj.events.Count; i++)
                {
                    var evt = SelectedAbilityObj.events[i];
                    if (evt.Obj is AbilityEventObj_CreateObjWithHandle)
                    {
                        // 只有选中的轨道才显示Handle，其他的隐藏
                        evt.Previewable = (i + 1 == SelectedTrackIndex);
                    }
                }
            }
        }

        /// <summary>
        /// Click Fields triggers Select, not Toggle
        /// </summary>
        /// <param name="i"></param>
        /// <param name="abilityEvent"></param>
        /// 
        public void OnClickFields(int i, AbilityEvent abilityEvent)
        {
            // 检查是否有事件被保护，如果有且不是点击的同一事件，则给出警告
            if (_isEventProtected && _protectedEvent != null && _protectedEvent != abilityEvent)
            {
                string protectedEventName = _protectedEvent?.Obj?.name ?? "未命名";
                string clickedEventName = abilityEvent?.Obj?.name ?? "未命名";
                Debug.LogWarning($"⚠️ 事件被保护中，忽略对事件 {clickedEventName} 的点击操作，当前保护事件: {protectedEventName}");
                return;
            }
            
            int oldSelectedTrackIndex = SelectedTrackIndex;
            SelectedTrackIndex = i + 1;
            
            Debug.Log($"OnClickFields: 轨道选择从 #{oldSelectedTrackIndex} 变更为 #{SelectedTrackIndex} ({abilityEvent.Obj?.name ?? "未命名"})");
            
            FocusOnEvent(abilityEvent);
            
            // 检查是否是CreateHitBox类型，如果是则强制开启预览
            if (abilityEvent.Obj is AbilityEventObj_CreateHitBox)
            {
                // 确保hitbox预览始终可见
                abilityEvent.Previewable = true;
                // 刷新场景视图
                SceneView.RepaintAll();
                // 强制更新预览
                HardResetPreviewToCurrentFrame();
            }
            
            // 确保只有选中轨道的CreateObjWithHandle事件的Handle可见
            EnsureCreateObjWithHandleVisible();
            
            // 强制刷新预览
            abilityEvent.Previewable = true;
            SceneView.RepaintAll();
            HardResetPreviewToCurrentFrame();
        }

        public void FocusOnEvent(AbilityEvent abilityEvent)
        {
            CombatInspector.GetInspector().CreateInspectedObj(abilityEvent.Obj);
            CurrentInspectedType = InspectedType.Track;
        }

        public void OnClickToggleActive(AbilityEventObj obj)
        {
            bool wasActive = obj.IsActive;
            obj.IsActive = !obj.IsActive;
            
            // 确保无论什么状态变化都刷新预览
            if (IsPlaying || IsLooping)
            {
                OnStopPlayAnimation();
                FlushAndInsPreviewToFrame0();
            }
            else
            {
                OnStopPlayAnimation();
                HardResetPreviewToCurrentFrame();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="eve"></param>
        public void OnTogglePreview(AbilityEvent eve)
        {
            eve.Previewable = !eve.Previewable;
            SceneView.RepaintAll();
            if (IsPlaying || IsLooping)
            {
                OnStopPlayAnimation();
                FlushAndInsPreviewToFrame0();
            }
            else
            {
                //OnStopPlayAnimation();

                HardResetPreviewToCurrentFrame();
            }
        }

        public void CreatAddTrackMenu()
        {
            System.Type[] typesToDisplay = TypeCache.GetTypesWithAttribute<AbilityEventAttribute>().OrderBy(m => m.Name).ToArray();
            AnimEventSearchProvider provider = ScriptableObject.CreateInstance("AnimEventSearchProvider") as AnimEventSearchProvider;
            provider.types = typesToDisplay;
            provider.OnSetIndexCallBack = (type) =>
            {
                AbilityEventObj obj = ScriptableObject.CreateInstance(type) as AbilityEventObj;
                obj.name = type.Name.Replace("AbilityEventObj_", "");
                AbilityEvent e = new AbilityEvent();
                e.Obj = obj;
                
                // Register for undo when creating a new event
                RegisterUndoForCreatedEvent(SelectedAbilityObj, obj);
                
                AssetDatabase.AddObjectToAsset(obj, SelectedAbilityObj);
                AssetDatabase.SaveAssets();
                AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(SelectedAbilityObj));

                SelectedAbilityObj.events.Add(e);

                EditorUtility.SetDirty(SelectedAbilityObj);
                OnAnimEventChanges();

            DestroyImmediate(provider);
            };
            SearchWindow.Open(new SearchWindowContext(GUIUtility.GUIToScreenPoint(Event.current.mousePosition + new Vector2(200, 0))), provider);
        }




        public bool IsDragging = false;
        public void StartDragElement(Vector2 StartPos)
        {
            DragStartPosition = StartPos;
            //StartDraggingInRect = true;
            //IsDragging = false;
        }
        public void DraggingElements(Vector2 CurrentPos, Rect EndIndicator)
        {
            DragEndRect = EndIndicator;
        }
        public void EndDrag()
        {
            IsDragging = false;
        }



        public void Tick()
        {
            LastTickTime = Time.realtimeSinceStartup;
            if (IsPlaying || IsLooping)
            {
                // Check if SelectedAbilityObj is null to prevent NullReferenceException
                if (SelectedAbilityObj == null)
                {
                    ResetPlayStates();
                    return;
                }

                float CurrentSpeedModifier = 1;
                // Add null check for SpeedModifiers
                if (SpeedModifiers != null)
                {
                    for (int i = 0; i < SpeedModifiers.Length; i++)
                    {
                        // Add null check for each element in SpeedModifiers
                        if (SpeedModifiers[i] != null)
                        {
                            CurrentSpeedModifier *= SpeedModifiers[i].CurrentAnimSpeedModifier;
                        }
                    }
                }
                CurrentSpeedModifier *= PlayTimeMultiplier;

                //Need the information on frame 0
                if (CurrentPlayTime < 0)
                {
                    CurrentPlayTime = 0;
                }
                else
                {
                    CurrentPlayTime += (1 / 60f) * CurrentSpeedModifier;
                }

                IterateFrame();
            }
        }


        public bool InPrefabMode()
        {
            if (UnityEditor.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage() != null)
            {
                return true;
            }
            return false;
        }

        public void OnStartPlay()
        {
            OnPreparePlay();
            IsPlaying = true;
            CombatGlobalEditorValue.IsPlaying = true;
        }
        public void OnStartLoop()
        {
            OnPreparePlay();
            IsLooping = true;
            CombatGlobalEditorValue.IsLooping = true;
        }
        public void OnPreparePlay()
        {
            if (InPrefabMode())
            {
                return;
            }
            OnStopPlayAnimation();
            LastTickTime = -1;
            CurrentPlayTime = -1;
        }

        public void OnPausePlay()
        {
            ResetPlayStates();
            
            if (_previewer != null && _previewer._combatController != null)
            {
                // 检查是否在编辑器拖动模式
                bool isEditorDragMode = !(IsPlaying || IsLooping);
                
                // 只有在播放模式下才恢复角色的原始位置和旋转
                if (!isEditorDragMode)
                {
                    _previewer._combatController.transform.position = CombatGlobalEditorValue.CharacterTransPosBeforePreview;
                    _previewer._combatController.transform.rotation = CombatGlobalEditorValue.CharacterRotBeforePreview;
                }
            }
        }
        public void OnStopPlayAnimation()
        {
            ResetPlayStates();
            AnimationBackToStart();
            PreviewBackToStart();
            
            if (_previewer != null && _previewer._combatController != null)
            {
                // 检查是否在编辑器拖动模式
                bool isEditorDragMode = !(IsPlaying || IsLooping);
                
                // 只有在播放模式下才恢复角色的原始位置和旋转
                if (!isEditorDragMode)
                {
                    _previewer._combatController.transform.position = CombatGlobalEditorValue.CharacterTransPosBeforePreview;
                    _previewer._combatController.transform.rotation = CombatGlobalEditorValue.CharacterRotBeforePreview;
                }
            }
        }



        public void ResetPlayStates()
        {
            IsPlaying = false;
            IsLooping = false;
            CombatGlobalEditorValue.IsLooping = false;
            CombatGlobalEditorValue.IsPlaying = false;
        }


        public void AnimationBackToStart()
        {
            //When Assembly Reload, this func calls, but when playmode is about to start, this func also calls.
            //So need to return if this is going to playmode.
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            StartTime = Time.realtimeSinceStartup;

            OnSetPointerOnTrack(0);
            OnPreviewAnimationAtPercentage(0);

            try
            {
                // Initialize SpeedModifiers to an empty array first to avoid null reference
                SpeedModifiers = new PreviewObject_AnimSpeed[0];
                // Only find objects if we're not in prefab mode and the editor is ready
                if (!InPrefabMode() && !EditorApplication.isCompiling && !EditorApplication.isUpdating)
                {
                    SpeedModifiers = FindObjectsOfType<PreviewObject_AnimSpeed>();
                }
            }
            catch (System.Exception ex)
            {
                // If any error occurs during FindObjectsOfType, ensure SpeedModifiers is still initialized
                Debug.LogWarning("Error finding PreviewObject_AnimSpeed: " + ex.Message);
                SpeedModifiers = new PreviewObject_AnimSpeed[0];
            }
        }

        public void IterateFrame()
        {
            // Add null check before accessing SelectedAbilityObj properties
            if (SelectedAbilityObj == null || SelectedAbilityObj.Clip == null)
            {
                ResetPlayStates();
                return;
            }
            
            //In Time Range, Preview.
            if (CurrentPlayTime <= (SelectedAbilityObj.PreviewPercentageRange.y - SelectedAbilityObj.PreviewPercentageRange.x) * SelectedAbilityObj.Clip.length + LoopWaitTime)
            {
                var CurrentPercentage = CurrentPlayTime / SelectedAbilityObj.Clip.length + SelectedAbilityObj.PreviewPercentageRange.x;
                var CurrentRealFrame = Mathf.RoundToInt(CurrentPercentage * SelectedAbilityObj.Clip.length * 60);
                var CurrentMaxFrame = Mathf.RoundToInt(SelectedAbilityObj.PreviewPercentageRange.y * SelectedAbilityObj.Clip.length * 60);
                int CurrentFrame = CurrentRealFrame < CurrentMaxFrame ? CurrentRealFrame : CurrentMaxFrame;

                //Debug.Log(CurrentMaxFrame);
                //Debug.Log(CurrentFrame);
                OnSetPointerOnTrack(CurrentFrame);

                OnPreviewAnimationAtPercentage(CurrentPercentage);
            }
            else if (IsPlaying)
            {
                OnSetPointerOnTrack(Mathf.RoundToInt(SelectedAbilityObj.PreviewPercentageRange.y * SelectedAbilityObj.Clip.length * 60));
                IsPlaying = false;
            }
            else if (IsLooping)
            {
                CurrentPlayTime = 0;
            }

            this.Repaint();
            //EditorWindow view = EditorWindow.GetWindow<SceneView>();
            //view.Repaint();
        }

        public void OnAnimEventChanges()
        {
            // 确保只有选中轨道的CreateObjWithHandle事件的Handle可见
            EnsureCreateObjWithHandleVisible();
            
            HardResetPreviewToCurrentFrame();
            //PreviewAnimationAtPercentage(CurrentPlayTime, true);
            LoadL3();
        }
        public void HardResetPreviewToCurrentFrame()
        {
            // 确保只有选中轨道的CreateObjWithHandle事件的Handle可见
            EnsureCreateObjWithHandleVisible();
            
            OnHardResetPreviewObj();
            OnPreviewAnimationAtFrame(CurrentFrame);
            SceneView.RepaintAll();
        }

        public void PreviewBackToStart()
        {
            if (_previewer != null)
            {
                _previewer.OnPreviewBackToStart();
            }
        }
        public void FlushAndInsPreviewToFrame0()
        {
            // 确保只有选中轨道的CreateObjWithHandle事件的Handle可见
            EnsureCreateObjWithHandleVisible();
            
            OnHardResetPreviewObj();
            OnPreviewAnimationAtFrame(0);
            SceneView.RepaintAll();
        }


        public void SetL2L3Target(AbilityScriptableObject obj)
        {
            SelectedAbilityObj = obj;
            LoadL3();
        }

        public void OnClickEventTrack(int i, AbilityEvent abilityEvent)
        {
            // 检查是否有事件被保护，如果有且不是点击的同一事件，则给出警告
            if (_isEventProtected && _protectedEvent != null && _protectedEvent != abilityEvent)
            {
                string protectedEventName = _protectedEvent?.Obj?.name ?? "未命名";
                string clickedEventName = abilityEvent?.Obj?.name ?? "未命名";
                Debug.LogWarning($"⚠️ 事件被保护中，忽略对事件 {clickedEventName} 的点击操作，当前保护事件: {protectedEventName}");
                return;
            }
            
            int oldSelectedTrackIndex = SelectedTrackIndex;
            SelectedTrackIndex = i + 1;
            
            Debug.Log($"OnClickEventTrack: 轨道选择从 #{oldSelectedTrackIndex} 变更为 #{SelectedTrackIndex} ({abilityEvent.Obj?.name ?? "未命名"})");
            
            FocusOnEvent(abilityEvent);
            
            // 无论什么类型的事件都强制刷新预览
            // 这样可以确保只渲染当前选中的HitBox
            abilityEvent.Previewable = true;
            
            // 确保只有选中轨道的CreateObjWithHandle事件的Handle可见
            EnsureCreateObjWithHandleVisible();
            
            SceneView.RepaintAll();
            HardResetPreviewToCurrentFrame();
        }

        /// <summary>
        /// 按时间顺序整理所有事件轨道
        /// </summary>
        public void SortEventsByTimeOrder()
        {
            if (SelectedAbilityObj == null || SelectedAbilityObj.events == null || SelectedAbilityObj.events.Count <= 1)
                return;

            // 记录撤销
            RegisterUndo(SelectedAbilityObj, "Sort Events By Time Order");

            // 创建事件时间和索引的键值对列表，用于排序
            List<KeyValuePair<float, int>> eventTimeList = new List<KeyValuePair<float, int>>();
            
            // 收集所有事件的时间点或时间范围的起始点
            for (int i = 0; i < SelectedAbilityObj.events.Count; i++)
            {
                AbilityEvent evt = SelectedAbilityObj.events[i];
                float timePoint = 0;
                
                // 根据事件类型获取时间点
                switch (evt.Obj.GetEventTimeType())
                {
                    case AbilityEventObj.EventTimeType.EventTime:
                        timePoint = evt.EventTime;
                        break;
                    case AbilityEventObj.EventTimeType.EventRange:
                        timePoint = evt.EventRange.x; // 使用范围的开始时间
                        break;
                    case AbilityEventObj.EventTimeType.EventMultiRange:
                        if (evt.EventMultiRange.Length > 0)
                            timePoint = evt.EventMultiRange[0]; // 使用多范围的第一个时间点
                        break;
                }
                
                eventTimeList.Add(new KeyValuePair<float, int>(timePoint, i));
            }
            
            // 按时间点排序
            eventTimeList.Sort((a, b) => a.Key.CompareTo(b.Key));
            
            // 创建排序后的事件列表
            List<AbilityEvent> sortedEvents = new List<AbilityEvent>();
            foreach (var item in eventTimeList)
            {
                sortedEvents.Add(SelectedAbilityObj.events[item.Value]);
            }
            
            // 更新事件列表
            SelectedAbilityObj.events = sortedEvents;
            
            // 刷新UI和数据
            EditorUtility.SetDirty(SelectedAbilityObj);
            AssetDatabase.SaveAssets();
            LoadL3();
            Repaint();
        }

    }

}


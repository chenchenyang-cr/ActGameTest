using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
namespace CombatEditor
{
    public partial class CombatEditor : EditorWindow
    {
        public void PaintL3()
        {
            if (!ConfigNullCheck()) return;
            PaintRuler();
            PaintTrack();
            PaintTimeLineScaler();
        }
        public void PaintTrack()
        {
            InitBeforePaintTrack();
            InitRect();
            PaintPreviewRange();
            InitData();

            for (int i = 0; i < AnimEventTracks.Count; i++)
            {
                PaintEventBG(i);
                PaintTrackBG(i);
                //Draggable
                if (AnimEventTracks[i].eve.Obj.GetEventTimeType() == AbilityEventObj.EventTimeType.EventRange)
                {
                    PaintRangeRect(i);
                }

                else if (AnimEventTracks[i].eve.Obj.GetEventTimeType() == AbilityEventObj.EventTimeType.EventTime)
                {
                    PaintPointRect(i);
                }

                else if (AnimEventTracks[i].eve.Obj.GetEventTimeType() == AbilityEventObj.EventTimeType.EventMultiRange)
                {
                    PaintMultiRangeRect(i);
                }
            }

            // 处理框选功能
            HandleBoxSelect();

            GUI.EndScrollView();
            PaintCurrentIndicator();
            PaintEndLine();
        }
        public void PaintPreviewRange()
        {
            GUI.Box(new Rect(AnimTrackRect.x, AnimTrackRect.y, L3SurfaceRect.width, AnimTrackRect.height), "", "AnimationEventBackground");

            //DrawSelected
            if (SelectedTrackIndex == 0)
            {
                EditorGUI.DrawRect(AnimTrackRect, SelectedTrackColor);
            }
            int AnimStartFrame = Mathf.RoundToInt(SelectedAbilityObj.PreviewPercentageRange.x * AnimFrameCount);
            int AnimEndFrame = Mathf.RoundToInt(SelectedAbilityObj.PreviewPercentageRange.y * AnimFrameCount);
            int[] AnimTimeRange =
                AnimClipHelper.DrawHorizontalDraggableRange(
                    AnimStartFrame,
                    AnimEndFrame,
                    AnimFrameCount,
                    AnimTrackRect,
                    Color.white,
                    "flow node 4",
                    5,
                    () =>
                    {
                        UpdateAsset(SelectedAbilityObj);
                        HardResetPreviewToCurrentFrame();
                    });
            SelectedAbilityObj.PreviewPercentageRange.x = (float)AnimTimeRange[0] / (float)AnimFrameCount;
            SelectedAbilityObj.PreviewPercentageRange.y = (float)AnimTimeRange[1] / (float)AnimFrameCount;
        }


        public void PaintPointRect(int i)
        {
            Rect AvilableTrackRect = new Rect(L3TrackAvailableRect.x, L3TrackAvailableRect.y + (i + 1) * LineHeight, L3TrackAvailableRect.width, LineHeight);
            Rect OutTrackRect = new Rect(L3TrackAvailableRect.x, L3TrackAvailableRect.y + (i + 1) * LineHeight, MaxWidth, LineHeight);

            int StartFrame = Mathf.RoundToInt((AnimEventTracks[i].eve.EventTime * AnimFrameCount));
            StartFrame = AnimEventTracks[i].helper.DrawEnhancedDraggablePoint(
                  StartFrame,
                  AnimFrameCount,
                  AvilableTrackRect,
                   Color.white,
                    "flow node 3",
                  TimePointWidth,
                  true,
                  true,false,null
                  , () =>
                  {
                      OnDragEventTimePoint();
                  }
                      );
            if (AnimFrameCount != 0)
            {
                AnimEventTracks[i].eve.EventTime = (float)StartFrame / (float)AnimFrameCount;
            }

            // 为有条件的事件添加标记
            if (AnimEventTracks[i].eve.condition.hasCondition)
            {
                // 计算事件点的位置
                float xPos = AvilableTrackRect.x + (AnimEventTracks[i].eve.EventTime * AvilableTrackRect.width);
                Rect conditionIconRect = new Rect(xPos - 8, AvilableTrackRect.y + (LineHeight/2) - 8, 16, 16);
                
                // 使用新的条件系统获取颜色和标签
                Color conditionColor = AnimEventTracks[i].eve.condition.GetConditionColor();
                string conditionLabel = AnimEventTracks[i].eve.condition.GetConditionShortLabel();
                
                // 绘制条件标记
                EditorGUI.DrawRect(conditionIconRect, conditionColor);
                GUI.Label(conditionIconRect, conditionLabel, EditorStyles.centeredGreyMiniLabel);
                
                // 添加鼠标提示
                if (conditionIconRect.Contains(Event.current.mousePosition))
                {
                    string tooltipText = "条件: " + AnimEventTracks[i].eve.condition.GetConditionDisplayName();
                    GUI.Label(new Rect(Event.current.mousePosition, new Vector2(120, 20)), tooltipText, EditorStyles.helpBox);
                }
            }
        }
        public void PaintRangeRect(int i)
        {

            Rect AvilableTrackRect = new Rect(L3TrackAvailableRect.x, L3TrackAvailableRect.y + (i + 1) * LineHeight, L3TrackAvailableRect.width, LineHeight);
            Rect OutTrackRect = new Rect(L3TrackAvailableRect.x, L3TrackAvailableRect.y + (i + 1) * LineHeight, MaxWidth, LineHeight);

            int StartFrame = Mathf.RoundToInt(AnimEventTracks[i].eve.EventRange.x * AnimFrameCount);
            int EndFrame = Mathf.RoundToInt(AnimEventTracks[i].eve.EventRange.y * AnimFrameCount);
            int[] TimeRange =
                AnimEventTracks[i].helper.DrawHorizontalDraggableRangeWithMovement(
                    StartFrame,
                    EndFrame,
                    AnimFrameCount,
                    AvilableTrackRect,
                    Color.white,
                    "flow node 3",
                    5,
                      () =>
                      {
                          OnDragEventTimePoint();
                      }
                      );

            if (AnimFrameCount != 0)
            {
                AnimEventTracks[i].eve.EventRange.x = (float)TimeRange[0] / (float)AnimFrameCount;
                AnimEventTracks[i].eve.EventRange.y = (float)TimeRange[1] / (float)AnimFrameCount;
            }
            
            // 为有条件的事件添加标记
            if (AnimEventTracks[i].eve.condition.hasCondition)
            {
                // 计算范围开始位置
                float xStartPos = AvilableTrackRect.x + (AnimEventTracks[i].eve.EventRange.x * AvilableTrackRect.width);
                float rangeWidth = (AnimEventTracks[i].eve.EventRange.y - AnimEventTracks[i].eve.EventRange.x) * AvilableTrackRect.width;
                Rect conditionRect = new Rect(xStartPos, AvilableTrackRect.y + (LineHeight/2), rangeWidth, 4);
                
                // 使用新的条件系统获取颜色和标签
                Color conditionColor = AnimEventTracks[i].eve.condition.GetConditionColor();
                string conditionLabel = AnimEventTracks[i].eve.condition.GetConditionShortLabel();
                
                // 绘制条件标记
                EditorGUI.DrawRect(conditionRect, conditionColor);
                
                // 添加条件标识图标
                Rect conditionIconRect = new Rect(xStartPos - 8, AvilableTrackRect.y + (LineHeight/2) - 8, 16, 16);
                EditorGUI.DrawRect(conditionIconRect, conditionColor);
                GUI.Label(conditionIconRect, conditionLabel, EditorStyles.centeredGreyMiniLabel);
                
                // 添加鼠标提示
                if (conditionIconRect.Contains(Event.current.mousePosition) || conditionRect.Contains(Event.current.mousePosition))
                {
                    string tooltipText = "条件: " + AnimEventTracks[i].eve.condition.GetConditionDisplayName();
                    GUI.Label(new Rect(Event.current.mousePosition, new Vector2(120, 20)), tooltipText, EditorStyles.helpBox);
                }
            }
        }

        public void PaintMultiRangeRect(int i)
        {
            Rect AvilableTrackRect = new Rect(L3TrackAvailableRect.x, L3TrackAvailableRect.y + (i + 1) * LineHeight, L3TrackAvailableRect.width, LineHeight);
            Rect OutTrackRect = new Rect(L3TrackAvailableRect.x, L3TrackAvailableRect.y + (i + 1) * LineHeight, MaxWidth, LineHeight);
            //Get Paintable Range
            int[] TargetFrames = new int[AnimEventTracks[i].eve.Obj.GetMultiRangeCount() - 1];
            for (int j = 0; j < TargetFrames.Length; j++)
            {
                TargetFrames[j] = Mathf.RoundToInt(AnimEventTracks[i].eve.EventMultiRange[j] * AnimFrameCount);
            }
            for (int k = 0; k < TargetFrames.Length; k++)
            {
            }
            string[] names = (AnimEventTracks[i].eve.Obj as AbilityEventObj_States).States;
            int[] Targets = AnimEventTracks[i].helper.DrawHorizontalMultiDraggableWithMovement(TargetFrames, names, AnimFrameCount, AvilableTrackRect, Color.white, TimePointWidth, OnDragEventTimePoint);
            for(int j =0; j < Targets.Length; j++)
            {
                AnimEventTracks[i].eve.EventMultiRange[j] = (float)Targets[j] / (float)AnimFrameCount;
            }
        }


        public void PaintEventBG(int i)
        {
            Rect OutTrackRect = new Rect(L3TrackAvailableRect.x, L3TrackAvailableRect.y + (i + 1) * LineHeight, MaxWidth, LineHeight);
            GUI.Box(OutTrackRect, "", "AnimationEventBackground");
        }
        public void PaintTrackBG(int i)
        {
            Rect AvilableTrackRect = new Rect(L3TrackAvailableRect.x, L3TrackAvailableRect.y + (i + 1) * LineHeight, L3TrackAvailableRect.width, LineHeight);
            
            // 为选中的轨道绘制背景
            if (SelectedTrackIndex == i + 1)
            {
                EditorGUI.DrawRect(AvilableTrackRect, SelectedTrackColor);
            }
            else if (selectedTrackIndices.Contains(i))
            {
                // 为框选选中的轨道绘制不同颜色背景
                EditorGUI.DrawRect(AvilableTrackRect, new Color(0, 0.5f, 1f, 0.2f));
            }
        }
        public void PaintCurrentIndicator()
        {
            if (CurrentFrame == 0) { return; }
            Vector3 CurrentTimeLineP1 = new Vector3(L3TrackAvailableRect.x + ((float)CurrentFrame / AnimFrameCount) * L3TrackAvailableRect.width, 0, 0);
            Vector3 CurrentTimeLineP2 = CurrentTimeLineP1 + new Vector3(0, position.height, 0);
            DrawVerticalLine(CurrentTimeLineP1, CurrentTimeLineP2, Color.white, 1);
        }
        public void PaintEndLine()
        {
            var EndLineTop = new Vector2(L3ViewRect.x + L3ViewRect.width, Height_Top);
            var EndLineBottom = EndLineTop + new Vector2(0, LineHeight * (AnimEventTracks.Count + 1));
            DrawVerticalLine(EndLineTop, EndLineBottom, new Color(1, 1, 1, 0.3f), 1);
        }
        public void PaintRuler()
        {
            #region NullCheck
            if (SelectedAbilityObj == null)
            {
                return;
            }
            if (SelectedAbilityObj.Clip == null)
            {
                return;
            }
            if (TopFrameThumbHelper == null)
            {
                TopFrameThumbHelper = new TimeLineHelper(this);
            }

            #endregion
            #region InitValues


            #endregion
            //UpdateAnimation
            #region UpdateAnimation
            CurrentFrame = TopFrameThumbHelper.DrawHorizontalDraggablePoint(CurrentFrame, AnimFrameCount, new Rect(L3TrackAvailableRect.x, 0, L3TrackAvailableRect.width, Height_Top), Color.white, GUIStyle.none, 12, true, true,false,
                (Percentage) =>
                {
                    OnDragRuler();
                },
                ()=>{ Repaint(); }
                );

            #endregion
            PaintScaleIndicators();


        }
        public void PaintScaleIndicators()
        {
            var StartX = Width_Ability + Width_TrackLabel;
            Rect L3HeadOutRect = new Rect(StartX, 0, position.width - Width_Inspector - StartX, Height_Top);
            Rect L3HeadInnerRect = new Rect(StartX, 0, L3TrackAvailableRect.width, Height_Top);
            Scroll_Ruler = GUI.BeginScrollView(L3HeadOutRect, Scroll_Ruler, L3HeadInnerRect, GUIStyle.none, GUIStyle.none);
            Scroll_Ruler = new Vector2(Scroll_Track.x, 0);
            var ViewableFrameCount = MaxWidth / FrameIntervalDistance;
            for (int i = 0; i < ViewableFrameCount; i += FrameIntervalCount)
            {
                Vector3 StartPoint = new Vector3(L3TrackAvailableRect.x + i * FrameIntervalDistance, L3TrackAvailableRect.y, 0);

                if (i <= AnimFrameCount)
                {
                    DrawVerticalLine(StartPoint, StartPoint - new Vector3(0, 10, 0), Color.white, 1);
                    GUI.Label(new Rect(StartPoint.x, StartPoint.y - 20, 35, 20), i.ToString());
                }
                else
                {
                    GUIStyle style = new GUIStyle(GUI.skin.label);
                    style.normal.textColor = new Color(1, 1, 1, 0.3f);

                    DrawVerticalLine(StartPoint, StartPoint - new Vector3(0, 10, 0), new Color(1, 1, 1, 0.3f), 1);
                    GUI.Label(new Rect(StartPoint.x, StartPoint.y - 20, 35, 20), i.ToString(), style);

                }

            }
            //���Ŀ̶�
            Vector3 FrameEndStartPoint = new Vector3(L3TrackAvailableRect.x + AnimFrameCount * FrameIntervalDistance, L3TrackAvailableRect.y, 0);
            DrawVerticalLine(FrameEndStartPoint, FrameEndStartPoint - new Vector3(0, 10, 0), Color.white, 1);
            GUI.Label(new Rect(FrameEndStartPoint.x, FrameEndStartPoint.y - 20, 35, 20), AnimFrameCount.ToString());
            GUI.EndScrollView();
        }

        bool ShowScaler = true;
        public void PaintTimeLineScaler()
        {
            var width = 150; // 增加宽度以容纳提示文本
            var height = 20;
            var Offset = 10;

            Rect rect;
            if (ShowScaler)
            {
                rect = new Rect(position.width - width - Offset, position.height - height - Offset - 3, width - 3, height);
            }
            else
            {
                rect  = new Rect(position.width - Offset - height, position.height - height - Offset - 3, height, height);
            }
            GUI.Box(rect,new GUIContent(""), EditorStyles.helpBox);
            GUILayout.BeginArea(rect);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(new GUIContent("S"), GUILayout.Width(height)))
            {
                ShowScaler = !ShowScaler;
            }
            if (ShowScaler)
            {
                // 添加提示文本
                GUILayout.Label(new GUIContent("缩放:"), GUILayout.Width(35));
                
                // 绘制滑块
                TimeLineScaler = GUILayout.HorizontalSlider(TimeLineScaler, 0.4f, 1f, GUILayout.Width(60));
                
                // 显示Ctrl+滚轮提示
                GUIStyle tipStyle = new GUIStyle(EditorStyles.miniLabel);
                tipStyle.normal.textColor = new Color(0.7f, 0.7f, 0.7f);
                GUILayout.Label(new GUIContent("Ctrl+滚轮"), tipStyle);
            }
            
            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        // 处理框选功能
        private void HandleBoxSelect()
        {
            Event e = Event.current;
            Rect trackAreaRect = new Rect(
                L3TrackAvailableRect.x,
                L3TrackAvailableRect.y + LineHeight, // 从第一条轨道开始
                L3TrackAvailableRect.width,
                LineHeight * AnimEventTracks.Count
            );

            // 处理鼠标事件
            switch (e.type)
            {
                case EventType.MouseDown:
                    if (e.button == 0 && trackAreaRect.Contains(e.mousePosition) && !isDraggingMultipleTracks)
                    {
                        // 检查是否点击了空白区域
                        bool clickedOnTrackContent = false;
                        
                        // 检查是否点击了任何选中的轨道内容
                        if (selectedTrackIndices.Count > 0)
                        {
                            for (int i = 0; i < selectedTrackIndices.Count; i++)
                            {
                                int trackIndex = selectedTrackIndices[i];
                                if (trackIndex >= 0 && trackIndex < AnimEventTracks.Count)
                                {
                                    Rect trackRect = new Rect(
                                        L3TrackAvailableRect.x, 
                                        L3TrackAvailableRect.y + (trackIndex + 1) * LineHeight, 
                                        L3TrackAvailableRect.width, 
                                        LineHeight
                                    );
                                    
                                    if (trackRect.Contains(e.mousePosition))
                                    {
                                        AbilityEventObj eventObj = AnimEventTracks[trackIndex].eve.Obj;
                                        Rect contentRect = GetTrackContentRect(trackIndex);
                                        
                                        if (contentRect.width > 0 && contentRect.Contains(e.mousePosition))
                                        {
                                            // 点击了轨道内容，开始多轨道拖动
                                            isDraggingMultipleTracks = true;
                                            multiTrackDragStartPos = e.mousePosition;
                                            multiTrackDragOffset = 0;
                                            clickedOnTrackContent = true;
                                            e.Use();
                                            return;
                                        }
                                    }
                                }
                            }
                        }
                        
                        // 只有当点击了空白区域时才开始框选
                        if (!clickedOnTrackContent)
                        {
                            isBoxSelecting = true;
                            boxSelectStartPos = e.mousePosition;
                            boxSelectCurrentPos = e.mousePosition;
                            
                            // 如果没有按住Ctrl/Command键，清除之前的选择
                            if (!e.control && !e.command)
                            {
                                selectedTrackIndices.Clear();
                            }
                            
                            e.Use();
                        }
                    }
                    break;

                case EventType.MouseDrag:
                    // 处理框选拖动
                    if (isBoxSelecting)
                    {
                        boxSelectCurrentPos = e.mousePosition;
                        Repaint();
                        e.Use();
                    }
                    // 处理多轨道拖动
                    else if (isDraggingMultipleTracks)
                    {
                        // 计算拖动的水平偏移量
                        float dragDeltaX = e.mousePosition.x - multiTrackDragStartPos.x;
                        float deltaPercentage = dragDeltaX / L3TrackAvailableRect.width;
                        multiTrackDragOffset = deltaPercentage;
                        
                        // 更新所有选中轨道的位置
                        UpdateSelectedTracksPosition();
                        
                        multiTrackDragStartPos = e.mousePosition;
                        Repaint();
                        e.Use();
                    }
                    break;

                case EventType.MouseUp:
                    if (isBoxSelecting)
                    {
                        // 结束框选并确定选中的轨道
                        FinalizeBoxSelection();
                        isBoxSelecting = false;
                        e.Use();
                    }
                    else if (isDraggingMultipleTracks)
                    {
                        // 结束多轨道拖动
                        isDraggingMultipleTracks = false;
                        RegisterUndo(SelectedAbilityObj, "Move Multiple Tracks");
                        UpdateAsset(SelectedAbilityObj);
                        e.Use();
                    }
                    break;
            }

            // 如果正在框选，绘制框选矩形
            if (isBoxSelecting)
            {
                Rect selectionRect = GetBoxSelectionRect();
                EditorGUI.DrawRect(selectionRect, new Color(0, 0.5f, 1f, 0.2f));
                GUI.Box(selectionRect, "", new GUIStyle() { border = new RectOffset(1, 1, 1, 1) });
            }
        }

        // 获取轨道内容的矩形区域
        private Rect GetTrackContentRect(int trackIndex)
        {
            Rect trackRect = new Rect(
                L3TrackAvailableRect.x,
                L3TrackAvailableRect.y + (trackIndex + 1) * LineHeight,
                L3TrackAvailableRect.width,
                LineHeight
            );

            AbilityEventObj eventObj = AnimEventTracks[trackIndex].eve.Obj;
            
            if (eventObj.GetEventTimeType() == AbilityEventObj.EventTimeType.EventRange)
            {
                float startX = L3TrackAvailableRect.x + (AnimEventTracks[trackIndex].eve.EventRange.x * L3TrackAvailableRect.width);
                float width = (AnimEventTracks[trackIndex].eve.EventRange.y - AnimEventTracks[trackIndex].eve.EventRange.x) * L3TrackAvailableRect.width;
                return new Rect(startX, trackRect.y, width, trackRect.height);
            }
            else if (eventObj.GetEventTimeType() == AbilityEventObj.EventTimeType.EventTime)
            {
                float xPos = L3TrackAvailableRect.x + (AnimEventTracks[trackIndex].eve.EventTime * L3TrackAvailableRect.width);
                return new Rect(xPos - TimePointWidth/2, trackRect.y, TimePointWidth, trackRect.height);
            }
            else if (eventObj.GetEventTimeType() == AbilityEventObj.EventTimeType.EventMultiRange)
            {
                // 对于多区段轨道，返回整个轨道区域
                float firstX = L3TrackAvailableRect.x + (AnimEventTracks[trackIndex].eve.EventMultiRange[0] * L3TrackAvailableRect.width);
                int segments = eventObj.GetMultiRangeCount();
                float lastX = L3TrackAvailableRect.x + (AnimEventTracks[trackIndex].eve.EventMultiRange[segments-2] * L3TrackAvailableRect.width);
                return new Rect(firstX, trackRect.y, lastX - firstX, trackRect.height);
            }

            return new Rect(0, 0, 0, 0);
        }

        // 更新所有选中轨道的位置
        private void UpdateSelectedTracksPosition()
        {
            if (multiTrackDragOffset == 0 || AnimFrameCount == 0)
                return;
                
            foreach (int trackIndex in selectedTrackIndices)
            {
                if (trackIndex >= 0 && trackIndex < AnimEventTracks.Count)
                {
                    AbilityEvent evt = AnimEventTracks[trackIndex].eve;
                    AbilityEventObj eventObj = evt.Obj;
                    
                    // 根据轨道类型更新位置
                    if (eventObj.GetEventTimeType() == AbilityEventObj.EventTimeType.EventRange)
                    {
                        // 计算新位置，但不改变长度
                        float width = evt.EventRange.y - evt.EventRange.x;
                        float newStartX = evt.EventRange.x + multiTrackDragOffset;
                        
                        // 确保不会超出边界
                        newStartX = Mathf.Clamp(newStartX, 0, 1 - width);
                        
                        evt.EventRange.x = newStartX;
                        evt.EventRange.y = newStartX + width;
                    }
                    else if (eventObj.GetEventTimeType() == AbilityEventObj.EventTimeType.EventTime)
                    {
                        // 单点轨道，直接移动点
                        float newTime = evt.EventTime + multiTrackDragOffset;
                        evt.EventTime = Mathf.Clamp01(newTime);
                    }
                    else if (eventObj.GetEventTimeType() == AbilityEventObj.EventTimeType.EventMultiRange)
                    {
                        // 多区段轨道，保持各区段相对位置不变
                        int segments = eventObj.GetMultiRangeCount();
                        float firstSegment = evt.EventMultiRange[0];
                        float lastSegment = evt.EventMultiRange[segments - 2];
                        float totalWidth = lastSegment - firstSegment;
                        
                        // 计算新的起始位置
                        float newStartPos = firstSegment + multiTrackDragOffset;
                        
                        // 确保不会超出边界
                        newStartPos = Mathf.Clamp(newStartPos, 0, 1 - totalWidth);
                        
                        // 更新所有区段位置
                        float offset = newStartPos - firstSegment;
                        for (int i = 0; i < segments - 1; i++)
                        {
                            evt.EventMultiRange[i] = Mathf.Clamp01(evt.EventMultiRange[i] + offset);
                        }
                    }
                }
            }
            
            // 重置偏移量，防止累积
            multiTrackDragOffset = 0;
        }

        // 获取框选区域的矩形
        private Rect GetBoxSelectionRect()
        {
            float left = Mathf.Min(boxSelectStartPos.x, boxSelectCurrentPos.x);
            float top = Mathf.Min(boxSelectStartPos.y, boxSelectCurrentPos.y);
            float width = Mathf.Abs(boxSelectCurrentPos.x - boxSelectStartPos.x);
            float height = Mathf.Abs(boxSelectCurrentPos.y - boxSelectStartPos.y);
            return new Rect(left, top, width, height);
        }

        // 完成框选并确定选中的轨道
        private void FinalizeBoxSelection()
        {
            Rect selectionRect = GetBoxSelectionRect();
            
            // 检查每条轨道是否在选区内
            for (int i = 0; i < AnimEventTracks.Count; i++)
            {
                Rect trackRect = new Rect(
                    L3TrackAvailableRect.x,
                    L3TrackAvailableRect.y + (i + 1) * LineHeight,
                    L3TrackAvailableRect.width,
                    LineHeight
                );
                
                // 如果轨道和选区有交叉，则选中该轨道
                if (trackRect.Overlaps(selectionRect))
                {
                    if (!selectedTrackIndices.Contains(i))
                    {
                        selectedTrackIndices.Add(i);
                    }
                }
            }
        }
    }
}

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
namespace CombatEditor
{
    public partial class CombatEditor
    {

        private void Update()
        {
            // Ensure we're in a valid state before calling Tick
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                return;
            }
            
            // Ensure LastTickTime is initialized
            if (LastTickTime <= 0)
            {
                LastTickTime = Time.realtimeSinceStartup;
            }
            
            if ((Time.realtimeSinceStartup - LastTickTime) >= (1 / 60f))
            {
                try
                {
                    Tick();
                }
                catch (System.Exception ex)
                {
                    // Log error and stop playing to prevent repeated exceptions
                    Debug.LogError("Error in CombatEditor Tick: " + ex.Message);
                    ResetPlayStates();
                }
            }
            
            if (Time.realtimeSinceStartup - StartTime >= 1)
            {
                StartTime = Mathf.Infinity;
            }
        }

        public void PaintL2()
        {
            if (SelectedController == null) return;
            if (SelectedController._animator == null) return;
            if (SelectedAbilityObj == null) return;
            if (SelectedAbilityObj.Clip == null) return;
            PaintPlayButtons();
            PaintTrackLabels();
            HandleDraggingEvents();

            if (DragHandleRequired)
            {
                PaintL2DragIndicator();
            }
            DragHandleRequired = false;
        }
        private static GUIStyle ToggleButtonStyleNormal = null;
        private static GUIStyle ToggleButtonStyleToggled = null;

        public void PaintPlayButtons()
        {
            //Control
            if (ToggleButtonStyleNormal == null)
            {
                ToggleButtonStyleNormal = new GUIStyle(EditorStyles.toolbarButton);
                ToggleButtonStyleToggled = new GUIStyle(EditorStyles.toolbarButton);
                ToggleButtonStyleNormal.fixedHeight = LineHeight;
                ToggleButtonStyleToggled.fixedHeight = LineHeight;
                ToggleButtonStyleToggled.normal.background = ToggleButtonStyleToggled.active.background;
            }
            float ButtonWidth = 40;
            float startX = (L2Rect.width - 6 * ButtonWidth) / 2;
            float ButtonHeight = LineHeight;

            Rect GoToZeroRect = new Rect(startX, 0, ButtonWidth, ButtonHeight);
            Rect PlayAnimRect = new Rect(startX + ButtonWidth, 0, ButtonWidth, ButtonHeight);
            Rect LoopAnimRect = new Rect(startX + ButtonWidth * 2, 0, ButtonWidth, ButtonHeight);
            Rect StopAnimRect = new Rect(startX + ButtonWidth * 3, 0, ButtonWidth, ButtonHeight);
            Rect ClearPreviewRect = new Rect(startX + ButtonWidth * 4, 0, ButtonWidth, ButtonHeight);
            
            // 添加整理轨道按钮 - 紧接着之前的按钮，并向右偏移16像素避免被竖线挡住
            Rect sortTracksRect = new Rect(startX + ButtonWidth * 5 + 16, 0, ButtonWidth * 1.2f, ButtonHeight);
            
            if (GUI.Button(GoToZeroRect, "", "ButtonLeft"))
            {
                AnimationBackToStart();
            }
            CombatEditorUtility.DrawEditorTextureOnRect(GoToZeroRect, 0.6f, "Animation.FirstKey");
            if (GUI.Button(PlayAnimRect, "", "ButtonMid"))
            {
                if (!IsPlaying)
                {
                    OnStartPlay();
                }
                else
                {
                    OnPausePlay();
                }
            }
            if (IsPlaying)
            {
                CombatEditorUtility.DrawEditorTextureOnRect(PlayAnimRect, 0.6f, "d_PauseButton@2x");
            }
            else
            {
                //CombatEditorUtility.DrawEditorTextureOnRect(PlayAnimRect, 0.6f, "d_PauseButton@2x");
                CombatEditorUtility.DrawEditorTextureOnRect(PlayAnimRect, 0.6f, "PlayButton@2x");
            }
            var LoopToggle = GUI.Toggle(LoopAnimRect, IsLooping, "", "ButtonMid");
            if (LoopToggle != IsLooping)
            {
                IsLooping = LoopToggle;
                if (IsLooping)
                {
                    OnStartLoop();
                }
                else
                {
                    OnPausePlay();
                }
            }
            if (IsLooping)
            {
                CombatEditorUtility.DrawEditorTextureOnRect(LoopAnimRect, 0.6f, "d_PauseButton@2x");
            }
            else
            {
                CombatEditorUtility.DrawEditorTextureOnRect(LoopAnimRect, 0.7f, "d_preAudioLoopOff@2x");
            }
            if (GUI.Button(StopAnimRect, "" ,"ButtonMid"))
            {
                OnStopPlayAnimation();
            }
            CombatEditorUtility.DrawEditorTextureOnRect(StopAnimRect, 0.6f, "beginButton");

            //if (GUI.Button(PauseRect, "", "ButtonMid"))
            //{
            //    OnStopPlayAnimation();
            //}
            //CombatEditorUtility.DrawEditorTextureOnRect(PauseRect, 0.6f, "d_PauseButton@2x");


            GUIStyle style = new GUIStyle("ButtonRight");
            style.margin = new RectOffset(0, 0, 0, 0);
            style.fontSize = 20;
            style.padding = new RectOffset(0, 0, 0, 0);
            style.alignment = TextAnchor.MiddleCenter;
            style.fontStyle = FontStyle.Bold;
            style.contentOffset = new Vector2(0, 0);
            if (GUI.Button(ClearPreviewRect, "T", style))
            {
                OnStopPlayAnimation();
                OnEndPreview();
            }

            // 添加整理轨道按钮
            GUIStyle sortButtonStyle = new GUIStyle("ButtonMid");
            sortButtonStyle.margin = new RectOffset(0, 1, 0, 0);
            sortButtonStyle.fontSize = 10;
            sortButtonStyle.alignment = TextAnchor.MiddleCenter;
            sortButtonStyle.fontStyle = FontStyle.Bold;
            Color originalColor = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.4f, 0.8f, 1f, 1f); // 使用浅蓝色背景使按钮更醒目
            if (GUI.Button(sortTracksRect, "整理轨道", sortButtonStyle))
            {
                SortEventsByTimeOrder();
            }
            GUI.backgroundColor = originalColor;
            
            //CombatEditorUtility.DrawEditorTextureOnRect(TimeMulIcon, 1, "d_AnimationClip Icon");

            //CombatEditorUtility.DrawEditorTextureOnRect(LoopWaitIcon, 0.6f, "beginButton");

        }

        // 添加静态变量来存储轨道UI信息
        private static Dictionary<int, AbilityEvent> _trackControlIDToEvent = new Dictionary<int, AbilityEvent>();
        private static Dictionary<int, int> _trackControlIDToIndex = new Dictionary<int, int>();
        
        public void PaintTrackLabels()
        {
            // 清除之前的映射
            _trackControlIDToEvent.Clear();
            _trackControlIDToIndex.Clear();

            #region ConfigAnimRange
            Rect AnimConfigRect = new Rect(L2Rect.x, L3TrackAvailableRect.y, L2Rect.width, LineHeight);

            TrackRect = new Rect(L2Rect.x, L2Rect.y + Height_Top, L2Rect.width, L2Rect.height - Height_Top);
            Scroll_Fields = GUI.BeginScrollView(TrackRect, Scroll_Fields, new Rect(TrackRect.x, TrackRect.y, TrackRect.width, (SelectedAbilityObj.events.Count + 2) * LineHeight), GUIStyle.none, GUIStyle.none);
            Scroll_Track = new Vector2(Scroll_Track.x, Scroll_Fields.y);

            GUIStyle style = new GUIStyle(GUI.skin.button);
            style.alignment = GUI.skin.label.alignment;

            Color DefaultColor = GUI.backgroundColor;
            if (CurrentInspectedType == InspectedType.PreviewConfig) GUI.backgroundColor = Color.green;
            if (GUI.Button(AnimConfigRect, "PreviewRange", style))
            {
                ChangeInspectedType(InspectedType.PreviewConfig);
            }
            GUI.backgroundColor = DefaultColor;
            
            // 添加整理轨道按钮
            Rect sortButtonRect = new Rect(AnimConfigRect.x + AnimConfigRect.width + 5, AnimConfigRect.y, 100, AnimConfigRect.height);
            Color originalBgColor = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.4f, 0.8f, 1f, 1f); // 使用浅蓝色背景
            GUIStyle sortBtnStyle = new GUIStyle(style);
            sortBtnStyle.fontStyle = FontStyle.Bold;
            if (GUI.Button(sortButtonRect, "按时间排序", sortBtnStyle))
            {
                SortEventsByTimeOrder();
            }
            GUI.backgroundColor = originalBgColor;
            #endregion
            List<AbilityEvent> eves = SelectedAbilityObj.events;

            for (int i = 0; i < eves.Count; i++)
            {
                if (eves[i].Obj == null)
                {
                    eves.RemoveAt(i);
                    i = 0;
                    AssetDatabase.SaveAssets();
                    continue;
                }

                Rect LabelRect = new Rect(L2Rect.x + 2 * LineHeight, L3TrackAvailableRect.y + (i + 1) * LineHeight, Width_TrackLabel - 3 * LineHeight - SplitterIntervalDistance, LineHeight);
                Rect PreviewToggle = new Rect(L2Rect.x + LineHeight, L3TrackAvailableRect.y + (i + 1) * LineHeight, LineHeight, LineHeight);
                Rect ToggleRect = new Rect(L2Rect.x, L3TrackAvailableRect.y + (i + 1) * LineHeight, LineHeight, LineHeight);
                var StartX = Width_Ability + Width_TrackLabel;
                Rect DeleteRect = new Rect(StartX - LineHeight - SplitterIntervalDistance , Height_Top + (i + 1) * LineHeight, LineHeight, LineHeight);

                AbilityEventObj obj = eves[i].Obj;
                Event e = Event.current;
                if (e.isKey && e.type == EventType.KeyDown)
                {
                    if (e.keyCode == KeyCode.F2 && LabelRect.Contains(e.mousePosition))
                    {
                        StartPaintRenameField(LabelRect, obj.name, () => { obj.name = NameOfRename; AssetDatabase.SaveAssets(); });
                        Debug.Log("UseEvent?");
                        e.Use();
                    }
                }

                int controlID = GUIUtility.GetControlID(FocusType.Passive);
                
                // 🎯 存储控件ID和对应的事件映射
                _trackControlIDToEvent[controlID] = eves[i];
                _trackControlIDToIndex[controlID] = i;
                
                if (GUIUtility.hotControl == controlID && !LabelRect.Contains(e.mousePosition))
                {
                    DragHandleRequired = true;
                }

                switch (e.type)
                {
                    case (EventType.MouseDown):
                        if (LabelRect.Contains(e.mousePosition))
                        {
                            GUIUtility.hotControl = controlID;
                            OnClickFields(i, eves[i]);
                            //Debug.Log("UseEvent?");
                            e.Use();
                            StartDragElement(new Vector2(LabelRect.x + LabelRect.width * 0.5f, LabelRect.y + LabelRect.height * 0.5f));
                            L2DragEndIndex = i;
                            DraggingFieldStartIndex = i;

                        }
                        break;
                    case (EventType.MouseUp):
                        {
                            if (GUIUtility.hotControl != controlID) break;
                            EndDrag();
                            OnSwapAnimEvents(DraggingFieldStartIndex, L2DragEndIndex);
                        }
                        break;
                    case (EventType.Ignore):
                        {
                            if (GUIUtility.hotControl != controlID) break;
                            EndDrag();
                            OnSwapAnimEvents(DraggingFieldStartIndex, L2DragEndIndex);
                        }
                        break;
                }

                if (!eves[i].Obj.IsActive)
                {
                    GUI.backgroundColor = Color.red;
                }
                if (SelectedTrackIndex == i + 1)
                {
                    HighlightBGIfInspectType(InspectedType.Track);
                }
                //ToggleOnAndOff
                if (GUI.Button(ToggleRect, GUIContent.none))
                {
                    OnClickToggleActive(eves[i].Obj);
                }

                if (eves[i].Obj.IsActive)
                {
                    CombatEditorUtility.DrawEditorTextureOnRect(ToggleRect, 0.5f, "FilterSelectedOnly@2x");
                }
                else
                {
                    CombatEditorUtility.DrawEditorTextureOnRect(ToggleRect, 0.7f, "scenevis_hidden@2x");
                }
                
                // 🎯 为标签按钮创建特殊的控件ID，用于右键检测
                int labelControlID = GUIUtility.GetControlID(FocusType.Passive);
                _trackControlIDToEvent[labelControlID] = eves[i];
                _trackControlIDToIndex[labelControlID] = i;
                
                //Label
                if (GUI.Button(LabelRect, eves[i].Obj.name, style))
                {
                    //OnClickFields(i, eves[i]);
                }
                
                #region TogglePreviewButton
                GUI.backgroundColor = DefaultColor;
                if (eves[i].Previewable)
                {
                    GUI.backgroundColor = Color.green;
                }
                int PreviewControlID = GUIUtility.GetControlID(FocusType.Passive);
                if (GUI.Button(PreviewToggle, GUIContent.none))
                {
                    OnTogglePreview(eves[i]);
                }
                if (eves[i].Previewable)
                {
                    CombatEditorUtility.DrawEditorTextureOnRect(PreviewToggle, 0.6f, "d_Record On@2x");
                }
                else
                {
                    CombatEditorUtility.DrawEditorTextureOnRect(PreviewToggle, 0.6f, "d_Record Off@2x");
                }

                GUI.backgroundColor = DefaultColor;

                #endregion
                if (GUI.Button(DeleteRect, "-", MyDeleteButtonStyle))
                {
                    Object[] assets = AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(SelectedAbilityObj));
                    if (assets.Contains(SelectedAbilityObj.events[i].Obj))
                    {
                        Undo.DestroyObjectImmediate(SelectedAbilityObj.events[i].Obj);
                        AssetDatabase.SaveAssets();
                    }
                    SelectedAbilityObj.events.RemoveAt(i);

                    OnAnimEventChanges();

                }
            }

            for (int i = 0; i < eves.Count; i++)
            {

            }
            PaintAddAbilityButton();
            GUI.EndScrollView();
        
        }
        
        public void PaintAddAbilityButton()
        {
            #region AddObjButton
            List<AbilityEvent> eves = SelectedAbilityObj.events;
            Rect AddButtonRect = new Rect(L2Rect.x, L3TrackAvailableRect.y + (eves.Count + 1) * LineHeight, L2Rect.width , LineHeight);
            if (GUI.Button(AddButtonRect, "+", MyDeleteButtonStyle))
            {
                CreatAddTrackMenu();
            }
            #endregion
        }

        public void PaintDragLabelDragger()
        {
            Debug.Log(DragEndRect);
            EditorGUI.DrawRect(DragEndRect, Color.red);
        }

        public void PaintL2DragIndicator()
        {
            EditorGUI.DrawRect(L2DragRect, Color.green);
            if (LastIndex != L2DragEndIndex)
            {
                Repaint();
            }
            LastIndex = L2DragEndIndex;
        }

        // 🎯 添加静态方法来获取鼠标悬停的轨道事件
        public static AbilityEvent GetMouseOverTrackEvent()
        {
            int hotControl = GUIUtility.hotControl;
            if (hotControl != 0 && _trackControlIDToEvent.ContainsKey(hotControl))
            {
                return _trackControlIDToEvent[hotControl];
            }
            return null;
        }
        
        // 🎯 添加静态方法来获取鼠标悬停的轨道索引
        public static int GetMouseOverTrackIndex()
        {
            int hotControl = GUIUtility.hotControl;
            if (hotControl != 0 && _trackControlIDToIndex.ContainsKey(hotControl))
            {
                return _trackControlIDToIndex[hotControl];
            }
            return -1;
        }
    }
}

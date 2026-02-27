using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor.Animations;

namespace CombatEditor
{	
	public class CombatPreviewController 
	{
	    public CombatController _combatController;
	    public CombatEditor editor;
	    public AbilityScriptableObject AbilityObj;
	
	    //CurrentRoot Data is needed when animations have rootmotions in preview.
	    public static Vector3 CurrentRootT;
	    public static Vector3 CurrentRootQ;
	    public static Vector3 CurrentMotionT;
	    public static Vector3 CurrentMotionQ;
	
	
	    public void SetPreviewTarget(CombatController combatController, AbilityScriptableObject animObj)
	    {
	        _combatController = combatController;
	    }
	
	    public void FlushAndInsAllPreviews( bool ResetToFrame0 = true)
	    {
	        if (EditorApplication.isPlaying) return;
	        if (_combatController == null)
	        {  return; }
	        if (_combatController._animator == null)
	        { Debug.Log("Please Assign the Animator In gear Icon"); return; }
	        FetchAbility();
	
	        OnDestroyPreview();
	
	        ResetPreviewGroup();
	        InitAllPreviews();
	
	        SetExpandedRecursive(GameObject.Find(CombatGlobalEditorValue.PreviewGroupName), true);
	        RecordPositionBeforeStart();
	   }
	
	    public Vector3 StartControllerPosition;
	
	    public GameObject PreviewGroupObj;
	    public void ResetPreviewGroup()
	    {
	    
	        DestroyPreviewGroupObj();
	        //Reload 
	        if (PreviewGroupObj == null)
	        {
	            PreviewGroupObj = new GameObject(CombatGlobalEditorValue.PreviewGroupName);
	            PreviewGroupObj.hideFlags = HideFlags.DontSaveInEditor;
	        }
	    }
	
	    public void DestroyPreviewGroupObj()
	    {
	        PreviewGroupObj = GameObject.Find(CombatGlobalEditorValue.PreviewGroupName);
	        if (PreviewGroupObj != null)
	        {
	            Object.DestroyImmediate(PreviewGroupObj);
	        }
	    }
	    
	    
	
	    public void OnDestroyPreview()
	    {
	        if (EditorApplication.isPlaying) return;
	        previewsSelfDestroy();
	        ClearAllPreviewHandles();
	        DestroyPreviewGroupObj();
	        ResetMotions();
	        
	        // 检查是否在编辑器拖动模式或编辑器操作（如保存、编译）
	        bool isEditorDragMode = editor != null && (!(editor.IsPlaying || editor.IsLooping) || editor.IsEditorOperation);
	        
	        // 只有在非编辑器模式下才恢复角色的原始位置和旋转
	        if (_combatController != null && !isEditorDragMode)
	        {
	            _combatController.transform.position = CombatGlobalEditorValue.CharacterTransPosBeforePreview;
	            _combatController.transform.rotation = CombatGlobalEditorValue.CharacterRotBeforePreview;
	        }
	        
	        if (AnimationMode.InAnimationMode())
	        {
	            AnimationMode.StopAnimationMode();
	        }
	    }
	
	    //Calls when click the stopbutton.
	    public void OnPreviewBackToStart()
	    {
	        if (EditorApplication.isPlaying) return;
	        previewBackToStart();
	        
	        // 检查是否在编辑器拖动模式
	        bool isEditorDragMode = !(editor.IsPlaying || editor.IsLooping);
	        
	        // 只有在播放预览的情况下才恢复角色的原始位置和旋转
	        if (_combatController != null && !isEditorDragMode)
	        {
	            _combatController.transform.position = CombatGlobalEditorValue.CharacterTransPosBeforePreview;
	            _combatController.transform.rotation = CombatGlobalEditorValue.CharacterRotBeforePreview;
	        }
	    }
	
	
	    public void RecordPositionBeforeStart()
	    {
	        StartControllerPosition = _combatController.transform.position;
	        CombatGlobalEditorValue.CharacterTransPosBeforePreview = _combatController.transform.position;
	        // Add recording of rotation before preview
	        CombatGlobalEditorValue.CharacterRotBeforePreview = _combatController.transform.rotation;
	    }
	
	
	    public void ResetMotions()
	    {
	        CurrentRootT = Vector3.zero;
	        CurrentRootQ = Vector3.zero;
	        CurrentMotionT = Vector3.zero;
	        CurrentMotionQ = Vector3.zero;
	    }
	
	    public void previewsSelfDestroy()
	    {
	        if (previews != null)
	        {
	            for (int i = 0; i < previews.Count; i++)
	            {
	                previews[i].DestroyPreview();
	            }
	        }
	    }
	    public void previewBackToStart()
	    {
	        if (previews != null)
	        {
	            for (int i = 0; i < previews.Count; i++)
	            {
	                previews[i].BackToStart();
	            }
	        }
	    }
	
	    public void OnPlayModeStart()
	    {
	        DestroyPreviewGroupObj();
	    }
	
	    //The currentRunning percentage.
	    float PercentageTime;
	
	    // Because of timescale, the particles need the RealTime to perform.
	    float RealTime;
	
    //预览功能
    public void ShowPreviewAtPercentage(float Percentage)
    {
        FetchAbility();
        if (EditorApplication.isPlaying || AbilityObj == null || _combatController == null || _combatController._animator == null) return;
        if (AbilityObj.Clip == null) return;
        PercentageTime = Percentage;
        UpdateAnimation(Percentage);
    }
	
	    public void FetchAbility()
	    {
	        if (editor == null)
	        {
	            editor = CombatEditorUtility.GetCurrentEditor();
	        }
	        AbilityObj = editor.SelectedAbilityObj;
	    }
	
	    public int DebugAnimFrame;
	
	   
	
	    int TotalFrame => (int)(AbilityObj.Clip.length * 60);
	
	
    /// <summary>
    /// 更新刷新动画
    /// </summary>
    public void UpdateAnimation(float percentage)
    {
        if (!AnimationMode.InAnimationMode())
        {
            AnimationMode.StartAnimationMode();
        }
        
        if (AnimationMode.InAnimationMode())
        {
            UpdatePreview(percentage);
        }
        
        // Unity 6 fix: Always repaint scene view after updating animation
        SceneView.RepaintAll();
    }
	
	
    public void UpdateAnimationInEditMode(float time)
    {
        AnimationMode.BeginSampling();
        CombatGlobalEditorValue.Percentage = time;
        if (_combatController != null)
        {
            // 保存原始位置和旋转的备份
            Vector3 originalPosition = CombatGlobalEditorValue.CharacterTransPosBeforePreview;
            Quaternion originalRotation = CombatGlobalEditorValue.CharacterRotBeforePreview;
            
            // Use precise animation clip sampling
            double animTime = time * AbilityObj.Clip.length;
            // Sample animation with the precise time calculation
            AnimationMode.SampleAnimationClip(_combatController._animator.gameObject, AbilityObj.Clip, (float)animTime);
            
            GetCurrentRootMotion(time);
            GetCurrentAnimationMotion(time);

            CombatGlobalEditorValue.CurrentRootMotionOffset = _combatController._animator.transform.rotation * CombatGlobalEditorValue.CurrentMotionTAtGround;
            
            // 编辑器模式下的特殊处理
            bool isEditorMode = !editor.IsPlaying && !editor.IsLooping;
            
            if (!isEditorMode)
            {
                // 只在播放模式下应用位置和旋转
                
                // 计算位置时始终基于原始位置，避免累积误差
                Vector3 position = originalPosition + CombatGlobalEditorValue.CurrentRootMotionOffset + CurrentFrameMotions;
                CurrentCharacterCenter = position;
                
                // 应用位置和旋转效果
                _combatController.transform.position = position;
                
                // 应用旋转效果时也基于原始旋转，避免累积误差
                if (CurrentFrameRotation != Quaternion.identity)
                {
                    Quaternion finalRotation = originalRotation * CurrentFrameRotation;
                    _combatController.transform.rotation = finalRotation;
                }
            }
            else
            {
                // 在编辑器模式下，仅计算当前中心点但不应用变换
                CurrentCharacterCenter = _combatController.transform.position;
            }
            
            CombatGlobalEditorValue.CharacterRootCenterAtCurrentFrame = CurrentCharacterCenter;
        }
        AnimationMode.EndSampling();
    }
	
	   
	
	    // added of Current Motions and Current Roots.
	    public Vector3 CurrentCharacterCenter;
	
	    Vector3 CurrentFrameMotions;
	    Quaternion CurrentFrameRotation = Quaternion.identity; // New field for storing rotation effects
	    
	    public void GetCurrentAnimationMotion(float timePercentage)
	    {
	        CurrentFrameMotions = Vector3.zero;
	        
	        // 检查是否在播放模式，如果不是，则保留当前旋转而不是重置
	        if (!editor.IsPlaying && !editor.IsLooping)
	        {
	            // 在编辑器状态下保留当前旋转
	            // CurrentFrameRotation保持不变
	        }
	        else
	        {
	            // 只在播放模式下重置旋转
	            CurrentFrameRotation = Quaternion.identity; // Reset rotation
	        }
	        
	        if(previews != null)
	        {
	            // Create a list to hold motion previews
	            List<AbilityEventPreview_Motion> Motions = new List<AbilityEventPreview_Motion>();
	            // Create a list to hold rotation previews
	            List<AbilityEventPreview_Rotation> Rotations = new List<AbilityEventPreview_Rotation>();
	            
	            // Find all motion and rotation previews
	            for(int i = 0; i < previews.Count; i++)
	            {
	                if(previews[i] != null)
	                {
	                    if(previews[i].GetType() == typeof(AbilityEventPreview_Motion))
	                    {
	                        Motions.Add((AbilityEventPreview_Motion)previews[i]);
	                    }
	                    else if(previews[i].GetType() == typeof(AbilityEventPreview_Rotation))
	                    {
	                        Rotations.Add((AbilityEventPreview_Rotation)previews[i]);
	                    }
	                }
	            }
	            
	            // Calculate total motion offset with improved precision
	            for(int i = 0; i < Motions.Count; i++)
	            {
	                if(Motions[i] != null)
	                {
	                    // Get precise offset calculation from each motion preview
	                    Vector3 motionOffset = Motions[i].GetOffsetAtCurrentFrame(timePercentage);
	                    // Add to the total with minimized floating point errors
	                    CurrentFrameMotions.x += motionOffset.x;
	                    CurrentFrameMotions.y += motionOffset.y;
	                    CurrentFrameMotions.z += motionOffset.z;
	                }
	            }
	            
	            // Calculate total rotation effect
	            for(int i = 0; i < Rotations.Count; i++)
	            {
	                if(Rotations[i] != null)
	                {
	                    // Get rotation from each rotation preview
	                    Quaternion rotationOffset = Rotations[i].GetRotationAtCurrentFrame(timePercentage);
	                    // Combine rotations
	                    CurrentFrameRotation = rotationOffset * CurrentFrameRotation;
	                }
	            }
	        }
	    }
	
	    public void GetCurrentRootMotion(float timePercentage)
	    {
	        var bindings = AnimationUtility.GetCurveBindings(AbilityObj.Clip);
	        //var targetBinding;
	
	        for (int i = 0; i < bindings.Length; i++)
	        {
	            var CurrentTime = timePercentage * AbilityObj.Clip.length;
	            var curve = AnimationUtility.GetEditorCurve(AbilityObj.Clip, bindings[i]);
	            var value = curve.Evaluate(timePercentage * AbilityObj.Clip.length);
	            switch (bindings[i].propertyName)
	            {
	                case "RootT.x":
	                    CurrentRootT.x = value; break;
	                case "RootT.y":
	                    CurrentRootT.y = value; break;
	                case "RootT.z":
	                    CurrentRootT.z = value; break;
	                case "RootQ.x":
	                    CurrentRootQ.x = value; break;
	                case "RootQ.y":
	                    CurrentRootQ.y = value; break;
	                case "RootQ.z":
	                    CurrentRootQ.z = value; break;
	                case "MotionT.x":
	                    CurrentMotionT.x = value; break;
	                case "MotionT.y":
	                    CurrentMotionT.y = value; break;
	                case "MotionT.z":
	                    CurrentMotionT.z = value; break;
	            }
	        }
	        CombatGlobalEditorValue.CurrentMotionTAtGround = new Vector3(CurrentRootT.x, 0, CurrentRootT.z);
	
	        //Debug.Log(timePercentage + ":" + new Vector3(CurrentRootT.x, CurrentRootT.y, CurrentRootT.z));
	
	        //Debug.Log(timePercentage + ":" + new Vector3(CurrentMotionT.x, 0, CurrentMotionT.z));
	
	    }
	
	    List<AbilityEventPreview> previews;
	
	    public void InitAllPreviews()
	    {
	        previews = new List<AbilityEventPreview>();
	
	        // 获取当前编辑器实例和选中的轨道索引
	        var editor = CombatEditorUtility.GetCurrentEditor();
	        int selectedTrackIndex = editor.SelectedTrackIndex - 1; // 转换为0基索引
	
	        if (AbilityObj != null)
	            for (int i = 0; i < AbilityObj.events.Count; i++)
	            {
	                var abilityEvent = AbilityObj.events[i];
	                if (abilityEvent.Obj == null) { AbilityObj.events.RemoveAt(i); i--; continue; }
	                
	                // 检查是否满足以下条件之一:
	                // 1. 事件是活跃的，并且不是CreateHitBox类型 (所有非HitBox类型的事件都照常预览)
	                // 2. 事件是活跃的，是CreateHitBox类型，且是当前选中的轨道
	                bool isHitBox = abilityEvent.Obj is AbilityEventObj_CreateHitBox;
	                bool isSelectedTrack = (i == selectedTrackIndex);
	                
	                if (abilityEvent.Obj.IsActive && (!isHitBox || (isHitBox && isSelectedTrack)))
	                {
	                    if (abilityEvent.Obj != null)
	                    {
	                        AbilityEventPreview preview;
	                        preview = abilityEvent.Obj.InitializePreview();
	
	                        if (preview != null)
	                        {
	                            preview.eve = abilityEvent;
	                            preview._combatController = _combatController;
	                            preview.AnimObj = AbilityObj;
	                            previews.Add(preview);
	                        }
	                    }
	                }
	            }
	            
	        // 确保只有选中轨道的CreateObjWithHandle事件的Handle可见，隐藏其他的
	        EnsureCreateObjWithHandleVisible();
	        
	        foreach (var preview in previews)
	        {
	            preview.InitPreview();
	        }
	    }
	    
	    /// <summary>
	    /// 确保只有选中轨道的CreateObjWithHandle事件的Handle可见，隐藏其他的
	    /// </summary>
	    private void EnsureCreateObjWithHandleVisible()
	    {
	        if (AbilityObj != null && AbilityObj.events != null)
	        {
	            // 获取当前编辑器实例和选中的轨道索引
	            var editor = CombatEditorUtility.GetCurrentEditor();
	            int selectedTrackIndex = editor != null ? editor.SelectedTrackIndex : 0;
	            
	            for (int i = 0; i < AbilityObj.events.Count; i++)
	            {
	                var evt = AbilityObj.events[i];
	                if (evt.Obj is AbilityEventObj_CreateObjWithHandle)
	                {
	                    // 只有选中的轨道才显示Handle，其他的隐藏
	                    evt.Previewable = (i + 1 == selectedTrackIndex);
	                }
	            }
	        }
	    }
	
	    /// <summary>
	    /// Clear All Preview Objects In Scene. 
	    /// Used when preview object changes, or after compile.
	    /// </summary>
	    public void ClearAllPreviewHandles()
	    {
	        PreviewGroupObj = GameObject.Find(CombatGlobalEditorValue.PreviewGroupName);
	        if (PreviewGroupObj != null)
	        {
	            var handles = PreviewGroupObj.GetComponents<PreviewerOnObject>();
	            foreach (var handle in handles)
	            {
	                handle.SelfDestroy();
	            }
	        }
	    }
	
	    public float PreviewAnimSpeed = 1;
	
	
	    public class AnimScaleTimePoint
	    {
	        public float Percentage;
	        AbilityEventPreview_AnimSpeed Preview;
	        public AnimScaleTimePoint(float percentage, AbilityEventPreview_AnimSpeed preview)
	        {
	            Percentage = percentage;
	            Preview = preview;
	        }
	    }
	
	    public class DividedTimeClip
	    {
	        public float StartTime;
	        public float EndTime;
	        public float Multiplier;
	    }
	
	    List<DividedTimeClip> dividedTimeClips = new List<DividedTimeClip>();
	    public void DivideTimeByTimeScaler()
	    {
	        if (previews != null)
	        {
	            //FetchDatas
	            List<AbilityEventPreview_AnimSpeed> speedScales = new List<AbilityEventPreview_AnimSpeed>();
	            List<AnimScaleTimePoint> SortedTimePercentages = new List<AnimScaleTimePoint>();
	            List<float> SortedTimePoints = new List<float>();
	            SortedTimePoints.Add(0);
	            SortedTimePoints.Add(1);
	            foreach (var preview in previews)
	            {
	                if (preview.GetType() == typeof(AbilityEventPreview_AnimSpeed))
	                {
	                    AbilityEventPreview_AnimSpeed speedScale = (AbilityEventPreview_AnimSpeed)preview;
	                    speedScales.Add(speedScale);
	                    SortedTimePoints.Add(speedScale.StartTimePercentage);
	                    SortedTimePoints.Add(speedScale.EndTimePercentage);
	                }
	            }
	            SortedTimePoints.Sort();
	
	            dividedTimeClips = new List<DividedTimeClip>();
	            //Calculate TimeClips
	            for (int i = 0; i < SortedTimePoints.Count - 1; i++)
	            {
	                var timeClip = new DividedTimeClip();
	                timeClip.StartTime = SortedTimePoints[i];
	                timeClip.EndTime = SortedTimePoints[i + 1];
	                float Multiplier = 1;
	                for(int j=0;j< speedScales.Count;j++)
	                {
	                    if(timeClip.StartTime >= speedScales[j].StartTimePercentage && timeClip.EndTime <= speedScales[j].EndTimePercentage)
	                    {
	                        Multiplier *= speedScales[j].Obj.Speed;
	                    }
	                }
	                timeClip.Multiplier = Multiplier;
	                dividedTimeClips.Add(timeClip);
	            }
	        }
	    }
	    public float GetScaledPercentage(float percentage)
	    {
	        float PercentageAfterScale = 0;
	        for (int i = 0; i < dividedTimeClips.Count; i++)
	        {
	            var timeClip = dividedTimeClips[i];
	            //Enter Next Clip.
	            if (percentage > timeClip.EndTime)
	            {
	                PercentageAfterScale += (timeClip.EndTime - timeClip.StartTime) / timeClip.Multiplier;
	            }
	            if (percentage <= timeClip.EndTime)
	            {
	                if (percentage >= timeClip.StartTime)
	                {
	                    PercentageAfterScale += (percentage - timeClip.StartTime) / timeClip.Multiplier;
	                }
	                    break;
	            }
	        }
	          
	        return PercentageAfterScale;
	    }
	
	
	
	
	    public void UpdatePreview(float percentage)
	    {
	        DivideTimeByTimeScaler();
	        if (previews != null)
	        {
	            foreach (var preview in previews)
	            {
	                preview.FetchCurrentValues();

	                preview.StartTimeScaledPercentage = GetScaledPercentage(preview.StartTimePercentage);
	                preview.EndTimeScaledPercentage = GetScaledPercentage(preview.EndTimePercentage);

	                // 编辑器模式特殊处理
	                bool isEditorMode = !(editor.IsPlaying || editor.IsLooping);
	                
	                // 在拖动预览时的处理
	                if (isEditorMode)
	                {
	                    if (preview.NeedStartFrameValue())
	                    {
	                        UpdateAnimationInEditMode(preview.StartTimePercentage);
	                    }
	                    preview.GetStartFrameDataBeforePreview();
	                }
	                // 播放模式下的处理
	                else
	                {
	                    if (preview.NeedStartFrameValue() && preview.IsOnStartFrame)
	                    {
	                        preview.GetStartFrameDataBeforePreview();
	                    }
	                }
	            }
	            UpdateAnimationInEditMode(percentage);
	            foreach (var preview in previews)
	            {
	                preview.StartTimeScaledPercentage = GetScaledPercentage(preview.StartTimePercentage);
	                preview.EndTimeScaledPercentage = GetScaledPercentage(preview.EndTimePercentage);

	                preview.PreviewRunning(PercentageTime);

	                preview.PreviewRunningInScale(GetScaledPercentage(PercentageTime));
	            }
	        }
	        else
	        UpdateAnimationInEditMode(percentage);
	    }
	
	
	    public static void Collapse(GameObject go, bool collapse)
	    {
	        //var LastSelected = Selection.activeObject;
	        // bail out immediately if the go doesn't have children
	        if (go.transform.childCount == 0) return;
	        // get a reference to the hierarchy window
	        var hierarchy = EditorWindow.GetWindow(typeof(EditorWindow).Assembly.GetType("UnityEditor.SceneHierarchyWindow"));
	
	
	
	        // select our go
	        Selection.activeObject = go;
	
	
	        // create a new key event (RightArrow for collapsing, LeftArrow for folding)
	        var key = new Event { keyCode = collapse ? KeyCode.RightArrow : KeyCode.LeftArrow, type = EventType.KeyDown };
	        // finally, send the window the event
	        hierarchy.SendEvent(key);
	
	
	        //Selection.activeObject = LastSelected;
	
	    }
	    //public static void SelectObject(Object obj)
	    //{
	    //    Selection.activeObject = obj;
	    //}
	    public static EditorWindow GetFocusedWindow(string window)
	    {
	        FocusOnWindow(window);
	        return EditorWindow.focusedWindow;
	    }
	    public static void FocusOnWindow(string window)
	    {
	        EditorApplication.ExecuteMenuItem("Window/" + window);
	    }
	
	    public static void SetExpandedRecursive(GameObject go, bool expand)
	    {
	        var type = typeof(EditorWindow).Assembly.GetType("UnityEditor.SceneHierarchyWindow");
	        var methodInfo = type.GetMethod("SetExpandedRecursive");
	
	        // This differs in unity versions.
	        // Old version should be "Window/Hierarchy."
	        EditorApplication.ExecuteMenuItem("Window/General/Hierarchy");
	
	        var window = EditorWindow.focusedWindow;
	
	        methodInfo.Invoke(window, new object[] { go.GetInstanceID(), expand });
	    }
	
	
	 
	
	}
}

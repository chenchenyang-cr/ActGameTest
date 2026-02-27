using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;

namespace CombatEditor
{	

	public class Track
	{
	    public Object trackContent;
	    public int TrackTriggerFrame;
	    public int TrackEndFrame;
	    public bool TrackIsSelected;
	    public TimeLineHelper helper;
	}
	
	public class AnimEventTrack
	{
	    public AbilityEvent eve;
	    public TimeLineHelper helper;
	    public int StartFrame;
	    public int EndFrame;
	    public AnimEventTrack(AbilityEvent e , EditorWindow window)
	    {
	        eve = e;
	        StartFrame = (int)e.GetEventStartTime();
	        EndFrame = (int)e.GetEventEndTime();
	        helper = new TimeLineHelper( window );
	    }
	}
	
	public partial class CombatEditor : EditorWindow
	{
	    public static string SandBoxPath = "Assets/ScriptableObjects/Abilities/Sandbox/";
	    public static string TemplatesPath = "Assets/ScriptableObjects/Abilities/Templates/";
	
	    // 编辑器操作标志，用于标识是否是由编辑器操作（如保存、编译）触发的预览重置
	    public bool IsEditorOperation = false;
	
	    public static float Height_Top = 40;
	    public static float LineHeight = 25;
	
	
	    Rect boxRect;
	    public float valueChangeValue = 5; 
	    public float Width_TrackLabel = 250;
	    int HeaderFontSize = 15;
	    public Vector2 TimeLineWindowSize = new Vector2(2000, 300);
	
	    public int FrameIntervalCount = 6;
	    public int FrameIntervalDistance => Mathf.RoundToInt( 10 * TimeLineScaler);
	
	    public Vector2 TestTrackPosition;
	    bool IsDraggingTracks;
	    int CurrentDraggedFrame;
	    Object TrackObj;
	    public List<Track> tracks = new List<Track>();
	
	    // 框选相关变量
	    private bool isBoxSelecting = false;
	    private Vector2 boxSelectStartPos;
	    private Vector2 boxSelectCurrentPos;
	    private List<int> selectedTrackIndices = new List<int>();
	    private bool isDraggingMultipleTracks = false;
	    private Vector2 multiTrackDragStartPos;
	    private float multiTrackDragOffset = 0;
	
	    public float Width_Inspector = 350;
	    TimeLineHelper TopFrameThumbHelper;
	
	    int CurrentFrame = 0;
	
	    Rect L3TrackAvailableRect;
	    Rect L2Rect;
	    int AnimFrameCount;
	
	    public bool IsInited = false;
	    GUIStyle MyBoxGUIStyle;
	    static GUIStyle MyDeleteButtonStyle;
	    public List<AnimEventTrack> AnimEventTracks;
	
	    public AbilityScriptableObject SelectedAbilityObj;
	    public TimeLineHelper AnimClipHelper;
	
	    public float Width_Ability = 200;
	
	    public CombatPreviewController _previewer;
	
	    public bool PreviewNeedReload;
	
	    //Preview will reload after frame update.
	    public void RequirePreviewReload()
	    {
	        PreviewNeedReload = true;
	    }

	    private void OnEnable()
	    {
	        EditorSceneManager.activeSceneChangedInEditMode += ChangeScene;
	        AssemblyReloadEvents.afterAssemblyReload += AnimationBackToStart;

            AssemblyReloadEvents.beforeAssemblyReload += OnEndPreview;
	        UnityEditor.EditorApplication.playModeStateChanged += PlayModeStateChange;
	    }
	  
	    private void OnDisable()
	    {
            EditorSceneManager.activeSceneChangedInEditMode -= ChangeScene;

            AssemblyReloadEvents.beforeAssemblyReload -= OnEndPreview;

            AssemblyReloadEvents.afterAssemblyReload -= AnimationBackToStart;
	        UnityEditor.EditorApplication.playModeStateChanged -= PlayModeStateChange;
	    }
	


	    string LastSceneName = "";
	    public void ChangeScene(Scene s1, Scene s2)
	    {
	        if(LastSceneName!= s2.name)
	        {
	            ClearCombatController();
	        }
	        LastSceneName = s2.name;
	    }
	
	
	    public void PlayModeStateChange(UnityEditor.PlayModeStateChange state)
	    {
	        if(state == UnityEditor.PlayModeStateChange.ExitingEditMode)
	        {
	            var previewer = this._previewer;
	            if (previewer != null)
	            {
	                previewer.OnPlayModeStart();
	            }
	        }
	    }
	
	    [MenuItem("Tools/CombatEditor")]
	    public static void Init()
	    {
	        // Get existing open window or if none, make a new one:
	        CombatEditor window = (CombatEditor)EditorWindow.GetWindow(typeof(CombatEditor));
	        window.Show();
	        window.InitWindow();
	    }
	    public void InitWindow()
	    {
	        _previewer = new CombatPreviewController();
	           IsInited = true;
	        TopFrameThumbHelper = new TimeLineHelper(this);
	        InitRect();
	        InitSplitLine();
	    }

        static int SplitterIntervalDistance = 10;
	    public void InitRect()
	    {
	        L2Rect = new Rect(Width_Ability, 0, Width_TrackLabel - SplitterIntervalDistance, position.height);
	        L3TrackAvailableRect = new Rect(Width_Ability + Width_TrackLabel, Height_Top, AnimFrameCount * FrameIntervalDistance, position.height - Height_Top);
	    }
	    bool ReloadAfterStart;
	
	    private void OnGUI()
	    {
            //ConfigController
            if (!IsInited)
	        {
	            InitWindow();
	        }
	        InitGUIStyle();
	
	        // Handle keyboard shortcut events
	        HandleKeyboardShortcuts();
	        
	        // 验证轨道选择状态，防止条件编辑后轨道跳转
	        ValidateTrackSelection();

	        PaintL1();
	        PaintL2();
	        PaintL3();

	        PaintSplitLine();
	        PaintRenameField();


            Resetter();


	    }
	

        public void Resetter()
        {
            Event e = Event.current;
            if(e.type == EventType.KeyDown)
            {
                if(e.keyCode == KeyCode.B)
                {
                    Debug.Log("ClearPreviews?");
                    OnEndPreview();
                }
            }
        }

	    float StartPlayTime;
	
	
	    Vector2 InspectorScrollPos;
	    Color SelectedTrackColor => new Color(Color.green.r, Color.green.g, Color.green.b, 0.2f);
	
	    public Color HorizontalLineColor => Color.grey;
	   
	    Vector2 Scroll_Track;
	    public int SelectedTrackIndex;
	    Vector2 Scroll_Fields;
	   
	    bool IsInspectingAnimationConfig;
	    Object CurrentInspectedObj;
	    public GUIStyle HeaderStyle;
	
	    public Color SelectedColor => Color.yellow;
	    public Color OnInspectedColor => Color.green;
	
	    public enum InspectedType{ Null,AnimationConfig,Track,CombatConfig, PreviewConfig}
	    public InspectedType CurrentInspectedType;
	    //public CurrentInspectedAbility;
	
	    Editor CurrentAbilityEditor;
	    GUIStyle AbilityConfigStyle;
	
	    GUIStyle AbilityButtonStyle;
	    GUIStyle PopUpStyle;
	
	    public int CurrentSelectedAbilityIndex;
	
	    Vector2 AbilityScroll;
	    float Width_Scroll = 12;
	    
	    /// <summary>
	    /// Handle keyboard shortcuts for operations like undo/redo
	    /// </summary>
	    private void HandleKeyboardShortcuts()
	    {
	        Event e = Event.current;
	        
	        // 处理Ctrl+滚轮缩放时间轴
	        if (e.type == EventType.ScrollWheel && (e.control || e.command))
	        {
	            // 获取滚轮的垂直滚动值（向上滚动为负值，向下滚动为正值）
	            float scrollDelta = e.delta.y;
	            
	            // 计算缩放因子 - 将滚动值转换为缩放增量
	            float zoomDelta = scrollDelta * 0.03f; // 调整这个值来控制缩放速度
	            
	            // 保存鼠标当前位置相对于可视区域的时间点
	            Rect viewportRect = new Rect(L3TrackAvailableRect.x, L3TrackAvailableRect.y, 
	                                         position.width - Width_Inspector - L3TrackAvailableRect.x, 
	                                         position.height - L3TrackAvailableRect.y);
	            
	            // 检查鼠标是否在轨道查看区域内
	            if (viewportRect.Contains(e.mousePosition))
	            {
	                // 记录当前TimeLineScaler值
	                float oldScaler = TimeLineScaler;
	                
	                // 计算鼠标位置在轨道上的相对时间点
	                float mouseTimePosition = (e.mousePosition.x - L3TrackAvailableRect.x) / L3TrackAvailableRect.width;
	                
	                // 计算当前视口位置（水平滚动条位置）
	                float viewportPos = Scroll_Track.x / MaxWidth;
	                
	                // 应用缩放
	                TimeLineScaler = Mathf.Clamp(TimeLineScaler - zoomDelta, 0.4f, 1f);
	                
	                // 重新计算时间轴布局
	                InitRect();
	                
	                // 计算缩放前后的比例
	                float scaleFactor = oldScaler / TimeLineScaler;
	                
	                // 调整滚动位置以保持鼠标指针下的时间点位置不变
	                float newScrollX = (viewportPos + mouseTimePosition) * scaleFactor - mouseTimePosition;
	                newScrollX = Mathf.Clamp01(newScrollX) * MaxWidth;
	                
	                // 应用新的滚动位置
	                Scroll_Track = new Vector2(newScrollX, Scroll_Track.y);
	            }
	            else
	            {
	                // 如果鼠标不在轨道区域，直接应用缩放
	                TimeLineScaler = Mathf.Clamp(TimeLineScaler - zoomDelta, 0.4f, 1f);
	                InitRect();
	            }
	            
	            // 强制重绘窗口
	            Repaint();
	            
	            // 标记事件已处理
	            e.Use();
	        }
	        
	        if (e.type == EventType.KeyDown)
	        {
	            // Handle Ctrl+Z for Undo
	            if (e.keyCode == KeyCode.Z && e.control)
	            {
	                Undo.PerformUndo();
	                OnAnimEventChanges(); // Refresh views after undo
	                e.Use();
	            }
	            
	            // Handle Ctrl+Y (Windows) or Ctrl+Shift+Z (Mac) for Redo
	            if ((e.keyCode == KeyCode.Y && e.control) || 
	                (e.keyCode == KeyCode.Z && e.control && e.shift))
	            {
	                Undo.PerformRedo();
	                OnAnimEventChanges(); // Refresh views after redo
	                e.Use();
	            }
	        }
	    }
	}
	
}

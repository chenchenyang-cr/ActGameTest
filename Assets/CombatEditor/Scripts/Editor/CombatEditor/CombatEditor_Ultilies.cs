using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

 namespace CombatEditor
{	
	public partial class CombatEditor : EditorWindow
	{
	    private Texture2D MakeTex(int width, int height, Color col)
	    {
	        Color[] pix = new Color[width * height];
	        for (int i = 0; i < pix.Length; ++i)
	        {
	            pix[i] = col;
	        }
	        Texture2D result = new Texture2D(width, height);
	        result.SetPixels(pix);
	        result.Apply();
	        return result;
	    }
	
	    public void InitGUIStyle()
	    {
	        InitDeleteButtonStyle();
	        InitBoxGUIStyle();
	        InitHeaderStyle();
	    }
	
	    //public GUIStyle OnInspectedButtonStyle;
	    //public void InitOnInspectedButtonStyle()
	    //{
	    //    if(OnInspectedButtonStyle == null)
	    //    {
	    //        OnInspectedButtonStyle = new GUIStyle(GUI.skin.button);
	    //    }
	    //}
	    //public void InitOnSelectedButtonStyle()
	    //{
	
	    //}
	
	    public void HighlightBGIfInspectType(InspectedType type)
	    {
	        if (CurrentInspectedType == type)
	        {
	            GUI.backgroundColor = OnInspectedColor;
	        }
	        else
	        {
	            GUI.backgroundColor = SelectedColor;
	        }
	    }
	
	    public void InitDeleteButtonStyle()
	    {
	        if (MyDeleteButtonStyle == null)
	        {
	            MyDeleteButtonStyle = new GUIStyle(GUI.skin.button);
	            MyDeleteButtonStyle.margin = new RectOffset(0, 0, 0, 0);
	            MyDeleteButtonStyle.fontSize = 20;
	            MyDeleteButtonStyle.padding = new RectOffset(0,0,0,0);
	            MyDeleteButtonStyle.alignment = TextAnchor.MiddleCenter;
	            MyDeleteButtonStyle.fontStyle = FontStyle.Bold;
	            MyDeleteButtonStyle.contentOffset = new Vector2(0, 0);
	        }
	    }
	
	
	    public void InitBoxGUIStyle()
	    {
	        if (MyBoxGUIStyle == null)
	        {
	            MyBoxGUIStyle = new GUIStyle(GUI.skin.box);
	            MyBoxGUIStyle.normal.background = MakeTex(2, 2, Color.white);
	            MyBoxGUIStyle.border = new RectOffset(2, 2, 2, 2);
	        }
	    }
	
	
	    public void InitHeaderStyle()
	    {
	        if (HeaderStyle == null)
	        {
	            HeaderStyle = new GUIStyle(EditorStyles.helpBox);
	            HeaderStyle.alignment = GUI.skin.button.alignment;
	            HeaderStyle.fontSize = HeaderFontSize;
	            HeaderStyle.fontStyle = FontStyle.Bold;
	        }
	    }
	
	
	    bool IsPaintingRenameField;
	    Rect RenameFieldRect;
	    Rect RenameTargetRect;
	    string NameOfRename = "";
	
	
	    public void StartPaintRenameField(Rect TargetRect, string DefaultName, System.Action finishRenameAction)
	    {
	        FinishRenameAction = finishRenameAction;
	        NameOfRename = DefaultName;
	        RenameTargetRect = TargetRect;
	        Event e = Event.current;
	        //RenameFieldRect = new Rect(e.mousePosition.x, e.mousePosition.y, 200, 100);
	        RenameFieldRect = TargetRect;
	        //Vector2 StartPos = new Vector2(e.mousePosition.x, e.mousePosition.y);
	        IsPaintingRenameField = true;
	        PaintRenameField();
	
	        GUI.FocusControl("RenameField");
	        //Debug.Log(GUI.GetNameOfFocusedControl());
	    }
	    System.Action FinishRenameAction;
	
	    public void PaintRenameField()
	    {
	        //EditorGUI.DrawRect(RenameTargetRect,Color.green);
	        if (!IsPaintingRenameField)
	        {
	            return;
	        }
	
	        Event e = Event.current;
	        if (e.isKey && e.keyCode == KeyCode.Return)
	        {
	            StopRename();
	        }
	        if (e.isMouse)
	        {
	            if (!RenameFieldRect.Contains(e.mousePosition))
	            {
	                StopRename();
	            }
	        }
	        GUI.SetNextControlName("RenameField");
	
	        Rect InputRect = new Rect(RenameFieldRect.x, RenameFieldRect.y, RenameFieldRect.width, RenameFieldRect.height);
	        GUI.FocusControl("RenameField");
	
	        GUIStyle RenameStyle = EditorStyles.textField;
	        RenameStyle.alignment = TextAnchor.MiddleLeft;
	        NameOfRename = EditorGUI.TextField(InputRect, NameOfRename, RenameStyle);
	
	
	        //GUI.depth = 1;
	
	        //Repaint();
	    }
	
	   
	
	
	    public void StopRename()
	    {
	        IsPaintingRenameField = false;
	        if (FinishRenameAction != null)
	        {
	            FinishRenameAction.Invoke();
	        }
	    }
	    public static T[] GetAtPath<T>(string path)
	    {
	
	        ArrayList al = new ArrayList();
	
	        path = path.Remove(0, 6);
	        string[] fileEntries = Directory.GetFiles(Application.dataPath + "/" + path);
	        foreach (string fileName in fileEntries)
	        {
	            int index = fileName.LastIndexOf("/");
	            string localPath = "Assets/" + path;
	
	            if (index > 0)
	                localPath += fileName.Substring(index);
	            //Debug.Log(path);
	            Object t = AssetDatabase.LoadAssetAtPath(localPath, typeof(T));
	
	            if (t != null)
	                al.Add(t);
	        }
	        T[] result = new T[al.Count];
	        for (int i = 0; i < al.Count; i++)
	            result[i] = (T)al[i];
	
	        return result;
	    }
	
	    public void DrawHorizontalLine(Vector3 p1, Vector3 p2, Color color, float Width)
	    {
	        EditorGUI.DrawRect(new Rect(p1.x, p1.y - Width / 2, (p2 - p1).x, Width), color);
	    }
	    public void DrawVerticalLine(Vector3 p1, Vector3 p2, Color color, float Width)
	    {
	        EditorGUI.DrawRect(new Rect(p1.x - Width / 2, p1.y, Width, (p2 - p1).y), color);
	    }
	    public void UpdateAsset(Object obj)
	    {
	
	        EditorUtility.SetDirty(SelectedAbilityObj);
	        AssetDatabase.SaveAssets();
	        //AssetDatabase.Refresh();
	    }
	    
	
	    public void LoadL3()
	    {
	        AnimEventTracks = new List<AnimEventTrack>();
	        if (SelectedAbilityObj != null)
	        {
	            for (int i = 0; i < SelectedAbilityObj.events.Count; i++)
	            {
	                AnimEventTracks.Add(new AnimEventTrack(SelectedAbilityObj.events[i], this));
	            }
	            if (SelectedAbilityObj.Clip != null)
	            {
	                AnimFrameCount = (int)(SelectedAbilityObj.Clip.length * 60);
	            }
	            else
	            {
	                AnimFrameCount = 0;
	            }
	        }
	        InitRect();
	    }
	   
	}
	
	public static class CombatEditorUtility
	{
	    public static void ReloadAnimEvents()
	    {
	        
	        GetCurrentEditor().LoadL3();
	    }
	    public static CombatEditor GetCurrentEditor()
	    {
	        return EditorWindow.GetWindow<CombatEditor>(false,"",false);
	    }
	    public static bool EditorExist()
	    {
	        return EditorWindow.HasOpenInstances<CombatEditor>();
	    }
	
	    public static Rect ScaleRect(Rect rect,float Scale)
	    {
	        Rect RectAfterScale = new Rect
	            (rect.x + 0.5f* (rect.width - rect.width * Scale),
	            rect.y + 0.5f * (rect.height - rect.height * Scale),
	            rect.width * Scale,rect.height*Scale);
	        return RectAfterScale;
	    }
	    public static void DrawEditorTextureOnRect(Rect rect,float Scale, string name)
	    {
	        rect = CombatEditorUtility.ScaleRect(rect, Scale);
	        var texture = EditorGUIUtility.IconContent(name).image;
	        if(texture == null)
	        {
	            return;
	        }
	
	        texture.filterMode = FilterMode.Bilinear;
	        GUI.DrawTexture(rect, EditorGUIUtility.IconContent(name).image);
	    }
	
	
	}


    public class TimeLineHelper
    {
        bool IsDraggingthis;
        public EditorWindow TargetWindow;
        public TimeLineHelper(EditorWindow window)
        {
            TargetWindow = window;
        }
        public int DrawHorizontalDraggablePoint(int Value,
            int MaxValue,
            Rect rect,
            Color color,
            GUIStyle style,
            float Width = 5,
            bool LeftMouse = true,
            bool DrawPoint = true,
            bool DragStartOnMouseIn = false,
            System.Action<float> DragAction = null,
            System.Action FinishAction = null)
        {
            Event e = Event.current;
            float Percentage = (float)Value / (float)MaxValue;

            Rect PointRect = new Rect(rect.x + Percentage * rect.width - Width / 2, rect.y , Width, rect.height );
            int controlID = GUIUtility.GetControlID(FocusType.Passive);

            //Draw white background when selected.
            if (GUIUtility.hotControl == controlID)
            {
                //if (rect.Contains(e.mousePosition))
                //{
                    EditorGUI.DrawRect(rect, 0.5f * Color.white);
                //}
            }

            //GUI.depth = 1;
            if (DrawPoint)
            {
                GUI.Box(PointRect, "", style);
            }
            //GUI.depth = 0;

            int TargetMouseButton = LeftMouse ? 0 : 1;

            //Paint On Focus?
        
            switch (e.GetTypeForControl(controlID))
            {
                case (EventType.MouseDown):
                    if (e.button == TargetMouseButton)
                    {
                        if ((DragStartOnMouseIn && PointRect.Contains(e.mousePosition)) || (!DragStartOnMouseIn && rect.Contains(e.mousePosition)))
                        {
                            GUIUtility.hotControl = controlID;
                            e.Use();
                        }

                    }
                    break;
                case (EventType.MouseDrag):
                    {
                        if (GUIUtility.hotControl == controlID && e.button == TargetMouseButton)
                        {
                            Percentage = ((e.mousePosition.x - rect.x) / rect.width);
                            Percentage = Mathf.Clamp(Percentage, 0, 1);
                            Value = Mathf.RoundToInt(Percentage * MaxValue);
                            Percentage = (float)Value / (float)MaxValue;
                            PointRect = new Rect(rect.x + Percentage * rect.width - Width / 2, rect.y, Width, rect.height);
                            if (DrawPoint)
                            {
                                EditorGUI.DrawRect(PointRect, color);
                            }
                            if (DragAction != null)
                            {
                                DragAction(Percentage);
                            }
                            TargetWindow.Repaint();
                        }
                    }
                    break;
                case (EventType.MouseUp):
                    {
                        if (e.button == TargetMouseButton)
                        {
                            if (GUIUtility.hotControl == controlID)
                            //if (IsDraggingthis)
                            {
                                GUIUtility.hotControl = 0;
                                if (FinishAction != null)
                                {
                                    FinishAction.Invoke();
                                }
                            }
                            //IsDraggingthis = false;
                            
                        }
                    }
                    break;
            }


            EditorGUIUtility.AddCursorRect(PointRect, MouseCursor.SlideArrow, controlID);
            return Value;
        }

        public int DrawEnhancedDraggablePoint(int Value,
            int MaxValue,
            Rect rect,
            Color color,
            GUIStyle style,
            float Width = 5,
            bool LeftMouse = true,
            bool DrawPoint = true,
            bool DragStartOnMouseIn = false,
            System.Action<float> DragAction = null,
            System.Action FinishAction = null)
        {
            Event e = Event.current;
            float Percentage = (float)Value / (float)MaxValue;

            // Define an enhanced point rect that extends slightly to make it easier to drag
            Rect PointRect = new Rect(rect.x + Percentage * rect.width - Width / 2, rect.y, Width, rect.height);
            
            // Define an area around the point that is more easily clicked
            Rect ClickableArea = new Rect(PointRect.x - Width, PointRect.y, Width * 3, PointRect.height);
            
            int controlID = GUIUtility.GetControlID(FocusType.Passive);

            // Draw white background when selected
            if (GUIUtility.hotControl == controlID)
            {
                EditorGUI.DrawRect(rect, 0.5f * Color.white);
            }

            // Draw the point
            if (DrawPoint)
            {
                GUI.Box(PointRect, "", style);
            }

            int TargetMouseButton = LeftMouse ? 0 : 1;
            
            switch (e.GetTypeForControl(controlID))
            {
                case (EventType.MouseDown):
                    if (e.button == TargetMouseButton)
                    {
                        // Using the clickable area that is larger than the visible point for better UX
                        if ((DragStartOnMouseIn && ClickableArea.Contains(e.mousePosition)) || (!DragStartOnMouseIn && rect.Contains(e.mousePosition)))
                        {
                            GUIUtility.hotControl = controlID;
                            e.Use();
                        }
                    }
                    break;
                case (EventType.MouseDrag):
                    {
                        if (GUIUtility.hotControl == controlID && e.button == TargetMouseButton)
                        {
                            Percentage = ((e.mousePosition.x - rect.x) / rect.width);
                            Percentage = Mathf.Clamp(Percentage, 0, 1);
                            Value = Mathf.RoundToInt(Percentage * MaxValue);
                            Percentage = (float)Value / (float)MaxValue;
                            PointRect = new Rect(rect.x + Percentage * rect.width - Width / 2, rect.y, Width, rect.height);
                            ClickableArea = new Rect(PointRect.x - Width, PointRect.y, Width * 3, PointRect.height);
                            
                            if (DrawPoint)
                            {
                                EditorGUI.DrawRect(PointRect, color);
                            }
                            if (DragAction != null)
                            {
                                DragAction(Percentage);
                            }
                            TargetWindow.Repaint();
                        }
                    }
                    break;
                case (EventType.MouseUp):
                    {
                        if (e.button == TargetMouseButton)
                        {
                            if (GUIUtility.hotControl == controlID)
                            {
                                GUIUtility.hotControl = 0;
                                if (FinishAction != null)
                                {
                                    FinishAction.Invoke();
                                }
                            }
                        }
                    }
                    break;
            }

            // Show drag cursor for both visible point and clickable area
            EditorGUIUtility.AddCursorRect(ClickableArea, MouseCursor.MoveArrow, controlID);
            return Value;
        }

        public int[] DrawHorizontalDraggableRange(int Value1, int Value2, int MaxValue, Rect rect, Color color, GUIStyle boxStyle, float Width = 5, System.Action FinishDragAction = null)
        {
            if (IsDraggingthis)
            {
            }
            //Right handle using left mouse, must start from right handle 
            int RightValue = DrawHorizontalDraggablePoint(Value2, MaxValue, rect, color, boxStyle, Width, true, false, true, null, FinishDragAction);
            //Right handle using right mouse, can start from anywhere in rect.
            RightValue = DrawHorizontalDraggablePoint(RightValue, MaxValue, rect, color, boxStyle, Width, false, false, false, null, FinishDragAction);

            int LeftValue = DrawHorizontalDraggablePoint(Value1, MaxValue, rect, color, boxStyle, Width, true, false, false, null, FinishDragAction);


            float Percentage1 = (float)Value1 / (float)MaxValue;
            float Percentage2 = (float)Value2 / (float)MaxValue;

            Color defaultColor = GUI.color;
            GUI.color = color;

            Rect TargetRect = new Rect(rect.x + Percentage1 * rect.width, rect.y, (Percentage2 - Percentage1) * rect.width, rect.height);
            if (Percentage2 > Percentage1)
            {
                GUI.Box(TargetRect, "", boxStyle);
            }
            else
            {
                GUI.Box(TargetRect, "", boxStyle);
            }
            GUI.color = defaultColor;
            return new int[] { LeftValue, RightValue };
        }

        public int[] DrawHorizontalDraggableRangeWithMovement(int Value1, int Value2, int MaxValue, Rect rect, Color color, GUIStyle boxStyle, float Width = 5, System.Action FinishDragAction = null)
        {
            if (IsDraggingthis)
            {
            }
            
            // Calculate the range width first - we need to preserve this
            int rangeWidth = Value2 - Value1;
            
            float Percentage1 = (float)Value1 / (float)MaxValue;
            float Percentage2 = (float)Value2 / (float)MaxValue;
            
            // Create the range rect
            Rect TargetRect = new Rect(rect.x + Percentage1 * rect.width, rect.y, (Percentage2 - Percentage1) * rect.width, rect.height);
            
            // Create control IDs for the different interaction areas
            int controlID_MoveWhole = GUIUtility.GetControlID(FocusType.Passive); // For moving the whole track
            int controlID_Left = GUIUtility.GetControlID(FocusType.Passive); // For left edge resizing
            int controlID_Right = GUIUtility.GetControlID(FocusType.Passive); // For right edge resizing
            
            Event e = Event.current;
            
            // Calculate edge rects
            Rect leftEdgeRect = new Rect(rect.x + Percentage1 * rect.width - Width/2, rect.y, Width, rect.height);
            Rect rightEdgeRect = new Rect(rect.x + Percentage2 * rect.width - Width/2, rect.y, Width, rect.height);
            
            // Define the drag area (exclude the edges from the drag area)
            Rect dragAreaRect = new Rect(
                leftEdgeRect.x + Width,
                TargetRect.y,
                rightEdgeRect.x - leftEdgeRect.x - Width,
                TargetRect.height
            );
            
            // Add cursor rects for better UX - should be added before drawing to ensure correct cursor display
            EditorGUIUtility.AddCursorRect(leftEdgeRect, MouseCursor.ResizeHorizontal);
            EditorGUIUtility.AddCursorRect(rightEdgeRect, MouseCursor.ResizeHorizontal);
            EditorGUIUtility.AddCursorRect(dragAreaRect, MouseCursor.MoveArrow);
            
            // Handle mouse events
            bool isResizing = false;
            
            // First check for mouse down to capture the start of the interaction
            if (e.type == EventType.MouseDown && e.button == 0)
            {
                if (leftEdgeRect.Contains(e.mousePosition))
                {
                    GUIUtility.hotControl = controlID_Left;
                    isResizing = true;
                    e.Use();
                }
                else if (rightEdgeRect.Contains(e.mousePosition))
                {
                    GUIUtility.hotControl = controlID_Right;
                    isResizing = true;
                    e.Use();
                }
                else if (dragAreaRect.Contains(e.mousePosition))
                {
                    GUIUtility.hotControl = controlID_MoveWhole;
                    e.Use();
                }
            }
            
            // Handle mouse drag events
            if (e.type == EventType.MouseDrag)
            {
                if (GUIUtility.hotControl == controlID_MoveWhole)
                {
                    // 直接使用鼠标位置计算，而不是使用delta
                    float newPos = (e.mousePosition.x - rect.x - (TargetRect.width / 2)) / rect.width;
                    newPos = Mathf.Clamp(newPos, 0, 1 - (Percentage2 - Percentage1));
                    
                    Value1 = Mathf.RoundToInt(newPos * MaxValue);
                    Value2 = Value1 + rangeWidth;
                    
                    TargetWindow.Repaint();
                    e.Use();
                }
                else if (GUIUtility.hotControl == controlID_Left)
                {
                    // Left handle dragging logic (resize from left)
                    float newPercentage = ((e.mousePosition.x - rect.x) / rect.width);
                    // 计算单帧对应的百分比值
                    float singleFramePercentage = 1f / MaxValue;
                    newPercentage = Mathf.Clamp(newPercentage, 0, Percentage2 - singleFramePercentage); // 允许缩小到单帧
                    Value1 = Mathf.RoundToInt(newPercentage * MaxValue);
                    
                    TargetWindow.Repaint();
                    e.Use();
                }
                else if (GUIUtility.hotControl == controlID_Right)
                {
                    // Right handle dragging logic (resize from right)
                    float newPercentage = ((e.mousePosition.x - rect.x) / rect.width);
                    // 计算单帧对应的百分比值
                    float singleFramePercentage = 1f / MaxValue;
                    newPercentage = Mathf.Clamp(newPercentage, Percentage1 + singleFramePercentage, 1); // 允许缩小到单帧
                    Value2 = Mathf.RoundToInt(newPercentage * MaxValue);
                    
                    TargetWindow.Repaint();
                    e.Use();
                }
            }
            
            // Handle mouse up to end interaction
            if (e.type == EventType.MouseUp && e.button == 0)
            {
                if (GUIUtility.hotControl == controlID_MoveWhole || 
                    GUIUtility.hotControl == controlID_Left || 
                    GUIUtility.hotControl == controlID_Right)
                {
                    GUIUtility.hotControl = 0;
                    if (FinishDragAction != null)
                    {
                        FinishDragAction.Invoke();
                    }
                    e.Use();
                }
            }
            
            // Update percentages after potential dragging
            Percentage1 = (float)Value1 / (float)MaxValue;
            Percentage2 = (float)Value2 / (float)MaxValue;
            
            // Draw the box
            Color defaultColor = GUI.color;
            GUI.color = color;
            TargetRect = new Rect(rect.x + Percentage1 * rect.width, rect.y, (Percentage2 - Percentage1) * rect.width, rect.height);
            
            // Draw the track box
            if (Percentage2 > Percentage1)
            {
                GUI.Box(TargetRect, "", boxStyle);
            }
            else
            {
                GUI.Box(TargetRect, "", boxStyle);
            }
            
            GUI.color = defaultColor;
            
            return new int[] { Value1, Value2 };
        }

        //int BoxEdgeWidth = 1;
        //static Color[] MultiColors= new Color{Color.blue,Color.cyan};
        
        public int[] DrawHorizontalMultiDraggable(int[] Values,string[] Names, int MaxValue, Rect rect, Color color, float Width = 5, System.Action FinishDragAction = null)
        {
            int[] ModifiedValue = Values;
          
            Color defaultColor = GUI.color;
            GUI.color = color;

            float[] VisiableValue = new float[Values.Length + 2];
            VisiableValue[0] = 0;
            VisiableValue[Values.Length + 1] = 1;
            for(int i =0;i<Values.Length;i++)
            {
                VisiableValue[i + 1] = (float)Values[i] / (float)MaxValue;
            }
            for (int i = 0; i < Values.Length; i++)
            {
                ModifiedValue[i] = DrawHorizontalDraggablePoint(Values[i], MaxValue, rect, color, "flow node " + 4, Width, true, false, true, null, FinishDragAction);
            }
            for (int i = 0; i < VisiableValue.Length - 1; i++)
            {
                Rect TargetRect = new Rect(rect.x + VisiableValue[i] * rect.width + 1, rect.y, (VisiableValue[i + 1] - VisiableValue[i]) * rect.width - 2 , rect.height);
                //Rect InnerRect = new Rect(TargetRect.x + BoxEdgeWidth, TargetRect.y + BoxEdgeWidth, TargetRect.width - 2 * BoxEdgeWidth, TargetRect.height - 2 * BoxEdgeWidth);

                if (Names[i] != "" && Names[i]!=null)
                {
                    GUI.Box(TargetRect, Names[i], "flow node " + 5);
                }
                else
                {
                    GUI.Box(TargetRect, Names[i], "flow node " + 0);
                }
               
            }
          


            return ModifiedValue;
        }

        public int[] DrawHorizontalMultiDraggableWithMovement(int[] Values, string[] Names, int MaxValue, Rect rect, Color color, float Width = 5, System.Action FinishDragAction = null)
        {
            // Create a copy to avoid modifying the original array
            int[] ModifiedValue = new int[Values.Length];
            for (int i = 0; i < Values.Length; i++)
            {
                ModifiedValue[i] = Values[i];
            }
            
            // Store original spacings between frames to preserve them when dragging
            int[] spacings = new int[Values.Length - 1];
            for (int i = 0; i < spacings.Length; i++)
            {
                spacings[i] = Values[i+1] - Values[i];
            }
            
            // Calculate the total track width
            int totalWidth = Values[Values.Length - 1] - Values[0];
            
            Color defaultColor = GUI.color;
            GUI.color = color;

            // Prepare visible values
            float[] VisiableValue = new float[Values.Length + 2];
            VisiableValue[0] = 0;
            VisiableValue[Values.Length + 1] = 1;
            for (int i = 0; i < Values.Length; i++)
            {
                VisiableValue[i + 1] = (float)Values[i] / (float)MaxValue;
            }
            
            // Create control IDs 
            int controlID_MoveWhole = GUIUtility.GetControlID(FocusType.Passive);
            int[] controlID_Points = new int[Values.Length];
            for (int i = 0; i < Values.Length; i++)
            {
                controlID_Points[i] = GUIUtility.GetControlID(FocusType.Passive);
            }
            
            Event e = Event.current;
            
            // Calculate point rects for resize handles
            Rect[] pointRects = new Rect[Values.Length];
            for (int i = 0; i < Values.Length; i++)
            {
                float percentage = (float)Values[i] / (float)MaxValue;
                pointRects[i] = new Rect(rect.x + percentage * rect.width - Width/2, rect.y, Width, rect.height);
                
                // Add resize cursor to points
                EditorGUIUtility.AddCursorRect(pointRects[i], MouseCursor.ResizeHorizontal);
            }
            
            // Calculate segment rects for dragging
            Rect[] segmentRects = new Rect[Names.Length];
            for (int i = 0; i < segmentRects.Length; i++)
            {
                float startX = (i == 0) ? rect.x : rect.x + VisiableValue[i] * rect.width;
                float endX = (i == segmentRects.Length - 1) ? rect.x + rect.width : rect.x + VisiableValue[i + 1] * rect.width;
                
                if (i > 0 && i < segmentRects.Length - 1)
                {
                    // For middle segments, exclude the point handles
                    startX += Width/2;
                    endX -= Width/2;
                }
                else if (i == 0)
                {
                    // First segment only excludes the right handle
                    endX -= Width/2;
                }
                else if (i == segmentRects.Length - 1)
                {
                    // Last segment only excludes the left handle
                    startX += Width/2;
                }
                
                segmentRects[i] = new Rect(
                    startX,
                    rect.y,
                    endX - startX,
                    rect.height
                );
                
                // Add move cursor to segments
                EditorGUIUtility.AddCursorRect(segmentRects[i], MouseCursor.MoveArrow);
            }
            
            // Handle mouse events
            bool isDraggingPoint = false;
            int draggingPointIndex = -1;
            
            // First check for mouse down to capture the start of the interaction
            if (e.type == EventType.MouseDown && e.button == 0)
            {
                // Check if clicking on a point handle
                for (int i = 0; i < pointRects.Length; i++)
                {
                    if (pointRects[i].Contains(e.mousePosition))
                    {
                        GUIUtility.hotControl = controlID_Points[i];
                        isDraggingPoint = true;
                        draggingPointIndex = i;
                        e.Use();
                        break;
                    }
                }
                
                // If not clicking on a point handle, check if clicking in a segment for whole track dragging
                if (!isDraggingPoint)
                {
                    for (int i = 0; i < segmentRects.Length; i++)
                    {
                        if (segmentRects[i].Contains(e.mousePosition))
                        {
                            GUIUtility.hotControl = controlID_MoveWhole;
                            e.Use();
                            break;
                        }
                    }
                }
            }
            
            // Handle mouse drag events
            if (e.type == EventType.MouseDrag)
            {
                if (GUIUtility.hotControl == controlID_MoveWhole)
                {
                    // 直接使用鼠标位置计算新位置，而不是增量
                    float totalPercentageWidth = (float)totalWidth / MaxValue;
                    
                    // 计算第一个点应该在的位置（鼠标位置减去半个轨道宽度）
                    float centerPoint = Values[0] + (totalWidth / 2);
                    float centerPercentage = (float)centerPoint / MaxValue;
                    
                    // 取得鼠标位置对应的百分比
                    float mousePercentage = (e.mousePosition.x - rect.x) / rect.width;
                    
                    // 计算新的起始位置
                    float newStartPercentage = mousePercentage - (totalPercentageWidth / 2);
                    
                    // 限制范围
                    newStartPercentage = Mathf.Clamp(newStartPercentage, 0, 1 - totalPercentageWidth);
                    
                    // 应用到第一个点
                    int newStart = Mathf.RoundToInt(newStartPercentage * MaxValue);
                    
                    // 更新所有点位置，保持间距不变
                    ModifiedValue[0] = newStart;
                    for (int i = 1; i < ModifiedValue.Length; i++)
                    {
                        ModifiedValue[i] = ModifiedValue[0];
                        for (int j = 0; j < i; j++)
                        {
                            ModifiedValue[i] += spacings[j];
                        }
                    }
                    
                    TargetWindow.Repaint();
                    e.Use();
                }
                else
                {
                    // Check if dragging a specific point
                    for (int i = 0; i < controlID_Points.Length; i++)
                    {
                        if (GUIUtility.hotControl == controlID_Points[i])
                        {
                            // Calculate new position for this point directly from mouse position
                            float mousePercentage = (e.mousePosition.x - rect.x) / rect.width;
                            
                            // Constrain movement based on neighboring points
                            if (i > 0)
                            {
                                // Don't let it go below the previous point
                                float minPercentage = (float)ModifiedValue[i-1] / MaxValue + 0.01f;
                                mousePercentage = Mathf.Max(mousePercentage, minPercentage);
                            }
                            else 
                            {
                                // First point can't go below 0
                                mousePercentage = Mathf.Max(mousePercentage, 0);
                            }
                            
                            if (i < ModifiedValue.Length - 1)
                            {
                                // Don't let it go above the next point
                                float maxPercentage = (float)ModifiedValue[i+1] / MaxValue - 0.01f;
                                mousePercentage = Mathf.Min(mousePercentage, maxPercentage);
                            } 
                            else
                            {
                                // Last point can't exceed 1
                                mousePercentage = Mathf.Min(mousePercentage, 1);
                            }
                            
                            // Update the point value
                            ModifiedValue[i] = Mathf.RoundToInt(mousePercentage * MaxValue);
                            
                            // Update spacings if we're dragging the first or last point to maintain track shape
                            if (i == 0 || i == ModifiedValue.Length - 1)
                            {
                                // Recalculate spacings
                                for (int j = 0; j < spacings.Length; j++)
                                {
                                    spacings[j] = ModifiedValue[j+1] - ModifiedValue[j];
                                }
                            }
                            
                            TargetWindow.Repaint();
                            e.Use();
                            break;
                        }
                    }
                }
            }
            
            // Handle mouse up to end interaction
            if (e.type == EventType.MouseUp && e.button == 0)
            {
                if (GUIUtility.hotControl == controlID_MoveWhole)
                {
                    GUIUtility.hotControl = 0;
                    if (FinishDragAction != null)
                    {
                        FinishDragAction.Invoke();
                    }
                    e.Use();
                }
                else
                {
                    for (int i = 0; i < controlID_Points.Length; i++)
                    {
                        if (GUIUtility.hotControl == controlID_Points[i])
                        {
                            GUIUtility.hotControl = 0;
                            if (FinishDragAction != null)
                            {
                                FinishDragAction.Invoke();
                            }
                            e.Use();
                            break;
                        }
                    }
                }
            }
            
            // 更新可见值以匹配当前修改值
            for (int i = 0; i < ModifiedValue.Length; i++)
            {
                VisiableValue[i + 1] = (float)ModifiedValue[i] / (float)MaxValue;
            }
            
            // Draw the segments
            for (int i = 0; i < VisiableValue.Length - 1; i++)
            {
                Rect TargetRect = new Rect(rect.x + VisiableValue[i] * rect.width + 1, rect.y, (VisiableValue[i + 1] - VisiableValue[i]) * rect.width - 2, rect.height);
                
                if (Names[i] != "" && Names[i] != null)
                {
                    GUI.Box(TargetRect, Names[i], "flow node " + 5);
                }
                else
                {
                    GUI.Box(TargetRect, Names[i], "flow node " + 0);
                }
            }
            
            // Draw the point handles
            for (int i = 0; i < ModifiedValue.Length; i++)
            {
                float percentage = (float)ModifiedValue[i] / (float)MaxValue;
                Rect pointRect = new Rect(rect.x + percentage * rect.width - Width/2, rect.y, Width, rect.height);
                
                // Draw the handle point
                GUI.Box(pointRect, "", "flow node " + 4);
            }
            
            GUI.color = defaultColor;
            return ModifiedValue;
        }



	
	    public float DrawSplitLine(float X, float width, float MinX, float MaxX)
	    {
	        //DrawVerticalLine();
	        float Percentage = X / TargetWindow.position.width;
	
	        //TriggerField
	        Rect DraggableStartField = new Rect(X - 8, 0, 16, TargetWindow.position.height);
	        //GUI.Box(DraggableStartField,"LineTrigger");
	        Event e = Event.current;
	        Rect rect = new Rect(0, 0, TargetWindow.position.width, TargetWindow.position.height);
	        Rect TargetRect = new Rect(rect.x + Percentage * rect.width, rect.y, width, rect.height);
	        EditorGUI.DrawRect(TargetRect, Color.grey);
	        int controlID = GUIUtility.GetControlID(FocusType.Passive);
            EditorGUIUtility.AddCursorRect(DraggableStartField, MouseCursor.SlideArrow, controlID);


            if (e.GetTypeForControl(controlID) == EventType.MouseDown)
	        {
	            if (e.button == 0)
	            {
	                if (DraggableStartField.Contains(e.mousePosition))
	                {
                        GUIUtility.hotControl = controlID;
                        e.Use();
                    }
	            }
	        }
	        if (e.GetTypeForControl(controlID) == EventType.MouseDrag)
	        {
                if (GUIUtility.hotControl == controlID)
                {
                    Percentage = ((e.mousePosition.x - rect.x) / rect.width);
                    Percentage = Mathf.Clamp(Percentage, MinX / rect.width, MaxX / rect.width);
                    TargetRect = new Rect(rect.x + Percentage * rect.width, rect.y, 10, rect.height);
                    EditorGUI.DrawRect(TargetRect, Color.grey);
                    TargetWindow.Repaint();
                }
            }
            
            return Percentage * TargetWindow.position.width;
        }
	
	 
	}
}

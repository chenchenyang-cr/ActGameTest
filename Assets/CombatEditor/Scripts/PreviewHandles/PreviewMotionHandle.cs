using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

#if UNITY_EDITOR
	
 namespace CombatEditor {	
	public class PreviewMotionHandle : PreviewerOnObject
	{
	    public Transform TargetTrans;
	    public  MotionTarget target;
	    public Vector3 StartPosition;
	    public override void SelfDestroy()
	    {
	        if(_combatController!=null)
	        {
	            _combatController.transform.position = StartPosition;
	        }
	        base.SelfDestroy();
	    }
	
	    public override void PaintHandle()
	    {
	        if (!_preview.eve.Previewable)
	        {
	            UpdateSelfTransByData();
	            return;
	        }
	        if (Selection.activeObject == gameObject)
	        {
	            Tools.hidden = true;
	        }
	        else
	        {
	            Tools.hidden = false;
	        }
	
	        EditorGUI.BeginChangeCheck();
	        var PosAfterMove = Handles.PositionHandle(TargetTrans.position, TargetTrans.rotation);
	        if (EditorGUI.EndChangeCheck())
	        {
	            TargetTrans.position = PosAfterMove;
	
	            SetTransformDataToEventObj(TargetTrans);
	        }
	
	        UpdateSelfTransByData();
	
	        Handles.SphereHandleCap(0, StartFramePos, Quaternion.identity, 0.1f, EventType.Repaint);
	        Handles.DrawLine(StartFramePos, TargetTrans.position);
	
        // 显示移动模式信息
        string modeInfo = "";
        Vector3 calculatedTarget = target.CalculateTargetPosition(_combatController, StartFramePos);
        
        if (target.UseAbsoluteCoordinates)
        {
            modeInfo = $"绝对坐标增量\n偏移: {target.Offset}\n目标位置: {calculatedTarget}";
        }
        else
        {
            modeInfo = $"本地坐标系\n偏移: {target.Offset}\n目标位置: {calculatedTarget}";
        }
        
        Handles.Label(TargetTrans.position + Vector3.up * 0.5f, modeInfo);
	
	    }
	
	    public void SetTransformDataToEventObj(Transform PreviewTransform)
	    {
	        var StartFrameOffset = PreviewTransform.position - StartFramePos;
        
        if (target.UseAbsoluteCoordinates)
        {
            // 绝对坐标增量值：直接使用世界坐标偏移量
            target.Offset = StartFrameOffset;
        }
        else
        {
            // 本地坐标系：转换为本地偏移量（原有行为）
	        target.Offset = Quaternion.Inverse(StartFrameRot) * StartFrameOffset;
        }
	    }
	
	
	    Vector3 StartFramePos;
	    Quaternion StartFrameRot;
	    Quaternion StartAnimatorRot;
	    public void SetStartFramePos(Vector3 pos, Quaternion rot, Quaternion AnimatorRot)
	    {
	        StartFramePos = pos;
	        StartFrameRot = rot;
	        StartAnimatorRot = AnimatorRot;
	    }
	    public void UpdateSelfTransByData()
	    {
	        Vector3 TargetPos = Vector3.zero;
	        Quaternion TargetRot = Quaternion.identity;
        
        // 计算锁定的目标位置（模拟运行时的逻辑）
        TargetPos = target.CalculateTargetPosition(_combatController, StartFramePos);
	        TargetRot = StartAnimatorRot;
	
	        TargetTrans.position = TargetPos;
	        TargetTrans.rotation = StartAnimatorRot;
	    }
	
	}
}
#endif

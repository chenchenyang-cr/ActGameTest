using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

 namespace CombatEditor
{
    
	//DisaplayHandles and control Object
	public class PreviewTransformHandle : PreviewerOnObject
	{
	    public InsedObject InsObjData;
	    public enum ControlTypeEnum { Translation, Rotation, Scale };
	    public ControlTypeEnum ControlType;
	
	    public Transform TargetTrans;
	
#if UNITY_EDITOR
	
	    public Action<Transform> ModifyTrans;
	
	    Vector3 StartFramePos;
	    Quaternion StartFrameRot;
	    Quaternion StartAnimatorRot;
	
	    public bool Previewable = false;
	
	    public void SetStartFramePos(Vector3 pos, Quaternion rot , Quaternion AnimatorRot)
	    {
	        StartFramePos = pos;
	        StartFrameRot = rot;
	        StartAnimatorRot = AnimatorRot;
	    }
	    public override void UpdateHiddenHandle()
	    {
	        UpdateSelfTransByData();
	    }
	    public override void PaintHandle()
	    {
	        if (TargetTrans == null) return;
	        ControlType = InsObjData.controlType;
	        UpdateTransformData();
	        var position = TargetTrans.position;
	        var rotation = TargetTrans.rotation;
	        var scale = TargetTrans.localScale;
	
	        if (ControlType == ControlTypeEnum.Translation)
	        {
	            EditorGUI.BeginChangeCheck();
	            var newPosition = Handles.PositionHandle(position, rotation);
	            if (EditorGUI.EndChangeCheck())
	            {
	                Undo.RecordObject(TargetTrans, "Move Preview Object");
	                TargetTrans.position = newPosition;
	                SetTransformDataToEventObj(TargetTrans);
	            }
	
	        }
	        if (ControlType == ControlTypeEnum.Rotation)
	        {
	            EditorGUI.BeginChangeCheck();
	            Handles.color = Color.green;
	            var newRotation = Handles.RotationHandle(rotation, position);
	            if (EditorGUI.EndChangeCheck())
	            {
	                Undo.RecordObject(TargetTrans, "Rotate Preview Object");
	                TargetTrans.rotation = newRotation;
	
	                SetTransformDataToEventObj(TargetTrans);
	            }
	        }
	        if (ControlType == ControlTypeEnum.Scale)
	        {
	            EditorGUI.BeginChangeCheck();
	            var newScale = Handles.ScaleHandle(scale, position, rotation, 1);
	            if (EditorGUI.EndChangeCheck())
	            {
	                Undo.RecordObject(TargetTrans, "Scale Preview Object");
	                TargetTrans.localScale = newScale;
	                SetTransformDataToEventObj(TargetTrans);
	            }
	        }
	    }
	    public override void UpdateTransformData()
	    {
	        UpdateSelfTransByData();
	    }
	
	
	    public void UpdateSelfTransByData()
	    {

	        Vector3 TargetPos = Vector3.zero;
	        Quaternion TargetRot = Quaternion.identity;


	        //If static, position and rotation is based on StartFrame.
	        //Is !FollowRot, rot by AnimatorFront, else rot by joint rotation.
	        if (!InsObjData.FollowNode)
	        {
	            TargetPos = StartFramePos + StartFrameRot * InsObjData.Offset;
	            if (InsObjData.RotateByNode)
	            {
	                TargetRot = StartFrameRot * InsObjData.Rot;
	            }
	            if (!InsObjData.RotateByNode)
	            {
	                TargetRot = StartAnimatorRot * InsObjData.Rot;
	            }
	        }
	        //If not static, position and rotation is based on currentFrame.
	        //Is !FollowRot, rot by AnimatorFront, else rot by joint rotation.
	        else
	        {
	            //Need To Add RootMotion cause root motion dont move the animator in editor mode
	            Transform trans = null;
	            
	            if (InsObjData.UseCustomTarget && !string.IsNullOrEmpty(InsObjData.CustomTargetName))
	            {
	                // Find target by name in the scene
	                GameObject targetGameObject = GameObject.Find(InsObjData.CustomTargetName);
	                if (targetGameObject != null)
	                {
	                    trans = targetGameObject.transform;
	                }
	                else
	                {
	                    Debug.LogWarning($"Preview: Custom target '{InsObjData.CustomTargetName}' not found in scene, using default node");
	                    trans = _combatController.GetNodeTranform(InsObjData.TargetNode);
	                }
	            }
	            else
	            {
	                trans = _combatController.GetNodeTranform(InsObjData.TargetNode);
	            }
	            
	            Vector3 NodePos = trans.position;
	            TargetPos = NodePos + trans.rotation * InsObjData.Offset;
	            if (!InsObjData.UseCustomTarget && InsObjData.TargetNode == CharacterNode.NodeType.Animator)
	            {
	                TargetPos += trans.rotation * CombatGlobalEditorValue.CurrentMotionTAtGround;
	            }

	            if (InsObjData.RotateByNode)
	            {
	                TargetRot = trans.rotation * InsObjData.Rot;
	            }

	            if (!InsObjData.RotateByNode)
	            {
	                TargetRot = _combatController.GetNodeTranform(CharacterNode.NodeType.Animator).rotation * InsObjData.Rot;
	            }
	        }
	        TargetTrans.position = TargetPos;
	        TargetTrans.rotation = TargetRot;
	    }
	
	
	    public void SetTransformDataToEventObj(Transform PreviewTransform)
	    {
	        SetOffset(PreviewTransform);
	        SetRot(PreviewTransform);
	        SetScale(PreviewTransform);
	    }
	
	    public void SetOffset(Transform PreviewTransform)
	    {
	        if (!InsObjData.FollowNode)
	        {
	            var StartFrameOffset = PreviewTransform.position - StartFramePos;
	            InsObjData.Offset = Quaternion.Inverse(StartFrameRot) * StartFrameOffset;
	        }
	        else
	        {
	            Transform trans = null;
	            
	            if (InsObjData.UseCustomTarget && !string.IsNullOrEmpty(InsObjData.CustomTargetName))
	            {
	                // Find target by name in the scene
	                GameObject targetGameObject = GameObject.Find(InsObjData.CustomTargetName);
	                if (targetGameObject != null)
	                {
	                    trans = targetGameObject.transform;
	                }
	                else
	                {
	                    trans = _combatController.GetNodeTranform(InsObjData.TargetNode);
	                }
	            }
	            else
	            {
	                trans = _combatController.GetNodeTranform(InsObjData.TargetNode);
	            }
	            
	            Vector3 OffsetWithRotation = PreviewTransform.position - trans.position;
	            if (!InsObjData.UseCustomTarget && InsObjData.TargetNode == CharacterNode.NodeType.Animator)
	            {
	                OffsetWithRotation -= trans.rotation * CombatGlobalEditorValue.CurrentMotionTAtGround;
	            }
	            InsObjData.Offset = Quaternion.Inverse(trans.rotation) * OffsetWithRotation;
	        }
	    }
	    public void SetRot(Transform PreviewTransform)
	    {
	        var AnimatorRot = _combatController._animator.transform.rotation;


	        if (!InsObjData.FollowNode)
	        {

	            if (InsObjData.RotateByNode)
	            {
	                InsObjData.Rot = Quaternion.Inverse(StartFrameRot) * PreviewTransform.rotation;
	            }
	            if (!InsObjData.RotateByNode)
	            {
	                InsObjData.Rot = Quaternion.Inverse(StartAnimatorRot) * PreviewTransform.rotation;
	            }
	        }
	        else
	        {
	            Transform trans = null;
	            
	            if (InsObjData.UseCustomTarget && !string.IsNullOrEmpty(InsObjData.CustomTargetName))
	            {
	                // Find target by name in the scene
	                GameObject targetGameObject = GameObject.Find(InsObjData.CustomTargetName);
	                if (targetGameObject != null)
	                {
	                    trans = targetGameObject.transform;
	                }
	                else
	                {
	                    trans = _combatController.GetNodeTranform(InsObjData.TargetNode);
	                }
	            }
	            else
	            {
	                trans = _combatController.GetNodeTranform(InsObjData.TargetNode);
	            }
	            
	            if (InsObjData.RotateByNode)
	            {
	                InsObjData.Rot = Quaternion.Inverse(trans.rotation) * PreviewTransform.rotation;
	            }
	            if (!InsObjData.RotateByNode)
	            {
	                InsObjData.Rot = Quaternion.Inverse(AnimatorRot) * PreviewTransform.rotation;
	            }
	        }
	        TargetTrans.localScale = PreviewTransform.localScale;
	    }
	
	    public void SetScale(Transform PreviewTransform)
	    {
	        TargetTrans.localScale = PreviewTransform.localScale;
	    }
	
	    //public Action UpdateTransfrom(Transform trans)
	    //{
	
	    //}
	
#endif
	}
	
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace CombatEditor
{
    public class PreviewRotationHandle : PreviewerOnObject
    {
        public RotationTarget target;
        public AbilityEventPreview_Rotation _preview;
        public CombatController _combatController;
        public Transform TargetTrans;
        public Quaternion StartRotation;

        private Quaternion NodeRotAtStart;
        private Quaternion AnimatorRotAtStart;

        Color HandleColor = new Color(1, 0.5f, 0, 0.8f);

        bool initialized = false;
        
        public void Init()
        {
            initialized = true;
        }
        
        public void SetStartFrameRotation(Quaternion nodeRotAtStart, Quaternion animatorRotAtStart)
        {
            NodeRotAtStart = nodeRotAtStart;
            AnimatorRotAtStart = animatorRotAtStart;
        }

#if UNITY_EDITOR
        void OnDrawGizmos()
        {
            if (!initialized) return;
            
            Handles.color = HandleColor;
            
            // 获取要旋转的目标对象的位置
            Transform targetTransform = target.GetTargetTransform(_combatController);
            Vector3 position = targetTransform != null ? targetTransform.position : _combatController.transform.position;
            float size = 0.5f;
            
            // Draw three circles representing rotation axes
            Handles.DrawWireDisc(position, Vector3.right, size);
            Handles.DrawWireDisc(position, Vector3.up, size);
            Handles.DrawWireDisc(position, Vector3.forward, size);
            
            // Draw rotation direction arrows
            if (target.EulerRotation.x != 0)
                DrawRotationArrow(position, Vector3.right, target.EulerRotation.x > 0);
                
            if (target.EulerRotation.y != 0)
                DrawRotationArrow(position, Vector3.up, target.EulerRotation.y > 0);
                
            if (target.EulerRotation.z != 0)
                DrawRotationArrow(position, Vector3.forward, target.EulerRotation.z > 0);
            
            // Draw handle label with target object name
            string targetName = targetTransform != null ? targetTransform.name : "Unknown";
            string labelText = $"Rotation: {target.EulerRotation.ToString("F1")}\nTarget: {targetName}";
            Handles.Label(position + Vector3.up * 0.7f, labelText);
        }
        
        private void DrawRotationArrow(Vector3 position, Vector3 axis, bool clockwise)
        {
            float arrowSize = 0.3f;
            Vector3 perpendicular = (axis == Vector3.up) ? Vector3.right : Vector3.up;
            Vector3 dir = Vector3.Cross(axis, perpendicular).normalized * arrowSize;
            
            if (!clockwise)
                dir = -dir;
                
            Vector3 start = position + axis * 0.2f;
            Handles.DrawLine(start, start + dir);
            Handles.DrawLine(start + dir, start + dir * 0.7f + dir.normalized * 0.1f);
        }

        public override void SelfDestroy()
        {
            Object.DestroyImmediate(this);
        }
#endif
    }
} 
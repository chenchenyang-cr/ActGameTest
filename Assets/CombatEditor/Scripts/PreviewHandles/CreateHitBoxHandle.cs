using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.IMGUI.Controls;

namespace CombatEditor
{
    // 该类只在编辑器中使用
    [ExecuteInEditMode]
    public class CreateHitBoxHandle : MonoBehaviour
    {
        public AbilityEventPreview_CreateHitBox preview;
        private AbilityEventObj_CreateHitBox eventObj => preview.Obj;

        // 处理器
        private BoxBoundsHandle boxHandle;
        private SphereBoundsHandle sphereHandle; 
        private CapsuleBoundsHandle capsuleHandle;

        // 颜色配置 - 使用蓝色避免与其他渲染冲突
        private Color handleColor = new Color(0f, 0f, 1f, 0.8f); // 蓝色
        private Color wireColor = new Color(0f, 0f, 1f, 1f); // 蓝色

        private void SyncHandleColorsFromEvent()
        {
            if (eventObj == null) return;

            // 统一由事件配置驱动可视化颜色，避免“Inspector 改色但 Scene 不变”
            Color baseColor = eventObj.hitBoxColor;
            float wireAlpha = Mathf.Clamp01(baseColor.a + 0.4f);
            float handleAlpha = Mathf.Clamp01(baseColor.a + 0.25f);

            wireColor = new Color(baseColor.r, baseColor.g, baseColor.b, wireAlpha);
            handleColor = new Color(baseColor.r, baseColor.g, baseColor.b, handleAlpha);

            if (boxHandle != null)
            {
                boxHandle.wireframeColor = wireColor;
                boxHandle.handleColor = handleColor;
            }

            if (sphereHandle != null)
            {
                sphereHandle.wireframeColor = wireColor;
                sphereHandle.handleColor = handleColor;
            }

            if (capsuleHandle != null)
            {
                capsuleHandle.wireframeColor = wireColor;
                capsuleHandle.handleColor = handleColor;
            }
        }

        private void OnEnable()
        {
            // 确保在编辑器中该组件被启用后初始化
            if (!Application.isPlaying)
            {
                Initialize();
                // 注册Scene视图更新回调
                SceneView.duringSceneGui += OnSceneGUICallback;
            }
        }

        private void OnDisable()
        {
            // 移除Scene视图更新回调
            SceneView.duringSceneGui -= OnSceneGUICallback;
        }

        private void OnDestroy()
        {
            // 确保移除回调
            SceneView.duringSceneGui -= OnSceneGUICallback;
        }

        // 用于Scene视图中的回调
        private void OnSceneGUICallback(SceneView sceneView)
        {
            if (this == null || !this.enabled)
                return;

            OnSceneGUI();
            sceneView.Repaint();
        }

        public void Initialize()
        {
            // 确保组件存在且活跃
            if (preview == null || eventObj == null)
                return;

            SyncHandleColorsFromEvent();

            switch (eventObj.hitBoxShape)
            {
                case HitBox.HitBoxShape.Box:
                    InitBoxHandle();
                    break;
                case HitBox.HitBoxShape.Sphere:
                    InitSphereHandle();
                    break;
                case HitBox.HitBoxShape.Capsule:
                    InitCapsuleHandle();
                    break;
            }
        }

        private void InitBoxHandle()
        {
            boxHandle = new BoxBoundsHandle();
            // 使用特定的轴向控制，只控制尺寸而不是位置
            // PrimitiveBoundsHandle.Axes.All会同时控制大小和位置，导致与我们的位置控制冲突
            // 我们选择只控制大小，不控制位置
            boxHandle.axes = PrimitiveBoundsHandle.Axes.X | PrimitiveBoundsHandle.Axes.Y | PrimitiveBoundsHandle.Axes.Z;
            boxHandle.size = eventObj.hitBoxSize;
            boxHandle.center = eventObj.hitBoxOffset;
            boxHandle.wireframeColor = wireColor;
            boxHandle.handleColor = handleColor;
        }

        private void InitSphereHandle()
        {
            sphereHandle = new SphereBoundsHandle();
            // None 在部分 Unity 版本下会导致句柄完全不绘制，这里保持可见性
            sphereHandle.axes = PrimitiveBoundsHandle.Axes.X | PrimitiveBoundsHandle.Axes.Y | PrimitiveBoundsHandle.Axes.Z;
            sphereHandle.radius = eventObj.radius;
            sphereHandle.center = eventObj.hitBoxOffset;
            sphereHandle.wireframeColor = wireColor;
            sphereHandle.handleColor = handleColor;
        }

        private void InitCapsuleHandle()
        {
            capsuleHandle = new CapsuleBoundsHandle();
            // None 在部分 Unity 版本下会导致句柄完全不绘制，这里保持可见性
            capsuleHandle.axes = PrimitiveBoundsHandle.Axes.X | PrimitiveBoundsHandle.Axes.Y | PrimitiveBoundsHandle.Axes.Z;
            capsuleHandle.radius = eventObj.radius;
            capsuleHandle.height = eventObj.height;
            capsuleHandle.center = eventObj.hitBoxOffset;
            capsuleHandle.wireframeColor = wireColor;
            capsuleHandle.handleColor = handleColor;
        }

        public void OnSceneGUI()
        {
            if (preview == null || eventObj == null)
                return;

            SyncHandleColorsFromEvent();

            // 如果形状变化，重新初始化handle
            if ((boxHandle != null && eventObj.hitBoxShape != HitBox.HitBoxShape.Box) ||
                (sphereHandle != null && eventObj.hitBoxShape != HitBox.HitBoxShape.Sphere) ||
                (capsuleHandle != null && eventObj.hitBoxShape != HitBox.HitBoxShape.Capsule))
            {
                boxHandle = null;
                sphereHandle = null;
                capsuleHandle = null;
                Initialize();
            }

            // 确保handle已初始化
            if (boxHandle == null && sphereHandle == null && capsuleHandle == null)
            {
                Initialize();
            }

            // 绘制handle
            Matrix4x4 handleMatrix = Matrix4x4.TRS(
                transform.position,
                transform.rotation,
                Vector3.one
            );

            using (new Handles.DrawingScope(handleMatrix))
            {
                EditorGUI.BeginChangeCheck();

                // 首先绘制位置控制手柄
                Vector3 worldPosition = transform.position + transform.rotation * eventObj.hitBoxOffset;
                
                // 绘制一个小指示点
                float handleSize = HandleUtility.GetHandleSize(worldPosition) * 0.05f;
                Handles.color = new Color(1f, 0.7f, 0f, 0.6f); // 半透明橙色，不那么刺眼
                Handles.DrawSolidDisc(worldPosition, Camera.current.transform.forward, handleSize);
                
                // 位置控制手柄
                Quaternion positionHandleRotation = Tools.pivotRotation == PivotRotation.Local
                    ? transform.rotation
                    : Quaternion.identity;
                Vector3 newPosition = Handles.PositionHandle(worldPosition, positionHandleRotation);
                
                // 如果位置变化，更新hitBoxOffset
                if (worldPosition != newPosition)
                {
                    Undo.RecordObject(eventObj, "Move HitBox");
                    eventObj.hitBoxOffset = Quaternion.Inverse(transform.rotation) * (newPosition - transform.position);
                    EditorUtility.SetDirty(eventObj);
                    // 通知预览组件更新
                    if (preview != null && preview.hitBoxVisualizer != null)
                    {
                        preview.UpdateVisualizer();
                    }
                }
                
                // 然后绘制尺寸控制手柄
                switch (eventObj.hitBoxShape)
                {
                    case HitBox.HitBoxShape.Box:
                        if (boxHandle != null)
                        {
                            boxHandle.center = eventObj.hitBoxOffset;
                            boxHandle.size = eventObj.hitBoxSize;
                            boxHandle.DrawHandle();
                        }
                        break;
                    case HitBox.HitBoxShape.Sphere:
                        if (sphereHandle != null)
                        {
                            sphereHandle.center = eventObj.hitBoxOffset;
                            sphereHandle.radius = eventObj.radius;
                            sphereHandle.DrawHandle();
                        }
                        break;
                    case HitBox.HitBoxShape.Capsule:
                        if (capsuleHandle != null)
                        {
                            capsuleHandle.center = eventObj.hitBoxOffset;
                            capsuleHandle.radius = eventObj.radius;
                            capsuleHandle.height = eventObj.height;
                            capsuleHandle.DrawHandle();
                        }
                        break;
                }

                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(eventObj, "Modify HitBox");

                    switch (eventObj.hitBoxShape)
                    {
                        case HitBox.HitBoxShape.Box:
                            // 这里只更新尺寸，位置已经通过位置手柄更新
                            eventObj.hitBoxSize = boxHandle.size;
                            break;
                        case HitBox.HitBoxShape.Sphere:
                            // 位置更新已经在位置手柄处理
                            eventObj.radius = sphereHandle.radius;
                            break;
                        case HitBox.HitBoxShape.Capsule:
                            // 位置更新已经在位置手柄处理
                            eventObj.radius = capsuleHandle.radius;
                            eventObj.height = capsuleHandle.height;
                            break;
                    }

                    EditorUtility.SetDirty(eventObj);
                    // 通知预览组件更新
                    if (preview != null && preview.hitBoxVisualizer != null)
                    {
                        preview.UpdateVisualizer();
                    }
                }
            }
        }

        // 用于调试目的：在场景视图中显示基本信息
        private void OnDrawGizmos()
        {
            if (preview == null || eventObj == null)
                return;

            // 显示小标记以标识handle位置
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, 0.1f);
        }
    }

    // 自定义编辑器已不需要，因为我们使用SceneView.duringSceneGui回调
}
#endif 

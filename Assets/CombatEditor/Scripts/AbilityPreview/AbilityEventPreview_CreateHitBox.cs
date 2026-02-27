using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace CombatEditor
{	
#if UNITY_EDITOR
	public class AbilityEventPreview_CreateHitBox : AbilityEventPreview
	{
	    private GameObject preview;
	    private PreviewHitBoxVisualizer visualizer;
	    private Transform bindTransform; // 绑定的Transform
	    
	    // 为了兼容性，保留旧的接口
	    public AbilityEventObj_CreateHitBox Obj => (AbilityEventObj_CreateHitBox)_EventObj;
	    public PreviewHitBoxVisualizer hitBoxVisualizer => visualizer;
	    
	    public AbilityEventPreview_CreateHitBox(AbilityEventObj eventObj) : base(eventObj)
	    {
	    }
	
	    public override void InitPreview()
	    {
	        base.InitPreview();
	        
	        AbilityEventObj_CreateHitBox hitBoxEvent = (AbilityEventObj_CreateHitBox)_EventObj;
	        
	        // 创建预览对象
	        preview = new GameObject("HitBoxPreview_" + _EventObj.name);
	        
	        // 尝试绑定指定的Transform
	        if (!string.IsNullOrEmpty(hitBoxEvent.bindTransformName) && hitBoxEvent.autoSearchBindTransform)
	        {
	            FindAndBindTransform(hitBoxEvent.bindTransformName);
	        }
	        
	        // 如果没有绑定的Transform，使用combatController作为默认
	        if (bindTransform == null)
	        {
	            bindTransform = _combatController.transform;
	        }
	        
	        // 设置预览对象为绑定Transform的子对象
	        preview.transform.parent = bindTransform;
	        
	        // 重置本地坐标和旋转
	        preview.transform.localPosition = Vector3.zero;
	        preview.transform.localRotation = Quaternion.identity;
	        
	        // 添加可视化组件
	        visualizer = preview.AddComponent<PreviewHitBoxVisualizer>();
	        
	        // 配置可视化组件
	        UpdateVisualizerProperties();
	        
	        // 添加CreateHitBoxHandle组件用于交互（保持与原有系统兼容）
	        var handle = preview.AddComponent<CreateHitBoxHandle>();
	        handle.preview = this;
	        handle.Initialize();
	    }
	    
	    // 搜索并绑定Transform
	    private void FindAndBindTransform(string transformName)
	    {
	        if (string.IsNullOrEmpty(transformName))
	            return;
	            
	        // 首先在combatController的子对象中搜索
	        if (_combatController != null)
	        {
	            Transform found = FindTransformInChildren(_combatController.transform, transformName);
	            if (found != null)
	            {
	                bindTransform = found;
	                Debug.Log($"HitBoxPreview: 在CombatController中找到并绑定Transform: {transformName}");
	                return;
	            }
	        }
	        
	        // 如果在CombatController中没找到，在整个场景中搜索
	        GameObject foundObj = GameObject.Find(transformName);
	        if (foundObj != null)
	        {
	            bindTransform = foundObj.transform;
	            Debug.Log($"HitBoxPreview: 在场景中找到并绑定Transform: {transformName}");
	        }
	        else
	        {
	            Debug.LogWarning($"HitBoxPreview: 未找到名为 '{transformName}' 的Transform");
	        }
	    }
	    
	    // 在子对象中递归搜索Transform
	    private Transform FindTransformInChildren(Transform parent, string name)
	    {
	        if (parent.name == name)
	            return parent;
	            
	        for (int i = 0; i < parent.childCount; i++)
	        {
	            Transform found = FindTransformInChildren(parent.GetChild(i), name);
	            if (found != null)
	                return found;
	        }
	        
	        return null;
	    }
	    
	    // 为兼容性提供的公共方法
	    public void UpdateVisualizer()
	    {
	        UpdateVisualizerProperties();
	    }
	    
	    // ColliderPreviewHandle期望的方法
	    public void SetHitBoxEvent(AbilityEventObj_CreateHitBox hitBoxEvent)
	    {
	        // 这个方法主要是为了兼容ColliderPreviewHandle
	        // 实际的配置在InitPreview中完成
	    }
	    
	    // 更新可视化器属性的私有方法
	    private void UpdateVisualizerProperties()
	    {
	        if (visualizer != null)
	        {
	            AbilityEventObj_CreateHitBox hitBoxEvent = (AbilityEventObj_CreateHitBox)_EventObj;
	            
	            // 更新可视化参数
	            visualizer.hitBoxShape = hitBoxEvent.hitBoxShape;
	            visualizer.hitBoxSize = hitBoxEvent.hitBoxSize;
	            visualizer.hitBoxOffset = hitBoxEvent.hitBoxOffset;
	            visualizer.radius = hitBoxEvent.radius;
	            visualizer.height = hitBoxEvent.height;
	            visualizer.hitBoxColor = hitBoxEvent.hitBoxColor;
	        }
	    }
	    
	    public override void PreviewRunning(float CurrentTimePercentage)
	    {
	        base.PreviewRunning(CurrentTimePercentage);
	        
	        // 由于预览对象现在是绑定Transform的子对象，会自动跟随移动和旋转
	        // 只需要更新可视化参数即可
	        UpdateVisualizerProperties();
	    }
	    
	    public override void DestroyPreview()
	    {
	        if (preview != null)
	        {
	            GameObject.DestroyImmediate(preview);
	            preview = null;
	            visualizer = null;
	            bindTransform = null;
	        }
	    }
	}
	
	// 仅用于编辑器预览的HitBox可视化组件
	public class PreviewHitBoxVisualizer : MonoBehaviour
	{
	    public HitBox.HitBoxShape hitBoxShape = HitBox.HitBoxShape.Box;
	    public Vector3 hitBoxOffset = Vector3.zero;
	    public Vector3 hitBoxSize = Vector3.one;
	    public float radius = 0.5f;
	    public float height = 1f;
	    public Color hitBoxColor = new Color(1f, 0f, 0f, 0.3f);
	    
	    private void OnDrawGizmos()
	    {
	        // 完全禁用Gizmos渲染，让Handle系统负责所有可视化
	        // 这样避免与Handle系统重复渲染
	        return;
	        
#if UNITY_EDITOR
	        // 只在编辑器模式且非播放状态时渲染，避免与实际HitBox重复
	        if (!UnityEditor.EditorApplication.isPlaying)
	        {
	            Gizmos.color = hitBoxColor;
	            Matrix4x4 rotationMatrix = Matrix4x4.TRS(transform.position, transform.rotation, transform.lossyScale);
	            Gizmos.matrix = rotationMatrix;
	            
	            switch (hitBoxShape)
	            {
	                case HitBox.HitBoxShape.Box:
	                    Gizmos.DrawCube(hitBoxOffset, hitBoxSize);
	                    break;
	                    
	                case HitBox.HitBoxShape.Sphere:
	                    Gizmos.DrawSphere(hitBoxOffset, radius);
	                    break;
	                    
	                case HitBox.HitBoxShape.Capsule:
	                    // 绘制胶囊体特有组件
	                    DrawCapsule(hitBoxOffset, radius, height, hitBoxColor);
	                    break;
	            }
	        }
#endif
	    }
	    
	    // 辅助方法：绘制胶囊体
	    private void DrawCapsule(Vector3 center, float radius, float height, Color color)
	    {
	        // 胶囊体的可视化需要多个步骤
	        // 绘制两个球体和连接它们的圆柱体
	        Vector3 top = center + Vector3.up * (height * 0.5f - radius);
	        Vector3 bottom = center - Vector3.up * (height * 0.5f - radius);
	        
	        Gizmos.color = color;
	        Gizmos.DrawSphere(top, radius);
	        Gizmos.DrawSphere(bottom, radius);
	        
	        // 绘制连接线
	        const int segments = 12;
	        float angleStep = 360f / segments;
	        
	        for (int i = 0; i < segments; i++)
	        {
	            float angle1 = i * angleStep * Mathf.Deg2Rad;
	            float angle2 = ((i + 1) % segments) * angleStep * Mathf.Deg2Rad;
	            
	            Vector3 point1Top = top + new Vector3(Mathf.Cos(angle1), 0, Mathf.Sin(angle1)) * radius;
	            Vector3 point2Top = top + new Vector3(Mathf.Cos(angle2), 0, Mathf.Sin(angle2)) * radius;
	            
	            Vector3 point1Bottom = bottom + new Vector3(Mathf.Cos(angle1), 0, Mathf.Sin(angle1)) * radius;
	            Vector3 point2Bottom = bottom + new Vector3(Mathf.Cos(angle2), 0, Mathf.Sin(angle2)) * radius;
	            
	            Gizmos.DrawLine(point1Top, point2Top);
	            Gizmos.DrawLine(point1Bottom, point2Bottom);
	            Gizmos.DrawLine(point1Top, point1Bottom);
	        }
	    }
	}
#endif
}

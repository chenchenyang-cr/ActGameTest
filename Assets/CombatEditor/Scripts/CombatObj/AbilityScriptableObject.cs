using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

 namespace CombatEditor
{
    // 定义事件条件类型
    [System.Serializable]
    public class EventCondition
    {
        public bool hasCondition = false; // 默认没有条件
        
        // 传统枚举条件类型 (保持向后兼容)
        public enum ConditionType 
        { 
            None = 0,           // 无条件
            HasHit = 1,         // 击中目标
            BeenHit = 2,        // 被击中
            InHitStop = 3,      // 在顿帧中
            HitChecked = 4      // 判定受击
        }
        
        public ConditionType conditionType = ConditionType.None;
        
        // 条件值，默认为false
        public bool conditionValue = false;
        
        // 新增：条件模式
        public enum ConditionMode
        {
            LegacyEnum = 0,     // 传统枚举模式
            Interface = 1       // 接口模式
        }
        
        public ConditionMode conditionMode = ConditionMode.LegacyEnum;
        
        // 新增：接口条件ID
        public string interfaceConditionId = "";
        
        // 缓存的接口条件实例
        [System.NonSerialized]
        private IEventCondition _cachedInterfaceCondition = null;
        
        // 检查条件是否满足
        public bool CheckCondition(CombatController controller)
        {
            if (!hasCondition)
                return true; // 没有条件时默认满足
            
            // 根据条件模式选择检查方式
            switch (conditionMode)
            {
                case ConditionMode.LegacyEnum:
                    return CheckLegacyCondition(controller);
                    
                case ConditionMode.Interface:
                    return CheckInterfaceCondition(controller);
                    
                default:
                    return true;
            }
        }
        
        // 检查传统枚举条件
        private bool CheckLegacyCondition(CombatController controller)
        {
            if (conditionType == ConditionType.None)
                return true;
                
            switch (conditionType)
            {
                case ConditionType.HasHit:
                    return controller.HasHitTarget();
                case ConditionType.BeenHit:
                    return controller.HasBeenHit();
                case ConditionType.InHitStop:
                    return controller.IsInHitStop();
                case ConditionType.HitChecked:
                    return controller.IsHitChecked();
                default:
                    return true;
            }
        }
        
        // 检查接口条件
        private bool CheckInterfaceCondition(CombatController controller)
        {
            if (string.IsNullOrEmpty(interfaceConditionId))
                return true;
            
            // 获取或创建接口条件实例
            if (_cachedInterfaceCondition == null)
            {
                _cachedInterfaceCondition = EventConditionManager.Instance.CreateCondition(interfaceConditionId);
            }
            
            // 如果仍然为null，表示条件不存在
            if (_cachedInterfaceCondition == null)
            {
                Debug.LogWarning($"Interface condition with ID '{interfaceConditionId}' not found. Returning true.");
                return true;
            }
            
            // 检查条件
            return _cachedInterfaceCondition.CheckCondition(controller);
        }
        
        // 获取条件显示名称
        public string GetConditionDisplayName()
        {
            if (!hasCondition)
                return "无条件";
            
            switch (conditionMode)
            {
                case ConditionMode.LegacyEnum:
                    return GetLegacyConditionDisplayName();
                    
                case ConditionMode.Interface:
                    return GetInterfaceConditionDisplayName();
                    
                default:
                    return "未知条件";
            }
        }
        
        // 获取传统条件显示名称
        private string GetLegacyConditionDisplayName()
        {
            switch (conditionType)
            {
                case ConditionType.None:
                    return "无条件";
                case ConditionType.HasHit:
                    return "击中目标";
                case ConditionType.BeenHit:
                    return "被击中";
                case ConditionType.InHitStop:
                    return "在顿帧中";
                case ConditionType.HitChecked:
                    return "判定受击";
                default:
                    return "未知条件";
            }
        }
        
        // 获取接口条件显示名称
        private string GetInterfaceConditionDisplayName()
        {
            if (string.IsNullOrEmpty(interfaceConditionId))
                return "无条件";
            
            var condition = EventConditionManager.Instance.GetCondition(interfaceConditionId);
            return condition?.DisplayName ?? "未知条件";
        }
        
        // 获取条件颜色
        public Color GetConditionColor()
        {
            if (!hasCondition)
                return Color.white;
            
            switch (conditionMode)
            {
                case ConditionMode.LegacyEnum:
                    return GetLegacyConditionColor();
                    
                case ConditionMode.Interface:
                    return GetInterfaceConditionColor();
                    
                default:
                    return Color.white;
            }
        }
        
        // 获取传统条件颜色
        private Color GetLegacyConditionColor()
        {
            switch (conditionType)
            {
                case ConditionType.HasHit:
                    return new Color(1f, 0.6f, 0.2f); // 橙色
                case ConditionType.BeenHit:
                    return new Color(1f, 0.2f, 0.2f); // 红色
                case ConditionType.InHitStop:
                    return new Color(0.4f, 0.4f, 1f); // 蓝色
                case ConditionType.HitChecked:
                    return new Color(0.2f, 0.8f, 0.2f); // 绿色
                default:
                    return Color.white;
            }
        }
        
        // 获取接口条件颜色
        private Color GetInterfaceConditionColor()
        {
            if (string.IsNullOrEmpty(interfaceConditionId))
                return Color.white;
            
            var condition = EventConditionManager.Instance.GetCondition(interfaceConditionId);
            return condition?.IconColor ?? Color.white;
        }
        
        // 获取条件简短标签
        public string GetConditionShortLabel()
        {
            if (!hasCondition)
                return "无";
            
            switch (conditionMode)
            {
                case ConditionMode.LegacyEnum:
                    return GetLegacyConditionShortLabel();
                    
                case ConditionMode.Interface:
                    return GetInterfaceConditionShortLabel();
                    
                default:
                    return "未知";
            }
        }
        
        // 获取传统条件简短标签
        private string GetLegacyConditionShortLabel()
        {
            switch (conditionType)
            {
                case ConditionType.None:
                    return "无";
                case ConditionType.HasHit:
                    return "击中";
                case ConditionType.BeenHit:
                    return "被击";
                case ConditionType.InHitStop:
                    return "顿帧";
                case ConditionType.HitChecked:
                    return "判定";
                default:
                    return "未知";
            }
        }
        
        // 获取接口条件简短标签
        private string GetInterfaceConditionShortLabel()
        {
            if (string.IsNullOrEmpty(interfaceConditionId))
                return "无";
            
            var condition = EventConditionManager.Instance.GetCondition(interfaceConditionId);
            return condition?.ShortLabel ?? "未知";
        }
        
        // 将旧版枚举条件转换为字符串ID
        public string GetLegacyConditionId()
        {
            switch (conditionType)
            {
                case ConditionType.HasHit: return "has_hit";
                case ConditionType.BeenHit: return "been_hit";
                case ConditionType.InHitStop: return "in_hit_stop";
                case ConditionType.HitChecked: return "hit_checked";
                case ConditionType.None:
                default:
                    return "none";
            }
        }
        
        // 清除缓存的接口条件实例
        public void ClearCache()
        {
            _cachedInterfaceCondition = null;
        }
    }
    
    [System.Serializable]
    public class AbilityEvent
    {

        public float EventTime;
        public Vector2 EventRange = new Vector2(0, 1);
        public float[] EventMultiRange = new float[4]{0.2f,0.4f,0.6f,0.8f}; 
	    public bool Previewable;
        public AbilityEventObj Obj;
        public EventCondition condition = new EventCondition(); // 添加条件字段

        public void ResetAbilityEvent()
	    {
	        if (Obj != null)
	        {
	   
	        }
	    }
	    public float GetEventStartTime()
	    {
	        if(Obj == null)
	        {
	            return 0;
	        }
	        if (Obj.GetEventTimeType() == AbilityEventObj.EventTimeType.Null)
	        {
	            return 0;
	        }
	        if (Obj.GetEventTimeType() == AbilityEventObj.EventTimeType.EventTime)
	        {
	            return EventTime;
	        }
	        if (Obj.GetEventTimeType() == AbilityEventObj.EventTimeType.EventRange)
	        {
	            return EventRange.x;
	        }
            if(Obj.GetEventTimeType() == AbilityEventObj.EventTimeType.EventMultiRange)
            {
                return EventRange.x;
            }

	        return 0;
	    }
	    public float GetEventEndTime()
	    {
	        if(!Obj)
	        {
	            return 0;
	        }
	        if (Obj.GetEventTimeType() == AbilityEventObj.EventTimeType.EventRange)
	        {
	            return EventRange.y;
	        }
            if (Obj.GetEventTimeType() == AbilityEventObj.EventTimeType.EventMultiRange)
            {
                return EventRange.y;
            }
            return 1;
	    }
        public AbilityEventObj.EventTimeType GetEventTimeType()
        {
            return Obj.GetEventTimeType();
        }
        
        // 检查事件条件是否满足
        public bool CheckCondition(CombatController controller)
        {
            return condition.CheckCondition(controller);
        }
    }
	
	[CreateAssetMenu(menuName =("AbilityObj"))]
	public class AbilityScriptableObject : ScriptableObject
	{
	    public AbilityTypes AbilityType;
	      
	    public enum AbilityTypes { OneShot, Loop , BlendingTree_1D, BlendingTree_2D }
	    public AnimationClip Clip;
	    [HideInInspector]
	    public Vector2 PreviewPercentageRange = new Vector2(0, 1);
	    //public float Speed = 1;
	    public float loopCount = 0;
	    
	    public List<AbilityEvent> events = new List<AbilityEvent>();
	
	    public void ResetEvent()
	    {
	        EventManager.TriggerEvent("ChangeAbilityEvent");
	    }
	
	
	}
}

using UnityEngine;

namespace CombatEditor
{
    /// <summary>
    /// 内置条件类，对应现有的硬编码条件类型
    /// </summary>
    public static class BuiltInConditions
    {
        /// <summary>
        /// 无条件
        /// </summary>
        public class NoneCondition : IEventCondition
        {
            public string ConditionId => "none";
            public string DisplayName => "无条件";
            public string Description => "无条件限制，事件总是会触发";
            public Color IconColor => Color.white;
            public string ShortLabel => "无";
            
            public bool CheckCondition(CombatController controller)
            {
                return true;
            }
        }
        
        /// <summary>
        /// 击中目标条件
        /// </summary>
        public class HasHitCondition : IEventCondition
        {
            public string ConditionId => "has_hit";
            public string DisplayName => "击中目标";
            public string Description => "当角色击中目标时触发事件\n\n在代码中使用 controller.SetHitTargetCondition(true/false) 设置条件";
            public Color IconColor => new Color(1f, 0.6f, 0.2f); // 橙色
            public string ShortLabel => "击中";
            
            public bool CheckCondition(CombatController controller)
            {
                return controller.HasHitTarget();
            }
        }
        
        /// <summary>
        /// 被击中条件
        /// </summary>
        public class BeenHitCondition : IEventCondition
        {
            public string ConditionId => "been_hit";
            public string DisplayName => "被击中";
            public string Description => "当角色被击中时触发事件\n\n在代码中使用 controller.SetBeenHitCondition(true/false) 设置条件";
            public Color IconColor => new Color(1f, 0.2f, 0.2f); // 红色
            public string ShortLabel => "被击";
            
            public bool CheckCondition(CombatController controller)
            {
                return controller.HasBeenHit();
            }
        }
        
        /// <summary>
        /// 在顿帧中条件
        /// </summary>
        public class InHitStopCondition : IEventCondition
        {
            public string ConditionId => "in_hit_stop";
            public string DisplayName => "在顿帧中";
            public string Description => "当角色处于顿帧状态时触发事件\n\n此条件在顿帧开始时自动设置为true，顿帧结束时自动设置为false";
            public Color IconColor => new Color(0.4f, 0.4f, 1f); // 蓝色
            public string ShortLabel => "顿帧";
            
            public bool CheckCondition(CombatController controller)
            {
                return controller.IsInHitStop();
            }
        }
        
        /// <summary>
        /// 判定受击条件
        /// </summary>
        public class HitCheckedCondition : IEventCondition
        {
            public string ConditionId => "hit_checked";
            public string DisplayName => "判定受击";
            public string Description => "当判定受击时触发事件\n\n在代码中使用 controller.SetHitCheckedCondition(true/false) 设置条件";
            public Color IconColor => new Color(0.2f, 0.8f, 0.2f); // 绿色
            public string ShortLabel => "判定";
            
            public bool CheckCondition(CombatController controller)
            {
                return controller.IsHitChecked();
            }
        }
    }
} 
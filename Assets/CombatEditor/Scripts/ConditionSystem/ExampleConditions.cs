using UnityEngine;

namespace CombatEditor
{
    /// <summary>
    /// 示例条件类，展示如何实现自定义条件
    /// </summary>
    public static class ExampleConditions
    {
        /// <summary>
        /// 血量百分比条件
        /// </summary>
        public class HealthPercentageCondition : IEventCondition
        {
            public string ConditionId => "health_percentage";
            public string DisplayName => "血量百分比";
            public string Description => "当角色血量百分比达到指定值时触发事件";
            public Color IconColor => new Color(0.8f, 0.2f, 0.2f); // 深红色
            public string ShortLabel => "血量";
            
            public bool CheckCondition(CombatController controller)
            {
                return controller.GetHealthPercentage() <= 0.5f; // 示例：血量低于50%时触发
            }
        }
        
        /// <summary>
        /// 在空中条件
        /// </summary>
        public class InAirCondition : IEventCondition
        {
            public string ConditionId => "in_air";
            public string DisplayName => "在空中";
            public string Description => "当角色在空中时触发事件";
            public Color IconColor => new Color(0.6f, 0.8f, 1f); // 天蓝色
            public string ShortLabel => "空中";
            
            public bool CheckCondition(CombatController controller)
            {
                return controller.IsInAir();
            }
        }
        
        /// <summary>
        /// 在地面条件
        /// </summary>
        public class OnGroundCondition : IEventCondition
        {
            public string ConditionId => "on_ground";
            public string DisplayName => "在地面";
            public string Description => "当角色在地面上时触发事件";
            public Color IconColor => new Color(0.6f, 0.4f, 0.2f); // 棕色
            public string ShortLabel => "地面";
            
            public bool CheckCondition(CombatController controller)
            {
                return controller.IsOnGround();
            }
        }
        
        /// <summary>
        /// 有目标条件
        /// </summary>
        public class HasTargetCondition : IEventCondition
        {
            public string ConditionId => "has_target";
            public string DisplayName => "有目标";
            public string Description => "当角色有攻击目标时触发事件";
            public Color IconColor => new Color(1f, 0.8f, 0.2f); // 金色
            public string ShortLabel => "目标";
            
            public bool CheckCondition(CombatController controller)
            {
                return controller.HasTarget();
            }
            
            public void Initialize(CombatController controller) { }
            public void Cleanup(CombatController controller) { }
            public ScriptableObject GetCustomParameters() => null;
            public void SetCustomParameters(ScriptableObject parameters) { }
        }
        
        /// <summary>
        /// 距离条件
        /// </summary>
        public class DistanceCondition : IEventCondition
        {
            public string ConditionId => "distance";
            public string DisplayName => "距离条件";
            public string Description => "当角色与目标的距离在指定范围内时触发事件";
            public Color IconColor => new Color(0.8f, 0.4f, 0.8f); // 紫色
            public string ShortLabel => "距离";
            
            public bool CheckCondition(CombatController controller)
            {
                return controller.GetTargetDistance() <= 5f; // 示例：距离目标5米以内时触发
            }
            
            public void Initialize(CombatController controller) { }
            public void Cleanup(CombatController controller) { }
            public ScriptableObject GetCustomParameters() => null;
            public void SetCustomParameters(ScriptableObject parameters) { }
        }
        
        /// <summary>
        /// 拥有Buff条件
        /// </summary>
        public class HasBuffCondition : IEventCondition
        {
            public string ConditionId => "has_buff";
            public string DisplayName => "拥有Buff";
            public string Description => "当角色拥有指定Buff时触发事件";
            public Color IconColor => new Color(0.2f, 1f, 0.2f); // 亮绿色
            public string ShortLabel => "Buff";
            
            public bool CheckCondition(CombatController controller)
            {
                return controller.HasBuff("example_buff"); // 示例：拥有特定buff时触发
            }
            
            public void Initialize(CombatController controller) { }
            public void Cleanup(CombatController controller) { }
            public ScriptableObject GetCustomParameters() => null;
            public void SetCustomParameters(ScriptableObject parameters) { }
        }
        
        /// <summary>
        /// 状态条件
        /// </summary>
        public class StateCondition : IEventCondition
        {
            public string ConditionId => "state";
            public string DisplayName => "状态条件";
            public string Description => "当角色处于指定状态时触发事件";
            public Color IconColor => new Color(0.4f, 0.6f, 1f); // 蓝色
            public string ShortLabel => "状态";
            
            public bool CheckCondition(CombatController controller)
            {
                return controller.IsInState("example_state"); // 示例：处于特定状态时触发
            }
            
            public void Initialize(CombatController controller) { }
            public void Cleanup(CombatController controller) { }
            public ScriptableObject GetCustomParameters() => null;
            public void SetCustomParameters(ScriptableObject parameters) { }
        }
        
        /// <summary>
        /// 攻击阶段条件
        /// </summary>
        public class AttackPhaseCondition : IEventCondition
        {
            public string ConditionId => "attack_phase";
            public string DisplayName => "攻击阶段";
            public string Description => "当角色处于指定攻击阶段时触发事件";
            public Color IconColor => new Color(1f, 0.4f, 0.2f); // 橙红色
            public string ShortLabel => "阶段";
            
            public bool CheckCondition(CombatController controller)
            {
                return controller.GetAttackPhase() == 3; // 示例：第3攻击阶段时触发
            }
            
            public void Initialize(CombatController controller) { }
            public void Cleanup(CombatController controller) { }
            public ScriptableObject GetCustomParameters() => null;
            public void SetCustomParameters(ScriptableObject parameters) { }
        }
        
        /// <summary>
        /// 连击数条件
        /// </summary>
        public class ComboCountCondition : IEventCondition
        {
            public string ConditionId => "combo_count";
            public string DisplayName => "连击数";
            public string Description => "当连击数达到指定值时触发事件";
            public Color IconColor => new Color(1f, 0.8f, 0.4f); // 金黄色
            public string ShortLabel => "连击";
            
            public bool CheckCondition(CombatController controller)
            {
                return controller.GetComboCount() >= 10; // 示例：连击10次或以上时触发
            }
            
            public void Initialize(CombatController controller) { }
            public void Cleanup(CombatController controller) { }
            public ScriptableObject GetCustomParameters() => null;
            public void SetCustomParameters(ScriptableObject parameters) { }
        }
        
        /// <summary>
        /// 时间条件
        /// </summary>
        public class TimeCondition : IEventCondition
        {
            public string ConditionId => "time";
            public string DisplayName => "时间条件";
            public string Description => "当游戏时间满足指定条件时触发事件";
            public Color IconColor => new Color(0.8f, 0.8f, 0.2f); // 黄色
            public string ShortLabel => "时间";
            
            public bool CheckCondition(CombatController controller)
            {
                return Time.time % 5f < 0.1f; // 示例：每5秒触发一次
            }
            
            public void Initialize(CombatController controller) { }
            public void Cleanup(CombatController controller) { }
            public ScriptableObject GetCustomParameters() => null;
            public void SetCustomParameters(ScriptableObject parameters) { }
        }
    }
} 
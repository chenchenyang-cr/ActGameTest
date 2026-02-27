using System;
using UnityEngine;

namespace CombatEditor
{
    /// <summary>
    /// 条件状态变更通知器
    /// 一个全局事件总线，用于在条件状态可能发生变化时通知相关系统
    /// </summary>
    public static class ConditionStateNotifier
    {
        /// <summary>
        /// 当一个条件的状态可能发生变化时触发
        /// 参数：
        /// - CombatController: 触发变化的角色
        /// - string: 发生变化的条件ID (e.g., "has_hit", "custom_buff_condition")
        /// </summary>
        public static event Action<CombatController, string> OnConditionStateChanged;

        /// <summary>
        /// 通知一个条件的状态可能发生了变化
        /// </summary>
        /// <param name="controller">触发变化的角色控制器</param>
        /// <param name="conditionId">发生变化的条件ID</param>
        public static void Notify(CombatController controller, string conditionId)
        {
            if (string.IsNullOrEmpty(conditionId)) return;
            
            //Debug.Log($"[ConditionNotifier] 📢 Notifying change for condition: '{conditionId}' on controller: {controller.name}");
            OnConditionStateChanged?.Invoke(controller, conditionId);
        }
    }
} 
using UnityEngine;

namespace CombatEditor
{
    /// <summary>
    /// 事件条件接口，所有自定义条件类型都必须实现此接口
    /// </summary>
    public interface IEventCondition
    {
        /// <summary>
        /// 条件的唯一标识符
        /// </summary>
        string ConditionId { get; }
        
        /// <summary>
        /// 条件的显示名称
        /// </summary>
        string DisplayName { get; }
        
        /// <summary>
        /// 条件的描述信息
        /// </summary>
        string Description { get; }
        
        /// <summary>
        /// 条件的图标颜色
        /// </summary>
        Color IconColor { get; }
        
        /// <summary>
        /// 条件的简短标签（用于UI显示）
        /// </summary>
        string ShortLabel { get; }
        
        /// <summary>
        /// 检查条件是否满足
        /// </summary>
        /// <param name="controller">战斗控制器</param>
        /// <returns>条件是否满足</returns>
        bool CheckCondition(CombatController controller);
    }
} 
using System.Collections;
using System.Collections.Generic;
using CombatEditor;
using UnityEngine;

namespace CombatEditor
{
    // 战斗事件监听器接口
    public interface ICombatEventListener
    {
        // 当角色击中目标时触发
        void OnHasHitTarget(CombatController attacker, CombatController target);
        
        // 当角色被击中时触发
        void OnBeenHit(CombatController target, CombatController attacker);
        
        // 当角色完成一次命中判定时触发
        void OnHitChecked(CombatController character);
        
        // 当角色进入顿帧状态时触发
        void OnEnterHitStop(CombatController character);
        
        // 当角色退出顿帧状态时触发
        void OnExitHitStop(CombatController character);
        
        // 当攻击阶段变更时触发
        void OnAttackPhaseChanged(CombatController character, int oldPhase, int newPhase);
        
        // 当动画机变量变更时触发
        void OnAnimatorVariableChanged(CombatController character, string variableName, object oldValue, object newValue);
        
        // 当动画机布尔变量被设置时触发
        void OnAnimatorBoolSet(CombatController character, string variableName, bool value);
        
        // 当动画机整数变量被设置时触发
        void OnAnimatorIntSet(CombatController character, string variableName, int value);
        
        // 当动画机浮点变量被设置时触发
        void OnAnimatorFloatSet(CombatController character, string variableName, float value);
        
        // 当动画机触发器被激活时触发
        void OnAnimatorTriggerSet(CombatController character, string triggerName);
    }

    public class CombatEvent : MonoBehaviour, ICombatEventListener
    {
        // 对应的CombatController引用，用于显示在Inspector中
        public CombatController combatController;
        
        void Start()
        {
            Application.targetFrameRate = 60; 
            // 不再需要手动注册，CombatController会自动发现并注册监听器
        }
        
        void Update()
        {
            
        }
        
        // 当角色击中目标时触发
        public void OnHasHitTarget(CombatController attacker, CombatController target)
        {
            Debug.Log($"OnHasHitTarget: {attacker.name}击中了{target.name}");
        }
        
        // 当角色被击中时触发
        public void OnBeenHit(CombatController target, CombatController attacker)
        {
            Debug.Log($"OnBeenHit: {target.name}被{attacker.name}击中");
        }
        
        // 当角色完成一次命中判定时触发
        public void OnHitChecked(CombatController character)
        {
            Debug.Log($"OnHitChecked: {character.name}完成了命中判定");
        }
        
        // 当角色进入顿帧状态时触发
        public void OnEnterHitStop(CombatController character)
        {
            Debug.Log($"OnEnterHitStop: {character.name}进入顿帧状态");
        }
        
        // 当角色退出顿帧状态时触发
        public void OnExitHitStop(CombatController character)
        {
            Debug.Log($"OnExitHitStop: {character.name}退出顿帧状态");
        }
        
        // 当攻击阶段变更时触发
        public void OnAttackPhaseChanged(CombatController character, int oldPhase, int newPhase)
        {
            Debug.Log($"OnAttackPhaseChanged: {character.name}的攻击阶段从{oldPhase}变为{newPhase}");
            
            // 可以在这里处理不同攻击阶段的逻辑，例如：
            switch (newPhase)
            {
                case 0:
                    // 阶段0的处理（常用作初始状态或未激活状态）
                    break;
                case 1:
                    // 阶段1的处理（例如：准备攻击）
                    break;
                case 2:
                    // 阶段2的处理（例如：攻击中）
                    break;
                case 3:
                    // 阶段3的处理（例如：收招/硬直）
                    break;
                default:
                    // 其他阶段处理
                    break;
            }
        }
        
        // 当动画机变量变更时触发
        public void OnAnimatorVariableChanged(CombatController character, string variableName, object oldValue, object newValue)
        {
            Debug.Log($"OnAnimatorVariableChanged: {character.name}的动画机变量{variableName}从{oldValue}变为{newValue}");
        }
        
        // 当动画机布尔变量被设置时触发
        public void OnAnimatorBoolSet(CombatController character, string variableName, bool value)
        {
            Debug.Log($"OnAnimatorBoolSet: {character.name}的动画机布尔变量{variableName}被设置为{value}");
        }
        
        // 当动画机整数变量被设置时触发
        public void OnAnimatorIntSet(CombatController character, string variableName, int value)
        {
            Debug.Log($"OnAnimatorIntSet: {character.name}的动画机整数变量{variableName}被设置为{value}");
        }
        
        // 当动画机浮点变量被设置时触发
        public void OnAnimatorFloatSet(CombatController character, string variableName, float value)
        {
            Debug.Log($"OnAnimatorFloatSet: {character.name}的动画机浮点变量{variableName}被设置为{value}");
        }
        
        // 当动画机触发器被激活时触发
        public void OnAnimatorTriggerSet(CombatController character, string triggerName)
        {
            Debug.Log($"OnAnimatorTriggerSet: {character.name}的动画机触发器{triggerName}被激活");
        }
        
        // OnDisable方法也可以保留，虽然不是必须的
        // CombatController在游戏对象销毁时会自动清理引用
    }
} 
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CombatEditor
{
    /// <summary>
    /// 事件条件管理器，负责注册和管理所有条件类型
    /// </summary>
    public class EventConditionManager
    {
        private static EventConditionManager _instance;
        public static EventConditionManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new EventConditionManager();
                    _instance.InitializeBuiltInConditions();
                }
                return _instance;
            }
        }
        
        private Dictionary<string, IEventCondition> _registeredConditions = new Dictionary<string, IEventCondition>();
        private Dictionary<string, Type> _registeredConditionTypes = new Dictionary<string, Type>();
        
        /// <summary>
        /// 注册条件类型
        /// </summary>
        /// <param name="conditionType">条件类型</param>
        public void RegisterCondition<T>() where T : IEventCondition, new()
        {
            T condition = new T();
            RegisterCondition(condition);
        }
        
        /// <summary>
        /// 注册条件实例
        /// </summary>
        /// <param name="condition">条件实例</param>
        public void RegisterCondition(IEventCondition condition)
        {
            if (condition == null)
            {
                Debug.LogError("Cannot register null condition");
                return;
            }
            
            string conditionId = condition.ConditionId;
            if (string.IsNullOrEmpty(conditionId))
            {
                Debug.LogError("Condition ID cannot be null or empty");
                return;
            }
            
            if (_registeredConditions.ContainsKey(conditionId))
            {
                Debug.LogWarning($"Condition with ID '{conditionId}' is already registered. Overwriting.");
            }
            
            _registeredConditions[conditionId] = condition;
            _registeredConditionTypes[conditionId] = condition.GetType();
            
            Debug.Log($"Registered condition: {condition.DisplayName} (ID: {conditionId})");
        }
        
        /// <summary>
        /// 获取条件实例
        /// </summary>
        /// <param name="conditionId">条件ID</param>
        /// <returns>条件实例</returns>
        public IEventCondition GetCondition(string conditionId)
        {
            if (string.IsNullOrEmpty(conditionId))
                return null;
                
            _registeredConditions.TryGetValue(conditionId, out IEventCondition condition);
            return condition;
        }
        
        /// <summary>
        /// 创建条件实例
        /// </summary>
        /// <param name="conditionId">条件ID</param>
        /// <returns>新的条件实例</returns>
        public IEventCondition CreateCondition(string conditionId)
        {
            if (string.IsNullOrEmpty(conditionId))
                return null;
                
            if (_registeredConditionTypes.TryGetValue(conditionId, out Type conditionType))
            {
                return (IEventCondition)Activator.CreateInstance(conditionType);
            }
            
            return null;
        }
        
        /// <summary>
        /// 获取所有注册的条件
        /// </summary>
        /// <returns>所有注册的条件列表</returns>
        public List<IEventCondition> GetAllConditions()
        {
            return _registeredConditions.Values.ToList();
        }
        
        /// <summary>
        /// 获取所有条件ID
        /// </summary>
        /// <returns>所有条件ID列表</returns>
        public List<string> GetAllConditionIds()
        {
            return _registeredConditions.Keys.ToList();
        }
        
        /// <summary>
        /// 获取所有条件的显示名称
        /// </summary>
        /// <returns>显示名称数组</returns>
        public string[] GetAllConditionDisplayNames()
        {
            return _registeredConditions.Values.Select(c => c.DisplayName).ToArray();
        }
        
        /// <summary>
        /// 根据显示名称获取条件ID
        /// </summary>
        /// <param name="displayName">显示名称</param>
        /// <returns>条件ID</returns>
        public string GetConditionIdByDisplayName(string displayName)
        {
            foreach (var kvp in _registeredConditions)
            {
                if (kvp.Value.DisplayName == displayName)
                    return kvp.Key;
            }
            return null;
        }
        
        /// <summary>
        /// 检查条件是否已注册
        /// </summary>
        /// <param name="conditionId">条件ID</param>
        /// <returns>是否已注册</returns>
        public bool IsConditionRegistered(string conditionId)
        {
            return !string.IsNullOrEmpty(conditionId) && _registeredConditions.ContainsKey(conditionId);
        }
        
        /// <summary>
        /// 取消注册条件
        /// </summary>
        /// <param name="conditionId">条件ID</param>
        public void UnregisterCondition(string conditionId)
        {
            if (string.IsNullOrEmpty(conditionId))
                return;
                
            if (_registeredConditions.ContainsKey(conditionId))
            {
                _registeredConditions.Remove(conditionId);
                _registeredConditionTypes.Remove(conditionId);
                Debug.Log($"Unregistered condition: {conditionId}");
            }
        }
        
        /// <summary>
        /// 清除所有注册的条件
        /// </summary>
        public void ClearAllConditions()
        {
            _registeredConditions.Clear();
            _registeredConditionTypes.Clear();
        }
        
        /// <summary>
        /// 初始化内置条件
        /// </summary>
        private void InitializeBuiltInConditions()
        {
            // 注册内置条件
            RegisterCondition<BuiltInConditions.NoneCondition>();
            RegisterCondition<BuiltInConditions.HasHitCondition>();
            RegisterCondition<BuiltInConditions.BeenHitCondition>();
            RegisterCondition<BuiltInConditions.InHitStopCondition>();
            RegisterCondition<BuiltInConditions.HitCheckedCondition>();
        }
        
        /// <summary>
        /// 自动发现并注册所有实现了IEventCondition接口的类
        /// </summary>
        public void AutoRegisterConditions()
        {
            var conditionTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(assembly => assembly.GetTypes())
                .Where(type => typeof(IEventCondition).IsAssignableFrom(type) && 
                             !type.IsInterface && 
                             !type.IsAbstract &&
                             type.GetConstructor(Type.EmptyTypes) != null);
            
            foreach (var type in conditionTypes)
            {
                try
                {
                    var condition = (IEventCondition)Activator.CreateInstance(type);
                    if (!IsConditionRegistered(condition.ConditionId))
                    {
                        RegisterCondition(condition);
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"Failed to register condition type {type.Name}: {e.Message}");
                }
            }
        }
    }
} 
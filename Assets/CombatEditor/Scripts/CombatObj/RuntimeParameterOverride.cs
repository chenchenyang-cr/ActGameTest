using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.Reflection;
using System;

namespace CombatEditor
{
    /// <summary>
    /// 运行时参数覆盖系统
    /// 允许在运行时动态修改轨道参数而不影响原始ScriptableObject
    /// </summary>
    public class RuntimeParameterOverride
    {
        // 存储原始参数的备份
        private Dictionary<string, object> _originalParameters = new Dictionary<string, object>();
        
        // 存储覆盖后的参数
        private Dictionary<string, object> _overrideParameters = new Dictionary<string, object>();
        
        // 参数路径到事件对象的映射
        private Dictionary<string, AbilityEventObj> _parameterToEventObj = new Dictionary<string, AbilityEventObj>();
        
        // 是否启用覆盖
        private bool _isOverrideEnabled = true;
        
        /// <summary>
        /// 获取参数的原始值
        /// </summary>
        public T GetOriginalParameter<T>(string parameterPath)
        {
            if (_originalParameters.TryGetValue(parameterPath, out var value))
            {
                return (T)value;
            }
            return default(T);
        }
        
        /// <summary>
        /// 获取当前参数值（如果有覆盖则返回覆盖值，否则返回原始值）
        /// </summary>
        public T GetCurrentParameter<T>(string parameterPath)
        {
            if (_isOverrideEnabled && _overrideParameters.TryGetValue(parameterPath, out var overrideValue))
            {
                return (T)overrideValue;
            }
            
            if (_originalParameters.TryGetValue(parameterPath, out var originalValue))
            {
                return (T)originalValue;
            }
            
            return default(T);
        }
        
        /// <summary>
        /// 设置参数覆盖值
        /// </summary>
        public void SetParameterOverride<T>(string parameterPath, T value)
        {
            _overrideParameters[parameterPath] = value;
            ApplyOverrideToEventObj(parameterPath, value);
        }
        
        /// <summary>
        /// 移除参数覆盖，恢复到原始值
        /// </summary>
        public void RemoveParameterOverride(string parameterPath)
        {
            if (_overrideParameters.ContainsKey(parameterPath))
            {
                _overrideParameters.Remove(parameterPath);
                
                // 恢复原始值
                if (_originalParameters.TryGetValue(parameterPath, out var originalValue))
                {
                    ApplyOverrideToEventObj(parameterPath, originalValue);
                }
            }
        }
        
        /// <summary>
        /// 清除所有参数覆盖
        /// </summary>
        public void ClearAllOverrides()
        {
            foreach (var kvp in _originalParameters)
            {
                ApplyOverrideToEventObj(kvp.Key, kvp.Value);
            }
            _overrideParameters.Clear();
        }
        
        /// <summary>
        /// 启用或禁用参数覆盖
        /// </summary>
        public void SetOverrideEnabled(bool enabled)
        {
            _isOverrideEnabled = enabled;
            
            if (enabled)
            {
                // 应用所有覆盖
                foreach (var kvp in _overrideParameters)
                {
                    ApplyOverrideToEventObj(kvp.Key, kvp.Value);
                }
            }
            else
            {
                // 恢复所有原始值
                foreach (var kvp in _originalParameters)
                {
                    ApplyOverrideToEventObj(kvp.Key, kvp.Value);
                }
            }
        }
        
        /// <summary>
        /// 备份原始参数
        /// </summary>
        public void BackupOriginalParameters(string parameterPath, object value, AbilityEventObj eventObj)
        {
            if (!_originalParameters.ContainsKey(parameterPath))
            {
                _originalParameters[parameterPath] = value;
                _parameterToEventObj[parameterPath] = eventObj;
            }
        }
        
        /// <summary>
        /// 应用覆盖值到事件对象
        /// </summary>
        private void ApplyOverrideToEventObj(string parameterPath, object value)
        {
            if (_parameterToEventObj.TryGetValue(parameterPath, out var eventObj))
            {
                string fieldName = GetFieldNameFromPath(parameterPath);
                var field = eventObj.GetType().GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (field != null)
                {
                    field.SetValue(eventObj, value);
                }
            }
        }
        
        /// <summary>
        /// 从参数路径中提取字段名
        /// </summary>
        private string GetFieldNameFromPath(string parameterPath)
        {
            // 参数路径格式: "GroupIndex.AbilityIndex.EventIndex.FieldName"
            var parts = parameterPath.Split('.');
            return parts[parts.Length - 1];
        }
        
        /// <summary>
        /// 获取所有覆盖的参数路径
        /// </summary>
        public List<string> GetOverriddenParameterPaths()
        {
            return _overrideParameters.Keys.ToList();
        }
        
        /// <summary>
        /// 获取所有原始参数路径
        /// </summary>
        public List<string> GetAllParameterPaths()
        {
            return _originalParameters.Keys.ToList();
        }
        
        /// <summary>
        /// 检查参数是否有覆盖
        /// </summary>
        public bool HasOverride(string parameterPath)
        {
            return _overrideParameters.ContainsKey(parameterPath);
        }
        
        /// <summary>
        /// 获取覆盖参数的数量
        /// </summary>
        public int GetOverrideCount()
        {
            return _overrideParameters.Count;
        }
        
        /// <summary>
        /// 获取总参数数量
        /// </summary>
        public int GetTotalParameterCount()
        {
            return _originalParameters.Count;
        }
    }
    
    /// <summary>
    /// 运行时参数管理器
    /// 管理CombatController的所有参数覆盖
    /// </summary>
    public class RuntimeParameterManager : MonoBehaviour
    {
        [Header("调试信息")]
        [SerializeField] private bool _debugMode = false;
        [SerializeField] private int _totalParameterCount = 0;
        [SerializeField] private int _overrideCount = 0;
        
        private CombatController _combatController;
        private RuntimeParameterOverride _parameterOverride;
        
        // 初始化状态
        private bool _isInitialized = false;
        
        void Start()
        {
            Initialize();
        }
        
        /// <summary>
        /// 初始化参数管理器
        /// </summary>
        public void Initialize()
        {
            if (_isInitialized) return;
            
            _combatController = GetComponent<CombatController>();
            if (_combatController == null)
            {
                Debug.LogError("RuntimeParameterManager: 找不到CombatController组件！");
                return;
            }
            
            _parameterOverride = new RuntimeParameterOverride();
            BackupAllParameters();
            _isInitialized = true;
            
            if (_debugMode)
            {
                Debug.Log($"RuntimeParameterManager: 初始化完成，备份了 {_totalParameterCount} 个参数");
            }
        }
        
        /// <summary>
        /// 备份所有参数到覆盖系统
        /// </summary>
        private void BackupAllParameters()
        {
            if (_combatController.CombatDatas == null) return;
            
            int parameterCount = 0;
            
            for (int groupIndex = 0; groupIndex < _combatController.CombatDatas.Count; groupIndex++)
            {
                var group = _combatController.CombatDatas[groupIndex];
                if (group.CombatObjs == null) continue;
                
                for (int abilityIndex = 0; abilityIndex < group.CombatObjs.Count; abilityIndex++)
                {
                    var ability = group.CombatObjs[abilityIndex];
                    if (ability == null || ability.events == null) continue;
                    
                    for (int eventIndex = 0; eventIndex < ability.events.Count; eventIndex++)
                    {
                        var abilityEvent = ability.events[eventIndex];
                        if (abilityEvent.Obj == null) continue;
                        
                        // 备份AbilityEvent本身的参数
                        BackupAbilityEventParameters(groupIndex, abilityIndex, eventIndex, abilityEvent);
                        
                        // 备份AbilityEventObj的参数
                        BackupEventObjParameters(groupIndex, abilityIndex, eventIndex, abilityEvent.Obj);
                        
                        parameterCount++;
                    }
                }
            }
            
            _totalParameterCount = parameterCount;
        }
        
        /// <summary>
        /// 备份AbilityEvent参数
        /// </summary>
        private void BackupAbilityEventParameters(int groupIndex, int abilityIndex, int eventIndex, AbilityEvent abilityEvent)
        {
            string basePath = $"{groupIndex}.{abilityIndex}.{eventIndex}";
            
            // 备份时间参数
            _parameterOverride.BackupOriginalParameters($"{basePath}.EventTime", abilityEvent.EventTime, abilityEvent.Obj);
            _parameterOverride.BackupOriginalParameters($"{basePath}.EventRange", abilityEvent.EventRange, abilityEvent.Obj);
            _parameterOverride.BackupOriginalParameters($"{basePath}.EventMultiRange", abilityEvent.EventMultiRange?.Clone(), abilityEvent.Obj);
            _parameterOverride.BackupOriginalParameters($"{basePath}.Previewable", abilityEvent.Previewable, abilityEvent.Obj);
        }
        
        /// <summary>
        /// 备份AbilityEventObj参数
        /// </summary>
        private void BackupEventObjParameters(int groupIndex, int abilityIndex, int eventIndex, AbilityEventObj eventObj)
        {
            string basePath = $"{groupIndex}.{abilityIndex}.{eventIndex}";
            
            // 使用反射获取所有公共字段
            var fields = eventObj.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance);
            
            foreach (var field in fields)
            {
                // 跳过Unity系统字段
                if (field.Name.StartsWith("m_") || field.Name == "name" || field.Name == "hideFlags")
                    continue;
                
                try
                {
                    object value = field.GetValue(eventObj);
                    
                    // 对于值类型和字符串直接备份，对于引用类型需要特殊处理
                    if (value != null && (field.FieldType.IsValueType || field.FieldType == typeof(string)))
                    {
                        _parameterOverride.BackupOriginalParameters($"{basePath}.{field.Name}", value, eventObj);
                    }
                    else if (value != null)
                    {
                        // 对于复杂类型，可以考虑序列化备份
                        _parameterOverride.BackupOriginalParameters($"{basePath}.{field.Name}", value, eventObj);
                    }
                }
                catch (Exception ex)
                {
                    if (_debugMode)
                    {
                        Debug.LogWarning($"备份参数失败 {basePath}.{field.Name}: {ex.Message}");
                    }
                }
            }
        }
        
        #region 公共API
        
        /// <summary>
        /// 设置参数覆盖值
        /// </summary>
        /// <param name="groupIndex">组索引</param>
        /// <param name="abilityIndex">能力索引</param>
        /// <param name="eventIndex">事件索引</param>
        /// <param name="parameterName">参数名称</param>
        /// <param name="value">新值</param>
        public void SetParameterOverride<T>(int groupIndex, int abilityIndex, int eventIndex, string parameterName, T value)
        {
            if (!_isInitialized) Initialize();
            
            string parameterPath = $"{groupIndex}.{abilityIndex}.{eventIndex}.{parameterName}";
            _parameterOverride.SetParameterOverride(parameterPath, value);
            
            UpdateDebugInfo();
            
            if (_debugMode)
            {
                Debug.Log($"设置参数覆盖: {parameterPath} = {value}");
            }
        }
        
        /// <summary>
        /// 获取当前参数值
        /// </summary>
        public T GetCurrentParameter<T>(int groupIndex, int abilityIndex, int eventIndex, string parameterName)
        {
            if (!_isInitialized) Initialize();
            
            string parameterPath = $"{groupIndex}.{abilityIndex}.{eventIndex}.{parameterName}";
            return _parameterOverride.GetCurrentParameter<T>(parameterPath);
        }
        
        /// <summary>
        /// 获取原始参数值
        /// </summary>
        public T GetOriginalParameter<T>(int groupIndex, int abilityIndex, int eventIndex, string parameterName)
        {
            if (!_isInitialized) Initialize();
            
            string parameterPath = $"{groupIndex}.{abilityIndex}.{eventIndex}.{parameterName}";
            return _parameterOverride.GetOriginalParameter<T>(parameterPath);
        }
        
        /// <summary>
        /// 移除参数覆盖
        /// </summary>
        public void RemoveParameterOverride(int groupIndex, int abilityIndex, int eventIndex, string parameterName)
        {
            if (!_isInitialized) Initialize();
            
            string parameterPath = $"{groupIndex}.{abilityIndex}.{eventIndex}.{parameterName}";
            _parameterOverride.RemoveParameterOverride(parameterPath);
            
            UpdateDebugInfo();
            
            if (_debugMode)
            {
                Debug.Log($"移除参数覆盖: {parameterPath}");
            }
        }
        
        /// <summary>
        /// 清除所有参数覆盖
        /// </summary>
        public void ClearAllOverrides()
        {
            if (!_isInitialized) Initialize();
            
            _parameterOverride.ClearAllOverrides();
            UpdateDebugInfo();
            
            if (_debugMode)
            {
                Debug.Log("清除所有参数覆盖");
            }
        }
        
        /// <summary>
        /// 启用或禁用参数覆盖系统
        /// </summary>
        public void SetOverrideEnabled(bool enabled)
        {
            if (!_isInitialized) Initialize();
            
            _parameterOverride.SetOverrideEnabled(enabled);
            
            if (_debugMode)
            {
                Debug.Log($"参数覆盖系统 {(enabled ? "启用" : "禁用")}");
            }
        }
        
        /// <summary>
        /// 检查参数是否有覆盖
        /// </summary>
        public bool HasOverride(int groupIndex, int abilityIndex, int eventIndex, string parameterName)
        {
            if (!_isInitialized) Initialize();
            
            string parameterPath = $"{groupIndex}.{abilityIndex}.{eventIndex}.{parameterName}";
            return _parameterOverride.HasOverride(parameterPath);
        }
        
        /// <summary>
        /// 获取所有覆盖的参数路径
        /// </summary>
        public List<string> GetOverriddenParameterPaths()
        {
            if (!_isInitialized) Initialize();
            
            return _parameterOverride.GetOverriddenParameterPaths();
        }
        
        /// <summary>
        /// 获取覆盖参数的数量
        /// </summary>
        public int GetOverrideCount()
        {
            if (!_isInitialized) Initialize();
            
            return _parameterOverride.GetOverrideCount();
        }
        
        /// <summary>
        /// 获取总参数数量
        /// </summary>
        public int GetTotalParameterCount()
        {
            if (!_isInitialized) Initialize();
            
            return _parameterOverride.GetTotalParameterCount();
        }
        
        /// <summary>
        /// 按动作名称设置参数覆盖（方便使用的API）
        /// </summary>
        public void SetParameterOverrideByAbilityName<T>(string abilityName, int eventIndex, string parameterName, T value)
        {
            if (!_isInitialized) Initialize();
            
            var indices = FindAbilityByName(abilityName);
            if (indices.HasValue)
            {
                SetParameterOverride(indices.Value.groupIndex, indices.Value.abilityIndex, eventIndex, parameterName, value);
            }
            else
            {
                Debug.LogWarning($"找不到名为 '{abilityName}' 的能力");
            }
        }
        
        /// <summary>
        /// 按动作名称移除参数覆盖
        /// </summary>
        public void RemoveParameterOverrideByAbilityName(string abilityName, int eventIndex, string parameterName)
        {
            if (!_isInitialized) Initialize();
            
            var indices = FindAbilityByName(abilityName);
            if (indices.HasValue)
            {
                RemoveParameterOverride(indices.Value.groupIndex, indices.Value.abilityIndex, eventIndex, parameterName);
            }
            else
            {
                Debug.LogWarning($"找不到名为 '{abilityName}' 的能力");
            }
        }
        
        #endregion
        
        #region 辅助方法
        
        /// <summary>
        /// 通过名称查找能力的索引
        /// </summary>
        private (int groupIndex, int abilityIndex)? FindAbilityByName(string abilityName)
        {
            for (int groupIndex = 0; groupIndex < _combatController.CombatDatas.Count; groupIndex++)
            {
                var group = _combatController.CombatDatas[groupIndex];
                if (group.CombatObjs == null) continue;
                
                for (int abilityIndex = 0; abilityIndex < group.CombatObjs.Count; abilityIndex++)
                {
                    var ability = group.CombatObjs[abilityIndex];
                    if (ability != null && ability.name == abilityName)
                    {
                        return (groupIndex, abilityIndex);
                    }
                }
            }
            return null;
        }
        
        /// <summary>
        /// 更新调试信息
        /// </summary>
        private void UpdateDebugInfo()
        {
            if (_parameterOverride != null)
            {
                _overrideCount = _parameterOverride.GetOverrideCount();
                _totalParameterCount = _parameterOverride.GetTotalParameterCount();
            }
        }
        
        #endregion
        
        #region Unity消息
        
        void Update()
        {
            if (_debugMode && _isInitialized)
            {
                UpdateDebugInfo();
            }
        }
        
        void OnDestroy()
        {
            if (_isInitialized)
            {
                ClearAllOverrides();
            }
        }
        
        #endregion
    }
} 
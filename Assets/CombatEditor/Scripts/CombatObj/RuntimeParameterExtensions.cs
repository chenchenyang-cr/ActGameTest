using System.Collections.Generic;
using UnityEngine;

namespace CombatEditor
{
    /// <summary>
    /// 运行时参数覆盖系统的扩展方法
    /// 提供更便于使用的API
    /// </summary>
    public static class RuntimeParameterExtensions
    {
        /// <summary>
        /// 为CombatController添加运行时参数管理器
        /// </summary>
        public static RuntimeParameterManager GetOrAddRuntimeParameterManager(this CombatController combatController)
        {
            var manager = combatController.GetComponent<RuntimeParameterManager>();
            if (manager == null)
            {
                manager = combatController.gameObject.AddComponent<RuntimeParameterManager>();
            }
            return manager;
        }
        
        /// <summary>
        /// 获取运行时参数包装器（面向对象的参数访问方式）
        /// </summary>
        public static RuntimeParameterWrapper GetRuntimeParameterWrapper(this CombatController combatController)
        {
            return new RuntimeParameterWrapper(combatController);
        }
        
        /// <summary>
        /// 批量设置参数覆盖
        /// </summary>
        public static void SetParameterOverrideBatch(this RuntimeParameterManager manager, 
            Dictionary<string, object> parameterOverrides)
        {
            foreach (var kvp in parameterOverrides)
            {
                var pathParts = kvp.Key.Split('.');
                if (pathParts.Length >= 4)
                {
                    if (int.TryParse(pathParts[0], out int groupIndex) &&
                        int.TryParse(pathParts[1], out int abilityIndex) &&
                        int.TryParse(pathParts[2], out int eventIndex))
                    {
                        string parameterName = pathParts[3];
                        manager.SetParameterOverride(groupIndex, abilityIndex, eventIndex, parameterName, kvp.Value);
                    }
                }
            }
        }
        
        /// <summary>
        /// 获取当前所有覆盖参数的详细信息
        /// </summary>
        public static Dictionary<string, object> GetAllOverrideDetails(this RuntimeParameterManager manager)
        {
            var result = new Dictionary<string, object>();
            var overriddenPaths = manager.GetOverriddenParameterPaths();
            
            foreach (var path in overriddenPaths)
            {
                var pathParts = path.Split('.');
                if (pathParts.Length >= 4)
                {
                    if (int.TryParse(pathParts[0], out int groupIndex) &&
                        int.TryParse(pathParts[1], out int abilityIndex) &&
                        int.TryParse(pathParts[2], out int eventIndex))
                    {
                        string parameterName = pathParts[3];
                        var currentValue = manager.GetCurrentParameter<object>(groupIndex, abilityIndex, eventIndex, parameterName);
                        result[path] = currentValue;
                    }
                }
            }
            
            return result;
        }
    }
    
    /// <summary>
    /// 运行时参数覆盖的预设配置
    /// </summary>
    [System.Serializable]
    public class RuntimeParameterPreset
    {
        [System.Serializable]
        public class ParameterOverride
        {
            public int groupIndex;
            public int abilityIndex;
            public int eventIndex;
            public string parameterName;
            public string valueType; // "float", "int", "bool", "string", "Vector3"
            public string serializedValue;
            
            public object GetValue()
            {
                switch (valueType.ToLower())
                {
                    case "float":
                        return float.TryParse(serializedValue, out float f) ? f : 0f;
                    case "int":
                        return int.TryParse(serializedValue, out int i) ? i : 0;
                    case "bool":
                        return bool.TryParse(serializedValue, out bool b) ? b : false;
                    case "string":
                        return serializedValue ?? "";
                    case "vector3":
                        return ParseVector3(serializedValue);
                    default:
                        return serializedValue;
                }
            }
            
            private Vector3 ParseVector3(string value)
            {
                if (string.IsNullOrEmpty(value)) return Vector3.zero;
                
                var parts = value.Split(',');
                if (parts.Length >= 3)
                {
                    float.TryParse(parts[0], out float x);
                    float.TryParse(parts[1], out float y);
                    float.TryParse(parts[2], out float z);
                    return new Vector3(x, y, z);
                }
                return Vector3.zero;
            }
        }
        
        [Header("预设信息")]
        public string presetName = "默认预设";
        public string description = "";
        
        [Header("参数覆盖")]
        public List<ParameterOverride> parameterOverrides = new List<ParameterOverride>();
        
        /// <summary>
        /// 应用预设到运行时参数管理器
        /// </summary>
        public void ApplyToManager(RuntimeParameterManager manager)
        {
            foreach (var paramOverride in parameterOverrides)
            {
                manager.SetParameterOverride(
                    paramOverride.groupIndex,
                    paramOverride.abilityIndex,
                    paramOverride.eventIndex,
                    paramOverride.parameterName,
                    paramOverride.GetValue()
                );
            }
        }
        
        /// <summary>
        /// 从运行时参数管理器创建预设
        /// </summary>
        public static RuntimeParameterPreset CreateFromManager(RuntimeParameterManager manager, string presetName)
        {
            var preset = new RuntimeParameterPreset
            {
                presetName = presetName,
                description = $"从运行时管理器创建于 {System.DateTime.Now:yyyy-MM-dd HH:mm:ss}"
            };
            
            var overriddenPaths = manager.GetOverriddenParameterPaths();
            foreach (var path in overriddenPaths)
            {
                var pathParts = path.Split('.');
                if (pathParts.Length >= 4)
                {
                    if (int.TryParse(pathParts[0], out int groupIndex) &&
                        int.TryParse(pathParts[1], out int abilityIndex) &&
                        int.TryParse(pathParts[2], out int eventIndex))
                    {
                        string parameterName = pathParts[3];
                        var currentValue = manager.GetCurrentParameter<object>(groupIndex, abilityIndex, eventIndex, parameterName);
                        
                        var paramOverride = new ParameterOverride
                        {
                            groupIndex = groupIndex,
                            abilityIndex = abilityIndex,
                            eventIndex = eventIndex,
                            parameterName = parameterName,
                            valueType = GetValueType(currentValue),
                            serializedValue = SerializeValue(currentValue)
                        };
                        
                        preset.parameterOverrides.Add(paramOverride);
                    }
                }
            }
            
            return preset;
        }
        
        private static string GetValueType(object value)
        {
            if (value == null) return "string";
            
            var type = value.GetType();
            if (type == typeof(float)) return "float";
            if (type == typeof(int)) return "int";
            if (type == typeof(bool)) return "bool";
            if (type == typeof(string)) return "string";
            if (type == typeof(Vector3)) return "vector3";
            
            return "string";
        }
        
        private static string SerializeValue(object value)
        {
            if (value == null) return "";
            
            if (value is Vector3 v3)
            {
                return $"{v3.x},{v3.y},{v3.z}";
            }
            
            return value.ToString();
        }
    }
    
    /// <summary>
    /// 运行时参数覆盖的便捷组件
    /// 提供Inspector界面的快速操作
    /// </summary>
    [System.Serializable]
    public class RuntimeParameterHelper : MonoBehaviour
    {
        [Header("目标")]
        public CombatController targetCombatController;
        
        [Header("预设")]
        public List<RuntimeParameterPreset> presets = new List<RuntimeParameterPreset>();
        
        [Header("当前操作")]
        public RuntimeParameterPreset currentPreset;
        
        private RuntimeParameterManager _manager;
        
        void Start()
        {
            if (targetCombatController == null)
            {
                targetCombatController = GetComponent<CombatController>();
            }
            
            if (targetCombatController != null)
            {
                _manager = targetCombatController.GetOrAddRuntimeParameterManager();
            }
        }
        
        /// <summary>
        /// 应用预设
        /// </summary>
        [ContextMenu("应用当前预设")]
        public void ApplyCurrentPreset()
        {
            if (_manager != null && currentPreset != null)
            {
                currentPreset.ApplyToManager(_manager);
                Debug.Log($"应用预设: {currentPreset.presetName}");
            }
        }
        
        /// <summary>
        /// 清除所有覆盖
        /// </summary>
        [ContextMenu("清除所有覆盖")]
        public void ClearAllOverrides()
        {
            if (_manager != null)
            {
                _manager.ClearAllOverrides();
                Debug.Log("清除所有参数覆盖");
            }
        }
        
        /// <summary>
        /// 从当前状态创建预设
        /// </summary>
        [ContextMenu("从当前状态创建预设")]
        public void CreatePresetFromCurrent()
        {
            if (_manager != null)
            {
                var preset = RuntimeParameterPreset.CreateFromManager(_manager, "新预设");
                presets.Add(preset);
                Debug.Log($"创建预设: {preset.presetName}，包含 {preset.parameterOverrides.Count} 个参数覆盖");
            }
        }
        
        /// <summary>
        /// 应用指定预设
        /// </summary>
        public void ApplyPreset(int presetIndex)
        {
            if (_manager != null && presetIndex >= 0 && presetIndex < presets.Count)
            {
                presets[presetIndex].ApplyToManager(_manager);
                Debug.Log($"应用预设: {presets[presetIndex].presetName}");
            }
        }
        
        /// <summary>
        /// 应用指定预设
        /// </summary>
        public void ApplyPreset(string presetName)
        {
            if (_manager != null)
            {
                var preset = presets.Find(p => p.presetName == presetName);
                if (preset != null)
                {
                    preset.ApplyToManager(_manager);
                    Debug.Log($"应用预设: {preset.presetName}");
                }
                else
                {
                    Debug.LogWarning($"找不到名为 '{presetName}' 的预设");
                }
            }
        }
        
        /// <summary>
        /// 获取运行时参数管理器
        /// </summary>
        public RuntimeParameterManager GetManager()
        {
            return _manager;
        }
    }
} 
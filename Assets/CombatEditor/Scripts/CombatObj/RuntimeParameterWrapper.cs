using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.Reflection;

namespace CombatEditor
{
    /// <summary>
    /// 运行时参数包装器 - 提供面向对象的参数访问方式
    /// </summary>
    public class RuntimeParameterWrapper
    {
        private CombatController _combatController;
        private RuntimeParameterManager _parameterManager;
        private List<RuntimeAction> _actions;

        public RuntimeParameterWrapper(CombatController combatController)
        {
            _combatController = combatController;
            _parameterManager = combatController.GetOrAddRuntimeParameterManager();
            RefreshActions();
        }

        /// <summary>
        /// 刷新动作列表
        /// </summary>
        public void RefreshActions()
        {
            _actions = new List<RuntimeAction>();

            for (int groupIndex = 0; groupIndex < _combatController.CombatDatas.Count; groupIndex++)
            {
                var group = _combatController.CombatDatas[groupIndex];
                for (int abilityIndex = 0; abilityIndex < group.CombatObjs.Count; abilityIndex++)
                {
                    var ability = group.CombatObjs[abilityIndex];
                    if (ability != null)
                    {
                        var action = new RuntimeAction(ability, groupIndex, abilityIndex, _parameterManager);
                        _actions.Add(action);
                    }
                }
            }
        }

        /// <summary>
        /// 获取所有动作
        /// </summary>
        public List<RuntimeAction> GetActions()
        {
            return _actions;
        }

        /// <summary>
        /// 通过名称获取动作（优先使用Group Label，然后使用ScriptableObject名称）
        /// </summary>
        public RuntimeAction GetAction(string actionName)
        {
            // 优先通过Group Label查找（CombatEditor界面显示的名称）
            var actionByGroupLabel = GetActionByGroupLabel(actionName);
            if (actionByGroupLabel != null)
            {
                return actionByGroupLabel;
            }

            // 如果Group Label找不到，再通过ScriptableObject名称查找（向后兼容）
            return _actions.FirstOrDefault(a => a.Name == actionName);
        }

        /// <summary>
        /// 通过索引获取动作
        /// </summary>
        public RuntimeAction GetAction(int index)
        {
            return index >= 0 && index < _actions.Count ? _actions[index] : null;
        }

        /// <summary>
        /// 获取包含指定文本的动作
        /// </summary>
        public List<RuntimeAction> GetActionsContaining(string text)
        {
            return _actions.Where(a => a.Name.Contains(text)).ToList();
        }

        /// <summary>
        /// 通过CombatGroup的Label获取该组的第一个动作
        /// </summary>
        public RuntimeAction GetActionByGroupLabel(string groupLabel)
        {
            for (int groupIndex = 0; groupIndex < _combatController.CombatDatas.Count; groupIndex++)
            {
                var group = _combatController.CombatDatas[groupIndex];
                if (group.Label == groupLabel && group.CombatObjs != null && group.CombatObjs.Count > 0)
                {
                    var ability = group.CombatObjs[0]; // 获取第一个动作
                    if (ability != null)
                    {
                        return _actions.FirstOrDefault(a => a.GroupIndex == groupIndex && a.AbilityIndex == 0);
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// 通过CombatGroup的Label获取该组的所有动作
        /// </summary>
        public List<RuntimeAction> GetActionsByGroupLabel(string groupLabel)
        {
            var groupActions = new List<RuntimeAction>();

            for (int groupIndex = 0; groupIndex < _combatController.CombatDatas.Count; groupIndex++)
            {
                var group = _combatController.CombatDatas[groupIndex];
                if (group.Label == groupLabel)
                {
                    var actionsInGroup = _actions.Where(a => a.GroupIndex == groupIndex).ToList();
                    groupActions.AddRange(actionsInGroup);
                    break;
                }
            }

            return groupActions;
        }

        /// <summary>
        /// 通过CombatGroup的Label和动作索引获取特定动作
        /// </summary>
        public RuntimeAction GetActionByGroupLabel(string groupLabel, int abilityIndex)
        {
            for (int groupIndex = 0; groupIndex < _combatController.CombatDatas.Count; groupIndex++)
            {
                var group = _combatController.CombatDatas[groupIndex];
                if (group.Label == groupLabel)
                {
                    return _actions.FirstOrDefault(a => a.GroupIndex == groupIndex && a.AbilityIndex == abilityIndex);
                }
            }

            return null;
        }

        /// <summary>
        /// 获取所有组标签
        /// </summary>
        public List<string> GetAllGroupLabels()
        {
            return _combatController.CombatDatas.Select(group => group.Label).ToList();
        }

        /// <summary>
        /// 清除所有参数覆盖
        /// </summary>
        public void ClearAllOverrides()
        {
            _parameterManager.ClearAllOverrides();
        }

        /// <summary>
        /// 获取覆盖状态信息
        /// </summary>
        public RuntimeOverrideInfo GetOverrideInfo()
        {
            return new RuntimeOverrideInfo(_parameterManager);
        }


        /// <summary>
        /// 运行时动作包装器
        /// </summary>
        public class RuntimeAction
        {
            private AbilityScriptableObject _originalAbility;
            private AbilityScriptableObject _runtimeCopy;
            private int _groupIndex;
            private int _abilityIndex;
            private RuntimeParameterManager _parameterManager;
            private List<RuntimeTrack> _tracks;

            public string Name => _originalAbility.name;
            public AbilityScriptableObject Ability => _runtimeCopy; // 返回运行时副本
            public AbilityScriptableObject OriginalAbility => _originalAbility; // 返回原始对象
            public int GroupIndex => _groupIndex;
            public int AbilityIndex => _abilityIndex;
            public bool IsUsingRuntimeCopy => _runtimeCopy != null;

            public RuntimeAction(AbilityScriptableObject ability, int groupIndex, int abilityIndex,
                RuntimeParameterManager parameterManager)
            {
                _originalAbility = ability;
                _groupIndex = groupIndex;
                _abilityIndex = abilityIndex;
                _parameterManager = parameterManager;

                // 在运行时模式下创建副本
                if (Application.isPlaying)
                {
                    Debug.Log($"✅ 为动作 '{ability.name}' 创建了运行时副本");
                }
                else
                {
                    _runtimeCopy = ability; // 编辑器模式下不创建副本
                }

                RefreshTracks();
            }

            /// <summary>
            /// 刷新轨道列表
            /// </summary>
            public void RefreshTracks()
            {
                _tracks = new List<RuntimeTrack>();

                var abilityToUse = _runtimeCopy ?? _originalAbility;

                for (int eventIndex = 0; eventIndex < abilityToUse.events.Count; eventIndex++)
                {
                    var abilityEvent = abilityToUse.events[eventIndex];
                    if (abilityEvent.Obj != null)
                    {
                        var track = new RuntimeTrack(abilityEvent, _groupIndex, _abilityIndex, eventIndex,
                            _parameterManager);
                        _tracks.Add(track);
                    }
                }
            }

            /// <summary>
            /// 获取所有轨道
            /// </summary>
            public List<RuntimeTrack> GetTracks()
            {
                return _tracks;
            }

            /// <summary>
            /// 通过索引获取轨道
            /// </summary>
            public RuntimeTrack GetTrack(int index)
            {
                return index >= 0 && index < _tracks.Count ? _tracks[index] : null;
            }

            /// <summary>
            /// 通过轨道类型获取轨道
            /// </summary>
            public RuntimeTrack GetTrack<T>() where T : AbilityEventObj
            {
                return _tracks.FirstOrDefault(t => t.EventObj is T);
            }

            /// <summary>
            /// 获取指定类型的所有轨道
            /// </summary>
            public List<RuntimeTrack> GetTracks<T>() where T : AbilityEventObj
            {
                return _tracks.Where(t => t.EventObj is T).ToList();
            }

            /// <summary>
            /// 通过轨道名称获取轨道
            /// </summary>
            public RuntimeTrack GetTrackByName(string trackName)
            {
                return _tracks.FirstOrDefault(t => t.Name == trackName);
            }

            /// <summary>
            /// 清除该动作的所有参数覆盖
            /// </summary>
            public void ClearOverrides()
            {
                foreach (var track in _tracks)
                {
                    track.ClearOverrides();
                }
            }

            /// <summary>
            /// 获取该动作的覆盖参数数量
            /// </summary>
            public int GetOverrideCount()
            {
                return _tracks.Sum(t => t.GetOverrideCount());
            }

            /// <summary>
            /// 重置运行时副本（重新从原始对象创建副本）
            /// </summary>
            public void ResetRuntimeCopy()
            {
                if (Application.isPlaying && _originalAbility != null)
                {
                    RefreshTracks();
                    Debug.Log($"🔄 重置了动作 '{_originalAbility.name}' 的运行时副本");
                }
            }

            /// <summary>
            /// 获取运行时副本状态信息
            /// </summary>
            public string GetRuntimeCopyInfo()
            {
                if (!Application.isPlaying)
                {
                    return "编辑器模式 - 直接操作原始对象";
                }

                if (_runtimeCopy != null && _runtimeCopy != _originalAbility)
                {
                    return $"运行时模式 - 使用副本 '{_runtimeCopy.name}'（安全模式）";
                }

                return "运行时模式 - 直接操作原始对象（不安全）";
            }
        }

        /// <summary>
        /// 运行时轨道包装器
        /// </summary>
        public class RuntimeTrack
        {
            private AbilityEvent _abilityEvent;
            private int _groupIndex;
            private int _abilityIndex;
            private int _eventIndex;
            private RuntimeParameterManager _parameterManager;
            private List<RuntimeParameter> _parameters;

            public string Name => _abilityEvent.Obj.name;
            public AbilityEventObj EventObj => _abilityEvent.Obj;
            public AbilityEvent Event => _abilityEvent;
            public int GroupIndex => _groupIndex;
            public int AbilityIndex => _abilityIndex;
            public int EventIndex => _eventIndex;
            public string TypeName => _abilityEvent.Obj.GetType().Name;

            public RuntimeTrack(AbilityEvent abilityEvent, int groupIndex, int abilityIndex, int eventIndex,
                RuntimeParameterManager parameterManager)
            {
                _abilityEvent = abilityEvent;
                _groupIndex = groupIndex;
                _abilityIndex = abilityIndex;
                _eventIndex = eventIndex;
                _parameterManager = parameterManager;
                RefreshParameters();
            }

            /// <summary>
            /// 刷新参数列表
            /// </summary>
            public void RefreshParameters()
            {
                _parameters = new List<RuntimeParameter>();

                // 添加AbilityEvent的基础参数
                _parameters.Add(new RuntimeParameter("EventTime", typeof(float), _groupIndex, _abilityIndex,
                    _eventIndex, _parameterManager));
                _parameters.Add(new RuntimeParameter("EventRange", typeof(Vector2), _groupIndex, _abilityIndex,
                    _eventIndex, _parameterManager));
                _parameters.Add(new RuntimeParameter("EventMultiRange", typeof(float[]), _groupIndex, _abilityIndex,
                    _eventIndex, _parameterManager));
                _parameters.Add(new RuntimeParameter("Previewable", typeof(bool), _groupIndex, _abilityIndex,
                    _eventIndex, _parameterManager));

                // 添加AbilityEventObj的参数
                var eventObjType = _abilityEvent.Obj.GetType();
                var fields = eventObjType.GetFields(BindingFlags.Public | BindingFlags.Instance);

                foreach (var field in fields)
                {
                    // 跳过Unity系统字段
                    if (field.Name.StartsWith("m_") || field.Name == "name" || field.Name == "hideFlags")
                        continue;

                    _parameters.Add(new RuntimeParameter(field.Name, field.FieldType, _groupIndex, _abilityIndex,
                        _eventIndex, _parameterManager));
                }
            }

            /// <summary>
            /// 获取所有参数
            /// </summary>
            public List<RuntimeParameter> GetParameters()
            {
                return _parameters;
            }

            /// <summary>
            /// 通过名称获取参数
            /// </summary>
            public RuntimeParameter GetParameter(string parameterName)
            {
                return _parameters.FirstOrDefault(p => p.Name == parameterName);
            }

            /// <summary>
            /// 设置参数值
            /// </summary>
            public void SetParameter<T>(string parameterName, T value)
            {
                var parameter = GetParameter(parameterName);
                if (parameter != null)
                {
                    parameter.SetValue(value);
                }
                else
                {
                    Debug.LogWarning($"轨道 {Name} 中找不到参数 {parameterName}");
                }
            }

            /// <summary>
            /// 获取参数值
            /// </summary>
            public T GetParameter<T>(string parameterName)
            {
                var parameter = GetParameter(parameterName);
                if (parameter != null)
                {
                    return parameter.GetValue<T>();
                }
                else
                {
                    Debug.LogWarning($"轨道 {Name} 中找不到参数 {parameterName}");
                    return default(T);
                }
            }

            /// <summary>
            /// 获取原始参数值
            /// </summary>
            public T GetOriginalParameter<T>(string parameterName)
            {
                var parameter = GetParameter(parameterName);
                if (parameter != null)
                {
                    return parameter.GetOriginalValue<T>();
                }
                else
                {
                    Debug.LogWarning($"轨道 {Name} 中找不到参数 {parameterName}");
                    return default(T);
                }
            }

            /// <summary>
            /// 检查参数是否有覆盖
            /// </summary>
            public bool HasOverride(string parameterName)
            {
                var parameter = GetParameter(parameterName);
                return parameter?.HasOverride() ?? false;
            }

            /// <summary>
            /// 移除参数覆盖
            /// </summary>
            public void RemoveOverride(string parameterName)
            {
                var parameter = GetParameter(parameterName);
                parameter?.RemoveOverride();
            }

            /// <summary>
            /// 清除该轨道的所有参数覆盖
            /// </summary>
            public void ClearOverrides()
            {
                foreach (var parameter in _parameters)
                {
                    parameter.RemoveOverride();
                }
            }

            /// <summary>
            /// 获取该轨道的覆盖参数数量
            /// </summary>
            public int GetOverrideCount()
            {
                return _parameters.Count(p => p.HasOverride());
            }

            /// <summary>
            /// 获取参数类型信息
            /// </summary>
            public Dictionary<string, System.Type> GetParameterTypes()
            {
                return _parameters.ToDictionary(p => p.Name, p => p.Type);
            }
        }

        /// <summary>
        /// 运行时参数包装器
        /// </summary>
        public class RuntimeParameter
        {
            private string _name;
            private System.Type _type;
            private int _groupIndex;
            private int _abilityIndex;
            private int _eventIndex;
            private RuntimeParameterManager _parameterManager;

            public string Name => _name;
            public System.Type Type => _type;
            public int GroupIndex => _groupIndex;
            public int AbilityIndex => _abilityIndex;
            public int EventIndex => _eventIndex;

            public RuntimeParameter(string name, System.Type type, int groupIndex, int abilityIndex, int eventIndex,
                RuntimeParameterManager parameterManager)
            {
                _name = name;
                _type = type;
                _groupIndex = groupIndex;
                _abilityIndex = abilityIndex;
                _eventIndex = eventIndex;
                _parameterManager = parameterManager;
            }

            /// <summary>
            /// 设置参数值
            /// </summary>
            public void SetValue<T>(T value)
            {
                _parameterManager.SetParameterOverride(_groupIndex, _abilityIndex, _eventIndex, _name, value);
            }

            /// <summary>
            /// 获取当前参数值
            /// </summary>
            public T GetValue<T>()
            {
                return _parameterManager.GetCurrentParameter<T>(_groupIndex, _abilityIndex, _eventIndex, _name);
            }

            /// <summary>
            /// 获取原始参数值
            /// </summary>
            public T GetOriginalValue<T>()
            {
                return _parameterManager.GetOriginalParameter<T>(_groupIndex, _abilityIndex, _eventIndex, _name);
            }

            /// <summary>
            /// 检查是否有覆盖
            /// </summary>
            public bool HasOverride()
            {
                return _parameterManager.HasOverride(_groupIndex, _abilityIndex, _eventIndex, _name);
            }

            /// <summary>
            /// 移除覆盖
            /// </summary>
            public void RemoveOverride()
            {
                _parameterManager.RemoveParameterOverride(_groupIndex, _abilityIndex, _eventIndex, _name);
            }

            /// <summary>
            /// 获取参数信息
            /// </summary>
            public RuntimeParameterInfo GetInfo()
            {
                return new RuntimeParameterInfo
                {
                    Name = _name,
                    Type = _type,
                    HasOverride = HasOverride(),
                    CurrentValue = GetValue<object>(),
                    OriginalValue = GetOriginalValue<object>(),
                    Path = $"{_groupIndex}.{_abilityIndex}.{_eventIndex}.{_name}"
                };
            }
        }

        /// <summary>
        /// 运行时参数信息
        /// </summary>
        public class RuntimeParameterInfo
        {
            public string Name;
            public System.Type Type;
            public bool HasOverride;
            public object CurrentValue;
            public object OriginalValue;
            public string Path;

            public override string ToString()
            {
                return $"{Name} ({Type.Name}): {CurrentValue}" + (HasOverride ? " [覆盖]" : " [原始]");
            }
        }

        /// <summary>
        /// 运行时覆盖信息
        /// </summary>
        public class RuntimeOverrideInfo
        {
            private RuntimeParameterManager _manager;

            public RuntimeOverrideInfo(RuntimeParameterManager manager)
            {
                _manager = manager;
            }

            public int TotalParameterCount => _manager.GetTotalParameterCount();
            public int OverrideCount => _manager.GetOverrideCount();
            public List<string> OverriddenPaths => _manager.GetOverriddenParameterPaths();
            public Dictionary<string, object> OverrideDetails => _manager.GetAllOverrideDetails();

            public override string ToString()
            {
                return $"参数覆盖状态: {OverrideCount}/{TotalParameterCount} 个参数被覆盖";
            }
        }
    }
}
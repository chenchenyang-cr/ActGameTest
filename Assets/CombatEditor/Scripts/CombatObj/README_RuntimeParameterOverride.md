# 运行时参数覆盖系统使用说明

## 概述

运行时参数覆盖系统允许你在游戏运行时动态修改CombatController中所有动作的轨道参数，而不会影响原始的ScriptableObject数据。这个系统特别适用于：

- 角色技能强化/弱化
- 临时buff/debuff效果
- 难度调整
- 特殊游戏模式
- 实时参数调试

## 核心特性

### ✅ 不影响原始数据
- 所有修改都是临时的，不会改变ScriptableObject文件
- 可以随时恢复到原始状态

### ✅ 完整的参数覆盖
- 支持所有轨道事件的参数
- 支持值类型（int、float、bool）和引用类型（Vector3、string等）
- 自动备份原始参数值

### ✅ 灵活的API
- 支持索引访问和名称访问
- 批量参数修改
- 预设系统
- 条件性修改

### ✅ 实时生效
- 参数修改立即生效
- 支持动态调整
- 无需重启或重新加载

## 快速开始

### 方法1：面向对象API（推荐）

```csharp
using CombatEditor;

public class MyController : MonoBehaviour
{
    private RuntimeParameterWrapper parameterWrapper;
    
    void Start()
    {
        var combatController = GetComponent<CombatController>();
        parameterWrapper = combatController.GetRuntimeParameterWrapper();
    }
    
    void ModifyParameters()
    {
        // 获取动作
        var attackAction = parameterWrapper.GetAction("Attack1");
        if (attackAction != null)
        {
            // 获取轨道
            var speedTrack = attackAction.GetTrack<AbilityEventObj_AnimSpeed>();
            if (speedTrack != null)
            {
                // 修改参数
                speedTrack.SetParameter("Speed", 2.0f);
                
                // 获取参数值
                float currentSpeed = speedTrack.GetParameter<float>("Speed");
                float originalSpeed = speedTrack.GetOriginalParameter<float>("Speed");
                
                // 检查是否有覆盖
                bool hasOverride = speedTrack.HasOverride("Speed");
                
                // 移除覆盖
                speedTrack.RemoveOverride("Speed");
            }
        }
    }
}
```

### 方法2：索引API（兼容性）

```csharp
using CombatEditor;

public class MyController : MonoBehaviour
{
    private RuntimeParameterManager parameterManager;
    
    void Start()
    {
        var combatController = GetComponent<CombatController>();
        parameterManager = combatController.GetOrAddRuntimeParameterManager();
    }
    
    void ModifyParameters()
    {
        // 修改第0组第0个能力的第0个事件的Speed参数
        parameterManager.SetParameterOverride<float>(0, 0, 0, "Speed", 2.0f);
        
        // 获取当前参数值
        float currentSpeed = parameterManager.GetCurrentParameter<float>(0, 0, 0, "Speed");
        
        // 获取原始参数值
        float originalSpeed = parameterManager.GetOriginalParameter<float>(0, 0, 0, "Speed");
        
        // 移除覆盖，恢复原始值
        parameterManager.RemoveParameterOverride(0, 0, 0, "Speed");
        
        // 清除所有覆盖
        parameterManager.ClearAllOverrides();
    }
}
```

### 3. 便捷API

```csharp
// 按动作名称修改参数
parameterManager.SetParameterOverrideByAbilityName<float>("Attack1", 0, "Speed", 1.5f);

// 批量修改参数
var parameterOverrides = new Dictionary<string, object>
{
    { "0.0.0.Speed", 2.0f },
    { "0.0.1.EventTime", 0.5f },
    { "0.1.0.hitBoxSize", new Vector3(2f, 2f, 2f) }
};
parameterManager.SetParameterOverrideBatch(parameterOverrides);
```

## 面向对象API详解

### 核心概念

面向对象API提供了更直观的参数访问方式，通过以下层次结构：

```
CombatController
├── RuntimeParameterWrapper
    ├── RuntimeAction (动作)
        ├── RuntimeTrack (轨道)
            ├── RuntimeParameter (参数)
```

### 主要类说明

#### RuntimeParameterWrapper
- **作用**: 包装器的入口点，管理所有动作
- **获取方式**: `combatController.GetRuntimeParameterWrapper()`
- **主要方法**:
  - `GetActions()`: 获取所有动作
  - `GetAction(string name)`: 按名称获取动作
  - `GetAction(int index)`: 按索引获取动作
  - `GetActionsContaining(string text)`: 获取包含指定文本的动作
  - `ClearAllOverrides()`: 清除所有覆盖

#### RuntimeAction
- **作用**: 代表一个动作（AbilityScriptableObject）
- **主要属性**: `Name`, `GroupIndex`, `AbilityIndex`
- **主要方法**:
  - `GetTracks()`: 获取所有轨道
  - `GetTrack(int index)`: 按索引获取轨道
  - `GetTrack<T>()`: 按类型获取轨道
  - `GetTracks<T>()`: 获取指定类型的所有轨道
  - `GetTrackByName(string name)`: 按名称获取轨道
  - `ClearOverrides()`: 清除该动作的所有覆盖

#### RuntimeTrack
- **作用**: 代表一个轨道（AbilityEvent）
- **主要属性**: `Name`, `TypeName`, `EventObj`
- **主要方法**:
  - `GetParameters()`: 获取所有参数
  - `GetParameter(string name)`: 获取指定参数
  - `SetParameter<T>(string name, T value)`: 设置参数值
  - `GetParameter<T>(string name)`: 获取参数值
  - `GetOriginalParameter<T>(string name)`: 获取原始参数值
  - `HasOverride(string name)`: 检查是否有覆盖
  - `RemoveOverride(string name)`: 移除覆盖
  - `ClearOverrides()`: 清除该轨道的所有覆盖

#### RuntimeParameter
- **作用**: 代表一个参数
- **主要属性**: `Name`, `Type`
- **主要方法**:
  - `SetValue<T>(T value)`: 设置值
  - `GetValue<T>()`: 获取当前值
  - `GetOriginalValue<T>()`: 获取原始值
  - `HasOverride()`: 检查是否有覆盖
  - `RemoveOverride()`: 移除覆盖
  - `GetInfo()`: 获取参数详细信息

### 使用示例

```csharp
// 获取包装器
var wrapper = combatController.GetRuntimeParameterWrapper();

// 示例1：修改指定动作的速度
var attackAction = wrapper.GetAction("Attack1");
var speedTrack = attackAction.GetTrack<AbilityEventObj_AnimSpeed>();
speedTrack.SetParameter("Speed", 2.0f);

// 示例2：修改所有碰撞盒大小
var actions = wrapper.GetActions();
foreach (var action in actions)
{
    var hitBoxTracks = action.GetTracks<AbilityEventObj_CreateHitBox>();
    foreach (var track in hitBoxTracks)
    {
        Vector3 originalSize = track.GetOriginalParameter<Vector3>("hitBoxSize");
        track.SetParameter("hitBoxSize", originalSize * 1.5f);
    }
}

// 示例3：条件性修改
var attackActions = wrapper.GetActionsContaining("Attack");
foreach (var action in attackActions)
{
    var tracks = action.GetTracks();
    foreach (var track in tracks)
    {
        if (track.TypeName == "AbilityEventObj_Motion")
        {
            track.SetParameter("MotionTime", 0.5f);
        }
    }
}

// 示例4：参数信息查看
var track = action.GetTrack(0);
var parameters = track.GetParameters();
foreach (var param in parameters)
{
    if (param.HasOverride())
    {
        var info = param.GetInfo();
        Debug.Log($"参数 {info.Name}: {info.OriginalValue} -> {info.CurrentValue}");
    }
}
```

## 参数路径格式（索引API）

参数使用路径格式来标识：`GroupIndex.AbilityIndex.EventIndex.ParameterName`

- `GroupIndex`: 动作组索引（从0开始）
- `AbilityIndex`: 组内能力索引（从0开始）  
- `EventIndex`: 能力内事件索引（从0开始）
- `ParameterName`: 参数字段名称

### 示例路径

```
0.0.0.Speed         // 第1组第1个能力第1个事件的Speed参数
0.1.2.hitBoxSize    // 第1组第2个能力第3个事件的hitBoxSize参数
1.0.0.EventTime     // 第2组第1个能力第1个事件的EventTime参数
```

## 常见使用场景

### 1. 角色技能强化

```csharp
// 提升所有攻击动作的速度
public void ApplySpeedBoost(float multiplier)
{
    for (int groupIndex = 0; groupIndex < combatController.CombatDatas.Count; groupIndex++)
    {
        var group = combatController.CombatDatas[groupIndex];
        for (int abilityIndex = 0; abilityIndex < group.CombatObjs.Count; abilityIndex++)
        {
            var ability = group.CombatObjs[abilityIndex];
            
            // 只修改包含"Attack"的动作
            if (ability.name.Contains("Attack"))
            {
                for (int eventIndex = 0; eventIndex < ability.events.Count; eventIndex++)
                {
                    var eventObj = ability.events[eventIndex].Obj;
                    
                    if (eventObj is AbilityEventObj_AnimSpeed)
                    {
                        float originalSpeed = parameterManager.GetOriginalParameter<float>(
                            groupIndex, abilityIndex, eventIndex, "Speed");
                        parameterManager.SetParameterOverride(
                            groupIndex, abilityIndex, eventIndex, "Speed", 
                            originalSpeed * multiplier);
                    }
                }
            }
        }
    }
}
```

### 2. 临时buff效果

```csharp
// 应用临时buff，5秒后自动恢复
public void ApplyTemporaryBuff()
{
    ApplySpeedBoost(1.5f);
    ApplyDamageBoost(2.0f);
    
    // 5秒后恢复
    StartCoroutine(RestoreAfterDelay(5f));
}

private IEnumerator RestoreAfterDelay(float delay)
{
    yield return new WaitForSeconds(delay);
    parameterManager.ClearAllOverrides();
}
```

### 3. 碰撞盒调整

```csharp
// 放大所有碰撞盒
public void EnlargeHitBoxes(Vector3 sizeMultiplier)
{
    for (int groupIndex = 0; groupIndex < combatController.CombatDatas.Count; groupIndex++)
    {
        var group = combatController.CombatDatas[groupIndex];
        for (int abilityIndex = 0; abilityIndex < group.CombatObjs.Count; abilityIndex++)
        {
            var ability = group.CombatObjs[abilityIndex];
            for (int eventIndex = 0; eventIndex < ability.events.Count; eventIndex++)
            {
                var eventObj = ability.events[eventIndex].Obj;
                
                if (eventObj is AbilityEventObj_CreateHitBox)
                {
                    Vector3 originalSize = parameterManager.GetOriginalParameter<Vector3>(
                        groupIndex, abilityIndex, eventIndex, "hitBoxSize");
                    Vector3 newSize = Vector3.Scale(originalSize, sizeMultiplier);
                    parameterManager.SetParameterOverride(
                        groupIndex, abilityIndex, eventIndex, "hitBoxSize", newSize);
                }
            }
        }
    }
}
```

### 4. 实时参数调整

```csharp
// 根据时间动态调整参数
public void StartRealTimeAdjustment()
{
    StartCoroutine(RealTimeAdjustmentCoroutine());
}

private IEnumerator RealTimeAdjustmentCoroutine()
{
    float elapsedTime = 0f;
    float duration = 10f;
    
    while (elapsedTime < duration)
    {
        // 根据正弦波动态调整速度
        float speedValue = 1f + Mathf.Sin(elapsedTime * 2f) * 0.5f;
        
        // 应用到所有动画速度事件
        ApplySpeedToAllAnimSpeedEvents(speedValue);
        
        elapsedTime += Time.deltaTime;
        yield return null;
    }
    
    // 恢复原始值
    parameterManager.ClearAllOverrides();
}
```

## 预设系统

### 创建预设

```csharp
// 从当前状态创建预设
var preset = RuntimeParameterPreset.CreateFromManager(parameterManager, "战斗增强预设");

// 手动创建预设
var preset = new RuntimeParameterPreset();
preset.presetName = "自定义预设";
preset.parameterOverrides.Add(new RuntimeParameterPreset.ParameterOverride
{
    groupIndex = 0,
    abilityIndex = 0,
    eventIndex = 0,
    parameterName = "Speed",
    valueType = "float",
    serializedValue = "2.0"
});
```

### 应用预设

```csharp
// 应用预设到管理器
preset.ApplyToManager(parameterManager);
```

## 便捷组件：RuntimeParameterHelper

为了方便在Inspector中操作，系统提供了`RuntimeParameterHelper`组件：

```csharp
// 添加到GameObject上
var helper = gameObject.AddComponent<RuntimeParameterHelper>();

// 设置目标
helper.targetCombatController = combatController;

// 应用预设
helper.ApplyPreset("预设名称");

// 清除所有覆盖
helper.ClearAllOverrides();
```

## 调试功能

### 启用调试模式

```csharp
// 在RuntimeParameterManager组件中启用调试模式
parameterManager.debugMode = true;
```

### 查看覆盖状态

```csharp
// 获取覆盖参数数量
int overrideCount = parameterManager.GetOverrideCount();

// 获取总参数数量
int totalCount = parameterManager.GetTotalParameterCount();

// 获取所有覆盖的参数路径
var overriddenPaths = parameterManager.GetOverriddenParameterPaths();

// 获取覆盖详情
var overrideDetails = parameterManager.GetAllOverrideDetails();
```

## 常见事件类型参数

### AbilityEventObj_AnimSpeed
- `Speed` (float): 动画速度倍数
- `UseCurveLerp` (bool): 是否使用曲线插值

### AbilityEventObj_CreateHitBox
- `hitBoxSize` (Vector3): 碰撞盒大小
- `hitBoxOffset` (Vector3): 碰撞盒偏移
- `hitBoxColor` (Color): 碰撞盒颜色
- `destroyOnHit` (bool): 命中后是否销毁
- `maxHits` (int): 最大命中次数

### AbilityEventObj_Motion
- `MotionTime` (float): 移动时间
- `TimeToDis` (AnimationCurve): 时间到距离的曲线

### AbilityEventObj_HitStop
- `hitStopFrames` (int): 顿帧帧数
- `animationSpeed` (float): 动画速度

### AbilityEvent (基础事件参数)
- `EventTime` (float): 事件时间
- `EventRange` (Vector2): 事件范围
- `EventMultiRange` (float[]): 多段事件范围
- `Previewable` (bool): 是否可预览

## 注意事项

### 1. 性能考虑
- 频繁的参数修改会影响性能，建议批量操作
- 不要在每帧都修改参数，除非确实需要

### 2. 类型安全
- 确保参数类型匹配，否则可能引发异常
- 使用泛型方法确保类型安全

### 3. 参数命名
- 参数名称必须与字段名称完全匹配（区分大小写）
- 建议使用IDE的自动补全功能避免拼写错误

### 4. 生命周期管理
- 组件销毁时会自动清除所有覆盖
- 场景切换时需要重新初始化

## 错误处理

```csharp
// 检查参数是否存在
if (parameterManager.HasOverride(0, 0, 0, "Speed"))
{
    // 参数已被覆盖
}

// 安全地获取参数
try
{
    float speed = parameterManager.GetCurrentParameter<float>(0, 0, 0, "Speed");
}
catch (Exception ex)
{
    Debug.LogError($"获取参数失败: {ex.Message}");
}
```

## 完整示例

### 面向对象API示例

参考 `RuntimeParameterWrapperExample.cs` 文件中的完整示例代码，包含：

1. 通过动作名称修改参数
2. 通过轨道类型批量修改
3. 条件性参数修改
4. 参数信息查看和调试
5. 特定动作覆盖清除
6. 同一轨道多参数修改
7. 通过轨道名称修改
8. 类型安全的参数修改

### 索引API示例

参考 `RuntimeParameterExample.cs` 文件中的完整示例代码，包含：

1. 基本参数修改
2. 按类型筛选修改
3. 批量参数操作
4. 预设系统使用
5. 实时参数调整
6. 临时效果应用

### 选择建议

- **新项目**: 建议使用面向对象API，更直观易用
- **现有项目**: 可以继续使用索引API，或逐步迁移到面向对象API
- **复杂操作**: 面向对象API提供更好的类型安全和代码可读性
- **性能考虑**: 两种API性能相当，面向对象API有轻微的包装开销

## 扩展功能

### 自定义参数类型支持

如果需要支持自定义参数类型，可以扩展 `RuntimeParameterPreset.ParameterOverride` 类：

```csharp
// 在ParameterOverride中添加新的类型支持
case "mycustomtype":
    return ParseMyCustomType(serializedValue);
```

### 事件监听

可以扩展系统添加参数变更事件：

```csharp
public class RuntimeParameterManager : MonoBehaviour
{
    public event System.Action<string, object> OnParameterChanged;
    
    // 在SetParameterOverride中触发事件
    OnParameterChanged?.Invoke(parameterPath, value);
}
```

## API对比

| 功能 | 面向对象API | 索引API |
|------|-------------|---------|
| **获取方式** | `combatController.GetRuntimeParameterWrapper()` | `combatController.GetOrAddRuntimeParameterManager()` |
| **修改参数** | `action.GetTrack<T>().SetParameter("param", value)` | `manager.SetParameterOverride(0, 0, 0, "param", value)` |
| **获取参数** | `track.GetParameter<T>("param")` | `manager.GetCurrentParameter<T>(0, 0, 0, "param")` |
| **按动作名称** | `wrapper.GetAction("Attack1")` | `manager.SetParameterOverrideByAbilityName()` |
| **按轨道类型** | `action.GetTracks<AbilityEventObj_AnimSpeed>()` | 需要手动遍历和类型检查 |
| **参数信息** | `parameter.GetInfo()` | `manager.GetAllOverrideDetails()` |
| **类型安全** | ✅ 编译时检查 | ⚠️ 运行时检查 |
| **代码可读性** | ✅ 高度直观 | ⚠️ 需要理解索引结构 |
| **学习曲线** | ✅ 容易上手 | ⚠️ 需要理解内部结构 |
| **性能** | ✅ 轻微包装开销 | ✅ 直接访问 |

## 总结

这个系统为CombatEditor提供了强大的运行时参数修改能力，让你能够在不修改原始数据的情况下动态调整游戏行为。通过面向对象API，你可以更直观地访问和修改参数，而索引API则提供了更底层的控制能力。

 
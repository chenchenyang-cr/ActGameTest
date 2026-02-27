# CombatEditor 条件系统使用文档

## 概述

CombatEditor 条件系统提供了一个灵活的接口，允许开发者创建自定义的事件条件。任何实现了 `IEventCondition` 接口的类都可以作为条件使用。

## 特性

- **接口驱动**: 通过 `IEventCondition` 接口定义条件
- **自动发现**: 自动发现并注册所有实现了接口的条件类
- **向后兼容**: 支持现有的硬编码条件类型
- **易于扩展**: 只需实现接口即可添加新的条件类型
- **编辑器集成**: 完全集成到 CombatEditor 的条件编辑界面

## 基本使用

### 1. 创建自定义条件

```csharp
using UnityEngine;
using CombatEditor;

public class CustomCondition : IEventCondition
{
    public string ConditionId => "custom_condition";
    public string DisplayName => "自定义条件";
    public string Description => "这是一个自定义条件的示例";
    public Color IconColor => Color.magenta;
    public string ShortLabel => "自定义";
    
    public bool CheckCondition(CombatController controller)
    {
        // 在这里实现你的条件逻辑
        return true; // 示例：总是返回true
    }
    
    public void Initialize(CombatController controller) { }
    public void Cleanup(CombatController controller) { }
    public ScriptableObject GetCustomParameters() => null;
    public void SetCustomParameters(ScriptableObject parameters) { }
}
```

### 2. 注册条件

条件会在编辑器启动时自动注册，但你也可以手动注册：

```csharp
// 手动注册条件
EventConditionManager.Instance.RegisterCondition<CustomCondition>();
```

### 3. 在编辑器中使用

1. 在 CombatEditor 中选择事件
2. 右键点击事件，选择"编辑条件"
3. 勾选"启用条件"
4. 将"条件模式"设置为"接口模式"
5. 从"接口条件"下拉菜单中选择你的自定义条件

## 接口详解

### IEventCondition 接口

```csharp
public interface IEventCondition
{
    string ConditionId { get; }        // 条件的唯一标识符
    string DisplayName { get; }        // 条件的显示名称
    string Description { get; }        // 条件的描述信息
    Color IconColor { get; }           // 条件的图标颜色
    string ShortLabel { get; }         // 条件的简短标签
    
    bool CheckCondition(CombatController controller);    // 检查条件是否满足
    void Initialize(CombatController controller);        // 初始化条件
    void Cleanup(CombatController controller);           // 清理条件
    ScriptableObject GetCustomParameters();              // 获取自定义参数
    void SetCustomParameters(ScriptableObject parameters); // 设置自定义参数
}
```

### 必须实现的属性

- **ConditionId**: 条件的唯一标识符，必须唯一
- **DisplayName**: 在编辑器中显示的条件名称
- **Description**: 条件的描述，会在编辑器中显示为帮助信息
- **IconColor**: 条件图标的颜色
- **ShortLabel**: 条件的简短标签，用于在时间轴上显示

### 必须实现的方法

- **CheckCondition**: 核心方法，返回条件是否满足
- **Initialize**: 可选，用于初始化条件
- **Cleanup**: 可选，用于清理条件
- **GetCustomParameters**: 可选，用于获取自定义参数
- **SetCustomParameters**: 可选，用于设置自定义参数

## 示例条件

系统提供了多个示例条件：

### 1. 血量百分比条件

```csharp
public class HealthPercentageCondition : IEventCondition
{
    public bool CheckCondition(CombatController controller)
    {
        return controller.GetHealthPercentage() <= 0.5f; // 血量低于50%
    }
}
```

### 2. 在空中条件

```csharp
public class InAirCondition : IEventCondition
{
    public bool CheckCondition(CombatController controller)
    {
        return controller.IsInAir(); // 角色在空中
    }
}
```

### 3. 距离条件

```csharp
public class DistanceCondition : IEventCondition
{
    public bool CheckCondition(CombatController controller)
    {
        return controller.GetTargetDistance() <= 5f; // 距离目标5米内
    }
}
```

## 高级用法

### 1. 带参数的条件

```csharp
[System.Serializable]
public class HealthConditionParameters : ScriptableObject
{
    public float threshold = 0.5f;
    public bool lessThan = true;
}

public class ParameterizedHealthCondition : IEventCondition
{
    private HealthConditionParameters _parameters;
    
    public ScriptableObject GetCustomParameters()
    {
        if (_parameters == null)
            _parameters = ScriptableObject.CreateInstance<HealthConditionParameters>();
        return _parameters;
    }
    
    public void SetCustomParameters(ScriptableObject parameters)
    {
        _parameters = parameters as HealthConditionParameters;
    }
    
    public bool CheckCondition(CombatController controller)
    {
        if (_parameters == null) return true;
        
        float healthPercentage = controller.GetHealthPercentage();
        return _parameters.lessThan ? 
               healthPercentage <= _parameters.threshold : 
               healthPercentage >= _parameters.threshold;
    }
}
```

### 2. 复合条件

```csharp
public class CompositeCondition : IEventCondition
{
    public bool CheckCondition(CombatController controller)
    {
        // 组合多个条件
        return controller.IsInAir() && 
               controller.HasTarget() && 
               controller.GetTargetDistance() <= 3f;
    }
}
```

## 管理命令

系统提供了以下菜单命令：

- **CombatEditor/条件系统/显示系统信息**: 显示所有已注册的条件
- **CombatEditor/条件系统/重新初始化**: 重新初始化条件系统
- **CombatEditor/条件系统/清除所有条件**: 清除所有已注册的条件

## 注意事项

1. **条件ID必须唯一**: 每个条件的 `ConditionId` 必须在系统中唯一
2. **性能考虑**: `CheckCondition` 方法会被频繁调用，避免在其中执行耗时操作
3. **线程安全**: 条件检查可能在不同线程中进行，确保实现是线程安全的
4. **向后兼容**: 现有项目可以继续使用传统的枚举条件模式

## 故障排除

1. **条件没有出现在下拉菜单中**: 检查条件类是否正确实现了 `IEventCondition` 接口
2. **条件检查不生效**: 确保 `CheckCondition` 方法返回正确的布尔值
3. **编辑器报错**: 检查条件类是否有无参构造函数
4. **自动发现失败**: 使用菜单命令手动重新初始化条件系统

## 扩展建议

1. **创建条件库**: 为项目创建专门的条件库，集中管理所有自定义条件
2. **使用命名空间**: 为避免命名冲突，建议使用命名空间
3. **编写测试**: 为复杂条件编写单元测试
4. **文档化**: 为每个自定义条件编写详细的文档说明

---

通过这个条件系统，开发者可以轻松扩展 CombatEditor 的条件功能，满足各种复杂的游戏逻辑需求。 
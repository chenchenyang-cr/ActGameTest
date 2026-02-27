# 运行时参数覆盖系统 - 概述

## 🚀 功能概述

运行时参数覆盖系统允许你在代码中动态修改CombatController里所有动作的轨道参数，**完全不影响原始的ScriptableObject数据**。这是一个专为游戏运行时参数调整而设计的系统。

## ✨ 核心特性

- ✅ **不影响原始数据** - 所有修改都是临时的，不会改变ScriptableObject文件
- ✅ **完整参数覆盖** - 支持所有轨道事件的参数（速度、碰撞盒、时间等）
- ✅ **面向对象API** - 直观的CombatController->Action->Track->Parameter访问方式
- ✅ **索引API兼容** - 保持向后兼容，支持传统索引访问
- ✅ **实时生效** - 参数修改立即生效，支持动态调整
- ✅ **预设系统** - 支持参数配置的保存和加载
- ✅ **类型安全** - 编译时类型检查，避免运行时错误

## 🎯 使用场景

- **角色技能强化/弱化** - 临时提升攻击速度、伤害、范围等
- **Buff/Debuff系统** - 动态调整角色能力参数
- **难度调整** - 根据玩家表现动态调整游戏难度
- **特殊游戏模式** - 实现狂暴模式、慢动作等特殊效果
- **实时调试** - 开发过程中快速调整参数

## 🛠️ 两种API方式

### 面向对象API（推荐）
```csharp
// 获取包装器
var wrapper = combatController.GetRuntimeParameterWrapper();

// 修改参数
var attackAction = wrapper.GetAction("Attack1");
var speedTrack = attackAction.GetTrack<AbilityEventObj_AnimSpeed>();
speedTrack.SetParameter("Speed", 2.0f);
```

### 索引API（兼容性）
```csharp
// 获取管理器
var manager = combatController.GetOrAddRuntimeParameterManager();

// 修改参数
manager.SetParameterOverride<float>(0, 0, 0, "Speed", 2.0f);
```

## 📁 文件结构

```
Assets/CombatEditor/Scripts/CombatObj/
├── RuntimeParameterOverride.cs          # 核心系统和管理器
├── RuntimeParameterWrapper.cs           # 面向对象API包装器
├── RuntimeParameterExtensions.cs        # 扩展方法和预设系统
├── RuntimeParameterOverride_Summary.md  # 本概述文件
└── README_RuntimeParameterOverride.md   # 详细使用说明

Assets/CombatEditor/Scripts/Examples/
├── RuntimeParameterExample.cs           # 索引API示例
└── RuntimeParameterWrapperExample.cs    # 面向对象API示例
```

## 🚀 快速开始

1. **添加到CombatController**
```csharp
// 面向对象API
var wrapper = combatController.GetRuntimeParameterWrapper();

// 或者索引API
var manager = combatController.GetOrAddRuntimeParameterManager();
```

2. **修改参数**
```csharp
// 面向对象方式
wrapper.GetAction("Attack1").GetTrack<AbilityEventObj_AnimSpeed>().SetParameter("Speed", 2.0f);

// 索引方式  
manager.SetParameterOverride<float>(0, 0, 0, "Speed", 2.0f);
```

3. **恢复原始值**
```csharp
// 清除所有覆盖
wrapper.ClearAllOverrides();
// 或
manager.ClearAllOverrides();
```

## 🎮 测试方法

1. 添加 `RuntimeParameterWrapperExample` 组件到你的CombatController
2. 运行游戏
3. 按键测试：
   - Q - 通过动作名称修改速度
   - W - 通过轨道类型修改碰撞盒
   - E - 条件性修改参数
   - R - 显示参数信息
   - Esc - 清除所有覆盖

## 📖 详细文档

- **完整使用指南**: `README_RuntimeParameterOverride.md`
- **面向对象API示例**: `RuntimeParameterWrapperExample.cs`
- **索引API示例**: `RuntimeParameterExample.cs`

## 🎯 选择建议

- **新项目**: 使用面向对象API，更直观易用
- **现有项目**: 可以继续使用索引API，或逐步迁移
- **复杂操作**: 面向对象API提供更好的类型安全性
- **性能要求**: 两种API性能相当，面向对象API有轻微包装开销

---

**这个系统让你能够在运行时灵活地修改战斗参数，而不会影响原始数据，非常适合实现各种动态效果和参数调整需求！** 
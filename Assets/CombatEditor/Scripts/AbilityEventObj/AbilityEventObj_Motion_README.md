# Motion事件 - 时间控制功能

## 功能概述

Motion事件现在支持三种时间控制模式：

1. **动画时间播放模式**（默认）：Motion事件跟随动画播放速度，受AnimSpeed和hitstop影响
2. **实际时间播放模式**：Motion事件基于实际时间播放，不受AnimSpeed和hitstop影响
3. **HitStop感知时间模式**（新增）：Motion事件不受AnimSpeed影响，但受hitstop影响

## 使用方法

### 1. 在Inspector中设置

在Motion事件的Inspector面板中：
- 选择"时间控制模式"下拉菜单来切换不同的时间模式
- 旧的"使用实际时间播放"复选框已被新系统替代，但保持向后兼容

### 2. 三种模式的详细说明

#### 动画时间播放模式（默认）
- Motion事件的播放速度与动画播放速度同步
- 受AnimSpeed事件影响：当动画速度变慢（如0.5倍速）时，Motion事件也会变慢
- 受hitstop影响：在hitstop期间，Motion事件会根据hitstop的动画速度进行调整
- 适用于需要与动画完全同步的移动效果

#### 实际时间播放模式
- Motion事件基于实际经过的时间播放
- 不受AnimSpeed事件影响：即使动画速度改变，Motion事件仍按固定时间播放
- 不受hitstop影响：即使触发hitstop，Motion事件仍会继续播放
- 总是以固定的实际时间完成移动
- 适用于需要固定时间完成的移动效果

#### HitStop感知时间模式（新增）
- Motion事件不受AnimSpeed事件影响：即使有AnimSpeed修改器，Motion事件仍按正常速度播放
- 受hitstop影响：在hitstop期间，Motion事件会根据hitstop的动画速度进行调整
- 在非hitstop状态下，使用正常的Time.deltaTime累积时间
- 在hitstop状态下，根据hitstop的动画速度调整时间流逝
- 适用于需要与hitstop同步但不受AnimSpeed影响的移动效果

### 3. 使用场景示例

#### 动画时间播放模式适用场景：
- 攻击动作中的向前冲刺
- 技能释放时的位移动画
- 需要与动画帧完全同步的移动

#### 实际时间播放模式适用场景：
- 定时闪避（总是需要0.2秒完成）
- 固定时长的位移效果
- 不希望受到任何时间缩放影响的移动
- 需要精确控制移动时间的场景

#### HitStop感知时间模式适用场景：
- 攻击时的位移：不受AnimSpeed影响但需要与hitstop同步
- 被击中时的后退：希望在hitstop期间也能体现出冲击感
- 特效移动：需要与hitstop的"暂停感"同步的视觉效果

### 4. 技术细节

#### 动画时间播放模式：
```csharp
// 基于动画时间百分比计算
float timePercentage = (currentTime - startTime) / (endTime - startTime);
```

#### 实际时间播放模式：
```csharp
// 基于实际经过时间计算
float realTimeElapsed = Time.time - _realTimeStartTime;
float normalizedRealTime = Mathf.Clamp01(realTimeElapsed / _eventDuration);
```

#### HitStop感知时间模式：
```csharp
// 累积时间，在hitstop期间根据hitstop速度调整
float deltaTime = Time.deltaTime;
if (_combatController.isInHitStop)
{
    float hitStopSpeed = _combatController._animator.speed;
    deltaTime *= hitStopSpeed;
}
_accumulatedHitStopAwareTime += deltaTime;
```

### 5. 与Rotation事件的配合

Rotation事件也支持相同的三种时间控制模式，可以与Motion事件配合使用：
- 相同模式：实现同步的移动和旋转效果
- 不同模式：实现复杂的移动+旋转组合效果

### 6. 向后兼容性

- 现有的Motion事件默认使用动画时间播放模式，行为不变
- 旧的"UseRealTimePlayback"字段会自动转换为对应的时间控制模式
- 不需要修改现有的Motion事件配置

### 7. 注意事项

1. **性能考虑**：HitStop感知时间模式需要额外的时间累积计算，但性能影响很小
2. **预览限制**：在编辑器预览中，HitStop感知时间模式简化为实际时间模式，因为无法完全模拟hitstop状态
3. **事件结束**：使用非动画时间模式时，事件可能会在动画播放结束前完成，系统会自动处理这种情况

### 8. 调试提示

- 可以在运行时观察Motion事件的行为来确认时间模式是否正确
- 实际时间播放模式下，即使动画暂停或变速，Motion事件也会继续按实际时间播放
- 动画时间播放模式下，Motion事件会与动画速度完全同步

## 示例配置

### 示例1：攻击冲刺（动画时间播放）
```
使用实际时间播放: false
移动偏移量: (0, 0, 2)
使用绝对坐标增量: false
```
这样配置会让角色向前冲刺，并且与攻击动画速度同步。

### 示例2：定时闪避（实际时间播放）
```
使用实际时间播放: true
移动偏移量: (3, 0, 0)
使用绝对坐标增量: true
```
这样配置会让角色在固定时间内向右闪避，不受动画速度影响。 
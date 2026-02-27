# Execute Method Track 事件使用说明

## 概述
Execute Method Track 是 Execute Method 事件的轨道版本，它会在指定的时间范围内持续执行方法，而不是只执行一次。这对于需要连续触发的效果非常有用。

## 与普通Execute Method的区别

| 特性 | Execute Method | Execute Method Track |
|------|----------------|---------------------|
| 事件类型 | EventTime（点事件） | EventRange（轨道事件） |
| 执行次数 | 只执行一次 | 在轨道期间持续执行 |
| 时间设置 | 单个时间点 | 时间范围（开始-结束） |
| 执行控制 | 无额外控制 | 可设置执行间隔、开始/结束执行 |

## 轨道执行设置

### 执行间隔 (executionInterval)
- **值为 0**：每帧都执行（高频率）
- **值 > 0**：按指定秒数间隔执行
- **示例**：
  - `0.1` = 每 0.1 秒执行一次（约每秒10次）
  - `0.5` = 每 0.5 秒执行一次（约每秒2次）
  - `1.0` = 每 1 秒执行一次

### 开始时执行 (executeOnStart)
- **true**：在轨道开始时立即执行一次
- **false**：等待第一个间隔后才开始执行
- **用途**：确保效果能立即开始

### 结束时执行 (executeOnEnd)
- **true**：在轨道结束时再执行一次
- **false**：轨道结束时不额外执行
- **用途**：确保最终状态正确设置

## 使用场景

### 1. 连续伤害效果
```
目标类: HealthController
方法: TakeDamage(float)
参数: damage = 5.0
执行间隔: 0.5 (每0.5秒造成5点伤害)
轨道时间: 3秒 (总共造成约30点伤害)
```

### 2. 连续播放音效
```
目标类: AudioController
方法: PlaySound(string)
参数: soundName = "heartbeat"
执行间隔: 1.0 (每秒播放一次心跳声)
开始时执行: true
```

### 3. 状态更新
```
目标类: PlayerController
方法: UpdateBuffStatus(bool)
参数: isActive = true
执行间隔: 0 (每帧更新)
开始时执行: true
结束时执行: false (避免结束时重复执行)
```

### 4. 定时检查
```
目标类: GameManager
方法: CheckWinCondition()
执行间隔: 0.2 (每0.2秒检查一次)
轨道时间: 整个关卡持续时间
```

## 配置步骤

### 1. 创建轨道事件
1. 在 CombatEditor 中创建新的能力事件
2. 选择 "AbilityEvents / Execute Method Track"
3. 设置轨道的开始和结束时间

### 2. 配置轨道设置
1. 设置 **执行间隔**：
   - 每帧执行：设为 0
   - 定时执行：设为具体秒数
2. 设置 **开始时执行**：是否立即开始
3. 设置 **结束时执行**：是否在结束时额外执行

### 3. 配置方法调用
1. 选择目标选择模式
2. 选择目标类和方法
3. 设置方法参数
4. （与普通 Execute Method 相同）

## 性能注意事项

### 执行频率的影响
- **每帧执行 (interval = 0)**：
  - 最高响应性
  - 性能开销最大
  - 适用于关键的实时更新

- **高频执行 (interval = 0.1)**：
  - 较好的响应性
  - 中等性能开销
  - 适用于需要快速反应的效果

- **低频执行 (interval >= 0.5)**：
  - 性能开销较小
  - 适用于不需要实时更新的效果

### 优化建议
1. **根据需求选择合适的执行间隔**
2. **避免在每帧执行的方法中进行昂贵操作**
3. **使用开始/结束执行标志避免不必要的重复**
4. **在方法内部添加必要的性能检查**

## 调试和监控

### 控制台输出
轨道事件会在控制台输出详细的执行信息：
```
预览模式: 轨道执行 PlayerController.TakeDamage - 每0.5秒执行一次
ExecuteMethodTrack执行出错: 目标对象为空
```

### 编辑器预览
在 Inspector 中会显示：
- 执行频率信息
- 参数配置状态
- 目标选择状态

## 示例脚本

```csharp
public class ContinuousEffectController : MonoBehaviour
{
    public void ApplyDamageOverTime(float damagePerTick)
    {
        // 连续伤害方法
        GetComponent<HealthController>().TakeDamage(damagePerTick);
        Debug.Log($"造成 {damagePerTick} 点持续伤害");
    }
    
    public void UpdateVisualEffect(bool isActive)
    {
        // 连续视觉效果更新
        GetComponent<ParticleSystem>().emission.enabled = isActive;
    }
    
    public void CheckGameState()
    {
        // 定期检查游戏状态
        if (GameManager.Instance.IsGameOver())
        {
            // 处理游戏结束逻辑
        }
    }
}
```

## 最佳实践

1. **合理设置执行间隔**：根据效果需求和性能要求平衡
2. **使用开始执行确保立即生效**：特别是状态改变类的方法
3. **谨慎使用结束执行**：避免与轨道结束后的逻辑冲突
4. **方法内部添加安全检查**：确保连续执行不会导致错误
5. **利用参数控制效果强度**：而不是增加执行频率

---

**版本**: v1.0  
**作者**: Combat Editor Team  
**更新时间**: 2024年1月 
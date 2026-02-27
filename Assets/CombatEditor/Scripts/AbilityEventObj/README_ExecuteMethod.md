# Execute Method 事件使用说明

## 概述
Execute Method 事件允许你在战斗系统中执行任意的自定义方法。这个事件可以调用与 CombatController 在同一 GameObject 上的任何组件的公共方法。

## 功能特性

### 支持的参数类型
- **int**: 整数
- **float**: 浮点数  
- **bool**: 布尔值
- **string**: 字符串
- **Vector3**: 三维向量
- **GameObject**: 游戏对象
- **Transform**: 变换组件
- **Color**: 颜色
- **AnimationCurve**: 动画曲线
- **enum**: 枚举类型（自动检测并显示下拉菜单）

### 自动检测功能
- 自动检测可用的类（与 CombatController 同层级的 MonoBehaviour）
- 自动检测类中的公共方法
- 自动生成方法参数字段
- 类型安全的参数匹配

## 使用步骤

### 1. 准备目标类
确保你要调用方法的类：
- 继承自 `MonoBehaviour`
- 与 `CombatController` 在同一个 GameObject 上
- 包含你想要调用的公共方法

示例：
```csharp
public class MyCustomController : MonoBehaviour
{
    public void SimpleAction()
    {
        Debug.Log("执行简单动作！");
    }
    
    public void ChangeHealth(float amount)
    {
        // 改变生命值的逻辑
    }
    
    public void ComplexAction(float multiplier, string message, bool enable)
    {
        // 复杂的逻辑处理
    }
}
```

### 2. 创建 Execute Method 事件
1. 在 CombatEditor 中创建新的能力事件
2. 选择 "AbilityEvents / Execute Method"
3. 在检查器中配置事件

### 3. 配置事件参数

#### 选择目标类
- 从下拉列表中选择要调用的类
- 点击"刷新"按钮可以重新扫描可用的类

#### 选择方法
- 选择类后，方法下拉列表会显示该类的所有可用方法
- 方法显示格式：`方法名(参数类型1, 参数类型2, ...)`
- 点击"刷新"按钮可以重新扫描方法

#### 设置参数
- 选择方法后，参数字段会自动生成
- 为每个参数设置适当的值
- 参数类型会自动匹配方法签名

## 示例用法

### 示例 1：播放音效
```
目标类: ExampleMethodExecutor
方法: PlaySound(string)
参数:
  - audioClipName: "sword_slash"
```

### 示例 2：改变生命值
```
目标类: ExampleMethodExecutor  
方法: ChangeHealth(float)
参数:
  - amount: -10.0
```

### 示例 3：复杂操作
```
目标类: ExampleMethodExecutor
方法: ComplexAction(float, string, bool)
参数:
  - multiplier: 2.0
  - message: "特殊攻击"
  - enable: true
```

### 示例 4：移动到位置
```
目标类: ExampleMethodExecutor
方法: MoveTo(Vector3)
参数:
  - position: (0, 0, 5)
```

### 示例 5：枚举参数 - 执行攻击
```
目标类: ExampleMethodExecutor
方法: ExecuteAttack(AttackType)
参数:
  - attackType: Heavy (从下拉菜单选择：Light, Heavy, Magic, Special)
```

### 示例 6：复合枚举参数
```
目标类: ExampleMethodExecutor
方法: CastSpell(ElementType, float)
参数:
  - element: Fire (从下拉菜单选择：Fire, Water, Earth, Air, Lightning, Ice, Dark, Light)
  - power: 15.0
```

## 最佳实践

### 1. 方法命名
- 使用清晰、描述性的方法名
- 避免使用 Unity 生命周期方法名（Start, Update 等）

### 2. 参数设计
- 保持参数列表简洁
- 使用基本类型便于配置
- 考虑使用默认参数值

### 3. 错误处理
- 在方法内部添加空值检查
- 提供有用的调试信息
- 优雅地处理异常情况

### 4. 性能考虑
- 避免在频繁调用的方法中进行昂贵的操作
- 考虑缓存频繁访问的组件引用

## 故障排除

### 常见问题

**问题**: 类没有出现在下拉列表中
**解决**: 
- 确保类继承自 MonoBehaviour
- 确保类是 public 的
- 点击"刷新"按钮重新扫描

**问题**: 方法没有出现在列表中
**解决**:
- 确保方法是 public 的
- 确保方法不是属性访问器
- 避免使用 Unity 生命周期方法

**问题**: 运行时方法执行失败
**解决**:
- 检查目标组件是否存在
- 验证参数类型是否匹配
- 查看控制台错误信息

**问题**: 参数类型不支持
**解决**:
- 使用支持的基本类型
- 考虑将复杂类型分解为基本类型
- 或者使用 GameObject/Transform 引用

## 枚举参数支持

### 特性
- **自动检测**: 系统会自动识别方法参数中的枚举类型
- **下拉菜单**: 在编辑器中显示友好的下拉菜单供选择
- **类型安全**: 确保传递的枚举值是正确的类型
- **支持所有枚举**: 包括自定义枚举和带有数值的枚举

### 使用枚举的建议
1. **定义清晰的枚举**:
   ```csharp
   public enum AttackType
   {
       Light,    // 轻攻击
       Heavy,    // 重攻击
       Magic,    // 魔法攻击
       Special   // 特殊攻击
   }
   ```

2. **在方法中使用枚举**:
   ```csharp
   public void ExecuteAttack(AttackType attackType)
   {
       Debug.Log($"执行攻击: {attackType}");
       // 根据枚举值执行不同逻辑
   }
   ```

3. **编辑器中的显示**:
   - 枚举类型名会显示在参数字段上方
   - 下拉菜单包含所有枚举值
   - 当前选择的枚举值会显示在下方

### 枚举支持的功能
- ✅ 基本枚举（Light, Heavy, Magic, Special）
- ✅ 带数值的枚举（Easy = 1, Normal = 2, Hard = 3）
- ✅ 多个枚举参数组合
- ✅ 枚举与其他类型参数混合使用
- ✅ 自定义命名空间中的枚举

## 扩展功能

如果需要支持更多参数类型，可以修改 `MethodParameter.ParameterType` 枚举和相关的处理代码。

如果需要调用静态方法，可以扩展 `AbilityEventEffect_ExecuteMethod` 类的 `ExecuteMethod` 方法。

## 注意事项

- 此功能使用反射，可能对性能有轻微影响
- 在发布版本中，某些代码可能被编译器优化掉
- 建议在正式发布前充分测试所有方法调用 
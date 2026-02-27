using UnityEngine;

/// <summary>
/// ExecuteMethod事件的使用示例和说明
/// 这个文件展示了如何正确配置ExecuteMethod事件的不同目标选择模式
/// </summary>
public class ExecuteMethod_UsageExample : MonoBehaviour
{
    // 这是一个示例类，展示可以被ExecuteMethod调用的各种方法
    
    // === 示例方法 - 可以被ExecuteMethod调用 ===
    
    /// <summary>
    /// 无参数方法示例
    /// 在ExecuteMethod中设置：
    /// - 目标类名: ExecuteMethod_UsageExample
    /// - 方法名: ExampleMethod
    /// </summary>
    public void ExampleMethod()
    {
        Debug.Log("ExecuteMethod调用了ExampleMethod()");
    }
    
    /// <summary>
    /// 带参数方法示例
    /// 在ExecuteMethod中设置：
    /// - 目标类名: ExecuteMethod_UsageExample
    /// - 方法名: ExampleMethodWithParams
    /// - 参数1: int类型
    /// - 参数2: string类型
    /// </summary>
    public void ExampleMethodWithParams(int intValue, string stringValue)
    {
        Debug.Log($"ExecuteMethod调用了ExampleMethodWithParams({intValue}, '{stringValue}')");
    }
    
    /// <summary>
    /// 游戏对象操作方法
    /// </summary>
    public void SetActive(bool active)
    {
        gameObject.SetActive(active);
        Debug.Log($"GameObject {gameObject.name} 设置为 {(active ? "激活" : "禁用")}");
    }
    
    /// <summary>
    /// 位置移动方法
    /// </summary>
    public void MoveTo(Vector3 position)
    {
        transform.position = position;
        Debug.Log($"GameObject {gameObject.name} 移动到 {position}");
    }
}

/*
ExecuteMethod目标选择模式使用说明：

1. CurrentCombatController（当前战斗控制器）
   - 使用场景：在当前执行事件的CombatController同一GameObject上查找组件
   - 配置：只需选择目标选择模式为CurrentCombatController
   - 示例：调用当前CombatController上的BossController组件的方法

2. SpecificTarget（指定目标）
   - 使用场景：调用特定GameObject上的组件方法
   - 配置：选择SpecificTarget模式，然后拖拽目标GameObject到specificTarget字段
   - 示例：调用场景中某个特定NPC的方法

3. FindByTag（按标签查找）
   - 使用场景：调用具有特定标签的GameObject上的组件方法
   - 配置：选择FindByTag模式，在searchValue字段输入标签名
   - 示例：调用标签为"Player"的GameObject上的方法
   - 注意：如果有多个相同标签的对象，会使用第一个找到的

4. FindByName（按名称查找）
   - 使用场景：调用具有特定名称的GameObject上的组件方法
   - 配置：选择FindByName模式，在searchValue字段输入GameObject名称
   - 示例：调用名为"Boss"的GameObject上的方法

配置步骤：
1. 在AbilityEventObj_ExecuteMethod中选择目标选择模式
2. 根据模式填写相应的字段（specificTarget或searchValue）
3. 选择目标类名（从下拉框选择或手动输入）
4. 选择方法名（从下拉框选择或手动输入）
5. 如果方法有参数，配置相应的参数值

调试提示：
- 在编辑器中可以看到预览信息，帮助确认目标是否正确
- 运行时会在控制台输出详细的执行信息和错误提示
- 如果目标找不到，会有相应的警告信息
*/ 
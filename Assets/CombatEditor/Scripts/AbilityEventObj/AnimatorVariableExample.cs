using UnityEngine;
using CombatEditor;

/// <summary>
/// 动画机变量控制事件系统使用示例
/// 
/// 这个示例展示了如何监听和响应动画机变量的变更事件
/// 可以用于实现复杂的游戏逻辑，如连击系统、状态管理等
/// </summary>
public class AnimatorVariableExample : MonoBehaviour, ICombatEventListener
{
    [Header("战斗控制器引用")]
    public CombatController combatController;
    
    [Header("动画机变量监听设置")]
    [Tooltip("是否启用详细的调试输出")]
    public bool enableDetailedLogging = true;
    
    [Header("连击系统示例")]
    [Tooltip("连击音效")]
    public AudioClip[] comboSounds;
    [Tooltip("最大连击数")]
    public int maxComboCount = 5;
    
    [Header("攻击状态示例")]
    [Tooltip("攻击状态特效")]
    public GameObject attackEffect;
    [Tooltip("防御状态特效")]
    public GameObject defenseEffect;
    
    private AudioSource audioSource;
    private int currentComboCount = 0;
    
    void Start()
    {
        // 获取音频组件
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        
        // 如果没有指定战斗控制器，尝试自动查找
        if (combatController == null)
        {
            combatController = GetComponent<CombatController>();
        }
        
        // 确保战斗控制器存在
        if (combatController != null)
        {
            // 事件监听器会在CombatController的Start()方法中自动注册
            Debug.Log($"AnimatorVariableExample: 已绑定到战斗控制器 {combatController.name}");
        }
        else
        {
            Debug.LogWarning("AnimatorVariableExample: 未找到CombatController组件！");
        }
    }
    
    #region ICombatEventListener 接口实现
    
    // 当动画机变量变更时触发
    public void OnAnimatorVariableChanged(CombatController character, string variableName, object oldValue, object newValue)
    {
        if (enableDetailedLogging)
        {
            Debug.Log($"[AnimatorVariableExample] 变量变更: {variableName} = {oldValue} → {newValue}");
        }
        
        // 根据变量名执行不同的逻辑
        switch (variableName.ToLower())
        {
            case "combocount":
                HandleComboCountChange(character, (int)newValue);
                break;
                
            case "isattacking":
                HandleAttackStateChange(character, (bool)newValue);
                break;
                
            case "isdefending":
                HandleDefenseStateChange(character, (bool)newValue);
                break;
        }
    }
    
    // 当动画机布尔变量被设置时触发
    public void OnAnimatorBoolSet(CombatController character, string variableName, bool value)
    {
        if (enableDetailedLogging)
        {
            Debug.Log($"[AnimatorVariableExample] 布尔变量设置: {variableName} = {value}");
        }
        
        // 处理布尔状态变更
        switch (variableName.ToLower())
        {
            case "isattacking":
                ToggleAttackEffect(value);
                break;
                
            case "isdefending":
                ToggleDefenseEffect(value);
                break;
                
            case "isinair":
                HandleAirborneState(character, value);
                break;
        }
    }
    
    // 当动画机整数变量被设置时触发
    public void OnAnimatorIntSet(CombatController character, string variableName, int value)
    {
        if (enableDetailedLogging)
        {
            Debug.Log($"[AnimatorVariableExample] 整数变量设置: {variableName} = {value}");
        }
        
        // 处理整数变量变更
        switch (variableName.ToLower())
        {
            case "combocount":
                PlayComboSound(value);
                UpdateComboUI(value);
                break;
                
            case "attackphase":
                HandleAttackPhaseChange(character, value);
                break;
                
            case "weapontype":
                HandleWeaponTypeChange(character, value);
                break;
        }
    }
    
    // 当动画机浮点变量被设置时触发
    public void OnAnimatorFloatSet(CombatController character, string variableName, float value)
    {
        if (enableDetailedLogging)
        {
            Debug.Log($"[AnimatorVariableExample] 浮点变量设置: {variableName} = {value}");
        }
        
        // 处理浮点变量变更
        switch (variableName.ToLower())
        {
            case "attackspeed":
                HandleAttackSpeedChange(character, value);
                break;
                
            case "movespeed":
                HandleMoveSpeedChange(character, value);
                break;
                
            case "health":
                HandleHealthChange(character, value);
                break;
        }
    }
    
    // 当动画机触发器被激活时触发
    public void OnAnimatorTriggerSet(CombatController character, string triggerName)
    {
        if (enableDetailedLogging)
        {
            Debug.Log($"[AnimatorVariableExample] 触发器激活: {triggerName}");
        }
        
        // 处理触发器激活
        switch (triggerName.ToLower())
        {
            case "specialattack":
                HandleSpecialAttack(character);
                break;
                
            case "takehit":
                HandleTakeHit(character);
                break;
                
            case "dodge":
                HandleDodge(character);
                break;
                
            case "block":
                HandleBlock(character);
                break;
        }
    }
    
    // 基础战斗事件接口实现（简化版）
    public void OnHasHitTarget(CombatController attacker, CombatController target) { }
    public void OnBeenHit(CombatController target, CombatController attacker) { }
    public void OnHitChecked(CombatController character) { }
    public void OnEnterHitStop(CombatController character) { }
    public void OnExitHitStop(CombatController character) { }
    public void OnAttackPhaseChanged(CombatController character, int oldPhase, int newPhase) { }
    
    #endregion
    
    #region 私有方法 - 业务逻辑处理
    
    /// <summary>
    /// 处理连击计数变更
    /// </summary>
    private void HandleComboCountChange(CombatController character, int newComboCount)
    {
        currentComboCount = newComboCount;
        
        // 连击特效处理
        if (newComboCount > 0 && newComboCount <= maxComboCount)
        {
            Debug.Log($"连击数更新: {newComboCount}");
            
            // 可以在这里添加连击特效、UI更新等
            // 例如：显示连击数字、播放连击音效、增加伤害倍率等
        }
        else if (newComboCount > maxComboCount)
        {
            Debug.Log($"达到最大连击数: {maxComboCount}！触发超级连击！");
            // 超级连击逻辑
        }
        else if (newComboCount == 0)
        {
            Debug.Log("连击中断");
            // 连击中断处理
        }
    }
    
    /// <summary>
    /// 处理攻击状态变更
    /// </summary>
    private void HandleAttackStateChange(CombatController character, bool isAttacking)
    {
        if (isAttacking)
        {
            Debug.Log($"{character.name} 进入攻击状态");
            // 攻击开始逻辑
        }
        else
        {
            Debug.Log($"{character.name} 退出攻击状态");
            // 攻击结束逻辑
        }
    }
    
    /// <summary>
    /// 处理防御状态变更
    /// </summary>
    private void HandleDefenseStateChange(CombatController character, bool isDefending)
    {
        if (isDefending)
        {
            Debug.Log($"{character.name} 进入防御状态");
        }
        else
        {
            Debug.Log($"{character.name} 退出防御状态");
        }
    }
    
    /// <summary>
    /// 切换攻击特效
    /// </summary>
    private void ToggleAttackEffect(bool enable)
    {
        if (attackEffect != null)
        {
            attackEffect.SetActive(enable);
        }
    }
    
    /// <summary>
    /// 切换防御特效
    /// </summary>
    private void ToggleDefenseEffect(bool enable)
    {
        if (defenseEffect != null)
        {
            defenseEffect.SetActive(enable);
        }
    }
    
    /// <summary>
    /// 处理空中状态
    /// </summary>
    private void HandleAirborneState(CombatController character, bool isInAir)
    {
        if (isInAir)
        {
            Debug.Log($"{character.name} 在空中");
            // 空中状态逻辑
        }
        else
        {
            Debug.Log($"{character.name} 着地");
            // 着地逻辑
        }
    }
    
    /// <summary>
    /// 播放连击音效
    /// </summary>
    private void PlayComboSound(int comboCount)
    {
        if (audioSource != null && comboSounds != null && comboSounds.Length > 0)
        {
            int soundIndex = Mathf.Min(comboCount - 1, comboSounds.Length - 1);
            if (soundIndex >= 0 && comboSounds[soundIndex] != null)
            {
                audioSource.PlayOneShot(comboSounds[soundIndex]);
            }
        }
    }
    
    /// <summary>
    /// 更新连击UI
    /// </summary>
    private void UpdateComboUI(int comboCount)
    {
        // 这里可以更新UI显示连击数
        // 例如：ComboUI.UpdateComboCount(comboCount);
    }
    
    /// <summary>
    /// 处理攻击阶段变更
    /// </summary>
    private void HandleAttackPhaseChange(CombatController character, int phase)
    {
        Debug.Log($"{character.name} 攻击阶段变更为: {phase}");
        // 根据攻击阶段执行不同逻辑
    }
    
    /// <summary>
    /// 处理武器类型变更
    /// </summary>
    private void HandleWeaponTypeChange(CombatController character, int weaponType)
    {
        Debug.Log($"{character.name} 武器类型变更为: {weaponType}");
        // 武器切换逻辑
    }
    
    /// <summary>
    /// 处理攻击速度变更
    /// </summary>
    private void HandleAttackSpeedChange(CombatController character, float attackSpeed)
    {
        Debug.Log($"{character.name} 攻击速度设置为: {attackSpeed}");
        // 攻击速度调整逻辑
    }
    
    /// <summary>
    /// 处理移动速度变更
    /// </summary>
    private void HandleMoveSpeedChange(CombatController character, float moveSpeed)
    {
        Debug.Log($"{character.name} 移动速度设置为: {moveSpeed}");
        // 移动速度调整逻辑
    }
    
    /// <summary>
    /// 处理生命值变更
    /// </summary>
    private void HandleHealthChange(CombatController character, float health)
    {
        Debug.Log($"{character.name} 生命值设置为: {health}");
        // 生命值变更逻辑
    }
    
    /// <summary>
    /// 处理特殊攻击触发器
    /// </summary>
    private void HandleSpecialAttack(CombatController character)
    {
        Debug.Log($"{character.name} 触发特殊攻击！");
        // 特殊攻击逻辑
    }
    
    /// <summary>
    /// 处理受击触发器
    /// </summary>
    private void HandleTakeHit(CombatController character)
    {
        Debug.Log($"{character.name} 受到攻击！");
        // 受击处理逻辑
    }
    
    /// <summary>
    /// 处理闪避触发器
    /// </summary>
    private void HandleDodge(CombatController character)
    {
        Debug.Log($"{character.name} 执行闪避！");
        // 闪避处理逻辑
    }
    
    /// <summary>
    /// 处理格挡触发器
    /// </summary>
    private void HandleBlock(CombatController character)
    {
        Debug.Log($"{character.name} 执行格挡！");
        // 格挡处理逻辑
    }
    
    #endregion
    
    #region 公共方法 - 外部调用接口
    
    /// <summary>
    /// 手动设置动画机变量（用于测试）
    /// </summary>
    public void TestSetAnimatorVariable(string variableName, object value)
    {
        if (combatController?._animator != null)
        {
            var animator = combatController._animator;
            
            // 根据值的类型设置对应的动画机变量
            switch (value)
            {
                case bool boolValue:
                    animator.SetBool(variableName, boolValue);
                    break;
                case int intValue:
                    animator.SetInteger(variableName, intValue);
                    break;
                case float floatValue:
                    animator.SetFloat(variableName, floatValue);
                    break;
                case string when value.ToString().ToLower() == "trigger":
                    animator.SetTrigger(variableName);
                    break;
            }
        }
    }
    
    /// <summary>
    /// 获取当前连击数（外部查询接口）
    /// </summary>
    public int GetCurrentComboCount()
    {
        return currentComboCount;
    }
    
    #endregion
} 
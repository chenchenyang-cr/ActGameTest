using UnityEngine;
 namespace CombatEditor {	
	[AbilityEvent]
	[CreateAssetMenu(menuName = "AbilityEvents / CreateHitBox")]
	//CreateHitBoxEvent
	public class AbilityEventObj_CreateHitBox : AbilityEventObj
	{
	    // Transform Binding Configuration
	    [Header("Transform Binding")]
	    [SerializeField]
	    [Tooltip("要绑定的Transform名称，留空则使用角色自身Transform")]
	    public string bindTransformName = "";
	    [SerializeField]
	    [Tooltip("是否自动搜索绑定的Transform")]
	    public bool autoSearchBindTransform = true;
	    
	    // HitBox Basic Properties
	    [Header("HitBox Properties")]
	    public Vector3 hitBoxOffset = Vector3.zero;
	    public Vector3 hitBoxSize = new Vector3(1, 1, 1);
	    
	    // 形状类型
	    public HitBox.HitBoxShape hitBoxShape = HitBox.HitBoxShape.Box;
	    
	    // 球体和胶囊体特有属性
	    public float radius = 0.5f;
	    public float height = 1f; // 仅用于胶囊体
	    
	    // 判定标签
	    public string[] hitTags = new string[] { "Player", "Enemy" };
	    
	    // 可视化颜色
	    public Color hitBoxColor = new Color(1f, 0f, 0f, 0.3f);
	    
	    // 是否在命中后自动销毁
	    public bool destroyOnHit = false;
	    
	    // 最大命中次数（0表示无限制）
	    public int maxHits = 0;
	
	    public override EventTimeType GetEventTimeType()
	    {
	        return EventTimeType.EventRange;
	    }
	    
	    public override AbilityEventEffect Initialize()
	    {
	        return new AbilityEventEffect_CreateHitBox(this);
	    }
	    
#if UNITY_EDITOR
	    public override AbilityEventPreview InitializePreview()
	    {
	        return new AbilityEventPreview_CreateHitBox(this);
	    }
#endif
	}
	
	public partial class AbilityEventEffect_CreateHitBox : AbilityEventEffect
	{
	    public HitBox CurrentHitBox;
	    private GameObject hitBoxObj;
	    private float trackDuration; // 存储轨道持续时间
	
	    public override void StartEffect()
	    {
	        base.StartEffect();
	        
	        // 创建HitBox游戏对象
	        hitBoxObj = new GameObject("HitBox_" + _EventObj.name);
	        
	        // 根据角色坐标设置位置
	        if (_combatController != null)
	        {
	            hitBoxObj.transform.position = _combatController.transform.position;
	            hitBoxObj.transform.rotation = _combatController.transform.rotation;
	            hitBoxObj.transform.parent = _combatController.transform;
	        }
	        
	        // 添加HitBox组件
	        CurrentHitBox = hitBoxObj.AddComponent<HitBox>();
	        
	        // 配置HitBox属性
	        if (CurrentHitBox != null)
	        {
	            CurrentHitBox.Init(_combatController);
	            
	            // 配置Transform绑定
	            AbilityEventObj_CreateHitBox hitBoxEvent = (AbilityEventObj_CreateHitBox)_EventObj;
	            CurrentHitBox.bindTransformName = hitBoxEvent.bindTransformName;
	            CurrentHitBox.autoSearchBindTransform = hitBoxEvent.autoSearchBindTransform;
	            
	            // 如果指定了绑定名称，尝试搜索并绑定
	            if (!string.IsNullOrEmpty(hitBoxEvent.bindTransformName))
	            {
	                CurrentHitBox.SearchAndBindTransform(hitBoxEvent.bindTransformName);
	            }
	            
	            // 配置基本属性
	            CurrentHitBox.hitBoxOffset = hitBoxEvent.hitBoxOffset;
	            CurrentHitBox.hitBoxSize = hitBoxEvent.hitBoxSize;
	            CurrentHitBox.shape = hitBoxEvent.hitBoxShape;
	            CurrentHitBox.radius = hitBoxEvent.radius;
	            CurrentHitBox.height = hitBoxEvent.height;
	            CurrentHitBox.hitTags = hitBoxEvent.hitTags;
	            
	            // 计算持续时间（基于轨道长度）
	            trackDuration = eve != null ? 
	                (eve.EventRange.y - eve.EventRange.x) * AnimObj.Clip.length : 
	                0.3f; // 默认值
	            
	            CurrentHitBox.duration = trackDuration;
	            
	            // 设置颜色
	            CurrentHitBox.hitBoxColor = hitBoxEvent.hitBoxColor;
	            
	            // 设置其他属性
	            CurrentHitBox.destroyOnHit = hitBoxEvent.destroyOnHit;
	            CurrentHitBox.maxHits = hitBoxEvent.maxHits;
	        }
	    }
	    
	    public override void EffectRunning()
	    {
	        base.EffectRunning();
	    }
	    
	    public override void EndEffect()
	    {
	        if (CurrentHitBox != null && hitBoxObj != null)
	        {
	            GameObject.Destroy(hitBoxObj);
	            CurrentHitBox = null;
	        }
	        base.EndEffect();
	    }
	}
	
	public partial class AbilityEventEffect_CreateHitBox : AbilityEventEffect
	{
	    AbilityEventObj_CreateHitBox TargetObj => (AbilityEventObj_CreateHitBox)_EventObj;
	    
	    public AbilityEventEffect_CreateHitBox(AbilityEventObj InitObj) : base(InitObj)
	    {
	        _EventObj = InitObj;
	    }
	}
}

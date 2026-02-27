using UnityEngine;
using CombatEditor;

namespace CombatEditor
{
    /// <summary>
    /// 演示参数覆盖实时生效的测试脚本
    /// </summary>
    public class RuntimeParameterOverrideTest : MonoBehaviour
    {
        [Header("目标")]
        public CombatController combatController;
        
        [Header("测试参数")]
        public float speedMultiplier = 2.0f;
        public Vector3 hitBoxSizeMultiplier = new Vector3(1.5f, 1.5f, 1.5f);
        
        private RuntimeParameterManager _manager;
        private RuntimeParameterWrapper _wrapper;
        
        void Start()
        {
            if (combatController == null)
                combatController = GetComponent<CombatController>();
            
            if (combatController != null)
            {
                _manager = combatController.GetOrAddRuntimeParameterManager();
                _wrapper = combatController.GetRuntimeParameterWrapper();
            }
        }
        
        void Update()
        {
            if (Input.GetKeyDown(KeyCode.T))
            {
                TestParameterOverride();
            }
            
            if (Input.GetKeyDown(KeyCode.Y))
            {
                TestParameterRestore();
            }
        }
        
        /// <summary>
        /// 测试参数覆盖
        /// </summary>
        [ContextMenu("测试参数覆盖")]
        public void TestParameterOverride()
        {
            if (_wrapper == null) return;
            
            Debug.Log("=== 开始测试参数覆盖 ===");
            
            var actions = _wrapper.GetActions();
            foreach (var action in actions)
            {
                var tracks = action.GetTracks();
                foreach (var track in tracks)
                {
                    // 测试Speed参数
                    if (track.TypeName == "AbilityEventObj_AnimSpeed")
                    {
                        float originalSpeed = track.GetOriginalParameter<float>("Speed");
                        float newSpeed = originalSpeed * speedMultiplier;
                        
                        Debug.Log($"🔄 修改 {action.Name} 的 Speed: {originalSpeed} → {newSpeed}");
                        track.SetParameter("Speed", newSpeed);
                        
                        // 验证修改是否生效
                        float currentSpeed = track.GetParameter<float>("Speed");
                        Debug.Log($"✅ 当前Speed值: {currentSpeed}");
                    }
                    
                    // 测试HitBoxSize参数
                    if (track.TypeName == "AbilityEventObj_CreateHitBox")
                    {
                        Vector3 originalSize = track.GetOriginalParameter<Vector3>("hitBoxSize");
                        Vector3 newSize = Vector3.Scale(originalSize, hitBoxSizeMultiplier);
                        
                        Debug.Log($"🔄 修改 {action.Name} 的 hitBoxSize: {originalSize} → {newSize}");
                        track.SetParameter("hitBoxSize", newSize);
                        
                        // 验证修改是否生效
                        Vector3 currentSize = track.GetParameter<Vector3>("hitBoxSize");
                        Debug.Log($"✅ 当前hitBoxSize值: {currentSize}");
                    }
                }
            }
            
            Debug.Log("=== 参数覆盖测试完成 ===");
            Debug.Log("现在执行相关技能，你会看到修改后的效果！");
        }
        
        /// <summary>
        /// 测试参数恢复
        /// </summary>
        [ContextMenu("恢复所有参数")]
        public void TestParameterRestore()
        {
            if (_manager == null) return;
            
            Debug.Log("=== 恢复所有参数到原始值 ===");
            _manager.ClearAllOverrides();
            Debug.Log("✅ 所有参数已恢复到原始值");
        }
        
        /// <summary>
        /// 显示当前覆盖状态
        /// </summary>
        [ContextMenu("显示覆盖状态")]
        public void ShowOverrideStatus()
        {
            if (_manager == null) return;
            
            var overriddenPaths = _manager.GetOverriddenParameterPaths();
            Debug.Log($"当前覆盖的参数数量: {overriddenPaths.Count}");
            
            foreach (var path in overriddenPaths)
            {
                Debug.Log($"📝 覆盖参数: {path}");
            }
        }
    }
} 
using UnityEngine;

namespace CombatEditor
{
    public class AnimationDebugger : MonoBehaviour
    {
        public Animator animator;
        public bool enableDebugOutput = true;
        
        private void Start()
        {
            if (animator == null)
                animator = GetComponent<Animator>();
                
            if (animator != null && enableDebugOutput)
            {
                Debug.Log($"[动画调试器] 初始化完成，层数: {animator.layerCount}");
            }
        }
        
        private void Update()
        {
            if (!enableDebugOutput || animator == null)
                return;
                
            // 检查当前播放的动画
            for (int i = 0; i < animator.layerCount; i++)
            {
                if (!animator.IsInTransition(i))
                {
                    var stateInfo = animator.GetCurrentAnimatorStateInfo(i);
                    var clipInfo = animator.GetCurrentAnimatorClipInfo(i);
                    
                    if (clipInfo.Length > 0)
                    {
                        var clip = clipInfo[0].clip;
                        bool isLooping = clip.isLooping;
                        
                        // 只在关键时刻打印信息
                        if (stateInfo.normalizedTime > 0.9f || stateInfo.normalizedTime < 0.1f)
                        {
                            Debug.Log($"[动画状态] 层{i}: {clip.name}, 循环: {isLooping}, 时间: {stateInfo.normalizedTime:F3}, 长度: {clip.length:F2}s");
                        }
                    }
                }
            }
        }
        
        [ContextMenu("检查所有动画循环设置")]
        public void CheckAllAnimationLoopSettings()
        {
            if (animator == null)
            {
                Debug.LogError("未找到Animator组件");
                return;
            }
            
            Debug.Log("=== 动画循环设置检查 ===");
            
            // 检查RuntimeAnimatorController中的所有动画
            var controller = animator.runtimeAnimatorController;
            if (controller != null)
            {
                var clips = controller.animationClips;
                foreach (var clip in clips)
                {
                    Debug.Log($"动画: {clip.name}, 循环: {clip.isLooping}, 长度: {clip.length:F2}s");
                }
            }
        }
    }
} 
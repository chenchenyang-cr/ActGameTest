
using UnityEngine;

namespace CombatEditor
{
    /// <summary>
    /// 自动销毁已播放完毕的粒子系统
    /// </summary>
    public class ParticleSystemAutoDestroy : MonoBehaviour
    {
        private ParticleSystem ps;

        public void Start()
        {
            ps = GetComponent<ParticleSystem>();
        }

        public void Update()
        {
            if (ps && !ps.IsAlive())
            {
                Destroy(gameObject);
            }
        }
    }
} 
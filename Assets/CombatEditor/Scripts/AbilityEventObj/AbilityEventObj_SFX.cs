using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CombatEditor
{
    [AbilityEvent]
    [CreateAssetMenu(menuName = "AbilityEvents / SFX")]
    public class AbilityEventObj_SFX : AbilityEventObj
    {
        public List<AudioClip> clips;
        public float Volume = 1;
        public override EventTimeType GetEventTimeType()
        {
            return EventTimeType.EventTime;
        }
        public override AbilityEventEffect Initialize()
        {
            return new AbilityEventEffect_SFX(this);
        }
        public override AbilityEventPreview InitializePreview()
        {
            return new AbilityEventPreview_SFX(this);
        }
    }
    public class AbilityEventEffect_SFX : AbilityEventEffect
    {
        public AbilityEventEffect_SFX(AbilityEventObj Obj) : base(Obj)
        {
            _EventObj = Obj;
        }
        public override void StartEffect()
        {
            base.StartEffect();
            var clip = ((AbilityEventObj_SFX)_EventObj).clips[Random.Range(0, ((AbilityEventObj_SFX)_EventObj).clips.Count)];
            clip.PlayClip(((AbilityEventObj_SFX)_EventObj).Volume);
        }
    }

    public static class AudioHelper
    {
        private static AudioSourcePool audioPool;
        
        static AudioHelper()
        {
            // 初始化音频池
            InitializeAudioPool();
        }
        
        private static void InitializeAudioPool()
        {
            if (audioPool == null)
            {
                GameObject poolObject = new GameObject("SFX_AudioPool");
                GameObject.DontDestroyOnLoad(poolObject);
                audioPool = poolObject.AddComponent<AudioSourcePool>();
            }
        }
        
        public static void PlayClip(this AudioClip clip, float Volume = 1)
        {
            if (audioPool == null)
            {
                InitializeAudioPool();
            }
            audioPool.PlayClip(clip, Volume);
        }
    }
    
    public class AudioSourcePool : MonoBehaviour
    {
        private List<AudioSource> availableSources = new List<AudioSource>();
        private List<AudioSource> activeSources = new List<AudioSource>();
        private const int INITIAL_POOL_SIZE = 5;
        private const int MAX_POOL_SIZE = 15;
        
        void Awake()
        {
            // 创建初始音频源池
            for (int i = 0; i < INITIAL_POOL_SIZE; i++)
            {
                CreateNewAudioSource();
            }
        }
        
        private AudioSource CreateNewAudioSource()
        {
            GameObject audioObj = new GameObject($"PooledAudioSource_{availableSources.Count}");
            audioObj.transform.SetParent(this.transform);
            AudioSource source = audioObj.AddComponent<AudioSource>();
            source.playOnAwake = false;
            availableSources.Add(source);
            return source;
        }
        
        public void PlayClip(AudioClip clip, float volume)
        {
            if (clip == null) return;
            
            AudioSource source = GetAvailableAudioSource();
            if (source != null)
            {
                source.clip = clip;
                source.volume = volume;
                source.pitch = 1f; // 重置音调
                source.Play();
                
                // 移动到活跃列表
                availableSources.Remove(source);
                activeSources.Add(source);
                
                // 启动协程来回收音频源
                StartCoroutine(ReturnToPoolWhenFinished(source, clip.length));
            }
        }
        
        private AudioSource GetAvailableAudioSource()
        {
            // 如果有可用的音频源，直接使用
            if (availableSources.Count > 0)
            {
                return availableSources[0];
            }
            
            // 如果池未达到最大大小，创建新的
            if (availableSources.Count + activeSources.Count < MAX_POOL_SIZE)
            {
                return CreateNewAudioSource();
            }
            
            // 否则查找最早播放的音频源并重用（防止音频堆积）
            AudioSource oldestSource = null;
            float earliestTime = float.MaxValue;
            
            for (int i = activeSources.Count - 1; i >= 0; i--)
            {
                var source = activeSources[i];
                if (!source.isPlaying)
                {
                    // 如果发现已停止播放的源，立即回收
                    activeSources.RemoveAt(i);
                    availableSources.Add(source);
                    return source;
                }
                else if (source.time < earliestTime)
                {
                    earliestTime = source.time;
                    oldestSource = source;
                }
            }
            
            // 如果所有源都在播放，重用播放时间最长的
            if (oldestSource != null)
            {
                oldestSource.Stop();
                activeSources.Remove(oldestSource);
                availableSources.Add(oldestSource);
                return oldestSource;
            }
            
            // 最后手段：创建临时音频源（这种情况很少发生）
            return CreateNewAudioSource();
        }
        
        private System.Collections.IEnumerator ReturnToPoolWhenFinished(AudioSource source, float clipLength)
        {
            // 等待音频播放完成
            yield return new WaitForSeconds(clipLength + 0.1f); // 加一点缓冲时间
            
            // 检查音频源是否仍在活跃列表中且已停止播放
            if (activeSources.Contains(source) && !source.isPlaying)
            {
                activeSources.Remove(source);
                availableSources.Add(source);
                
                // 清理音频源状态
                source.clip = null;
                source.volume = 1f;
            }
        }
        
        // 清理方法，用于停止所有音频
        public void StopAllAudio()
        {
            foreach (var source in activeSources)
            {
                if (source.isPlaying)
                {
                    source.Stop();
                }
            }
            
            // 将所有活跃源返回到可用池
            availableSources.AddRange(activeSources);
            activeSources.Clear();
        }
    }

}

using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using MEC;

namespace Sound
{
    public class SoundManager : MonoBehaviour
    {
        public static SoundManager Instance = null;

        private Dictionary<string, AudioClip> _soundPool = new();
        private Dictionary<string, AsyncOperationHandle<AudioClip>> _handlePool = new(); // 로딩된 핸들
        private readonly Dictionary<string, List<string>> _labelToClipNames = new(); // 라벨, 클립 이름 목록

        private Dictionary<string, Task<AudioClip>> _loadingTasks = new();
        private Dictionary<string, Task> _loadingLabels = new();
        private HashSet<string> _loadedLabels = new();

        // 시간 추적 + 캐시 제어용
        private readonly Dictionary<string, float> _clipUsageTime = new();
        [SerializeField] private int _maxCacheSize = 100;
        [SerializeField] private float _cacheCleanupInterval = 10f; // 초 단위, 기본값 10초

        private CoroutineHandle _cleanupHandle; 
        
        [Header("Global Audio Settings")]
        public AudioSource bgmAudioSource;
        [SerializeField] private string _bgmName = "";
        public AudioSource feedbackSource;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(this.gameObject);
            }
            else if (Instance != this)
            {
                Debug.LogWarning("SoundManager is Duplicated!! Remaining One Instance ...");
                Destroy(this.gameObject);
                return;
            }
        }

        private void Start()
        {
            _cleanupHandle = Timing.RunCoroutine(CacheCleanupLoopMEC());

            if (bgmAudioSource == null)
                bgmAudioSource = GetComponent<AudioSource>();

            if (!string.IsNullOrEmpty(_bgmName) && bgmAudioSource != null)
            {
                PlayClip(_bgmName, loop: true, isBgm: true);
            }
        }

        private void OnDestroy()
        {
            Timing.KillCoroutines(_cleanupHandle); 
            ReleaseAllSounds();
        }

        #region Addressable Sound Asset Calls
        /// <summary>
        /// 특정 AudioSource를 통해 효과음(OneShot)을 재생합니다.
        /// </summary>
        public async void PlaySound(string key, AudioSource source, float volumeScale = 1.0f)
        {
            try
            {
                if (source == null || !source.gameObject.activeInHierarchy || !source.enabled)
                    return;

                AudioClip clip = await LoadSound(key);

                if (clip != null && source != null && source.isActiveAndEnabled)
                {
                    source.PlayOneShot(clip, volumeScale);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[SoundManager] PlaySound Failed ({key}): {e.Message}");
            }
        }

        /// <summary>
        /// BGM이나 끊기지 않는 소리용. AudioSource의 클립을 갈아끼우고 재생합니다.
        /// </summary>
        public async void PlayClip(string key, bool loop = true, bool isBgm = false)
        {
            try
            {
                if (bgmAudioSource == null) return;

                AudioClip clip = await LoadSound(key);

                if (clip != null && bgmAudioSource != null)
                {
                    // 이미 같은 노래가 재생 중이면 무시
                    if (bgmAudioSource.clip == clip && bgmAudioSource.isPlaying) return;

                    bgmAudioSource.Stop();
                    bgmAudioSource.clip = clip;
                    bgmAudioSource.loop = loop;
                    bgmAudioSource.Play();
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[SoundManager] PlayClip Failed ({key}): {e.Message}");
            }
        }

        /// <summary>
        /// 사운드 클립을 로드한다
        /// </summary>
        /// <param name="key"> 사운드 이름</param>
        /// <returns> 로드된 오디오 클립 </returns>
        public async Task<AudioClip> LoadSound(string key)
        {
            if (_soundPool.TryGetValue(key, out var clip))
            {
                _clipUsageTime[key] = Time.time;
                return clip;
            }

            if (_loadingTasks.TryGetValue(key, out var existingTask))
            {
                return await existingTask;
            }

            var loadTask = LoadInternal(key);
            _loadingTasks[key] = loadTask;

            try
            {
                var loadedClip = await loadTask;
                if (loadedClip != null)
                {
                    _clipUsageTime[key] = Time.time;
                }
                return loadedClip;
            }
            finally
            {
                _loadingTasks.Remove(key); // 무조건 제거해야 중복 방지 로직이 깨지지 않음
            }
        }
        #endregion

        #region Addressable Load & Hashing Logics
        private async Task<AudioClip> LoadInternal(string key)
        {
            try
            {
                var handle = Addressables.LoadAssetAsync<AudioClip>(key);
                await handle.Task;

                if (handle.Status == AsyncOperationStatus.Succeeded)
                {
                    _soundPool[key] = handle.Result;
                    _handlePool[key] = handle;
                    return handle.Result;
                }
                else
                {
                    Debug.LogWarning($"[SoundManager] Asset Load Failed: {key}");
                    Addressables.Release(handle);
                    return null;
                }
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// 어드레서블 에셋 매니저를 이용해 사운드 데이터를 라벨로 로드하고 Dictionary에 캐싱한다
        /// </summary>
        /// <param name="label"> 사운드 에셋 라벨 </param>
        /// <returns></returns>
        public Task LoadSoundsByLabel(string label)
        {
            if (_loadedLabels.Contains(label))
                return Task.CompletedTask;

            if (_loadingLabels.TryGetValue(label, out var existingTask))
                return existingTask;

            var newTask = InternalLoadSoundsByLabel(label);
            _loadingLabels[label] = newTask;
            return newTask;
        }

        private async Task InternalLoadSoundsByLabel(string label)
        {
            var locationsHandle = Addressables.LoadResourceLocationsAsync(label, typeof(AudioClip));
            await locationsHandle.Task;

            if (locationsHandle.Status == AsyncOperationStatus.Succeeded)
            {
                var tasks = new List<Task>();
                var loadedKeys = new List<string>();

                foreach (var location in locationsHandle.Result)
                {
                    string key = location.PrimaryKey;

                    if (!_soundPool.ContainsKey(key))
                    {
                        tasks.Add(LoadSound(key));
                        loadedKeys.Add(key);
                    }
                }

                await Task.WhenAll(tasks);

                if (loadedKeys.Count > 0)
                {
                    _loadedLabels.Add(label);
                    _labelToClipNames[label] = loadedKeys;
                    Debug.Log($"[SoundManager] Label '{label}': {loadedKeys.Count} clips loaded.");

                    CleanupLRUCache();
                }
            }
            else
            {
                Debug.LogError($"[SoundManager] Failed to load locations for label: {label}");
            }

            Addressables.Release(locationsHandle);
            _loadingLabels.Remove(label);
        }

        /// <summary>
        /// 특정 사운드 리소스를 메모리에서 해제
        /// </summary>
        public void ReleaseSound(string clipName)
        {
            if (_handlePool.TryGetValue(clipName, out var handle))
            {
                Addressables.Release(handle);
                _handlePool.Remove(clipName);
            }

            _soundPool.Remove(clipName);
        }

        /// <summary>
        /// 전체 캐시된 사운드를 해제
        /// </summary>
        public void ReleaseAllSounds()
        {
            foreach (var handle in _handlePool.Values)
            {
                Addressables.Release(handle);
            }

            _handlePool.Clear();
            _soundPool.Clear(); 
            _clipUsageTime.Clear();
        }
        #endregion

        #region Sound Cache Handle Logics

        private IEnumerator<float> CacheCleanupLoopMEC()
        {
            while (true)
            {
                yield return Timing.WaitForSeconds(_cacheCleanupInterval);
                CleanupLRUCache();
            }
        }

        private void CleanupLRUCache()
        {
            if (_soundPool.Count <= _maxCacheSize) return;

            AudioClip currentBgmClip = null;
            if (bgmAudioSource != null && bgmAudioSource.isPlaying)
            {
                currentBgmClip = bgmAudioSource.clip;
            }

            var sortedByTime = new List<KeyValuePair<string, float>>(_clipUsageTime);
            sortedByTime.Sort((a, b) => a.Value.CompareTo(b.Value));

            int removeCount = _soundPool.Count - _maxCacheSize;
            int removed = 0;

            for (int i = 0; i < sortedByTime.Count; i++)
            {
                if (removed >= removeCount) break;

                string key = sortedByTime[i].Key;

                if (_soundPool.TryGetValue(key, out var clip) && clip == currentBgmClip)
                {
                    continue;
                }

                ReleaseSound(key);
                _clipUsageTime.Remove(key);
                removed++;
            }

            // 관련 라벨에서 제거된 클립 이름이 전부 사라졌다면 라벨도 정리
            var labelsToRemove = new List<string>();
            foreach (var kvp in _labelToClipNames)
            {
                bool allClipsRemoved = true;
                foreach (var clipName in kvp.Value)
                {
                    if (_soundPool.ContainsKey(clipName))
                    {
                        allClipsRemoved = false;
                        break;
                    }
                }
                if (allClipsRemoved) labelsToRemove.Add(kvp.Key);
            }

            foreach (var label in labelsToRemove)
            {
                _loadedLabels.Remove(label);
                _labelToClipNames.Remove(label);
            }

            foreach (var label in labelsToRemove)
            {
                _loadedLabels.Remove(label);
                _labelToClipNames.Remove(label);
                Debug.Log($"[SoundManager] {label} 라벨의 모든 클립이 제거되어 라벨 기록도 삭제됨.");
            }
        }
        #endregion
    }
}
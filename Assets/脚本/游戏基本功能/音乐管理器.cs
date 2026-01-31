using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Audio;
using System;
using System.Collections;

/// <summary>
/// 音乐管理器 - 单例模式
/// 功能：
/// 1. 根据场景自动切换BGM
/// 2. 支持淡入淡出过渡效果
/// 3. 支持触发特殊BGM（如Boss战、特殊事件等）
/// 4. 特殊BGM结束后可自动恢复场景BGM
/// </summary>
public class MusicManager : MonoBehaviour
{
    #region 单例模式
    public static MusicManager Instance { get; private set; }
    #endregion

    #region 数据结构
    [Serializable]
    public class MusicGroup
    {
        [Tooltip("音乐组名称（用于手动调用）")]
        public string groupName;
        
        [Tooltip("背景音乐")]
        public AudioClip musicClip;
        
        [Tooltip("属于该音乐组的场景名称")]
        public string[] sceneNames;
        
        [Tooltip("音量 (0-1)")]
        [Range(0f, 1f)]
        public float volume = 1f;
        
        [Tooltip("是否循环播放")]
        public bool loop = true;
    }

    [Serializable]
    public class SpecialBGM
    {
        [Tooltip("特殊BGM名称（用于触发）")]
        public string bgmName;
        
        [Tooltip("特殊BGM音频")]
        public AudioClip clip;
        
        [Tooltip("音量 (0-1)")]
        [Range(0f, 1f)]
        public float volume = 1f;
        
        [Tooltip("是否循环播放")]
        public bool loop = true;
        
        [Tooltip("播放结束后是否自动恢复场景BGM")]
        public bool autoResumeSceneBGM = true;
        
        [Tooltip("触发优先级（数值越高优先级越高）")]
        public int priority = 0;
    }
    #endregion

    #region Inspector设置
    [Header("=== 音频混合器 ===")]
    [Tooltip("Audio Mixer（可选）")]
    public AudioMixer audioMixer;
    
    [Tooltip("音乐混合器组")]
    public AudioMixerGroup musicMixerGroup;

    [Header("=== 场景音乐设置 ===")]
    [Tooltip("音乐组 - 配置各场景对应的BGM")]
    public MusicGroup[] musicGroups;

    [Header("=== 特殊BGM设置 ===")]
    [Tooltip("特殊BGM列表（Boss战、特殊事件等）")]
    public SpecialBGM[] specialBGMs;

    [Header("=== 过渡设置 ===")]
    [Tooltip("淡入淡出时长（秒）")]
    public float fadeDuration = 1.5f;

    [Header("=== 默认设置 ===")]
    [Tooltip("未配置场景的默认音乐（可选）")]
    public AudioClip defaultMusic;
    
    [Tooltip("默认音量")]
    [Range(0f, 1f)]
    public float defaultVolume = 0.7f;
    #endregion

    #region 私有变量
    private AudioSource audioSourceA;
    private AudioSource audioSourceB;
    private AudioSource currentAudioSource;
    
    private MusicGroup currentMusicGroup;
    private SpecialBGM currentSpecialBGM;
    private Coroutine fadeCoroutine;
    private Coroutine specialBGMCoroutine;
    
    private bool isPlayingSpecialBGM = false;
    private float masterVolume = 1f;
    #endregion

    #region Unity生命周期
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeAudioSources();
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Start()
    {
        // 加载保存的音量设置
        masterVolume = PlayerPrefs.GetFloat("MusicVolume", 1f);
        
        // 播放当前场景的音乐
        string currentSceneName = SceneManager.GetActiveScene().name;
        PlayMusicForScene(currentSceneName, false);
    }
    #endregion

    #region 初始化
    private void InitializeAudioSources()
    {
        audioSourceA = gameObject.AddComponent<AudioSource>();
        audioSourceB = gameObject.AddComponent<AudioSource>();

        ConfigureAudioSource(audioSourceA);
        ConfigureAudioSource(audioSourceB);

        currentAudioSource = audioSourceA;
    }

    private void ConfigureAudioSource(AudioSource source)
    {
        source.loop = true;
        source.playOnAwake = false;
        source.volume = 0f;
        
        if (musicMixerGroup != null)
        {
            source.outputAudioMixerGroup = musicMixerGroup;
        }
    }
    #endregion

    #region 场景音乐管理
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 如果正在播放特殊BGM，不切换场景音乐
        if (isPlayingSpecialBGM) return;
        
        PlayMusicForScene(scene.name, true);
    }

    private void PlayMusicForScene(string sceneName, bool withFade)
    {
        MusicGroup targetGroup = GetMusicGroupForScene(sceneName);

        // 如果是同一个音乐组且正在播放，则不重复播放
        if (targetGroup == currentMusicGroup && currentAudioSource != null && currentAudioSource.isPlaying)
        {
            return;
        }

        if (withFade)
        {
            CrossFadeToMusic(targetGroup);
        }
        else
        {
            PlayMusicImmediate(targetGroup);
        }
    }

    private MusicGroup GetMusicGroupForScene(string sceneName)
    {
        if (musicGroups != null)
        {
            foreach (var group in musicGroups)
            {
                if (group.sceneNames != null)
                {
                    foreach (var scene in group.sceneNames)
                    {
                        if (scene == sceneName)
                        {
                            return group;
                        }
                    }
                }
            }
        }
        return null;
    }

    private void PlayMusicImmediate(MusicGroup group)
    {
        currentMusicGroup = group;

        AudioClip clipToPlay = group != null ? group.musicClip : defaultMusic;
        float targetVolume = (group != null ? group.volume : defaultVolume) * masterVolume;
        bool shouldLoop = group != null ? group.loop : true;

        if (clipToPlay == null)
        {
            if (currentAudioSource != null)
            {
                currentAudioSource.Stop();
            }
            return;
        }

        currentAudioSource.clip = clipToPlay;
        currentAudioSource.volume = targetVolume;
        currentAudioSource.loop = shouldLoop;
        currentAudioSource.time = 0f;
        currentAudioSource.Play();
    }

    private void CrossFadeToMusic(MusicGroup group)
    {
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
        }

        fadeCoroutine = StartCoroutine(CrossFadeCoroutine(group));
    }

    private IEnumerator CrossFadeCoroutine(MusicGroup targetGroup)
    {
        AudioSource fadeOutSource = currentAudioSource;
        AudioSource fadeInSource = (currentAudioSource == audioSourceA) ? audioSourceB : audioSourceA;

        AudioClip clipToPlay = targetGroup != null ? targetGroup.musicClip : defaultMusic;
        float targetVolume = (targetGroup != null ? targetGroup.volume : defaultVolume) * masterVolume;
        bool shouldLoop = targetGroup != null ? targetGroup.loop : true;

        if (clipToPlay == null)
        {
            yield return StartCoroutine(FadeOut(fadeOutSource, fadeDuration));
            currentMusicGroup = null;
            yield break;
        }

        fadeInSource.clip = clipToPlay;
        fadeInSource.volume = 0f;
        fadeInSource.loop = shouldLoop;
        fadeInSource.time = 0f;
        fadeInSource.Play();

        float timer = 0f;
        float startVolumeOut = fadeOutSource.volume;

        while (timer < fadeDuration)
        {
            timer += Time.unscaledDeltaTime;
            float progress = timer / fadeDuration;

            if (fadeOutSource.isPlaying)
            {
                fadeOutSource.volume = Mathf.Lerp(startVolumeOut, 0f, progress);
            }

            fadeInSource.volume = Mathf.Lerp(0f, targetVolume, progress);

            yield return null;
        }

        fadeOutSource.volume = 0f;
        fadeOutSource.Stop();
        fadeInSource.volume = targetVolume;

        currentAudioSource = fadeInSource;
        currentMusicGroup = targetGroup;
    }

    private IEnumerator FadeOut(AudioSource source, float duration)
    {
        float startVolume = source.volume;
        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.unscaledDeltaTime;
            source.volume = Mathf.Lerp(startVolume, 0f, timer / duration);
            yield return null;
        }

        source.volume = 0f;
        source.Stop();
    }
    #endregion

    #region ========== 特殊BGM功能 ==========
    
    /// <summary>
    /// 通过名称触发特殊BGM
    /// </summary>
    /// <param name="bgmName">特殊BGM名称</param>
    /// <param name="withFade">是否淡入淡出</param>
    public void TriggerSpecialBGM(string bgmName, bool withFade = true)
    {
        SpecialBGM targetBGM = GetSpecialBGMByName(bgmName);
        
        if (targetBGM == null)
        {
            Debug.LogWarning($"特殊BGM '{bgmName}' 未找到！");
            return;
        }

        // 检查优先级
        if (isPlayingSpecialBGM && currentSpecialBGM != null)
        {
            if (targetBGM.priority < currentSpecialBGM.priority)
            {
                Debug.Log($"特殊BGM '{bgmName}' 优先级低于当前播放的BGM，已忽略");
                return;
            }
        }

        PlaySpecialBGM(targetBGM, withFade);
    }

    /// <summary>
    /// 直接播放特殊BGM（通过AudioClip）
    /// </summary>
    /// <param name="clip">音频片段</param>
    /// <param name="volume">音量</param>
    /// <param name="loop">是否循环</param>
    /// <param name="autoResume">是否自动恢复场景BGM</param>
    public void TriggerSpecialBGM(AudioClip clip, float volume = 1f, bool loop = false, bool autoResume = true)
    {
        if (clip == null)
        {
            Debug.LogWarning("特殊BGM音频片段为空！");
            return;
        }

        SpecialBGM tempBGM = new SpecialBGM
        {
            bgmName = "TempSpecialBGM",
            clip = clip,
            volume = volume,
            loop = loop,
            autoResumeSceneBGM = autoResume,
            priority = 100 // 直接调用的临时BGM给予高优先级
        };

        PlaySpecialBGM(tempBGM, true);
    }

    private SpecialBGM GetSpecialBGMByName(string bgmName)
    {
        if (specialBGMs != null)
        {
            foreach (var bgm in specialBGMs)
            {
                if (bgm.bgmName == bgmName)
                {
                    return bgm;
                }
            }
        }
        return null;
    }

    private void PlaySpecialBGM(SpecialBGM bgm, bool withFade)
    {
        // 停止之前的特殊BGM协程
        if (specialBGMCoroutine != null)
        {
            StopCoroutine(specialBGMCoroutine);
        }

        specialBGMCoroutine = StartCoroutine(PlaySpecialBGMCoroutine(bgm, withFade));
    }

    private IEnumerator PlaySpecialBGMCoroutine(SpecialBGM bgm, bool withFade)
    {
        isPlayingSpecialBGM = true;
        currentSpecialBGM = bgm;

        AudioSource fadeOutSource = currentAudioSource;
        AudioSource fadeInSource = (currentAudioSource == audioSourceA) ? audioSourceB : audioSourceA;

        float targetVolume = bgm.volume * masterVolume;

        // 淡出当前音乐
        if (withFade && fadeOutSource.isPlaying)
        {
            float timer = 0f;
            float startVolumeOut = fadeOutSource.volume;

            fadeInSource.clip = bgm.clip;
            fadeInSource.volume = 0f;
            fadeInSource.loop = bgm.loop;
            fadeInSource.time = 0f;
            fadeInSource.Play();

            while (timer < fadeDuration)
            {
                timer += Time.unscaledDeltaTime;
                float progress = timer / fadeDuration;

                fadeOutSource.volume = Mathf.Lerp(startVolumeOut, 0f, progress);
                fadeInSource.volume = Mathf.Lerp(0f, targetVolume, progress);

                yield return null;
            }

            fadeOutSource.Stop();
        }
        else
        {
            fadeOutSource.Stop();
            fadeInSource.clip = bgm.clip;
            fadeInSource.volume = targetVolume;
            fadeInSource.loop = bgm.loop;
            fadeInSource.time = 0f;
            fadeInSource.Play();
        }

        currentAudioSource = fadeInSource;

        // 如果不循环，等待播放完毕
        if (!bgm.loop)
        {
            yield return new WaitForSecondsRealtime(bgm.clip.length);

            // 自动恢复场景BGM
            if (bgm.autoResumeSceneBGM)
            {
                StopSpecialBGM(true);
            }
            else
            {
                isPlayingSpecialBGM = false;
                currentSpecialBGM = null;
            }
        }
    }

    /// <summary>
    /// 停止特殊BGM并恢复场景音乐
    /// </summary>
    /// <param name="withFade">是否淡入淡出</param>
    public void StopSpecialBGM(bool withFade = true)
    {
        if (!isPlayingSpecialBGM) return;

        if (specialBGMCoroutine != null)
        {
            StopCoroutine(specialBGMCoroutine);
        }

        isPlayingSpecialBGM = false;
        currentSpecialBGM = null;

        // 恢复场景BGM
        string currentSceneName = SceneManager.GetActiveScene().name;
        PlayMusicForScene(currentSceneName, withFade);
    }

    /// <summary>
    /// 检查是否正在播放特殊BGM
    /// </summary>
    public bool IsPlayingSpecialBGM => isPlayingSpecialBGM;

    /// <summary>
    /// 获取当前特殊BGM名称
    /// </summary>
    public string CurrentSpecialBGMName => currentSpecialBGM?.bgmName;
    #endregion

    #region ========== 公共控制方法 ==========
    
    /// <summary>
    /// 设置音乐音量
    /// </summary>
    /// <param name="volume">音量值 (0-1)</param>
    public void SetMusicVolume(float volume)
    {
        masterVolume = Mathf.Clamp01(volume);
        PlayerPrefs.SetFloat("MusicVolume", masterVolume);
        PlayerPrefs.Save();

        // 更新当前播放的音量
        if (currentAudioSource != null && currentAudioSource.isPlaying)
        {
            float baseVolume;
            if (isPlayingSpecialBGM && currentSpecialBGM != null)
            {
                baseVolume = currentSpecialBGM.volume;
            }
            else
            {
                baseVolume = currentMusicGroup != null ? currentMusicGroup.volume : defaultVolume;
            }
            currentAudioSource.volume = baseVolume * masterVolume;
        }
    }

    /// <summary>
    /// 获取当前音量
    /// </summary>
    public float GetMusicVolume()
    {
        return masterVolume;
    }

    /// <summary>
    /// 暂停音乐
    /// </summary>
    public void PauseMusic()
    {
        if (currentAudioSource != null)
        {
            currentAudioSource.Pause();
        }
    }

    /// <summary>
    /// 恢复音乐
    /// </summary>
    public void ResumeMusic()
    {
        if (currentAudioSource != null)
        {
            currentAudioSource.UnPause();
        }
    }

    /// <summary>
    /// 停止所有音乐
    /// </summary>
    public void StopMusic()
    {
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
        }
        if (specialBGMCoroutine != null)
        {
            StopCoroutine(specialBGMCoroutine);
        }

        if (audioSourceA != null) audioSourceA.Stop();
        if (audioSourceB != null) audioSourceB.Stop();

        currentMusicGroup = null;
        currentSpecialBGM = null;
        isPlayingSpecialBGM = false;
    }

    /// <summary>
    /// 通过名称播放音乐组
    /// </summary>
    /// <param name="groupName">音乐组名称</param>
    /// <param name="withFade">是否淡入淡出</param>
    public void PlayMusicGroup(string groupName, bool withFade = true)
    {
        // 停止特殊BGM
        if (isPlayingSpecialBGM)
        {
            isPlayingSpecialBGM = false;
            currentSpecialBGM = null;
        }

        MusicGroup targetGroup = null;

        if (musicGroups != null)
        {
            foreach (var group in musicGroups)
            {
                if (group.groupName == groupName)
                {
                    targetGroup = group;
                    break;
                }
            }
        }

        if (targetGroup != null)
        {
            if (withFade)
            {
                CrossFadeToMusic(targetGroup);
            }
            else
            {
                PlayMusicImmediate(targetGroup);
            }
        }
        else
        {
            Debug.LogWarning($"音乐组 '{groupName}' 未找到！");
        }
    }

    /// <summary>
    /// 静音切换
    /// </summary>
    public void ToggleMute()
    {
        if (currentAudioSource != null)
        {
            currentAudioSource.mute = !currentAudioSource.mute;
        }
    }
    #endregion
}

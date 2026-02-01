using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// 统一的场景过渡管理器
/// 使用单例模式，跨场景保持
/// 所有场景切换的淡入淡出都通过这里处理
/// </summary>
public class TransitionManager : MonoBehaviour
{
    public static TransitionManager Instance { get; private set; }

    [Header("=== 过渡设置 ===")]
    [Tooltip("默认过渡颜色")]
    public Color defaultColor = Color.black;
    
    [Tooltip("默认淡出时长")]
    public float defaultFadeOutDuration = 0.5f;
    
    [Tooltip("默认淡入时长")]
    public float defaultFadeInDuration = 0.5f;
    
    [Tooltip("场景加载后等待时间（等待相机等就位）")]
    public float waitAfterLoad = 0.2f;

    [Header("=== Debug ===")]
    public bool enableDebug = false;

    // 过渡状态
    private GameObject transitionCanvas;
    private Image transitionImage;
    private CanvasGroup canvasGroup;
    private bool isTransitioning = false;

    // 跨场景数据
    private static bool needsFadeIn = false;
    private static Color fadeInColor = Color.black;
    private static float fadeInDuration = 0.5f;
    private static float fadeInWaitTime = 0.2f;

    public bool IsTransitioning => isTransitioning;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            CreateTransitionCanvas();
            
            if (enableDebug) Debug.Log("[TransitionManager] 初始化完成");
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // 检查是否需要播放淡入
        if (needsFadeIn)
        {
            needsFadeIn = false;
            StartCoroutine(PlayFadeIn(fadeInColor, fadeInDuration, fadeInWaitTime));
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

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (enableDebug) Debug.Log("[TransitionManager] 场景加载: " + scene.name + ", needsFadeIn: " + needsFadeIn + ", isTransitioning: " + isTransitioning);
        
        // 场景加载后播放淡入
        if (needsFadeIn)
        {
            needsFadeIn = false;
            StartCoroutine(PlayFadeIn(fadeInColor, fadeInDuration, fadeInWaitTime));
        }
        else
        {
            // 如果不需要淡入，确保重置过渡状态
            isTransitioning = false;
            if (transitionCanvas != null)
            {
                transitionCanvas.SetActive(false);
            }
            if (enableDebug) Debug.Log("[TransitionManager] 无需淡入，重置过渡状态");
        }
    }

    /// <summary>
    /// 创建过渡 Canvas
    /// </summary>
    private void CreateTransitionCanvas()
    {
        // 创建 Canvas
        transitionCanvas = new GameObject("TransitionCanvas");
        transitionCanvas.transform.SetParent(transform);
        
        Canvas canvas = transitionCanvas.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 99999; // 最高层级
        
        transitionCanvas.AddComponent<CanvasScaler>();
        
        // 创建遮罩 Image
        GameObject imageObj = new GameObject("TransitionImage");
        imageObj.transform.SetParent(transitionCanvas.transform, false);
        
        transitionImage = imageObj.AddComponent<Image>();
        transitionImage.color = defaultColor;
        transitionImage.raycastTarget = false;
        
        RectTransform rt = imageObj.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        
        canvasGroup = imageObj.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;
        
        // 初始隐藏
        transitionCanvas.SetActive(false);
    }

    // ==================== 公共方法 ====================

    /// <summary>
    /// 加载场景（带过渡动画）
    /// </summary>
    public void LoadScene(string sceneName, Color? color = null, float? fadeOut = null, float? fadeIn = null, float? wait = null)
    {
        if (isTransitioning)
        {
            if (enableDebug) Debug.LogWarning("[TransitionManager] 正在过渡中，忽略请求");
            return;
        }
        
        Color useColor = color ?? defaultColor;
        float useFadeOut = fadeOut ?? defaultFadeOutDuration;
        float useFadeIn = fadeIn ?? defaultFadeInDuration;
        float useWait = wait ?? waitAfterLoad;
        
        StartCoroutine(TransitionToScene(sceneName, useColor, useFadeOut, useFadeIn, useWait));
    }

    /// <summary>
    /// 加载场景（通过索引）
    /// </summary>
    public void LoadSceneByIndex(int index, Color? color = null, float? fadeOut = null, float? fadeIn = null, float? wait = null)
    {
        string sceneName = SceneUtility.GetScenePathByBuildIndex(index);
        sceneName = System.IO.Path.GetFileNameWithoutExtension(sceneName);
        LoadScene(sceneName, color, fadeOut, fadeIn, wait);
    }

    /// <summary>
    /// 返回主菜单（白色过渡）
    /// </summary>
    public void ReturnToMainMenu(string menuSceneName, float fadeOutDuration = 1f, float fadeInDuration = 1f)
    {
        LoadScene(menuSceneName, Color.white, fadeOutDuration, fadeInDuration, 0.1f);
    }

    /// <summary>
    /// 只播放淡出（不切换场景）
    /// </summary>
    public void FadeOut(Color? color = null, float? duration = null, System.Action onComplete = null)
    {
        if (isTransitioning) return;
        
        Color useColor = color ?? defaultColor;
        float useDuration = duration ?? defaultFadeOutDuration;
        
        StartCoroutine(PlayFadeOutOnly(useColor, useDuration, onComplete));
    }

    /// <summary>
    /// 只播放淡入（从当前状态）
    /// </summary>
    public void FadeIn(float? duration = null, System.Action onComplete = null)
    {
        float useDuration = duration ?? defaultFadeInDuration;
        StartCoroutine(PlayFadeInOnly(useDuration, onComplete));
    }

    /// <summary>
    /// 强制重置过渡状态（用于修复卡住的情况）
    /// </summary>
    public void ForceReset()
    {
        StopAllCoroutines();
        isTransitioning = false;
        needsFadeIn = false;
        
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
        }
        if (transitionCanvas != null)
        {
            transitionCanvas.SetActive(false);
        }
        
        if (enableDebug) Debug.Log("[TransitionManager] 强制重置完成");
    }

    // ==================== 内部协程 ====================

    private IEnumerator TransitionToScene(string sceneName, Color color, float fadeOutDur, float fadeInDur, float waitTime)
    {
        isTransitioning = true;
        
        if (enableDebug) Debug.Log($"[TransitionManager] 开始过渡到 {sceneName}，颜色: {color}");
        
        // 保存淡入参数供新场景使用
        needsFadeIn = true;
        fadeInColor = color;
        fadeInDuration = fadeInDur;
        fadeInWaitTime = waitTime;
        
        // 播放淡出
        yield return StartCoroutine(PlayFadeOut(color, fadeOutDur));
        
        if (enableDebug) Debug.Log("[TransitionManager] 淡出完成，加载场景");
        
        // 加载场景
        SceneManager.LoadScene(sceneName);
        
        // 注意：淡入会在 OnSceneLoaded 中触发
    }

    private IEnumerator PlayFadeOut(Color color, float duration)
    {
        transitionCanvas.SetActive(true);
        transitionImage.color = color;
        canvasGroup.alpha = 0f;
        
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.Clamp01(elapsed / duration);
            yield return null;
        }
        
        canvasGroup.alpha = 1f;
    }

    private IEnumerator PlayFadeIn(Color color, float duration, float waitTime)
    {
        if (enableDebug) Debug.Log($"[TransitionManager] 开始淡入，等待: {waitTime}s");
        
        transitionCanvas.SetActive(true);
        transitionImage.color = color;
        canvasGroup.alpha = 1f;
        
        // 等待相机等就位
        yield return new WaitForSecondsRealtime(waitTime);
        
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            canvasGroup.alpha = 1f - Mathf.Clamp01(elapsed / duration);
            yield return null;
        }
        
        canvasGroup.alpha = 0f;
        transitionCanvas.SetActive(false);
        isTransitioning = false;
        
        if (enableDebug) Debug.Log("[TransitionManager] 淡入完成");
    }

    private IEnumerator PlayFadeOutOnly(Color color, float duration, System.Action onComplete)
    {
        isTransitioning = true;
        
        transitionCanvas.SetActive(true);
        transitionImage.color = color;
        canvasGroup.alpha = 0f;
        
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.Clamp01(elapsed / duration);
            yield return null;
        }
        
        canvasGroup.alpha = 1f;
        isTransitioning = false;
        
        onComplete?.Invoke();
    }

    private IEnumerator PlayFadeInOnly(float duration, System.Action onComplete)
    {
        if (canvasGroup == null || canvasGroup.alpha <= 0)
        {
            onComplete?.Invoke();
            yield break;
        }
        
        isTransitioning = true;
        
        float startAlpha = canvasGroup.alpha;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, elapsed / duration);
            yield return null;
        }
        
        canvasGroup.alpha = 0f;
        transitionCanvas.SetActive(false);
        isTransitioning = false;
        
        onComplete?.Invoke();
    }

    // 编辑器重置
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        Instance = null;
        needsFadeIn = false;
        fadeInColor = Color.black;
        fadeInDuration = 0.5f;
        fadeInWaitTime = 0.2f;
    }
}

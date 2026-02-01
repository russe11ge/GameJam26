using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// 场景过渡管理器 - 只负责场景切换时的淡入淡出动画
/// </summary>
public class TransitionManager : MonoBehaviour
{
    public static TransitionManager Instance;

    [Header("=== 过渡设置 ===")]
    [Tooltip("默认过渡颜色")]
    public Color defaultColor = Color.black;
    
    [Tooltip("淡出时长")]
    public float fadeOutDuration = 0.5f;
    
    [Tooltip("淡入时长")]
    public float fadeInDuration = 0.5f;
    
    [Tooltip("场景加载后等待时间")]
    public float waitAfterLoad = 0.3f;

    [Header("=== Debug ===")]
    public bool enableDebug = true;

    // 过渡 UI
    private GameObject transitionCanvas;
    private Image transitionImage;
    private CanvasGroup canvasGroup;

    // 跨场景数据
    private static bool needsFadeIn = false;
    private static Color fadeInColor = Color.black;

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
        if (enableDebug) Debug.Log($"[TransitionManager] 场景加载: {scene.name}");
        
        if (needsFadeIn)
        {
            needsFadeIn = false;
            StartCoroutine(PlayFadeIn());
        }
    }

    private void CreateTransitionCanvas()
    {
        transitionCanvas = new GameObject("TransitionCanvas");
        transitionCanvas.transform.SetParent(transform);
        
        Canvas canvas = transitionCanvas.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 9999;
        
        canvasGroup = transitionCanvas.AddComponent<CanvasGroup>();
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;
        
        GameObject panel = new GameObject("Panel");
        panel.transform.SetParent(transitionCanvas.transform, false);
        
        transitionImage = panel.AddComponent<Image>();
        transitionImage.color = defaultColor;
        transitionImage.raycastTarget = false;
        
        RectTransform rt = panel.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        
        canvasGroup.alpha = 0f;
        transitionCanvas.SetActive(false);
    }

    /// <summary>
    /// 加载场景（带过渡动画）
    /// </summary>
    public void LoadScene(string sceneName, Color? color = null)
    {
        Color useColor = color ?? defaultColor;
        StartCoroutine(TransitionToScene(sceneName, useColor));
    }

    /// <summary>
    /// 加载场景（通过索引）
    /// </summary>
    public void LoadSceneByIndex(int index, Color? color = null)
    {
        string sceneName = SceneUtility.GetScenePathByBuildIndex(index);
        sceneName = System.IO.Path.GetFileNameWithoutExtension(sceneName);
        LoadScene(sceneName, color);
    }

    private IEnumerator TransitionToScene(string sceneName, Color color)
    {
        if (enableDebug) Debug.Log($"[TransitionManager] 开始过渡到 {sceneName}");
        
        // 准备淡入参数
        needsFadeIn = true;
        fadeInColor = color;
        
        // 淡出
        yield return StartCoroutine(PlayFadeOut(color));
        
        if (enableDebug) Debug.Log("[TransitionManager] 淡出完成，加载场景");
        
        // 加载场景
        SceneManager.LoadScene(sceneName);
    }

    private IEnumerator PlayFadeOut(Color color)
    {
        transitionCanvas.SetActive(true);
        transitionImage.color = color;
        canvasGroup.alpha = 0f;
        
        float elapsed = 0f;
        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.Clamp01(elapsed / fadeOutDuration);
            yield return null;
        }
        canvasGroup.alpha = 1f;
    }

    private IEnumerator PlayFadeIn()
    {
        if (enableDebug) Debug.Log("[TransitionManager] 开始淡入");
        
        transitionCanvas.SetActive(true);
        transitionImage.color = fadeInColor;
        canvasGroup.alpha = 1f;
        
        // 等待场景稳定
        yield return new WaitForSecondsRealtime(waitAfterLoad);
        
        float elapsed = 0f;
        while (elapsed < fadeInDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            canvasGroup.alpha = 1f - Mathf.Clamp01(elapsed / fadeInDuration);
            yield return null;
        }
        
        canvasGroup.alpha = 0f;
        transitionCanvas.SetActive(false);
        
        if (enableDebug) Debug.Log("[TransitionManager] 淡入完成");
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        Instance = null;
        needsFadeIn = false;
        fadeInColor = Color.black;
    }
}

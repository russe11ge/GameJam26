using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

/// <summary>
/// 主菜单控制器
/// 雪人按钮 + 悬浮效果 + 黑色过渡动画（手动添加遮罩）
/// </summary>
public class MainMenuController : MonoBehaviour
{
    [Header("=== 雪人按钮 ===")]
    [Tooltip("雪人按钮（需要有 Button 组件）")]
    public Button snowmanButton;

    [Header("=== 目标场景 ===")]
    [Tooltip("进入游戏的目标场景名称")]
    public string targetSceneName;
    
    [Tooltip("目标生成点ID（可选）")]
    public string targetSpawnPointID;

    [Header("=== 悬浮效果 ===")]
    [Tooltip("悬浮时显示的效果图层")]
    public GameObject hoverEffectLayer;
    
    [Tooltip("悬浮效果淡入时长")]
    public float hoverFadeInDuration = 0.3f;
    
    [Tooltip("悬浮效果淡出时长")]
    public float hoverFadeOutDuration = 0.5f;
    
    [Tooltip("鼠标移出后效果消失的延迟")]
    public float hoverFadeOutDelay = 0.2f;

    [Header("=== 悬浮音效 ===")]
    [Tooltip("悬浮时播放的音效")]
    public AudioClip hoverSound;
    
    [Tooltip("每次悬浮只播放一次（移出后重置）")]
    public bool playSoundOncePerHover = true;

    [Header("=== 点击音效 ===")]
    [Tooltip("点击时播放的音效")]
    public AudioClip clickSound;

    [Header("=== 黑色遮罩（手动添加）===")]
    [Tooltip("黑色遮罩 GameObject（需要有 CanvasGroup）")]
    public GameObject blackOverlay;
    
    [Tooltip("进入主菜单时的淡入时长（黑色消失）")]
    public float fadeInDuration = 0.5f;
    
    [Tooltip("离开主菜单时的淡出时长（黑色出现）")]
    public float fadeOutDuration = 1f;
    
    [Tooltip("场景加载后等待时间")]
    public float waitAfterLoad = 0.2f;

    [Header("=== Debug ===")]
    public bool enableDebug = false;

    // 静态变量 - 跨场景通信
    private static bool needsFadeIn = true;
    private static float savedFadeInDuration = 0.5f;

    // 私有变量
    private CanvasGroup hoverEffectCanvasGroup;
    private CanvasGroup blackOverlayCanvasGroup;
    private AudioSource audioSource;
    private bool isHovering = false;
    private bool isStartingGame = false;
    private bool hasPlayedHoverSound = false;
    private Coroutine fadeCoroutine;

    private void Awake()
    {
        // 初始化 AudioSource
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }

        // 初始化悬浮效果图层
        if (hoverEffectLayer != null)
        {
            hoverEffectCanvasGroup = hoverEffectLayer.GetComponent<CanvasGroup>();
            if (hoverEffectCanvasGroup == null)
            {
                hoverEffectCanvasGroup = hoverEffectLayer.AddComponent<CanvasGroup>();
            }
            hoverEffectCanvasGroup.alpha = 0f;
            hoverEffectLayer.SetActive(true);
        }

        // 初始化黑色遮罩
        if (blackOverlay != null)
        {
            blackOverlayCanvasGroup = blackOverlay.GetComponent<CanvasGroup>();
            if (blackOverlayCanvasGroup == null)
            {
                blackOverlayCanvasGroup = blackOverlay.AddComponent<CanvasGroup>();
            }
            // 初始全黑
            blackOverlayCanvasGroup.alpha = 1f;
            // 不阻挡鼠标
            blackOverlayCanvasGroup.blocksRaycasts = false;
            blackOverlayCanvasGroup.interactable = false;
            blackOverlay.SetActive(true);
        }

        // 设置按钮事件
        SetupButtonEvents();
    }

    private void Start()
    {
        // 进入主菜单时播放淡入动画
        if (needsFadeIn)
        {
            needsFadeIn = false;
            float duration = savedFadeInDuration > 0 ? savedFadeInDuration : fadeInDuration;
            StartCoroutine(PlayFadeIn(duration));
        }
        else
        {
            // 不需要淡入，直接隐藏遮罩
            if (blackOverlayCanvasGroup != null)
            {
                blackOverlayCanvasGroup.alpha = 0f;
            }
        }
    }

    private void SetupButtonEvents()
    {
        if (snowmanButton == null)
        {
            if (enableDebug) Debug.LogWarning("[MainMenu] 未设置雪人按钮！");
            return;
        }

        snowmanButton.onClick.AddListener(OnSnowmanClick);

        EventTrigger trigger = snowmanButton.gameObject.GetComponent<EventTrigger>();
        if (trigger == null)
        {
            trigger = snowmanButton.gameObject.AddComponent<EventTrigger>();
        }

        // 鼠标进入
        EventTrigger.Entry enterEntry = new EventTrigger.Entry();
        enterEntry.eventID = EventTriggerType.PointerEnter;
        enterEntry.callback.AddListener((data) => OnSnowmanHoverEnter());
        trigger.triggers.Add(enterEntry);

        // 鼠标离开
        EventTrigger.Entry exitEntry = new EventTrigger.Entry();
        exitEntry.eventID = EventTriggerType.PointerExit;
        exitEntry.callback.AddListener((data) => OnSnowmanHoverExit());
        trigger.triggers.Add(exitEntry);

        if (enableDebug) Debug.Log("[MainMenu] 按钮事件设置完成");
    }

    /// <summary>
    /// 进入主菜单时的淡入动画（黑色消失）
    /// </summary>
    private IEnumerator PlayFadeIn(float duration)
    {
        if (blackOverlayCanvasGroup == null) yield break;
        
        if (enableDebug) Debug.Log("[MainMenu] 播放淡入动画");
        
        blackOverlayCanvasGroup.alpha = 1f;

        yield return new WaitForSecondsRealtime(waitAfterLoad);

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            blackOverlayCanvasGroup.alpha = 1f - Mathf.Clamp01(elapsed / duration);
            yield return null;
        }

        blackOverlayCanvasGroup.alpha = 0f;
        
        if (enableDebug) Debug.Log("[MainMenu] 淡入完成");
    }

    /// <summary>
    /// 离开主菜单时的淡出动画（黑色出现）
    /// </summary>
    private IEnumerator PlayFadeOutAndLoadScene()
    {
        if (blackOverlayCanvasGroup == null)
        {
            SceneManager.LoadScene(targetSceneName);
            yield break;
        }
        
        if (enableDebug) Debug.Log("[MainMenu] 播放淡出动画");
        
        blackOverlayCanvasGroup.alpha = 0f;

        float elapsed = 0f;
        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            blackOverlayCanvasGroup.alpha = Mathf.Clamp01(elapsed / fadeOutDuration);
            yield return null;
        }

        blackOverlayCanvasGroup.alpha = 1f;
        
        if (enableDebug) Debug.Log("[MainMenu] 淡出完成，加载场景: " + targetSceneName);

        SceneManager.LoadScene(targetSceneName);
    }

    /// <summary>
    /// 静态方法 - 设置返回主菜单时需要淡入
    /// </summary>
    public static void SetNeedsFadeIn(float duration = 0.5f)
    {
        needsFadeIn = true;
        savedFadeInDuration = duration;
    }

    // ==================== 雪人交互 ====================

    private void OnSnowmanHoverEnter()
    {
        if (isStartingGame) return;
        
        if (enableDebug) Debug.Log("[MainMenu] 鼠标悬浮在雪人上");
        
        isHovering = true;

        if (hoverSound != null && audioSource != null)
        {
            if (!playSoundOncePerHover || !hasPlayedHoverSound)
            {
                audioSource.PlayOneShot(hoverSound);
                hasPlayedHoverSound = true;
            }
        }

        ShowHoverEffect();
    }

    private void OnSnowmanHoverExit()
    {
        if (isStartingGame) return;
        
        if (enableDebug) Debug.Log("[MainMenu] 鼠标离开雪人");
        
        isHovering = false;
        hasPlayedHoverSound = false;

        HideHoverEffect();
    }

    private void OnSnowmanClick()
    {
        if (isStartingGame) return;
        
        if (string.IsNullOrEmpty(targetSceneName))
        {
            Debug.LogWarning("[MainMenu] 未设置目标场景！");
            return;
        }

        if (enableDebug) Debug.Log("[MainMenu] 点击雪人，开始游戏");
        
        isStartingGame = true;

        if (clickSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(clickSound);
        }

        if (GameManager.Instance != null && !string.IsNullOrEmpty(targetSpawnPointID))
        {
            GameManager.Instance.targetSpawnID = targetSpawnPointID;
        }

        StartCoroutine(PlayFadeOutAndLoadScene());
    }

    // ==================== 悬浮效果 ====================

    private void ShowHoverEffect()
    {
        if (hoverEffectCanvasGroup == null) return;

        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
        }

        fadeCoroutine = StartCoroutine(FadeEffect(hoverEffectCanvasGroup.alpha, 1f, hoverFadeInDuration));
    }

    private void HideHoverEffect()
    {
        if (hoverEffectCanvasGroup == null) return;

        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
        }

        fadeCoroutine = StartCoroutine(DelayedFadeOut());
    }

    private IEnumerator DelayedFadeOut()
    {
        yield return new WaitForSeconds(hoverFadeOutDelay);

        if (isHovering) yield break;

        yield return StartCoroutine(FadeEffect(hoverEffectCanvasGroup.alpha, 0f, hoverFadeOutDuration));
    }

    private IEnumerator FadeEffect(float from, float to, float duration)
    {
        if (hoverEffectCanvasGroup == null) yield break;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            hoverEffectCanvasGroup.alpha = Mathf.Lerp(from, to, elapsed / duration);
            yield return null;
        }
        
        hoverEffectCanvasGroup.alpha = to;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        needsFadeIn = true;
        savedFadeInDuration = 0.5f;
    }
}

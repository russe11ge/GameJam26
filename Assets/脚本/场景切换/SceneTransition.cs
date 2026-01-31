using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// 场景切换触发器
/// 当玩家进入触发区域时，切换到目标场景
/// 使用方法：添加到带有Collider2D（勾选IsTrigger）的物体上
/// </summary>
public class SceneTransition : MonoBehaviour
{
    [Header("=== 目标设置 ===")]
    [Tooltip("要切换到的场景名称")]
    public string targetSceneName;
    
    [Tooltip("目标场景中的生成点ID")]
    public string targetSpawnPointID;

    [Header("=== 触发设置 ===")]
    [Tooltip("可触发的标签")]
    public string triggerTag = "Player";
    
    [Tooltip("是否需要按键确认")]
    public bool requireKeyPress = false;
    
    [Tooltip("确认按键")]
    public KeyCode confirmKey = KeyCode.E;

    [Header("=== 过渡动画设置 ===")]
    [Tooltip("启用过渡动画")]
    public bool enableTransition = true;
    
    [Tooltip("淡入淡出时间")]
    public float fadeDuration = 0.5f;
    
    [Tooltip("遮罩颜色")]
    public Color fadeColor = Color.black;

    [Header("=== 可选功能 ===")]
    [Tooltip("切换前播放的音效")]
    public AudioClip transitionSound;

    private bool isTransitioning = false;
    private bool playerInTrigger = false;
    private AudioSource audioSource;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null && transitionSound != null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }
    }

    private void Update()
    {
        // 如果需要按键确认且玩家在触发区域内
        if (requireKeyPress && playerInTrigger && !isTransitioning)
        {
            if (Input.GetKeyDown(confirmKey))
            {
                StartTransition();
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag(triggerTag) || isTransitioning) return;

        playerInTrigger = true;

        // 如果不需要按键确认，直接切换
        if (!requireKeyPress)
        {
            StartTransition();
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag(triggerTag))
        {
            playerInTrigger = false;
        }
    }

    /// <summary>
    /// 开始场景切换
    /// </summary>
    private void StartTransition()
    {
        if (isTransitioning) return;
        
        if (string.IsNullOrEmpty(targetSceneName))
        {
            Debug.LogError("[SceneTransition] 目标场景名称未设置！");
            return;
        }

        isTransitioning = true;
        StartCoroutine(TransitionRoutine());
    }

    /// <summary>
    /// 场景切换协程
    /// </summary>
    private IEnumerator TransitionRoutine()
    {
        // 播放音效
        if (transitionSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(transitionSound);
        }

        // 冻结游戏
        Time.timeScale = 0f;

        // 创建淡出效果
        GameObject fadeCanvas = null;
        CanvasGroup canvasGroup = null;

        if (enableTransition)
        {
            fadeCanvas = CreateFadeCanvas();
            canvasGroup = fadeCanvas.GetComponentInChildren<CanvasGroup>();

            // 淡出动画
            float timer = 0f;
            while (timer < fadeDuration)
            {
                timer += Time.unscaledDeltaTime;
                canvasGroup.alpha = Mathf.Lerp(0f, 1f, timer / fadeDuration);
                yield return null;
            }
            canvasGroup.alpha = 1f;
        }

        // 设置目标生成点
        if (GameManager.Instance != null)
        {
            GameManager.Instance.targetSpawnID = targetSpawnPointID;
        }

        // 恢复时间
        Time.timeScale = 1f;

        // 加载场景
        SceneManager.LoadScene(targetSceneName);
    }

    /// <summary>
    /// 创建淡出遮罩Canvas
    /// </summary>
    private GameObject CreateFadeCanvas()
    {
        // 创建Canvas
        GameObject canvasObj = new GameObject("TransitionCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 9999;
        DontDestroyOnLoad(canvasObj);

        // 创建遮罩Panel
        GameObject panel = new GameObject("FadePanel");
        panel.transform.SetParent(canvasObj.transform, false);

        Image img = panel.AddComponent<Image>();
        img.color = fadeColor;

        // 铺满屏幕
        RectTransform rt = panel.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        // 添加CanvasGroup控制透明度
        CanvasGroup cg = panel.AddComponent<CanvasGroup>();
        cg.alpha = 0f;

        // 添加淡入脚本（新场景加载后自动淡入）
        TransitionFadeIn fadeScript = canvasObj.AddComponent<TransitionFadeIn>();
        fadeScript.fadeDuration = fadeDuration;

        return canvasObj;
    }

    /// <summary>
    /// 手动触发场景切换
    /// </summary>
    public void TriggerTransition()
    {
        StartTransition();
    }

    #region Editor
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.magenta;
        
        // 绘制触发区域
        BoxCollider2D box = GetComponent<BoxCollider2D>();
        if (box != null)
        {
            Gizmos.DrawWireCube(transform.position + (Vector3)box.offset, box.size);
        }
        else
        {
            Gizmos.DrawWireSphere(transform.position, 0.5f);
        }

        // 绘制箭头指向目标
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(transform.position, transform.position + Vector3.right);
        
        #if UNITY_EDITOR
        string label = $"→ {targetSceneName}\n   @ {targetSpawnPointID}";
        UnityEditor.Handles.Label(transform.position + Vector3.up * 0.5f, label);
        #endif
    }
    #endregion
}

/// <summary>
/// 辅助脚本：场景加载后淡入并销毁
/// </summary>
public class TransitionFadeIn : MonoBehaviour
{
    public float fadeDuration = 0.5f;

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
        StartCoroutine(FadeInAndDestroy());
    }

    private IEnumerator FadeInAndDestroy()
    {
        // 等待一帧确保场景初始化完成
        yield return null;

        CanvasGroup cg = GetComponentInChildren<CanvasGroup>();
        if (cg != null)
        {
            float timer = 0f;
            while (timer < fadeDuration)
            {
                timer += Time.unscaledDeltaTime;
                cg.alpha = Mathf.Lerp(1f, 0f, timer / fadeDuration);
                yield return null;
            }
            cg.alpha = 0f;
        }

        // 销毁Canvas
        Destroy(gameObject);
    }
}

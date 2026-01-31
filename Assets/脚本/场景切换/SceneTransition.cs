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

    private IEnumerator TransitionRoutine()
    {
        // 播放音效
        if (transitionSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(transitionSound);
        }

        // 设置目标生成点
        if (GameManager.Instance != null)
        {
            GameManager.Instance.targetSpawnID = targetSpawnPointID;
        }

        if (enableTransition)
        {
            // 创建淡出Canvas（初始透明）
            GameObject fadeCanvas = CreateFadeCanvas();
            CanvasGroup canvasGroup = fadeCanvas.GetComponentInChildren<CanvasGroup>();
            
            // 确保初始透明
            canvasGroup.alpha = 0f;

            // 淡出动画（屏幕从透明变黑）
            float timer = 0f;
            while (timer < fadeDuration)
            {
                timer += Time.unscaledDeltaTime;
                canvasGroup.alpha = Mathf.Lerp(0f, 1f, timer / fadeDuration);
                yield return null;
            }
            canvasGroup.alpha = 1f;

            // 标记可以开始淡入了（在新场景加载后）
            TransitionFadeIn fadeScript = fadeCanvas.GetComponent<TransitionFadeIn>();
            if (fadeScript != null)
            {
                fadeScript.readyToFadeIn = true;
            }

            // 加载场景
            SceneManager.LoadScene(targetSceneName);
        }
        else
        {
            // 无过渡动画，直接加载
            SceneManager.LoadScene(targetSceneName);
        }
    }

    private GameObject CreateFadeCanvas()
    {
        // 创建Canvas
        GameObject canvasObj = new GameObject("TransitionCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 9999;
        
        // 添加CanvasScaler确保正确缩放
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        
        // 添加GraphicRaycaster
        canvasObj.AddComponent<GraphicRaycaster>();
        
        DontDestroyOnLoad(canvasObj);

        // 创建遮罩Panel
        GameObject panel = new GameObject("FadePanel");
        panel.transform.SetParent(canvasObj.transform, false);

        Image img = panel.AddComponent<Image>();
        img.color = fadeColor;
        img.raycastTarget = false;

        // 铺满屏幕
        RectTransform rt = panel.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        // 添加CanvasGroup控制透明度（初始透明）
        CanvasGroup cg = panel.AddComponent<CanvasGroup>();
        cg.alpha = 0f;
        cg.blocksRaycasts = false;

        // 添加淡入脚本（但不立即执行）
        TransitionFadeIn fadeScript = canvasObj.AddComponent<TransitionFadeIn>();
        fadeScript.fadeDuration = fadeDuration;
        fadeScript.readyToFadeIn = false; // 等待淡出完成后才能淡入

        return canvasObj;
    }

    public void TriggerTransition()
    {
        StartTransition();
    }

    #region Editor
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.magenta;
        
        BoxCollider2D box = GetComponent<BoxCollider2D>();
        if (box != null)
        {
            Gizmos.DrawWireCube(transform.position + (Vector3)box.offset, box.size);
        }
        else
        {
            Gizmos.DrawWireSphere(transform.position, 0.5f);
        }

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
    public bool readyToFadeIn = false; // 控制是否可以开始淡入
    
    private bool hasFadedIn = false;
    private string originalSceneName;

    private void Awake()
    {
        // 记录当前场景名，用于判断是否已切换场景
        originalSceneName = SceneManager.GetActiveScene().name;
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
        // 只有当场景真正切换后才淡入
        if (scene.name != originalSceneName && !hasFadedIn)
        {
            StartCoroutine(FadeInAndDestroy());
        }
    }

    private IEnumerator FadeInAndDestroy()
    {
        hasFadedIn = true;
        
        // 等待几帧确保场景完全初始化
        yield return null;
        yield return null;

        CanvasGroup cg = GetComponentInChildren<CanvasGroup>();
        
        if (cg == null)
        {
            Debug.LogWarning("[TransitionFadeIn] 未找到CanvasGroup，直接销毁");
            Destroy(gameObject);
            yield break;
        }

        // 确保遮罩是不透明的（黑色）
        cg.alpha = 1f;

        // 淡入动画（从黑色变透明）
        float timer = 0f;
        while (timer < fadeDuration)
        {
            timer += Time.unscaledDeltaTime;
            cg.alpha = Mathf.Lerp(1f, 0f, timer / fadeDuration);
            yield return null;
        }
        
        cg.alpha = 0f;

        // 销毁Canvas
        Debug.Log("[TransitionFadeIn] 过渡完成，销毁遮罩");
        Destroy(gameObject);
    }

    // 安全措施：如果10秒后还没销毁，强制销毁
    private void Start()
    {
        StartCoroutine(SafetyDestroy());
    }

    private IEnumerator SafetyDestroy()
    {
        yield return new WaitForSecondsRealtime(10f);
        
        if (gameObject != null)
        {
            Debug.LogWarning("[TransitionFadeIn] 安全销毁：遮罩超时未消失");
            Destroy(gameObject);
        }
    }
}

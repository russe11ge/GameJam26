using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// 场景切换触发器
/// </summary>
public class SceneTransition : MonoBehaviour
{
    [Header("=== 目标设置 ===")]
    public string targetSceneName;
    public string targetSpawnPointID;

    [Header("=== 触发设置 ===")]
    public string triggerTag = "Player";
    
    [Tooltip("是否需要按键确认才能进入")]
    public bool requireKeyPress = false;
    
    [Tooltip("确认按键")]
    public KeyCode confirmKey = KeyCode.E;

    [Header("=== 过渡动画 ===")]
    public bool enableTransition = true;
    public float fadeDuration = 0.5f;
    public float waitBeforeFadeIn = 0.3f;
    public Color fadeColor = Color.black;

    [Header("=== 音效 ===")]
    [Tooltip("切换场景时播放的音效")]
    public AudioClip transitionSound;

    private bool isTransitioning = false;
    private bool playerInTrigger = false;
    private AudioSource audioSource;

    private void Awake()
    {
        // 获取或添加 AudioSource
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null && transitionSound != null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }
    }

    private void Update()
    {
        // 需要按键确认时，检测按键
        if (requireKeyPress && playerInTrigger && !isTransitioning)
        {
            if (Input.GetKeyDown(confirmKey))
            {
                Debug.Log($"[SceneTransition] 按下 {confirmKey}，触发切换");
                StartTransition();
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag(triggerTag) || isTransitioning) return;

        playerInTrigger = true;

        // 不需要按键时，直接触发
        if (!requireKeyPress)
        {
            Debug.Log($"[SceneTransition] 自动触发! 目标: {targetSceneName} @ {targetSpawnPointID}");
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
        if (string.IsNullOrEmpty(targetSceneName)) return;
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
        GameManager.Instance.targetSpawnID = targetSpawnPointID;

        if (enableTransition)
        {
            // 创建遮罩并淡出
            GameObject fadeCanvas = CreateFadeCanvas();
            CanvasGroup cg = fadeCanvas.GetComponentInChildren<CanvasGroup>();

            float timer = 0f;
            while (timer < fadeDuration)
            {
                timer += Time.unscaledDeltaTime;
                cg.alpha = Mathf.Lerp(0f, 1f, timer / fadeDuration);
                yield return null;
            }
            cg.alpha = 1f;
        }

        SceneManager.LoadScene(targetSceneName);
    }

    private GameObject CreateFadeCanvas()
    {
        GameObject canvasObj = new GameObject("TransitionCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 9999;
        DontDestroyOnLoad(canvasObj);

        GameObject panel = new GameObject("FadePanel");
        panel.transform.SetParent(canvasObj.transform, false);

        Image img = panel.AddComponent<Image>();
        img.color = fadeColor;
        img.raycastTarget = false;

        RectTransform rt = panel.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        CanvasGroup cg = panel.AddComponent<CanvasGroup>();
        cg.alpha = 0f;

        TransitionFadeIn fadeScript = canvasObj.AddComponent<TransitionFadeIn>();
        fadeScript.fadeDuration = fadeDuration;
        fadeScript.waitTime = waitBeforeFadeIn;

        return canvasObj;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = requireKeyPress ? Color.yellow : Color.magenta;
        BoxCollider2D box = GetComponent<BoxCollider2D>();
        if (box != null)
        {
            Gizmos.DrawWireCube(transform.position + (Vector3)box.offset, box.size);
        }
    }
}

/// <summary>
/// 场景加载后淡入并销毁
/// </summary>
public class TransitionFadeIn : MonoBehaviour
{
    public float fadeDuration = 0.5f;
    public float waitTime = 0.3f;
    
    private bool hasFadedIn = false;
    private string originalScene;

    private void Awake()
    {
        originalScene = SceneManager.GetActiveScene().name;
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
        if (scene.name != originalScene && !hasFadedIn)
        {
            StartCoroutine(FadeIn());
        }
    }

    private IEnumerator FadeIn()
    {
        hasFadedIn = true;

        CanvasGroup cg = GetComponentInChildren<CanvasGroup>();
        if (cg == null)
        {
            Destroy(gameObject);
            yield break;
        }

        cg.alpha = 1f;

        // 等待相机跟随完成
        yield return new WaitForSecondsRealtime(waitTime);

        // 淡入
        float timer = 0f;
        while (timer < fadeDuration)
        {
            timer += Time.unscaledDeltaTime;
            cg.alpha = Mathf.Lerp(1f, 0f, timer / fadeDuration);
            yield return null;
        }

        Destroy(gameObject);
    }

    private void Start()
    {
        // 安全销毁
        Destroy(gameObject, 10f);
    }
}

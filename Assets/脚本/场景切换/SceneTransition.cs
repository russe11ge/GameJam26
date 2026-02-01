using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// 场景切换触发器
/// 支持面具要求、按键确认、过渡动画
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

    [Header("=== 面具要求（可选）===")]
    [Tooltip("是否需要特定面具才能触发")]
    public bool requireMask = false;
    
    [Tooltip("需要的面具ID")]
    public string requiredMaskId;
    
    [Tooltip("没有面具时显示的提示物体（可选）")]
    public GameObject noMaskHint;
    
    [Tooltip("提示显示时长")]
    public float hintDuration = 2f;

    [Header("=== 过渡动画 ===")]
    [Tooltip("过渡颜色")]
    public Color fadeColor = Color.black;
    
    [Tooltip("淡出时长")]
    public float fadeOutDuration = 0.5f;
    
    [Tooltip("淡入时长")]
    public float fadeInDuration = 0.5f;
    
    [Tooltip("场景加载后等待时间")]
    public float waitAfterLoad = 0.3f;

    [Header("=== 音效 ===")]
    [Tooltip("切换场景时播放的音效")]
    public AudioClip transitionSound;
    
    [Tooltip("没有面具时播放的音效")]
    public AudioClip noMaskSound;

    private bool isTransitioning = false;
    private bool playerInTrigger = false;
    private AudioSource audioSource;
    private Coroutine hintCoroutine;

    // 场景加载后的冷却时间，防止刚进入场景就触发
    private static float sceneLoadTime = 0f;
    private const float SCENE_LOAD_COOLDOWN = 1f; // 1秒冷却

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null && (transitionSound != null || noMaskSound != null))
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }

        // 初始隐藏提示
        if (noMaskHint != null)
        {
            noMaskHint.SetActive(false);
        }
    }

    private void Start()
    {
        // 记录场景加载时间
        sceneLoadTime = Time.time;
    }

    private bool IsInCooldown()
    {
        return Time.time - sceneLoadTime < SCENE_LOAD_COOLDOWN;
    }

    private void Update()
    {
        if (requireKeyPress && playerInTrigger && !isTransitioning && !IsInCooldown())
        {
            if (Input.GetKeyDown(confirmKey))
            {
                TryTransition();
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag(triggerTag) || isTransitioning) return;

        playerInTrigger = true;

        // 场景加载后的冷却检查
        if (IsInCooldown())
        {
            Debug.Log($"[SceneTransition] 冷却中，忽略触发 (剩余 {SCENE_LOAD_COOLDOWN - (Time.time - sceneLoadTime):F1}s)");
            return;
        }

        if (!requireKeyPress)
        {
            TryTransition();
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag(triggerTag))
        {
            playerInTrigger = false;
            
            // 离开时隐藏提示
            if (noMaskHint != null && noMaskHint.activeSelf)
            {
                noMaskHint.SetActive(false);
            }
        }
    }

    /// <summary>
    /// 尝试触发场景切换（检查面具要求）
    /// </summary>
    private void TryTransition()
    {
        // 检查面具要求
        if (requireMask && !string.IsNullOrEmpty(requiredMaskId))
        {
            if (!CheckMaskRequirement())
            {
                // 没有所需面具
                Debug.Log($"[SceneTransition] 需要面具 {requiredMaskId}，但玩家没有");
                ShowNoMaskHint();
                return;
            }
        }

        // 满足条件，开始切换
        Debug.Log($"[SceneTransition] 触发切换! 目标: {targetSceneName} @ {targetSpawnPointID}");
        StartTransition();
    }

    /// <summary>
    /// 检查玩家是否拥有所需面具
    /// </summary>
    private bool CheckMaskRequirement()
    {
        if (PlayerMaskManager.Instance == null)
        {
            Debug.LogWarning("[SceneTransition] PlayerMaskManager 不存在");
            return false;
        }

        // 检查当前佩戴的面具是否匹配
        return PlayerMaskManager.Instance.currentMaskId == requiredMaskId;
    }

    /// <summary>
    /// 显示没有面具的提示
    /// </summary>
    private void ShowNoMaskHint()
    {
        // 播放提示音效
        if (noMaskSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(noMaskSound);
        }

        // 显示提示物体
        if (noMaskHint != null)
        {
            if (hintCoroutine != null)
            {
                StopCoroutine(hintCoroutine);
            }
            hintCoroutine = StartCoroutine(ShowHintCoroutine());
        }
    }

    private IEnumerator ShowHintCoroutine()
    {
        noMaskHint.SetActive(true);
        yield return new WaitForSeconds(hintDuration);
        noMaskHint.SetActive(false);
    }

    private void StartTransition()
    {
        if (string.IsNullOrEmpty(targetSceneName)) return;
        if (isTransitioning) return;
        
        isTransitioning = true;
        
        // 隐藏提示
        if (noMaskHint != null)
        {
            noMaskHint.SetActive(false);
        }
        
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

        // 使用 TransitionManager 进行过渡
        if (TransitionManager.Instance != null)
        {
            TransitionManager.Instance.LoadScene(targetSceneName, fadeColor);
        }
        else
        {
            Debug.LogWarning("[SceneTransition] TransitionManager 不存在，直接加载场景");
            SceneManager.LoadScene(targetSceneName);
        }
    }

    private void OnDrawGizmos()
    {
        // 根据设置显示不同颜色
        if (requireMask)
        {
            Gizmos.color = Color.cyan; // 需要面具
        }
        else if (requireKeyPress)
        {
            Gizmos.color = Color.yellow; // 需要按键
        }
        else
        {
            Gizmos.color = Color.magenta; // 自动触发
        }
        
        BoxCollider2D box = GetComponent<BoxCollider2D>();
        if (box != null)
        {
            Gizmos.DrawWireCube(transform.position + (Vector3)box.offset, box.size);
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        sceneLoadTime = 0f;
    }
}

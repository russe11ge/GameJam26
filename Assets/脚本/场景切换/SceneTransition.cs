using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// 场景切换触发器
/// 使用 TransitionManager 进行统一的过渡动画
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
                Debug.Log($"[SceneTransition] 按下 {confirmKey}，触发切换");
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
        if (isTransitioning) return;
        
        isTransitioning = true;
        
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
            TransitionManager.Instance.LoadScene(
                targetSceneName, 
                fadeColor, 
                fadeOutDuration, 
                fadeInDuration, 
                waitAfterLoad
            );
        }
        else
        {
            // 如果没有 TransitionManager，直接加载
            Debug.LogWarning("[SceneTransition] TransitionManager 不存在，直接加载场景");
            SceneManager.LoadScene(targetSceneName);
        }
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

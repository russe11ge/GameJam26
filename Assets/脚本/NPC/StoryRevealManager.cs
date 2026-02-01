using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 故事揭示管理器 - 单例
/// 控制对话后的特殊画面展示
/// </summary>
public class StoryRevealManager : MonoBehaviour
{
    public static StoryRevealManager Instance;

    [Header("=== 输入设置 ===")]
    public KeyCode advanceKey = KeyCode.Space;
    
    [Tooltip("按空格时的加速倍率")]
    public float speedUpMultiplier = 3f;

    [Header("=== 玩家控制 ===")]
    [Tooltip("揭示过程中禁用玩家移动")]
    public bool freezePlayerDuringReveal = true;

    [Header("=== Debug ===")]
    public bool enableDebug = true;

    private bool isRevealing = false;
    private GameObject playerObject;
    private PlayerMove2D playerMoveScript;
    private bool isSpeedUp = false;
    private bool allElementsShown = false;
    
    // 当前显示的内容
    private Image currentBackground;
    private CanvasGroup backgroundCanvasGroup;
    private List<CanvasGroup> currentElements = new List<CanvasGroup>();
    
    // 当前设置
    private float currentBgFadeDuration;
    private float currentElementFadeDuration;
    private float currentDelayBetween;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Update()
    {
        if (!isRevealing) return;

        // 按住空格加速
        isSpeedUp = Input.GetKey(advanceKey);

        // 所有元素显示后，按空格关闭
        if (allElementsShown && Input.GetKeyDown(advanceKey))
        {
            if (enableDebug) Debug.Log("[StoryReveal] 玩家按下空格，关闭画面");
            StartCoroutine(CloseReveal());
        }
    }

    /// <summary>
    /// 开始故事揭示
    /// </summary>
    public void StartReveal(
        Image background,
        float bgFadeDuration,
        List<GameObject> elements,
        float elementFadeDuration,
        float delayBetween)
    {
        if (isRevealing)
        {
            if (enableDebug) Debug.Log("[StoryReveal] 已在揭示中，跳过");
            return;
        }

        if (elements == null || elements.Count == 0)
        {
            if (enableDebug) Debug.LogWarning("[StoryReveal] 没有元素可显示");
            return;
        }

        if (enableDebug) Debug.Log($"[StoryReveal] 开始揭示，共 {elements.Count} 个元素");

        // 保存设置
        currentBackground = background;
        currentBgFadeDuration = bgFadeDuration;
        currentElementFadeDuration = elementFadeDuration;
        currentDelayBetween = delayBetween;

        // 准备背景
        if (currentBackground != null)
        {
            backgroundCanvasGroup = currentBackground.GetComponent<CanvasGroup>();
            if (backgroundCanvasGroup == null)
            {
                backgroundCanvasGroup = currentBackground.gameObject.AddComponent<CanvasGroup>();
            }
            backgroundCanvasGroup.alpha = 0f;
            currentBackground.gameObject.SetActive(true);
        }

        // 准备元素
        currentElements.Clear();
        foreach (var obj in elements)
        {
            if (obj == null) continue;

            CanvasGroup cg = obj.GetComponent<CanvasGroup>();
            if (cg == null)
            {
                cg = obj.AddComponent<CanvasGroup>();
            }
            cg.alpha = 0f;
            obj.SetActive(true);
            currentElements.Add(cg);
        }

        isRevealing = true;
        allElementsShown = false;
        isSpeedUp = false;

        // 冻结玩家
        if (freezePlayerDuringReveal)
        {
            FreezePlayer(true);
        }

        StartCoroutine(RevealSequence());
    }

    private void FreezePlayer(bool freeze)
    {
        if (playerObject == null)
        {
            playerObject = GameObject.FindGameObjectWithTag("Player");
        }

        if (playerObject != null)
        {
            if (playerMoveScript == null)
            {
                playerMoveScript = playerObject.GetComponent<PlayerMove2D>();
            }

            if (playerMoveScript != null)
            {
                playerMoveScript.enabled = !freeze;
                if (enableDebug) Debug.Log($"[StoryReveal] 玩家移动: {(freeze ? "禁用" : "启用")}");
            }

            if (freeze)
            {
                var rb = playerObject.GetComponent<Rigidbody2D>();
                if (rb != null)
                {
                    rb.linearVelocity = Vector2.zero;
                }
            }
        }
    }

    private IEnumerator RevealSequence()
    {
        // 1. 显示背景
        if (backgroundCanvasGroup != null)
        {
            yield return StartCoroutine(FadeCanvasGroup(backgroundCanvasGroup, 0f, 1f, currentBgFadeDuration));
        }

        // 2. 依次显示元素
        for (int i = 0; i < currentElements.Count; i++)
        {
            if (enableDebug) Debug.Log($"[StoryReveal] 显示元素 {i + 1}/{currentElements.Count}");

            // 淡入元素
            yield return StartCoroutine(FadeCanvasGroup(currentElements[i], 0f, 1f, currentElementFadeDuration));

            // 等待间隔（最后一个不等待）
            if (i < currentElements.Count - 1)
            {
                yield return StartCoroutine(WaitWithSpeedUp(currentDelayBetween));
            }
        }

        // 3. 所有元素显示完毕
        allElementsShown = true;
        if (enableDebug) Debug.Log("[StoryReveal] 所有元素已显示，按空格关闭");
    }

    private IEnumerator FadeCanvasGroup(CanvasGroup cg, float from, float to, float duration)
    {
        if (cg == null) yield break;

        float elapsed = 0f;
        cg.alpha = from;

        while (elapsed < duration)
        {
            float speed = isSpeedUp ? speedUpMultiplier : 1f;
            elapsed += Time.unscaledDeltaTime * speed;
            cg.alpha = Mathf.Lerp(from, to, elapsed / duration);
            yield return null;
        }

        cg.alpha = to;
    }

    private IEnumerator WaitWithSpeedUp(float duration)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float speed = isSpeedUp ? speedUpMultiplier : 1f;
            elapsed += Time.unscaledDeltaTime * speed;
            yield return null;
        }
    }

    private IEnumerator CloseReveal()
    {
        float fadeDuration = 0.3f;

        // 同时淡出所有元素
        foreach (var cg in currentElements)
        {
            if (cg != null)
            {
                StartCoroutine(FadeCanvasGroup(cg, 1f, 0f, fadeDuration));
            }
        }

        // 淡出背景
        if (backgroundCanvasGroup != null)
        {
            yield return StartCoroutine(FadeCanvasGroup(backgroundCanvasGroup, 1f, 0f, fadeDuration));
        }

        // 隐藏所有
        foreach (var cg in currentElements)
        {
            if (cg != null)
            {
                cg.gameObject.SetActive(false);
            }
        }

        if (currentBackground != null)
        {
            currentBackground.gameObject.SetActive(false);
        }

        currentElements.Clear();
        currentBackground = null;
        backgroundCanvasGroup = null;
        isRevealing = false;
        allElementsShown = false;

        // 解冻玩家
        if (freezePlayerDuringReveal)
        {
            FreezePlayer(false);
        }

        if (enableDebug) Debug.Log("[StoryReveal] 揭示结束");
    }

    /// <summary>
    /// 是否正在揭示中
    /// </summary>
    public bool IsRevealing() => isRevealing;
}

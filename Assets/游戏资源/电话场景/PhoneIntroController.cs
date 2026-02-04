using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class PhoneIntroController : MonoBehaviour
{
    [Header("Splash (Image before ringtone)")]
    public GameObject splashImageObject;   // 把SplashImage拖进来
    public float splashDuration = 2.0f;    // 显示2秒

    [Header("Audio - Ringtone")]
    public AudioSource ringtoneSource;
    public float ringtoneDuration = 3.0f;
    public bool stopRingtoneWhenTextStarts = true;

    [Header("Audio - Typewriter Loop (Long Audio)")]
    public AudioSource typeLoopSource;
    public bool typeSoundPlaysDuringTyping = true;

    [Header("UI")]
    public TextMeshProUGUI introText;
    [TextArea(2, 6)]
    public List<string> lines = new List<string>();

    [Header("Typewriter")]
    public float charDelay = 0.03f;
    public float lineGapDelay = 0.6f;
    public bool allowSkipWithSpace = true;

    [Header("After Intro (Optional)")]
    public bool autoLoadNextScene = false;
    public string nextSceneName = "";
    public float endDelay = 1.0f;

    [Header("=== 过渡动画设置 ===")]
    [Tooltip("启用黑色过渡动画")]
    public bool useFadeTransition = true;
    
    [Tooltip("过渡颜色")]
    public Color fadeColor = Color.black;
    
    [Tooltip("淡出时长（画面变黑）")]
    public float fadeOutDuration = 0.8f;
    
    [Tooltip("淡出前的额外延迟")]
    public float delayBeforeFade = 0.5f;

    private bool skipping = false;

    void Start()
    {
        if (introText != null)
        {
            introText.text = "";
            introText.maxVisibleCharacters = int.MaxValue;
        }

        if (typeLoopSource != null) typeLoopSource.Stop();

        // 确保Splash一开始是显示的（如果你想脚本控制）
        if (splashImageObject != null) splashImageObject.SetActive(true);

        StartCoroutine(RunIntro());
    }

    void Update()
    {
        if (allowSkipWithSpace && Input.GetKeyDown(KeyCode.Space))
        {
            skipping = true;

            // 跳过时立刻关掉打字机音效（更干净）
            if (typeLoopSource != null && typeLoopSource.isPlaying)
                typeLoopSource.Stop();

            // 跳过时也可以关铃声（可选）
            if (ringtoneSource != null && ringtoneSource.isPlaying)
                ringtoneSource.Stop();

            // 跳过时也把图片关掉（可选）
            if (splashImageObject != null && splashImageObject.activeSelf)
                splashImageObject.SetActive(false);
        }
    }

    private IEnumerator RunIntro()
    {
        // 0) 先显示图片2秒
        if (!skipping && splashImageObject != null)
            yield return new WaitForSeconds(splashDuration);

        if (splashImageObject != null)
            splashImageObject.SetActive(false);

        if (skipping) yield return SkipShowAllAndMaybeLoad();

        // 1) 电话铃声
        if (ringtoneSource != null) ringtoneSource.Play();

        if (!skipping)
            yield return new WaitForSeconds(ringtoneDuration);

        if (stopRingtoneWhenTextStarts && ringtoneSource != null)
            ringtoneSource.Stop();

        if (skipping) yield return SkipShowAllAndMaybeLoad();

        if (introText == null) yield break;

        // 2) 打字机长音频：整段打字期间Loop
        if (typeSoundPlaysDuringTyping && typeLoopSource != null && !typeLoopSource.isPlaying)
            typeLoopSource.Play();

        introText.text = "";
        introText.maxVisibleCharacters = int.MaxValue;

        for (int i = 0; i < lines.Count; i++)
        {
            if (skipping)
            {
                introText.text = string.Join("\n", lines);
                introText.maxVisibleCharacters = int.MaxValue;
                break;
            }

            if (i > 0) introText.text += "\n";

            string line = lines[i];

            string baseText = introText.text;
            string fullText = baseText + line;
            introText.text = fullText;

            introText.ForceMeshUpdate();

            int baseVisible = CountVisibleCharacters(baseText);
            int totalVisible = introText.textInfo.characterCount;

            introText.maxVisibleCharacters = baseVisible;

            for (int v = baseVisible; v <= totalVisible; v++)
            {
                if (skipping)
                {
                    introText.maxVisibleCharacters = int.MaxValue;
                    break;
                }

                introText.maxVisibleCharacters = v;
                yield return new WaitForSeconds(charDelay);
            }

            yield return new WaitForSeconds(lineGapDelay);
        }

        // 3) 停止打字机长音频
        if (typeLoopSource != null && typeLoopSource.isPlaying)
            typeLoopSource.Stop();

        // 4) 结束后跳转（可选）
        yield return new WaitForSeconds(endDelay);

        if (autoLoadNextScene && !string.IsNullOrEmpty(nextSceneName))
        {
            yield return StartCoroutine(LoadSceneWithTransition());
        }
    }

    private IEnumerator SkipShowAllAndMaybeLoad()
    {
        // 跳过时：直接显示全部文字
        if (introText != null && lines != null && lines.Count > 0)
        {
            introText.text = string.Join("\n", lines);
            introText.maxVisibleCharacters = int.MaxValue;
        }

        // 直接按 endDelay 后跳转（如果你开了自动跳转）
        yield return new WaitForSeconds(endDelay);

        if (autoLoadNextScene && !string.IsNullOrEmpty(nextSceneName))
        {
            yield return StartCoroutine(LoadSceneWithTransition());
        }
    }

    /// <summary>
    /// 带过渡动画的场景加载
    /// </summary>
    private IEnumerator LoadSceneWithTransition()
    {
        // 额外延迟
        if (delayBeforeFade > 0)
        {
            yield return new WaitForSeconds(delayBeforeFade);
        }

        if (useFadeTransition)
        {
            // 优先使用 TransitionManager
            if (TransitionManager.Instance != null)
            {
                TransitionManager.Instance.LoadScene(nextSceneName, fadeColor);
                yield break; // TransitionManager 会处理后续
            }

            // 如果没有 TransitionManager，使用自己的淡出效果
            yield return StartCoroutine(FadeOutAndLoad());
        }
        else
        {
            // 不使用过渡，直接加载
            SceneManager.LoadScene(nextSceneName);
        }
    }

    /// <summary>
    /// 自带的淡出效果（当 TransitionManager 不存在时使用）
    /// </summary>
    private IEnumerator FadeOutAndLoad()
    {
        // 创建临时的黑色遮罩
        GameObject canvasObj = new GameObject("FadeCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 9999;

        CanvasGroup canvasGroup = canvasObj.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false;

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

        // 淡出动画
        float elapsed = 0f;
        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.Clamp01(elapsed / fadeOutDuration);
            yield return null;
        }
        canvasGroup.alpha = 1f;

        // 加载场景
        SceneManager.LoadScene(nextSceneName);
    }

    private int CountVisibleCharacters(string richText)
    {
        if (introText == null) return 0;

        string cachedText = introText.text;
        int cachedMax = introText.maxVisibleCharacters;

        introText.text = richText;
        introText.ForceMeshUpdate();
        int count = introText.textInfo.characterCount;

        introText.text = cachedText;
        introText.maxVisibleCharacters = cachedMax;
        introText.ForceMeshUpdate();

        return count;
    }
}
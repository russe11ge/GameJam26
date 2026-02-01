using System.Collections;
using System.Collections.Generic;
using UnityEngine;
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
            SceneManager.LoadScene(nextSceneName);
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
            SceneManager.LoadScene(nextSceneName);
        }
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
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// 返回主菜单 - 带白色过渡效果
/// 可以挂在任何物体上，通过按钮调用
/// </summary>
public class ReturnToMainMenu : MonoBehaviour
{
    [Header("=== 目标设置 ===")]
    [Tooltip("主菜单场景名称")]
    public string mainMenuSceneName = "1. 主菜单";
    
    [Tooltip("主菜单场景索引（如果名称为空）")]
    public int mainMenuSceneIndex = 0;

    [Header("=== 过渡设置 ===")]
    [Tooltip("过渡颜色")]
    public Color transitionColor = Color.white;
    
    [Tooltip("淡出时长（当前场景）")]
    public float fadeOutDuration = 1f;
    
    [Tooltip("淡入时长（主菜单）")]
    public float fadeInDuration = 1f;

    [Header("=== UI 隐藏设置 ===")]
    [Tooltip("过渡开始时要隐藏的 UI（如暂停菜单）")]
    public GameObject[] uiToHide;

    private static bool needsFadeIn = false;
    private static Color savedTransitionColor = Color.white;
    private static float savedFadeInDuration = 1f;

    private bool isTransitioning = false;

    private void Start()
    {
        // 检查是否需要播放淡入动画
        if (needsFadeIn)
        {
            needsFadeIn = false;
            StartCoroutine(PlayFadeIn());
        }
    }

    /// <summary>
    /// 返回主菜单（Button OnClick 调用）
    /// </summary>
    public void GoToMainMenu()
    {
        if (isTransitioning) return;
        
        Time.timeScale = 1f;
        
        // 保存淡入设置供主菜单使用
        needsFadeIn = true;
        savedTransitionColor = transitionColor;
        savedFadeInDuration = fadeInDuration;
        
        StartCoroutine(TransitionToMainMenu());
    }

    private IEnumerator TransitionToMainMenu()
    {
        isTransitioning = true;

        // 立即隐藏指定的 UI
        HideUI();

        // 创建过渡 Canvas
        GameObject transitionCanvas = CreateTransitionCanvas();
        CanvasGroup canvasGroup = transitionCanvas.GetComponentInChildren<CanvasGroup>();

        // 淡出（当前场景变白）
        float elapsed = 0f;
        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / fadeOutDuration);
            yield return null;
        }
        canvasGroup.alpha = 1f;

        // 加载主菜单
        if (!string.IsNullOrEmpty(mainMenuSceneName))
        {
            SceneManager.LoadScene(mainMenuSceneName);
        }
        else
        {
            SceneManager.LoadScene(mainMenuSceneIndex);
        }
    }

    private void HideUI()
    {
        if (uiToHide == null) return;
        
        foreach (var ui in uiToHide)
        {
            if (ui != null)
            {
                ui.SetActive(false);
            }
        }
    }

    private IEnumerator PlayFadeIn()
    {
        // 创建过渡 Canvas
        GameObject transitionCanvas = CreateTransitionCanvas(savedTransitionColor);
        CanvasGroup canvasGroup = transitionCanvas.GetComponentInChildren<CanvasGroup>();
        canvasGroup.alpha = 1f;

        // 淡入（白色消失）
        float elapsed = 0f;
        while (elapsed < savedFadeInDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / savedFadeInDuration);
            yield return null;
        }

        Destroy(transitionCanvas);
    }

    private GameObject CreateTransitionCanvas(Color? color = null)
    {
        Color useColor = color ?? transitionColor;

        GameObject canvasObj = new GameObject("MainMenuTransitionCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 99999;
        DontDestroyOnLoad(canvasObj);

        GameObject panel = new GameObject("TransitionPanel");
        panel.transform.SetParent(canvasObj.transform, false);

        Image img = panel.AddComponent<Image>();
        img.color = useColor;
        img.raycastTarget = false;

        RectTransform rt = panel.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        CanvasGroup cg = panel.AddComponent<CanvasGroup>();
        cg.alpha = 0f;
        cg.blocksRaycasts = false;

        return canvasObj;
    }
}

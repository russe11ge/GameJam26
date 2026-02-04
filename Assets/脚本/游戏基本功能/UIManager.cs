using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.Events;

/// <summary>
/// UI管理器 - 包含暂停菜单、黑色遮罩过渡、返回主菜单
/// </summary>
public class UIManager : MonoBehaviour
{
    #region 单例（仅当前场景内）
    private static UIManager _instance;
    public static UIManager Instance => _instance;
    #endregion

    #region Inspector设置
    [Header("=== 暂停菜单设置 ===")]
    [Tooltip("启用暂停菜单")]
    public bool enablePauseMenu = true;
    
    [Tooltip("暂停菜单Panel")]
    public GameObject pauseMenuPanel;
    
    [Tooltip("暂停按键")]
    public KeyCode pauseKey = KeyCode.Escape;
    
    [Tooltip("暂停菜单过渡时长")]
    public float pauseMenuTransitionDuration = 0.3f;

    [Header("=== 黑色遮罩（可选，不填会自动创建）===")]
    [Tooltip("黑色遮罩 GameObject（留空会自动创建最高优先级遮罩）")]
    public GameObject blackOverlay;
    
    [Tooltip("返回主菜单时淡出时长")]
    public float fadeOutDuration = 0.8f;
    
    [Tooltip("遮罩颜色")]
    public Color fadeColor = Color.black;

    [Header("=== 主菜单设置 ===")]
    [Tooltip("主菜单场景名称（在这里填写你的主菜单场景名）")]
    public string mainMenuSceneName = "开始界面";
    
    [Tooltip("主菜单淡入时长")]
    public float mainMenuFadeInDuration = 0.5f;

    [Header("=== 重置游戏设置 ===")]
    [Tooltip("重置后进入的场景名称")]
    public string resetSceneName = "开始界面";
    
    [Tooltip("重置后进入的场景索引（如果场景名称为空则使用索引）")]
    public int resetSceneIndex = 0;

    [Header("=== Debug ===")]
    public bool enableDebug = false;
    #endregion

    #region 私有变量
    private bool isPaused = false;
    private bool isTransitioning = false;
    private bool isPauseTransitioning = false;
    private CanvasGroup pauseMenuCanvasGroup;
    private CanvasGroup blackOverlayCanvasGroup;
    
    public UnityAction OnGamePaused;
    public UnityAction OnGameResumed;
    public UnityAction OnGameReset;
    #endregion

    #region Unity生命周期
    private void Awake()
    {
        _instance = this;
        InitializePanels();
    }

    private void Start()
    {
        HideAllPanels();
    }

    private void Update()
    {
        if (enablePauseMenu && Input.GetKeyDown(pauseKey) && !isTransitioning)
        {
            TogglePause();
        }
    }

    private void OnDestroy()
    {
        if (_instance == this)
        {
            _instance = null;
        }
    }
    #endregion

    #region 初始化
    private void InitializePanels()
    {
        // 初始化暂停菜单
        if (pauseMenuPanel != null)
        {
            pauseMenuCanvasGroup = pauseMenuPanel.GetComponent<CanvasGroup>();
            if (pauseMenuCanvasGroup == null)
                pauseMenuCanvasGroup = pauseMenuPanel.AddComponent<CanvasGroup>();
        }

        // 初始化黑色遮罩
        if (blackOverlay != null)
        {
            blackOverlayCanvasGroup = blackOverlay.GetComponent<CanvasGroup>();
            if (blackOverlayCanvasGroup == null)
                blackOverlayCanvasGroup = blackOverlay.AddComponent<CanvasGroup>();
            
            // 初始隐藏，不阻挡鼠标
            blackOverlayCanvasGroup.alpha = 0f;
            blackOverlayCanvasGroup.blocksRaycasts = false;
            blackOverlayCanvasGroup.interactable = false;
            blackOverlay.SetActive(true);
        }
    }

    private void HideAllPanels()
    {
        if (pauseMenuPanel != null)
        {
            if (pauseMenuCanvasGroup != null)
                pauseMenuCanvasGroup.alpha = 0f;
            pauseMenuPanel.SetActive(false);
        }
        
        isPaused = false;
        Time.timeScale = 1f;
    }
    #endregion

    // ==================== Button OnClick 可用方法 ====================

    #region 【返回主菜单 - 黑色过渡】

    /// <summary>
    /// 返回主菜单（带黑色遮罩过渡）
    /// 画面整体变黑（最高优先级UI黑色遮罩），然后回到主菜单scene
    /// </summary>
    public void GoToMainMenu()
    {
        if (isTransitioning) return;

        if (string.IsNullOrEmpty(mainMenuSceneName))
        {
            Debug.LogError("[UIManager] mainMenuSceneName 为空！请在 Inspector 中设置主菜单场景名称。");
            return;
        }

        if (enableDebug) Debug.Log($"[UIManager] 返回主菜单: {mainMenuSceneName}");

        isTransitioning = true;
        Time.timeScale = 1f;

        StartCoroutine(TransitionToMainMenu());
    }

    private IEnumerator TransitionToMainMenu()
    {
        // 如果没有手动设置黑色遮罩，自动创建一个最高优先级的
        CanvasGroup fadeCanvasGroup = blackOverlayCanvasGroup;
        GameObject autoCreatedCanvas = null;
        
        if (fadeCanvasGroup == null)
        {
            autoCreatedCanvas = CreateHighPriorityFadeCanvas();
            fadeCanvasGroup = autoCreatedCanvas.GetComponent<CanvasGroup>();
            if (enableDebug) Debug.Log("[UIManager] 自动创建黑色遮罩");
        }

        // 记录暂停菜单初始 alpha
        float pauseMenuStartAlpha = 0f;
        if (pauseMenuCanvasGroup != null && pauseMenuPanel != null && pauseMenuPanel.activeSelf)
        {
            pauseMenuStartAlpha = pauseMenuCanvasGroup.alpha;
        }

        if (enableDebug) Debug.Log("[UIManager] 开始黑屏过渡");

        // 同时：黑色遮罩出现 + 暂停菜单消失
        float elapsed = 0f;
        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / fadeOutDuration);

            // 黑色遮罩渐渐出现
            if (fadeCanvasGroup != null)
            {
                fadeCanvasGroup.alpha = progress;
            }

            // 暂停菜单渐渐消失
            if (pauseMenuCanvasGroup != null && pauseMenuStartAlpha > 0)
            {
                pauseMenuCanvasGroup.alpha = Mathf.Lerp(pauseMenuStartAlpha, 0f, progress);
            }

            yield return null;
        }

        // 确保完全黑屏
        if (fadeCanvasGroup != null)
        {
            fadeCanvasGroup.alpha = 1f;
        }

        if (enableDebug) Debug.Log($"[UIManager] 黑屏完成，加载主菜单: {mainMenuSceneName}");

        // 通知主菜单需要淡入
        MainMenuController.SetNeedsFadeIn(mainMenuFadeInDuration);

        // 加载主菜单
        SceneManager.LoadScene(mainMenuSceneName);
    }

    /// <summary>
    /// 创建最高优先级的黑色遮罩 Canvas
    /// </summary>
    private GameObject CreateHighPriorityFadeCanvas()
    {
        // 创建 Canvas
        GameObject canvasObj = new GameObject("UIManager_FadeCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 32767; // 最高优先级

        // 添加 CanvasGroup
        CanvasGroup canvasGroup = canvasObj.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = true; // 阻挡点击
        canvasGroup.interactable = false;

        // 创建黑色面板
        GameObject panel = new GameObject("FadePanel");
        panel.transform.SetParent(canvasObj.transform, false);

        Image img = panel.AddComponent<Image>();
        img.color = fadeColor;
        img.raycastTarget = true;

        // 铺满全屏
        RectTransform rt = panel.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        return canvasObj;
    }

    #endregion

    #region 【场景加载】

    /// <summary>
    /// 加载主菜单（场景0）- 简单加载，无过渡
    /// </summary>
    public void LoadMainMenu()
    {
        Time.timeScale = 1f;
        isPaused = false;
        SceneManager.LoadScene(0);
    }

    /// <summary>
    /// 加载下一个场景
    /// </summary>
    public void LoadNextScene()
    {
        int next = SceneManager.GetActiveScene().buildIndex + 1;
        if (next < SceneManager.sceneCountInBuildSettings)
        {
            SceneManager.LoadScene(next);
        }
    }

    /// <summary>
    /// 加载上一个场景
    /// </summary>
    public void LoadPreviousScene()
    {
        int prev = SceneManager.GetActiveScene().buildIndex - 1;
        if (prev >= 0)
        {
            SceneManager.LoadScene(prev);
        }
    }

    /// <summary>
    /// 重新加载当前场景
    /// </summary>
    public void ReloadCurrentScene()
    {
        Time.timeScale = 1f;
        isPaused = false;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    /// <summary>
    /// 通过场景名称加载
    /// </summary>
    public void LoadScene(string sceneName)
    {
        Time.timeScale = 1f;
        isPaused = false;
        SceneManager.LoadScene(sceneName);
    }

    /// <summary>
    /// 通过场景索引加载
    /// </summary>
    public void LoadSceneByIndex(int sceneIndex)
    {
        Time.timeScale = 1f;
        isPaused = false;
        SceneManager.LoadScene(sceneIndex);
    }

    #endregion

    #region 【重置游戏】

    /// <summary>
    /// 重置游戏 - 清除所有数据并返回指定场景
    /// </summary>
    public void ResetGame()
    {
        Time.timeScale = 1f;
        isPaused = false;
        
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();
        OnGameReset?.Invoke();
        
        Debug.Log("游戏已重置");
        
        // 使用黑色过渡返回主菜单
        GoToMainMenu();
    }

    #endregion

    #region 【暂停菜单】

    /// <summary>
    /// 切换暂停状态
    /// </summary>
    public void TogglePause()
    {
        if (isPauseTransitioning || isTransitioning) return;
        
        if (isPaused)
            ResumeGame();
        else
            PauseGame();
    }

    /// <summary>
    /// 暂停游戏
    /// </summary>
    public void PauseGame()
    {
        if (isPauseTransitioning || isPaused || isTransitioning) return;
        
        if (enableDebug) Debug.Log("[UIManager] 暂停游戏");
        
        isPaused = true;
        Time.timeScale = 0f;
        
        if (pauseMenuPanel != null)
        {
            StartCoroutine(PauseMenuFadeIn());
        }
        
        OnGamePaused?.Invoke();
    }

    /// <summary>
    /// 继续游戏
    /// </summary>
    public void ResumeGame()
    {
        if (isPauseTransitioning || !isPaused || isTransitioning) return;
        
        if (enableDebug) Debug.Log("[UIManager] 继续游戏");
        
        if (pauseMenuPanel != null)
        {
            StartCoroutine(PauseMenuFadeOut());
        }
        else
        {
            isPaused = false;
            Time.timeScale = 1f;
        }
        
        OnGameResumed?.Invoke();
    }

    #endregion

    #region 【游戏控制】

    /// <summary>
    /// 退出游戏
    /// </summary>
    public void QuitGame()
    {
        Debug.Log("退出游戏");
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }

    /// <summary>
    /// 打开网页链接
    /// </summary>
    public void OpenURL(string url)
    {
        Application.OpenURL(url);
    }

    #endregion

    #region 【UI面板控制】

    /// <summary>
    /// 显示面板
    /// </summary>
    public void ShowPanel(GameObject panel)
    {
        if (panel != null)
            panel.SetActive(true);
    }

    /// <summary>
    /// 隐藏面板
    /// </summary>
    public void HidePanel(GameObject panel)
    {
        if (panel != null)
            panel.SetActive(false);
    }

    /// <summary>
    /// 切换面板显隐
    /// </summary>
    public void TogglePanel(GameObject panel)
    {
        if (panel != null)
            panel.SetActive(!panel.activeSelf);
    }

    #endregion

    #region 【暂停菜单动画】
    
    private IEnumerator PauseMenuFadeIn()
    {
        isPauseTransitioning = true;
        
        pauseMenuPanel.SetActive(true);
        
        if (pauseMenuCanvasGroup != null)
        {
            pauseMenuCanvasGroup.alpha = 0f;
            
            float t = 0f;
            while (t < pauseMenuTransitionDuration)
            {
                t += Time.unscaledDeltaTime;
                pauseMenuCanvasGroup.alpha = Mathf.Lerp(0f, 1f, t / pauseMenuTransitionDuration);
                yield return null;
            }
            
            pauseMenuCanvasGroup.alpha = 1f;
        }
        
        isPauseTransitioning = false;
    }
    
    private IEnumerator PauseMenuFadeOut()
    {
        isPauseTransitioning = true;
        
        if (pauseMenuCanvasGroup != null)
        {
            float t = 0f;
            while (t < pauseMenuTransitionDuration)
            {
                t += Time.unscaledDeltaTime;
                pauseMenuCanvasGroup.alpha = Mathf.Lerp(1f, 0f, t / pauseMenuTransitionDuration);
                yield return null;
            }
            
            pauseMenuCanvasGroup.alpha = 0f;
        }
        
        pauseMenuPanel.SetActive(false);
        isPaused = false;
        Time.timeScale = 1f;
        
        isPauseTransitioning = false;
    }

    #endregion

    #region 【属性】
    public bool IsPaused => isPaused;
    public bool IsTransitioning => isTransitioning;
    #endregion
}

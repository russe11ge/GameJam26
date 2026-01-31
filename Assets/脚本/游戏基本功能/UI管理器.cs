using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Events;

/// <summary>
/// UI管理器 - 单例模式
/// 管理所有UI相关功能，包括按钮点击事件、场景切换等
/// 使用方法：在Button的OnClick()中选择UIManager的公共方法
/// </summary>
public class UIManager : MonoBehaviour
{
    #region 单例模式
    private static UIManager _instance;
    public static UIManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<UIManager>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("UIManager");
                    _instance = go.AddComponent<UIManager>();
                }
            }
            return _instance;
        }
    }
    #endregion

    #region 设置选项
    [Header("=== 基础设置 ===")]
    [Tooltip("是否在场景切换时保留此对象")]
    public bool dontDestroyOnLoad = true;

    [Header("=== 暂停菜单设置 ===")]
    [Tooltip("是否启用暂停菜单功能")]
    public bool enablePauseMenu = true;
    
    [Tooltip("暂停菜单Panel")]
    public GameObject pauseMenuPanel;
    
    [Tooltip("暂停按键（默认Escape）")]
    public KeyCode pauseKey = KeyCode.Escape;

    [Header("=== 过渡动画设置 ===")]
    [Tooltip("是否启用场景过渡动画")]
    public bool enableTransition = true;
    
    [Tooltip("过渡动画Panel（包含淡入淡出效果）")]
    public GameObject transitionPanel;
    
    [Tooltip("过渡动画时长")]
    public float transitionDuration = 0.5f;
    #endregion

    #region 私有变量
    private bool isPaused = false;
    private bool isTransitioning = false;
    private CanvasGroup transitionCanvasGroup;
    #endregion

    #region Unity 生命周期
    private void Awake()
    {
        // 单例检查
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;

        if (dontDestroyOnLoad)
        {
            DontDestroyOnLoad(gameObject);
        }

        // 初始化过渡动画
        if (transitionPanel != null)
        {
            transitionCanvasGroup = transitionPanel.GetComponent<CanvasGroup>();
            if (transitionCanvasGroup == null)
            {
                transitionCanvasGroup = transitionPanel.AddComponent<CanvasGroup>();
            }
        }
    }

    private void Update()
    {
        // 暂停菜单检测
        if (enablePauseMenu && Input.GetKeyDown(pauseKey))
        {
            TogglePause();
        }
    }
    #endregion

    #region ========== 场景管理功能 ==========
    
    /// <summary>
    /// 通过场景名称加载场景
    /// </summary>
    /// <param name="sceneName">场景名称</param>
    public void LoadScene(string sceneName)
    {
        if (enableTransition)
        {
            StartCoroutine(LoadSceneWithTransition(sceneName));
        }
        else
        {
            SceneManager.LoadScene(sceneName);
        }
    }

    /// <summary>
    /// 通过场景索引加载场景
    /// </summary>
    /// <param name="sceneIndex">场景索引</param>
    public void LoadSceneByIndex(int sceneIndex)
    {
        if (enableTransition)
        {
            StartCoroutine(LoadSceneByIndexWithTransition(sceneIndex));
        }
        else
        {
            SceneManager.LoadScene(sceneIndex);
        }
    }

    /// <summary>
    /// 加载主菜单 (场景索引0)
    /// </summary>
    public void LoadMainMenu()
    {
        Time.timeScale = 1f;
        isPaused = false;
        LoadSceneByIndex(0);
    }

    /// <summary>
    /// 加载下一个场景
    /// </summary>
    public void LoadNextScene()
    {
        int nextIndex = SceneManager.GetActiveScene().buildIndex + 1;
        if (nextIndex < SceneManager.sceneCountInBuildSettings)
        {
            LoadSceneByIndex(nextIndex);
        }
        else
        {
            Debug.LogWarning("已经是最后一个场景！");
        }
    }

    /// <summary>
    /// 加载上一个场景
    /// </summary>
    public void LoadPreviousScene()
    {
        int prevIndex = SceneManager.GetActiveScene().buildIndex - 1;
        if (prevIndex >= 0)
        {
            LoadSceneByIndex(prevIndex);
        }
        else
        {
            Debug.LogWarning("已经是第一个场景！");
        }
    }

    /// <summary>
    /// 重新加载当前场景
    /// </summary>
    public void ReloadCurrentScene()
    {
        Time.timeScale = 1f;
        isPaused = false;
        LoadSceneByIndex(SceneManager.GetActiveScene().buildIndex);
    }

    // 带过渡动画的场景加载协程
    private IEnumerator LoadSceneWithTransition(string sceneName)
    {
        if (isTransitioning) yield break;
        isTransitioning = true;

        // 淡出
        yield return StartCoroutine(FadeOut());

        // 加载场景
        SceneManager.LoadScene(sceneName);

        // 淡入
        yield return StartCoroutine(FadeIn());

        isTransitioning = false;
    }

    private IEnumerator LoadSceneByIndexWithTransition(int sceneIndex)
    {
        if (isTransitioning) yield break;
        isTransitioning = true;

        yield return StartCoroutine(FadeOut());
        SceneManager.LoadScene(sceneIndex);
        yield return StartCoroutine(FadeIn());

        isTransitioning = false;
    }
    #endregion

    #region ========== 暂停菜单功能 ==========
    
    /// <summary>
    /// 切换暂停状态
    /// </summary>
    public void TogglePause()
    {
        if (isPaused)
        {
            ResumeGame();
        }
        else
        {
            PauseGame();
        }
    }

    /// <summary>
    /// 暂停游戏
    /// </summary>
    public void PauseGame()
    {
        isPaused = true;
        Time.timeScale = 0f;
        
        if (pauseMenuPanel != null)
        {
            pauseMenuPanel.SetActive(true);
        }

        // 触发暂停事件
        OnGamePaused?.Invoke();
    }

    /// <summary>
    /// 继续游戏
    /// </summary>
    public void ResumeGame()
    {
        isPaused = false;
        Time.timeScale = 1f;
        
        if (pauseMenuPanel != null)
        {
            pauseMenuPanel.SetActive(false);
        }

        // 触发继续事件
        OnGameResumed?.Invoke();
    }

    /// <summary>
    /// 获取暂停状态
    /// </summary>
    public bool IsPaused => isPaused;

    // 暂停/继续事件
    public UnityAction OnGamePaused;
    public UnityAction OnGameResumed;
    #endregion

    #region ========== 游戏控制功能 ==========
    
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
    /// 打开URL链接
    /// </summary>
    /// <param name="url">网址</param>
    public void OpenURL(string url)
    {
        Application.OpenURL(url);
    }
    #endregion

    #region ========== UI面板控制 ==========
    
    /// <summary>
    /// 显示指定Panel
    /// </summary>
    /// <param name="panel">要显示的Panel</param>
    public void ShowPanel(GameObject panel)
    {
        if (panel != null)
        {
            panel.SetActive(true);
        }
    }

    /// <summary>
    /// 隐藏指定Panel
    /// </summary>
    /// <param name="panel">要隐藏的Panel</param>
    public void HidePanel(GameObject panel)
    {
        if (panel != null)
        {
            panel.SetActive(false);
        }
    }

    /// <summary>
    /// 切换Panel显示状态
    /// </summary>
    /// <param name="panel">要切换的Panel</param>
    public void TogglePanel(GameObject panel)
    {
        if (panel != null)
        {
            panel.SetActive(!panel.activeSelf);
        }
    }

    /// <summary>
    /// 带动画显示Panel
    /// </summary>
    public void ShowPanelAnimated(GameObject panel)
    {
        if (panel != null)
        {
            panel.SetActive(true);
            StartCoroutine(FadeInPanel(panel));
        }
    }

    /// <summary>
    /// 带动画隐藏Panel
    /// </summary>
    public void HidePanelAnimated(GameObject panel)
    {
        if (panel != null)
        {
            StartCoroutine(FadeOutPanel(panel));
        }
    }
    #endregion

    #region ========== 过渡动画 ==========
    
    private IEnumerator FadeOut()
    {
        if (transitionPanel == null || transitionCanvasGroup == null) yield break;

        transitionPanel.SetActive(true);
        float elapsed = 0f;

        while (elapsed < transitionDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            transitionCanvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / transitionDuration);
            yield return null;
        }

        transitionCanvasGroup.alpha = 1f;
    }

    private IEnumerator FadeIn()
    {
        if (transitionPanel == null || transitionCanvasGroup == null) yield break;

        float elapsed = 0f;

        while (elapsed < transitionDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            transitionCanvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / transitionDuration);
            yield return null;
        }

        transitionCanvasGroup.alpha = 0f;
        transitionPanel.SetActive(false);
    }

    private IEnumerator FadeInPanel(GameObject panel)
    {
        CanvasGroup cg = panel.GetComponent<CanvasGroup>();
        if (cg == null) cg = panel.AddComponent<CanvasGroup>();

        cg.alpha = 0f;
        float elapsed = 0f;

        while (elapsed < transitionDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            cg.alpha = Mathf.Lerp(0f, 1f, elapsed / transitionDuration);
            yield return null;
        }

        cg.alpha = 1f;
    }

    private IEnumerator FadeOutPanel(GameObject panel)
    {
        CanvasGroup cg = panel.GetComponent<CanvasGroup>();
        if (cg == null) cg = panel.AddComponent<CanvasGroup>();

        float elapsed = 0f;

        while (elapsed < transitionDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            cg.alpha = Mathf.Lerp(1f, 0f, elapsed / transitionDuration);
            yield return null;
        }

        cg.alpha = 0f;
        panel.SetActive(false);
    }
    #endregion

    #region ========== 实用工具 ==========
    
    /// <summary>
    /// 延迟执行方法
    /// </summary>
    /// <param name="delay">延迟时间</param>
    /// <param name="action">要执行的方法</param>
    public void DelayedAction(float delay, UnityAction action)
    {
        StartCoroutine(DelayedActionCoroutine(delay, action));
    }

    private IEnumerator DelayedActionCoroutine(float delay, UnityAction action)
    {
        yield return new WaitForSecondsRealtime(delay);
        action?.Invoke();
    }

    /// <summary>
    /// 显示确认对话框
    /// </summary>
    public void ShowConfirmDialog(GameObject dialogPanel, UnityAction onConfirm, UnityAction onCancel)
    {
        if (dialogPanel != null)
        {
            dialogPanel.SetActive(true);
        }
    }
    #endregion
}

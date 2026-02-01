using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Events;

/// <summary>
/// UI管理器 - 每个场景独立
/// 使用 TransitionManager 处理场景过渡动画
/// </summary>
public class UIManager : MonoBehaviour
{
    #region 单例（仅当前场景内）
    private static UIManager _instance;
    public static UIManager Instance => _instance;
    #endregion

    #region Inspector设置
    [Header("=== 过渡动画设置 ===")]
    [Tooltip("过渡颜色")]
    public Color transitionColor = Color.black;
    
    [Tooltip("淡出动画时长")]
    public float fadeOutDuration = 0.5f;
    
    [Tooltip("淡入动画时长")]
    public float fadeInDuration = 0.5f;

    [Header("=== 暂停菜单设置 ===")]
    [Tooltip("启用暂停菜单")]
    public bool enablePauseMenu = true;
    
    [Tooltip("暂停菜单Panel")]
    public GameObject pauseMenuPanel;
    
    [Tooltip("暂停按键")]
    public KeyCode pauseKey = KeyCode.Escape;
    
    [Tooltip("暂停菜单过渡时长")]
    public float pauseMenuTransitionDuration = 0.3f;

    [Header("=== 重置游戏设置 ===")]
    [Tooltip("重置后进入的场景名称")]
    public string resetSceneName = "1. 主菜单";
    
    [Tooltip("重置后进入的场景索引（如果场景名称为空则使用索引）")]
    public int resetSceneIndex = 0;
    #endregion

    #region 私有变量
    private bool isPaused = false;
    private bool isTransitioning = false;
    private bool isPauseTransitioning = false;
    private CanvasGroup pauseMenuCanvasGroup;
    
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
        if (enablePauseMenu && Input.GetKeyDown(pauseKey))
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
        if (pauseMenuPanel != null)
        {
            pauseMenuCanvasGroup = pauseMenuPanel.GetComponent<CanvasGroup>();
            if (pauseMenuCanvasGroup == null)
                pauseMenuCanvasGroup = pauseMenuPanel.AddComponent<CanvasGroup>();
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

    #region 【场景加载 - 无参数】

    /// <summary>
    /// 加载主菜单（场景0）
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
        int next = SceneManager.GetActiveScene().buildIndex + 1;
        if (next < SceneManager.sceneCountInBuildSettings)
        {
            LoadSceneByIndex(next);
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
            LoadSceneByIndex(prev);
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

    #endregion

    #region 【场景加载 - 带参数】

    /// <summary>
    /// 通过场景名称加载
    /// </summary>
    public void LoadScene(string sceneName)
    {
        if (isTransitioning) return;
        
        Time.timeScale = 1f;
        isPaused = false;
        isTransitioning = true;
        
        if (TransitionManager.Instance != null)
        {
            TransitionManager.Instance.LoadScene(sceneName, transitionColor, fadeOutDuration, fadeInDuration);
        }
        else
        {
            SceneManager.LoadScene(sceneName);
        }
    }

    /// <summary>
    /// 通过场景索引加载
    /// </summary>
    public void LoadSceneByIndex(int sceneIndex)
    {
        if (isTransitioning) return;
        
        Time.timeScale = 1f;
        isPaused = false;
        isTransitioning = true;
        
        if (TransitionManager.Instance != null)
        {
            TransitionManager.Instance.LoadSceneByIndex(sceneIndex, transitionColor, fadeOutDuration, fadeInDuration);
        }
        else
        {
            SceneManager.LoadScene(sceneIndex);
        }
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
        
        if (!string.IsNullOrEmpty(resetSceneName))
        {
            LoadScene(resetSceneName);
        }
        else
        {
            LoadSceneByIndex(resetSceneIndex);
        }
    }

    /// <summary>
    /// 重置游戏并加载指定场景（通过场景名）
    /// </summary>
    public void ResetGameToScene(string sceneName)
    {
        Time.timeScale = 1f;
        isPaused = false;
        
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();
        OnGameReset?.Invoke();
        
        LoadScene(sceneName);
    }

    /// <summary>
    /// 重置游戏并加载指定场景（通过索引）
    /// </summary>
    public void ResetGameToSceneIndex(int sceneIndex)
    {
        Time.timeScale = 1f;
        isPaused = false;
        
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();
        OnGameReset?.Invoke();
        
        LoadSceneByIndex(sceneIndex);
    }

    #endregion

    #region 【暂停菜单】

    /// <summary>
    /// 切换暂停状态
    /// </summary>
    public void TogglePause()
    {
        if (isPauseTransitioning) return;
        
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
        if (isPauseTransitioning || isPaused) return;
        
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
        if (isPauseTransitioning || !isPaused) return;
        
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

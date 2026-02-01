using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 返回主菜单 - 使用手动添加的黑色遮罩
/// 配合 MainMenuController 的黑色淡入
/// </summary>
public class ReturnToMainMenu : MonoBehaviour
{
    [Header("=== 目标设置 ===")]
    [Tooltip("主菜单场景名称")]
    public string mainMenuSceneName = "1. 主菜单";

    [Header("=== 黑色遮罩（手动添加）===")]
    [Tooltip("黑色遮罩 GameObject（需要有 CanvasGroup）")]
    public GameObject blackOverlay;
    
    [Tooltip("淡出时长（黑色出现）")]
    public float fadeOutDuration = 1f;
    
    [Tooltip("主菜单淡入时长")]
    public float mainMenuFadeInDuration = 0.5f;

    [Header("=== UI 淡出设置 ===")]
    [Tooltip("过渡时要渐渐消失的 UI（如暂停菜单面板）")]
    public GameObject[] uiToFadeOut;

    [Header("=== Debug ===")]
    public bool enableDebug = true;

    private CanvasGroup blackOverlayCanvasGroup;
    private bool isTransitioning = false;

    private void Awake()
    {
        // 初始化黑色遮罩
        if (blackOverlay != null)
        {
            blackOverlayCanvasGroup = blackOverlay.GetComponent<CanvasGroup>();
            if (blackOverlayCanvasGroup == null)
            {
                blackOverlayCanvasGroup = blackOverlay.AddComponent<CanvasGroup>();
            }
            // 初始隐藏
            blackOverlayCanvasGroup.alpha = 0f;
            // 不阻挡鼠标
            blackOverlayCanvasGroup.blocksRaycasts = false;
            blackOverlayCanvasGroup.interactable = false;
            blackOverlay.SetActive(true);
        }
    }

    /// <summary>
    /// 返回主菜单（Button OnClick 调用）
    /// </summary>
    public void GoToMainMenu()
    {
        if (isTransitioning) return;
        
        if (enableDebug) Debug.Log("[ReturnToMainMenu] GoToMainMenu() 被调用");
        
        Time.timeScale = 1f;
        isTransitioning = true;
        
        StartCoroutine(TransitionToMainMenu());
    }

    private IEnumerator TransitionToMainMenu()
    {
        // 准备需要淡出的 UI
        CanvasGroup[] uiCanvasGroups = PrepareUIForFadeOut();
        float[] originalAlphas = null;
        
        if (uiCanvasGroups != null)
        {
            originalAlphas = new float[uiCanvasGroups.Length];
            for (int i = 0; i < uiCanvasGroups.Length; i++)
            {
                if (uiCanvasGroups[i] != null)
                {
                    originalAlphas[i] = uiCanvasGroups[i].alpha;
                }
            }
        }

        if (enableDebug) Debug.Log("[ReturnToMainMenu] 开始淡出");

        // 淡出（黑色遮罩出现 + UI 消失）
        float elapsed = 0f;
        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / fadeOutDuration);
            
            // 黑色遮罩渐渐出现
            if (blackOverlayCanvasGroup != null)
            {
                blackOverlayCanvasGroup.alpha = progress;
            }
            
            // UI 渐渐消失
            if (uiCanvasGroups != null && originalAlphas != null)
            {
                for (int i = 0; i < uiCanvasGroups.Length; i++)
                {
                    if (uiCanvasGroups[i] != null)
                    {
                        uiCanvasGroups[i].alpha = Mathf.Lerp(originalAlphas[i], 0f, progress);
                    }
                }
            }
            
            yield return null;
        }

        // 确保完全淡出
        if (blackOverlayCanvasGroup != null)
        {
            blackOverlayCanvasGroup.alpha = 1f;
        }

        if (enableDebug) Debug.Log("[ReturnToMainMenu] 淡出完成，加载主菜单");

        // 通知 MainMenuController 需要淡入
        MainMenuController.SetNeedsFadeIn(mainMenuFadeInDuration);

        // 加载主菜单
        SceneManager.LoadScene(mainMenuSceneName);
    }

    private CanvasGroup[] PrepareUIForFadeOut()
    {
        if (uiToFadeOut == null || uiToFadeOut.Length == 0) return null;

        CanvasGroup[] groups = new CanvasGroup[uiToFadeOut.Length];
        
        for (int i = 0; i < uiToFadeOut.Length; i++)
        {
            if (uiToFadeOut[i] == null) continue;
            
            CanvasGroup cg = uiToFadeOut[i].GetComponent<CanvasGroup>();
            if (cg == null)
            {
                cg = uiToFadeOut[i].AddComponent<CanvasGroup>();
            }
            groups[i] = cg;
        }
        
        return groups;
    }
}

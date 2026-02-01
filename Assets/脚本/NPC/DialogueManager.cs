using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance;

    [Header("UI References")]
    [Tooltip("BottomBar 整体对象（黑边+文字的父物体）")]
    public GameObject bottomBarRoot;

    [Tooltip("对白文本（TextMeshProUGUI）")]
    public TextMeshProUGUI dialogueText;

    [Tooltip("提示文本（可选，比如：Press Space）")]
    public TextMeshProUGUI hintText;

    [Header("Input")]
    public KeyCode advanceKey = KeyCode.Space;

    [Header("Typewriter (Optional)")]
    public bool useTypewriter = false;
    [Range(0.005f, 0.08f)]
    public float charDelay = 0.02f;

    // 内部状态
    private readonly Queue<string> lines = new Queue<string>();
    private bool isTalking = false;
    private bool isTyping = false;

    private string currentLine = "";
    private Coroutine typingCoroutine;

    private Action onDialogueComplete;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
    }

    void Start()
    {
        HideUI();
    }

    void Update()
    {
        if (!isTalking) return;

        if (Input.GetKeyDown(advanceKey))
        {
            // 如果正在打字，按一次键直接补全这一句
            if (useTypewriter && isTyping)
            {
                FinishTypingImmediately();
                return;
            }

            DisplayNextLine();
        }
    }

    /// <summary>
    /// 开始对话：传入台词列表 + 可选结束回调（用于解锁面具/推进关卡）
    /// </summary>
    public void StartDialogue(List<string> dialogueLines, Action onComplete = null)
    {
        if (dialogueLines == null || dialogueLines.Count == 0)
        {
            Debug.LogWarning("[DialogueManager] StartDialogue called with empty lines.");
            return;
        }

        // 终止正在进行的对话（避免重入）
        ForceStopDialogue();

        ShowUI();

        lines.Clear();
        foreach (var line in dialogueLines)
            lines.Enqueue(line);

        onDialogueComplete = onComplete;
        isTalking = true;

        DisplayNextLine();
    }

    /// <summary>
    /// 对外：是否正在对话（用来冻结玩家移动）
    /// </summary>
    public bool IsTalking() => isTalking;

    /// <summary>
    /// 强制停止（比如切场景、紧急中断）
    /// </summary>
    public void ForceStopDialogue()
    {
        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);

        typingCoroutine = null;
        isTyping = false;
        isTalking = false;
        currentLine = "";
        lines.Clear();
        onDialogueComplete = null;

        HideUI();
    }

    // ---------------- Internal ----------------

    private void DisplayNextLine()
    {
        // 没有下一句：结束
        if (lines.Count == 0)
        {
            EndDialogue();
            return;
        }

        currentLine = lines.Dequeue();

        if (!useTypewriter)
        {
            dialogueText.text = currentLine;
        }
        else
        {
            StartTypeLine(currentLine);
        }
    }

    private void EndDialogue()
    {
        isTalking = false;

        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);

        typingCoroutine = null;
        isTyping = false;
        currentLine = "";

        HideUI();

        // 触发结束回调（解锁面具/推进线性）
        var cb = onDialogueComplete;
        onDialogueComplete = null;
        cb?.Invoke();
    }

    private void ShowUI()
    {
        if (bottomBarRoot != null) bottomBarRoot.SetActive(true);
        if (dialogueText != null) dialogueText.text = "";
        if (hintText != null)
        {
            hintText.gameObject.SetActive(true);
            // 你可以在HintText里写：Press [Space]
        }
    }

    private void HideUI()
    {
        if (dialogueText != null) dialogueText.text = "";
        if (hintText != null) hintText.gameObject.SetActive(false);
        if (bottomBarRoot != null) bottomBarRoot.SetActive(false);
    }

    // -------- Typewriter --------

    private void StartTypeLine(string line)
    {
        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);

        typingCoroutine = StartCoroutine(TypeLineCoroutine(line));
    }

    private IEnumerator TypeLineCoroutine(string line)
    {
        isTyping = true;
        dialogueText.text = "";

        for (int i = 0; i < line.Length; i++)
        {
            dialogueText.text += line[i];
            yield return new WaitForSeconds(charDelay);
        }

        isTyping = false;
        typingCoroutine = null;
    }

    private void FinishTypingImmediately()
    {
        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);

        typingCoroutine = null;
        isTyping = false;

        dialogueText.text = currentLine;
    }
}
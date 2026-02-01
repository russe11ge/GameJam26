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

    [Header("Typewriter")]
    [Tooltip("启用打字机效果")]
    public bool useTypewriter = true;
    
    [Tooltip("每个字符的显示间隔（秒）")]
    [Range(0.01f, 0.1f)]
    public float charDelay = 0.03f;

    // 内部状态
    private readonly Queue<string> lines = new Queue<string>();
    private bool isTalking = false;
    private bool isTyping = false;

    private string currentLine = "";
    private int totalCharacters = 0;
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
    /// 开始对话
    /// </summary>
    public void StartDialogue(List<string> dialogueLines, Action onComplete = null)
    {
        if (dialogueLines == null || dialogueLines.Count == 0)
        {
            Debug.LogWarning("[DialogueManager] StartDialogue called with empty lines.");
            return;
        }

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
    /// 是否正在对话
    /// </summary>
    public bool IsTalking() => isTalking;

    /// <summary>
    /// 强制停止对话
    /// </summary>
    public void ForceStopDialogue()
    {
        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);

        typingCoroutine = null;
        isTyping = false;
        isTalking = false;
        currentLine = "";
        totalCharacters = 0;
        lines.Clear();
        onDialogueComplete = null;

        HideUI();
    }

    // ---------------- Internal ----------------

    private void DisplayNextLine()
    {
        if (lines.Count == 0)
        {
            EndDialogue();
            return;
        }

        currentLine = lines.Dequeue();

        if (!useTypewriter)
        {
            // 直接显示全部
            dialogueText.text = currentLine;
            dialogueText.maxVisibleCharacters = currentLine.Length;
        }
        else
        {
            // 打字机效果
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
        totalCharacters = 0;

        HideUI();

        var cb = onDialogueComplete;
        onDialogueComplete = null;
        cb?.Invoke();
    }

    private void ShowUI()
    {
        if (bottomBarRoot != null) bottomBarRoot.SetActive(true);
        if (dialogueText != null)
        {
            dialogueText.text = "";
            dialogueText.maxVisibleCharacters = 0;
        }
        if (hintText != null)
        {
            hintText.gameObject.SetActive(true);
        }
    }

    private void HideUI()
    {
        if (dialogueText != null)
        {
            dialogueText.text = "";
            dialogueText.maxVisibleCharacters = 0;
        }
        if (hintText != null) hintText.gameObject.SetActive(false);
        if (bottomBarRoot != null) bottomBarRoot.SetActive(false);
    }

    // -------- Typewriter (using maxVisibleCharacters) --------

    private void StartTypeLine(string line)
    {
        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);

        typingCoroutine = StartCoroutine(TypeLineCoroutine(line));
    }

    private IEnumerator TypeLineCoroutine(string line)
    {
        isTyping = true;
        
        // 设置完整文本，但隐藏所有字符
        dialogueText.text = line;
        totalCharacters = line.Length;
        dialogueText.maxVisibleCharacters = 0;

        // 逐字显示
        for (int i = 1; i <= totalCharacters; i++)
        {
            dialogueText.maxVisibleCharacters = i;
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

        // 显示所有字符
        dialogueText.maxVisibleCharacters = totalCharacters;
    }
}

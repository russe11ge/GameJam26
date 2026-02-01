using System;
using System.Collections.Generic;
using UnityEngine;

public class NPCConditionalDialogue : MonoBehaviour
{
    [Header("Interaction")]
    public KeyCode interactKey = KeyCode.E;
    public bool playerMustBeInRange = true;

    [Header("Condition")]
    public string requiredMaskId;          // 需要的面具（比如 mask1）
    public bool silentWhenLocked = true;   // 没满足条件时是否完全不说话

    [Header("Dialogue (Locked)")]
    [TextArea(2, 4)]
    public List<string> lockedLines = new List<string>() { "……（他没有回应你）" };

    [Header("Dialogue (Unlocked)")]
    [TextArea(2, 4)]
    public List<string> mainLines = new List<string>();

    [Header("Reward After Dialogue (Optional)")]
    public bool rewardMaskAfterTalk = false;
    public string rewardMaskId;            // 对话完发的面具（比如 mask2）
    public bool triggerOnce = true;

    private bool playerInRange = false;
    private bool hasTriggered = false;

    void Update()
    {
        if (triggerOnce && hasTriggered) return;

        if (playerMustBeInRange && !playerInRange) return;

        // 对话中不允许触发
        if (DialogueManager.Instance != null && DialogueManager.Instance.IsTalking())
            return;

        if (Input.GetKeyDown(interactKey))
        {
            TryTalk();
        }
    }

    private void TryTalk()
    {
        bool unlocked = true;

        if (!string.IsNullOrEmpty(requiredMaskId))
        {
            if (PlayerMaskManager.Instance == null) unlocked = false;
            else unlocked = PlayerMaskManager.Instance.HasMask(requiredMaskId);
        }

        // 没解锁：要么不响应，要么弹一句锁定对白
        if (!unlocked)
        {
            if (silentWhenLocked) return;

            DialogueManager.Instance.StartDialogue(lockedLines);
            return;
        }

        // 解锁：播放主对白
        DialogueManager.Instance.StartDialogue(mainLines, OnDialogueFinished);
        hasTriggered = true;
    }

    private void OnDialogueFinished()
    {
        if (rewardMaskAfterTalk && !string.IsNullOrEmpty(rewardMaskId))
        {
            PlayerMaskManager.Instance.UnlockMask(rewardMaskId);
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player")) playerInRange = true;
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player")) playerInRange = false;
    }
}
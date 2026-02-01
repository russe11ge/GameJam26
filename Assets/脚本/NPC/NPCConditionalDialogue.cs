using System;
using System.Collections.Generic;
using UnityEngine;

public class NPCConditionalDialogue : MonoBehaviour
{
    [Header("Interaction")]
    public KeyCode interactKey = KeyCode.E;
    public bool playerMustBeInRange = true;

    [Header("Condition")]
    public string requiredMaskId;
    public bool silentWhenLocked = true;

    [Header("Dialogue (Locked)")]
    [TextArea(2, 4)]
    public List<string> lockedLines = new List<string>() { "……（他没有回应你）" };

    [Header("Dialogue (Unlocked)")]
    [TextArea(2, 4)]
    public List<string> mainLines = new List<string>();

    [Header("Reward After Dialogue (Optional)")]
    public bool rewardMaskAfterTalk = false;
    public string rewardMaskId;
    public bool triggerOnce = true;

    [Header("Debug")]
    public bool enableDebug = true;

    private bool playerInRange = false;
    private bool hasTriggered = false;

    void Start()
    {
        if (enableDebug)
        {
            Debug.Log($"[NPC:{gameObject.name}] 初始化完成");
            Debug.Log($"[NPC:{gameObject.name}] 交互键: {interactKey}");
            Debug.Log($"[NPC:{gameObject.name}] 需要在范围内: {playerMustBeInRange}");
            Debug.Log($"[NPC:{gameObject.name}] mainLines 数量: {mainLines.Count}");
            
            // 检查 Collider
            Collider2D col = GetComponent<Collider2D>();
            if (col == null)
            {
                Debug.LogError($"[NPC:{gameObject.name}] ❌ 没有 Collider2D！无法检测玩家进入范围");
            }
            else if (!col.isTrigger)
            {
                Debug.LogError($"[NPC:{gameObject.name}] ❌ Collider2D 没有勾选 Is Trigger！");
            }
            else
            {
                Debug.Log($"[NPC:{gameObject.name}] ✓ Collider2D 设置正确");
            }
            
            // 检查 DialogueManager
            if (DialogueManager.Instance == null)
            {
                Debug.LogWarning($"[NPC:{gameObject.name}] ⚠ DialogueManager.Instance 暂时为 null（可能还没初始化）");
            }
        }
    }

    void Update()
    {
        // 始终检测按键（即使不在范围内也报告）
        if (Input.GetKeyDown(interactKey))
        {
            if (enableDebug)
            {
                Debug.Log($"═══════════════════════════════════════");
                Debug.Log($"[NPC:{gameObject.name}] 按下了 {interactKey}");
                Debug.Log($"[NPC:{gameObject.name}] 状态检查:");
                Debug.Log($"  - playerInRange: {playerInRange}");
                Debug.Log($"  - playerMustBeInRange: {playerMustBeInRange}");
                Debug.Log($"  - hasTriggered: {hasTriggered}");
                Debug.Log($"  - triggerOnce: {triggerOnce}");
                Debug.Log($"  - DialogueManager.Instance: {(DialogueManager.Instance != null ? "存在" : "NULL")}");
                if (DialogueManager.Instance != null)
                {
                    Debug.Log($"  - IsTalking: {DialogueManager.Instance.IsTalking()}");
                }
            }

            // 检查各种条件
            if (triggerOnce && hasTriggered)
            {
                if (enableDebug) Debug.Log($"[NPC:{gameObject.name}] ⛔ 跳过：已触发过（triggerOnce=true）");
                return;
            }

            if (playerMustBeInRange && !playerInRange)
            {
                if (enableDebug) Debug.Log($"[NPC:{gameObject.name}] ⛔ 跳过：玩家不在范围内");
                return;
            }

            if (DialogueManager.Instance == null)
            {
                if (enableDebug) Debug.LogError($"[NPC:{gameObject.name}] ❌ DialogueManager.Instance 为 null！");
                return;
            }

            if (DialogueManager.Instance.IsTalking())
            {
                if (enableDebug) Debug.Log($"[NPC:{gameObject.name}] ⛔ 跳过：正在对话中");
                return;
            }

            if (enableDebug) Debug.Log($"[NPC:{gameObject.name}] ✓ 所有条件通过，开始对话");
            TryTalk();
        }
    }

    private void TryTalk()
    {
        if (enableDebug) Debug.Log($"[NPC:{gameObject.name}] TryTalk 执行中...");
        
        bool unlocked = true;

        if (!string.IsNullOrEmpty(requiredMaskId))
        {
            if (enableDebug) Debug.Log($"[NPC:{gameObject.name}] 检查面具: {requiredMaskId}");
            
            if (PlayerMaskManager.Instance == null)
            {
                if (enableDebug) Debug.LogWarning($"[NPC:{gameObject.name}] PlayerMaskManager.Instance 为 null");
                unlocked = false;
            }
            else
            {
                unlocked = PlayerMaskManager.Instance.HasMask(requiredMaskId);
                if (enableDebug) Debug.Log($"[NPC:{gameObject.name}] 玩家拥有面具 '{requiredMaskId}': {unlocked}");
            }
        }

        if (!unlocked)
        {
            if (silentWhenLocked)
            {
                if (enableDebug) Debug.Log($"[NPC:{gameObject.name}] ⛔ 未解锁 + 静默模式，不说话");
                return;
            }

            if (enableDebug) Debug.Log($"[NPC:{gameObject.name}] 播放锁定对白 ({lockedLines.Count} 句)");
            DialogueManager.Instance.StartDialogue(lockedLines);
            return;
        }

        if (mainLines.Count == 0)
        {
            if (enableDebug) Debug.LogWarning($"[NPC:{gameObject.name}] ❌ mainLines 为空！没有对话内容");
            return;
        }
        
        if (enableDebug) Debug.Log($"[NPC:{gameObject.name}] ▶ 播放主对白 ({mainLines.Count} 句)");
        DialogueManager.Instance.StartDialogue(mainLines, OnDialogueFinished);
        hasTriggered = true;
    }

    private void OnDialogueFinished()
    {
        if (enableDebug) Debug.Log($"[NPC:{gameObject.name}] 对话结束回调");
        
        if (rewardMaskAfterTalk && !string.IsNullOrEmpty(rewardMaskId))
        {
            if (PlayerMaskManager.Instance != null)
            {
                PlayerMaskManager.Instance.UnlockMask(rewardMaskId);
                if (enableDebug) Debug.Log($"[NPC:{gameObject.name}] 🎁 奖励面具: {rewardMaskId}");
            }
            else
            {
                Debug.LogError($"[NPC:{gameObject.name}] ❌ 无法奖励面具，PlayerMaskManager 为 null");
            }
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (enableDebug) Debug.Log($"[NPC:{gameObject.name}] OnTriggerEnter2D: {other.gameObject.name} (Tag: {other.tag})");
        
        if (other.CompareTag("Player"))
        {
            playerInRange = true;
            if (enableDebug) Debug.Log($"[NPC:{gameObject.name}] ✓ 玩家进入范围");
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = false;
            if (enableDebug) Debug.Log($"[NPC:{gameObject.name}] 玩家离开范围");
        }
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NPCConditionalDialogue : MonoBehaviour
{
    [Header("=== 交互方式 ===")]
    public InteractionMode interactionMode = InteractionMode.KeyPress;
    public KeyCode interactKey = KeyCode.E;

    public enum InteractionMode
    {
        KeyPress,
        AutoOnEnter
    }

    [Header("=== 玩家控制 ===")]
    public bool freezePlayerDuringDialogue = true;

    [Header("=== 条件 ===")]
    public string requiredMaskId;
    public bool silentWhenLocked = true;

    [Header("=== 对话内容 (锁定时) ===")]
    [TextArea(2, 4)]
    public List<string> lockedLines = new List<string>() { "……（他没有回应你）" };

    [Header("=== 对话内容 (首次) ===")]
    [TextArea(2, 4)]
    public List<string> mainLines = new List<string>();

    [Header("=== 对话内容 (后续) ===")]
    [TextArea(2, 4)]
    public List<string> afterDialogueLines = new List<string>();

    [Header("=== 奖励 (可选) ===")]
    public bool rewardMaskAfterTalk = false;
    public string rewardMaskId;

    [Header("=== 对话后立即消失 ===")]
    [Tooltip("对话完成后立即消失（渐变透明）")]
    public bool disappearAfterDialogue = false;
    
    [Tooltip("消失时播放的音效")]
    public AudioClip disappearSound;
    
    [Tooltip("消失渐变时长")]
    public float disappearDuration = 1f;

    [Header("=== 离开场景后永久消失 ===")]
    [Tooltip("对话完成后，离开场景再回来时消失")]
    public bool disappearAfterLeaveScene = false;
    
    [Tooltip("此NPC的唯一ID")]
    public string npcUniqueId;

    [Header("=== Debug ===")]
    public bool enableDebug = true;

    private bool playerInRange = false;
    private bool hasCompletedFirstDialogue = false;
    private bool isDisappearing = false;
    private GameObject playerObject;
    private SpriteRenderer spriteRenderer;
    private AudioSource audioSource;

    // 记录永久消失的NPC
    private static HashSet<string> disappearedNPCs = new HashSet<string>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        // 不清除，保持游戏会话内状态
    }

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        audioSource = GetComponent<AudioSource>();
        
        if (audioSource == null && disappearSound != null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }

        // 检查是否已永久消失
        if (!string.IsNullOrEmpty(npcUniqueId) && disappearedNPCs.Contains(npcUniqueId))
        {
            if (enableDebug) Debug.Log($"[NPC:{gameObject.name}] 已永久消失，禁用");
            DisableNPCImmediate();
            return;
        }
    }

    void Update()
    {
        if (isDisappearing) return;
        
        if (DialogueManager.Instance != null && DialogueManager.Instance.IsTalking())
            return;

        if (StoryRevealManager.Instance != null && StoryRevealManager.Instance.IsRevealing())
            return;

        if (interactionMode == InteractionMode.KeyPress)
        {
            if (Input.GetKeyDown(interactKey))
            {
                if (!playerInRange) return;
                TryTalk();
            }
        }
    }

    private void TryTalk()
    {
        if (DialogueManager.Instance == null) return;

        bool unlocked = true;

        if (!string.IsNullOrEmpty(requiredMaskId))
        {
            if (PlayerMaskManager.Instance == null)
                unlocked = false;
            else
                unlocked = PlayerMaskManager.Instance.HasMask(requiredMaskId);
        }

        // 未解锁
        if (!unlocked)
        {
            if (silentWhenLocked) return;

            if (freezePlayerDuringDialogue) FreezePlayer(true);
            
            DialogueManager.Instance.StartDialogue(lockedLines, () => {
                if (freezePlayerDuringDialogue) FreezePlayer(false);
            });
            return;
        }

        // 已完成首次对话
        if (hasCompletedFirstDialogue)
        {
            if (afterDialogueLines.Count == 0) return;

            if (freezePlayerDuringDialogue) FreezePlayer(true);

            DialogueManager.Instance.StartDialogue(afterDialogueLines, () => {
                if (freezePlayerDuringDialogue) FreezePlayer(false);
            });
            return;
        }

        // 首次对话
        if (mainLines.Count == 0) return;

        if (freezePlayerDuringDialogue) FreezePlayer(true);

        DialogueManager.Instance.StartDialogue(mainLines, OnFirstDialogueFinished);
    }

    private void OnFirstDialogueFinished()
    {
        if (enableDebug) Debug.Log($"[NPC:{gameObject.name}] 首次对话结束");
        
        hasCompletedFirstDialogue = true;
        
        if (freezePlayerDuringDialogue) FreezePlayer(false);

        // 奖励面具
        if (rewardMaskAfterTalk && !string.IsNullOrEmpty(rewardMaskId))
        {
            if (PlayerMaskManager.Instance != null)
            {
                PlayerMaskManager.Instance.UnlockMask(rewardMaskId);
            }
        }

        // 标记为永久消失（离开场景后生效）
        if (disappearAfterLeaveScene && !string.IsNullOrEmpty(npcUniqueId))
        {
            disappearedNPCs.Add(npcUniqueId);
            if (enableDebug) Debug.Log($"[NPC:{gameObject.name}] 已标记为永久消失");
        }

        // 触发故事揭示
        var storyTrigger = GetComponent<NPCStoryRevealTrigger>();
        if (storyTrigger != null)
        {
            storyTrigger.OnDialogueComplete();
        }

        // 对话后立即消失
        if (disappearAfterDialogue)
        {
            // 也标记为永久消失
            if (!string.IsNullOrEmpty(npcUniqueId))
            {
                disappearedNPCs.Add(npcUniqueId);
            }
            StartCoroutine(DisappearWithFade());
        }
    }

    private IEnumerator DisappearWithFade()
    {
        isDisappearing = true;
        
        if (enableDebug) Debug.Log($"[NPC:{gameObject.name}] 开始消失");

        // 立即禁用 Collider
        var colliders = GetComponents<Collider2D>();
        foreach (var col in colliders)
        {
            col.enabled = false;
        }

        // 播放音效
        if (disappearSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(disappearSound);
        }

        // 渐变消失
        if (spriteRenderer != null)
        {
            Color startColor = spriteRenderer.color;
            float elapsed = 0f;

            while (elapsed < disappearDuration)
            {
                elapsed += Time.deltaTime;
                float alpha = Mathf.Lerp(1f, 0f, elapsed / disappearDuration);
                spriteRenderer.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
                yield return null;
            }

            spriteRenderer.color = new Color(startColor.r, startColor.g, startColor.b, 0f);
        }

        // 等待音效播放完毕
        if (disappearSound != null)
        {
            yield return new WaitForSeconds(Mathf.Max(0, disappearSound.length - disappearDuration));
        }

        if (enableDebug) Debug.Log($"[NPC:{gameObject.name}] 消失完成");
        
        gameObject.SetActive(false);
    }

    private void DisableNPCImmediate()
    {
        // 直接禁用整个 GameObject
        gameObject.SetActive(false);
    }

    private void FreezePlayer(bool freeze)
    {
        if (playerObject == null)
        {
            playerObject = GameObject.FindGameObjectWithTag("Player");
        }

        if (playerObject != null)
        {
            var moveScript = playerObject.GetComponent<PlayerMove2D>();
            if (moveScript != null)
            {
                moveScript.enabled = !freeze;
            }

            if (freeze)
            {
                var rb = playerObject.GetComponent<Rigidbody2D>();
                if (rb != null)
                {
                    rb.linearVelocity = Vector2.zero;
                }
            }
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        
        playerInRange = true;
        playerObject = other.gameObject;

        if (interactionMode == InteractionMode.AutoOnEnter)
        {
            if (DialogueManager.Instance != null && DialogueManager.Instance.IsTalking())
                return;
            TryTalk();
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = false;
        }
    }

    /// <summary>
    /// 重置所有永久消失的NPC
    /// </summary>
    public static void ResetAllDisappearedNPCs()
    {
        disappearedNPCs.Clear();
        Debug.Log("[NPC] 所有永久消失的NPC已重置");
    }
}

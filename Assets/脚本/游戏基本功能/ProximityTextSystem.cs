using System;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

/// <summary>
/// 接近显示文字系统
/// 当玩家靠近 Collider 时，对应的文字渐渐显示
/// 离开时渐渐消失
/// 支持面具要求
/// </summary>
public class ProximityTextSystem : MonoBehaviour
{
    [Serializable]
    public class ProximityPair
    {
        [Tooltip("触发区域 Collider")]
        public Collider2D triggerCollider;
        
        [Tooltip("要显示的文字（TextMeshProUGUI）")]
        public TextMeshProUGUI text;
        
        [Header("面具要求（可选）")]
        [Tooltip("是否需要特定面具才显示")]
        public bool requireMask = false;
        
        [Tooltip("需要的面具ID")]
        public string requiredMaskId;
        
        [HideInInspector]
        public bool playerInside = false;
        
        [HideInInspector]
        public float currentAlpha = 0f;
    }

    [Header("=== 配对设置 ===")]
    [Tooltip("Collider 和 Text 的配对列表")]
    public List<ProximityPair> pairs = new List<ProximityPair>();

    [Header("=== 动画设置 ===")]
    [Tooltip("淡入速度")]
    public float fadeInSpeed = 2f;
    
    [Tooltip("淡出速度")]
    public float fadeOutSpeed = 2f;

    [Header("=== 玩家设置 ===")]
    [Tooltip("玩家标签")]
    public string playerTag = "Player";

    private Transform playerTransform;

    private void Start()
    {
        // 初始化所有文字为透明
        foreach (var pair in pairs)
        {
            if (pair.text != null)
            {
                SetTextAlpha(pair.text, 0f);
                pair.currentAlpha = 0f;
            }
        }

        // 查找玩家
        GameObject player = GameObject.FindGameObjectWithTag(playerTag);
        if (player != null)
        {
            playerTransform = player.transform;
        }
    }

    private void Update()
    {
        if (playerTransform == null)
        {
            // 尝试重新查找玩家
            GameObject player = GameObject.FindGameObjectWithTag(playerTag);
            if (player != null)
            {
                playerTransform = player.transform;
            }
            else
            {
                return;
            }
        }

        // 检测每个配对
        foreach (var pair in pairs)
        {
            if (pair.triggerCollider == null || pair.text == null) continue;

            // 检测玩家是否在 Collider 内
            bool isInside = IsPlayerInsideCollider(pair.triggerCollider);
            
            // 检查面具要求
            bool shouldShow = isInside && CheckMaskRequirement(pair);

            // 更新透明度
            if (shouldShow)
            {
                // 淡入
                pair.currentAlpha = Mathf.MoveTowards(pair.currentAlpha, 1f, fadeInSpeed * Time.deltaTime);
            }
            else
            {
                // 淡出
                pair.currentAlpha = Mathf.MoveTowards(pair.currentAlpha, 0f, fadeOutSpeed * Time.deltaTime);
            }

            SetTextAlpha(pair.text, pair.currentAlpha);
            pair.playerInside = isInside;
        }
    }

    /// <summary>
    /// 检查面具要求是否满足
    /// </summary>
    private bool CheckMaskRequirement(ProximityPair pair)
    {
        // 不需要面具
        if (!pair.requireMask || string.IsNullOrEmpty(pair.requiredMaskId))
        {
            return true;
        }

        // 检查 PlayerMaskManager
        if (PlayerMaskManager.Instance == null)
        {
            return false;
        }

        // 检查当前佩戴的面具是否匹配
        return PlayerMaskManager.Instance.currentMaskId == pair.requiredMaskId;
    }

    private bool IsPlayerInsideCollider(Collider2D collider)
    {
        if (collider == null || playerTransform == null) return false;

        // 使用 OverlapPoint 检测玩家中心点是否在 Collider 内
        return collider.OverlapPoint(playerTransform.position);
    }

    private void SetTextAlpha(TextMeshProUGUI text, float alpha)
    {
        if (text == null) return;
        
        Color color = text.color;
        color.a = alpha;
        text.color = color;
    }
}

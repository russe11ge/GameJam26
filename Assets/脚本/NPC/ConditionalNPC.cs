using UnityEngine;

/// <summary>
/// 条件NPC - 根据玩家值显示/隐藏
/// </summary>
public class ConditionalNPC : MonoBehaviour
{
    [Header("=== 条件设置 ===")]
    [Tooltip("要检测的值名称")]
    public string checkValueKey = "value1";
    
    [Tooltip("需要的值（等于此值时NPC出现）")]
    public int requiredValue = 1;

    [Header("=== 显示设置 ===")]
    [Tooltip("要控制显示/隐藏的物体（留空则控制自己）")]
    public GameObject targetObject;
    
    [Tooltip("要控制的 Collider2D（留空则自动获取）")]
    public Collider2D targetCollider;

    [Header("=== Debug ===")]
    public bool enableDebug = true;

    private bool isShowing = false;
    private SpriteRenderer spriteRenderer;

    private void Start()
    {
        // 自动获取组件
        if (targetObject == null)
        {
            targetObject = gameObject;
        }
        
        if (targetCollider == null)
        {
            targetCollider = GetComponent<Collider2D>();
        }

        spriteRenderer = targetObject.GetComponent<SpriteRenderer>();

        // 初始隐藏
        SetVisible(false);
        
        if (enableDebug)
        {
            Debug.Log($"[ConditionalNPC:{gameObject.name}] 初始化，检测值: '{checkValueKey}' == {requiredValue}");
        }
    }

    private void Update()
    {
        // 检测玩家值
        if (GameManager.Instance == null) return;

        int currentValue = GameManager.Instance.GetPlayerValue(checkValueKey);
        bool shouldShow = (currentValue == requiredValue);

        // 状态改变时更新
        if (shouldShow != isShowing)
        {
            SetVisible(shouldShow);
            
            if (enableDebug)
            {
                Debug.Log($"[ConditionalNPC:{gameObject.name}] 值 '{checkValueKey}' = {currentValue}，{(shouldShow ? "显示" : "隐藏")} NPC");
            }
        }
    }

    private void SetVisible(bool visible)
    {
        isShowing = visible;

        // 控制渲染（显示/隐藏）
        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = visible;
        }

        // 控制 Collider（碰撞）
        if (targetCollider != null)
        {
            targetCollider.enabled = visible;
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = isShowing ? Color.green : Color.red;
        Gizmos.DrawWireSphere(transform.position, 0.3f);
    }
}

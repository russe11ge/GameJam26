using UnityEngine;

/// <summary>
/// 简单的 2D 角色动画（不使用 Animator）
/// 通过代码切换精灵图片实现动画效果
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class SimplePlayerAnimation : MonoBehaviour
{
    [Header("=== 精灵设置 ===")]
    [Tooltip("静止状态的精灵（只有一张）")]
    public Sprite idleSprite;
    
    [Tooltip("走动状态的精灵（多张，按顺序播放）")]
    public Sprite[] walkSprites;

    [Header("=== 动画设置 ===")]
    [Tooltip("走动动画帧率（每秒切换几帧）")]
    public float walkFrameRate = 8f;

    [Header("=== 移动检测 ===")]
    [Tooltip("移动速度阈值（低于此值视为静止）")]
    public float moveThreshold = 0.1f;
    
    [Tooltip("使用 Rigidbody2D 检测移动（否则使用位置变化）")]
    public bool useRigidbody = true;

    [Header("=== 翻转设置 ===")]
    [Tooltip("反转翻转方向（如果角色方向不对，勾选此项）")]
    public bool invertFlip = false;

    // 私有变量
    private SpriteRenderer spriteRenderer;
    private Rigidbody2D rb;
    private int currentFrame = 0;
    private float frameTimer = 0f;
    private bool isWalking = false;
    private Vector3 lastPosition;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();
        lastPosition = transform.position;

        // 设置初始精灵
        if (idleSprite != null)
        {
            spriteRenderer.sprite = idleSprite;
        }
    }

    private void Update()
    {
        // 检测移动
        float horizontalVelocity = 0f;

        if (useRigidbody && rb != null)
        {
            horizontalVelocity = rb.linearVelocity.x;
        }
        else
        {
            // 使用位置变化检测
            horizontalVelocity = (transform.position.x - lastPosition.x) / Time.deltaTime;
            lastPosition = transform.position;
        }

        // 判断是否在走动
        bool wasWalking = isWalking;
        isWalking = Mathf.Abs(horizontalVelocity) > moveThreshold;

        // 处理翻转（向右移动朝右，向左移动朝左）
        if (horizontalVelocity > moveThreshold)
        {
            spriteRenderer.flipX = invertFlip; // 向右走
        }
        else if (horizontalVelocity < -moveThreshold)
        {
            spriteRenderer.flipX = !invertFlip; // 向左走
        }

        // 更新动画
        if (isWalking)
        {
            UpdateWalkAnimation();
        }
        else
        {
            // 切换到静止状态
            if (wasWalking || spriteRenderer.sprite != idleSprite)
            {
                SetIdleSprite();
            }
        }
    }

    /// <summary>
    /// 更新走动动画
    /// </summary>
    private void UpdateWalkAnimation()
    {
        if (walkSprites == null || walkSprites.Length == 0) return;

        frameTimer += Time.deltaTime;
        float frameDuration = 1f / walkFrameRate;

        if (frameTimer >= frameDuration)
        {
            frameTimer -= frameDuration;
            currentFrame = (currentFrame + 1) % walkSprites.Length;
            spriteRenderer.sprite = walkSprites[currentFrame];
        }
    }

    /// <summary>
    /// 设置为静止精灵
    /// </summary>
    private void SetIdleSprite()
    {
        if (idleSprite != null)
        {
            spriteRenderer.sprite = idleSprite;
        }
        currentFrame = 0;
        frameTimer = 0f;
    }

    /// <summary>
    /// 外部调用 - 强制设置朝向
    /// </summary>
    public void SetDirection(bool faceRight)
    {
        spriteRenderer.flipX = faceRight ? invertFlip : !invertFlip;
    }

    /// <summary>
    /// 外部调用 - 强制播放走动动画
    /// </summary>
    public void PlayWalk()
    {
        isWalking = true;
    }

    /// <summary>
    /// 外部调用 - 强制播放静止动画
    /// </summary>
    public void PlayIdle()
    {
        isWalking = false;
        SetIdleSprite();
    }
}

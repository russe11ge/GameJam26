using UnityEngine;

public class PlayerMove2D : MonoBehaviour
{
    public float moveSpeed = 5f;

    private Rigidbody2D rb;
    private float moveInput;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        // 获取左右输入（A/D 或 ← →）
        moveInput = Input.GetAxisRaw("Horizontal");
    }

    void FixedUpdate()
    {
        // 用 Rigidbody 移动角色
        rb.linearVelocity = new Vector2(moveInput * moveSpeed, rb.linearVelocity.y);
        if (DialogueManager.Instance != null && DialogueManager.Instance.IsTalking())
            return;
    }
    
}
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerCrtl : MonoBehaviour
{
    Rigidbody2D rb;
    CircleCollider2D coll;
    public float speed;
    public float smooth;
    public float jumpPower;
    

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        coll = GetComponent<CircleCollider2D>();
    }

    void Update()
    {
        // 左右の矢印キーやA/Dキーの状態を -1.0 〜 1.0 の数値で取得
        float x = 0;
        if (Keyboard.current != null)//現在のキーボードが使用可能かどうか（なくてもいい）
        {
            // A/Dキーや左右矢印キーの押し込み具合を判定
            float left = Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed ? 1f : 0f;
            float right = Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed ? 1f : 0f;
            x = right - left;
        }
        rb.AddForceX((x * speed - rb.linearVelocityX) * smooth * Time.deltaTime);

        RaycastHit2D groundHit = Physics2D.Raycast(transform.position + (Vector3)coll.offset, Vector2.down, coll.radius + 0.1f, LayerMask.GetMask("Ground"));

        if(groundHit)
        {
            if(Keyboard.current.spaceKey.wasPressedThisFrame) 
            {
                rb.linearVelocityY = 0;
                rb.AddForce(Vector2.up * jumpPower, ForceMode2D.Impulse);
            }
        }
        
    }
}
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerCrtl : MonoBehaviour
{
    Rigidbody2D rb;
    CircleCollider2D coll;
    bool isJump = false;
    bool isSlope = false;
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

        if(x > 0)
        {
            transform.rotation = Quaternion.Euler(0, 0, 0);
        }
        if(x < 0)
        {
            transform.rotation = Quaternion.Euler(0, 180, 0);
        }

        //RaycastHit2D groundHit = Physics2D.Raycast(transform.position + (Vector3)coll.offset, Vector2.down, coll.radius + 0.1f, LayerMask.GetMask("Ground"));
        RaycastHit2D slopeHitForward = Physics2D.Raycast(transform.position + (Vector3)coll.offset + (transform.right * coll.radius / 2), Vector2.down, coll.radius + 0.1f, LayerMask.GetMask("Ground"));
        RaycastHit2D slopeHitBack = Physics2D.Raycast(transform.position + (Vector3)coll.offset - (transform.right * coll.radius / 2), Vector2.down, coll.radius + 0.1f, LayerMask.GetMask("Ground"));

        Debug.Log((bool)slopeHitForward + "," + (bool)slopeHitBack/* + "," + (bool)groundHit*/);

        if(slopeHitForward || slopeHitBack)
        {
            if(rb.linearVelocityY < 0)
            {
                isJump = false;
            }
            if(Keyboard.current.spaceKey.wasPressedThisFrame) 
            {
                isJump = true;
                rb.linearVelocityY = 0;
                rb.AddForce(Vector2.up * jumpPower, ForceMode2D.Impulse);
            }
        }

        if(slopeHitForward ^ slopeHitBack)
        {
            isSlope = true;
            if(x == 0 && !isJump)
            {
                rb.constraints = RigidbodyConstraints2D.FreezeRotation | RigidbodyConstraints2D.FreezePositionY;
            }
            else
            {
                rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            }
        }
        else
        {
            if(isSlope && !isJump)
            {
                rb.linearVelocityY = -3;
            }
            isSlope = false;
        }
        
    }
}
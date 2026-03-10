using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerCrtl : MonoBehaviour
{
    Rigidbody2D rb;
    CircleCollider2D coll;
    public float jumpPower;
    

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        coll = GetComponent<CircleCollider2D>();
    }

    void Update()
    {
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
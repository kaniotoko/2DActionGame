using UnityEngine;
using UnityEngine.InputSystem;

public class BearCtrl : MonoBehaviour
{
    Rigidbody2D rb;
    CapsuleCollider2D coll;
    float time;
    float targetPosX;
    public float speed;
    

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        coll = GetComponent<CapsuleCollider2D>();
    }

    void Update()
    {
        Vector3 origin = transform.position + (Vector3)coll.offset;
        float rayDist = (coll.size.y / 2) + 1.5f;

        RaycastHit2D slopeHitForward = Physics2D.Raycast(origin + (transform.right * coll.size.x / 4), Vector2.down, rayDist, LayerMask.GetMask("Ground"));
        RaycastHit2D slopeHitBack = Physics2D.Raycast(origin - (transform.right * coll.size.x / 4), Vector2.down, rayDist, LayerMask.GetMask("Ground"));

        if (slopeHitForward || slopeHitBack)
        {
            if (rb.linearVelocityY <= 0)
            {
                rb.constraints = RigidbodyConstraints2D.FreezeRotation;
                
                rb.linearVelocityX = transform.right.x * speed;
            }
        }
        
        if (!slopeHitForward)
        {
            if (Mathf.Approximately(transform.rotation.eulerAngles.y, 0))
            {
                transform.rotation = Quaternion.Euler(0, 180, 0);
            }
            else
            {
                transform.rotation = Quaternion.Euler(0, 0, 0);
            }
            
            rb.linearVelocityX = 0;
        }

        if (transform.position.y < -7.5f)
        {
            Destroy(gameObject);
        }
    }
}
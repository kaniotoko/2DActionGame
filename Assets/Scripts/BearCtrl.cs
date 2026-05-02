using UnityEngine;

public class BearCtrl : MonoBehaviour
{
    Rigidbody2D rb;
    CapsuleCollider2D coll;
    public float speed;
    public float wallDist = 0.5f; // 壁を検知する距離

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        coll = GetComponent<CapsuleCollider2D>();
    }

    void Update()
    {
        Vector3 origin = transform.position + (Vector3)coll.offset;

        // --- 崖の判定 ---
        float rayDist = (coll.size.y / 2) + 1.5f;
        RaycastHit2D slopeHitForward = Physics2D.Raycast(origin + (transform.right * coll.size.x / 4), Vector2.down, rayDist, LayerMask.GetMask("Ground"));
        RaycastHit2D slopeHitBack = Physics2D.Raycast(origin - (transform.right * coll.size.x / 4), Vector2.down, rayDist, LayerMask.GetMask("Ground"));

        // --- 壁の判定 ---
        RaycastHit2D wallHit = Physics2D.Raycast(origin, transform.right, wallDist + (coll.size.x / 2), LayerMask.GetMask("Ground"));
        Debug.DrawRay(origin, transform.right * (wallDist + (coll.size.x / 2)), Color.red);

        if (slopeHitForward || slopeHitBack)
        {
            if (rb.linearVelocity.y <= 0f)
            {
                rb.constraints = RigidbodyConstraints2D.FreezeRotation;
                rb.linearVelocity = new Vector2(transform.right.x * speed, rb.linearVelocity.y);
            }
        }

        if (!slopeHitForward || wallHit)
        {
            Flip();
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        }

        if (transform.position.y < -11f)
        {
            Destroy(gameObject);
        }
    }

    void Flip()
    {
        if (Mathf.Approximately(transform.rotation.eulerAngles.y, 0f))
        {
            transform.rotation = Quaternion.Euler(0f, 180f, 0f);
        }
        else
        {
            transform.rotation = Quaternion.Euler(0f, 0f, 0f);
        }
    }
}
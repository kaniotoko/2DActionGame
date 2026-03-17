using UnityEngine;
using UnityEngine.InputSystem;

public class EnemyCrtl : MonoBehaviour
{
    public Transform player;
    Rigidbody2D rb;
    CircleCollider2D coll;
    Animator anim;
    bool isJump = false;
    bool isSlope = false;
    float time;
    float targetPosX;
    public float smooth;
    public float jumpPower;
    public float jumpTime; //何秒後にジャンプするか
    public float moveDistance;
    

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        coll = GetComponent<CircleCollider2D>();
        anim = GetComponent<Animator>();
        player = GameObject.Find("Player").transform; //シーンに出現された時Playerというオブジェクトを代入する
    }

    void Update()
    {
        //RaycastHit2D groundHit = Physics2D.Raycast(transform.position + (Vector3)coll.offset, Vector2.down, coll.radius + 0.1f, LayerMask.GetMask("Ground"));
        RaycastHit2D slopeHitForward = Physics2D.Raycast(transform.position + (Vector3)coll.offset + (transform.right * coll.radius / 2), Vector2.down, coll.radius + 0.1f, LayerMask.GetMask("Ground"));
        RaycastHit2D slopeHitBack = Physics2D.Raycast(transform.position + (Vector3)coll.offset - (transform.right * coll.radius / 2), Vector2.down, coll.radius + 0.1f, LayerMask.GetMask("Ground"));

        if(rb.linearVelocityY <= 0)
        {
            anim.SetBool("isFall", true);
        }

        if(slopeHitForward || slopeHitBack)
        {
            anim.SetBool("isFall", false);

            if(Vector3.Distance(player.position, transform.position) < moveDistance)
            {
                time += Time.deltaTime;
            }

            if(rb.linearVelocityY <= 0)
            {
                isJump = false;
                anim.SetBool("isJump", false);
                rb.constraints = RigidbodyConstraints2D.FreezeRotation | RigidbodyConstraints2D.FreezePosition;
            }
        }
        
        if(time >= jumpTime)
        {
            isJump = true;
            anim.SetBool("isJump", true);
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            rb.linearVelocityY = 0;
            rb.AddForce(Vector2.up * jumpPower, ForceMode2D.Impulse);

            if(player.position.x > transform.position.x)
            {
                transform.rotation = Quaternion.Euler(0, 180, 0);
            }
            if(player.position.x < transform.position.x)
            {
                transform.rotation = Quaternion.Euler(0, 0, 0);
            }
            targetPosX = player.position.x;

            time = 0;
        }
        if(isJump)
        {
            rb.AddForceX((targetPosX - transform.position.x) * smooth * Time.deltaTime);
        }

        if(transform.position.y < -7.5f)
        {
            Destroy(gameObject);
        }
        
    }
}
using System.Collections;
using UnityEngine;

public class BatCtrl : MonoBehaviour
{
    Transform player;
    Rigidbody2D rb;
    Collider2D col;
    Animator anim;

    public float detectDistance = 12f;// プレイヤーを検知する距離
    public float moveSpeed = 3f;// プレイヤーに向かって飛ぶ速さ
    public float dropSpeed = 1.5f;// 落下する速さ
    public float dropDistance = 4f;// 落下する距離

    bool Attacking = false;
    bool Death = false;
    bool dropping = false;
    float dropStartY;// 落下開始時のY座標

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        anim = GetComponent<Animator>();
        rb.gravityScale = 0;
        player = GameObject.Find("Player").transform;
    }

    void Update()
    {
        if (Death) return;

        float dist = Vector2.Distance(transform.position, player.position);

        if (!Attacking && dist <= detectDistance)
        {
            anim.SetBool("isAttacking", true);
            Attacking = true;
            dropping = true;
            dropStartY = transform.position.y;
        }

        if (Attacking)
        {
            if (dropping)
            {
                rb.linearVelocity = Vector2.down * dropSpeed;
                if (transform.position.y <= dropStartY - dropDistance)
                {
                    dropping = false;
                }
            }
            else
            {
                // normalizedで長さ1の方向ベクトルにする
                Vector2 dir = ((Vector2)player.position - (Vector2)transform.position).normalized;
                rb.linearVelocity = dir * moveSpeed;
            }
        }

        FlipSprite();
    }

    void FlipSprite()
    {
        if (player.position.x > transform.position.x)
            transform.rotation = Quaternion.Euler(0, 0, 0);
        else
            transform.rotation = Quaternion.Euler(0, 180, 0);
    }

    // Groundレイヤーのブロックに当たったら死亡処理を開始する
    void OnCollisionEnter2D(Collision2D collision)
    {
        if (Death) return;
        if (collision.gameObject.layer == LayerMask.NameToLayer("Ground"))
            StartCoroutine(DieRoutine());
    }

    // PlayerCtrlからスタンプ判定時に呼ばれる
    public void Stomped()
    {
        if (Death) return;
        StartCoroutine(DieRoutine());
    }

    // 1秒間コライダーを無効にして静止した後に消える
    IEnumerator DieRoutine()
    {
        Death = true;
        rb.linearVelocity = Vector2.zero;
        rb.gravityScale = 0;
        col.enabled = false;// コライダーを無効にしてプレイヤーが踏んだ後にもう一度当たるのを防ぐ
        yield return new WaitForSeconds(1f);
        Destroy(gameObject);
    }
}

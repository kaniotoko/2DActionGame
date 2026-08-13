using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerCrtl : MonoBehaviour
{
    public MainManager mainManager;
    Rigidbody2D rb;
    CircleCollider2D coll;
    Animator anim;
    bool isJump = false;
    bool wasFreezeY = false;
    bool isDead = false;
    public float speed;
    public float smooth;
    public float jumpPower;
    public AudioSource jumpSE;
    [HideInInspector] public float platformVelX = 0f;
    

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        coll = GetComponent<CircleCollider2D>();
        anim = GetComponent<Animator>();
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
        rb.AddForceX((x * speed + platformVelX - rb.linearVelocityX) * smooth * Time.deltaTime);

        anim.SetFloat("Speed", Mathf.Abs(x));

        if(x > 0)
        {
            transform.rotation = Quaternion.Euler(0, 0, 0);
        }
        if(x < 0)
        {
            transform.rotation = Quaternion.Euler(0, 180, 0);
        }

        //RaycastHit2D groundHit = Physics2D.Raycast(transform.position + (Vector3)coll.offset, Vector2.down, coll.radius + 0.1f, LayerMask.GetMask("Ground"));
        RaycastHit2D slopeHitForward = Physics2D.Raycast(transform.position + (Vector3)coll.offset + (transform.right * coll.radius/2), Vector2.down, coll.radius + 0.1f, LayerMask.GetMask("Ground"));
        RaycastHit2D slopeHitBack = Physics2D.Raycast(transform.position + (Vector3)coll.offset - (transform.right * coll.radius/2), Vector2.down, coll.radius + 0.1f, LayerMask.GetMask("Ground"));

        //左右ギリギリの位置でも接地判定ができるようにするため、Raycastを2本飛ばす
        RaycastHit2D edgeHitForward = Physics2D.Raycast(transform.position + (Vector3)coll.offset + (transform.right * coll.radius), Vector2.down, coll.radius + 0.1f, LayerMask.GetMask("Ground"));
        RaycastHit2D edgeHitBack = Physics2D.Raycast(transform.position + (Vector3)coll.offset - (transform.right * coll.radius), Vector2.down, coll.radius + 0.1f, LayerMask.GetMask("Ground"));

        //Debug.Log((bool)slopeHitForward + "," + (bool)slopeHitBack/* + "," + (bool)groundHit*/);

        //片方だけヒット＝足元の地面が途切れている状態。斜面か崖ハジのどちらかにいる
        bool onSlope = slopeHitForward ^ slopeHitBack;
        bool onEdge = edgeHitForward ^ edgeHitBack;
        //4本のうち1本でも当たっていれば接地とみなす
        bool isGrounded = slopeHitForward || slopeHitBack || edgeHitForward || edgeHitBack;

        if(isGrounded)
        {
            anim.SetBool("isFall", false);
            if(rb.linearVelocityY <= 0)
            {
                isJump = false;
                anim.SetBool("isJump", false);
            }
            if(Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                isJump = true;
                anim.SetBool("isJump", true);
                rb.linearVelocityY = 0;
                rb.AddForce(Vector2.up * jumpPower, ForceMode2D.Impulse);
                jumpSE.Play();
            }
        }
        else if(rb.linearVelocityY <= 0)
        {
            anim.SetBool("isFall", true);
        }

        //斜面・崖ハジで静止している間だけY座標を固定してずり落ちを防ぐ。
        //条件が外れたフレームでは必ずelse側が代入されるので、空中に固定が残ることはない
        bool freezeY = isGrounded && (onSlope || onEdge) && x == 0 && !isJump;
        rb.constraints = freezeY
            ? RigidbodyConstraints2D.FreezeRotation | RigidbodyConstraints2D.FreezePositionY
            : RigidbodyConstraints2D.FreezeRotation;

        //固定を解除した瞬間は地面に押し付けて、斜面の途中で浮き上がるのを防ぐ
        if(wasFreezeY && !freezeY && !isJump)
        {
            rb.linearVelocityY = -3;
        }
        wasFreezeY = freezeY;

        if(transform.position.y < -11f && !isDead)
        {
            isDead = true; // 死んだフラグを立てる
            Debug.Log("落下死：一度だけ実行します");
            mainManager.GameOver();
        }
        
    }
    void OnTriggerEnter2D(Collider2D other)
    {
        if(other.gameObject.layer == LayerMask.NameToLayer("Gem"))
        {
            mainManager.GameClear();
        }
    }

    void OnCollisionEnter2D(Collision2D other)
    {
        //ボスは通常の敵と違い、平常時は踏んでも倒せずゲームオーバーになる。
        //気絶中に上から踏んだときだけ攻撃が通り、ボスは踏まれた時点で気絶から復帰する
        if(other.gameObject.layer == LayerMask.NameToLayer("Boss"))
        {
            BossCtrl boss = other.gameObject.GetComponent<BossCtrl>();
            //ボスのコライダーは足元にオフセットされた大きな円なので、
            //敵と同じ transform.position の比較ではなく、ボスが公開している踏みつけラインで判定する
            float playerBottomY = transform.position.y + coll.offset.y - coll.radius;

            if(boss != null && boss.IsStunned && playerBottomY >= boss.StompLineY)
            {
                boss.Stomped();
                isJump = true;
                anim.SetBool("isJump", true);
                rb.linearVelocityY = 0;
                rb.AddForce(Vector2.up * jumpPower, ForceMode2D.Impulse);
            }
            else
            {
                mainManager.GameOver();
            }
            return;
        }

        if(other.gameObject.layer == LayerMask.NameToLayer("Enemy"))
        {
            if(other.transform.position.y < transform.position.y - coll.radius)
            {
                Destroy(other.gameObject);
                isJump = true;
                anim.SetBool("isJump", true);
                rb.linearVelocityY = 0;
                rb.AddForce(Vector2.up * jumpPower, ForceMode2D.Impulse);
            }
            else
            {
                mainManager.GameOver();
            }
        }
    }
}
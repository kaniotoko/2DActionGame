using System.Collections;
using UnityEngine;

public class DogCtrl : MonoBehaviour
{
    Transform player;
    Rigidbody2D rb;
    CapsuleCollider2D coll;
    Animator anim;

    [Header("検知設定")]
    public float noticeRange = 8f;

    [Header("Idle設定")]
    public float idleSpeed = 1.5f;
    public float wallDist = 0.5f;

    [Header("Chase設定")]
    public float chaseSpeed = 4f;
    public float cliffStopTime = 0.5f;
    public float cliffJumpForceY = 10f;
    public float cliffJumpForceX = 5f;

    [Header("Notice設定")]
    public float bouncePower = 3f;

    [Header("デバッグ")]
    public bool isIdle = true;
    public bool isNotice = false;
    public bool isChase = false;
    public bool isSet = false;
    public bool isJump = false;

    enum DogState { Idle, NoticeEnter, Chase, CliffStop, Jumping, Returning }
    DogState state = DogState.Idle;

    float idleCycleTimer = 0f;
    const float IDLE_WALK_TIME = 3f;
    const float IDLE_STOP_TIME = 2f;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        coll = GetComponent<CapsuleCollider2D>();
        anim = GetComponent<Animator>();
        player = GameObject.Find("Player").transform;
    }

    void Update()
    {
        if (player == null) return;

        isNotice = Vector3.Distance(transform.position, player.position) < noticeRange;

        switch (state)
        {
            case DogState.Idle:
                if (isNotice)
                    TransitionTo(DogState.NoticeEnter);
                else
                    UpdateIdle();
                break;

            case DogState.NoticeEnter:
                if (!isNotice)
                    TransitionTo(DogState.Returning);
                break;

            case DogState.Chase:
                if (!isNotice)
                    TransitionTo(DogState.Returning);
                else
                    UpdateChase();
                break;
        }

        // bool フラグを state から同期（アニメーターや外部スクリプト参照用）
        isIdle  = state == DogState.Idle;
        isChase = state == DogState.Chase || state == DogState.CliffStop || state == DogState.Jumping;
        isSet   = state == DogState.CliffStop;
        isJump  = state == DogState.Jumping;

        if (transform.position.y < -11f)
            Destroy(gameObject);
    }

    void TransitionTo(DogState next)
    {
        StopAllCoroutines();
        state = next;

        switch (next)
        {
            case DogState.NoticeEnter:
                rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
                StartCoroutine(NoticeRoutine());
                break;

            case DogState.CliffStop:
                rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
                StartCoroutine(CliffJumpRoutine());
                break;

            case DogState.Returning:
                rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
                StartCoroutine(ReturnToIdleRoutine());
                break;

            case DogState.Idle:
                idleCycleTimer = 0f;
                break;
        }
    }

    // -------------------------------------------------------
    // Idle：崖・壁を検知しながらゆっくりパトロール
    //       3秒歩いて2秒止まるサイクルを繰り返す
    // -------------------------------------------------------
    void UpdateIdle()
    {
        idleCycleTimer += Time.deltaTime;
        bool stopped = idleCycleTimer % (IDLE_WALK_TIME + IDLE_STOP_TIME) >= IDLE_WALK_TIME;

        if (anim != null) anim.SetBool("isWalk", !stopped);

        if (stopped)
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            return;
        }

        Vector3 origin = transform.position + (Vector3)coll.offset;
        float rayDist = coll.size.y / 2 + 1.5f;

        RaycastHit2D slopeForward = Physics2D.Raycast(
            origin + transform.right * (coll.size.x / 4),
            Vector2.down, rayDist, LayerMask.GetMask("Ground"));
        RaycastHit2D slopeBack = Physics2D.Raycast(
            origin - transform.right * (coll.size.x / 4),
            Vector2.down, rayDist, LayerMask.GetMask("Ground"));
        RaycastHit2D wallHit = Physics2D.Raycast(
            origin, transform.right, wallDist + coll.size.x / 2,
            LayerMask.GetMask("Ground"));

        if ((slopeForward || slopeBack) && rb.linearVelocity.y <= 0f)
            rb.linearVelocity = new Vector2(transform.right.x * idleSpeed, rb.linearVelocity.y);

        if (!slopeForward || wallHit)
        {
            Flip();
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        }
    }

    // -------------------------------------------------------
    // Chase：プレイヤーを追いかける。崖端で飛び越えジャンプ
    // -------------------------------------------------------
    void UpdateChase()
    {
        if (player.position.x > transform.position.x)
            transform.rotation = Quaternion.Euler(0, 180, 0);
        else
            transform.rotation = Quaternion.Euler(0, 0, 0);

        Vector3 origin = transform.position + (Vector3)coll.offset;
        float rayDist = coll.size.y / 2 + 1.5f;

        RaycastHit2D slopeForward = Physics2D.Raycast(
            origin + transform.right * (coll.size.x / 4),
            Vector2.down, rayDist, LayerMask.GetMask("Ground"));

        bool grounded = IsGrounded();

        if (!slopeForward && grounded)
        {
            TransitionTo(DogState.CliffStop);
            return;
        }

        if (grounded)
            rb.linearVelocity = new Vector2(transform.right.x * chaseSpeed, rb.linearVelocity.y);
    }

    // -------------------------------------------------------
    // Notice：プレイヤー発見時に2回小さくはねてからChaseへ
    // -------------------------------------------------------
    IEnumerator NoticeRoutine()
    {
        if (anim != null) anim.SetBool("isNotice", true);

        for (int i = 0; i < 2; i++)
        {
            yield return new WaitUntil(IsGrounded);
            rb.linearVelocity = new Vector2(0f, bouncePower);
            yield return null;                        // 1フレーム待って地面から浮く
            yield return new WaitUntil(IsGrounded);
            if (i < 1) yield return new WaitForSeconds(0.1f);
        }

        if (anim != null) anim.SetBool("isNotice", false);

        // Update が NoticeEnter を監視しているので、ここに到達するのは
        // プレイヤーが範囲内にいる場合のみ（範囲外なら TransitionTo(Returning) 済み）
        if (isNotice)
            state = DogState.Chase;
        else
            StartCoroutine(ReturnToIdleRoutine());
    }

    // -------------------------------------------------------
    // 崖端：0.5秒停止 → 高くジャンプして足場を飛び越える
    // -------------------------------------------------------
    IEnumerator CliffJumpRoutine()
    {
        yield return new WaitForSeconds(cliffStopTime);

        state = DogState.Jumping;
        float dir = transform.right.x;
        rb.linearVelocity = new Vector2(dir * cliffJumpForceX, cliffJumpForceY);

        yield return null;                           // 1フレーム待って地面から浮く
        yield return new WaitUntil(IsGrounded);

        if (isNotice)
            state = DogState.Chase;
        else
            StartCoroutine(ReturnToIdleRoutine());
    }

    // -------------------------------------------------------
    // Returning：1秒静止してからIdleへ戻る
    // -------------------------------------------------------
    IEnumerator ReturnToIdleRoutine()
    {
        state = DogState.Returning;
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        yield return new WaitForSeconds(1f);
        state = DogState.Idle;
        idleCycleTimer = 0f;
    }

    bool IsGrounded()
    {
        Vector3 origin = transform.position + (Vector3)coll.offset;
        return Physics2D.Raycast(origin, Vector2.down, coll.size.y / 2 + 0.15f, LayerMask.GetMask("Ground"));
    }

    void Flip()
    {
        if (Mathf.Approximately(transform.rotation.eulerAngles.y, 0f))
            transform.rotation = Quaternion.Euler(0f, 180f, 0f);
        else
            transform.rotation = Quaternion.Euler(0f, 0f, 0f);
    }
}

using System.Collections;
using UnityEngine;

public class BossCtrl : MonoBehaviour
{
    Transform player;
    Rigidbody2D rb;
    CircleCollider2D coll;
    Animator anim;

    [Header("小ジャンプ設定")]
    public float smallJumpPowerY = 13f;   // 小ジャンプの上向きの初速
    public float smallJumpPowerX = 5f;    // 小ジャンプでプレイヤー方向へ進む速さ
    public int smallJumpCount = 2;        // 大ジャンプに移る前に繰り返す回数
    public float smallJumpInterval = 0.5f;// 着地してから次の小ジャンプまでの待ち時間

    [Header("大ジャンプ設定")]
    public float bigJumpPowerY = 22f;     // 大ジャンプの上向きの初速
    public float chaseSpeedX = 30f;       // 上昇中にプレイヤーの真上へ回り込む速さ
    public float slamSpeed = 30f;         // 頂点で落下地点を確定させたあとの急降下速度
    public float slamDelay = 0.2f;        // 頂点で落下地点を確定してから急降下するまでの溜め

    [Header("着地後の設定")]
    public float landRecoverTime = 0.8f;  // 着地してから次の行動に移るまでの硬直

    [Header("デバッグ")]
    public BossState state = BossState.Idle;

    // アニメーションの状態は必ずこの enum を正とし、Animator の bool は SyncAnimatorParams で導出する
    // （bool を個別に持つと isIdle と isSJump が同時に true になる不整合が起きうるため）
    public enum BossState { Idle, SmallJump, BigJump, Slam }

    float defaultGravityScale;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        coll = GetComponent<CircleCollider2D>();
        anim = GetComponent<Animator>();
        player = GameObject.Find("Player").transform;
        defaultGravityScale = rb.gravityScale;

        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        StartCoroutine(PatternA());
    }

    void Update()
    {
        if (player == null) return;

        FacePlayer();
        SyncAnimatorParams();
    }

    // -------------------------------------------------------
    // 行動パターン①
    // 小ジャンプでプレイヤーに近づく → 大ジャンプでプレイヤーの真上へ → 急降下して着地
    // -------------------------------------------------------
    IEnumerator PatternA()
    {
        while (true)
        {
            for (int i = 0; i < smallJumpCount; i++)
            {
                yield return SmallJumpRoutine();
                yield return new WaitForSeconds(smallJumpInterval);
            }

            yield return BigJumpRoutine();
            yield return new WaitForSeconds(landRecoverTime);
        }
    }

    // -------------------------------------------------------
    // 小ジャンプ：プレイヤーの方向へ跳ねて距離を詰める
    // 距離に関わらず必ずプレイヤー側へ進む
    // -------------------------------------------------------
    IEnumerator SmallJumpRoutine()
    {
        state = BossState.SmallJump;

        // プレイヤーが右にいれば +1、左にいれば -1
        float dir = Mathf.Sign(player.position.x - transform.position.x);

        rb.linearVelocity = new Vector2(dir * smallJumpPowerX, smallJumpPowerY);

        yield return WaitForLanding();

        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        state = BossState.Idle;
    }

    // -------------------------------------------------------
    // 大ジャンプ：上昇中はプレイヤーのX座標を追いかけ、
    // 頂点（上向きの速度が0以下になった瞬間）で落下地点を確定して急降下する
    // -------------------------------------------------------
    IEnumerator BigJumpRoutine()
    {
        state = BossState.BigJump;
        rb.linearVelocity = new Vector2(0f, bigJumpPowerY);

        // 地面から離れるまで待つ（離れる前に頂点判定へ入らないようにする）
        yield return new WaitUntil(() => !IsGrounded());

        // 上昇中：プレイヤーの真上へ回り込む
        while (rb.linearVelocity.y > 0f)
        {
            float diffX = player.position.x - transform.position.x;
            float velX = Mathf.Clamp(diffX * chaseSpeedX, -chaseSpeedX, chaseSpeedX);
            rb.linearVelocity = new Vector2(velX, rb.linearVelocity.y);
            yield return null;
        }

        // 頂点：ここで落下地点を確定させる（以降プレイヤーを追わない）
        state = BossState.Slam;

        // 落下前の溜め。空中で静止させてプレイヤーに回避の猶予を与える
        rb.linearVelocity = Vector2.zero;
        rb.gravityScale = 0f;
        yield return new WaitForSeconds(slamDelay);

        // 急降下
        rb.gravityScale = defaultGravityScale;
        rb.linearVelocity = new Vector2(0f, -slamSpeed);
        yield return WaitForLanding();

        // 着地。ジャンプも落下もしていないので Idle に戻す（landRecoverTime の硬直中も Idle）
        state = BossState.Idle;
        rb.linearVelocity = Vector2.zero;

        // TODO: ここで衝撃波（Shockwave）を左右に発生させる
    }

    // -------------------------------------------------------
    // 空中に出てから着地するまで待つ
    // -------------------------------------------------------
    IEnumerator WaitForLanding()
    {
        yield return new WaitUntil(() => !IsGrounded());
        yield return new WaitUntil(() => IsGrounded() && rb.linearVelocity.y <= 0f);
    }

    bool IsGrounded()
    {
        Vector3 origin = transform.position + (Vector3)coll.offset;
        return Physics2D.Raycast(origin, Vector2.down, coll.radius + 0.15f, LayerMask.GetMask("Ground"));
    }

    // -------------------------------------------------------
    // 常にプレイヤーの方を向く。Vultureのスプライトは回転0で左向きなのでEagleCtrlと同じ扱いにする
    // -------------------------------------------------------
    void FacePlayer()
    {
        if (player.position.x > transform.position.x)
            transform.rotation = Quaternion.Euler(0f, 0f, 0f);
        else
            transform.rotation = Quaternion.Euler(0f, 180f, 0f);
    }

    // -------------------------------------------------------
    // Animator へ state を反映する
    // 行動パターン①（衝撃波なし）で使うのは isIdle / isSJump / isBJump / isFall1 の4つ
    // 今回のコミットでは isIdle / isSJump まで実装している
    // -------------------------------------------------------
    void SyncAnimatorParams()
    {
        if (anim == null) return;

        // Idle：ジャンプモーションも落下モーションもしていない状態
        // 小ジャンプの着地ごと、および大ジャンプ後の着地硬直中もここに入る
        anim.SetBool("isIdle", state == BossState.Idle);

        // SmallJump：跳び上がってから着地するまで。着地した瞬間に Idle へ戻る
        anim.SetBool("isSJump", state == BossState.SmallJump);
    }
}

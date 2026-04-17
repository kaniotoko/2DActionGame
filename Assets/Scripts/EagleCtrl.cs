using UnityEngine;

public class EagleCtrl : MonoBehaviour
{
    public Transform player;
    Rigidbody2D rb;
    Animator anim;

    [Header("基本設定")]
    public float hoverHeight;     // プレイヤーからの高さ
    public float amplitude;       // 左右に揺れる振幅
    public float frequency;       // 揺れるスピード
    
    [Header("攻撃設定")]
    public float attackInterval = 3.0f;  // 攻撃の間隔
    public float attackSpeed = 5.0f;     // 攻撃（弧を描く）スピード
    
    float timer;
    float attackProgress = 0f;           // 攻撃の進捗（0〜1）
    bool isAttacking = false;
    Vector3 startAttackPos;              // 攻撃開始時の位置

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        if (player == null)
            player = GameObject.Find("Player").transform;

        // 重力の影響を受けないように設定
        rb.gravityScale = 0;
    }

    void Update()
    {
        if (!isAttacking)
        {
            HoverBehavior();
            
            // 攻撃タイマー
            timer += Time.deltaTime;
            if (timer >= attackInterval)
            {
                StartAttack();
            }
        }
        else
        {
            AttackBehavior();
        }

        // 向きの制御（常に進行方向を向く）
        FlipSprite();
    }

    // --- 旋回（単振動）のアルゴリズム ---
    void HoverBehavior()
    {
        // プレイヤーの真上を基準点にする
        Vector3 basePos = player.position + Vector3.up * hoverHeight;
        
        // Mathf.Sinを使って左右に揺れる（単振動）
        float offsetX = Mathf.Sin(Time.time * frequency) * amplitude;
        
        Vector3 targetPos = new Vector3(basePos.x + offsetX, basePos.y, 0);
        
        // 直接座標を書き換える（空中を浮遊するため）
        transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * 2f);
    }

    // --- 攻撃開始の準備 ---
    void StartAttack()
    {
        isAttacking = true;
        timer = 0;
        attackProgress = 0;
        startAttackPos = transform.position;
        anim.SetBool("isFall", true);
    }

    // --- 楕円を描く攻撃のアルゴリズム ---
    void AttackBehavior()
    {
        attackProgress += Time.deltaTime * attackSpeed;

        // 攻撃の終点は、開始地点と左右対称の位置（プレイヤーを軸にする）
        // プレイヤーとの距離d*sinθの反対側へ抜ける動き
        float distanceToCenter = startAttackPos.x - player.position.x;
        Vector3 endAttackPos = new Vector3(player.position.x - distanceToCenter, startAttackPos.y, 0);

        // 楕円のパラメータ
        float a = Mathf.Abs(endAttackPos.x - startAttackPos.x) / 2; // 半長軸 (X方向)
        float b = hoverHeight; // 半短軸 (Y方向)
        float centerX = (startAttackPos.x + endAttackPos.x) / 2;
        float centerY = startAttackPos.y;

        // 進捗0〜π(180度) に変換
        float angle = attackProgress * Mathf.PI;
        
        // 楕円軌道の計算
        float currentX = centerX + a * Mathf.Cos(angle);
        float currentY = centerY - b * Mathf.Sin(angle);

        transform.position = new Vector3(currentX, currentY, 0);

        // 攻撃終了判定（半円を描ききったら）
        if (attackProgress >= Mathf.PI)
        {
            isAttacking = false;
            anim.SetBool("isFall", false);
        }
    }

    void FlipSprite()
    {
        // 前フレームとの差分で向きを変える
        if (rb.linearVelocityX > 0.01f || transform.position.x > player.position.x + 0.1f)
            transform.rotation = Quaternion.Euler(0, 0, 0);
        else
            transform.rotation = Quaternion.Euler(0, 180, 0);
    }
}
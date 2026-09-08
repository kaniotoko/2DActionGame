using UnityEngine;

public class EagleCtrl : MonoBehaviour
{
    public Transform player;
    Rigidbody2D rb;
    Animator anim;

    [Header("基本設定")]
    public float hoverHeight;  
    public float amplitude;    
    public float frequency;    
    
    [Header("攻撃設定")]
    public float attackInterval;
    public float attackSpeed;

    [Header("前動作設定")]
    public float preAttackHopHeight = 0.8f; // 急降下の前にその場で跳ね上がる高さ
    public float preAttackTime = 0.35f;     // 跳ね上がりにかける時間

    float timer;
    float currentAngle;
    float startAngle;
    float rotationDirection = 1f;
    bool isAttacking = false;
    bool isPreAttacking = false;   // 前動作（その場で軽く跳ね上がる）中かどうか
    float preAttackTimer;
    Vector3 preAttackStartPos;     // 前動作の開始位置。その場で跳ねるのでXはここで固定される
    Vector3 startAttackPos;
    Vector3 fixedCenterPos;    // ★追加：攻撃中に固定される中心点

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        if (player == null)
            player = GameObject.Find("Player").transform;

        rb.gravityScale = 0;
    }

    void Update()
    {
        if (isPreAttacking)
        {
            PreAttackBehavior();
        }
        else if (!isAttacking)
        {
            HoverBehavior();

            timer += Time.deltaTime;
            if (timer >= attackInterval)
            {
                StartPreAttack();
            }
        }
        else
        {
            AttackBehavior();
        }

        FlipSprite();
    }

    void HoverBehavior()
    {
        Vector3 basePos = player.position + Vector3.up * hoverHeight;
        float offsetX = Mathf.Sin(Time.time * frequency) * amplitude;
        Vector3 targetPos = new Vector3(basePos.x + offsetX, basePos.y, 0);
        
        transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * 2f);// Lerpで滑らかに移動させると自然になります
    }

    // -------------------------------------------------------
    // 前動作の開始。ホバリングをやめ、その場で軽く跳ね上がるフェーズに入る
    // -------------------------------------------------------
    void StartPreAttack()
    {
        isPreAttacking = true;
        timer = 0;
        preAttackTimer = 0;

        // 跳ねている間はXを動かさないので、ここで開始位置ごと保存しておく
        preAttackStartPos = transform.position;
    }

    // -------------------------------------------------------
    // 前動作：その場で微力にジャンプする
    // 頂点で上昇速度が0になるカーブ（Sin 0→π/2）を使うので、
    // そのまま急降下（AttackBehavior）に繋いでも動きが途切れない
    // -------------------------------------------------------
    void PreAttackBehavior()
    {
        preAttackTimer += Time.deltaTime;
        float t = Mathf.Clamp01(preAttackTimer / preAttackTime);

        float hop = preAttackHopHeight * Mathf.Sin(t * Mathf.PI * 0.5f);
        transform.position = new Vector3(preAttackStartPos.x, preAttackStartPos.y + hop, 0);

        if (t >= 1f)
        {
            isPreAttacking = false;
            StartAttack();
        }
    }

    void StartAttack()
    {
        isAttacking = true;

        // ★ここで今回の中心点を「確定」させて保存する
        fixedCenterPos = player.position + Vector3.up * hoverHeight;

        // 保存した固定中心点(fixedCenterPos)を使って、角度を逆算する
        Vector3 diff = transform.position - fixedCenterPos;

        // 前動作で跳ね上がった分だけ中心点より高い位置から始まるので、
        // 楕円の半径（a, b）で正規化してから角度を求める。
        // こうしないと軌道の開始点と現在位置がズレて、降下の瞬間にワープして見える
        float startA = Mathf.Max(Mathf.Abs(diff.x), 0.0001f);
        float startB = Mathf.Max(hoverHeight, 0.0001f);
        startAngle = Mathf.Atan2(diff.y / startB, diff.x / startA);
        currentAngle = startAngle;

        if (transform.position.x > player.position.x)
            rotationDirection = -1f;
            
        else
            rotationDirection = 1f;
        
        startAttackPos = transform.position;
        anim.SetBool("isFall", true);
    }

    void AttackBehavior()
    {
        currentAngle += Time.deltaTime * attackSpeed * rotationDirection;

        // ★Updateで更新されるplayer.positionではなく、保存したfixedCenterPosを使う
        float a = Mathf.Abs(startAttackPos.x - fixedCenterPos.x); // 横半径も固定中心点基準にする
        float b = hoverHeight;

        float currentX = fixedCenterPos.x + a * Mathf.Cos(currentAngle); //x座標を媒介変数表示
        float currentY = fixedCenterPos.y + b * Mathf.Sin(currentAngle); //y座標を媒介変数表示

        transform.position = new Vector3(currentX, currentY, 0);

        if (Mathf.Abs(currentAngle - startAngle) >= Mathf.PI)
        {
            isAttacking = false;
            anim.SetBool("isFall", false);
        }
    }

    void FlipSprite()
    {
        // 攻撃中は進行方向、待機中はプレイヤーを向くようにすると自然です
        if (transform.position.x > player.position.x)
            transform.rotation = Quaternion.Euler(0, 0, 0);
        else
            transform.rotation = Quaternion.Euler(0, 180, 0);
    }
}
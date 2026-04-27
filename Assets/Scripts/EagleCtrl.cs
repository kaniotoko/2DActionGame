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
    
    float timer;
    float currentAngle;         
    float startAngle;           
    float rotationDirection = 1f; 
    bool isAttacking = false;
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
        if (!isAttacking)
        {
            HoverBehavior();
            
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

        FlipSprite();
    }

    void HoverBehavior()
    {
        Vector3 basePos = player.position + Vector3.up * hoverHeight;
        float offsetX = Mathf.Sin(Time.time * frequency) * amplitude;
        Vector3 targetPos = new Vector3(basePos.x + offsetX, basePos.y, 0);
        
        transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * 2f);
    }

    void StartAttack()
    {
        isAttacking = true;
        timer = 0;

        // ★ここで今回の中心点を「確定」させて保存する
        fixedCenterPos = player.position + Vector3.up * hoverHeight;
        
        // 保存した固定中心点(fixedCenterPos)を使って、角度を逆算する
        Vector3 diff = transform.position - fixedCenterPos;
        
        startAngle = Mathf.Atan2(diff.y, diff.x); 
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

        float currentX = fixedCenterPos.x + a * Mathf.Cos(currentAngle);
        float currentY = fixedCenterPos.y + b * Mathf.Sin(currentAngle);

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
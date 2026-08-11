using UnityEngine;

// Boss の行動パターン②で撃ち出される攻撃用イーグル。
// 通常の Eagle（EagleCtrl）はプレイヤーを追ってホバリング＆急降下するが、
// こちらはプレイヤーを一切参照せず、生成時に渡された方向へ直進するだけの弾として振る舞う。
public class AttackEagleCtrl : MonoBehaviour
{
    [Header("基本設定")]
    public float speed = 12f;    // 水平方向の速さ。BossCtrl から Launch() で上書きされる
    public float lifeTime = 15f; // 保険：消し損ねてもこの時間で必ず消える

    Rigidbody2D rb;
    float dirX = 1f;      // +1で右へ、-1で左へ飛ぶ
    float despawnX;       // このX座標を進行方向に通過したら消える
    bool hasDespawnX = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        // 重力で落ちずに水平に飛ばすため、プレハブの設定に関わらずここで無効化する
        rb.gravityScale = 0f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
    }

    // -------------------------------------------------------
    // BossCtrl から生成直後に呼ばれ、飛ぶ向き・速さ・消える位置を決める
    // -------------------------------------------------------
    public void Launch(float directionX, float moveSpeed, float despawnPosX)
    {
        dirX = directionX >= 0f ? 1f : -1f;
        speed = moveSpeed;
        despawnX = despawnPosX;
        hasDespawnX = true;

        FaceMoveDirection();
        Destroy(gameObject, lifeTime);
    }

    void FixedUpdate()
    {
        // プレイヤーと接触して押し返されても軌道が崩れないよう、毎フレーム速度を上書きする
        rb.linearVelocity = new Vector2(dirX * speed, 0f);

        if (hasDespawnX && (dirX > 0f ? transform.position.x > despawnX : transform.position.x < despawnX))
        {
            Destroy(gameObject);
        }
    }

    // -------------------------------------------------------
    // Eagleのスプライトは回転0で左向きなので、右へ飛ぶときだけ反転させる
    // -------------------------------------------------------
    void FaceMoveDirection()
    {
        if (dirX > 0f)
            transform.rotation = Quaternion.Euler(0f, 180f, 0f);
        else
            transform.rotation = Quaternion.Euler(0f, 0f, 0f);
    }
}

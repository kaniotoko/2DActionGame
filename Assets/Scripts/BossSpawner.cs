using System.Collections;
using UnityEngine;

public class BossSpawner : MonoBehaviour
{
    public GameObject bossPrefab;
    // プレイヤーが横にどのくらい近づいたら出すか。
    // 他の敵のSpawnerと違いBossSpawnerは空中の高い位置に置くので、
    // Vector2.Distance で測ると高さの差（十数ユニット）に距離のほとんどを食われてしまい、
    // 値を大きくしても実際にはかなり近づくまで始まらない。
    // 高さは無視して横の距離だけで判定する
    public float spawnDistance = 45f;
    private bool hasSpawned = false;
    public Transform player;
    private GameObject spawnedBoss;

    [Header("登場演出")]
    public GameObject smokePrefab;      // Bossが出てくる前にこの位置へ出す煙。enemy-deathのスプライトで作ったプレハブをセットする
    public float smokeTime = 1f;        // 煙を出してからBossを出現させるまでの時間
    public float cameraBlendTime = 1.5f;// カメラがプレイヤーとBossの間を移動するのにかける時間

    void Start()
    {
        player = GameObject.Find("Player").transform; // シーンに出現された時Playerというオブジェクトを代入する
    }

    // -------------------------------------------------------
    // 他の敵のSpawnerと違い、一度出したBossは離れても消さない。
    // ボス戦の途中でプレイヤーが下がるとBossが消えて体力（hp）や
    // 行動パターンの進行が最初からやり直しになってしまうため、
    // 出現後は撃破されるまでそのまま残す
    //
    // 撃破されたときは BossCtrl 側が自分を Destroy するが、
    // hasSpawned は true のままなので再び出現することはない
    // -------------------------------------------------------
    void Update()
    {
        if (hasSpawned) return;

        if (Mathf.Abs(transform.position.x - player.position.x) < spawnDistance)
        {
            // 演出の途中でもう一度入ってこないよう、開始した時点で立てる
            hasSpawned = true;
            StartCoroutine(IntroRoutine());
        }
    }

    // -------------------------------------------------------
    // 登場演出の進行
    // 煙はBossがまだ存在しない時点で出す必要があり、プレイヤーのロックとカメラの操作も
    // 煙から戦闘開始までをまたぐので、Boss本体ではなくSpawnerが全体の進行を持つ。
    // Boss自身の動き（下降 → 停止 → 登場モーション）だけを BossCtrl.PlayIntro に任せる
    // -------------------------------------------------------
    IEnumerator IntroRoutine()
    {
        PlayerCrtl playerCtrl = player.GetComponent<PlayerCrtl>();
        CameraCtrl cameraCtrl = FindFirstObjectByType<CameraCtrl>();

        // ① プレイヤーをその場に固定して、カメラをこちらへ寄せる
        if (playerCtrl != null) playerCtrl.Lock();
        if (cameraCtrl != null) cameraCtrl.SetTarget(transform, cameraBlendTime);

        // ② 煙を出す
        GameObject smoke = null;
        if (smokePrefab != null) smoke = Instantiate(smokePrefab, transform.position, Quaternion.identity);

        yield return new WaitForSeconds(smokeTime);

        if (smoke != null) Destroy(smoke);

        // ③ 煙と同じ位置にBossを出す。カメラの追尾先もBossへ移す。
        //    煙と同じ座標なので、ここは即座に切り替えても見た目は動かない
        spawnedBoss = Instantiate(bossPrefab, transform.position, Quaternion.identity);
        if (cameraCtrl != null) cameraCtrl.SetTarget(spawnedBoss.transform, 0f);

        BossCtrl boss = spawnedBoss.GetComponent<BossCtrl>();

        // 降りてくる間、プレイヤーはロックされていて避けられない。
        // 真下にいると接触してその場でゲームオーバーになってしまうので、
        // 演出が終わるまでBossとプレイヤーの当たり判定を切っておく
        Collider2D bossColl = spawnedBoss.GetComponent<Collider2D>();
        Collider2D playerColl = player.GetComponent<Collider2D>();
        bool collisionIgnored = bossColl != null && playerColl != null;
        if (collisionIgnored) Physics2D.IgnoreCollision(bossColl, playerColl, true);

        // ④〜⑥ 下降 → 着地して停止 → 登場モーション
        if (boss != null) yield return boss.PlayIntro();

        // ⑦ カメラをプレイヤーに戻す。
        //    戻り終わるまでは待つ。移動中にBossが動き出すと画面外で行動が進んでしまうため、
        //    プレイヤーのロックもここまで続ける
        if (cameraCtrl != null)
        {
            cameraCtrl.ResetTarget(cameraBlendTime);
            yield return new WaitForSeconds(cameraBlendTime);
        }

        // ⑧ 当たり判定と操作を元どおりにして戦闘開始
        if (collisionIgnored) Physics2D.IgnoreCollision(bossColl, playerColl, false);
        if (playerCtrl != null) playerCtrl.Unlock();
        if (boss != null) boss.StartBattle();
    }
}

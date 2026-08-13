using System.Collections;
using System.Collections.Generic;
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
    public float preSlamLiftSpeed = 6f;   // 急降下の前動作：真上へ持ち上げる速さ
    public float preSlamLiftTime = 0.15f; // 急降下の前動作：持ち上げ続ける時間

    [Header("着地後の設定")]
    public float landRecoverTime = 0.8f;  // 着地してから次の行動に移るまでの硬直

    [Header("行動パターン②：端への移動")]
    public int patternARepeat = 2;        // 行動②へ移る前に行動①を繰り返す回数
    public float stageLeftX = -11.2f;      // ステージ左端。Bossがここへ移動して構える
    public float stageRightX = 58.6f;      // ステージ右端。Bossがここへ移動して構える
    public float moveRiseHeight = 12f;     // 端へ移動する前に真上へ上がる高さ
    public float moveRiseSpeed = 20f;      // 真上へ上がるときの速さ
    public float moveHorizontalSpeed = 30f;// 上空を水平移動するときの速さ
    public float moveFallSpeed = 20f;      // 目標のX座標に着いてから下降するときの速さ

    [Header("行動パターン②：イーグル攻撃")]
    public GameObject attackEaglePrefab;   // AttackEagle.prefab をセットする
    public float eagleSpeed = 12f;         // イーグルがステージを横切る速さ
    public float eagleWaveInterval = 1.2f; // 波と波の間隔
    public float eagleWaveEndWait = 1f;    // 最後の波を撃ってから次の行動に移るまでの待ち
    public float eagleSpawnOffset = 1f;    // Bossのコライダーの端から、進行方向へどれだけ離して生成するか
    public float eagleDespawnMargin = 5f;  // 反対側の端からどれだけ外側まで飛ばしてから消すか

    // 下から順に並べた高さ。eagleWaves の番号はこの配列の添字。
    // 値は「地面から、イーグルのコライダーの底までの高さ」。
    //   地面 ＝ Bossのコライダーの底（Bossは端の地面に立っているのでこれが地面の高さになる）
    // 0より大きくしておけば地面に埋まらない。
    // AttackEagleの大きさを1マス（Gridのセルサイズ＝1 unit）とみなし、1段ごとに1 unitずつ上げている
    public float[] eagleHeights = { 0.2f, 1.2f, 2.2f, 3.2f };

    // 飛ばす順番。下から 1-2、3-4、1-2（添字なので 0-1、2-3、0-1）
    public EagleWave[] eagleWaves =
    {
        new EagleWave { lowerIndex = 0, upperIndex = 1 },
        new EagleWave { lowerIndex = 2, upperIndex = 3 },
        new EagleWave { lowerIndex = 0, upperIndex = 1 },
    };

    // 1波ぶん＝縦に2体並んだイーグルの高さの組み合わせ
    [System.Serializable]
    public class EagleWave
    {
        public int lowerIndex; // 下側のイーグルの高さ番号（0が一番下）
        public int upperIndex; // 上側のイーグルの高さ番号
    }

    [Header("行動パターン③：高く跳んで滞空 → 落下 → 気絶")]
    // ①と同じ跳び方（初速を与えて重力で減速させる）で、より強い初速を与えて高く跳ぶ。
    // 頂点の高さ ＝ 初速² ÷ (2 × 重力30)。26なら地面から約11.3
    // カメラは orthographic size 10 なのでプレイヤーの±10しか映らない。
    // Bossのコライダーは中心から下へ5.8あるので、31を超えると体ごと画面外に出る
    public float highJumpPowerY = 21f;
    public float floatChaseSpeedX = 30f;   // 滞空中にプレイヤーの真上へ回り込む速さ
    public float floatHoverTime = 3f;      // 頂点でプレイヤーの頭上を飛び続ける時間（①はここで静止するだけ）
    public float stunTime = 5f;            // 気絶して動けない時間。踏まれた場合は途中で打ち切る
    public float stunEndIdleTime = 2f;     // 気絶から復帰したあと、Idleのまま静止している時間

    [Header("行動パターン③：気絶中の足場")]
    public GameObject stunPlatformPrefab;  // プレイヤーがBossの頭上まで登るための足場。Groundレイヤーのプレハブをセットする
    public int stunPlatformBlockCount = 3; // 1つの足場を何個のブロックを横に並べて作るか

    // 足場を出す位置。BossのX座標と、Bossが立っている地面の高さを原点とした相対座標。
    //   x ＝ 足場の中心。ブロックはこの位置を中心に左右へ並ぶ
    //   y ＝ 足場の踏み面（プレイヤーが乗る面）の高さ
    // Bossのコライダーは半径3・上端が地面から6の高さなので、
    // 遠い側を低く・近い側を高くして、階段状に登ってBossの上へ跳び移れるようにしている
    public Vector2[] stunPlatformOffsets =
    {
        new Vector2(-9f, 2.5f),
        new Vector2(-6f, 5f),
        new Vector2(9f, 2.5f),
        new Vector2(6f, 5f),
    };

    [Header("体力・踏みつけ判定")]
    public int maxHp = 3;                  // 気絶中に踏める回数
    public float stompTolerance = 0.5f;    // 踏みつけ判定の余裕。大きいほど甘くなる

    [Header("デバッグ")]
    public BossState state = BossState.Idle;

    // アニメーションの状態は必ずこの enum を正とし、Animator の bool は SyncAnimatorParams で導出する
    // （bool を個別に持つと isIdle と isSJump が同時に true になる不整合が起きうるため）
    public enum BossState { Idle, SmallJump, BigJump, Fall1, MoveJump, HighJump, Fall2, Stun }

    float defaultGravityScale;
    int hp;
    bool stompedInStun = false;                                    // 今回の気絶中にもう踏まれたか。気絶の待ち時間を打ち切るために使う
    List<GameObject> stunPlatforms = new List<GameObject>();       // 気絶中に出している足場。復帰時にまとめて消す
    HashSet<string> animBoolParams = new HashSet<string>();        // Animator に実際にある bool パラメータ名

    // 気絶中か。プレイヤー側の踏みつけ判定で参照する
    public bool IsStunned => state == BossState.Stun;

    // プレイヤーのコライダーの底がこの高さ以上なら「Bossを上から踏んだ」とみなす。
    // Bossのコライダーは半径3の大きな円なので、transform.position では上下の判定ができない
    public float StompLineY => transform.position.y + coll.offset.y + coll.radius - stompTolerance;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        coll = GetComponent<CircleCollider2D>();
        anim = GetComponent<Animator>();
        player = GameObject.Find("Player").transform;
        defaultGravityScale = rb.gravityScale;
        hp = maxHp;

        CacheAnimatorBoolParams();

        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        StartCoroutine(BossRoutine());
    }

    void Update()
    {
        if (player == null) return;

        // 気絶中は倒れているので、プレイヤーの方を向き直さない
        if (state != BossState.Stun) FacePlayer();

        SyncAnimatorParams();
    }

    // -------------------------------------------------------
    // Bossの行動全体の流れ
    // 行動①を patternARepeat 回 → 行動② → 行動③ → 最初に戻る
    // 行動③の気絶が終わったら、そのまま①からのルーティンを再開する
    // -------------------------------------------------------
    IEnumerator BossRoutine()
    {
        while (true)
        {
            for (int i = 0; i < patternARepeat; i++)
            {
                yield return PatternA();
            }

            yield return PatternB();
            yield return PatternC();
        }
    }

    // -------------------------------------------------------
    // 行動パターン①（1サイクル分）
    // 小ジャンプでプレイヤーに近づく → 大ジャンプでプレイヤーの真上へ → 急降下して着地
    // -------------------------------------------------------
    IEnumerator PatternA()
    {
        for (int i = 0; i < smallJumpCount; i++)
        {
            yield return SmallJumpRoutine();
            yield return new WaitForSeconds(smallJumpInterval);
        }

        yield return BigJumpRoutine();
        yield return new WaitForSeconds(landRecoverTime);
    }

    // -------------------------------------------------------
    // 行動パターン②（1サイクル分）
    // 左端へ移動してイーグルを右向きに飛ばす → 右端へ移動して左向きに飛ばす
    // -------------------------------------------------------
    IEnumerator PatternB()
    {
        yield return MoveJumpRoutine(stageLeftX);
        yield return new WaitForSeconds(landRecoverTime);

        // 左端にいるので、Bossの右側から出して反対側（右端）へ横切らせる
        yield return EagleWaveRoutine(stageRightX);

        yield return MoveJumpRoutine(stageRightX);
        yield return new WaitForSeconds(landRecoverTime);

        // 右端にいるので、今度はBossの左側から出して左端へ横切らせる
        yield return EagleWaveRoutine(stageLeftX);
    }

    // -------------------------------------------------------
    // 行動パターン③（1サイクル分）
    // プレイヤーの上空へ高く浮上 → 数秒プレイヤーを追いながら滞空 → 前動作をつけて落下
    // → 着地して横向きに倒れ、数秒間気絶（この間だけプレイヤーに踏まれる）
    // → 復帰したあと数秒Idleで静止して、①からのルーティンへ戻る
    //
    // 跳び上がりかたは①の大ジャンプとまったく同じで、
    //   ・初速が強い（highJumpPowerY）ぶん頂点が高い
    //   ・頂点で静止せず、プレイヤーの頭上を追いながら floatHoverTime 秒飛び続ける
    //   ・着地したあとに気絶して無防備になる
    // という点が違う
    // -------------------------------------------------------
    IEnumerator PatternC()
    {
        // ① ①の大ジャンプと同じ跳び方。より強い初速を与えて高く跳び上がる
        state = BossState.HighJump;
        rb.linearVelocity = new Vector2(0f, highJumpPowerY);

        // 地面から離れるまで待つ（離れる前に頂点判定へ入らないようにする）
        yield return new WaitUntil(() => !IsGrounded());

        // 上昇中：プレイヤーの真上へ回り込む
        while (rb.linearVelocity.y > 0f)
        {
            rb.linearVelocity = new Vector2(ChaseVelocityX(chaseSpeedX), rb.linearVelocity.y);
            yield return null;
        }

        // ② 頂点。①はここで静止するだけだが、③は重力を切って高さを保ったまま
        //    プレイヤーの頭上を追いかけ続ける
        rb.gravityScale = 0f;

        float hovered = 0f;
        while (hovered < floatHoverTime)
        {
            rb.linearVelocity = new Vector2(ChaseVelocityX(floatChaseSpeedX), 0f);
            hovered += Time.deltaTime;
            yield return null;
        }

        // ③ 落下地点を確定させる（以降プレイヤーを追わない）
        rb.linearVelocity = Vector2.zero;
        yield return new WaitForSeconds(slamDelay);

        // 前動作：少しだけ真上へ持ち上げてから落とす。重力を切ったままなので等速で上がる
        state = BossState.Fall2;
        rb.linearVelocity = new Vector2(0f, preSlamLiftSpeed);
        yield return new WaitForSeconds(preSlamLiftTime);

        // ④ 落下して着地
        rb.gravityScale = defaultGravityScale;
        rb.linearVelocity = new Vector2(0f, -slamSpeed);
        yield return WaitForLanding();

        rb.linearVelocity = Vector2.zero;

        // ⑤ 気絶
        yield return StunRoutine();

        // ⑥ 復帰後は数秒その場で静止してから、次のルーティンへ
        state = BossState.Idle;
        yield return new WaitForSeconds(stunEndIdleTime);
    }

    // -------------------------------------------------------
    // 気絶：横向きに倒れて stunTime 秒動けなくなる
    // この間だけプレイヤーが上から踏んでダメージを与えられる（踏まれたら即座に復帰する）
    // 周りに足場を出して、プレイヤーがBossの頭上まで登れるようにする
    //
    // 「横向きに倒れる」見た目は Stun のアニメーションで表現する。
    // Bossのコライダーは中心が足元にずれた円（offset -2.8 / 半径3）なので、
    // transform を回転させると当たり判定ごと横へずれて地面から浮いてしまう。
    // そのため挙動側では transform を回転させず、状態を Stun にするだけにしている
    // -------------------------------------------------------
    IEnumerator StunRoutine()
    {
        state = BossState.Stun;
        stompedInStun = false;

        SpawnStunPlatforms();

        // 踏まれたらその時点で気絶を打ち切る
        float stunned = 0f;
        while (stunned < stunTime && !stompedInStun)
        {
            stunned += Time.deltaTime;
            yield return null;
        }

        DespawnStunPlatforms();
    }

    // -------------------------------------------------------
    // 気絶中に上から踏まれたときにプレイヤー側（PlayerCrtl）から呼ばれる
    // -------------------------------------------------------
    public void Stomped()
    {
        // 気絶していないときの接触はプレイヤー側でゲームオーバーとして処理される。
        // 1回の気絶で複数回ダメージが入らないよう、踏まれたあとの接触も無視する
        if (state != BossState.Stun || stompedInStun) return;

        hp--;
        stompedInStun = true;

        if (hp <= 0)
        {
            // Bossを消すとコルーチンも止まるので、出しっぱなしの足場はここで片付ける
            DespawnStunPlatforms();

            // TODO: 撃破時の演出（別途実装）
            Destroy(gameObject);
        }
    }

    // -------------------------------------------------------
    // 気絶中の足場を出す
    // stunPlatformOffsets の1件につき、ブロックを stunPlatformBlockCount 個だけ横に並べて1つの足場にする
    // -------------------------------------------------------
    void SpawnStunPlatforms()
    {
        if (stunPlatformPrefab == null) return;

        // Bossは地面に着地しているので、コライダーの底がそのまま地面の高さになる
        float groundY = transform.position.y + coll.offset.y - coll.radius;

        // 足場ブロックの大きさと、位置から踏み面までの距離。
        // 生成後の bounds は物理エンジンの同期待ちで正しい値が返らないことがあるため、
        // イーグルと同じくプレハブのコライダー設定から直接求める
        float blockWidth = 1f;
        float topOffset = 0f;
        BoxCollider2D prefabColl = stunPlatformPrefab.GetComponent<BoxCollider2D>();
        if (prefabColl != null)
        {
            blockWidth = prefabColl.size.x;
            topOffset = prefabColl.offset.y + prefabColl.size.y / 2f;
        }

        int blockCount = Mathf.Max(1, stunPlatformBlockCount);

        foreach (Vector2 offset in stunPlatformOffsets)
        {
            // ブロックを offset.x を中心に左右へ均等に並べる
            float leftBlockX = transform.position.x + offset.x - (blockCount - 1) * blockWidth / 2f;

            // offset.y は踏み面の高さなので、プレハブの原点の高さに直す
            float spawnY = groundY + offset.y - topOffset;

            for (int i = 0; i < blockCount; i++)
            {
                Vector3 spawnPos = new Vector3(leftBlockX + i * blockWidth, spawnY, 0f);
                GameObject platform = Instantiate(stunPlatformPrefab, spawnPos, Quaternion.identity);

                // 足場がBossのコライダーに重なったときに押し出されて位置がずれるのを防ぐ。
                // イーグルと同じく、レイヤーごとではなく個体ごとに無効化する
                Collider2D platformColl = platform.GetComponent<Collider2D>();
                if (platformColl != null) Physics2D.IgnoreCollision(platformColl, coll);

                stunPlatforms.Add(platform);
            }
        }
    }

    // -------------------------------------------------------
    // 気絶が終わったら足場をまとめて消す
    // -------------------------------------------------------
    void DespawnStunPlatforms()
    {
        foreach (GameObject platform in stunPlatforms)
        {
            if (platform != null) Destroy(platform);
        }

        stunPlatforms.Clear();
    }

    // -------------------------------------------------------
    // プレイヤーの真上へ回り込むためのX方向の速度
    // 近づくほど遅くなるので、プレイヤーの真上で行ったり来たりせずに落ち着く
    // -------------------------------------------------------
    float ChaseVelocityX(float speed)
    {
        float diffX = player.position.x - transform.position.x;
        return Mathf.Clamp(diffX * speed, -speed, speed);
    }

    // -------------------------------------------------------
    // 端への移動：真上へ上がる → 上空を目標のX座標まで水平移動 → 下降して着地
    // 重力を切って手動で動かすので、ステージ幅がどれだけ広くても必ず目標の位置に着地する
    // -------------------------------------------------------
    IEnumerator MoveJumpRoutine(float targetX)
    {
        // 上昇・水平移動・下降のすべてを通して MoveJump のまま。着地した時点で Idle に戻す
        state = BossState.MoveJump;
        rb.gravityScale = 0f;

        // ① 真上へ上がる
        float peakY = transform.position.y + moveRiseHeight;
        rb.linearVelocity = new Vector2(0f, moveRiseSpeed);
        yield return new WaitUntil(() => transform.position.y >= peakY);

        // ② 上空を水平移動する。目標のX座標を通り過ぎたら止める
        float dirX = targetX > transform.position.x ? 1f : -1f;
        rb.linearVelocity = new Vector2(dirX * moveHorizontalSpeed, 0f);
        yield return new WaitUntil(() => (targetX - transform.position.x) * dirX <= 0f);

        // 通り過ぎたぶんのズレを消して、X座標をぴったり目標に合わせる
        transform.position = new Vector3(targetX, transform.position.y, transform.position.z);

        // ③ 下降して着地
        rb.gravityScale = defaultGravityScale;
        rb.linearVelocity = new Vector2(0f, -moveFallSpeed);
        yield return WaitForLanding();

        // 着地。イーグルを呼ぶ間はその場から動かない
        state = BossState.Idle;
        rb.linearVelocity = Vector2.zero;
    }

    // -------------------------------------------------------
    // イーグルの群れを飛ばす
    // 1波につき縦に2体（eagleWaves で指定した高さの組み合わせ）を同時に出す
    // towardX には反対側の端のX座標を渡す。飛ぶ向きはBossの現在位置との差から決めるので、
    // stageLeftX / stageRightX にどちらの値が入っていても必ず反対側の端へ向かって飛ぶ
    // -------------------------------------------------------
    IEnumerator EagleWaveRoutine(float towardX)
    {
        state = BossState.Idle;

        float dirX = towardX > transform.position.x ? 1f : -1f;

        foreach (EagleWave wave in eagleWaves)
        {
            SpawnAttackEagle(wave.lowerIndex, dirX, towardX);
            SpawnAttackEagle(wave.upperIndex, dirX, towardX);
            yield return new WaitForSeconds(eagleWaveInterval);
        }

        yield return new WaitForSeconds(eagleWaveEndWait);
    }

    // -------------------------------------------------------
    // 攻撃用イーグルを1体生成して撃ち出す
    // heightIndex は eagleHeights の添字（0が一番下）
    // -------------------------------------------------------
    void SpawnAttackEagle(int heightIndex, float dirX, float towardX)
    {
        if (attackEaglePrefab == null || eagleHeights.Length == 0) return;

        int index = Mathf.Clamp(heightIndex, 0, eagleHeights.Length - 1);

        // Bossのコライダーの、進行方向側の端から eagleSpawnOffset だけ離した位置で生成する。
        // 左端にいるとき（右へ飛ばすとき）はコライダーの右端の少しプラス側、
        // 右端にいるとき（左へ飛ばすとき）はコライダーの左端の少しマイナス側になる
        float spawnX = transform.position.x + coll.offset.x + dirX * (coll.radius + eagleSpawnOffset);
        float despawnX = towardX + dirX * eagleDespawnMargin;

        // Bossは端の地面に立っているので、Bossのコライダーの底がそのまま地面の高さになる。
        // イーグルのコライダーの底をこの高さに合わせる
        float bossBottomY = transform.position.y + coll.offset.y - coll.radius;
        float targetBottomY = bossBottomY + eagleHeights[index];

        // イーグルの位置から、そのコライダーの底までの距離（負の値）。
        // 生成後の bounds は物理エンジンの同期待ちで正しい値が返らないことがあるため、
        // プレハブのコライダー設定から直接求める
        float bottomOffset = 0f;
        CircleCollider2D prefabColl = attackEaglePrefab.GetComponent<CircleCollider2D>();
        if (prefabColl != null) bottomOffset = prefabColl.offset.y - prefabColl.radius;

        float spawnY = targetBottomY - bottomOffset;

        GameObject eagle = Instantiate(attackEaglePrefab, new Vector3(spawnX, spawnY, 0f), Quaternion.identity);

        // このイーグルとBossの間だけ衝突を無効化して、すり抜けるようにする。
        // 生成位置がBossのコライダーに重なったときに押し出されて軌道が崩れるのと、
        // 飛んでいる途中でBossが移動してきてぶつかるのを防ぐ。
        // レイヤー（Enemy×Boss）ごと切ると他の敵とBossの衝突まで消えてしまうので、個体ごとに無効化する
        Collider2D eagleColl = eagle.GetComponent<Collider2D>();
        if (eagleColl != null) Physics2D.IgnoreCollision(eagleColl, coll);

        AttackEagleCtrl ctrl = eagle.GetComponent<AttackEagleCtrl>();
        if (ctrl != null) ctrl.Launch(dirX, eagleSpeed, despawnX);
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
            rb.linearVelocity = new Vector2(ChaseVelocityX(chaseSpeedX), rb.linearVelocity.y);
            yield return null;
        }

        // 頂点：ここで落下地点を確定させる（以降プレイヤーを追わない）
        // 滞空中もまだ BigJump のまま。落下に転じた時点で Fall1 へ移す
        // 落下前の溜め。空中で静止させてプレイヤーに回避の猶予を与える
        rb.linearVelocity = Vector2.zero;
        rb.gravityScale = 0f;
        yield return new WaitForSeconds(slamDelay);

        // 前動作：少しだけ真上へ持ち上げてから落とす
        // 重力を切ったままなので等速で上がり、上昇量は速さ×時間で決まる
        state = BossState.Fall1;
        rb.linearVelocity = new Vector2(0f, preSlamLiftSpeed);
        yield return new WaitForSeconds(preSlamLiftTime);

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
    // 行動パターン②では端への移動中に isMJump を使う
    // -------------------------------------------------------
    void SyncAnimatorParams()
    {
        if (anim == null) return;

        // Idle：ジャンプモーションも落下モーションもしていない状態
        // 小ジャンプの着地ごと、および大ジャンプ後の着地硬直中もここに入る
        SetBoolIfExists("isIdle", state == BossState.Idle);

        // SmallJump：跳び上がってから着地するまで。着地した瞬間に Idle へ戻る
        SetBoolIfExists("isSJump", state == BossState.SmallJump);

        // BigJump：跳び上がってプレイヤーの真上へ回り込み、滞空し終えるまで
        // 落下に転じた時点で Fall1 へ移るので、ここで false になる
        SetBoolIfExists("isBJump", state == BossState.BigJump);

        // Fall1：滞空が終わってから着地するまでの落下中
        SetBoolIfExists("isFall1", state == BossState.Fall1);

        // MoveJump：端へ移動するために跳び上がってから着地するまで
        // 上昇・上空の水平移動・下降のすべてを含み、着地した瞬間に Idle へ戻る
        SetBoolIfExists("isMJump", state == BossState.MoveJump);

        // ここから下は行動パターン③用。アニメーションの対応は別ブランチで行うため、
        // BossAnimationCtrl にはまだこれらのパラメータがない
        // HighJump：高く浮上してプレイヤーの頭上で滞空している間
        SetBoolIfExists("isHJump", state == BossState.HighJump);

        // Fall2：滞空が終わってから着地するまでの落下中（前動作を含む）
        SetBoolIfExists("isFall2", state == BossState.Fall2);

        // Stun：着地して横向きに倒れ、気絶している間
        SetBoolIfExists("isStun", state == BossState.Stun);
    }

    // -------------------------------------------------------
    // Animator にあるパラメータだけを設定する
    // 行動パターン③のパラメータは別ブランチで追加するまで存在せず、
    // そのまま SetBool すると毎フレーム警告が出てしまうため
    // -------------------------------------------------------
    void SetBoolIfExists(string paramName, bool value)
    {
        if (animBoolParams.Contains(paramName)) anim.SetBool(paramName, value);
    }

    // Animator.parameters は呼ぶたびに配列を作るので、起動時に一度だけ名前を控えておく
    void CacheAnimatorBoolParams()
    {
        if (anim == null) return;

        foreach (AnimatorControllerParameter param in anim.parameters)
        {
            if (param.type == AnimatorControllerParameterType.Bool) animBoolParams.Add(param.name);
        }
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BossCtrl : MonoBehaviour
{
    Transform player;
    Rigidbody2D rb;
    CircleCollider2D coll;
    Animator anim;
    SpriteRenderer sr;
    MainManager mainManager;   // Boss戦BGMの再生／停止を任せる。シーンに常駐しているものを探して持つ

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
    public float stunFallAngle = -90f;     // 気絶したときに倒れ込むZ角度。-90で進行方向側へ倒れる
    public float stunFallTime = 0.3f;      // 倒れ込み／起き上がりにかける時間

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

    [Header("登場演出")]
    // 煙とプレイヤーのロック、カメラの操作は BossSpawner が担当する。
    // ここではBoss自身の動き（下降 → 停止 → 登場モーション）だけを持つ
    public bool playIntro = true;         // trueならStartでは動き出さず、BossSpawnerがPlayIntroを回す
    // 出現位置から地面へ下りるときの速さ（1秒あたりに下がるユニット数）。
    // 重力は使わないので、この値がそのまま下降の速さになる。
    // スライダーにしてあるので、再生しながらドラッグして詰められる
    [Range(0.1f, 10f)]
    public float descendSpeed = 4f;

    public float descendMaxDistance = 100f; // 出現位置から真下にこの距離まで地面を探す
    public float beginTime = 3.5f;        // 着地してから戦闘開始までの間、登場モーション（Begin）を再生する時間

    [Header("撃破演出")]
    // 気絶中にHPが0になったら、その場で消さずにこの流れで撃破を見せる。
    // PreDeath（点滅しながら3秒）→ Death（1巡）→ 消滅 → Gem出現
    public float preDeathTime = 3f;      // PreDeathのまま点滅し続ける時間
    // Deathのクリップ1巡ぶんの長さ。Begin（beginTime）と同じく、クリップの長さを手で入れる。
    // Deathのクリップは Loop Time をオフにしておくこと
    public float deathTime = 1f;
    public float gemSpawnDelay = 0.3f;   // Bossの姿が消えてからGemを出すまでの間

    [Header("撃破後のGem")]
    public GameObject gemPrefab;                          // Gem.prefab をセットする
    public Vector2 gemSpawnPos = new Vector2(23.6f, 5f);  // 空中に出す位置
    public float gemGroundY = -1.7f;                      // 下ろしきったときのY
    public float gemDescendSpeed = 1.5f;                  // 1秒あたりに下がるユニット数

    [Header("被ダメージ演出")]
    public float damageBlinkTime = 1.5f;     // 踏まれてから点滅し続ける時間
    public float damageBlinkInterval = 0.08f;// 表示／非表示を切り替える間隔。小さいほど速く点滅する

    // BossDamage.mp3 を鳴らす AudioSource。Boss.prefab に付けたものをセットする。
    // 点滅している間ずっと鳴らすので、Loopをオン・Play On Awakeをオフにしておくこと
    public AudioSource damageSE;

    [Header("デバッグ")]
    public BossState state = BossState.Idle;

    // アニメーションの状態は必ずこの enum を正とし、Animator の bool は SyncAnimatorParams で導出する
    // （bool を個別に持つと isIdle と isSJump が同時に true になる不整合が起きうるため）
    public enum BossState { Idle, SmallJump, BigJump, Fall1, MoveJump, HighJump, Fall2, Stun, SpawnFall, Begin, PreDeath, Death }

    float defaultGravityScale;
    float facingY = 0f;                                            // 向きを表すY回転。0で右向き、180で左向き
    float stunAngle = 0f;                                          // 気絶時の倒れ込みを表すZ回転。平常時は0
    int hp;
    bool stompedInStun = false;                                    // 今回の気絶中にもう踏まれたか。気絶の待ち時間を打ち切るために使う
    List<GameObject> stunPlatforms = new List<GameObject>();       // 気絶中に出している足場。復帰時にまとめて消す
    Coroutine blinkRoutine;                                        // 実行中の被ダメージ点滅。踏み直されたら止めて回し直す

    // 気絶中か。プレイヤー側の踏みつけ判定で参照する
    public bool IsStunned => state == BossState.Stun;

    // プレイヤーのコライダーの底がこの高さ以上なら「Bossを上から踏んだ」とみなす。
    // Bossのコライダーは半径3の大きな円なので、transform.position では上下の判定ができない
    public float StompLineY => transform.position.y + coll.offset.y + coll.radius - stompTolerance;

    // 初期化はStartではなくAwakeで行う。
    // BossSpawnerはInstantiateしたその場で PlayIntro を呼ぶが、
    // Startは次のフレームまで実行されないため、Startで初期化していると
    // PlayIntroの中で rb がまだ null のままになってしまう
    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        coll = GetComponent<CircleCollider2D>();
        anim = GetComponent<Animator>();
        sr = GetComponent<SpriteRenderer>();
        player = GameObject.Find("Player").transform;
        mainManager = FindFirstObjectByType<MainManager>();
        defaultGravityScale = rb.gravityScale;
        hp = maxHp;

        rb.constraints = RigidbodyConstraints2D.FreezeRotation;

        // 登場演出があるときは、BossSpawnerがPlayIntroを回し終えてから
        // StartBattleを呼んでくれるので、ここでは動き出さない。
        // PlayIntroが呼ばれるまでの数フレームIdleが再生されないよう、先に状態を移しておく。
        // 重力も切っておく。PlayIntroが動き出す前の数フレームで落ち始めないようにする
        if (playIntro)
        {
            state = BossState.SpawnFall;
            rb.gravityScale = 0f;
            rb.linearVelocity = Vector2.zero;
        }
    }

    void Start()
    {
        // 登場演出なしでシーンに直接置いた場合だけ、そのまま戦闘を始める
        if (!playIntro) StartBattle();
    }

    // -------------------------------------------------------
    // 戦闘開始。行動①からのルーティンを回し始める
    // 登場演出があるときは BossSpawner から呼ばれる
    // -------------------------------------------------------
    public void StartBattle()
    {
        state = BossState.Idle;

        // 登場演出なしでシーンに直接置いた場合はここが最初のIdleになる。
        // 演出ありのときは PlayIntro の最後で既に鳴り始めているので、
        // MainManager 側で二重再生（頭出しのやり直し）を防いでいる
        if (mainManager != null) mainManager.PlayBossBGM();

        StartCoroutine(BossRoutine());
    }

    // -------------------------------------------------------
    // 登場演出のうち、Boss自身の動きの部分
    // 出現位置から等速で下降 → 着地したらそのまま登場モーション
    //
    // 下降を重力ではなく手で動かすのは、
    // ステージのどこに出現させても同じ速さでゆっくり下りてほしいため
    // -------------------------------------------------------
    public IEnumerator PlayIntro()
    {
        // 下降中も着地後の停止中も SpawnFall のまま
        state = BossState.SpawnFall;

        // 下降中は物理に任せず、Kinematicにして transform を直接動かす。
        // Dynamicのまま速度で下ろすと、地面に触れた時点で物理側が止めてしまい、
        // Box2Dが接触面に残すわずかな隙間のぶん目標より少し上で停止する。
        // その状態だと「目標の高さまで下りたか」の判定が永久に成立せず、
        // 地面の上に乗っているのにSpawnFallから抜けられなくなる
        rb.linearVelocity = Vector2.zero;
        rb.gravityScale = 0f;
        rb.bodyType = RigidbodyType2D.Kinematic;

        // 先に真下の地面を探して、着地するY座標を確定させておく。
        // IsGrounded のレイはコライダーの底から0.15下までしか届かないので、
        // 着地の判定にそれを使うと、真下に地面が無かったときにいつまでも抜けられず
        // 降り続けてしまう（しかも何のエラーも出ない）
        Vector3 origin = transform.position + (Vector3)coll.offset;
        RaycastHit2D groundHit = Physics2D.Raycast(origin, Vector2.down, descendMaxDistance, LayerMask.GetMask("Ground"));

        if (!groundHit)
        {
            // 地面が無いままだと下りる先が決まらないので、その場で演出を打ち切って戦闘に移る。
            // Kinematicのままだと宙に浮いて動かなくなるので、必ず元に戻しておく
            Debug.LogWarning($"BossCtrl: 出現位置 x={transform.position.x} の真下 {descendMaxDistance} 以内に Ground が見つかりません。BossSpawner の位置を地面の上へ移してください", this);
            RestorePhysics();
            yield break;
        }

        // コライダーの底が地面にちょうど接する高さ
        float targetY = groundHit.point.y + coll.radius - coll.offset.y;

        // targetY まで一定の速さで下ろす。
        // MoveTowards は行き過ぎずに必ず目標へ収束するので、
        // 物理の誤差で止まらなくなることがない。
        // 再生中にインスペクタで descendSpeed を動かせば、その場で速さが変わる
        while (transform.position.y > targetY)
        {
            float y = Mathf.MoveTowards(transform.position.y, targetY, descendSpeed * Time.deltaTime);
            transform.position = new Vector3(transform.position.x, y, transform.position.z);
            yield return null;
        }

        // 着地。物理を元に戻して、以降は通常どおり重力で地面に留まる
        RestorePhysics();

        // 着地したらそのまま戦闘開始直前の登場モーションへ移る。
        // 着地後の間の取りかたは Begin の再生時間（beginTime）で調整する
        state = BossState.Begin;
        yield return new WaitForSeconds(beginTime);

        // Begin を出し終えたらすぐ Idle に戻す。
        // このあと BossSpawner がカメラをプレイヤーへ戻し終えるまで待ってから
        // StartBattle を呼ぶので、ここで戻しておかないと
        // カメラが動いている間ずっと Begin のままになってしまう
        state = BossState.Idle;

        // 登場モーションを出し終えて最初にIdleになったこの瞬間からBGMを鳴らす。
        // カメラがプレイヤーへ戻り始めるのと同時に鳴り出し、
        // 実際に動き出す（StartBattle）ころには曲が立ち上がっている
        if (mainManager != null) mainManager.PlayBossBGM();
    }

    void Update()
    {
        // ゲームオーバー／クリアで時間が止まったら、ループ中の被ダメージSEを止める。
        // Time.timeScale = 0 でも音は止まらないうえ、点滅のコルーチンも
        // WaitForSeconds で待ったまま進まないので、放っておくと鳴り続けてしまう
        // （Update自体は timeScale = 0 でも呼ばれるのでここで面倒を見られる）
        if (Time.timeScale == 0f) StopDamageSE();

        if (player == null) return;

        FacePlayer();
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
    // 登場演出のあいだ切っていた物理を元に戻す
    // bodyType を戻すと constraints が外れることがあるので、あわせて入れ直す
    // -------------------------------------------------------
    void RestorePhysics()
    {
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        rb.linearVelocity = Vector2.zero;
        rb.gravityScale = defaultGravityScale;
    }

    // -------------------------------------------------------
    // 気絶：横向きに倒れて stunTime 秒動けなくなる
    // この間だけプレイヤーが上から踏んでダメージを与えられる（踏まれたら即座に復帰する）
    // 周りに足場を出して、プレイヤーがBossの頭上まで登れるようにする
    //
    // 倒れ込みは transform のZ回転で表現する。
    // Bossのコライダーは原点を中心とした円（offset 0 / 半径3）なので、
    // 回転軸と円の中心が一致しており、何度回しても当たり判定は1ミリも動かない。
    // アニメーションのクリップで回さないのは、Unityの回転カーブがx/y/zの3軸セットで、
    // Zだけを動かせずY（＝FacePlayerの向き）まで上書きしてしまうため
    // -------------------------------------------------------
    IEnumerator StunRoutine()
    {
        state = BossState.Stun;
        stompedInStun = false;

        SpawnStunPlatforms();

        // 横向きに倒れ込む
        float elapsed = 0f;
        while (elapsed < stunFallTime)
        {
            elapsed += Time.deltaTime;
            stunAngle = Mathf.Lerp(0f, stunFallAngle, elapsed / stunFallTime);
            yield return null;
        }
        stunAngle = stunFallAngle;

        // 倒れたまま気絶。踏まれたらその時点で打ち切る
        while (elapsed < stunTime && !stompedInStun)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        // 起き上がる
        float gettingUp = 0f;
        float fallenAngle = stunAngle;
        while (gettingUp < stunFallTime)
        {
            gettingUp += Time.deltaTime;
            stunAngle = Mathf.Lerp(fallenAngle, 0f, gettingUp / stunFallTime);
            yield return null;
        }
        stunAngle = 0f;

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
            // 撃破。行動のルーティン（BossRoutine）も気絶（StunRoutine）も
            // ここで打ち切って、撃破演出だけを回す。
            // StopAllCoroutines のあとに始めれば DeathRoutine 自身は巻き添えにならない
            StopAllCoroutines();
            StartCoroutine(DeathRoutine());
            return;
        }

        // ダメージが入ったことを見せる点滅。
        // 気絶の打ち切り → 起き上がり → 復帰後の静止 をまたいで続くので、
        // 行動のコルーチン（BossRoutine）とは独立して回す
        StartDamageBlink(damageBlinkTime, true);
    }

    // -------------------------------------------------------
    // 撃破演出
    // PreDeathで点滅しながら preDeathTime 秒 → Deathを1巡 → 姿を消す → Gem出現
    //
    // Stomped から StopAllCoroutines のあとに開始されるので、
    // 途中で他の行動や気絶の処理に割り込まれることはない
    // -------------------------------------------------------
    IEnumerator DeathRoutine()
    {
        // その場で動きを止める。重力はそのままなので地面の上に留まる
        rb.linearVelocity = Vector2.zero;

        // ここから先はプレイヤーがぶつかっても何も起きないようにする。
        // IsStunned が false になった瞬間から、瀕死のBossに触れただけで
        // ゲームオーバーになってしまうため（PlayerCrtl の Boss レイヤーの分岐）。
        // レイヤーごとではなく個体ごとに切るのは、登場演出やイーグルと同じ
        Collider2D playerColl = player != null ? player.GetComponent<Collider2D>() : null;
        if (playerColl != null) Physics2D.IgnoreCollision(coll, playerColl, true);

        // Bossを消すとコルーチンも止まるので、出しっぱなしの足場はここで片付ける
        DespawnStunPlatforms();

        // StopAllCoroutines で点滅が途中で止まっていた場合、
        // ループ再生中の被ダメージSEが鳴りっぱなしになるので念のため止めておく
        StopDamageSE();

        // ① PreDeath：倒れた姿勢を起こして、ダメージのときと同じように点滅させる。
        //    撃破のSEはまだ無いので、ここでは音を鳴らさない
        stunAngle = 0f;
        state = BossState.PreDeath;
        StartDamageBlink(preDeathTime, false);
        yield return new WaitForSeconds(preDeathTime);

        // ② Death：クリップを1巡ぶん再生する。
        //    点滅は必ずここで止める。放っておくと切り替えの間隔ぶんだけ
        //    Deathに入ってからも点滅が続いてしまう
        StopDamageBlink();
        state = BossState.Death;
        yield return new WaitForSeconds(deathTime);

        // ③ 姿を消す。GameObject の Destroy はGemを出したあとだが、
        //    見た目はここで消えるので「Bossが消滅してからGemが出る」順序になる
        if (sr != null) sr.enabled = false;
        yield return new WaitForSeconds(gemSpawnDelay);

        // ④ 撃破後のBGMに切り替える。
        //    AudioSourceはBoss本体ではなくMainManagerに置いてあるので、
        //    このあと Destroy されてもGemを取るまで鳴り続けられる
        if (mainManager != null) mainManager.PlayClearBossBGM();

        // ⑤ クリア用のGemを空中に出す。降下はGem側（GemCtrl）が続けるので、
        //    このあとBossが消えても止まらない
        SpawnGem();

        Destroy(gameObject);
    }

    // -------------------------------------------------------
    // Bossを倒したときにクリア用のGemを出す
    // gemSpawnPos の空中に出してから gemGroundY までゆっくり下ろす。
    // 降下中もコライダーは生きているので、プレイヤーが跳んで触れればその時点でクリアになる
    // -------------------------------------------------------
    void SpawnGem()
    {
        if (gemPrefab == null) return;

        GameObject gem = Instantiate(gemPrefab, gemSpawnPos, Quaternion.identity);

        GemCtrl ctrl = gem.GetComponent<GemCtrl>();
        if (ctrl != null) ctrl.StartDescend(gemGroundY, gemDescendSpeed);
    }

    // -------------------------------------------------------
    // 点滅を開始する
    // 点滅中にもう一度呼ばれた場合は、前の点滅を止めてから回し直す
    // duration：点滅し続ける時間。被ダメージと撃破（PreDeath）で長さが違う
    // playSE   ：被ダメージSEを鳴らすか。撃破演出では鳴らさない
    // -------------------------------------------------------
    void StartDamageBlink(float duration, bool playSE)
    {
        StopDamageBlink();
        blinkRoutine = StartCoroutine(DamageBlinkRoutine(duration, playSE));
    }

    // -------------------------------------------------------
    // 実行中の点滅を止める
    // 途中で止めてもスプライトが消えたままにならないよう、必ず表示に戻す
    // -------------------------------------------------------
    void StopDamageBlink()
    {
        if (blinkRoutine != null) StopCoroutine(blinkRoutine);
        blinkRoutine = null;

        if (sr != null) sr.enabled = true;
    }

    // -------------------------------------------------------
    // duration 秒のあいだ、スプライトの表示／非表示を繰り返す
    //
    // 既存のクリップは m_Sprite しか動かしていないので、SpriteRenderer の enabled を
    // ここで切り替えてもAnimatorと取り合いにならない。
    // Animatorのレイヤーは同時に1つのstateしか再生できず、点滅用のstateを作ると
    // その間モーションが止まってしまうため、演出はアニメーションではなくコードで持つ
    // -------------------------------------------------------
    IEnumerator DamageBlinkRoutine(float duration, bool playSE)
    {
        // 0以下だと切り替えが進まず無限ループになるので下限を設ける
        float interval = Mathf.Max(0.02f, damageBlinkInterval);

        // 点滅と同じ長さだけ鳴らす。SE（0.4秒ほど）のほうが点滅より短いので、
        // AudioSource側のLoopをオンにしておき、点滅を終えるときにここで止める
        if (playSE && damageSE != null) damageSE.Play();

        for (float elapsed = 0f; elapsed < duration; elapsed += interval)
        {
            if (sr != null) sr.enabled = !sr.enabled;
            yield return new WaitForSeconds(interval);
        }

        // 何回切り替えて終わっても、必ず表示された状態で終える
        if (sr != null) sr.enabled = true;
        StopDamageSE();
        blinkRoutine = null;
    }

    // -------------------------------------------------------
    // 被ダメージSEを止める
    // 点滅を終えたときのほか、Update からゲームが止まったときにも呼ぶ
    // -------------------------------------------------------
    void StopDamageSE()
    {
        if (damageSE != null && damageSE.isPlaying) damageSE.Stop();
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
    // 向き（Y）と気絶時の倒れ込み（Z）をここで合成する。
    // 気絶中は向き直らないので、倒れ込む直前に向いていた方向のまま倒れる
    // 降りてくる間（SpawnFall）はプレイヤーの方を向く。
    // 着地後の登場モーション（Begin）では向き直らないので、降りきったときの向きのまま構える
    // -------------------------------------------------------
    void FacePlayer()
    {
        // 撃破演出中（PreDeath／Death）も向き直らない。倒れる直前の向きのまま最期を迎える
        if (state != BossState.Stun && state != BossState.Begin
            && state != BossState.PreDeath && state != BossState.Death)
            facingY = player.position.x > transform.position.x ? 0f : 180f;

        transform.rotation = Quaternion.Euler(0f, facingY, stunAngle);
    }

    // -------------------------------------------------------
    // Animator へ state を反映する
    // 行動パターン①で使うのは isIdle / isSJump / isBJump / isFall1
    // 行動パターン②では端への移動中に isMJump を使う
    // 行動パターン③では isBJump2 / isFall2 / isStun を使う
    // -------------------------------------------------------
    void SyncAnimatorParams()
    {
        if (anim == null) return;

        // Idle：ジャンプモーションも落下モーションもしていない状態
        // 小ジャンプの着地ごと、および大ジャンプ後の着地硬直中もここに入る
        // 気絶から復帰したあとの静止（stunEndIdleTime）中もここに入る
        anim.SetBool("isIdle", state == BossState.Idle);

        // SmallJump：跳び上がってから着地するまで。着地した瞬間に Idle へ戻る
        anim.SetBool("isSJump", state == BossState.SmallJump);

        // BigJump：跳び上がってプレイヤーの真上へ回り込み、滞空し終えるまで
        // 落下に転じた時点で Fall1 へ移るので、ここで false になる
        anim.SetBool("isBJump", state == BossState.BigJump);

        // Fall1：滞空が終わってから着地するまでの落下中
        anim.SetBool("isFall1", state == BossState.Fall1);

        // MoveJump：端へ移動するために跳び上がってから着地するまで
        // 上昇・上空の水平移動・下降のすべてを含み、着地した瞬間に Idle へ戻る
        anim.SetBool("isMJump", state == BossState.MoveJump);

        // BigJump2：③で高く跳び上がり、プレイヤーの頭上で滞空し終えるまで
        // ①のBigJumpと同じ跳び方の強化版。落下に転じた時点で Fall2 へ移る
        // （state 名は HighJump。跳ぶ強さを決めるのは highJumpPowerY）
        anim.SetBool("isBJump2", state == BossState.HighJump);

        // Fall2：滞空が終わってから着地するまでの落下中（前動作を含む）
        anim.SetBool("isFall2", state == BossState.Fall2);

        // Stun：着地して横向きに倒れ、気絶している間
        // 倒れ込みと起き上がりのZ回転は FacePlayer が担当するので、
        // クリップ側は倒れているポーズのスプライトだけを持てばよい
        anim.SetBool("isStun", state == BossState.Stun);

        // SpawnFall：登場演出で出現してから地面に下りきるまで
        // 着地した瞬間に Begin へ移る
        anim.SetBool("isSpawnFall", state == BossState.SpawnFall);

        // Begin：着地してから戦闘開始までの登場モーション
        // 再生し終わると Idle に移り、行動①からのルーティンが始まる
        anim.SetBool("isBegin", state == BossState.Begin);

        // PreDeath：気絶中にHPが0になってから、Deathに移るまで
        // Stun からここへ移った時点で isStun は false になるので、
        // コントローラ側の遷移条件は isPreDeath だけでよい
        anim.SetBool("isPreDeath", state == BossState.PreDeath);

        // Death：撃破のモーション。1巡ぶん再生し終わるとBossが消える
        anim.SetBool("isDeath", state == BossState.Death);
    }
}

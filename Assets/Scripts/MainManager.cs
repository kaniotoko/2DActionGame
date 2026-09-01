using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainManager : MonoBehaviour
{
    public GameObject[] stages;
    public bool[] stageShowBG;
    public int stageNumber;
    public GameObject gameOverView;
    public GameObject gameClearView;
    public AudioSource deathSE;
    public AudioSource goalSE;

    [Header("BGM")]
    // どのBGMを鳴らすかは、どのAudioSourceも Play On Awake をオフにして
    // すべてこの MainManager から切り替える。
    // Play On Awake に任せると、ステージ10で鳴らしたくない曲が
    // 消すまでの数フレームだけ鳴ってしまう
    public AudioSource stageBGM;    // 通常ステージのBGM。Main Camera の子の BGM オブジェクトをセットする
    public AudioSource preBossBGM;  // Boss戦前.mp3。ステージ10に入ってからBossが現れるまで
    public AudioSource bossBGM;     // Boss戦.mp3。Bossが動き出してから撃破するまで

    // Boss戦のステージ番号（0始まり）。stages[] の添字と同じなので、ステージ10なら9。
    // このステージだけ stageBGM ではなく preBossBGM から始める
    public int bossStageNumber = 9;

    [Header("エンディング")]
    // 最終ステージをクリアしたときに読み込むシーン。Build Settings に登録しておくこと
    public string finishSceneName = "FinishScene";
    // ゴールSEを聞かせてからエンディングに切り替えるまでの待ち時間（秒）
    public float finishSceneDelay = 2f;

    void Start()
    {
        Time.timeScale = 1;

        // これを追加！ これでTime.timeScale = 0でも音が止まりません
        if (deathSE != null)
        {
            deathSE.ignoreListenerPause = true;
        }

        stageNumber = PlayerPrefs.GetInt("TryStage");
        Instantiate(stages[stageNumber]);

        // stageShowBG が true のステージは通常の背景(BG)、
        // false のステージは薄暗い背景(DarkBG)に切り替える
        bool showBG = stageNumber >= stageShowBG.Length || stageShowBG[stageNumber];

        var bg = Camera.main.transform.Find("BG");
        if (bg != null) bg.gameObject.SetActive(showBG);

        var darkBG = Camera.main.transform.Find("DarkBG");
        if (darkBG != null) darkBG.gameObject.SetActive(!showBG);

        PlayStageStartBGM();
    }

    void Update()
    {
        
    }

    // -------------------------------------------------------
    // ステージに入ったときのBGM
    // Boss戦のステージだけ Boss戦前.mp3、それ以外は通常のステージBGMを鳴らす
    // -------------------------------------------------------
    void PlayStageStartBGM()
    {
        AudioSource bgm = stageNumber == bossStageNumber ? preBossBGM : stageBGM;

        if (bgm == null) return;

        bgm.loop = true;
        bgm.Play();
    }

    // -------------------------------------------------------
    // Boss戦のBGMに切り替える（BossCtrl から呼ばれる）
    // 鳴っている最中に呼ばれても頭出しし直さないようにしておく
    // -------------------------------------------------------
    public void PlayBossBGM()
    {
        if (bossBGM == null || bossBGM.isPlaying) return;

        // それまで鳴っていたBGMと重なってしまうので、先に止める。
        // 鳴っていないほうを止めても何も起きないので、両方まとめて止めてよい
        StopBGM(stageBGM);
        StopBGM(preBossBGM);

        bossBGM.loop = true;
        bossBGM.Play();
    }

    // -------------------------------------------------------
    // Boss戦のBGMを止める
    // 撃破時（BossCtrl）のほか、ゲームオーバー／クリアからも呼ばれる。
    // 撃破後はとりあえず無音にしたいので、ここで別のBGMを鳴らし直すことはしない
    // -------------------------------------------------------
    public void StopBossBGM()
    {
        StopBGM(bossBGM);
    }

    // -------------------------------------------------------
    // AudioSource が未設定でも落ちないようにするための小道具
    // -------------------------------------------------------
    void StopBGM(AudioSource bgm)
    {
        if (bgm != null) bgm.Stop();
    }

    public void GameOver()
    {
        StopBossBGM();
        deathSE.Play();
        gameOverView.SetActive(true);
        Time.timeScale = 0; //ゲーム内の時間を止める
    }

    public void GameClear()
    {
        StopBossBGM();
        goalSE.Play();
        Time.timeScale = 0;
        if(stageNumber > PlayerPrefs.GetInt("ClearStage"))
        {
            PlayerPrefs.SetInt("ClearStage", stageNumber); //PlayerPrefsは端末にデータを保存してくれる(ClearStageという名前で、stageNumberに保存してくれる)
        }

        // 最終ステージには「つぎへ」の行き先がないので、
        // クリア画面を出さずにそのままエンディングへ送る
        if (IsFinalStage())
        {
            StartCoroutine(LoadFinishScene());
            return;
        }

        gameClearView.SetActive(true);
    }

    // -------------------------------------------------------
    // いま遊んでいるのが stages[] の最後のステージかどうか
    // -------------------------------------------------------
    bool IsFinalStage()
    {
        return stageNumber >= stages.Length - 1;
    }

    // -------------------------------------------------------
    // ゴールSEを少し聞かせてからエンディングへ
    // GameClear() で Time.timeScale = 0 にしているので、
    // その影響を受けない WaitForSecondsRealtime で待つ
    // -------------------------------------------------------
    IEnumerator LoadFinishScene()
    {
        yield return new WaitForSecondsRealtime(finishSceneDelay);
        Time.timeScale = 1;
        SceneManager.LoadScene(finishSceneName);
    }

    public void Retry()
    {
        PlayerPrefs.SetInt("TryStage", stageNumber);
        LoadMainScene();
    }

    public void Next()
    {
        // 最終ステージでは押せないUIだが、押されても stages[] の範囲外を
        // 読みに行かないようにエンディングへ逃がしておく
        if (IsFinalStage())
        {
            Time.timeScale = 1;
            SceneManager.LoadScene(finishSceneName);
            return;
        }

        PlayerPrefs.SetInt("TryStage", stageNumber + 1);
        LoadMainScene();
    }

    public void LoadMainScene()
    {
        SceneManager.LoadScene("MainScene");
    }

    public void LoadStartScene()
    {
        SceneManager.LoadScene("StartScene");
    }
}

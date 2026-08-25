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
    }

    void Update()
    {
        
    }

    public void GameOver()
    {
        deathSE.Play();
        gameOverView.SetActive(true);
        Time.timeScale = 0; //ゲーム内の時間を止める
    }
    
    public void GameClear()
    {
        goalSE.Play();
        gameClearView.SetActive(true);
        Time.timeScale = 0;
        if(stageNumber > PlayerPrefs.GetInt("ClearStage"))
        {
            PlayerPrefs.SetInt("ClearStage", stageNumber); //PlayerPrefsは端末にデータを保存してくれる(ClearStageという名前で、stageNumberに保存してくれる)
        }
    }

    public void Retry()
    {
        PlayerPrefs.SetInt("TryStage", stageNumber);
        LoadMainScene();
    }

    public void Next()
    {
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

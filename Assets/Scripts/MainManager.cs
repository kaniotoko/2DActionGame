using UnityEngine;
using UnityEngine.SceneManagement;

public class MainManager : MonoBehaviour
{
    public GameObject[] stages;
    public int stageNumber;
    public GameObject gameOverView;
    public GameObject gameClearView;

    void Start()
    {
        Time.timeScale = 1;

        stageNumber = PlayerPrefs.GetInt("TryStage");
        Instantiate(stages[stageNumber]); //Instantiateでstageを出現させる
    }

    void Update()
    {
        
    }

    public void GameOver()
    {
        gameOverView.SetActive(true);
        Time.timeScale = 0; //ゲーム内の時間を止める
    }
    
    public void GameClear()
    {
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

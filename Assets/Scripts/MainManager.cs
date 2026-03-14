using UnityEngine;

public class MainManager : MonoBehaviour
{
    public GameObject gameOverView;
    public GameObject gameClearView;

    void Start()
    {
        
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
    }
}

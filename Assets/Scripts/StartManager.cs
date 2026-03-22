using UnityEngine;
using UnityEngine.SceneManagement;

public class StartManager : MonoBehaviour
{
    public Transform content;
    public GameObject stageButtons;
    public int stageCount;

    void Start()
    {
        Time.timeScale = 1;

        for (int i = 0; i < stageCount; i+= 2)
        {
            StageButtonsCtrl newStageButton = Instantiate(stageButtons, content).GetComponent<StageButtonsCtrl>();
            newStageButton.stageNumber1 = i;
            newStageButton.stageNumber2 = i + 1;
            newStageButton.stageText1.text = "ステージ" + (i+1);
            newStageButton.stageText2.text = "ステージ" + (i+2);
        }
    }

    void Update()
    {
        
    }

    public void LoadMainScene()
    {
        SceneManager.LoadScene("MainScene");
    }
}

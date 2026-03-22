using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class StageButtonsCtrl : MonoBehaviour
{

    public int stageNumber1;
    public int stageNumber2;
    public TextMeshProUGUI stageText1;
    public TextMeshProUGUI stageText2;

    public void TryStage1()
    {
        if(stageNumber1 <= PlayerPrefs.GetInt("ClearStage")+1)
        {
            PlayerPrefs.SetInt("TryStage", stageNumber1);
            LoadMainScene();
        }
    }

    public void TryStage2()
    {
        if(stageNumber2 <= PlayerPrefs.GetInt("ClearStage")+1)
        {
            PlayerPrefs.SetInt("TryStage", stageNumber2);
            LoadMainScene();
        }
    }

    public void LoadMainScene()
    {
        SceneManager.LoadScene("MainScene");
    }
}

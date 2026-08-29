using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

// FinishScene（エンディング）に置く。
// 「Thank you for playing」を見せて、クリック／キー入力でタイトルに戻す
public class FinishCtrl : MonoBehaviour
{
    // 表示した瞬間の押しっぱなしでスキップされないように、この秒数は入力を受け付けない
    public float inputDelay = 1.5f;
    // 「クリックでタイトルへ」の案内テキスト。inputDelay を過ぎてから表示する。
    // 未設定でも動くので、案内がいらなければ空のままでよい
    public GameObject pressAnyKeyText;

    float elapsed;

    void Start()
    {
        // MainScene 側で 0 にした時間が残っている場合があるので戻しておく
        Time.timeScale = 1;

        if (pressAnyKeyText != null) pressAnyKeyText.SetActive(false);
    }

    void Update()
    {
        elapsed += Time.deltaTime;
        if (elapsed < inputDelay) return;

        if (pressAnyKeyText != null && !pressAnyKeyText.activeSelf)
        {
            pressAnyKeyText.SetActive(true);
        }

        if (IsAnyInput())
        {
            LoadStartScene();
        }
    }

    // このプロジェクトは New Input System のみ有効なので、
    // 旧APIの Input.anyKeyDown は実行時に例外になる。使わないこと
    bool IsAnyInput()
    {
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) return true;
        if (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame) return true;

        return false;
    }

    // ボタンの OnClick から呼びたくなったとき用に public にしてある
    public void LoadStartScene()
    {
        SceneManager.LoadScene("StartScene");
    }
}

using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

// FinishScene（エンディング）に置く。
// 「Thank you for playing」を見せて、クリック／キー入力でタイトルに戻す
public class FinishCtrl : MonoBehaviour
{
    // 案内テキストが出てから入力を受け付けるまでの秒数。
    // 表示した瞬間の押しっぱなしでスキップされるのを防ぐ
    public float inputDelay = 0.5f;
    // 「クリックでタイトルへ」の案内テキスト。
    // 表示のタイミングは ThanksText の SlideInCtrl が管理しているので、
    // ここでは「表示されたか」を入力受付の合図として見るだけ。
    // 未設定でも動くので、案内がいらなければ空のままでよい
    public GameObject pressAnyKeyText;

    float elapsed;

    void Start()
    {
        // MainScene 側で 0 にした時間が残っている場合があるので戻しておく
        Time.timeScale = 1;
    }

    void Update()
    {
        // ThanksText が降りきって案内テキストが表示されるまでは受け付けない
        if (pressAnyKeyText != null && !pressAnyKeyText.activeSelf) return;

        elapsed += Time.deltaTime;
        if (elapsed < inputDelay) return;

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

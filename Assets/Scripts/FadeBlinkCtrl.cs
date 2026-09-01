using UnityEngine;
using UnityEngine.UI;

// UI要素の透明度をゆっくり上下させて、じんわり点滅させる。
// FinishScene の PressAnyKeyText のような「押してください」表示に付ける想定。
//
// CanvasGroup 経由だと反映されないことがあったので、
// Graphic（TextMeshProUGUI や Image の共通の親）の色のアルファを直接書き換えている
public class FadeBlinkCtrl : MonoBehaviour
{
    // 1往復（暗い→明るい→暗い）にかかる秒数。大きいほどゆっくり
    public float cycleDuration = 2f;
    // 一番暗いときの透明度。0にすると完全に消える
    [Range(0f, 1f)] public float minAlpha = 0.3f;
    // 一番明るいときの透明度
    [Range(0f, 1f)] public float maxAlpha = 1f;

    Graphic graphic;
    float elapsed;

    // FinishCtrl が SetActive(true) で表示し直すので、
    // そのたびに必ず一番暗い状態から始まるようにリセットしておく
    void OnEnable()
    {
        elapsed = 0f;
        Apply();
    }

    void Update()
    {
        elapsed += Time.unscaledDeltaTime;
        Apply();
    }

    void Apply()
    {
        if (cycleDuration <= 0f) return;

        // Awake ではなくここで取るのは、コンポーネントを後から足した場合でも
        // 取りこぼさないようにするため
        if (graphic == null)
        {
            graphic = GetComponent<Graphic>();
            if (graphic == null)
            {
                Debug.LogWarning("FadeBlinkCtrl: 同じオブジェクトに Text や Image がありません", this);
                enabled = false;
                return;
            }
        }

        // Sin波なので折り返しがなめらか。
        // -π/2 ずらしてあるので elapsed = 0 のとき minAlpha から始まる
        float wave = Mathf.Sin(elapsed / cycleDuration * Mathf.PI * 2f - Mathf.PI * 0.5f);
        float t = (wave + 1f) * 0.5f;

        Color color = graphic.color;
        color.a = Mathf.Lerp(minAlpha, maxAlpha, t);
        graphic.color = color;
    }
}

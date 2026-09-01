using UnityEngine;

// RectTransform を画面外から「いま置いてある位置」まで滑り込ませる。
// FinishScene の ThanksText に付けて、降りきったら AuthorText などを表示する想定。
//
// ゴール座標はコードに書かず、Inspector で配置した位置をそのまま使う。
// レイアウトを変えたくなったらシーン上で動かすだけでよい
[RequireComponent(typeof(RectTransform))]
public class SlideInCtrl : MonoBehaviour
{
    [Header("動き")]
    // シーン開始からスライドを始めるまでの待ち時間（秒）
    public float startDelay = 0.3f;
    // 降りきるまでにかかる秒数。大きいほどゆっくり
    public float duration = 1.5f;
    // 開始位置を本来の位置から何ピクセル上にずらすか。
    // Canvas の基準の高さ(600)より大きくしておけば確実に画面外から始まる
    public float startOffsetY = 600f;

    [Header("降りきった後に表示するもの")]
    // ここに入れたオブジェクトはシーン開始時に隠され、スライド完了時に表示される
    public GameObject[] activateOnComplete;

    // スライドが終わったかどうか。他のスクリプトから読めるようにしてある
    public bool IsFinished { get; private set; }

    RectTransform rect;
    Vector2 targetPos;  // Inspector で配置した本来の位置（ゴール）
    Vector2 startPos;   // 画面外の開始位置
    float elapsed;

    // Start ではなく Awake で初期化する。
    // すべての Awake は すべての Start より先に走るので、
    // 元の位置に表示されたフレームが一瞬映る、という事故を防げる
    void Awake()
    {
        rect = GetComponent<RectTransform>();

        targetPos = rect.anchoredPosition;
        startPos = targetPos + Vector2.up * startOffsetY;
        rect.anchoredPosition = startPos;

        SetActiveAll(false);
    }

    void Update()
    {
        if (IsFinished) return;

        // Time.timeScale の影響を受けないようにしておく。
        // MainScene 側で 0 にした時間が戻る前でも確実に動く
        elapsed += Time.unscaledDeltaTime;

        if (elapsed < startDelay) return;

        // duration が 0 のときはゼロ除算になるので、即座に完了扱いにする
        float t = duration <= 0f ? 1f : (elapsed - startDelay) / duration;

        if (t >= 1f)
        {
            rect.anchoredPosition = targetPos;
            IsFinished = true;
            SetActiveAll(true);
            return;
        }

        // イーズアウト（最初は速く、止まる直前でゆっくり）。
        // 等速だと目的地でピタッと止まって機械的に見える
        float eased = 1f - Mathf.Pow(1f - t, 3f);
        rect.anchoredPosition = Vector2.Lerp(startPos, targetPos, eased);
    }

    void SetActiveAll(bool active)
    {
        if (activateOnComplete == null) return;

        foreach (GameObject obj in activateOnComplete)
        {
            if (obj != null) obj.SetActive(active);
        }
    }
}

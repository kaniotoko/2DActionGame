using UnityEngine;

public class CameraCtrl : MonoBehaviour
{
    public Transform player; //Transform型関数では位置情報を扱う
    Vector3 startPos; //最初の座標

    Transform target;      //いま追尾している対象。通常はplayer
    Vector3 blendStartPos; //ブレンドを始めたときのカメラの位置
    float blendTime = 0f;  //ブレンドにかける時間。0なら即時追尾
    float blendElapsed = 0f;

    void Start()
    {
        startPos = transform.position;
        target = player;
    }

    // -------------------------------------------------------
    // 追尾先を切り替える
    // blendTime を指定すると、その秒数をかけて現在の位置から新しい追尾先へ滑らかに寄る。
    // 0を渡せば従来どおり即座に追尾先へ飛ぶ
    // -------------------------------------------------------
    public void SetTarget(Transform newTarget, float blendTime)
    {
        if (newTarget == null) return;

        target = newTarget;
        StartBlend(blendTime);
    }

    // -------------------------------------------------------
    // プレイヤーの追尾に戻す
    // -------------------------------------------------------
    public void ResetTarget(float blendTime)
    {
        target = player;
        StartBlend(blendTime);
    }

    void StartBlend(float time)
    {
        blendStartPos = transform.position;
        blendTime = time;
        blendElapsed = 0f;
    }

    void Update()
    {
        if (target == null) return;

        Vector3 dest = new Vector3(target.position.x, target.position.y, startPos.z);

        if (blendElapsed < blendTime)
        {
            blendElapsed += Time.deltaTime;

            // 開始位置から目標へ SmoothStep で補間する。
            // 目標（追尾先）は動き続けるが、開始位置は固定なので
            // 時間が経つほど確実に目標へ寄っていき、blendTime でぴったり重なる
            float t = Mathf.Clamp01(blendElapsed / blendTime);
            transform.position = Vector3.Lerp(blendStartPos, dest, Mathf.SmoothStep(0f, 1f, t));
            return;
        }

        transform.position = dest;
    }
}

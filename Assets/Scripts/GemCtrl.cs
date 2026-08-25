using UnityEngine;

public class GemCtrl : MonoBehaviour
{
    [Header("降下設定")]
    public float descendSpeed = 1.5f;   // 1秒あたりに下がるユニット数

    float targetY;
    bool descending = false;

    // -------------------------------------------------------
    // Bossを倒したときなど、空中に出したGemをゆっくり下ろしたいときに外部から呼ぶ
    //
    // 降下は Rigidbody2D の重力ではなく transform を直接動かして行う。
    // Gemはコライダーがトリガーなので地面に乗せて止めることができず、
    // 「一定の速さで下ろして狙った高さでぴったり止める」には手で動かすほうが確実なため
    // -------------------------------------------------------
    public void StartDescend(float targetY, float speed)
    {
        this.targetY = targetY;
        if (speed > 0f) descendSpeed = speed;
        descending = true;
    }

    // -------------------------------------------------------
    // StartDescend が呼ばれるまでは何もしないので、
    // ステージにあらかじめ置いてあるGemに付けても挙動は変わらない
    // -------------------------------------------------------
    void Update()
    {
        if (!descending) return;

        float y = Mathf.MoveTowards(transform.position.y, targetY, descendSpeed * Time.deltaTime);
        transform.position = new Vector3(transform.position.x, y, transform.position.z);

        if (Mathf.Approximately(y, targetY)) descending = false;
    }
}

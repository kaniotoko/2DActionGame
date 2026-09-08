using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class WaveManager : MonoBehaviour
{
    [System.Serializable]
    public class SpawnData
    {
        public GameObject enemyPrefab; // 出現させる敵
        public Vector2 spawnPosition;  // 出現位置
        public float spawnTime;        // 開始何秒後に出現させるか
        public float duration;         // 何秒間存在するか
    }

    public List<SpawnData> waves;    // 出現データのリスト
    public GameObject gemPrefab;     // クリアアイテム
    public Vector2 gemSpawnPos;      // Gemの出現位置

    [Header("登場演出")]
    public GameObject smokePrefab;   // 敵が出てくる前に出現位置へ出す煙。WaveSpawnSmokeプレハブをセットする
    public float smokeTime = 0.5f;   // 煙を出してから敵を出現させるまでの時間

    private int spawnCount = 0;
    private int totalSpawned = 12;

    void Start()
    {
        totalSpawned = waves.Count;
        foreach (var data in waves)
        {
            StartCoroutine(SpawnRoutine(data));
        }
    }

    IEnumerator SpawnRoutine(SpawnData data)
    {
        // 煙はspawnTimeちょうどではなく、その手前から出す。
        // こうすると敵が出てくる時刻はこれまでどおりspawnTimeのままなので、
        // 演出を足しても12ウェーブ全体の間合いやGem出現までの時間が変わらない。
        // spawnTimeがsmokeTimeより短いデータでも待ち時間がマイナスにならないよう、
        // 煙の長さはspawnTimeを上限にする
        float smokeDelay = smokePrefab != null ? Mathf.Min(smokeTime, data.spawnTime) : 0f;

        // 指定の時間まで待つ（煙を出す分だけ手前で起きる）
        yield return new WaitForSeconds(data.spawnTime - smokeDelay);

        // 煙を出して、消えたところに敵が現れるようにする
        if (smokePrefab != null)
        {
            GameObject smoke = Instantiate(smokePrefab, data.spawnPosition, Quaternion.identity);
            yield return new WaitForSeconds(smokeDelay);
            Destroy(smoke);
        }

        // 敵を生成
        GameObject enemy = Instantiate(data.enemyPrefab, data.spawnPosition, Quaternion.identity);

        // 一定時間後に消滅させる（Playerに倒されていなければ）
        Destroy(enemy, data.duration);

        // 全部の敵の処理（出現）が終わったかチェック
        spawnCount++;
        if (spawnCount >= totalSpawned)
        {
            // 全ての敵が出し切られた後の処理
            Invoke("SpawnGem", 22.0f);
        }
    }

    void SpawnGem()
    {
        Instantiate(gemPrefab, gemSpawnPos, Quaternion.identity);
        Debug.Log("Gem出現！クリア可能です！");
    }
}
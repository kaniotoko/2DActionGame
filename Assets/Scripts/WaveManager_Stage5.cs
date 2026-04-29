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
    
    private int spawnCount = 0;
    private int totalSpawned = 0;

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
        // 指定の時間まで待つ
        yield return new WaitForSeconds(data.spawnTime);

        // 敵を生成
        GameObject enemy = Instantiate(data.enemyPrefab, data.spawnPosition, Quaternion.identity);
        
        // 一定時間後に消滅させる（Playerに倒されていなければ）
        Destroy(enemy, data.duration);

        // 全部の敵の処理（出現）が終わったかチェック
        spawnCount++;
        if (spawnCount >= totalSpawned)
        {
            // 全ての敵が出し切られた後の処理（例：5秒後にGem出現）
            Invoke("SpawnGem", 5.0f);
        }
    }

    void SpawnGem()
    {
        Instantiate(gemPrefab, gemSpawnPos, Quaternion.identity);
        Debug.Log("Gem出現！クリア可能です！");
    }
}
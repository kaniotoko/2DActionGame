using UnityEngine;

public class BatSpawner : MonoBehaviour
{
    public GameObject batPrefab;
    private float spawnDistance = 30f; // プレイヤーがどのくらい近づいたら出すか
    private float despawnDistance = 30f; // プレイヤーがどのくらい離れたら消すか
    private bool hasSpawned = false;
    public Transform player;
    private GameObject spawnedBat;

    void Start()
    {
        player = GameObject.Find("Player").transform; // シーンに出現された時Playerというオブジェクトを代入する
    }

    void Update()
    {
        if (!hasSpawned && Vector2.Distance(transform.position, player.position) < spawnDistance)
        {
            Spawn();
        }
        else if (hasSpawned && Vector2.Distance(transform.position, player.position) >= despawnDistance)
        {
            Despawn();
        }
    }

    void Spawn()
    {
        spawnedBat = Instantiate(batPrefab, transform.position, Quaternion.identity);// プレハブを生成
        hasSpawned = true;
    }

    void Despawn()
    {
        // オブジェクトが存在すれば削除
        if (spawnedBat != null)
        {
            Destroy(spawnedBat);
        }
        // 再度近づいた時に生成されるようフラグをリセット
        hasSpawned = false;
    }
}

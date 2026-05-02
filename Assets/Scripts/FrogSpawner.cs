using UnityEngine;

public class Spawner : MonoBehaviour
{
    public GameObject frogPrefab;
    private float spawnDistance = 30f; // プレイヤーがどのくらい近づいたら出すか
    private bool hasSpawned = false;
    public Transform player;

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
    }

    void Spawn()
    {
        Instantiate(frogPrefab, transform.position, Quaternion.identity);// プレハブを生成
        hasSpawned = true;
    }
}

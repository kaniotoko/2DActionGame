using UnityEngine;

public class BearSpawner : MonoBehaviour
{
    public GameObject bearPrefab;
    public float spawnDistance = 10f; // プレイヤーがどのくらい近づいたら出すか
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
        Instantiate(bearPrefab, transform.position, Quaternion.identity);// プレハブを生成
        hasSpawned = true;
    }
}

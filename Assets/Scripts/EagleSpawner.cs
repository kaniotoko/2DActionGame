using UnityEngine;
using UnityEngine.InputSystem;

public class EagleSpawner : MonoBehaviour
{
    public GameObject eaglePrefab;
    private float spawnDistance = 30f; // プレイヤーがどのくらい近づいたら出すか
    private float despawnDistance = 50f; // プレイヤーがどのくらい離れたら消すか
    private bool hasSpawned = false;
    public Transform player;
    private GameObject spawnedEagle;

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
        spawnedEagle = Instantiate(eaglePrefab, transform.position, Quaternion.identity);// プレハブを生成
        hasSpawned = true;
    }

    void Despawn()
    {
        // オブジェクトが存在すれば削除
        if (spawnedEagle != null)
        {
            Destroy(spawnedEagle);
        }
        // 再度近づいた時に生成されるようフラグをリセット
        hasSpawned = false;
    }
}

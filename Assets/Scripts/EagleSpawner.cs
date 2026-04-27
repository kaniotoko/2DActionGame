using UnityEngine;
using UnityEngine.InputSystem;

public class EagleSpawner : MonoBehaviour
{
    public GameObject eaglePrefab;
    public float spawnDistance = 15f; // プレイヤーがどのくらい近づいたら出すか
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
        Instantiate(eaglePrefab, transform.position, Quaternion.identity);// プレハブを生成
        hasSpawned = true;
    }
}

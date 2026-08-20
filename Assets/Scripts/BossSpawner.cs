using UnityEngine;

public class BossSpawner : MonoBehaviour
{
    public GameObject bossPrefab;
    public float spawnDistance = 30f; // プレイヤーがどのくらい近づいたら出すか
    private bool hasSpawned = false;
    public Transform player;
    private GameObject spawnedBoss;

    void Start()
    {
        player = GameObject.Find("Player").transform; // シーンに出現された時Playerというオブジェクトを代入する
    }

    // -------------------------------------------------------
    // 他の敵のSpawnerと違い、一度出したBossは離れても消さない。
    // ボス戦の途中でプレイヤーが下がるとBossが消えて体力（hp）や
    // 行動パターンの進行が最初からやり直しになってしまうため、
    // 出現後は撃破されるまでそのまま残す
    //
    // 撃破されたときは BossCtrl 側が自分を Destroy するが、
    // hasSpawned は true のままなので再び出現することはない
    // -------------------------------------------------------
    void Update()
    {
        if (hasSpawned) return;

        if (Vector2.Distance(transform.position, player.position) < spawnDistance)
        {
            Spawn();
        }
    }

    void Spawn()
    {
        spawnedBoss = Instantiate(bossPrefab, transform.position, Quaternion.identity);// プレハブを生成
        hasSpawned = true;
    }
}

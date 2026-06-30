using UnityEngine;

public class DogSpawner : MonoBehaviour
{
    public GameObject dogPrefab;
    private float spawnDistance = 30f;
    private float despawnDistance = 50f;
    private bool hasSpawned = false;
    public Transform player;
    private GameObject spawnedDog;

    void Start()
    {
        player = GameObject.Find("Player").transform;
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
        spawnedDog = Instantiate(dogPrefab, transform.position, Quaternion.identity);
        hasSpawned = true;
    }

    void Despawn()
    {
        if (spawnedDog != null)
        {
            Destroy(spawnedDog);
        }
        hasSpawned = false;
    }
}

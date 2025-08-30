using UnityEngine;
using System.Collections;

public class EnemySpawner : MonoBehaviour
{
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private float spawnIntervalSeconds = 15f;
    [SerializeField] private float offscreenMargin = 0f; // how far beyond the camera bounds to spawn

    private Camera cam;

    private void Awake()
    {
        cam = Camera.main;
    }

    private void OnEnable()
    {
        StartCoroutine(SpawnLoop());
    }

    private IEnumerator SpawnLoop()
    {
        var wait = new WaitForSeconds(spawnIntervalSeconds);
        while (true)
        {
            SpawnOne();
            yield return wait;
        }
    }

    private void SpawnOne()
    {
        if (cam == null) cam = Camera.main;
        if (cam == null || enemyPrefab == null) return;

        // Pick a random edge outside the camera view
        Vector3 min = cam.ViewportToWorldPoint(new Vector3(0, 0, 0));
        Vector3 max = cam.ViewportToWorldPoint(new Vector3(1, 1, 0));

        float leftX = min.x - offscreenMargin;
        float rightX = max.x + offscreenMargin;
        float bottomY = min.y - offscreenMargin;
        float topY = max.y + offscreenMargin;

        // Choose side: 0=left,1=right,2=bottom,3=top
        int side = Random.Range(0, 4);
        Vector3 pos = Vector3.zero;
        switch (side)
        {
            case 0: pos = new Vector3(leftX, Random.Range(bottomY, topY), 0); break;
            case 1: pos = new Vector3(rightX, Random.Range(bottomY, topY), 0); break;
            case 2: pos = new Vector3(Random.Range(leftX, rightX), bottomY, 0); break;
            case 3: pos = new Vector3(Random.Range(leftX, rightX), topY, 0); break;
        }

        Instantiate(enemyPrefab, pos, Quaternion.identity);
    }
}

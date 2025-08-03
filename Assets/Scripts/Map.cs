using Unity.AI.Navigation;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(NavMeshSurface))]
public class Map : NetworkBehaviour
{
    [Header("Map Settings")]
    public int mapWidth = 20;
    public int mapLength = 20;
    public int border = 5;

    [Header("Prefabs")]
    public GameObject obstaclePrefab;

    [Header("Obstacle Settings")]
    public int obstacleCount = 30;
    public int minObstacleLangth = 2;
    public int maxObstacleLangth = 8;
    [Range(0f, 360f)]
    public int obstacleRotationYMin = 0;
    [Range(0f, 360f)]
    public int obstacleRotationYMax = 360;

    [Header("Spawn Points")]
    public GameObject unit1SpawnPoint;
    public GameObject unit2SpawnPoint;
    public GameObject player1SpawnPoint;
    public GameObject player2SpawnPoint;

    private const float planeSize = 10f;

    public void Setup()
    {
        transform.position = new Vector3(mapWidth / 2f, 0, mapLength / 2f);
        transform.localScale = new Vector3(mapWidth / planeSize, 1, mapLength / planeSize);
        var ren = GetComponent<Renderer>();
        ren.material.mainTextureScale.Set(mapWidth / planeSize, mapLength / planeSize);

        for (int i = 0; i < obstacleCount; i++)
        {
            Vector3 spawnPos = new Vector3(
                Random.Range(border, mapWidth - border),
                Random.Range(0.4f, 0.6f),
                Random.Range(border, mapLength - border)
            );

            Quaternion rotation = Quaternion.Euler(0, Random.Range(obstacleRotationYMin, obstacleRotationYMax), 0);
            float length = Random.Range(minObstacleLangth, maxObstacleLangth);
            Vector3 scale = new Vector3(length, 1f, 1f);

            var obs = Instantiate(obstaclePrefab, spawnPos, rotation);
            obs.transform.localScale = scale;

            var netObj = obs.GetComponent<NetworkObject>();
            if (netObj != null)
                netObj.Spawn();

        }

        NavMeshSurface navMeshSurface = GetComponent<NavMeshSurface>();
        navMeshSurface.BuildNavMesh();
        var thisNetObj = GetComponent<NetworkObject>();
        thisNetObj.Spawn();
    }
    void Start() {
        NavMeshSurface navMeshSurface = GetComponent<NavMeshSurface>();
        if (!navMeshSurface.navMeshData)
        {
            navMeshSurface.BuildNavMesh();
        }

        GameObject playerCamera = Camera.main.gameObject;
        if (NetworkManager.Singleton.LocalClientId == 0)
        {
            playerCamera.transform.SetPositionAndRotation(player1SpawnPoint.transform.position, player1SpawnPoint.transform.rotation);
        }
        else
        {
            playerCamera.transform.SetPositionAndRotation(player2SpawnPoint.transform.position, player2SpawnPoint.transform.rotation);
        }
    }
}

using System.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;

public class GameInitializer : NetworkBehaviour
{
    private Map map;

    [Header("Prefabs")]
    public GameObject unitType1Prefab;
    public GameObject unitType2Prefab;
    public GameObject mapPrefab;

    [Header("Unit Count")]
    public int unitType1Count = 3;
    public int unitType2Count = 2;

    async void Start()
    {
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        NetworkManager.Singleton.OnServerStarted += OnServerStarted;

        NetworkManager.Singleton.StartClient();
        await Task.Delay(1000);

        if (!NetworkManager.Singleton.IsConnectedClient)
        {
            await ShutdownAndWait();
            NetworkManager.Singleton.StartHost();
        }
    }

    public async Task ShutdownAndWait()
    {
        NetworkManager.Singleton.Shutdown();

        while (NetworkManager.Singleton.IsListening || NetworkManager.Singleton.IsClient || NetworkManager.Singleton.IsServer)
        {
            await Task.Delay(50);
        }
    }

    private void OnServerStarted()
    {
        var obj = Instantiate(mapPrefab);
        map = obj.GetComponent<Map>();
        map.Setup();
    }

    private void OnClientConnected(ulong clientId)
    {
        if (!IsServer)
        {
            return;
        }

        if (clientId == 0)
        {
            SpawnUnitsForPlayer(clientId, map.unit1SpawnPoint.transform.position);
        }
        else
        {
            SpawnUnitsForPlayer(clientId, map.unit2SpawnPoint.transform.position);
            TurnManager.Instance.StartGame();
        }
    }

    private void SpawnUnitsForPlayer(ulong ownerClientId, Vector3 spawnPoint)
    {
        int spawnIndex = 0;

        for (int i = 0; i < unitType1Count; i++, spawnIndex++)
        {
            Vector3 pos = spawnPoint + Vector3.right * 2 * spawnIndex;
            GameObject unit = Instantiate(unitType1Prefab, pos, Quaternion.identity);
            var networkObject = unit.GetComponent<NetworkObject>();
            networkObject.SpawnAsPlayerObject(ownerClientId, true);
        }

        for (int i = 0; i < unitType2Count; i++, spawnIndex++)
        {
            Vector3 pos = spawnPoint + Vector3.right * 2 * spawnIndex;
            GameObject unit = Instantiate(unitType2Prefab, pos, Quaternion.identity);
            var networkObject = unit.GetComponent<NetworkObject>();
            networkObject.SpawnAsPlayerObject(ownerClientId, true);
        }
    }
}

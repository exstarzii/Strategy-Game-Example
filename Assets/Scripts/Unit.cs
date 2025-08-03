using System;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class Unit : NetworkBehaviour
{
    public float moveSpeed = 10f;
    public float attackRange = 10f;
    public GameObject selectionIndicator;

    public Material player1Mat;
    public Material player2Mat;

    [HideInInspector]
    public new Collider collider;
    private NavMeshAgent agent;

    public event Action OnMoved;
    public event Action OnAttacked;

    public static NetworkVariable<bool> isMoving = new NetworkVariable<bool>(false, writePerm: NetworkVariableWritePermission.Server);

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        collider = GetComponent<Collider>();
    }

    private void Start()
    {
        var ren = GetComponent<Renderer>();
        if (OwnerClientId == 0)
        {
            ren.material = player1Mat;
        }
        else
        {
            ren.material = player2Mat;
        }
    }

    void Update()
    {
        if (IsServer && isMoving.Value)
        {
            isMoving.Value = agent.pathPending ||
                  agent.remainingDistance > agent.stoppingDistance ||
                  agent.velocity.sqrMagnitude > 0.01f;

            if (!isMoving.Value)
            {
                OnMoved?.Invoke();
            }
        }
    }

    public bool TryGoByPath(NavMeshPath path)
    {
        if (Utils.CheckPath(path, moveSpeed) && IsOwner)
        {
            MoveServerRpc(path.corners[^1]);
            return true;
        }
        return false;
    }

    public void SetSelected(bool selected)
    {
        selectionIndicator.SetActive(selected);
    }

    [ServerRpc]
    void MoveServerRpc(Vector3 pos, ServerRpcParams rpcParams = default)
    {
        if (OwnerClientId == rpcParams.Receive.SenderClientId &&
            TurnManager.Instance.CanMoveClient(rpcParams.Receive.SenderClientId) &&
            !isMoving.Value &&
            Utils.CheckPath(this, pos, out NavMeshPath path))
        {
            agent.SetDestination(pos);
            isMoving.Value = true;
        }
    }

    public void TakeDamage()
    {
        if (IsServer)
        {
            GetComponent<NetworkObject>().Despawn();
        }
    }

    public bool TryAttack(Unit target)
    {
        if (!IsOwner || !Utils.CanAttack(this.transform.position, target.collider, attackRange)) return false;

        TryAttackServerRpc(target.NetworkObjectId);
        return true;
    }

    [ServerRpc]
    private void TryAttackServerRpc(ulong targetNetworkObjectId, ServerRpcParams rpcParams = default)
    {
        if (!NetworkManager.SpawnManager.SpawnedObjects.TryGetValue(targetNetworkObjectId, out NetworkObject targetNetObj) ||
            OwnerClientId != rpcParams.Receive.SenderClientId ||
            isMoving.Value ||
            !TurnManager.Instance.CanAttackClient(rpcParams.Receive.SenderClientId))
        {
            return;
        }

        Unit targetUnit = targetNetObj.GetComponent<Unit>();
        if (targetUnit == null || targetUnit == this)
        {
            return;
        }

        if (Utils.CanAttack(this.transform.position, targetUnit.collider, attackRange))
        {
            agent.ResetPath();
            targetUnit.TakeDamage();
            OnAttacked?.Invoke();
        }
    }
}

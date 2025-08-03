using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class TurnManager : NetworkBehaviour
{
    public static TurnManager Instance;

    public enum GameState { Waiting, InProgress, Finished }

    [Header("UI Panels")]
    public GameObject waitingPanel;
    public GameObject gamePanel;
    public GameObject gameOverPanel;

    [Header("Game UI Elements")]
    public Text turnInfoText;
    public Text actionInfoText;
    public Text timerText;
    public Button endTurnButton;
    public Button exitButton;
    public Text gameOverText;

    [Header("Config")]
    public float turnDuration = 60f;
    public int maxAttacks = 1;
    public int maxMoves = 1;
    public int maxTurns = 15;

    [Header("Management")]
    public UnitController unitController;
    public CameraController3rd cameraController;

    private NetworkVariable<GameState> gameState = new NetworkVariable<GameState>(GameState.Waiting);
    private NetworkVariable<ulong> currentTurnPlayerId = new NetworkVariable<ulong>(writePerm: NetworkVariableWritePermission.Server);
    private NetworkVariable<int> currentTurnNumber = new NetworkVariable<int>(1, writePerm: NetworkVariableWritePermission.Server);
    private NetworkVariable<float> turnStartTime = new NetworkVariable<float>(0f, writePerm: NetworkVariableWritePermission.Server);
    private NetworkVariable<int> usedMoves = new NetworkVariable<int>(0, writePerm: NetworkVariableWritePermission.Server);
    private NetworkVariable<int> usedAttacks = new NetworkVariable<int>(0, writePerm: NetworkVariableWritePermission.Server);

    private float timeOffset=0;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
    }

    private void Start()
    {
        endTurnButton.onClick.AddListener(RequestEndTurn);
        exitButton.onClick.AddListener(ExitGame);

        gameState.OnValueChanged += OnGameStateChanged;
        currentTurnNumber.OnValueChanged += (_, _) => UpdateGameUI();
        usedMoves.OnValueChanged += (_, _) => UpdateGameUI();
        usedAttacks.OnValueChanged += (_, _) => UpdateGameUI();
        currentTurnPlayerId.OnValueChanged += (_, _) => UpdateGameUI();

        turnStartTime.OnValueChanged += (_, startTime) =>
        {
            timeOffset = Time.time - startTime;
        };

        SetUIPanelState(GameState.Waiting);
    }

    private void Update()
    {
        if (IsServer && gameState.Value == GameState.InProgress)
        {
            if (Time.time - timeOffset - turnStartTime.Value >= turnDuration)
            {
                EndTurn();
            }
        }

        if (gameState.Value == GameState.InProgress)
        {
            float timeLeft = Mathf.Max(0, turnDuration - (Time.time - timeOffset - turnStartTime.Value));
            timerText.text = $"Осталось времени: {Mathf.CeilToInt(timeLeft)}с";
        }
    }

    public void StartGame()
    {
        if (!IsServer) return;

        foreach (var unit in FindObjectsOfType<Unit>())
        {
            unit.OnMoved += HandleUnitMoved;
            unit.OnAttacked += HandleUnitAttacked;
        }

        currentTurnPlayerId.Value = NetworkManager.ConnectedClientsList[0].ClientId;
        currentTurnNumber.Value = 1;
        turnStartTime.Value = Time.time;
        usedMoves.Value = 0;
        usedAttacks.Value = 0;
        gameState.Value = GameState.InProgress;
    }

    private void OnGameStateChanged(GameState prev, GameState next)
    {
        SetUIPanelState(next);
        UpdateGameUI();
    }

    private void SetUIPanelState(GameState state)
    {
        waitingPanel.SetActive(state == GameState.Waiting);
        gamePanel.SetActive(state == GameState.InProgress);
        gameOverPanel.SetActive(state == GameState.Finished);
        unitController.enabled = state == GameState.InProgress;
        cameraController.enabled = state == GameState.InProgress;
    }

    private void HandleUnitMoved()
    {
        usedMoves.Value++;
        
        if (usedMoves.Value >= maxMoves && usedAttacks.Value >= maxAttacks)
            EndTurn();
    }

    private void HandleUnitAttacked()
    {
        usedAttacks.Value++;
        if (usedMoves.Value >= maxMoves && usedAttacks.Value >= maxAttacks)
            EndTurn();
    }

    private void RequestEndTurn()
    {
        if (IsMyTurn)
        {
            EndTurnServerRpc();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void EndTurnServerRpc(ServerRpcParams rpcParams = default)
    {
        if (rpcParams.Receive.SenderClientId == currentTurnPlayerId.Value)
        {
            EndTurn();
        }
    }

    private void EndTurn()
    {
        ulong nextPlayer = GetOtherPlayerId(currentTurnPlayerId.Value);
        currentTurnNumber.Value++;
        turnStartTime.Value = Time.time;
        usedMoves.Value = 0;
        usedAttacks.Value = 0;

        if (currentTurnNumber.Value >= maxTurns)
        {
            int myUnits = CountUnits(currentTurnPlayerId.Value);
            int enemyUnits = CountUnits(nextPlayer);
            if (myUnits != enemyUnits)
            {
                DeclareWinner(myUnits > enemyUnits ? currentTurnPlayerId.Value : nextPlayer);
                return;
            }
        }

        currentTurnPlayerId.Value = nextPlayer;
    }

    private void DeclareWinner(ulong winnerId)
    {
        gameState.Value = GameState.Finished;
        SendGameEndClientRpc(winnerId);
    }

    [ClientRpc]
    void SendGameEndClientRpc(ulong winnerId)
    {
        gameOverText.text = winnerId == NetworkManager.LocalClientId ? "Вы победили!" : "Вы проиграли.";
    }

    private int CountUnits(ulong playerId)
    {
        int count = 0;
        foreach (var unit in FindObjectsOfType<Unit>())
        {
            if (unit.OwnerClientId == playerId)
                count++;
        }
        return count;
    }

    private ulong GetOtherPlayerId(ulong id)
    {
        foreach (var client in NetworkManager.ConnectedClientsList)
        {
            if (client.ClientId != id)
                return client.ClientId;
        }
        return id;
    }

    public bool IsMyTurn => NetworkManager.Singleton.LocalClientId == currentTurnPlayerId.Value;
    public bool CanMove => IsMyTurn && usedMoves.Value < maxMoves;
    public bool CanAttack => IsMyTurn && usedAttacks.Value < maxAttacks;

    public bool CanMoveClient(ulong CliendId)
    {
        return CliendId == currentTurnPlayerId.Value && usedMoves.Value < maxMoves;
    }

    public bool CanAttackClient(ulong CliendId)
    {
        return CliendId == currentTurnPlayerId.Value && usedAttacks.Value < maxAttacks;
    }

    private void UpdateGameUI()
    {
        if (gameState.Value != GameState.InProgress) return;

        turnInfoText.text = IsMyTurn ? $"Ваш ход (Ход {currentTurnNumber.Value})" : $"Ход противника (Ход {currentTurnNumber.Value})";
        actionInfoText.text = IsMyTurn ? $"Перемещение: {maxMoves - usedMoves.Value}, Атака: {maxAttacks - usedAttacks.Value}" : "";
        endTurnButton.gameObject.SetActive(IsMyTurn);
    }

    void ExitGame()
    {
        Application.Quit();
    }
}

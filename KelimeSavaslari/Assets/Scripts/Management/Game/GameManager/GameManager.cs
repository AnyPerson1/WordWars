using Mirror;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Threading;

[Serializable]
public struct TileChangeData : NetworkMessage
{
    public Vector3Int position;
    public char letter;
    public uint playerNetId;
}

public class GameManager : NetworkBehaviour
{
    [Header("Map Settings")]
    [SerializeField] private int mapWidth = 10;
    [SerializeField] private int mapHeight = 10;

    [SyncVar(hook = nameof(OnRoomIdChanged))]
    public Guid currentRoomId = Guid.Empty;

    // Server-side logical map
    private LogicalTile[,] serverMap;

    // Network components
    private NetworkMatch networkMatchComponent;
    private CancellationTokenSource cancellationTokenSource;

    #region Network Lifecycle

    public override void OnStartServer()
    {
        base.OnStartServer();
        networkMatchComponent = GetComponent<NetworkMatch>();
        cancellationTokenSource = new CancellationTokenSource();

        InitializeMap();
        Debug.Log($"[Server] GameManager initialized with {mapWidth}x{mapHeight} map");
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        networkMatchComponent = GetComponent<NetworkMatch>();
        Debug.Log($"[Client] GameManager started");
    }

    public override void OnStopServer()
    {
        base.OnStopServer();
        cancellationTokenSource?.Cancel();
        cancellationTokenSource?.Dispose();
    }

    #endregion

    #region Map Management

    [Server]
    private void InitializeMap()
    {
        serverMap = new LogicalTile[mapWidth, mapHeight];

        for (int x = 0; x < mapWidth; x++)
        {
            for (int y = 0; y < mapHeight; y++)
            {
                serverMap[x, y] = new LogicalTile
                {
                    letter = ' ',
                    isChangeable = true
                };
            }
        }

        Debug.Log($"[Server] Map initialized: {mapWidth}x{mapHeight}");
    }

    [Server]
    public void Initialize(Guid roomId)
    {
        currentRoomId = roomId;
        if (networkMatchComponent != null)
        {
            networkMatchComponent.matchId = roomId;
        }
        Debug.Log($"[Server] Room {roomId} initialized");
    }

    [Server]
    public void SendMapToPlayer(GamePlayer player)
    {
        if (serverMap == null)
        {
            Debug.LogError("[Server] Cannot send map - server map is null");
            return;
        }

        // Convert 2D array to 1D for network transmission
        LogicalTile[] flatMap = ArrayConverter.Flatten2DArray(serverMap);
        player.RpcReceiveMapData(flatMap, mapWidth, mapHeight);

        Debug.Log($"[Server] Map data sent to player {player.netId}");
    }

    #endregion

    #region Tile Changes

    [Server]
    public void ProcessTileChange(Vector3Int position, char letter, uint playerNetId)
    {
        // Validate position
        if (position.x < 0 || position.x >= mapWidth ||
            position.y < 0 || position.y >= mapHeight)
        {
            Debug.LogWarning($"[Server] Invalid tile position: {position}");
            return;
        }

        // Update server map
        serverMap[position.x, position.y] = new LogicalTile
        {
            letter = letter,
            isChangeable = true
        };

        // Broadcast change to all clients
        RpcBroadcastTileChange(position, letter, playerNetId);

        Debug.Log($"[Server] Tile changed at {position} to '{letter}' by player {playerNetId}");
    }

    [ClientRpc]
    private void RpcBroadcastTileChange(Vector3Int position, char letter, uint playerNetId)
    {
        // Find local player and update their tilemap
        GamePlayer localPlayer = NetworkClient.localPlayer?.GetComponent<GamePlayer>();
        if (localPlayer != null)
        {
            localPlayer.ApplyTileChange(position, letter);
            Debug.Log($"[Client] Applied tile change at {position} to '{letter}'");
        }
    }

    #endregion

    #region Room Management

    private void OnRoomIdChanged(Guid oldId, Guid newId)
    {
        Debug.Log($"[Client] Room ID changed: {oldId} -> {newId}");
        if (networkMatchComponent != null)
        {
            networkMatchComponent.matchId = newId;
        }
    }

    [Server]
    public async void StartGameCountdown(int seconds)
    {
        CancellationToken token = cancellationTokenSource.Token;

        try
        {
            for (int i = seconds; i > 0; i--)
            {
                RpcDisplayMessage($"Game starting in {i} seconds...");
                await System.Threading.Tasks.Task.Delay(1000, token);
            }

            RpcStartGame();
        }
        catch (OperationCanceledException)
        {
            Debug.LogWarning($"[Server] Game countdown cancelled for room {currentRoomId}");
            RpcDisplayMessage("Game start cancelled.");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Server] Error in game countdown: {ex.Message}");
            RpcDisplayMessage($"Error starting game: {ex.Message}");
        }
    }

    [ClientRpc]
    public void RpcStartGame()
    {
        Debug.Log($"[Client] Game started for room {currentRoomId}!");
    }

    [ClientRpc]
    public void RpcDisplayMessage(string message)
    {
        Debug.Log($"[Client] Room message: {message}");
    }

    #endregion

    #region Getters

    public int MapWidth => mapWidth;
    public int MapHeight => mapHeight;

    #endregion
}
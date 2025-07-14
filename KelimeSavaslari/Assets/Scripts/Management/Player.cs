using Mirror;
using UnityEngine;
using System;
using System.Collections.Generic;
using UnityEngine.Tilemaps;

public class GamePlayer : NetworkBehaviour
{
    [Header("Network Status")]
    [SyncVar(hook = nameof(OnPlayerStatusChanged))]
    public string playerStatus = "None";

    [SyncVar(hook = nameof(OnRoomIdChanged))]
    public Guid roomId = Guid.Empty;

    [Header("Tilemap Settings")]
    [SerializeField] private Tilemap playerTilemap;
    [SerializeField] private TileBase defaultEmptyTile;
    [SerializeField] private List<TileMapping> tileMappings;

    [Header("Tilemap Offset")]
    [SerializeField] private Vector3Int tilemapOffset = new Vector3Int(0, 0, 0);

    // Components
    private NetworkMatch networkMatchComponent;

    [Serializable]
    public struct TileMapping
    {
        public char character;
        public TileBase tileAsset;
    }

    #region Network Lifecycle

    public override void OnStartServer()
    {
        base.OnStartServer();
        networkMatchComponent = GetComponent<NetworkMatch>();
        Debug.Log($"[Server] GamePlayer {netId} started");
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        networkMatchComponent = GetComponent<NetworkMatch>();

        // Initialize tilemap reference
        if (playerTilemap == null)
        {
            playerTilemap = GetComponentInChildren<Tilemap>();
            if (playerTilemap == null)
            {
                Debug.LogError($"[Client] No Tilemap found for player {netId}");
                return;
            }
        }

        // If this is the local player, try to join the game
        if (isLocalPlayer)
        {
            Debug.Log($"[Client] Local player {netId} requesting to join game");
            CmdTryJoinGame();
        }
    }

    #endregion

    #region Room Management

    [Command]
    private void CmdTryJoinGame()
    {
        Debug.Log($"[Server] Player {netId} attempting to join game");
        CustomRoomManager.Instance?.TryJoinRoom(connectionToClient);
    }

    public void SetPlayerState(string state, Guid roomGuid)
    {
        playerStatus = state;
        roomId = roomGuid;

        if (networkMatchComponent != null)
        {
            networkMatchComponent.matchId = roomGuid;
        }

        Debug.Log($"[Server] Player {netId} state set to '{state}' for room {roomGuid}");
    }

    private void OnPlayerStatusChanged(string oldStatus, string newStatus)
    {
        Debug.Log($"[Client] Player {netId} status changed: {oldStatus} -> {newStatus}");
    }

    private void OnRoomIdChanged(Guid oldId, Guid newId)
    {
        Debug.Log($"[Client] Player {netId} room changed: {oldId} -> {newId}");

        if (isLocalPlayer && newId != Guid.Empty)
        {
            Debug.Log($"[Client] Local player joined room {newId}");
        }
    }

    #endregion

    #region Tile Management

    [Command]
    public void CmdRequestTileChange(Vector3Int position, char letter)
    {
        Debug.Log($"[Server] Player {netId} requesting tile change at {position} to '{letter}'");

        GameManager gameManager = FindFirstObjectByType<GameManager>();
        if (gameManager != null)
        {
            gameManager.ProcessTileChange(position, letter, netId);
        }
        else
        {
            Debug.LogError("[Server] GameManager not found!");
        }
    }

    [ClientRpc]
    public void RpcReceiveMapData(LogicalTile[] flatMapData, int width, int height)
    {
        if (!isLocalPlayer) return;

        Debug.Log($"[Client] Receiving map data: {width}x{height}");

        if (playerTilemap == null)
        {
            Debug.LogError("[Client] Cannot apply map data - tilemap is null");
            return;
        }

        // Convert flat array back to 2D
        LogicalTile[,] mapData = ArrayConverter.Unflatten1DArray(flatMapData, width, height);
        if (mapData == null)
        {
            Debug.LogError("[Client] Failed to convert map data");
            return;
        }

        // Apply all tiles to the tilemap
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                LogicalTile logicalTile = mapData[x, y];
                ApplyTileChange(new Vector3Int(x, y, 0), logicalTile.letter);
            }
        }

        Debug.Log($"[Client] Map data applied successfully");
    }

    public void ApplyTileChange(Vector3Int position, char letter)
    {
        if (playerTilemap == null)
        {
            Debug.LogError("[Client] Cannot apply tile change - tilemap is null");
            return;
        }

        TileBase tileToSet = GetTileBaseFromCharacter(letter);
        if (tileToSet == null)
        {
            Debug.LogWarning($"[Client] No tile found for character '{letter}'");
            return;
        }

        // Apply offset and set tile
        Vector3Int finalPosition = position + tilemapOffset;
        playerTilemap.SetTile(finalPosition, tileToSet);

        Debug.Log($"[Client] Tile set at {finalPosition} (logical: {position}) to '{letter}'");
    }

    private TileBase GetTileBaseFromCharacter(char character)
    {
        // Check for empty space
        if (character == ' ')
        {
            return defaultEmptyTile;
        }

        // Check tile mappings
        foreach (var mapping in tileMappings)
        {
            if (mapping.character == character)
            {
                return mapping.tileAsset;
            }
        }

        Debug.LogWarning($"[Client] No tile mapping found for character '{character}'");
        return defaultEmptyTile;
    }

    #endregion

    #region Input Helper

    public Vector3Int WorldToLogicalPosition(Vector3 worldPosition)
    {
        if (playerTilemap == null) return Vector3Int.zero;

        Vector3Int cellPosition = playerTilemap.WorldToCell(worldPosition);
        return cellPosition;
    }

    #endregion

    #region Getters

    public Vector3Int TilemapOffset => tilemapOffset;

    #endregion
}
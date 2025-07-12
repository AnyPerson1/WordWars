using Mirror;
using UnityEngine;
using System;
using System.Collections.Generic;
using UnityEngine.Tilemaps;

public class GamePlayer : NetworkBehaviour
{
    [SyncVar(hook = nameof(OnNetworkPlayerStatusChanged))]
    public string networkPlayerStatus = "None";

    [SyncVar(hook = nameof(OnNetworkRoomIdChanged))]
    public Guid networkRoomId = Guid.Empty;

    private NetworkMatch networkMatchComponent;

    [SerializeField] public Tilemap playerTilemap;

    [Header("Tile Definitions (Player Specific)")]
    [SerializeField] private TileBase defaultEmptyTile;
    [SerializeField] private List<TileMapping> tileMappings;

    [Serializable]
    public struct TileMapping
    {
        public char character;
        public TileBase tileAsset;
    }

    [Header("Tilemap Visual Offset")]
    [SyncVar] public int tilemapOffsetX = 0; 
    [SyncVar] public int tilemapOffsetY = 0; 


    public override void OnStartClient()
    {
        base.OnStartClient();
        networkMatchComponent = GetComponent<NetworkMatch>();

        if (playerTilemap == null)
        {
            playerTilemap = GetComponentInChildren<Tilemap>();
        }
        
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        networkMatchComponent = GetComponent<NetworkMatch>();

        if (playerTilemap == null)
        {
            playerTilemap = GetComponentInChildren<Tilemap>();
        }
    }

    void OnNetworkPlayerStatusChanged(string oldStatus, string newStatus)
    {
        Debug.Log($"Player {netId} Status Changed: {oldStatus} -> {newStatus}");
    }

    void OnNetworkRoomIdChanged(Guid oldId, Guid newId)
    {
        Debug.Log($"Player {netId} Room ID Changed: {oldId} -> {newId}");
    }

    [Server]
    public void SetPlayerStateAndMatch(string status, Guid roomId)
    {
        networkPlayerStatus = status;
        networkRoomId = roomId;

        if (networkMatchComponent != null)
        {
            networkMatchComponent.matchId = roomId;
        }
    }

    // Draggable objeden gelen komut
    [Command]
    public void CmdAttemptTileChange(int logicalRow, int logicalCol, char droppedCharacter)
    {
        Room currentRoom = CustomRoomManager.Instance.activeRooms.GetValueOrDefault(networkRoomId);
        if (currentRoom != null && currentRoom.GameManagerNetIdentity != null)
        {
            GameManager gameManager = currentRoom.GameManagerNetIdentity.GetComponent<GameManager>();
            if (gameManager != null)
            {
                gameManager.UpdateTilemapFromDrop(logicalRow, logicalCol, droppedCharacter, netId);
            }
            else
            {
                Debug.LogError($"CmdAttemptTileChange: GameManager (NetId: {currentRoom.GameManagerNetIdentity.netId}) bulunamadý.");
            }
        }
        else
        {
            Debug.LogError($"CmdAttemptTileChange: Oda {networkRoomId} veya GameManager referansý bulunamadý.");
        }
    }

    [ClientRpc]
    public void RpcApplyTilemapData(LogicalTile[] flatTiles, int rows, int cols)
    {
        if (playerTilemap == null)
        {
            Debug.LogError($"RpcApplyTilemapData: Player {netId} için hedef Tilemap bulunamadý!");
            return;
        }

        LogicalTile[,] new2DTiles = ArrayConverter.Unflatten1DArray(flatTiles, rows, cols);

        if (new2DTiles == null)
        {
            Debug.LogError("RpcApplyTilemapData: Harita verisi Unflatten edilemedi!");
            return;
        }

        playerTilemap.ClearAllTiles();

        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                LogicalTile logicalTile = new2DTiles[r, c];
                TileBase tileToSet = GetTileBaseFromLogicalTile(logicalTile);

                if (tileToSet != null)
                {
                    // Mantýksal koordinatý (r,c) görsel Tilemap koordinatýna dönüþtür
                    playerTilemap.SetTile(new Vector3Int(c + tilemapOffsetX, r + tilemapOffsetY, 0), tileToSet);
                }
                else
                {
                    Debug.LogWarning($"RpcApplyTilemapData (Player {netId}): '{logicalTile.letter}' karakteri için TileBase bulunamadý veya null.");
                }
            }
        }
        Debug.Log($"Ýstemci ({netId}): Kendi haritasý {rows}x{cols} baþarýyla uygulandý.");
    }

    // GameManager'ýn ApplySingleTileChangeToPlayerTilemap çaðýrabilmesi için public yapýldý
    public TileBase GetTileBaseFromLogicalTile(LogicalTile logicalTile)
    {
        foreach (var mapping in tileMappings)
        {
            if (mapping.character == logicalTile.letter)
            {
                return mapping.tileAsset;
            }
        }

        if (logicalTile.letter == ' ')
        {
            return defaultEmptyTile;
        }

        Debug.LogWarning($"GetTileBaseFromLogicalTile (Player {netId}): '{logicalTile.letter}' için eþleþen TileBase bulunamadý. Varsayýlan boþ Tile döndürüldü.");
        return defaultEmptyTile;
    }
}
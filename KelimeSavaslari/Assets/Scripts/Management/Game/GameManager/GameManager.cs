using Mirror;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine.Tilemaps;

[Serializable]
public struct TileChangeData
{
    public uint playerNetId;
    public Vector3Int tilemapCellPosition;
    public LogicalTile newLogicalTile;
}

public class GameManager : NetworkBehaviour
{
    [SyncVar(hook = nameof(OnRoomIdChanged))]
    public Guid currentRoomId = Guid.Empty;

    private LogicalTile[,] serverLogicalMap;
    private int mapRows;
    private int mapCols;

    public SyncList<TileChangeData> tileChanges = new SyncList<TileChangeData>();

    private NetworkMatch networkMatchComponent;
    private CancellationTokenSource roomCancellationTokenSource;

    public override void OnStartServer()
    {
        base.OnStartServer();
        networkMatchComponent = GetComponent<NetworkMatch>();
        roomCancellationTokenSource = new CancellationTokenSource();

        tileChanges.Callback += OnTileChangesUpdated;
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        networkMatchComponent = GetComponent<NetworkMatch>();

        if (!isServer)
        {
            tileChanges.Callback += OnTileChangesUpdated;
        }
    }

    public override void OnStopServer()
    {
        base.OnStopServer();
        if (roomCancellationTokenSource != null)
        {
            roomCancellationTokenSource.Cancel();
            roomCancellationTokenSource.Dispose();
            roomCancellationTokenSource = null;
        }
        tileChanges.Callback -= OnTileChangesUpdated;
    }

    public override void OnStopClient()
    {
        base.OnStopClient();
        if (!isServer)
        {
            tileChanges.Callback -= OnTileChangesUpdated;
        }
    }

    void OnRoomIdChanged(Guid oldId, Guid newId)
    {
        Debug.Log($"GameManager {netId} Room ID Changed: {oldId} -> {newId}");
    }

    private void OnTileChangesUpdated(SyncList<TileChangeData>.Operation op, int itemIndex, TileChangeData oldItem, TileChangeData newItem)
    {
        if (isServer) return;

        if (op == SyncList<TileChangeData>.Operation.OP_ADD)
        {
            ApplySingleTileChangeToPlayerTilemap(newItem);
        }
    }

    private void ApplySingleTileChangeToPlayerTilemap(TileChangeData changeData)
    {
        GamePlayer localPlayer = NetworkClient.localPlayer?.GetComponent<GamePlayer>();
        if (localPlayer != null && localPlayer.playerTilemap != null)
        {
            TileBase tileToSet = localPlayer.GetTileBaseFromLogicalTile(changeData.newLogicalTile);

            if (tileToSet != null)
            {
                // SyncList'ten gelen konum zaten istemcinin Tilemap'i için ayarlanmýþ (offset'li)
                localPlayer.playerTilemap.SetTile(changeData.tilemapCellPosition, tileToSet);
                Debug.Log($"Ýstemci ({NetworkClient.connection.identity.netId}): Tilemap'te {changeData.tilemapCellPosition} konumunda deðiþiklik uygulandý: {changeData.newLogicalTile.letter}");
            }
            else
            {
                Debug.LogWarning($"TileChangeData: '{changeData.newLogicalTile.letter}' için TileBase bulunamadý veya null.");
            }
        }
    }

    [Server]
    public void Initialize(Guid roomId)
    {
        currentRoomId = roomId;
        if (networkMatchComponent != null)
        {
            networkMatchComponent.matchId = roomId;
        }
        else
        {
            Debug.LogError($"Server: GameManager {netId} üzerinde NetworkMatch bileþeni yok, matchId ayarlanamadý.");
        }
        if (roomCancellationTokenSource == null || roomCancellationTokenSource.IsCancellationRequested)
        {
            roomCancellationTokenSource = new CancellationTokenSource();
        }

        serverLogicalMap = CreateTestMap(5, 5);
        mapRows = serverLogicalMap.GetLength(0);
        mapCols = serverLogicalMap.GetLength(1);

        SendFullMapToClients(serverLogicalMap);
    }

    [Server]
    private LogicalTile[,] CreateTestMap(int rows, int cols)
    {
        LogicalTile[,] map = new LogicalTile[rows, cols];
        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                if (r == 0 || r == rows - 1 || c == 0 || c == cols - 1)
                {
                    map[r, c] = new LogicalTile { letter = 'X', isChangeable = false };
                }
                else if ((r + c) % 2 == 0)
                {
                    map[r, c] = new LogicalTile { letter = 'A', isChangeable = true };
                }
                else
                {
                    map[r, c] = new LogicalTile { letter = ' ', isChangeable = true };
                }
            }
        }
        return map;
    }

    [Server]
    public void SendFullMapToClients(LogicalTile[,] mapData)
    {
        if (mapData == null)
        {
            Debug.LogError("SendFullMapToClients: Harita verisi null!");
            return;
        }

        int rows = mapData.GetLength(0);
        int cols = mapData.GetLength(1);

        LogicalTile[] flatMapData = ArrayConverter.Flatten2DArray(mapData);

        if (flatMapData == null)
        {
            Debug.LogError("SendFullMapToClients: Harita verisi Flatten edilemedi!");
            return;
        }

        Room currentRoom = CustomRoomManager.Instance.activeRooms.GetValueOrDefault(currentRoomId);

        if (currentRoom == null)
        {
            Debug.LogWarning($"SendFullMapToClients: Oda {currentRoomId} aktif odalar listesinde bulunamadý.");
            return;
        }

        foreach (NetworkConnectionToClient conn in currentRoom.Players)
        {
            GamePlayer playerComponent = conn.identity?.GetComponent<GamePlayer>();
            if (playerComponent != null)
            {
                playerComponent.RpcApplyTilemapData(flatMapData, rows, cols);
                Debug.Log($"Sunucu: Oyuncu {conn.identity.netId}'e tam harita verisi gönderildi.");
            }
            else
            {
                Debug.LogWarning($"Sunucu: Oda {currentRoomId} içindeki {conn.identity?.netId.ToString() ?? "N/A"} ID'li oyuncu bileþeninde GamePlayer bulunamadý.");
            }
        }
    }

    // Sürükle-býrak olayýndan gelen Tilemap güncelleme isteðini iþler (Sunucu tarafýnda çalýþýr)
    // Þimdi mantýksal koordinatlarý ve iþlemi baþlatan oyuncunun netId'sini alýyor
    [Server]
    public void UpdateTilemapFromDrop(int logicalRow, int logicalCol, char droppedCharacter, uint callingPlayerNetId)
    {
        if (logicalRow < 0 || logicalRow >= mapRows ||
            logicalCol < 0 || logicalCol >= mapCols)
        {
            Debug.LogWarning($"UpdateTilemapFromDrop: Geçersiz mantýksal koordinat ({logicalRow},{logicalCol}). Harita dýþý.");
            return;
        }

        LogicalTile currentTile = serverLogicalMap[logicalRow, logicalCol];

        if (currentTile.letter == ' ' || !currentTile.isChangeable)
        {
            Debug.LogWarning($"UpdateTilemapFromDrop: Tile ({logicalRow},{logicalCol}) deðiþtirilemez veya zaten boþ. Mevcut karakter: '{currentTile.letter}'");
            return;
        }

        serverLogicalMap[logicalRow, logicalCol] = new LogicalTile
        {
            letter = droppedCharacter,
            isChangeable = true
        };

        GamePlayer callingPlayer = NetworkServer.spawned[callingPlayerNetId]?.GetComponent<GamePlayer>();
        if (callingPlayer == null)
        {
            Debug.LogError($"UpdateTilemapFromDrop: Ýþlemi baþlatan oyuncu (NetId: {callingPlayerNetId}) sunucuda bulunamadý.");
            return;
        }

        // Ýstemcinin Tilemap'ine uygulanacak görsel hücre koordinatýný hesapla
        Vector3Int visualCellPosition = new Vector3Int(logicalCol + callingPlayer.tilemapOffsetX, logicalRow + callingPlayer.tilemapOffsetY, 0);

        tileChanges.Add(new TileChangeData
        {
            playerNetId = callingPlayerNetId,
            tilemapCellPosition = visualCellPosition,
            newLogicalTile = serverLogicalMap[logicalRow, logicalCol]
        });

    }

    [ClientRpc]
    public void RpcStartGame()
    {
        Debug.Log($"Ýstemci ({NetworkClient.connection.identity.netId}): Oda {currentRoomId} için oyun baþladý!");
    }

    [ClientRpc]
    public void RpcDisplayMessage(string message)
    {
        Debug.Log($"Ýstemci ({NetworkClient.connection.identity.netId}): Oda mesajý: {message}");
    }

    [Server]
    public async void StartGameCountdown(int seconds)
    {
        CancellationToken token = roomCancellationTokenSource.Token;
        try
        {
            for (int i = seconds; i > 0; i--)
            {
                RpcDisplayMessage($"Oyun {i} saniye içinde baþlýyor...");
                await System.Threading.Tasks.Task.Delay(1000, token);
            }
            RpcStartGame();
        }
        catch (OperationCanceledException)
        {
            Debug.LogWarning($"Oyun geri sayýmý oda {currentRoomId} için iptal edildi.");
            RpcDisplayMessage("Oyun baþlatma iptal edildi.");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Geri sayým sýrasýnda hata: {ex.Message}");
        }
    }
}
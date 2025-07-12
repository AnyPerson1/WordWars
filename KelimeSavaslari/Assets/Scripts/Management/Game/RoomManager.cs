using Mirror;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;
using System.Threading; // CancellationToken için

public class CustomRoomManager : NetworkManager
{
    public static CustomRoomManager Instance;
    private const int MAX_PLAYERS_PER_ROOM = 2;

    public Dictionary<NetworkConnectionToClient, Player> connections = new Dictionary<NetworkConnectionToClient, Player>();
    public Dictionary<Guid, Room> activeRooms = new Dictionary<Guid, Room>();

    [SerializeField] GameObject managerPrefab;
    // [SerializeField] GameObject playerPrefabOverride;

    public override void Awake()
    {
        base.Awake();
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Debug.LogWarning("CustomRoomManager: Birden fazla CustomRoomManager örneði algýlandý. Yenisi yok ediliyor.");
            Destroy(gameObject);
        }
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        Debug.Log("CustomRoomManager: Sunucu Baþlatýldý.");
    }

    public override void OnServerAddPlayer(NetworkConnectionToClient conn)
    {
        base.OnServerAddPlayer(conn);

        Debug.Log($"CustomRoomManager: Oyuncu {conn.connectionId} sunucuya baðlandý.");

        Player newPlayerData = new Player
        {
            status = "Search",
            currentRoomId = Guid.Empty
        };

        if (connections.TryAdd(conn, newPlayerData))
        {
            Debug.Log($"CustomRoomManager: Oyuncu {conn.connectionId} 'Search' durumuyla kaydedildi. Toplam baðlantý: {connections.Count}");

            GamePlayer gamePlayer = conn.identity?.GetComponent<GamePlayer>();
            if (gamePlayer != null)
            {
                gamePlayer.SetPlayerStateAndMatch("Search", Guid.Empty);
            }
            else
            {
                Debug.LogError($"CustomRoomManager: Baðlanan oyuncuda (ConnId: {conn.connectionId}) GamePlayer bileþeni bulunamadý. Lütfen playerPrefab'ý kontrol edin.");
            }
        }
        else
        {
            Debug.LogWarning($"CustomRoomManager: Oyuncu {conn.connectionId} zaten kaydedilmiþ. Çift baðlantý oluþtu mu?");
        }
    }

    public override void OnServerDisconnect(NetworkConnectionToClient conn)
    {
        Debug.Log($"CustomRoomManager: Oyuncu {conn.connectionId} baðlantýsý kesildi.");

        if (connections.TryGetValue(conn, out Player disconnectedPlayerData))
        {
            Guid roomLeftId = disconnectedPlayerData.currentRoomId;

            if (roomLeftId != Guid.Empty && activeRooms.TryGetValue(roomLeftId, out Room roomLeft))
            {
                roomLeft.RemovePlayer(conn);
                Debug.Log($"CustomRoomManager: Oyuncu {conn.connectionId} oda {roomLeftId}'den ayrýldý. Odadaki kalan oyuncu: {roomLeft.Players.Count}");

                if (roomLeft.IsEmpty)
                {
                    CleanUpRoom(roomLeftId, roomLeft);
                }
                else
                {
                    if (roomLeft.GameManagerNetIdentity != null)
                    {
                        GameManager gm = roomLeft.GameManagerNetIdentity.GetComponent<GameManager>();
                        if (gm != null) gm.RpcDisplayMessage($"Oyuncu {conn.connectionId} odadan ayrýldý. Kalan oyuncu: {roomLeft.Players.Count}");
                    }
                }
            }

            connections.Remove(conn);
            Debug.Log($"CustomRoomManager: Oyuncu {conn.connectionId} verisi silindi. Kalan baðlantý: {connections.Count}");
        }
        else
        {
            Debug.LogWarning($"CustomRoomManager: Baðlantýsý kesilen oyuncu {conn.connectionId} 'connections' listesinde bulunamadý. Zaten temizlenmiþ olabilir.");
        }

        base.OnServerDisconnect(conn);
    }

    [Server]
    private void CleanUpRoom(Guid roomId, Room roomToClean)
    {
        Debug.Log($"CustomRoomManager: Oda {roomId} temizleniyor.");

        if (roomToClean.GameManagerNetIdentity != null && roomToClean.GameManagerNetIdentity.isServer)
        {
            GameManager gm = roomToClean.GameManagerNetIdentity.GetComponent<GameManager>();
            if (gm != null && gm.isActiveAndEnabled)
            {
                // GameManager üzerindeki CancellationTokenSource'u iptal et
                if (gm.GetType().GetField("roomCancellationTokenSource", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)?.GetValue(gm) is CancellationTokenSource cts)
                {
                    cts.Cancel();
                    cts.Dispose(); // Kaynaklarý serbest býrak
                }
            }
            NetworkServer.Destroy(roomToClean.GameManagerNetIdentity.gameObject);
        }
        activeRooms.Remove(roomId);
    }

    [Server]
    public void TryJoinRoom(NetworkConnectionToClient playerConnection)
    {
        Debug.Log($"CustomRoomManager: Oyuncu {playerConnection.connectionId} odaya katýlmaya çalýþýyor.");

        if (!connections.TryGetValue(playerConnection, out Player playerData))
        {
            Debug.LogWarning($"CustomRoomManager: Oyuncu verisi bulunamadý {playerConnection.connectionId}. Odaya katýlamýyor.");
            return;
        }

        if (playerData.currentRoomId != Guid.Empty)
        {
            Debug.Log($"CustomRoomManager: Oyuncu {playerConnection.connectionId} zaten bir odada ({playerData.currentRoomId}). Eski odadan çýkarýlýyor.");
            LeaveRoom(playerConnection, playerData.currentRoomId);
        }

        GamePlayer gamePlayer = playerConnection.identity?.GetComponent<GamePlayer>();
        if (gamePlayer == null)
        {
            Debug.LogWarning($"CustomRoomManager: GamePlayer bileþeni bulunamadý {playerConnection.connectionId}. matchId ayarlanamýyor.");
            return;
        }

        Guid assignedRoomId = Guid.Empty;
        Room targetRoom = null;

        foreach (Room room in activeRooms.Values)
        {
            if (room.HasSpace && !room.IsGameStarted)
            {
                assignedRoomId = room.RoomId;
                targetRoom = room;
                break;
            }
        }

        if (targetRoom == null)
        {
            assignedRoomId = Guid.NewGuid();
            targetRoom = new Room(assignedRoomId, MAX_PLAYERS_PER_ROOM);
            activeRooms.Add(assignedRoomId, targetRoom);
            Debug.Log($"CustomRoomManager: Yeni oda oluþturuldu: {assignedRoomId}");

            if (managerPrefab != null)
            {
                GameObject gameManagerGO = Instantiate(managerPrefab);
                NetworkServer.Spawn(gameManagerGO);

                GameManager newGameManager = gameManagerGO.GetComponent<GameManager>();
                if (newGameManager != null)
                {
                    newGameManager.Initialize(assignedRoomId);
                    targetRoom.GameManagerNetIdentity = newGameManager.netIdentity;
                    Debug.Log($"Sunucu: Yeni GameManager {newGameManager.netId} objesi için matchId '{assignedRoomId}' olarak ayarlandý.");
                }
                else
                {
                    Debug.LogError("CustomRoomManager: Spawn edilen managerPrefab üzerinde GameManager bileþeni bulunamadý!");
                }
            }
            else
            {
                Debug.LogWarning("CustomRoomManager: managerPrefab atanmamýþ! Oda için GameManager spawn edilmeyecek.");
            }
        }

        targetRoom.AddPlayer(playerConnection);
        playerData.currentRoomId = assignedRoomId;
        playerData.status = "InRoom";

        gamePlayer.SetPlayerStateAndMatch("InRoom", assignedRoomId);

        Debug.Log($"CustomRoomManager: Oyuncu {playerConnection.connectionId} odaya katýldý: {assignedRoomId}. Odadaki oyuncu sayýsý: {targetRoom.Players.Count}");

        if (targetRoom.Players.Count == targetRoom.MaxPlayers && !targetRoom.IsGameStarted)
        {
            targetRoom.IsGameStarted = true;
            Debug.Log($"CustomRoomManager: Oda {assignedRoomId} doldu, oyun baþlayabilir!");

            if (targetRoom.GameManagerNetIdentity != null)
            {
                GameManager gameManagerForThisRoom = targetRoom.GameManagerNetIdentity.GetComponent<GameManager>();
                if (gameManagerForThisRoom != null)
                {
                    gameManagerForThisRoom.RpcStartGame();
                    // gameManagerForThisRoom.StartGameCountdown(5);
                }
            }
        }
    }

    [Server]
    public void LeaveRoom(NetworkConnectionToClient playerConnection, Guid roomId)
    {
        if (activeRooms.TryGetValue(roomId, out Room room))
        {
            room.RemovePlayer(playerConnection);
            Debug.Log($"CustomRoomManager: Oyuncu {playerConnection.connectionId} oda {roomId}'den manuel olarak ayrýldý. Odadaki kalan oyuncu: {room.Players.Count}");

            if (connections.TryGetValue(playerConnection, out Player playerData))
            {
                playerData.currentRoomId = Guid.Empty;
                playerData.status = "Search";
                GamePlayer gamePlayer = playerConnection.identity?.GetComponent<GamePlayer>();
                if (gamePlayer != null)
                {
                    gamePlayer.SetPlayerStateAndMatch("Search", Guid.Empty);
                }
            }

            if (room.IsEmpty)
            {
                CleanUpRoom(roomId, room);
            }
            else
            {
                if (room.GameManagerNetIdentity != null)
                {
                    GameManager gm = room.GameManagerNetIdentity.GetComponent<GameManager>();
                    if (gm != null) gm.RpcDisplayMessage($"Bir oyuncu odadan ayrýldý. Kalan oyuncu: {room.Players.Count}");
                }
            }
        }
    }
}

public class Room
{
    public Guid RoomId { get; private set; }
    public List<NetworkConnectionToClient> Players { get; private set; }
    public int MaxPlayers { get; private set; }
    public bool IsGameStarted { get; set; }
    public NetworkIdentity GameManagerNetIdentity { get; set; }

    public Room(Guid id, int maxPlayers)
    {
        RoomId = id;
        MaxPlayers = maxPlayers;
        Players = new List<NetworkConnectionToClient>();
        IsGameStarted = false;
        GameManagerNetIdentity = null;
    }

    public bool HasSpace => Players.Count < MaxPlayers;
    public bool IsEmpty => Players.Count == 0;

    public void AddPlayer(NetworkConnectionToClient conn)
    {
        if (!Players.Contains(conn))
        {
            if (HasSpace)
            {
                Players.Add(conn);
                Debug.Log($"Oda {RoomId}: Oyuncu {conn.connectionId} eklendi. Toplam: {Players.Count}");
            }
            else
            {
                Debug.LogWarning($"Oda {RoomId} dolu. Oyuncu {conn.connectionId} eklenemedi.");
            }
        }
        else
        {
            Debug.LogWarning($"Oda {RoomId}: Oyuncu {conn.connectionId} zaten odada.");
        }
    }

    public void RemovePlayer(NetworkConnectionToClient conn)
    {
        if (Players.Remove(conn))
        {
            Debug.Log($"Oda {RoomId}: Oyuncu {conn.connectionId} çýkarýldý. Kalan: {Players.Count}");
        }
        else
        {
            Debug.LogWarning($"Oda {RoomId}: Oyuncu {conn.connectionId} bu odada bulunamadý, çýkarýlamadý.");
        }
    }
}
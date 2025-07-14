using UnityEngine;
using UnityEngine.Tilemaps;
using Mirror;

public class Test : MonoBehaviour
{
    [Header("Test Settings")]
    [SerializeField] private char testCharacter = 'E';
    [SerializeField] private KeyCode testKey = KeyCode.Mouse0;

    [Header("References")]
    [SerializeField] private Tilemap targetTilemap;

    private GamePlayer localPlayer;
    private Camera mainCamera;

    private void Start()
    {
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogError("[Test] Main camera not found!");
            enabled = false;
            return;
        }

        if (targetTilemap == null)
        {
            targetTilemap = GetComponentInChildren<Tilemap>();
            if (targetTilemap == null)
            {
                Debug.LogError("[Test] No Tilemap found! Please assign one in the Inspector.");
                enabled = false;
                return;
            }
        }

        Debug.Log("[Test] Test script initialized");
    }

    private void Update()
    {
        if (localPlayer == null)
        {
            if (NetworkClient.localPlayer != null)
            {
                localPlayer = NetworkClient.localPlayer.GetComponent<GamePlayer>();
                if (localPlayer == null)
                {
                    Debug.LogWarning("[Test] Local player found but no GamePlayer component!");
                }
            }
            return;
        }

        HandleInput();
    }

    private void HandleInput()
    {
        if (Input.GetKeyDown(testKey))
        {
            HandleTileClick();
        }

        if (Input.GetKeyDown(KeyCode.Alpha1)) SendTileChange('A');
        if (Input.GetKeyDown(KeyCode.Alpha2)) SendTileChange('B');
        if (Input.GetKeyDown(KeyCode.Alpha3)) SendTileChange('C');
        if (Input.GetKeyDown(KeyCode.Alpha4)) SendTileChange('D');
        if (Input.GetKeyDown(KeyCode.Alpha5)) SendTileChange('E');
        if (Input.GetKeyDown(KeyCode.Space)) SendTileChange(' ');
    }

    private void HandleTileClick()
    {
        if (localPlayer == null || targetTilemap == null) return;

        Vector3 mouseWorldPosition = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPosition.z = 0; 

        Vector3Int logicalPosition = localPlayer.WorldToLogicalPosition(mouseWorldPosition);


        SendTileChange(testCharacter, logicalPosition);
    }

    private void SendTileChange(char character)
    {
        if (localPlayer == null) return;

        Vector3 mouseWorldPosition = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPosition.z = 0;

        Vector3Int logicalPosition = localPlayer.WorldToLogicalPosition(mouseWorldPosition);
        SendTileChange(character, logicalPosition);
    }

    private void SendTileChange(char character, Vector3Int logicalPosition)
    {
        if (localPlayer == null) return;

        Debug.Log($"[Test] Sending tile change: {logicalPosition} -> '{character}'");
        localPlayer.CmdRequestTileChange(logicalPosition, character);
    }

    private void OnGUI()
    {
        if (localPlayer == null) return;

        // Display instructions
        GUI.Label(new Rect(10, 10, 300, 20), "Click to place tile, or use number keys 1-5");
        GUI.Label(new Rect(10, 30, 300, 20), "Space to clear tile");
        GUI.Label(new Rect(10, 50, 300, 20), $"Current character: '{testCharacter}'");

        // Show mouse position info
        Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPos.z = 0;
        Vector3Int logicalPos = localPlayer.WorldToLogicalPosition(mouseWorldPos);

        GUI.Label(new Rect(10, 70, 300, 20), $"Mouse logical position: {logicalPos}");
    }
}
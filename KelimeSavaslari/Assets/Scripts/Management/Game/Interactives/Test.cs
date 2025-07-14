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
        // Get camera reference
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogError("[Test] Main camera not found!");
            enabled = false;
            return;
        }

        // Get tilemap if not assigned
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
        // Get local player reference if we don't have it
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

        // Handle input
        HandleInput();
    }

    private void HandleInput()
    {
        // Handle mouse click
        if (Input.GetKeyDown(testKey))
        {
            HandleTileClick();
        }

        // Handle keyboard input for different characters
        if (Input.GetKeyDown(KeyCode.Alpha1)) SendTileChange('A');
        if (Input.GetKeyDown(KeyCode.Alpha2)) SendTileChange('B');
        if (Input.GetKeyDown(KeyCode.Alpha3)) SendTileChange('C');
        if (Input.GetKeyDown(KeyCode.Alpha4)) SendTileChange('D');
        if (Input.GetKeyDown(KeyCode.Alpha5)) SendTileChange('E');
        if (Input.GetKeyDown(KeyCode.Space)) SendTileChange(' '); // Clear tile
    }

    private void HandleTileClick()
    {
        if (localPlayer == null || targetTilemap == null) return;

        // Get mouse position in world space
        Vector3 mouseWorldPosition = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPosition.z = 0; // Ensure z is 0 for 2D

        // Convert to logical position using GamePlayer's helper method
        Vector3Int logicalPosition = localPlayer.WorldToLogicalPosition(mouseWorldPosition);

        // Send tile change request
        SendTileChange(testCharacter, logicalPosition);
    }

    private void SendTileChange(char character)
    {
        if (localPlayer == null) return;

        // Use mouse position for tile selection
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
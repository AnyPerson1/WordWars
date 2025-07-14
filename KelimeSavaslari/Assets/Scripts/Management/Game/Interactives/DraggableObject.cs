using UnityEngine;
using UnityEngine.Tilemaps;
using Mirror; // NetworkClient.localPlayer ve Command çaðýrmak için hala Mirror gerekli

public class DraggableTileObject : MonoBehaviour
{
    private Vector3 initialPosition;
    private bool isDragging = false;
    private Tilemap targetTilemap; 
    private GamePlayer localGamePlayer; 

    [SerializeField] private char tileCharacter = 'E'; 

    void Start()
    {
        if (NetworkClient.localPlayer != null)
        {
            localGamePlayer = NetworkClient.localPlayer.GetComponent<GamePlayer>();
            if (localGamePlayer != null)
            {
                targetTilemap = localGamePlayer.GetComponentInChildren<Tilemap>();
            }
        }

        if (localGamePlayer == null) Debug.LogError("DraggableTileObject: Lokal GamePlayer bulunamadý.");
        if (targetTilemap == null) Debug.LogError("DraggableTileObject: Hedef Tilemap bulunamadý.");
    }

    void OnMouseDown()
    {
        if (localGamePlayer == null || !localGamePlayer.isLocalPlayer) return;

        initialPosition = transform.position;
        isDragging = true;
    }

    void OnMouseDrag()
    {
        if (!isDragging) return;

        Vector3 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mousePosition.z = transform.position.z;
        transform.position = mousePosition;
    }

    void OnMouseUp()
    {
        if (!isDragging) return;

        isDragging = false;

        if (targetTilemap != null && localGamePlayer != null)
        {
            Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector3Int cellPosition = targetTilemap.WorldToCell(mouseWorldPos);
            //int logicalCol = cellPosition.x - localGamePlayer.tilemapOffsetX;
            //int logicalRow = cellPosition.y - localGamePlayer.tilemapOffsetY;

            //localGamePlayer.CmdAttemptTileChange(logicalRow, logicalCol, tileCharacter);
            transform.position = initialPosition;
        }
        else
        {

            transform.position = initialPosition;
        }
    }
}
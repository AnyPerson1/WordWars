using UnityEngine;
using UnityEngine.Tilemaps; // Tilemap bileþeni için

public class CameraFitToTilemap : MonoBehaviour
{
    [SerializeField]
    private Tilemap targetTilemap; // Ekraný sýðdýrmak istediðiniz Tilemap'i buraya sürükleyin

    [SerializeField]
    [Range(0f, 5f)]
    private float padding = 1.0f; // Tilemap çevresinde býrakýlacak opsiyonel boþluk (dünya birimi)

    private Camera mainCamera;

    void Start()
    {
        mainCamera = Camera.main;

        if (mainCamera == null)
        {
            Debug.LogError("Sahneye etiketlenmiþ 'MainCamera' bulunamadý! Lütfen kameranýzý 'MainCamera' olarak etiketleyin.");
            return;
        }

        if (targetTilemap == null)
        {
            Debug.LogError("Hedef Tilemap atanmadý! Lütfen Inspector'da 'Target Tilemap' alanýna bir Tilemap sürükleyin.");
            return;
        }

        FitTilemapToScreen();
    }

    /// <summary>
    /// Sadece üzerinde sprite olan (boyanmýþ) tile'larý hesaba katarak kamerayý tilemap'e sýðdýrýr.
    /// </summary>
    public void FitTilemapToScreen()
    {
        // 1. Minimum ve Maksimum Tile Konumlarýný Dünya Koordinatlarýnda Bulma
        // Baþlangýçta min/max deðerlerini çok büyük/küçük sayýlarla baþlatýyoruz
        Vector3 minWorldPos = new Vector3(float.MaxValue, float.MaxValue, 0f);
        Vector3 maxWorldPos = new Vector3(float.MinValue, float.MinValue, 0f);

        bool foundAnyTile = false; // Tilemap'te hiç boyanmýþ tile var mý kontrolü

        // Tilemap'in mantýksal sýnýrlarý içinde döngü yapýyoruz.
        // CellBounds, tilemap üzerinde tile'larýn yerleþtirilebileceði potansiyel alaný temsil eder.
        BoundsInt cellBounds = targetTilemap.cellBounds;

        for (int x = cellBounds.xMin; x < cellBounds.xMax; x++)
        {
            for (int y = cellBounds.yMin; y < cellBounds.yMax; y++)
            {
                // Z genellikle 2D'de 0'dýr, ancak tilemap'in derinliðini de içerebilir
                Vector3Int cellPosition = new Vector3Int(x, y, cellBounds.z);

                // Bu hücrede gerçekten bir tile var mý kontrol et
                // HasTile() metodu, GetTile() == null kontrolünden daha verimlidir.
                if (targetTilemap.HasTile(cellPosition))
                {
                    foundAnyTile = true;

                    // Tile'ýn dünya koordinatlarýndaki sol alt köþesini al
                    Vector3 tileWorldMin = targetTilemap.CellToWorld(cellPosition);
                    // Tile'ýn dünya koordinatlarýndaki sað üst köþesini hesapla
                    // targetTilemap.cellSize, Grid'in Cell Size'ý ve Tilemap'in Scale'ý ile çarpýlmýþ dünya biriminde boyuttur.
                    Vector3 tileWorldMax = tileWorldMin + targetTilemap.cellSize;

                    // Minimum ve maksimum dünya koordinatlarýný güncelle
                    minWorldPos.x = Mathf.Min(minWorldPos.x, tileWorldMin.x);
                    minWorldPos.y = Mathf.Min(minWorldPos.y, tileWorldMin.y);
                    maxWorldPos.x = Mathf.Max(maxWorldPos.x, tileWorldMax.x);
                    maxWorldPos.y = Mathf.Max(maxWorldPos.y, tileWorldMax.y);
                }
            }
        }

        if (!foundAnyTile)
        {
            Debug.LogWarning("Hedef Tilemap üzerinde hiç boyanmýþ tile bulunamadý. Kamera ayarlanamadý.");
            return;
        }

        // Bulunan min ve max deðerleriyle yeni bir Bounds (sýnýr kutusu) oluþtur
        Bounds customTilemapBounds = new Bounds();
        customTilemapBounds.SetMinMax(minWorldPos, maxWorldPos);


        // 2. Padding ve Kamera Hesaplamasý (Bu kýsým önceki koddan ayný kalýr)
        Vector3 paddedSize = customTilemapBounds.size + new Vector3(padding * 2, padding * 2, 0);

        float tilemapAspect = paddedSize.x / paddedSize.y;
        float cameraAspect = mainCamera.aspect;

        float newOrthographicSize;

        if (tilemapAspect >= cameraAspect)
        {
            // Tilemap, kameradan daha geniþ veya ayný oranda.
            // Geniþliði sýðdýrmak için kameranýn yüksekliðini ayarlamamýz gerekiyor.
            newOrthographicSize = (paddedSize.x / cameraAspect) / 2f;
        }
        else
        {
            // Tilemap, kameradan daha uzun.
            // Yüksekliði sýðdýrmak için kameranýn orthographicSize'ýný doðrudan kullanabiliriz.
            newOrthographicSize = paddedSize.y / 2f;
        }

        mainCamera.orthographicSize = newOrthographicSize;

        // Kamerayý hesaplanan sýnýrlarýn merkezine konumlandýr
        Vector3 cameraPosition = Vector3.zero;
        // Kameranýn mevcut Z konumunu koru (2D için genellikle -10 gibi bir deðerdir)
        cameraPosition.z = mainCamera.transform.position.z;
        mainCamera.transform.position = cameraPosition;

        Debug.Log("Tilemap ekrana sýðdýrýldý. Yeni Orthographic Boyut: " + newOrthographicSize);
    }
}
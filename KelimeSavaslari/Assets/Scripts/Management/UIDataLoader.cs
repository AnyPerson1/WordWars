using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System;
using System.Text;

public class UIDataLoader : MonoBehaviour
{
    [Header("Player Data ScriptableObject")]
    [SerializeField] public PlayerData data;

    [Header("UI Referanslarý")]
    [SerializeField] public Image profilePhoto;
    [SerializeField] public Button SearchGame;
    [SerializeField] public TextMeshProUGUI level;
    [SerializeField] public TextMeshProUGUI username;
    [SerializeField] public TextMeshProUGUI wins;
    [SerializeField] public TextMeshProUGUI loses;
    [SerializeField] public TextMeshProUGUI winsThisWeek;
    [SerializeField] public TextMeshProUGUI losesThisWeek;
    [SerializeField] public TextMeshProUGUI longestWord;

    [Header("Unenable on game")]
    [SerializeField] public GameObject[] objects;

    private void Start()
    {
        LoadDataIntoUI();
        SearchGame.onClick.AddListener(OnSearchGame);
    }

    public void OnSearchGame()
    {
        foreach (GameObject obj in objects)
        {
            obj.SetActive(false);
        }
        CustomRoomManager.Instance.StartHost();
        CustomRoomManager.Instance.StartClient();
        FindFirstObjectByType<GamePlayer>().CmdTryJoinGame();
    }

    public void LoadDataIntoUI()
    {
        if (data == null)
        {
            Debug.LogError("[UIDataLoader] PlayerData ScriptableObject'i atanmamýþ! Lütfen Inspector'dan atayýn.");
            return;
        }

        if (username != null)
        {
            username.text = data.username;
        }
        if (level != null)
        {
            level.text = $"{data.level}";
        }
        if (wins != null)
        {
            wins.text = $"{data.win}";
        }
        if (loses != null)
        {
            loses.text = $"{data.lose}";
        }
        if (winsThisWeek != null)
        {
            winsThisWeek.text = $"{data.weeklyWin}";
        }
        if (losesThisWeek != null)
        {
            losesThisWeek.text = $"{data.weeklyLose}";
        }
        if (longestWord != null)
        {
            longestWord.text = $"{data.longestWord}";
        }

        if (profilePhoto != null && !string.IsNullOrEmpty(data.profilePicture))
        {
            try
            {
                byte[] imageBytes = Convert.FromBase64String(data.profilePicture);
                Texture2D texture = new Texture2D(1, 1);

                if (texture.LoadImage(imageBytes))
                {
                    Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
                    profilePhoto.sprite = sprite;
                }
                else
                {
                    Debug.LogWarning("[UIDataLoader] Profil fotoðrafý Base64 verisi geçersiz veya yüklenemedi.");
                }
            }
            catch (FormatException e)
            {
                Debug.LogError($"[UIDataLoader] Geçersiz Base64 string formatý: {e.Message}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[UIDataLoader] Profil fotoðrafý yüklenirken bilinmeyen hata: {e.Message}");
            }
        }
        else if (profilePhoto != null)
        {
            Debug.Log("[UIDataLoader] Profil fotoðrafý verisi boþ veya null.");
        }
    }
}


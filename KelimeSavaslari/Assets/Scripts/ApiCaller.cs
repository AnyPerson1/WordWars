using UnityEngine;
using System;
using System.Collections;
public class ApiCaller : MonoBehaviour
{
    public static ApiCaller Instance { get; private set; }

    [SerializeField] private NetworkData networkSettings;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (networkSettings != null)
        {
            ApiUtility.IpAddress = networkSettings.ipAdress;
            ApiUtility.Port = networkSettings.port;
        }
        else
        {
            Debug.LogWarning("[ApiCaller] NetworkData ScriptableObject is not assigned. Using default API settings (localhost:5000).");
        }
    }

    private string BaseUrl => $"http://{ApiUtility.IpAddress}:{ApiUtility.Port}";

    public void UpdateProfilePicture(string username, string base64Photo, Action onSuccess, Action<string> onFailure)
    {
        var data = new UpdateProfilePictureData { base64Photo = base64Photo };
        StartCoroutine(ApiUtility.Api<BaseResponse>($"{BaseUrl}/profile/{username}/photo", "POST", data,
            res =>
            {
                if (res.success)
                {
                    Debug.Log("🖼 Profil fotoğrafı güncellendi.");
                    onSuccess?.Invoke();
                }
                else
                {
                    Debug.LogError($"❌ Profil fotoğrafı güncellenemedi: {res.message}");
                    onFailure?.Invoke(res.message);
                }
            },
            errMsg =>
            {
                Debug.LogError($"❌ Fotoğraf güncellenemedi! Hata: {errMsg}");
                onFailure?.Invoke(errMsg);
            }
        ));
    }

    public void LevelUp(string username, Action<int> onSuccess, Action<string> onFailure)
    {
        StartCoroutine(ApiUtility.Api<LevelUpResponse>($"{BaseUrl}/level-up/{username}", "POST", null,
            res =>
            {
                if (res.success)
                {
                    Debug.Log($"🆙 Seviye atlandı! Yeni seviye: {res.newLevel}");
                    onSuccess?.Invoke(res.newLevel);
                }
                else
                {
                    Debug.LogError($"❌ Seviye atlatılamadı: {res.message}");
                    onFailure?.Invoke(res.message);
                }
            },
            errMsg =>
            {
                Debug.LogError($"❌ Level atlatılamadı! Hata: {errMsg}");
                onFailure?.Invoke(errMsg);
            }
        ));
    }

    public IEnumerator Register(string username, string password, PlayerData playerData, Action onSuccess, Action<string> onFailure)
    {
        var data = new AuthData { username = username, password = password };
        yield return ApiUtility.Api<AuthResponse>($"{BaseUrl}/register", "POST", data,
            res =>
            {
                if (res.success && res.user != null)
                {
                    playerData.PopulateFromUser(res.user);
                    onSuccess?.Invoke();
                }
                else
                {
                    Debug.LogError($"Kayıt başarısız: {res.message}");
                    onFailure?.Invoke(res.message);
                }
            },
            errMsg =>
            {
                Debug.LogError($"Kayıt API hatası: {errMsg}");
                onFailure?.Invoke(errMsg);
            });
    }

    public IEnumerator Login(string username, string password, PlayerData playerData, Action onSuccess, Action<string> onFailure)
    {
        var data = new AuthData { username = username, password = password };
        yield return ApiUtility.Api<AuthResponse>($"{BaseUrl}/login", "POST", data,
            res =>
            {
                if (res.success && res.user != null)
                {
                    playerData.PopulateFromUser(res.user);
                    onSuccess?.Invoke();
                }
                else
                {
                    Debug.LogError($"Giriş başarısız: {res.message}");
                    onFailure?.Invoke(res.message);
                }
            },
            errMsg =>
            {
                Debug.LogError($"Giriş API hatası: {errMsg}");
                onFailure?.Invoke(errMsg);
            });
    }

    public IEnumerator GetProfile(string username, PlayerData playerData, Action onSuccess, Action<string> onFailure)
    {
        yield return ApiUtility.Api<AuthResponse>($"{BaseUrl}/profile/{username}", "GET", null,
            res =>
            {
                if (res.success && res.user != null)
                {
                    playerData.PopulateFromUser(res.user);
                    onSuccess?.Invoke();
                }
                else
                {
                    Debug.LogError($"Profil alınamadı: {res.message}");
                    onFailure?.Invoke(res.message);
                }
            },
            errMsg =>
            {
                Debug.LogError($"Profil API hatası: {errMsg}");
                onFailure?.Invoke(errMsg);
            });
    }
}
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections;

public class AuthManager : MonoBehaviour
{
    [Header("UI Referanslarý")]
    [SerializeField] private GameObject loginRegisterScreen;
    [SerializeField] private TMP_InputField usernameInput;
    [SerializeField] private TMP_InputField passwordInput;
    [SerializeField] private Button registerButton;
    [SerializeField] private Button loginButton;
    [SerializeField] private Button playButton;
    [SerializeField] private Button quitButton;
    [SerializeField] private TextMeshProUGUI feedbackText;

    [Header("Game Data")]
    [SerializeField] private PlayerData playerData;

    private const string LoggedInKey = "LoggedIn";
    private const string UsernameKey = "Username";

    private void Awake()
    {
        if (ApiCaller.Instance == null)
        {
            Debug.LogError("ApiCaller instance not found! Please ensure it's in your scene and set up correctly.");
            enabled = false;
        }
    }

    private void Start()
    {
        registerButton.onClick.AddListener(OnRegisterButtonClicked);
        loginButton.onClick.AddListener(OnLoginButtonClicked);
        playButton.onClick.AddListener(OnPlayClicked);
        quitButton.onClick.AddListener(() => Application.Quit());

        // Check if user was previously logged in
        InitializeUIState();
    }

    private void InitializeUIState()
    {
        if (PlayerPrefs.GetInt(LoggedInKey, 0) == 1 && PlayerPrefs.HasKey(UsernameKey))
        {
            loginRegisterScreen.SetActive(false);
            playButton.gameObject.SetActive(true);
            quitButton.gameObject.SetActive(true);
            feedbackText.text = $"Hoþ geldin, {PlayerPrefs.GetString(UsernameKey)}!";
        }
        else
        {
            loginRegisterScreen.SetActive(true);
            playButton.gameObject.SetActive(false);
            quitButton.gameObject.SetActive(false);
            feedbackText.text = "Lütfen giriþ yapýn veya kayýt olun.";
        }
    }

    private void OnPlayClicked()
    {
        if (PlayerPrefs.GetInt(LoggedInKey, 0) == 1)
        {
            string username = PlayerPrefs.GetString(UsernameKey);
            feedbackText.text = "Profil yükleniyor...";
            StartCoroutine(ApiCaller.Instance.GetProfile(username, playerData,
                () =>
                {
                    feedbackText.text = $"Profil baþarýyla yüklendi: {playerData.username}";
                    ForwardToGameScene();
                },
                (errorMessage) =>
                {
                    feedbackText.text = $"Profil alýnamadý: {errorMessage}. Lütfen tekrar giriþ yapýn.";
                    PlayerPrefs.SetInt(LoggedInKey, 0); 
                    playerData.ClearData(); 
                    InitializeUIState(); 
                }));
        }
        else
        {
            InitializeUIState(); 
        }
    }

    private void ForwardToGameScene()
    {
        SceneManager.LoadScene(1); 
    }

    private void OnRegisterButtonClicked()
    {
        string username = usernameInput.text.Trim();
        string password = passwordInput.text.Trim();

        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            feedbackText.text = "Kullanýcý adý ve þifre boþ býrakýlamaz!";
            return;
        }

        feedbackText.text = "Kayýt olunuyor...";
        StartCoroutine(ApiCaller.Instance.Register(username, password, playerData,
            () =>
            {
                feedbackText.text = $"Kayýt baþarýlý! Hoþ geldin, {playerData.username}";
                PlayerPrefs.SetInt(LoggedInKey, 1);
                PlayerPrefs.SetString(UsernameKey, username);
                InitializeUIState();
            },
            (errorMessage) =>
            {
                feedbackText.text = $"Kayýt baþarýsýz: {errorMessage}";
                playerData.ClearData();
            }));
    }

    private void OnLoginButtonClicked()
    {
        string username = usernameInput.text.Trim();
        string password = passwordInput.text.Trim();

        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            feedbackText.text = "Kullanýcý adý ve þifre boþ býrakýlamaz!";
            return;
        }

        feedbackText.text = "Giriþ yapýlýyor...";
        StartCoroutine(ApiCaller.Instance.Login(username, password, playerData,
            () =>
            {
                feedbackText.text = $"Giriþ baþarýlý! Hoþ geldin, {playerData.username}";
                PlayerPrefs.SetInt(LoggedInKey, 1);
                PlayerPrefs.SetString(UsernameKey, username);
                InitializeUIState();
            },
            (errorMessage) =>
            {
                feedbackText.text = $"Giriþ baþarýsýz: {errorMessage}";
                playerData.ClearData();
            }));
    }
}
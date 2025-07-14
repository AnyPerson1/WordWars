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


    private void Start()
    {
        if (ApiCaller.Instance == null)
        {
            Debug.LogError("ApiCaller instance not found! Please ensure it's in your scene and set up correctly.");
            enabled = false;
        }
        registerButton.onClick.AddListener(OnRegisterButtonClicked);
        loginButton.onClick.AddListener(OnLoginButtonClicked);
        playButton.onClick.AddListener(OnPlayClicked);
        quitButton.onClick.AddListener(() => Application.Quit());

        InitializeUIState();
    }

    private void InitializeUIState()
    {
        if (PlayerPrefs.GetInt(LoggedInKey, 0) == 1 && PlayerPrefs.HasKey(UsernameKey))
        {
            loginRegisterScreen.SetActive(false);
            playButton.gameObject.SetActive(true);
            quitButton.gameObject.SetActive(true);
            feedbackText.text = $"Welcome, {PlayerPrefs.GetString(UsernameKey)}!";
        }
        else
        {
            loginRegisterScreen.SetActive(true);
            playButton.gameObject.SetActive(false);
            quitButton.gameObject.SetActive(false);
            feedbackText.text = "Please login or sign up.";
        }
    }

    private void OnPlayClicked()
    {
        if (PlayerPrefs.GetInt(LoggedInKey, 0) == 1)
        {
            string username = PlayerPrefs.GetString(UsernameKey);
            feedbackText.text = "Profile loading...";
            StartCoroutine(ApiCaller.Instance.GetProfile(username, playerData,
                () =>
                {
                    feedbackText.text = $"Profile succesfully loaded: {playerData.username}";
                    ForwardToGameScene();
                },
                (errorMessage) =>
                {
                    feedbackText.text = $"Profile couldn`t loaded: {errorMessage}. Please try again.";
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
            feedbackText.text = "Fields can`t be empty!";
            return;
        }

        feedbackText.text = "Signing...";
        StartCoroutine(ApiCaller.Instance.Register(username, password, playerData,
            () =>
            {
                feedbackText.text = $"Sign Successful, Welcome {playerData.username}";
                PlayerPrefs.SetInt(LoggedInKey, 1);
                PlayerPrefs.SetString(UsernameKey, username);
                InitializeUIState();
            },
            (errorMessage) =>
            {
                feedbackText.text = $"Sign unsuccessful: {errorMessage}";
                playerData.ClearData();
            }));
    }

    private void OnLoginButtonClicked()
    {
        string username = usernameInput.text.Trim();
        string password = passwordInput.text.Trim();

        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            feedbackText.text = "Fields can`t be empty!!";
            return;
        }

        feedbackText.text = "Logging in...";
        StartCoroutine(ApiCaller.Instance.Login(username, password, playerData,
            () =>
            {
                feedbackText.text = $"Logged successfully, welcome {playerData.username}";
                PlayerPrefs.SetInt(LoggedInKey, 1);
                PlayerPrefs.SetString(UsernameKey, username);
                InitializeUIState();
            },
            (errorMessage) =>
            {
                feedbackText.text = $"Login unsuccessful: {errorMessage}";
                playerData.ClearData();
            }));
    }
}
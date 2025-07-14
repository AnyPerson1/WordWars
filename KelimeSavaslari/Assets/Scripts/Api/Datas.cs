using System;
using System.Collections.Generic;
using UnityEngine;

// Base response for API calls that might just indicate success/failure or a message
[Serializable]
public class BaseResponse
{
    public bool success;
    public string message;
}

// User data received from the API
[Serializable]
public class User
{
    public string id;
    public string username;
    public int win;
    public int lose;
    public int level;
    public string profilePicture; // Assuming this is a URL or base64 string
    public string longestWord;
    public int weeklyWin;
    public int weeklyLose;
}

// Response for authentication (login/register)
[Serializable]
public class AuthResponse : BaseResponse
{
    public User user;
    // You might add a token here if your API returns one
    public string token;
}

// Response for LevelUp specifically
[Serializable]
public class LevelUpResponse : BaseResponse
{
    public int newLevel;
}

// Data to send for authentication requests
[Serializable]
public class AuthData
{
    public string username;
    public string password;
}

// Data for updating profile picture
[Serializable]
public class UpdateProfilePictureData
{
    public string base64Photo;
}

// ScriptableObject to hold player data globally
[CreateAssetMenu(menuName = "Player/PlayerData")]
public class PlayerData : ScriptableObject
{
    public string id;
    public string username;
    public int win;
    public int lose;
    public int level;
    public string profilePicture;
    public string longestWord;
    public int weeklyWin;
    public int weeklyLose;

    // Method to populate PlayerData from a User object
    public void PopulateFromUser(User user)
    {
        if (user == null) return;

        id = user.id;
        username = user.username;
        win = user.win;
        lose = user.lose;
        level = user.level;
        profilePicture = user.profilePicture;
        longestWord = user.longestWord;
        weeklyWin = user.weeklyWin;
        weeklyLose = user.weeklyLose;
    }

    public void ClearData()
    {
        id = string.Empty;
        username = string.Empty;
        win = 0;
        lose = 0;
        level = 0;
        profilePicture = string.Empty;
        longestWord = string.Empty;
        weeklyWin = 0;
        weeklyLose = 0;
    }
}

[CreateAssetMenu(menuName = "NetworkData/NetworkData")]
public class NetworkData : ScriptableObject
{
    public string ipAdress = "localhost";
    public int port = 5000;
}
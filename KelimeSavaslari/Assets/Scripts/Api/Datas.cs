using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class BaseResponse
{
    public bool success;
    public string message;
}

[Serializable]
public class User
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
}

[Serializable]
public class AuthResponse : BaseResponse
{
    public User user;
    public string token;
}

[Serializable]
public class LevelUpResponse : BaseResponse
{
    public int newLevel;
}

[Serializable]
public class AuthData
{
    public string username;
    public string password;
}

[Serializable]
public class UpdateProfilePictureData
{
    public string base64Photo;
}

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
using UnityEngine;
using UnityEngine.Networking;
using System;
using System.Collections;

public class ApiUtility : MonoBehaviour
{
    public static NetworkData networkData;
    public static IEnumerator Api<T>(string url, string method, object data, Action<T> callback)
    {

        UnityWebRequest request;

        if (method.ToUpper() == "GET")
        {
            request = UnityWebRequest.Get(url);
        }
        else if (method.ToUpper() == "POST")
        {
            string jsonData = JsonUtility.ToJson(data);
            request = new UnityWebRequest(url, "POST");
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
        }
        else
        {
            Debug.LogError("Yalnýzca GET ve POST!");
            yield break;
        }

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            T result = JsonUtility.FromJson<T>(request.downloadHandler.text);
            callback?.Invoke(result);
        }
        else
        {
            Debug.LogError("API hatasý: " + request.error);
        }
    }
}

[CreateAssetMenu(menuName = "NetworkData/NetworkData")]
public class NetworkData : ScriptableObject
{
    public string ipAdress = "localhost";
    public int port = 5000;
}
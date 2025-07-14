using UnityEngine;
using UnityEngine.Networking;
using System;
using System.Collections;
using System.Text;

public class ApiUtility : MonoBehaviour
{
    public static string IpAddress { get; set; } = "localhost";
    public static int Port { get; set; } = 3000;

    public static IEnumerator Api<T>(string url, string method, object data, Action<T> onSuccess, Action<string> onFailure)
    {
        UnityWebRequest request;

        string normalizedMethod = method.ToUpper();

        if (normalizedMethod == "GET")
        {
            request = UnityWebRequest.Get(url);
        }
        else if (normalizedMethod == "POST")
        {
            request = new UnityWebRequest(url, "POST");
            if (data != null)
            {
                string jsonData = JsonUtility.ToJson(data);
                byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.SetRequestHeader("Content-Type", "application/json");
            }
            request.downloadHandler = new DownloadHandlerBuffer();
        }
        else
        {
            Debug.LogError($"[ApiUtility] Invalid HTTP method: {method}. Only GET and POST are supported.");
            onFailure?.Invoke("Invalid HTTP method specified.");
            yield break;
        }

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            try
            {
                T result = JsonUtility.FromJson<T>(request.downloadHandler.text);
                onSuccess?.Invoke(result);
            }
            catch (Exception e)
            {
                Debug.LogError($"[ApiUtility] JSON parsing error for {url}: {e.Message}\nResponse: {request.downloadHandler.text}");
                onFailure?.Invoke($"Failed to parse API response: {e.Message}");
            }
        }
        else
        {
            string errorMessage = $"[ApiUtility] API Request Error: {request.error} (URL: {url}, Code: {request.responseCode})";
            Debug.LogError(errorMessage);
            onFailure?.Invoke(request.error);
        }
    }
}
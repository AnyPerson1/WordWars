using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class DictionaryManager : MonoBehaviour
{
    public static DictionaryManager Instance { get; private set; }
    private readonly HashSet<string> validWords = new HashSet<string>();

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Sahne deðiþse bile sözlüðün kaybolmamasýný saðlar
            LoadDictionary();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void LoadDictionary()
    {
        string filePath = Path.Combine(Application.streamingAssetsPath, "words.txt");

        if (File.Exists(filePath))
        {
            string[] words = File.ReadAllLines(filePath);
            foreach (string word in words)
            {
                // Kelimeleri büyük harfe çevirip boþluklarý temizleyerek ekle
                validWords.Add(word.Trim().ToUpper());
            }
            Debug.Log($"[DictionaryManager] Sözlük yüklendi: {validWords.Count} kelime.");
        }
        else
        {
            Debug.LogError($"[DictionaryManager] Sözlük dosyasý bulunamadý: {filePath}");
        }
    }

    public bool IsValidWord(string word)
    {
        if (string.IsNullOrEmpty(word)) return false;
        return validWords.Contains(word.ToUpper());
    }
}
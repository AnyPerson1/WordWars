using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;

[System.Serializable]
public class LetterInfo
{
    public char letter;
    public int count;
    public int points;
    public TileBase tileAsset;
}

public class LetterPoolManager : MonoBehaviour
{

    [Header("Turkish Letter Distribution")]
    public LetterInfo[] letterDistribution = new LetterInfo[]
    {
        new LetterInfo { letter = 'A', count = 12, points = 1 },
        new LetterInfo { letter = 'B', count = 3, points = 3 },
        new LetterInfo { letter = 'C', count = 4, points = 4 },
        new LetterInfo { letter = 'Ç', count = 4, points = 4 },
        new LetterInfo { letter = 'D', count = 4, points = 3 },
        new LetterInfo { letter = 'E', count = 12, points = 1 },
        new LetterInfo { letter = 'F', count = 2, points = 7 },
        new LetterInfo { letter = 'G', count = 3, points = 5 },
        new LetterInfo { letter = 'Ð', count = 2, points = 8 },
        new LetterInfo { letter = 'H', count = 2, points = 5 },
        new LetterInfo { letter = 'I', count = 9, points = 1 },
        new LetterInfo { letter = 'Ý', count = 7, points = 1 },
        new LetterInfo { letter = 'J', count = 1, points = 10 },
        new LetterInfo { letter = 'K', count = 7, points = 1 },
        new LetterInfo { letter = 'L', count = 7, points = 1 },
        new LetterInfo { letter = 'M', count = 4, points = 2 },
        new LetterInfo { letter = 'N', count = 6, points = 1 },
        new LetterInfo { letter = 'O', count = 3, points = 2 },
        new LetterInfo { letter = 'Ö', count = 2, points = 7 },
        new LetterInfo { letter = 'P', count = 2, points = 5 },
        new LetterInfo { letter = 'R', count = 6, points = 1 },
        new LetterInfo { letter = 'S', count = 5, points = 2 },
        new LetterInfo { letter = 'Þ', count = 3, points = 4 },
        new LetterInfo { letter = 'T', count = 5, points = 1 },
        new LetterInfo { letter = 'U', count = 3, points = 2 },
        new LetterInfo { letter = 'Ü', count = 3, points = 3 },
        new LetterInfo { letter = 'V', count = 2, points = 7 },
        new LetterInfo { letter = 'Y', count = 3, points = 3 },
        new LetterInfo { letter = 'Z', count = 2, points = 4 }
    };

    private List<char> letterPool = new List<char>();
    private Dictionary<char, int> letterPoints = new Dictionary<char, int>();
    private Dictionary<char, TileBase> letterTiles = new Dictionary<char, TileBase>();

    public void InitializePool()
    {
        letterPool.Clear();
        letterPoints.Clear();
        letterTiles.Clear();

        foreach (var letterInfo in letterDistribution)
        {
            letterPoints[letterInfo.letter] = letterInfo.points;
            letterTiles[letterInfo.letter] = letterInfo.tileAsset;

            for (int i = 0; i < letterInfo.count; i++)
            {
                letterPool.Add(letterInfo.letter);
            }
        }

        // Karýþtýr
        for (int i = 0; i < letterPool.Count; i++)
        {
            char temp = letterPool[i];
            int randomIndex = Random.Range(i, letterPool.Count);
            letterPool[i] = letterPool[randomIndex];
            letterPool[randomIndex] = temp;
        }
    }

    public char[] DrawLetters(int count)
    {
        List<char> drawnLetters = new List<char>();

        for (int i = 0; i < count && letterPool.Count > 0; i++)
        {
            char letter = letterPool[letterPool.Count - 1];
            letterPool.RemoveAt(letterPool.Count - 1);
            drawnLetters.Add(letter);
        }

        return drawnLetters.ToArray();
    }

    public void ReturnLetters(char[] letters)
    {
        letterPool.AddRange(letters);
    }

    public int GetLetterPoints(char letter)
    {
        return letterPoints.ContainsKey(letter) ? letterPoints[letter] : 0;
    }

    public bool HasLettersRemaining()
    {
        return letterPool.Count > 0;
    }

    public TileBase GetTileForChar(char letter)
    {
        if (letterTiles.ContainsKey(letter))
        {
            return letterTiles[letter];
        }
        Debug.LogWarning($"Tile asset for character '{letter}' not found in LetterPoolManager.");
        return null;
    }
}
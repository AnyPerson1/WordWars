using UnityEngine;
using System;
using UnityEngine.Tilemaps;

[System.Serializable]
public class Player
{
    public Guid currentRoomId;
    public string status;
    public bool isTurn;
    public int wins;
    public int score;
    public int loses;
    public int level;

    public Player()
    {
        currentRoomId = Guid.Empty;
        status = "None";
    }
}

[Serializable] // SyncList içinde kullanýlabilmesi için
public struct LogicalTile
{
    public char letter;
    public bool isChangeable;

    public override string ToString()
    {
        return $"({letter}, {isChangeable})";
    }

    // Eþitlik kontrolü için de faydalý
    public override bool Equals(object obj)
    {
        if (obj is LogicalTile other)
        {
            return letter == other.letter && isChangeable == other.isChangeable;
        }
        return false;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(letter, isChangeable);
    }

    public static bool operator ==(LogicalTile a, LogicalTile b) => a.Equals(b);
    public static bool operator !=(LogicalTile a, LogicalTile b) => !a.Equals(b);
}

public static class ArrayConverter
{
    public static T[] Flatten2DArray<T>(T[,] array)
    {
        if (array == null)
        {
            Debug.LogError("Flatten2DArray: Giriþ dizisi null olamaz.");
            return null;
        }

        int rows = array.GetLength(0);
        int cols = array.GetLength(1); 
        T[] flatArray = new T[rows * cols];

        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                flatArray[r * cols + c] = array[r, c];
            }
        }
        return flatArray;
    }


    public static T[,] Unflatten1DArray<T>(T[] flatArray, int rows, int cols)
    {
        if (flatArray == null)
        {
            Debug.LogError("Unflatten1DArray: Giriþ dizisi null olamaz.");
            return null;
        }
        if (rows <= 0 || cols <= 0)
        {
            Debug.LogError("Unflatten1DArray: Satýr ve sütun sayýlarý pozitif olmalýdýr.");
            return null;
        }
        if (flatArray.Length != rows * cols)
        {
            Debug.LogError("Unflatten1DArray: Tek boyutlu dizinin boyutu, belirtilen satýr ve sütun sayýlarýna uymuyor.");
            return null;
        }

        T[,] unflattenedArray = new T[rows, cols];

        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                unflattenedArray[r, c] = flatArray[r * cols + c];
            }
        }
        return unflattenedArray;
    }
}

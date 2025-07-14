using UnityEngine;
using System;

#region Player Data

[Serializable]
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
        isTurn = false;
        wins = 0;
        score = 0;
        loses = 0;
        level = 1;
    }
}

#endregion

#region Tile Data

[Serializable]
public struct LogicalTile
{
    public char letter;
    public bool isChangeable;

    public LogicalTile(char letter, bool isChangeable = true)
    {
        this.letter = letter;
        this.isChangeable = isChangeable;
    }

    public override string ToString()
    {
        return $"Tile('{letter}', changeable={isChangeable})";
    }

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

    // Helper methods
    public bool IsEmpty => letter == ' ';
    public bool IsLetter => char.IsLetter(letter);
    public bool IsValid => letter != '\0';
}

#endregion

#region Array Utilities

public static class ArrayConverter
{
    /// <summary>
    /// Converts a 2D array to a 1D array for network transmission
    /// </summary>
    public static T[] Flatten2DArray<T>(T[,] array)
    {
        if (array == null)
        {
            Debug.LogError("[ArrayConverter] Cannot flatten null array");
            return null;
        }

        int width = array.GetLength(0);
        int height = array.GetLength(1);
        T[] flatArray = new T[width * height];

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                flatArray[x * height + y] = array[x, y];
            }
        }

        return flatArray;
    }

    /// <summary>
    /// Converts a 1D array back to a 2D array after network transmission
    /// </summary>
    public static T[,] Unflatten1DArray<T>(T[] flatArray, int width, int height)
    {
        if (flatArray == null)
        {
            Debug.LogError("[ArrayConverter] Cannot unflatten null array");
            return null;
        }

        if (width <= 0 || height <= 0)
        {
            Debug.LogError("[ArrayConverter] Width and height must be positive");
            return null;
        }

        if (flatArray.Length != width * height)
        {
            Debug.LogError($"[ArrayConverter] Array size mismatch. Expected {width * height}, got {flatArray.Length}");
            return null;
        }

        T[,] array = new T[width, height];

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                array[x, y] = flatArray[x * height + y];
            }
        }

        return array;
    }

    /// <summary>
    /// Creates a copy of a 2D array
    /// </summary>
    public static T[,] Copy2DArray<T>(T[,] original)
    {
        if (original == null) return null;

        int width = original.GetLength(0);
        int height = original.GetLength(1);
        T[,] copy = new T[width, height];

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                copy[x, y] = original[x, y];
            }
        }

        return copy;
    }

    /// <summary>
    /// Validates if coordinates are within array bounds
    /// </summary>
    public static bool IsValidPosition<T>(T[,] array, int x, int y)
    {
        if (array == null) return false;
        return x >= 0 && x < array.GetLength(0) && y >= 0 && y < array.GetLength(1);
    }
}

#endregion

#region Debug Utilities

public static class DebugHelper
{
    /// <summary>
    /// Prints a 2D array to console for debugging
    /// </summary>
    public static void PrintArray<T>(T[,] array, string title = "Array")
    {
        if (array == null)
        {
            Debug.Log($"[{title}] Array is null");
            return;
        }

        int width = array.GetLength(0);
        int height = array.GetLength(1);

        Debug.Log($"[{title}] {width}x{height} Array:");

        for (int y = height - 1; y >= 0; y--) // Print from top to bottom
        {
            string row = "";
            for (int x = 0; x < width; x++)
            {
                row += $"[{array[x, y]}] ";
            }
            Debug.Log($"Row {y}: {row}");
        }
    }

    /// <summary>
    /// Logs a message with timestamp and context
    /// </summary>
    public static void LogWithContext(string message, string context = "Game")
    {
        Debug.Log($"[{context}] {System.DateTime.Now:HH:mm:ss.fff} - {message}");
    }
}

#endregion
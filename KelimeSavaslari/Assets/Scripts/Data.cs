using System;
using UnityEngine;

public class ImageSerialize
{
    public static string SpriteToBase64(Sprite sprite)
    {
        Texture2D texture = sprite.texture;

        byte[] pngData = texture.EncodeToPNG();
        return Convert.ToBase64String(pngData);
    }

    public static Sprite Base64ToSprite(string base64)
    {
        byte[] imageBytes = Convert.FromBase64String(base64);

        Texture2D texture = new Texture2D(2, 2);
        if (!texture.LoadImage(imageBytes))
        {
            Debug.LogError("Base64 string'ten görüntü yüklenemedi.");
            return null;
        }
        Rect rect = new Rect(0, 0, texture.width, texture.height);
        Vector2 pivot = new Vector2(0.5f, 0.5f); // ortalý pivot
        return Sprite.Create(texture, rect, pivot);
    }
}

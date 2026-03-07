using Godot;

public static class CursorManager
{
    private const string PointerTexturePath = "res://Assets/others/pointer_chroma.png";
    private const int PointerMaxHeight = 96;
    private const int PointerMinAlpha = 8;
    private const float PointerScaleFactor = 0.3333f;

    public static void ApplyDefaultCursor()
    {
        Texture2D pointerTexture = ResourceLoader.Load<Texture2D>(PointerTexturePath);
        if (pointerTexture == null)
        {
            GD.PushWarning($"Pointer texture not found: {PointerTexturePath}");
            return;
        }

        Image pointerImage = pointerTexture.GetImage();
        if (pointerImage == null)
        {
            GD.PushWarning("Failed to read pointer image data.");
            return;
        }

        Rect2I opaqueRect = FindOpaqueRect(pointerImage);
        if (opaqueRect.Size == Vector2I.Zero)
        {
            GD.PushWarning("Pointer image is fully transparent.");
            return;
        }

        Image croppedImage = pointerImage.GetRegion(opaqueRect);
        int baseHeight = Mathf.Min(PointerMaxHeight, croppedImage.GetHeight());
        int targetHeight = Mathf.Max(1, Mathf.RoundToInt(baseHeight * PointerScaleFactor));
        int targetWidth = Mathf.Max(1, Mathf.RoundToInt(croppedImage.GetWidth() * (targetHeight / (float)croppedImage.GetHeight())));
        croppedImage.Resize(targetWidth, targetHeight, Image.Interpolation.Nearest);

        Texture2D customCursor = ImageTexture.CreateFromImage(croppedImage);
        Input.SetCustomMouseCursor(customCursor, Input.CursorShape.Arrow, new Vector2(2, 2));
    }

    private static Rect2I FindOpaqueRect(Image image)
    {
        int minX = image.GetWidth();
        int minY = image.GetHeight();
        int maxX = -1;
        int maxY = -1;

        for (int y = 0; y < image.GetHeight(); y++)
        {
            for (int x = 0; x < image.GetWidth(); x++)
            {
                if (image.GetPixel(x, y).A * 255.0f < PointerMinAlpha)
                {
                    continue;
                }

                if (x < minX)
                {
                    minX = x;
                }

                if (y < minY)
                {
                    minY = y;
                }

                if (x > maxX)
                {
                    maxX = x;
                }

                if (y > maxY)
                {
                    maxY = y;
                }
            }
        }

        if (maxX < minX || maxY < minY)
        {
            return new Rect2I();
        }

        return new Rect2I(minX, minY, maxX - minX + 1, maxY - minY + 1);
    }
}

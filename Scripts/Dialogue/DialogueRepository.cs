using Godot;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

public static class DialogueRepository
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip
    };

    public static DialogueSceneDefinition LoadScene(string resourcePath)
    {
        if (string.IsNullOrWhiteSpace(resourcePath))
        {
            return new DialogueSceneDefinition();
        }

        string absolutePath = ProjectSettings.GlobalizePath(resourcePath);
        if (!File.Exists(absolutePath))
        {
            GD.PushWarning($"Dialogue scene json not found: {resourcePath}");
            return new DialogueSceneDefinition();
        }

        string json = File.ReadAllText(absolutePath);
        return JsonSerializer.Deserialize<DialogueSceneDefinition>(json, SerializerOptions) ?? new DialogueSceneDefinition();
    }

    public static Dictionary<string, DialogueCharacterDefinition> LoadCharacters(string resourcePath)
    {
        if (string.IsNullOrWhiteSpace(resourcePath))
        {
            return new Dictionary<string, DialogueCharacterDefinition>();
        }

        string absolutePath = ProjectSettings.GlobalizePath(resourcePath);
        if (!File.Exists(absolutePath))
        {
            GD.PushWarning($"Dialogue character json not found: {resourcePath}");
            return new Dictionary<string, DialogueCharacterDefinition>();
        }

        string json = File.ReadAllText(absolutePath);
        return JsonSerializer.Deserialize<Dictionary<string, DialogueCharacterDefinition>>(json, SerializerOptions)
            ?? new Dictionary<string, DialogueCharacterDefinition>();
    }
}

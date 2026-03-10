using Godot;
using System.Collections.Generic;

public sealed class CharacterRepository
{
    private readonly Dictionary<string, DialogueCharacterDefinition> _definitions;
    private readonly Dictionary<string, Texture2D> _portraitCache = new();
    private readonly Dictionary<string, Texture2D> _sModelCache = new();

    private CharacterRepository(Dictionary<string, DialogueCharacterDefinition> definitions)
    {
        _definitions = definitions;
    }

    public static CharacterRepository LoadFrom(string resourcePath)
    {
        return new CharacterRepository(DialogueRepository.LoadCharacters(resourcePath));
    }

    public DialogueCharacterDefinition GetDefinition(string characterId)
    {
        if (!string.IsNullOrWhiteSpace(characterId) && _definitions.TryGetValue(characterId, out DialogueCharacterDefinition definition))
        {
            return definition;
        }

        return new DialogueCharacterDefinition
        {
            DisplayName = characterId,
            DefaultPortrait = "normal"
        };
    }

    public string GetDisplayName(string characterId)
    {
        DialogueCharacterDefinition definition = GetDefinition(characterId);
        return string.IsNullOrWhiteSpace(definition.DisplayName) ? characterId : definition.DisplayName;
    }

    public float GetPortraitScale(string characterId)
    {
        return GetDefinition(characterId).PortraitScale;
    }

    public float GetSModelScale(string characterId)
    {
        return GetDefinition(characterId).SModelScale;
    }

    public Texture2D GetPortrait(string characterId, string expression)
    {
        DialogueCharacterDefinition definition = GetDefinition(characterId);
        string resolvedExpression = expression;
        if (string.IsNullOrWhiteSpace(resolvedExpression) || !definition.Portraits.ContainsKey(resolvedExpression))
        {
            resolvedExpression = definition.DefaultPortrait;
        }

        string portraitPath = string.Empty;
        if (!string.IsNullOrWhiteSpace(resolvedExpression)
            && definition.Portraits.TryGetValue(resolvedExpression, out string resolvedPortraitPath))
        {
            portraitPath = resolvedPortraitPath;
        }
        else
        {
            foreach (KeyValuePair<string, string> pair in definition.Portraits)
            {
                portraitPath = pair.Value;
                break;
            }
        }

        if (string.IsNullOrWhiteSpace(portraitPath))
        {
            return null;
        }

        string cacheKey = $"{characterId}:{resolvedExpression}:{portraitPath}";
        if (_portraitCache.TryGetValue(cacheKey, out Texture2D cachedTexture))
        {
            return cachedTexture;
        }

        Texture2D texture = ResourceLoader.Load<Texture2D>(portraitPath);
        if (texture == null)
        {
            GD.PushWarning($"Failed to load portrait texture: {portraitPath}");
            return null;
        }

        _portraitCache[cacheKey] = texture;
        return texture;
    }

    public Texture2D GetSModelSheet(string characterId)
    {
        DialogueCharacterDefinition definition = GetDefinition(characterId);
        if (string.IsNullOrWhiteSpace(definition.SModelSheet))
        {
            return null;
        }

        if (_sModelCache.TryGetValue(characterId, out Texture2D cachedTexture))
        {
            return cachedTexture;
        }

        Texture2D texture = ResourceLoader.Load<Texture2D>(definition.SModelSheet);
        if (texture == null)
        {
            GD.PushWarning($"Failed to load s-model sheet: {definition.SModelSheet}");
            return null;
        }

        _sModelCache[characterId] = texture;
        return texture;
    }
}

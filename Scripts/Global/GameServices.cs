using Godot;

public partial class GameServices : Node
{
    public static GameServices Instance { get; private set; }

    [Export]
    public string CharactersPath { get; set; } = "res://Assets/dialogue/characters.json";

    private CharacterRepository _characters;

    public CharacterRepository Characters
    {
        get
        {
            _characters ??= CharacterRepository.LoadFrom(CharactersPath);
            return _characters;
        }
    }

    public override void _EnterTree()
    {
        if (Instance != null && Instance != this)
        {
            GD.PushWarning("Duplicate GameServices instance detected.");
        }

        Instance = this;
    }

    public override void _Ready()
    {
        _ = Characters;
    }

    public override void _ExitTree()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}

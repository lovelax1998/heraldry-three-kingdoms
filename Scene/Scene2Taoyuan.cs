using Godot;

public partial class Scene2Taoyuan : Control
{
    [Export] public string HeaderChapterText { get; set; } = "第一章 · 桃园四结义";
    [Export] public string HeaderLocationText { get; set; } = "涿郡 · 白日";
    [Export] public bool HeaderFadeIn { get; set; } = true;

    [ExportGroup("Actors")]
    [Export] public string LiubeiCharacterId { get; set; } = "liubei";
    [Export] public string LiubeiFacing { get; set; } = "right";
    [Export] public float LiubeiScale { get; set; } = 0.575f;
    [Export] public string GuanyuCharacterId { get; set; } = "guanyu";
    [Export] public string GuanyuFacing { get; set; } = "right";
    [Export] public float GuanyuScale { get; set; } = 0.575f;
    [Export] public string ZhangfeiCharacterId { get; set; } = "zhangfei";
    [Export] public string ZhangfeiFacing { get; set; } = "left";
    [Export] public float ZhangfeiScale { get; set; } = 0.575f;

    private CharacterRepository _characterRepository;
    private SModelActor _liubeiActor;
    private SModelActor _guanyuActor;
    private SModelActor _zhangfeiActor;
    private Marker2D _liubeiMarker;
    private Marker2D _guanyuMarker;
    private Marker2D _zhangfeiMarker;

    public override void _Ready()
    {
        CursorManager.ApplyDefaultCursor();
        GameUi.Instance?.HideStoryHeader();
        GameUi.Instance?.ShowStoryHeader(HeaderChapterText, HeaderLocationText, HeaderFadeIn);

        _characterRepository = GameServices.Instance?.Characters;
        if (_characterRepository == null)
        {
            GD.PushError("CharacterRepository is not available. Check GameServices autoload.");
            return;
        }

        _liubeiActor = GetNodeOrNull<SModelActor>("WorldRoot/LiubeiActor");
        _guanyuActor = GetNodeOrNull<SModelActor>("WorldRoot/GuanyuActor");
        _zhangfeiActor = GetNodeOrNull<SModelActor>("WorldRoot/ZhangfeiActor");
        _liubeiMarker = GetNodeOrNull<Marker2D>("WorldRoot/LiubeiMarker");
        _guanyuMarker = GetNodeOrNull<Marker2D>("WorldRoot/GuanyuMarker");
        _zhangfeiMarker = GetNodeOrNull<Marker2D>("WorldRoot/ZhangfeiMarker");

        SetupActor(_liubeiActor, _liubeiMarker, LiubeiCharacterId, LiubeiFacing, LiubeiScale);
        SetupActor(_guanyuActor, _guanyuMarker, GuanyuCharacterId, GuanyuFacing, GuanyuScale);
        SetupActor(_zhangfeiActor, _zhangfeiMarker, ZhangfeiCharacterId, ZhangfeiFacing, ZhangfeiScale);
    }

    private void SetupActor(SModelActor actor, Marker2D marker, string characterId, string facing, float scale)
    {
        if (actor == null || marker == null || string.IsNullOrWhiteSpace(characterId))
        {
            return;
        }

        Texture2D sheet = _characterRepository.GetSModelSheet(characterId);
        if (sheet == null)
        {
            actor.Visible = false;
            GD.PushWarning($"Missing s-model sheet for '{characterId}' in scene2.");
            return;
        }

        actor.Texture = sheet;
        actor.Position = marker.Position;
        actor.Scale = Vector2.One * Mathf.Max(0.01f, scale);
        actor.Visible = true;
        actor.PlayIdleFacing(SModelActor.NormalizeFacing(facing));
    }

    public override void _ExitTree()
    {
        GameUi.Instance?.HideStoryHeader();
    }
}
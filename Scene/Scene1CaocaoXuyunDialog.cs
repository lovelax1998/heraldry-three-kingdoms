using Godot;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public partial class Scene1CaocaoXuyunDialog : Node2D, IDialogueActionRunner
{
    [Export] public string SceneScriptPath { get; set; } = "res://Assets/dialogue/scripts/scene1_caocao_xuyun_dialog.json";
    [ExportGroup("Action Coordinates")]
    [Export] public Vector2 ActionReferenceSize { get; set; } = new Vector2(1280.0f, 720.0f);
    [ExportGroup("Intro Layout")]
    [Export] public Vector2 IntroCaocaoPosition { get; set; } = new Vector2(610.0f, 420.0f);
    [Export] public Vector2 IntroXuyunOffset { get; set; } = new Vector2(52.0f, 62.0f);
    [Export] public Vector2 IntroTeleportEffectOffset { get; set; } = new Vector2(0.0f, -8.0f);
    [Export] public Vector2 IntroGroundShadowOffset { get; set; } = new Vector2(-116.0f, 100.0f);
    [Export] public Vector2 IntroGroundShadowSize { get; set; } = new Vector2(275.0f, 50.0f);
    [Export] public float SceneActorScale { get; set; } = 0.575f;
    [Export] public float TeleportEffectScale { get; set; } = 0.58f;
    [Export] public float IntroPaceDistance { get; set; } = 37.5f;
    [Export] public float IntroPaceDuration { get; set; } = 0.70f;
    [ExportGroup("Dialogue Emotes")]
    [Export] public string DefaultEmoteResourcePath { get; set; } = "res://Assets/Characters/general/question_chroma.png";
    [Export] public Vector2 DefaultEmoteOffset { get; set; } = new Vector2(0.0f, -58.0f);
    [Export] public float DefaultEmoteScale { get; set; } = 0.40f;
    [Export] public int DefaultEmoteStartFrame { get; set; } = 0;
    [Export] public int DefaultEmoteEndFrame { get; set; } = 12;
    [Export] public float DefaultEmoteFramesPerSecond { get; set; } = 24.0f;
    [Export] public int DefaultEmoteSheetColumns { get; set; } = 5;
    [Export] public int DefaultEmoteSheetRows { get; set; } = 5;
    [Export] public int DefaultEmoteSheetOuterPadding { get; set; } = 3;
    [Export] public int DefaultEmoteFrameSpacing { get; set; } = 2;
    [Export] public int DefaultEmoteFrameInset { get; set; } = 2;
    [Export] public int DefaultEmoteZIndex { get; set; } = 24;
    [ExportGroup("Scene Reveal")]
    [Export] public float SceneRevealHoldDuration { get; set; } = 0.16f;
    [Export] public float SceneRevealFadeDuration { get; set; } = 0.90f;
    [Export] public float SceneRevealCameraDuration { get; set; } = 1.05f;
    [Export] public Vector2 SceneRevealCameraStartZoom { get; set; } = new Vector2(0.965f, 0.965f);
    [Export] public Vector2 SceneRevealCameraEndZoom { get; set; } = Vector2.One;
    [Export] public Vector2 SceneRevealCameraStartOffset { get; set; } = new Vector2(0.0f, 10.0f);
    [ExportGroup("Scene Header")]
    [Export] public float SceneHeaderFadeDuration { get; set; } = 0.32f;
    [ExportGroup("Scene Flow")]
    [Export] public string NextScenePath { get; set; } = "res://Scene/scene2_taoyuan.tscn";
    [Export] public string NextSceneTransitionTitle { get; set; } = string.Empty;
    [Export] public string NextSceneTransitionSubtitle { get; set; } = string.Empty;
    [Export] public float NextSceneTransitionDelay { get; set; } = 0.20f;
    [Export] public float NextSceneBgmFadeDuration { get; set; } = 0.42f;
    [ExportGroup("Dialogue Sfx")]
    [Export] public float WalkStepInterval { get; set; } = 0.18f;
    [Export] public float WalkPitchMin { get; set; } = 0.96f;
    [Export] public float WalkPitchMax { get; set; } = 1.04f;
    [Export] public float AttackPitchMin { get; set; } = 0.94f;
    [Export] public float AttackPitchMax { get; set; } = 1.05f;

    private Sprite2D _background;
    private ColorRect _groundShadow;
    private AudioStreamPlayer _bgmPlayer;
    private AudioStreamPlayer _dialogueAppearSfx;
    private AudioStreamPlayer _dialogueWalkSfx;
    private AudioStreamPlayer _dialogueAttackSfx;
    private DialoguePlayer _dialoguePlayer;
    private Texture2D _xuyunTeleportDisappearTexture;
    private Texture2D _defaultEmoteTexture;
    private SModelActor _caocaoActor;
    private SModelActor _xuyunActor;
    private SheetEffectPlayer _xuyunTeleportEffect;
    private SheetEffectPlayer _emoteEffect;
    private Marker2D _introCaocaoMarker;
    private Marker2D _introXuyunMarker;
    private Marker2D _introTeleportEffectMarker;
    private Marker2D _introGroundShadowMarker;
    private ColorRect _sceneRevealOverlay;
    private Camera2D _revealCamera;
    private CharacterRepository _characterRepository;
    private DialogueSceneDefinition _sceneDefinition;
    private readonly Dictionary<string, SModelActor> _sceneActors = new(StringComparer.OrdinalIgnoreCase);
    private bool _sceneTransitionStarted;

    public override async void _Ready()
    {
        CursorManager.ApplyDefaultCursor();

        _background = GetNode<Sprite2D>("Background");
        _groundShadow = GetNodeOrNull<ColorRect>("GroundShadow");
        _bgmPlayer = GetNodeOrNull<AudioStreamPlayer>("BgmPlayer");
        _dialogueAppearSfx = GetNodeOrNull<AudioStreamPlayer>("DialogueAppearSfx");
        _dialogueWalkSfx = GetNodeOrNull<AudioStreamPlayer>("DialogueWalkSfx");
        _dialogueAttackSfx = GetNodeOrNull<AudioStreamPlayer>("DialogueAttackSfx");
        _dialoguePlayer = GetNode<DialoguePlayer>("DialoguePlayer");
        _caocaoActor = GetNode<SModelActor>("CaocaoActor");
        _xuyunActor = GetNode<SModelActor>("XuyunActor");
        _xuyunTeleportEffect = GetNode<SheetEffectPlayer>("XuyunTeleportEffect");
        _emoteEffect = GetNodeOrNull<SheetEffectPlayer>("EmoteEffect") ?? GetNodeOrNull<SheetEffectPlayer>("QuestionEffect");
        _introCaocaoMarker = GetNodeOrNull<Marker2D>("IntroCaocaoMarker");
        _introXuyunMarker = GetNodeOrNull<Marker2D>("IntroXuyunMarker");
        _introTeleportEffectMarker = GetNodeOrNull<Marker2D>("IntroTeleportEffectMarker");
        _introGroundShadowMarker = GetNodeOrNull<Marker2D>("IntroGroundShadowMarker");
        _sceneRevealOverlay = GetNodeOrNull<ColorRect>("SceneRevealLayer/Overlay");
        _revealCamera = GetNodeOrNull<Camera2D>("RevealCamera");

        GameUi.Instance?.HideStoryHeader();

        _characterRepository = GameServices.Instance?.Characters;
        if (_characterRepository == null)
        {
            GD.PushError("CharacterRepository is not available. Check GameServices autoload.");
            return;
        }

        _sceneDefinition = DialogueRepository.LoadScene(SceneScriptPath);
        _xuyunTeleportDisappearTexture = ResourceLoader.Load<Texture2D>("res://Assets/Characters/xuyun/xuyun_teleport_disappear.png");
        _defaultEmoteTexture = ResourceLoader.Load<Texture2D>(DefaultEmoteResourcePath);

        ApplyBackground();
        ConfigureWorldActors();
        ConfigureSceneHeaderUi();
        ConfigureSceneRevealOverlay();
        ConfigureRevealCamera();
        PlayBgmIfNeeded();

        _dialoguePlayer.SetActionRunner(this);
        _dialoguePlayer.DialogueFinished += OnDialogueFinished;

        await PlaySceneRevealAsync();
        await ShowSceneHeaderAsync();
        await RunWorldIntroAsync();
        _dialoguePlayer.StartDialogue(_sceneDefinition, _characterRepository);
    }

    private void ConfigureSceneHeaderUi()
    {
        string chapterText = BuildSceneHeaderChapterText();
        string locationText = BuildSceneHeaderLocationText();
        bool hasHeader = !string.IsNullOrWhiteSpace(chapterText) || !string.IsNullOrWhiteSpace(locationText);
        if (!hasHeader)
        {
            GameUi.Instance?.HideStoryHeader();
            return;
        }

        GameUi.Instance?.SetStoryHeaderContext(chapterText, locationText);
    }

    private async Task ShowSceneHeaderAsync()
    {
        string chapterText = BuildSceneHeaderChapterText();
        string locationText = BuildSceneHeaderLocationText();
        bool hasHeader = !string.IsNullOrWhiteSpace(chapterText) || !string.IsNullOrWhiteSpace(locationText);
        if (!hasHeader)
        {
            return;
        }

        GameUi.Instance?.ShowStoryHeader(chapterText, locationText, fadeIn: true);
        if (SceneHeaderFadeDuration > 0.0f)
        {
            await ToSignal(GetTree().CreateTimer(SceneHeaderFadeDuration), SceneTreeTimer.SignalName.Timeout);
        }
    }

    private string BuildSceneHeaderChapterText()
    {
        string chapterNumber = _sceneDefinition?.ChapterNumber?.Trim() ?? string.Empty;
        string chapterTitle = _sceneDefinition?.ChapterTitle?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(chapterNumber))
        {
            return chapterTitle;
        }

        if (string.IsNullOrWhiteSpace(chapterTitle))
        {
            return chapterNumber;
        }

        return $"{chapterNumber} · {chapterTitle}";
    }

    private string BuildSceneHeaderLocationText()
    {
        string location = _sceneDefinition?.Location?.Trim() ?? string.Empty;
        string environment = _sceneDefinition?.Environment?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(location))
        {
            return environment;
        }

        if (string.IsNullOrWhiteSpace(environment))
        {
            return location;
        }

        return $"{location} · {environment}";
    }
    private void ConfigureWorldActors()
    {
        _sceneActors.Clear();

        ConfigureSceneActor(_caocaoActor, "caocao", ResolveIntroCaocaoViewportPosition(), "back", visible: true);
        ConfigureSceneActor(_xuyunActor, "xuyun", ResolveIntroXuyunViewportPosition(), "left", visible: false);

        if (_xuyunTeleportEffect != null)
        {
            _xuyunTeleportEffect.Visible = false;
            _xuyunTeleportEffect.Position = ResolveIntroTeleportEffectViewportPosition();
            _xuyunTeleportEffect.Scale = Vector2.One * Mathf.Max(0.01f, TeleportEffectScale);
            _xuyunTeleportEffect.Modulate = new Color(1, 1, 1, 0.0f);
        }

        if (_emoteEffect != null)
        {
            _emoteEffect.Visible = false;
            _emoteEffect.Position = ResolveIntroCaocaoViewportPosition() + ScaleActionOffset(DefaultEmoteOffset);
            _emoteEffect.Scale = Vector2.One * Mathf.Max(0.01f, DefaultEmoteScale);
            _emoteEffect.Modulate = Colors.White;
            _emoteEffect.ZIndex = DefaultEmoteZIndex;
        }

        if (_groundShadow != null)
        {
            _groundShadow.Position = ResolveIntroGroundShadowViewportPosition();
            _groundShadow.Size = ScaleActionOffset(IntroGroundShadowSize);
            _groundShadow.Visible = false;
        }
    }

    private void ConfigureSceneActor(SModelActor actor, string actorId, Vector2 position, string facing, bool visible)
    {
        if (actor == null)
        {
            return;
        }

        Texture2D actorTexture = _characterRepository?.GetSModelSheet(actorId);
        if (actorTexture != null)
        {
            actor.Texture = actorTexture;
        }

        float actorScale = (_characterRepository?.GetSModelScale(actorId) ?? 1.0f) * SceneActorScale;
        actor.Position = position;
        actor.Scale = Vector2.One * Mathf.Max(0.01f, actorScale);
        actor.Visible = visible;
        actor.Modulate = visible ? Colors.White : new Color(1, 1, 1, 0.0f);
        actor.PlayIdleFacing(SModelActor.NormalizeFacing(facing));

        RegisterSceneActor(actorId, actor);
        RegisterSceneActor(actor.Name.ToString(), actor);
    }

    private void RegisterSceneActor(string key, SModelActor actor)
    {
        if (actor == null || string.IsNullOrWhiteSpace(key))
        {
            return;
        }

        _sceneActors[key] = actor;
    }

    private Vector2 ResolveIntroCaocaoViewportPosition()
    {
        return _introCaocaoMarker?.Position ?? ScaleActionOffset(IntroCaocaoPosition);
    }

    private Vector2 ResolveIntroXuyunViewportPosition()
    {
        if (_introXuyunMarker != null)
        {
            return _introXuyunMarker.Position;
        }

        return ResolveIntroCaocaoViewportPosition() + ScaleActionOffset(IntroXuyunOffset);
    }

    private Vector2 ResolveIntroTeleportEffectViewportPosition()
    {
        if (_introTeleportEffectMarker != null)
        {
            return _introTeleportEffectMarker.Position;
        }

        return ResolveIntroXuyunViewportPosition() + ScaleActionOffset(IntroTeleportEffectOffset);
    }

    private Vector2 ResolveIntroGroundShadowViewportPosition()
    {
        if (_introGroundShadowMarker != null)
        {
            return _introGroundShadowMarker.Position;
        }

        return ResolveIntroCaocaoViewportPosition() + ScaleActionOffset(IntroGroundShadowOffset);
    }

    private async Task RunWorldIntroAsync()
    {
        if (_caocaoActor == null)
        {
            return;
        }

        Vector2 centerPosition = ResolveIntroCaocaoViewportPosition();
        _caocaoActor.Position = centerPosition;
        _caocaoActor.Visible = true;
        _caocaoActor.Modulate = Colors.White;
        _caocaoActor.PlayIdleFacing("back");

        float standDuration = Mathf.Max(0.0f, _sceneDefinition?.IntroStandDuration ?? 0.0f);
        if (standDuration > 0.0f)
        {
            await ToSignal(GetTree().CreateTimer(standDuration), SceneTreeTimer.SignalName.Timeout);
        }

        int paceSteps = Math.Max(0, _sceneDefinition?.IntroPaceSteps ?? 0);
        if (paceSteps > 0)
        {
            float paceDistance = ScaleActionCoordinateX(IntroPaceDistance);
            bool moveLeft = true;

            for (int stepIndex = 0; stepIndex < paceSteps; stepIndex++)
            {
                float direction = moveLeft ? -1.0f : 1.0f;
                Vector2 targetPosition = centerPosition + new Vector2(paceDistance * direction, 0.0f);
                string facing = moveLeft ? "left" : "right";
                await MoveActorToViewportPositionAsync(_caocaoActor, targetPosition, IntroPaceDuration, facing);
                moveLeft = !moveLeft;
            }

            if ((_caocaoActor.Position - centerPosition).LengthSquared() > 0.01f)
            {
                await MoveActorToViewportPositionAsync(_caocaoActor, centerPosition, IntroPaceDuration, "back");
            }
        }

        _caocaoActor.PlayIdleFacing("back");

        float pauseAfterPacing = Mathf.Max(0.0f, _sceneDefinition?.IntroPauseAfterPacing ?? 0.0f);
        if (pauseAfterPacing > 0.0f)
        {
            await ToSignal(GetTree().CreateTimer(pauseAfterPacing), SceneTreeTimer.SignalName.Timeout);
        }
    }
    private void ConfigureSceneRevealOverlay()
    {
        if (_sceneRevealOverlay == null)
        {
            return;
        }

        Rect2 viewportRect = GetViewportRect();
        _sceneRevealOverlay.Position = Vector2.Zero;
        _sceneRevealOverlay.Size = viewportRect.Size;
        _sceneRevealOverlay.Visible = true;
        _sceneRevealOverlay.Modulate = Colors.White;
        _sceneRevealOverlay.Color = Colors.Black;
    }

    private void ConfigureRevealCamera()
    {
        if (_revealCamera == null)
        {
            return;
        }

        Vector2 viewportCenter = GetViewportRect().Size / 2.0f;
        _revealCamera.Position = viewportCenter + SceneRevealCameraStartOffset;
        _revealCamera.Zoom = SceneRevealCameraStartZoom;
        _revealCamera.Enabled = true;
        _revealCamera.MakeCurrent();
    }

    private async Task PlaySceneRevealAsync()
    {
        if (_sceneRevealOverlay == null)
        {
            return;
        }

        ConfigureSceneRevealOverlay();

        if (SceneRevealHoldDuration > 0.0f)
        {
            await ToSignal(GetTree().CreateTimer(SceneRevealHoldDuration), SceneTreeTimer.SignalName.Timeout);
        }

        Vector2 viewportCenter = GetViewportRect().Size / 2.0f;

        Tween revealTween = CreateTween();
        revealTween.SetParallel();
        revealTween.TweenProperty(_sceneRevealOverlay, "modulate:a", 0.0f, SceneRevealFadeDuration)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Sine);

        if (_revealCamera != null)
        {
            revealTween.TweenProperty(_revealCamera, "zoom", SceneRevealCameraEndZoom, SceneRevealCameraDuration)
                .SetEase(Tween.EaseType.Out)
                .SetTrans(Tween.TransitionType.Cubic);
            revealTween.TweenProperty(_revealCamera, "position", viewportCenter, SceneRevealCameraDuration)
                .SetEase(Tween.EaseType.Out)
                .SetTrans(Tween.TransitionType.Cubic);
        }

        await ToSignal(revealTween, Tween.SignalName.Finished);
        _sceneRevealOverlay.Visible = false;
    }

    private void ApplyBackground()
    {
        if (_background == null || _sceneDefinition == null || string.IsNullOrWhiteSpace(_sceneDefinition.Background))
        {
            return;
        }

        Texture2D texture = ResourceLoader.Load<Texture2D>(_sceneDefinition.Background);
        if (texture == null)
        {
            GD.PushWarning($"Failed to load scene background: {_sceneDefinition.Background}");
            return;
        }

        Vector2 viewportSize = GetViewportRect().Size;
        Vector2 textureSize = texture.GetSize();
        _background.Texture = texture;
        _background.Centered = true;
        _background.Position = viewportSize / 2.0f;
        _background.Scale = new Vector2(viewportSize.X / textureSize.X, viewportSize.Y / textureSize.Y);
    }

    public Task RunLineEnterActionsAsync(DialogueLineDefinition line)
    {
        return RunActorActionsAsync(line?.EnterActions);
    }

    public Task RunLineExitActionsAsync(DialogueLineDefinition line)
    {
        return RunActorActionsAsync(line?.ExitActions);
    }

    private async Task RunActorActionsAsync(IReadOnlyList<DialogueActorActionDefinition> actions)
    {
        if (actions == null || actions.Count == 0)
        {
            return;
        }

        foreach (DialogueActorActionDefinition action in actions)
        {
            if (action == null || string.IsNullOrWhiteSpace(action.Type))
            {
                continue;
            }

            await RunActorActionAsync(action);
        }
    }

    private async Task RunActorActionAsync(DialogueActorActionDefinition action)
    {
        SModelActor actor = ResolveSceneActor(action.Target);
        if (actor == null)
        {
            GD.PushWarning($"Dialogue action target '{action.Target}' was not found in {Name}.");
            return;
        }

        string actionType = action.Type.Trim().ToLowerInvariant();
        switch (actionType)
        {
            case "face":
                ApplyActorFacing(actor, action.Facing);
                if (action.Duration > 0.0f)
                {
                    await ToSignal(GetTree().CreateTimer(action.Duration), SceneTreeTimer.SignalName.Timeout);
                }
                break;
            case "move":
                await MoveActorAsync(actor, action);
                break;
            case "attack":
                await PlayAttackActionAsync(actor, action);
                break;
            case "wait":
                if (action.Duration > 0.0f)
                {
                    await ToSignal(GetTree().CreateTimer(action.Duration), SceneTreeTimer.SignalName.Timeout);
                }
                break;
            case "disappear":
                await PlayDisappearActionAsync(actor, action);
                break;
            case "appear":
            case "teleport_appear":
                await PlayAppearActionAsync(actor, action);
                break;
            case "question":
            case "emote":
                await PlayEmoteEffectAsync(actor, action);
                break;
            default:
                GD.PushWarning($"Unsupported dialogue action type '{action.Type}' on {Name}.");
                break;
        }
    }

    private SModelActor ResolveSceneActor(string target)
    {
        if (string.IsNullOrWhiteSpace(target))
        {
            return null;
        }

        if (_sceneActors.TryGetValue(target, out SModelActor actor))
        {
            return actor;
        }

        return GetNodeOrNull<SModelActor>(target);
    }

    private void ApplyActorFacing(SModelActor actor, string facing)
    {
        if (actor == null)
        {
            return;
        }

        actor.PlayIdleFacing(SModelActor.NormalizeFacing(facing));
    }

    private Vector2 GetActionCoordinateScale()
    {
        Vector2 viewportSize = GetViewportRect().Size;
        if (viewportSize.X <= 0.0f || viewportSize.Y <= 0.0f)
        {
            return Vector2.One;
        }

        Vector2 referenceSize = new Vector2(
            Mathf.Max(1.0f, ActionReferenceSize.X),
            Mathf.Max(1.0f, ActionReferenceSize.Y));

        return new Vector2(
            viewportSize.X / referenceSize.X,
            viewportSize.Y / referenceSize.Y);
    }

    private Vector2 ScaleActionOffset(Vector2 offset)
    {
        Vector2 scale = GetActionCoordinateScale();
        return new Vector2(offset.X * scale.X, offset.Y * scale.Y);
    }

    private float ScaleActionCoordinateX(float value)
    {
        return value * GetActionCoordinateScale().X;
    }

    private float ScaleActionCoordinateY(float value)
    {
        return value * GetActionCoordinateScale().Y;
    }

    private Vector2 ResolveActorTargetPosition(SModelActor actor, DialogueActorActionDefinition action)
    {
        Vector2 currentPosition = actor?.Position ?? Vector2.Zero;
        float targetX = action != null && action.X.HasValue
            ? ScaleActionCoordinateX(action.X.Value)
            : currentPosition.X;
        float targetY = action != null && action.Y.HasValue
            ? ScaleActionCoordinateY(action.Y.Value)
            : currentPosition.Y;
        Vector2 offset = ScaleActionOffset(new Vector2(action?.OffsetX ?? 0.0f, action?.OffsetY ?? 0.0f));
        return new Vector2(targetX, targetY) + offset;
    }

    private async Task MoveActorToViewportPositionAsync(SModelActor actor, Vector2 targetPosition, float duration, string facing)
    {
        if (actor == null)
        {
            return;
        }

        Vector2 startPosition = actor.Position;
        Vector2 delta = targetPosition - startPosition;
        if (delta.LengthSquared() <= 0.01f)
        {
            if (!string.IsNullOrWhiteSpace(facing))
            {
                ApplyActorFacing(actor, facing);
            }

            return;
        }

        string walkFacing = ResolveMoveFacing(delta);
        string endFacing = string.IsNullOrWhiteSpace(facing)
            ? walkFacing
            : SModelActor.NormalizeFacing(facing);
        float moveDuration = Mathf.Max(0.01f, duration);

        actor.PlayWalkFacing(walkFacing);
        Task walkSfxTask = PlayWalkStepsAsync(moveDuration);

        Tween moveTween = CreateTween();
        moveTween.TweenProperty(actor, "position", targetPosition, moveDuration)
            .SetEase(Tween.EaseType.InOut)
            .SetTrans(Tween.TransitionType.Sine);
        await ToSignal(moveTween, Tween.SignalName.Finished);
        await walkSfxTask;

        actor.PlayIdleFacing(endFacing);
    }

    private async Task MoveActorAsync(SModelActor actor, DialogueActorActionDefinition action)
    {
        if (actor == null)
        {
            return;
        }

        Vector2 startPosition = actor.Position;
        Vector2 targetPosition = ResolveActorTargetPosition(actor, action);
        Vector2 delta = targetPosition - startPosition;

        if (delta.LengthSquared() <= 0.01f)
        {
            if (!string.IsNullOrWhiteSpace(action.Facing))
            {
                ApplyActorFacing(actor, action.Facing);
            }
            return;
        }

        string walkFacing = ResolveMoveFacing(delta);
        string endFacing = string.IsNullOrWhiteSpace(action.Facing)
            ? walkFacing
            : SModelActor.NormalizeFacing(action.Facing);

        float moveDuration = Mathf.Max(0.01f, action.Duration);
        actor.PlayWalkFacing(walkFacing);

        Task walkSfxTask = PlayWalkStepsAsync(moveDuration);

        Tween moveTween = CreateTween();
        moveTween.TweenProperty(actor, "position", targetPosition, moveDuration)
            .SetEase(Tween.EaseType.InOut)
            .SetTrans(Tween.TransitionType.Sine);
        await ToSignal(moveTween, Tween.SignalName.Finished);
        await walkSfxTask;

        actor.PlayIdleFacing(endFacing);
    }

    private async Task PlayAppearActionAsync(SModelActor actor, DialogueActorActionDefinition action)
    {
        if (actor == null || _xuyunTeleportEffect == null)
        {
            return;
        }

        Vector2 actorPosition = ResolveActorTargetPosition(actor, action);
        Texture2D effectTexture = LoadAppearTexture(action, actor);

        actor.Position = actorPosition;
        actor.Visible = false;
        actor.Modulate = new Color(1, 1, 1, 0.0f);

        if (!string.IsNullOrWhiteSpace(action?.Facing))
        {
            ApplyActorFacing(actor, action.Facing);
        }

        if (effectTexture == null)
        {
            actor.Visible = true;
            actor.Modulate = Colors.White;
            return;
        }

        _xuyunTeleportEffect.Texture = effectTexture;
        _xuyunTeleportEffect.Position = actor.Position + ScaleActionOffset(IntroTeleportEffectOffset);
        _xuyunTeleportEffect.Scale = Vector2.One * Mathf.Max(0.01f, action?.Scale ?? TeleportEffectScale);
        _xuyunTeleportEffect.Modulate = new Color(1, 1, 1, 0.0f);
        _xuyunTeleportEffect.Visible = true;

        PlayDialogueAppearSfx();
        _xuyunTeleportEffect.PlayOnce();

        Tween effectTween = CreateTween();
        effectTween.SetParallel(true);
        effectTween.TweenProperty(_xuyunTeleportEffect, "modulate:a", 1.0f, 0.14f)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Sine);
        effectTween.TweenProperty(_xuyunTeleportEffect, "scale", Vector2.One * (Mathf.Max(0.01f, action?.Scale ?? TeleportEffectScale) * 1.05f), 0.24f)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Back);

        await ToSignal(GetTree().CreateTimer(0.66f), SceneTreeTimer.SignalName.Timeout);

        actor.Visible = true;
        Vector2 actorBaseScale = actor.Scale;
        actor.Scale = actorBaseScale * 0.94f;
        Tween actorTween = CreateTween();
        actorTween.SetParallel(true);
        actorTween.TweenProperty(actor, "modulate:a", 1.0f, 0.20f)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Sine);
        actorTween.TweenProperty(actor, "scale", actorBaseScale * 1.03f, 0.14f)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Back);
        actorTween.Chain().TweenProperty(actor, "scale", actorBaseScale, 0.10f)
            .SetEase(Tween.EaseType.InOut)
            .SetTrans(Tween.TransitionType.Sine);

        await ToSignal(_xuyunTeleportEffect, SheetEffectPlayer.SignalName.EffectFinished);

        Tween fadeEffectTween = CreateTween();
        fadeEffectTween.SetParallel(true);
        fadeEffectTween.TweenProperty(_xuyunTeleportEffect, "modulate:a", 0.0f, 0.14f)
            .SetEase(Tween.EaseType.In)
            .SetTrans(Tween.TransitionType.Sine);
        fadeEffectTween.TweenProperty(_xuyunTeleportEffect, "scale", Vector2.One * (Mathf.Max(0.01f, action?.Scale ?? TeleportEffectScale) * 1.12f), 0.14f)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Cubic);
        await ToSignal(fadeEffectTween, Tween.SignalName.Finished);
        _xuyunTeleportEffect.Visible = false;

        if (action != null && action.Duration > 0.0f)
        {
            await ToSignal(GetTree().CreateTimer(action.Duration), SceneTreeTimer.SignalName.Timeout);
        }
    }

    private async Task PlayAttackActionAsync(SModelActor actor, DialogueActorActionDefinition action)
    {
        if (actor == null)
        {
            return;
        }

        string attackFacing = string.IsNullOrWhiteSpace(action?.Facing)
            ? "front"
            : SModelActor.NormalizeFacing(action.Facing);

        float originalAnimationFps = actor.AnimationFps;
        if (action?.FramesPerSecond is float attackFps && attackFps > 0.0f)
        {
            actor.AnimationFps = attackFps;
        }

        PlayAttackSfx();
        actor.PlayAttackFacing(attackFacing);

        float attackDuration = action != null && action.Duration > 0.0f
            ? action.Duration
            : actor.GetDirectionalAnimationDuration("attack", attackFacing);
        attackDuration = Mathf.Max(0.12f, attackDuration);

        await ToSignal(GetTree().CreateTimer(attackDuration), SceneTreeTimer.SignalName.Timeout);

        actor.AnimationFps = originalAnimationFps;
        actor.PlayIdleFacing(attackFacing);
    }
    private async Task PlayDisappearActionAsync(SModelActor actor, DialogueActorActionDefinition action)
    {
        if (actor == null)
        {
            return;
        }

        Texture2D effectTexture = LoadDisappearTexture(action, actor);
        if (effectTexture == null)
        {
            actor.Visible = false;
            actor.Modulate = Colors.White;
            return;
        }

        _xuyunTeleportEffect.Texture = effectTexture;
        _xuyunTeleportEffect.Position = actor.Position + ScaleActionOffset(IntroTeleportEffectOffset);
        _xuyunTeleportEffect.Scale = Vector2.One * TeleportEffectScale;
        _xuyunTeleportEffect.Modulate = Colors.White;
        _xuyunTeleportEffect.Visible = true;

        PlayDialogueAppearSfx();
        _xuyunTeleportEffect.PlayOnce();

        Tween actorFadeTween = CreateTween();
        actorFadeTween.TweenProperty(actor, "modulate:a", 0.0f, 0.10f)
            .SetEase(Tween.EaseType.In)
            .SetTrans(Tween.TransitionType.Sine);
        await ToSignal(actorFadeTween, Tween.SignalName.Finished);
        actor.Visible = false;
        actor.Modulate = Colors.White;

        await ToSignal(_xuyunTeleportEffect, SheetEffectPlayer.SignalName.EffectFinished);

        Tween fadeEffectTween = CreateTween();
        fadeEffectTween.SetParallel(true);
        fadeEffectTween.TweenProperty(_xuyunTeleportEffect, "modulate:a", 0.0f, 0.12f)
            .SetEase(Tween.EaseType.In)
            .SetTrans(Tween.TransitionType.Sine);
        fadeEffectTween.TweenProperty(_xuyunTeleportEffect, "scale", Vector2.One * (TeleportEffectScale * 1.10f), 0.12f)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Cubic);
        await ToSignal(fadeEffectTween, Tween.SignalName.Finished);
        _xuyunTeleportEffect.Visible = false;
    }

    private Texture2D LoadAppearTexture(DialogueActorActionDefinition action, SModelActor actor)
    {
        if (!string.IsNullOrWhiteSpace(action?.Resource))
        {
            Texture2D explicitTexture = ResourceLoader.Load<Texture2D>(action.Resource);
            if (explicitTexture != null)
            {
                return explicitTexture;
            }
        }

        if (ReferenceEquals(actor, _xuyunActor))
        {
            return _xuyunTeleportEffect?.Texture;
        }

        return _xuyunTeleportEffect?.Texture;
    }

    private Texture2D LoadDisappearTexture(DialogueActorActionDefinition action, SModelActor actor)
    {
        if (!string.IsNullOrWhiteSpace(action?.Resource))
        {
            Texture2D explicitTexture = ResourceLoader.Load<Texture2D>(action.Resource);
            if (explicitTexture != null)
            {
                return explicitTexture;
            }
        }

        if (ReferenceEquals(actor, _xuyunActor))
        {
            return _xuyunTeleportDisappearTexture;
        }

        return null;
    }

    private async Task PlayEmoteEffectAsync(SModelActor actor, DialogueActorActionDefinition action)
    {
        if (actor == null || _emoteEffect == null)
        {
            return;
        }

        Texture2D effectTexture = string.IsNullOrWhiteSpace(action?.Resource)
            ? _defaultEmoteTexture
            : ResourceLoader.Load<Texture2D>(action.Resource) ?? _defaultEmoteTexture;
        if (effectTexture == null)
        {
            return;
        }

        ConfigureEmoteEffectPlayer(action, effectTexture);
        _emoteEffect.Position = actor.Position + ResolveEmoteOffset(action);
        _emoteEffect.Modulate = Colors.White;
        _emoteEffect.Visible = true;
        _emoteEffect.PlayOnce();

        if (action != null && action.Duration > 0.0f)
        {
            await ToSignal(GetTree().CreateTimer(action.Duration), SceneTreeTimer.SignalName.Timeout);
        }
    }

    private void ConfigureEmoteEffectPlayer(DialogueActorActionDefinition action, Texture2D effectTexture)
    {
        if (_emoteEffect == null)
        {
            return;
        }

        int startFrame = Mathf.Max(0, action?.StartFrame ?? DefaultEmoteStartFrame);
        int endFrame = Mathf.Max(startFrame, action?.EndFrame ?? DefaultEmoteEndFrame);

        _emoteEffect.Texture = effectTexture;
        _emoteEffect.Scale = Vector2.One * Mathf.Max(0.01f, action?.Scale ?? DefaultEmoteScale);
        _emoteEffect.StartFrame = startFrame;
        _emoteEffect.EndFrame = endFrame;
        _emoteEffect.FramesPerSecond = Mathf.Max(1.0f, action?.FramesPerSecond ?? DefaultEmoteFramesPerSecond);
        _emoteEffect.SheetColumns = Mathf.Max(1, action?.SheetColumns ?? DefaultEmoteSheetColumns);
        _emoteEffect.SheetRows = Mathf.Max(1, action?.SheetRows ?? DefaultEmoteSheetRows);
        _emoteEffect.SheetOuterPadding = Mathf.Max(0, action?.SheetOuterPadding ?? DefaultEmoteSheetOuterPadding);
        _emoteEffect.FrameSpacing = Mathf.Max(0, action?.FrameSpacing ?? DefaultEmoteFrameSpacing);
        _emoteEffect.FrameInset = Mathf.Max(0, action?.FrameInset ?? DefaultEmoteFrameInset);
        _emoteEffect.HideWhenFinished = true;
        _emoteEffect.ZIndex = DefaultEmoteZIndex;
    }

    private Vector2 ResolveEmoteOffset(DialogueActorActionDefinition action)
    {
        float offsetX = action?.OffsetX ?? DefaultEmoteOffset.X;
        float offsetY = action?.OffsetY ?? DefaultEmoteOffset.Y;
        return ScaleActionOffset(new Vector2(offsetX, offsetY));
    }

    private string ResolveMoveFacing(Vector2 delta)
    {
        if (Mathf.Abs(delta.X) >= Mathf.Abs(delta.Y))
        {
            return delta.X < 0.0f ? "left" : "right";
        }

        return delta.Y < 0.0f ? "back" : "front";
    }

    private void PlayAttackSfx()
    {
        if (_dialogueAttackSfx?.Stream == null)
        {
            return;
        }

        float minPitch = Mathf.Min(AttackPitchMin, AttackPitchMax);
        float maxPitch = Mathf.Max(AttackPitchMin, AttackPitchMax);
        _dialogueAttackSfx.PitchScale = (float)GD.RandRange(minPitch, maxPitch);
        if (_dialogueAttackSfx.Playing)
        {
            _dialogueAttackSfx.Stop();
        }

        _dialogueAttackSfx.Play();
    }

    private void PlayDialogueAppearSfx()
    {
        if (_dialogueAppearSfx?.Stream == null)
        {
            return;
        }

        _dialogueAppearSfx.PitchScale = 1.0f;
        _dialogueAppearSfx.Play();
    }

    private async Task PlayWalkStepsAsync(float duration)
    {
        if (_dialogueWalkSfx?.Stream == null || duration <= 0.0f)
        {
            return;
        }

        float elapsed = 0.0f;
        float interval = Mathf.Max(0.08f, WalkStepInterval);
        while (elapsed < duration + 0.001f)
        {
            float minPitch = Mathf.Min(WalkPitchMin, WalkPitchMax);
            float maxPitch = Mathf.Max(WalkPitchMin, WalkPitchMax);
            _dialogueWalkSfx.PitchScale = (float)GD.RandRange(minPitch, maxPitch);
            _dialogueWalkSfx.Play();

            float waitTime = Mathf.Min(interval, duration - elapsed);
            if (waitTime <= 0.0f)
            {
                break;
            }

            await ToSignal(GetTree().CreateTimer(waitTime), SceneTreeTimer.SignalName.Timeout);
            elapsed += interval;
        }
    }

    private async Task FadeOutBgmAsync(float duration)
    {
        if (_bgmPlayer == null || !_bgmPlayer.Playing)
        {
            return;
        }

        float originalVolume = _bgmPlayer.VolumeDb;
        if (duration <= 0.0f)
        {
            _bgmPlayer.Stop();
            _bgmPlayer.VolumeDb = originalVolume;
            return;
        }

        Tween fadeTween = CreateTween();
        fadeTween.TweenProperty(_bgmPlayer, "volume_db", -40.0f, duration)
            .SetEase(Tween.EaseType.In)
            .SetTrans(Tween.TransitionType.Sine);
        await ToSignal(fadeTween, Tween.SignalName.Finished);

        _bgmPlayer.Stop();
        _bgmPlayer.VolumeDb = originalVolume;
    }
    private void PlayBgmIfNeeded()
    {
        if (_bgmPlayer == null || _sceneDefinition == null || string.IsNullOrWhiteSpace(_sceneDefinition.Bgm))
        {
            return;
        }

        AudioStream stream = ResourceLoader.Load<AudioStream>(_sceneDefinition.Bgm);
        if (stream == null)
        {
            GD.PushWarning($"Failed to load scene BGM: {_sceneDefinition.Bgm}");
            return;
        }

        _bgmPlayer.Stream = stream;
        _bgmPlayer.Play();
    }

    private async void OnDialogueFinished()
    {
        if (_sceneTransitionStarted)
        {
            return;
        }

        _sceneTransitionStarted = true;

        if (NextSceneTransitionDelay > 0.0f)
        {
            await ToSignal(GetTree().CreateTimer(NextSceneTransitionDelay), SceneTreeTimer.SignalName.Timeout);
        }

        Task bgmFadeTask = FadeOutBgmAsync(NextSceneBgmFadeDuration);

        if (string.IsNullOrWhiteSpace(NextScenePath))
        {
            await bgmFadeTask;
            GD.Print("Scene1 dialogue finished.");
            return;
        }

        Error changeSceneResult;
        if (GameUi.Instance != null)
        {
            changeSceneResult = await GameUi.Instance.TransitionToSceneAsync(
                NextScenePath,
                NextSceneTransitionTitle,
                NextSceneTransitionSubtitle);
        }
        else
        {
            changeSceneResult = GetTree().ChangeSceneToFile(NextScenePath);
        }

        await bgmFadeTask;

        if (changeSceneResult != Error.Ok)
        {
            GD.PushWarning($"Failed to change scene to next story scene: {NextScenePath} ({changeSceneResult})");
            _sceneTransitionStarted = false;
        }
    }

    public override void _ExitTree()
    {
        GameUi.Instance?.HideStoryHeader();
    }
}




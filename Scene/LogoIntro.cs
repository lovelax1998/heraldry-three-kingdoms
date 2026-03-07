using Godot;

public partial class LogoIntro : Node2D
{
    private VideoStreamPlayer _introVideo;
    private AudioStreamPlayer _loopMusic;
    private AnimatedSprite2D _maocaoLogo;
    private Sprite2D _titleLogo;
    private AudioStreamPlayer _titleSfxPlayer;
    private AudioStreamPlayer _startGameHoverSfxPlayer;
    private AudioStreamPlayer _startGameClickSfxPlayer;
    private Area2D _startGameButton;
    private CanvasItem _startGameGlow;
    private Sprite2D _startGameLabel;
    private readonly RandomNumberGenerator _rng = new();

    private bool _introFinished;
    private bool _startGameHovered;
    private Vector2 _maocaoRestScale = Vector2.One;
    private Vector2 _titleLandingPosition;
    private Vector2 _titleRestScale = Vector2.One;
    private float _titleTextureWidth;
    private float _titleTextureHeight;
    private Vector2 _startGameRestPosition;
    private Vector2 _startGameLabelRestScale = Vector2.One;
    private Tween _startGameIdleTween;
    private Tween _startGameHoverTween;
    private static readonly Color StartGameBaseColor = new Color(1.0f, 1.0f, 1.0f, 1.0f);
    private static readonly Color StartGameIdleColor = new Color(1.0f, 0.97f, 0.94f, 1.0f);
    private static readonly Color StartGameHoverColor = new Color(1.0f, 0.92f, 0.98f, 1.0f);

    [Export] public float FadeInDuration { get; set; } = 1.5f;
    [Export] public AudioStream TitleImpactSfx { get; set; }
    [Export] public float TitleDropDuration { get; set; } = 0.68f;
    [Export] public int TitleBurstParticles { get; set; } = 18;
    [Export] public float StartGameEntranceDelay { get; set; } = 0.12f;
    [Export] public float StartGameHoverScale { get; set; } = 1.08f;

    public override void _Ready()
    {
        _rng.Randomize();

        _introVideo = GetNode<VideoStreamPlayer>("IntroVideo");
        _loopMusic = GetNode<AudioStreamPlayer>("LoopMusic");
        _maocaoLogo = GetNode<AnimatedSprite2D>("Maocao");
        _titleLogo = GetNodeOrNull<Sprite2D>("GameTitle");
        _titleSfxPlayer = GetNodeOrNull<AudioStreamPlayer>("TitleSfx");
        _startGameHoverSfxPlayer = GetNodeOrNull<AudioStreamPlayer>("StartGameHoverSfx");
        _startGameClickSfxPlayer = GetNodeOrNull<AudioStreamPlayer>("StartGameClickSfx");
        _startGameButton = GetNodeOrNull<Area2D>("StartGameButton");
        _startGameGlow = GetNodeOrNull<CanvasItem>("StartGameButton/Glow");
        _startGameLabel = GetNodeOrNull<Sprite2D>("StartGameButton/Label");

        _maocaoRestScale = _maocaoLogo.Scale;

        if (_titleLogo == null)
        {
            GD.PushWarning("Scene03 is missing a 'GameTitle' Sprite2D node.");
        }

        if (_titleSfxPlayer == null)
        {
            _titleSfxPlayer = new AudioStreamPlayer();
            _titleSfxPlayer.Name = "TitleSfxRuntime";
            AddChild(_titleSfxPlayer);
        }

        if (_startGameButton == null || _startGameLabel == null)
        {
            GD.PushWarning("Scene03 is missing the 'StartGameButton' structure.");
        }
        else
        {
            _startGameButton.InputPickable = true;
        }

        _titleSfxPlayer.Stream = TitleImpactSfx;

        CursorManager.ApplyDefaultCursor();
        InitializeLogo();
        InitializeTitle();
        InitializeStartGame();

        _introVideo.Finished += OnIntroVideoFinished;

        if (!_introVideo.IsPlaying())
        {
            _introVideo.Play();
        }
    }

    public override void _Process(double delta)
    {
        if (_startGameButton == null || _startGameLabel == null || !_startGameButton.Visible)
        {
            if (_startGameHovered)
            {
                SetStartGameHoverState(false);
            }

            return;
        }

        bool isHovered = IsMouseOverStartGame();
        if (isHovered != _startGameHovered)
        {
            SetStartGameHoverState(isHovered);
        }
    }

    private void InitializeLogo()
    {
        _maocaoLogo.Visible = false;
        _maocaoLogo.Modulate = new Color(1, 1, 1, 0);
        _maocaoLogo.Scale = _maocaoRestScale * 0.82f;
        _maocaoLogo.Rotation = 0.1f;
    }

    private void InitializeTitle()
    {
        if (_titleLogo == null || _titleLogo.Texture == null)
        {
            return;
        }

        _titleLandingPosition = _titleLogo.Position;
        _titleRestScale = _titleLogo.Scale == Vector2.Zero ? Vector2.One : _titleLogo.Scale;

        Vector2 textureSize = _titleLogo.Texture.GetSize();
        _titleTextureWidth = textureSize.X * _titleRestScale.X;
        _titleTextureHeight = textureSize.Y * _titleRestScale.Y;

        _titleLogo.Visible = false;
        _titleLogo.Modulate = new Color(1, 1, 1, 0);
        _titleLogo.Position = new Vector2(
            _titleLandingPosition.X,
            -(_titleTextureHeight * 0.55f) - 120.0f
        );
        _titleLogo.Scale = new Vector2(_titleRestScale.X * 0.86f, _titleRestScale.Y * 1.22f);
        _titleLogo.Rotation = -0.08f;
    }

    private void InitializeStartGame()
    {
        if (_startGameButton == null || _startGameLabel == null)
        {
            return;
        }

        _startGameRestPosition = _startGameButton.Position;
        _startGameLabelRestScale = _startGameLabel.Scale == Vector2.Zero ? Vector2.One : _startGameLabel.Scale;

        _startGameButton.Visible = false;
        _startGameButton.Position = _startGameRestPosition + new Vector2(0, 54.0f);

        _startGameLabel.Modulate = new Color(StartGameBaseColor.R, StartGameBaseColor.G, StartGameBaseColor.B, 0);
        _startGameLabel.Scale = _startGameLabelRestScale * 0.78f;
        _startGameLabel.Rotation = -0.025f;

        if (_startGameGlow != null)
        {
            _startGameGlow.Visible = false;
        }
    }

    private void OnIntroVideoFinished()
    {
        if (_introFinished)
        {
            return;
        }

        _introFinished = true;

        _loopMusic.Play();
        ShowMaocaoLogoWithEffects();
        ShowTitleWithEffects();
        ShowStartGameWithEffects();
    }

    private void ShowMaocaoLogoWithEffects()
    {
        _maocaoLogo.Visible = true;

        Tween tween = CreateTween();
        tween.SetParallel();

        tween.TweenProperty(_maocaoLogo, "modulate:a", 1.0f, FadeInDuration)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Sine);

        tween.TweenProperty(_maocaoLogo, "rotation", 0.0f, FadeInDuration)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Back);

        tween.TweenProperty(_maocaoLogo, "scale", _maocaoRestScale, FadeInDuration)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Back);

        tween.Finished += OnTweenFinished;
    }

    private void ShowTitleWithEffects()
    {
        if (_titleLogo == null)
        {
            return;
        }

        _titleLogo.Visible = true;

        Vector2 impactPosition = _titleLandingPosition + new Vector2(0, 26.0f);
        Vector2 impactScale = new Vector2(_titleRestScale.X * 1.08f, _titleRestScale.Y * 0.68f);
        Vector2 reboundScale = new Vector2(_titleRestScale.X * 0.95f, _titleRestScale.Y * 1.05f);

        Tween tween = CreateTween();
        tween.TweenProperty(_titleLogo, "modulate:a", 1.0f, 0.18f)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Sine);

        tween.Parallel().TweenProperty(_titleLogo, "position", impactPosition, TitleDropDuration)
            .SetEase(Tween.EaseType.In)
            .SetTrans(Tween.TransitionType.Cubic);

        tween.Parallel().TweenProperty(_titleLogo, "scale", impactScale, TitleDropDuration)
            .SetEase(Tween.EaseType.In)
            .SetTrans(Tween.TransitionType.Cubic);

        tween.Parallel().TweenProperty(_titleLogo, "rotation", 0.09f, TitleDropDuration)
            .SetEase(Tween.EaseType.In)
            .SetTrans(Tween.TransitionType.Sine);

        tween.TweenCallback(Callable.From(TriggerTitleImpact));

        tween.TweenProperty(_titleLogo, "position", _titleLandingPosition, 0.22f)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Elastic);

        tween.Parallel().TweenProperty(_titleLogo, "scale", reboundScale, 0.14f)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Sine);

        tween.Parallel().TweenProperty(_titleLogo, "rotation", -0.03f, 0.14f)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Sine);

        tween.TweenProperty(_titleLogo, "scale", _titleRestScale, 0.16f)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Back);

        tween.Parallel().TweenProperty(_titleLogo, "rotation", 0.0f, 0.16f)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Back);

        tween.Finished += OnTitleEntranceFinished;
    }

    private void ShowStartGameWithEffects()
    {
        if (_startGameButton == null || _startGameLabel == null)
        {
            return;
        }

        _startGameButton.Visible = true;

        Tween tween = CreateTween();
        if (StartGameEntranceDelay > 0.0f)
        {
            tween.TweenInterval(StartGameEntranceDelay);
        }

        tween.TweenProperty(_startGameButton, "position", _startGameRestPosition + new Vector2(0, -10.0f), 0.40f)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Back);

        tween.Parallel().TweenProperty(_startGameLabel, "modulate:a", 1.0f, 0.22f)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Sine);

        tween.Parallel().TweenProperty(_startGameLabel, "scale", _startGameLabelRestScale * 1.06f, 0.40f)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Back);

        tween.Parallel().TweenProperty(_startGameLabel, "rotation", 0.018f, 0.40f)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Sine);

        tween.Parallel().TweenProperty(_startGameLabel, "modulate", StartGameIdleColor, 0.40f)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Sine);

        tween.TweenProperty(_startGameButton, "position", _startGameRestPosition, 0.18f)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Sine);

        tween.Parallel().TweenProperty(_startGameLabel, "scale", _startGameLabelRestScale, 0.18f)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Back);

        tween.Parallel().TweenProperty(_startGameLabel, "rotation", 0.0f, 0.18f)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Back);

        tween.Parallel().TweenProperty(_startGameLabel, "modulate", StartGameIdleColor, 0.18f)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Sine);

        tween.Finished += OnStartGameEntranceFinished;
    }

    private void TriggerTitleImpact()
    {
        PlayTitleImpactSfx();
        SpawnTitleBurst();
    }

    private void PlayTitleImpactSfx()
    {
        if (_titleSfxPlayer == null)
        {
            return;
        }

        if (TitleImpactSfx != null)
        {
            _titleSfxPlayer.Stream = TitleImpactSfx;
        }

        if (_titleSfxPlayer.Stream != null)
        {
            _titleSfxPlayer.Play();
        }
    }

    private void SpawnTitleBurst()
    {
        if (_titleLogo == null || _titleLogo.Texture == null)
        {
            return;
        }

        float spreadX = _titleTextureWidth * 0.23f;
        float originY = _titleLandingPosition.Y + (_titleTextureHeight * 0.17f);

        for (int index = 0; index < TitleBurstParticles; index++)
        {
            Polygon2D spark = new Polygon2D();
            spark.Polygon = new[]
            {
                new Vector2(0, -10),
                new Vector2(6, 0),
                new Vector2(0, 10),
                new Vector2(-6, 0)
            };
            spark.Color = GetBurstColor(index);
            spark.Position = new Vector2(
                _titleLandingPosition.X + _rng.RandfRange(-spreadX, spreadX),
                originY + _rng.RandfRange(-16.0f, 26.0f)
            );
            spark.Scale = Vector2.One * _rng.RandfRange(0.45f, 1.15f);
            spark.Rotation = _rng.RandfRange(-Mathf.Pi, Mathf.Pi);
            spark.ZIndex = 18;
            AddChild(spark);

            Vector2 targetPosition = spark.Position + new Vector2(
                _rng.RandfRange(-150.0f, 150.0f),
                _rng.RandfRange(-110.0f, 35.0f)
            );
            float duration = _rng.RandfRange(0.35f, 0.7f);

            Tween sparkTween = CreateTween();
            sparkTween.TweenProperty(spark, "position", targetPosition, duration)
                .SetEase(Tween.EaseType.Out)
                .SetTrans(Tween.TransitionType.Quart);
            sparkTween.Parallel().TweenProperty(spark, "scale", Vector2.Zero, duration)
                .SetEase(Tween.EaseType.In)
                .SetTrans(Tween.TransitionType.Sine);
            sparkTween.Parallel().TweenProperty(spark, "modulate:a", 0.0f, duration)
                .SetEase(Tween.EaseType.In)
                .SetTrans(Tween.TransitionType.Sine);
            sparkTween.Parallel().TweenProperty(spark, "rotation", spark.Rotation + _rng.RandfRange(-2.0f, 2.0f), duration)
                .SetEase(Tween.EaseType.Out)
                .SetTrans(Tween.TransitionType.Sine);
            sparkTween.TweenCallback(Callable.From(() => spark.QueueFree()));
        }
    }

    private Color GetBurstColor(int index)
    {
        return (index % 4) switch
        {
            0 => new Color(1.0f, 0.94f, 0.70f, 1.0f),
            1 => new Color(1.0f, 0.78f, 0.43f, 1.0f),
            2 => new Color(1.0f, 0.50f, 0.47f, 1.0f),
            _ => new Color(0.65f, 0.92f, 1.0f, 1.0f)
        };
    }

    private void OnTweenFinished()
    {
        PlayLogoAnimation();
    }

    private void OnTitleEntranceFinished()
    {
        if (_titleLogo == null)
        {
            return;
        }

        Tween idleTween = CreateTween();
        idleTween.SetLoops();
        idleTween.TweenProperty(_titleLogo, "position:y", _titleLandingPosition.Y - 6.0f, 1.6f)
            .SetEase(Tween.EaseType.InOut)
            .SetTrans(Tween.TransitionType.Sine);
        idleTween.TweenProperty(_titleLogo, "position:y", _titleLandingPosition.Y + 4.0f, 1.8f)
            .SetEase(Tween.EaseType.InOut)
            .SetTrans(Tween.TransitionType.Sine);
    }

    private void OnStartGameEntranceFinished()
    {
        StartStartGameIdleTween();
    }

    private bool IsMouseOverStartGame()
    {
        if (_startGameButton == null || _startGameLabel == null || _startGameLabel.Texture == null)
        {
            return false;
        }

        Vector2 localMouse = _startGameButton.ToLocal(GetGlobalMousePosition()) - _startGameLabel.Position;
        Vector2 textureSize = _startGameLabel.Texture.GetSize();
        Vector2 halfSize = new Vector2(
            textureSize.X * _startGameLabel.Scale.X * 0.5f,
            textureSize.Y * _startGameLabel.Scale.Y * 0.5f
        );

        Rect2 rect = new Rect2(-halfSize, halfSize * 2.0f);
        return rect.HasPoint(localMouse);
    }

    private void SetStartGameHoverState(bool hovered)
    {
        _startGameHovered = hovered;

        if (hovered)
        {
            OnStartGameMouseEntered();
            return;
        }

        OnStartGameMouseExited();
    }

    private void OnStartGameMouseEntered()
    {
        if (_startGameButton == null || !_startGameButton.Visible || _startGameLabel == null)
        {
            return;
        }

        KillTween(ref _startGameIdleTween);
        KillTween(ref _startGameHoverTween);
        PlayStartGameHoverSfx();
        SpawnStartGameHoverBurst();

        _startGameHoverTween = CreateTween();
        _startGameHoverTween.TweenProperty(_startGameButton, "position", _startGameRestPosition + new Vector2(0, -18.0f), 0.12f)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Back);
        _startGameHoverTween.Parallel().TweenProperty(_startGameLabel, "scale", _startGameLabelRestScale * (StartGameHoverScale + 0.05f), 0.12f)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Back);
        _startGameHoverTween.Parallel().TweenProperty(_startGameLabel, "rotation", -0.02f, 0.12f)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Sine);
        _startGameHoverTween.Parallel().TweenProperty(_startGameLabel, "modulate", StartGameHoverColor, 0.12f)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Sine);
        _startGameHoverTween.TweenProperty(_startGameButton, "position", _startGameRestPosition + new Vector2(0, -12.0f), 0.10f)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Sine);
        _startGameHoverTween.Parallel().TweenProperty(_startGameLabel, "scale", _startGameLabelRestScale * StartGameHoverScale, 0.10f)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Back);
        _startGameHoverTween.Parallel().TweenProperty(_startGameLabel, "rotation", 0.012f, 0.10f)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Sine);
        _startGameHoverTween.Finished += StartStartGameHoverLoop;
    }

    private void OnStartGameMouseExited()
    {
        if (_startGameButton == null || _startGameLabel == null)
        {
            return;
        }

        KillTween(ref _startGameHoverTween);

        _startGameHoverTween = CreateTween();
        _startGameHoverTween.TweenProperty(_startGameButton, "position", _startGameRestPosition, 0.16f)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Back);
        _startGameHoverTween.Parallel().TweenProperty(_startGameLabel, "scale", _startGameLabelRestScale, 0.16f)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Back);
        _startGameHoverTween.Parallel().TweenProperty(_startGameLabel, "rotation", 0.0f, 0.16f)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Sine);
        _startGameHoverTween.Parallel().TweenProperty(_startGameLabel, "modulate", StartGameIdleColor, 0.16f)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Sine);
        _startGameHoverTween.Finished += OnStartGameHoverResetFinished;
    }

    private void OnStartGameHoverResetFinished()
    {
        if (!_startGameHovered)
        {
            StartStartGameIdleTween();
        }
    }

    private void StartStartGameIdleTween()
    {
        if (_startGameHovered || _startGameButton == null || _startGameLabel == null)
        {
            return;
        }

        KillTween(ref _startGameIdleTween);

        _startGameIdleTween = CreateTween();
        _startGameIdleTween.SetLoops();
        _startGameIdleTween.TweenProperty(_startGameButton, "position", _startGameRestPosition + new Vector2(-1.5f, -5.0f), 0.85f)
            .SetEase(Tween.EaseType.InOut)
            .SetTrans(Tween.TransitionType.Sine);
        _startGameIdleTween.Parallel().TweenProperty(_startGameLabel, "scale", _startGameLabelRestScale * 1.025f, 0.85f)
            .SetEase(Tween.EaseType.InOut)
            .SetTrans(Tween.TransitionType.Sine);
        _startGameIdleTween.Parallel().TweenProperty(_startGameLabel, "rotation", -0.009f, 0.85f)
            .SetEase(Tween.EaseType.InOut)
            .SetTrans(Tween.TransitionType.Sine);
        _startGameIdleTween.Parallel().TweenProperty(_startGameLabel, "modulate", StartGameIdleColor, 0.85f)
            .SetEase(Tween.EaseType.InOut)
            .SetTrans(Tween.TransitionType.Sine);

        _startGameIdleTween.TweenProperty(_startGameButton, "position", _startGameRestPosition + new Vector2(1.5f, 2.0f), 0.95f)
            .SetEase(Tween.EaseType.InOut)
            .SetTrans(Tween.TransitionType.Sine);
        _startGameIdleTween.Parallel().TweenProperty(_startGameLabel, "scale", _startGameLabelRestScale, 0.95f)
            .SetEase(Tween.EaseType.InOut)
            .SetTrans(Tween.TransitionType.Sine);
        _startGameIdleTween.Parallel().TweenProperty(_startGameLabel, "rotation", 0.008f, 0.95f)
            .SetEase(Tween.EaseType.InOut)
            .SetTrans(Tween.TransitionType.Sine);
        _startGameIdleTween.Parallel().TweenProperty(_startGameLabel, "modulate", StartGameBaseColor, 0.95f)
            .SetEase(Tween.EaseType.InOut)
            .SetTrans(Tween.TransitionType.Sine);
    }

    private void StartStartGameHoverLoop()
    {
        if (!_startGameHovered || _startGameButton == null || _startGameLabel == null)
        {
            return;
        }

        KillTween(ref _startGameIdleTween);

        _startGameIdleTween = CreateTween();
        _startGameIdleTween.SetLoops();
        _startGameIdleTween.TweenProperty(_startGameButton, "position", _startGameRestPosition + new Vector2(-3.0f, -16.0f), 0.18f)
            .SetEase(Tween.EaseType.InOut)
            .SetTrans(Tween.TransitionType.Sine);
        _startGameIdleTween.Parallel().TweenProperty(_startGameLabel, "scale", _startGameLabelRestScale * (StartGameHoverScale + 0.04f), 0.18f)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Back);
        _startGameIdleTween.Parallel().TweenProperty(_startGameLabel, "rotation", -0.02f, 0.18f)
            .SetEase(Tween.EaseType.InOut)
            .SetTrans(Tween.TransitionType.Sine);
        _startGameIdleTween.Parallel().TweenProperty(_startGameLabel, "modulate", StartGameHoverColor, 0.18f)
            .SetEase(Tween.EaseType.InOut)
            .SetTrans(Tween.TransitionType.Sine);

        _startGameIdleTween.TweenProperty(_startGameButton, "position", _startGameRestPosition + new Vector2(3.0f, -11.0f), 0.17f)
            .SetEase(Tween.EaseType.InOut)
            .SetTrans(Tween.TransitionType.Sine);
        _startGameIdleTween.Parallel().TweenProperty(_startGameLabel, "scale", _startGameLabelRestScale * (StartGameHoverScale - 0.01f), 0.17f)
            .SetEase(Tween.EaseType.InOut)
            .SetTrans(Tween.TransitionType.Sine);
        _startGameIdleTween.Parallel().TweenProperty(_startGameLabel, "rotation", 0.018f, 0.17f)
            .SetEase(Tween.EaseType.InOut)
            .SetTrans(Tween.TransitionType.Sine);
        _startGameIdleTween.Parallel().TweenProperty(_startGameLabel, "modulate", StartGameBaseColor, 0.17f)
            .SetEase(Tween.EaseType.InOut)
            .SetTrans(Tween.TransitionType.Sine);

        _startGameIdleTween.TweenProperty(_startGameButton, "position", _startGameRestPosition + new Vector2(0.0f, -15.0f), 0.16f)
            .SetEase(Tween.EaseType.InOut)
            .SetTrans(Tween.TransitionType.Sine);
        _startGameIdleTween.Parallel().TweenProperty(_startGameLabel, "scale", _startGameLabelRestScale * (StartGameHoverScale + 0.02f), 0.16f)
            .SetEase(Tween.EaseType.InOut)
            .SetTrans(Tween.TransitionType.Sine);
        _startGameIdleTween.Parallel().TweenProperty(_startGameLabel, "rotation", 0.0f, 0.16f)
            .SetEase(Tween.EaseType.InOut)
            .SetTrans(Tween.TransitionType.Sine);
        _startGameIdleTween.Parallel().TweenProperty(_startGameLabel, "modulate", new Color(1.0f, 0.98f, 0.92f, 1.0f), 0.16f)
            .SetEase(Tween.EaseType.InOut)
            .SetTrans(Tween.TransitionType.Sine);
    }

    private void SpawnStartGameHoverBurst()
    {
        if (_startGameLabel == null || _startGameLabel.Texture == null)
        {
            return;
        }

        Vector2 textureSize = _startGameLabel.Texture.GetSize();
        float halfWidth = textureSize.X * _startGameLabelRestScale.X * 0.45f;
        float halfHeight = textureSize.Y * _startGameLabelRestScale.Y * 0.45f;

        for (int index = 0; index < 7; index++)
        {
            Polygon2D spark = new Polygon2D();
            spark.Polygon = new[]
            {
                new Vector2(0, -7),
                new Vector2(4, 0),
                new Vector2(0, 7),
                new Vector2(-4, 0)
            };
            spark.Color = index % 2 == 0
                ? new Color(1.0f, 0.95f, 0.75f, 0.95f)
                : new Color(1.0f, 0.78f, 0.93f, 0.95f);
            spark.Position = _startGameRestPosition + new Vector2(
                _rng.RandfRange(-halfWidth, halfWidth),
                _rng.RandfRange(-halfHeight, halfHeight)
            );
            spark.Scale = Vector2.One * _rng.RandfRange(0.55f, 1.0f);
            spark.Rotation = _rng.RandfRange(-Mathf.Pi, Mathf.Pi);
            spark.ZIndex = 26;
            AddChild(spark);

            Tween sparkTween = CreateTween();
            sparkTween.TweenProperty(spark, "position", spark.Position + new Vector2(_rng.RandfRange(-30.0f, 30.0f), _rng.RandfRange(-24.0f, 8.0f)), 0.22f)
                .SetEase(Tween.EaseType.Out)
                .SetTrans(Tween.TransitionType.Quart);
            sparkTween.Parallel().TweenProperty(spark, "scale", Vector2.Zero, 0.22f)
                .SetEase(Tween.EaseType.In)
                .SetTrans(Tween.TransitionType.Sine);
            sparkTween.Parallel().TweenProperty(spark, "modulate:a", 0.0f, 0.22f)
                .SetEase(Tween.EaseType.In)
                .SetTrans(Tween.TransitionType.Sine);
            sparkTween.TweenCallback(Callable.From(() => spark.QueueFree()));
        }
    }

    private void PlayStartGameHoverSfx()
    {
        if (_startGameHoverSfxPlayer?.Stream == null)
        {
            return;
        }

        _startGameHoverSfxPlayer.Stop();
        _startGameHoverSfxPlayer.Play();
    }

    private void PlayStartGameClickSfx()
    {
        if (_startGameClickSfxPlayer?.Stream == null)
        {
            return;
        }

        _startGameClickSfxPlayer.Stop();
        _startGameClickSfxPlayer.Play();
    }

    private void KillTween(ref Tween tween)
    {
        if (tween != null && GodotObject.IsInstanceValid(tween))
        {
            tween.Kill();
        }

        tween = null;
    }

    private void PlayLogoAnimation()
    {
        if (_maocaoLogo.SpriteFrames.HasAnimation("idle"))
        {
            _maocaoLogo.Play("idle");
        }
        else if (_maocaoLogo.SpriteFrames.HasAnimation("loop"))
        {
            _maocaoLogo.Play("loop");
        }
        else
        {
            _maocaoLogo.Play();
        }
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventMouseButton mouseButtonEvent
            && mouseButtonEvent.Pressed
            && mouseButtonEvent.ButtonIndex == MouseButton.Left
            && _startGameHovered
            && _startGameButton != null
            && _startGameButton.Visible)
        {
            PlayStartGameClickSfx();
            GD.Print("Start Game button clicked.");
            return;
        }

        if (@event.IsActionPressed("ui_accept") || @event.IsActionPressed("ui_cancel"))
        {
            if (_introVideo != null && _introVideo.IsPlaying())
            {
                _introVideo.Stop();
                OnIntroVideoFinished();
            }
        }
    }
}

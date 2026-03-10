using Godot;
using System.Threading.Tasks;

public partial class SceneTransitionLayer : CanvasLayer
{
    [Export] public float CoverDuration { get; set; } = 0.48f;
    [Export] public float CoverHoldDuration { get; set; } = 0.18f;
    [Export] public float RevealDuration { get; set; } = 0.56f;
    [Export] public float CaptionRiseDistance { get; set; } = 24.0f;
    [Export] public float CaptionPanelWidth { get; set; } = 620.0f;
    [Export] public float CaptionPanelHeight { get; set; } = 96.0f;
    [Export] public float CaptionBottomMargin { get; set; } = 76.0f;
    [Export] public Color BackdropColor { get; set; } = new Color(0.01f, 0.01f, 0.015f, 1.0f);

    private ColorRect _backdrop;
    private Panel _captionPanel;
    private Label _titleLabel;
    private Label _subtitleLabel;
    private Vector2 _lastViewportSize = Vector2.Zero;

    public override void _Ready()
    {
        _backdrop = GetNodeOrNull<ColorRect>("Backdrop");
        _captionPanel = GetNodeOrNull<Panel>("CaptionPanel");
        _titleLabel = GetNodeOrNull<Label>("CaptionPanel/TitleLabel");
        _subtitleLabel = GetNodeOrNull<Label>("CaptionPanel/SubtitleLabel");

        ConfigureStyles();
        ApplyLayout(true);
        HideImmediately();
    }

    public override void _Process(double delta)
    {
        ApplyLayout();
    }

    public void HideImmediately()
    {
        Visible = false;
        ApplyLayout(true);

        if (_backdrop != null)
        {
            _backdrop.Color = BackdropColor;
            _backdrop.Modulate = new Color(1, 1, 1, 0.0f);
        }

        SetCaptionAlpha(0.0f);
        if (_captionPanel != null)
        {
            _captionPanel.Scale = Vector2.One;
            _captionPanel.Position = GetCaptionRestPosition();
        }
    }

    public async Task PlayCoverAsync(string titleText = "", string subtitleText = "")
    {
        PrepareCaption(titleText, subtitleText);
        ApplyLayout(true);
        Visible = true;

        if (_backdrop != null)
        {
            _backdrop.Color = BackdropColor;
            _backdrop.Modulate = new Color(1, 1, 1, 0.0f);
        }

        Vector2 captionRestPosition = GetCaptionRestPosition();
        if (_captionPanel != null)
        {
            _captionPanel.Position = captionRestPosition + new Vector2(0.0f, CaptionRiseDistance);
            _captionPanel.Scale = Vector2.One * 0.975f;
        }
        SetCaptionAlpha(0.0f);

        Tween transitionTween = CreateTween();
        transitionTween.SetParallel(true);

        if (_backdrop != null)
        {
            transitionTween.TweenProperty(_backdrop, "modulate:a", 1.0f, CoverDuration)
                .SetEase(Tween.EaseType.InOut)
                .SetTrans(Tween.TransitionType.Sine);
        }

        if (_captionPanel != null && _captionPanel.Visible)
        {
            float captionDuration = Mathf.Max(0.20f, CoverDuration * 0.82f);
            transitionTween.TweenProperty(_captionPanel, "modulate:a", 1.0f, captionDuration)
                .SetEase(Tween.EaseType.Out)
                .SetTrans(Tween.TransitionType.Cubic);
            transitionTween.TweenProperty(_captionPanel, "position", captionRestPosition, captionDuration)
                .SetEase(Tween.EaseType.Out)
                .SetTrans(Tween.TransitionType.Cubic);
            transitionTween.TweenProperty(_captionPanel, "scale", Vector2.One, captionDuration)
                .SetEase(Tween.EaseType.Out)
                .SetTrans(Tween.TransitionType.Cubic);

            if (_titleLabel != null && _titleLabel.Visible)
            {
                transitionTween.TweenProperty(_titleLabel, "modulate:a", 1.0f, captionDuration)
                    .SetEase(Tween.EaseType.Out)
                    .SetTrans(Tween.TransitionType.Cubic);
            }

            if (_subtitleLabel != null && _subtitleLabel.Visible)
            {
                transitionTween.TweenProperty(_subtitleLabel, "modulate:a", 1.0f, captionDuration)
                    .SetEase(Tween.EaseType.Out)
                    .SetTrans(Tween.TransitionType.Cubic);
            }
        }

        await ToSignal(transitionTween, Tween.SignalName.Finished);

        if (CoverHoldDuration > 0.0f)
        {
            await ToSignal(GetTree().CreateTimer(CoverHoldDuration), SceneTreeTimer.SignalName.Timeout);
        }
    }

    public async Task PlayRevealAsync()
    {
        if (!Visible)
        {
            return;
        }

        ApplyLayout(true);

        Tween transitionTween = CreateTween();
        transitionTween.SetParallel(true);

        if (_backdrop != null)
        {
            transitionTween.TweenProperty(_backdrop, "modulate:a", 0.0f, RevealDuration)
                .SetEase(Tween.EaseType.Out)
                .SetTrans(Tween.TransitionType.Sine);
        }

        if (_captionPanel != null && _captionPanel.Visible)
        {
            float captionDuration = Mathf.Max(0.22f, RevealDuration * 0.72f);
            Vector2 endPosition = GetCaptionRestPosition() - new Vector2(0.0f, CaptionRiseDistance * 0.35f);
            transitionTween.TweenProperty(_captionPanel, "modulate:a", 0.0f, captionDuration)
                .SetEase(Tween.EaseType.In)
                .SetTrans(Tween.TransitionType.Sine);
            transitionTween.TweenProperty(_captionPanel, "position", endPosition, captionDuration)
                .SetEase(Tween.EaseType.In)
                .SetTrans(Tween.TransitionType.Sine);

            if (_titleLabel != null && _titleLabel.Visible)
            {
                transitionTween.TweenProperty(_titleLabel, "modulate:a", 0.0f, captionDuration)
                    .SetEase(Tween.EaseType.In)
                    .SetTrans(Tween.TransitionType.Sine);
            }

            if (_subtitleLabel != null && _subtitleLabel.Visible)
            {
                transitionTween.TweenProperty(_subtitleLabel, "modulate:a", 0.0f, captionDuration)
                    .SetEase(Tween.EaseType.In)
                    .SetTrans(Tween.TransitionType.Sine);
            }
        }

        await ToSignal(transitionTween, Tween.SignalName.Finished);
        HideImmediately();
    }

    private void PrepareCaption(string titleText, string subtitleText)
    {
        string resolvedTitle = titleText?.Trim() ?? string.Empty;
        string resolvedSubtitle = subtitleText?.Trim() ?? string.Empty;
        bool hasCaption = !string.IsNullOrWhiteSpace(resolvedTitle) || !string.IsNullOrWhiteSpace(resolvedSubtitle);

        if (_captionPanel != null)
        {
            _captionPanel.Visible = hasCaption;
        }

        if (_titleLabel != null)
        {
            _titleLabel.Text = resolvedTitle;
            _titleLabel.Visible = !string.IsNullOrWhiteSpace(resolvedTitle);
        }

        if (_subtitleLabel != null)
        {
            _subtitleLabel.Text = resolvedSubtitle;
            _subtitleLabel.Visible = !string.IsNullOrWhiteSpace(resolvedSubtitle);
        }
    }

    private void ConfigureStyles()
    {
        if (_backdrop != null)
        {
            _backdrop.MouseFilter = Control.MouseFilterEnum.Stop;
            _backdrop.Color = BackdropColor;
        }

        if (_captionPanel != null)
        {
            _captionPanel.MouseFilter = Control.MouseFilterEnum.Ignore;

            StyleBoxFlat panelStyle = new StyleBoxFlat();
            panelStyle.BgColor = new Color(0.06f, 0.05f, 0.06f, 0.78f);
            panelStyle.BorderColor = new Color(0.85f, 0.74f, 0.52f, 0.95f);
            panelStyle.BorderWidthLeft = 2;
            panelStyle.BorderWidthTop = 2;
            panelStyle.BorderWidthRight = 2;
            panelStyle.BorderWidthBottom = 2;
            panelStyle.CornerRadiusTopLeft = 6;
            panelStyle.CornerRadiusTopRight = 6;
            panelStyle.CornerRadiusBottomRight = 6;
            panelStyle.CornerRadiusBottomLeft = 6;
            _captionPanel.AddThemeStyleboxOverride("panel", panelStyle);
        }

        SystemFont systemFont = new SystemFont
        {
            FontNames = new[]
            {
                "Microsoft YaHei UI",
                "Microsoft YaHei",
                "SimHei",
                "SimSun"
            }
        };

        if (_titleLabel != null)
        {
            _titleLabel.MouseFilter = Control.MouseFilterEnum.Ignore;
            _titleLabel.HorizontalAlignment = HorizontalAlignment.Center;
            _titleLabel.VerticalAlignment = VerticalAlignment.Center;
            _titleLabel.LabelSettings = new LabelSettings
            {
                Font = systemFont,
                FontSize = 22,
                FontColor = new Color(0.97f, 0.93f, 0.86f, 1.0f),
                OutlineColor = new Color(0.02f, 0.02f, 0.02f, 0.95f),
                OutlineSize = 3
            };
        }

        if (_subtitleLabel != null)
        {
            _subtitleLabel.MouseFilter = Control.MouseFilterEnum.Ignore;
            _subtitleLabel.HorizontalAlignment = HorizontalAlignment.Center;
            _subtitleLabel.VerticalAlignment = VerticalAlignment.Center;
            _subtitleLabel.LabelSettings = new LabelSettings
            {
                Font = systemFont,
                FontSize = 16,
                FontColor = new Color(0.88f, 0.88f, 0.88f, 0.96f),
                OutlineColor = new Color(0.02f, 0.02f, 0.02f, 0.92f),
                OutlineSize = 3
            };
        }
    }

    private void ApplyLayout(bool force = false)
    {
        Vector2 viewportSize = GetViewport().GetVisibleRect().Size;
        if (viewportSize == Vector2.Zero)
        {
            return;
        }

        if (!force && viewportSize.IsEqualApprox(_lastViewportSize))
        {
            return;
        }

        _lastViewportSize = viewportSize;

        if (_backdrop != null)
        {
            _backdrop.Position = Vector2.Zero;
            _backdrop.Size = viewportSize;
        }

        if (_captionPanel != null)
        {
            _captionPanel.Size = new Vector2(Mathf.Min(CaptionPanelWidth, viewportSize.X - 120.0f), CaptionPanelHeight);
            _captionPanel.Position = GetCaptionRestPosition();
        }

        if (_titleLabel != null)
        {
            _titleLabel.Position = new Vector2(24.0f, 14.0f);
            _titleLabel.Size = new Vector2(Mathf.Max(120.0f, (_captionPanel?.Size.X ?? CaptionPanelWidth) - 48.0f), 34.0f);
        }

        if (_subtitleLabel != null)
        {
            _subtitleLabel.Position = new Vector2(24.0f, 48.0f);
            _subtitleLabel.Size = new Vector2(Mathf.Max(120.0f, (_captionPanel?.Size.X ?? CaptionPanelWidth) - 48.0f), 24.0f);
        }
    }

    private Vector2 GetCaptionRestPosition()
    {
        Vector2 viewportSize = GetViewport().GetVisibleRect().Size;
        Vector2 panelSize = _captionPanel?.Size ?? new Vector2(CaptionPanelWidth, CaptionPanelHeight);
        return new Vector2(
            (viewportSize.X - panelSize.X) * 0.5f,
            viewportSize.Y - CaptionBottomMargin - panelSize.Y);
    }

    private void SetCaptionAlpha(float alpha)
    {
        if (_captionPanel != null)
        {
            _captionPanel.Modulate = new Color(1, 1, 1, alpha);
        }

        if (_titleLabel != null)
        {
            _titleLabel.Modulate = new Color(1, 1, 1, alpha);
        }

        if (_subtitleLabel != null)
        {
            _subtitleLabel.Modulate = new Color(1, 1, 1, alpha);
        }
    }
}
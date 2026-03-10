using Godot;

public partial class StoryHeaderBar : CanvasLayer
{
    [Export] public string ChapterText { get; set; } = string.Empty;
    [Export] public string LocationText { get; set; } = string.Empty;
    [Export] public float HeaderHeight { get; set; } = 56.0f;
    [Export] public bool AutoFadeIn { get; set; } = false;
    [Export] public float FadeDuration { get; set; } = 0.32f;

    private Panel _headerPanel;
    private Label _chapterLabel;
    private Label _locationLabel;
    private Vector2 _lastViewportSize = Vector2.Zero;

    public override void _Ready()
    {
        _headerPanel = GetNodeOrNull<Panel>("HeaderPanel");
        _chapterLabel = GetNodeOrNull<Label>("HeaderPanel/ChapterLabel");
        _locationLabel = GetNodeOrNull<Label>("HeaderPanel/LocationLabel");

        ConfigureStyles();
        ApplyLayout(true);

        if (AutoFadeIn)
        {
            SetAlpha(0.0f);
            PlayFadeIn();
        }
        else
        {
            SetAlpha(1.0f);
        }
    }

    public override void _Process(double delta)
    {
        ApplyLayout();
    }

    public void SetContext(string chapterText, string locationText, bool fadeIn = false)
    {
        ChapterText = chapterText?.Trim() ?? string.Empty;
        LocationText = locationText?.Trim() ?? string.Empty;
        ApplyLayout(true);

        if (fadeIn)
        {
            SetAlpha(0.0f);
            PlayFadeIn();
        }
        else
        {
            SetAlpha(1.0f);
        }
    }

    public void HideBar()
    {
        ChapterText = string.Empty;
        LocationText = string.Empty;
        SetAlpha(0.0f);
        Visible = false;
    }

    private void ConfigureStyles()
    {
        if (_headerPanel == null)
        {
            return;
        }

        _headerPanel.MouseFilter = Control.MouseFilterEnum.Ignore;

        StyleBoxFlat headerStyle = new StyleBoxFlat();
        headerStyle.BgColor = new Color(0.03f, 0.02f, 0.03f, 0.62f);
        headerStyle.BorderColor = new Color(0.88f, 0.76f, 0.54f, 0.82f);
        headerStyle.BorderWidthBottom = 2;
        _headerPanel.AddThemeStyleboxOverride("panel", headerStyle);

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

        if (_chapterLabel != null)
        {
            _chapterLabel.MouseFilter = Control.MouseFilterEnum.Ignore;
            _chapterLabel.HorizontalAlignment = HorizontalAlignment.Left;
            _chapterLabel.VerticalAlignment = VerticalAlignment.Center;
            _chapterLabel.LabelSettings = new LabelSettings
            {
                Font = systemFont,
                FontSize = 19,
                FontColor = new Color(0.98f, 0.93f, 0.83f, 1.0f),
                OutlineColor = new Color(0.04f, 0.03f, 0.03f, 0.82f),
                OutlineSize = 3
            };
        }

        if (_locationLabel != null)
        {
            _locationLabel.MouseFilter = Control.MouseFilterEnum.Ignore;
            _locationLabel.HorizontalAlignment = HorizontalAlignment.Right;
            _locationLabel.VerticalAlignment = VerticalAlignment.Center;
            _locationLabel.LabelSettings = new LabelSettings
            {
                Font = systemFont,
                FontSize = 17,
                FontColor = new Color(0.92f, 0.92f, 0.92f, 0.98f),
                OutlineColor = new Color(0.04f, 0.03f, 0.03f, 0.82f),
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

        bool hasHeader = !string.IsNullOrWhiteSpace(ChapterText) || !string.IsNullOrWhiteSpace(LocationText);
        Visible = hasHeader;
        if (!hasHeader || _headerPanel == null)
        {
            return;
        }

        _headerPanel.Position = Vector2.Zero;
        _headerPanel.Size = new Vector2(viewportSize.X, Mathf.Max(24.0f, HeaderHeight));

        if (_chapterLabel != null)
        {
            _chapterLabel.Position = new Vector2(22.0f, 8.0f);
            _chapterLabel.Size = new Vector2(Mathf.Max(240.0f, viewportSize.X * 0.50f - 34.0f), 38.0f);
            _chapterLabel.Text = ChapterText;
            _chapterLabel.Visible = !string.IsNullOrWhiteSpace(ChapterText);
        }

        if (_locationLabel != null)
        {
            _locationLabel.Position = new Vector2(viewportSize.X * 0.52f, 8.0f);
            _locationLabel.Size = new Vector2(Mathf.Max(180.0f, viewportSize.X * 0.46f - 28.0f), 38.0f);
            _locationLabel.Text = LocationText;
            _locationLabel.Visible = !string.IsNullOrWhiteSpace(LocationText);
        }
    }

    private void SetAlpha(float alpha)
    {
        if (_headerPanel != null)
        {
            _headerPanel.Modulate = new Color(1, 1, 1, alpha);
        }

        if (_chapterLabel != null)
        {
            _chapterLabel.Modulate = new Color(1, 1, 1, alpha);
        }

        if (_locationLabel != null)
        {
            _locationLabel.Modulate = new Color(1, 1, 1, alpha);
        }
    }

    private void PlayFadeIn()
    {
        Tween fadeTween = CreateTween();
        fadeTween.SetParallel(true);

        if (_headerPanel != null)
        {
            fadeTween.TweenProperty(_headerPanel, "modulate:a", 1.0f, FadeDuration)
                .SetEase(Tween.EaseType.Out)
                .SetTrans(Tween.TransitionType.Sine);
        }

        if (_chapterLabel != null && _chapterLabel.Visible)
        {
            fadeTween.TweenProperty(_chapterLabel, "modulate:a", 1.0f, FadeDuration)
                .SetEase(Tween.EaseType.Out)
                .SetTrans(Tween.TransitionType.Sine);
        }

        if (_locationLabel != null && _locationLabel.Visible)
        {
            fadeTween.TweenProperty(_locationLabel, "modulate:a", 1.0f, FadeDuration)
                .SetEase(Tween.EaseType.Out)
                .SetTrans(Tween.TransitionType.Sine);
        }
    }
}


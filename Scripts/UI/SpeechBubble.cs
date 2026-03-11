using Godot;
using System.Collections.Generic;

public partial class SpeechBubble : Control
{
    [Export] public float DefaultBubbleWidth { get; set; } = 280.0f;
    [Export] public float MinBubbleWidth { get; set; } = 180.0f;
    [Export] public float MaxBubbleWidth { get; set; } = 340.0f;
    [Export] public float ScreenMargin { get; set; } = 12.0f;
    [Export] public float MinimumBottomMargin { get; set; } = 18.0f;
    [Export] public Vector2 DefaultBubbleOffset { get; set; } = new Vector2(0.0f, -92.0f);
    [Export] public float DefaultTypewriterSpeed { get; set; } = 34.0f;
    [Export] public float TailHeight { get; set; } = 30.0f;
    [Export] public float TailBaseWidth { get; set; } = 52.0f;
    [Export] public float TailInsetWidth { get; set; } = 22.0f;
    [Export] public float TailSafeMargin { get; set; } = 34.0f;
    [Export] public float CornerRadius { get; set; } = 16.0f;
    [Export] public float BorderWidth { get; set; } = 3.0f;
    [Export] public Vector2 ShadowOffset { get; set; } = new Vector2(0.0f, 5.0f);

    private readonly Color _fillColor = new Color(0.20f, 0.13f, 0.06f, 0.82f);
    private readonly Color _borderColor = new Color(0.84f, 0.68f, 0.30f, 0.96f);
    private readonly Color _shadowColor = new Color(0.02f, 0.01f, 0.0f, 0.32f);

    private Label _speakerLabel;
    private Label _textLabel;

    private Node2D _followActor;
    private string _fullText = string.Empty;
    private Vector2 _bubbleOffset;
    private float _bubbleWidth;
    private bool _showSpeakerName;
    private Vector2 _fallbackAnchor;
    private Vector2 _lastViewportSize = Vector2.Zero;
    private double _visibleCharacters;
    private float _typewriterSpeed;
    private float _bodyHeight;
    private float _tailCenterX;

    public bool IsTyping { get; private set; }

    public override void _Ready()
    {
        _speakerLabel = GetNode<Label>("SpeakerLabel");
        _textLabel = GetNode<Label>("TextLabel");

        MouseFilter = MouseFilterEnum.Ignore;
        _speakerLabel.MouseFilter = MouseFilterEnum.Ignore;
        _textLabel.MouseFilter = MouseFilterEnum.Ignore;

        ConfigureFonts();
        Visible = false;
    }

    public override void _Process(double delta)
    {
        if (!Visible)
        {
            return;
        }

        ApplyLayout();

        if (!IsTyping)
        {
            return;
        }

        _visibleCharacters += _typewriterSpeed * delta;
        int visibleCount = Mathf.Min(_fullText.Length, Mathf.RoundToInt((float)_visibleCharacters));
        _textLabel.VisibleCharacters = visibleCount;
        if (visibleCount >= _fullText.Length)
        {
            _textLabel.VisibleCharacters = -1;
            IsTyping = false;
        }
    }

    public override void _Draw()
    {
        if (!Visible || Size == Vector2.Zero || _bodyHeight <= 0.0f)
        {
            return;
        }

        Vector2[] outline = BuildBubbleOutline(_bubbleWidth, _bodyHeight, _tailCenterX);
        Vector2[] shadow = OffsetPoints(outline, ShadowOffset);
        Vector2[] closedOutline = CloseLoop(outline);

        DrawColoredPolygon(shadow, _shadowColor);
        DrawColoredPolygon(outline, _fillColor);
        DrawPolyline(closedOutline, _borderColor, BorderWidth, true);
    }

    public void Setup(
        Node2D actor,
        string speakerName,
        string text,
        float typewriterSpeed,
        Vector2 bubbleOffset,
        float bubbleWidth,
        bool showSpeakerName,
        Vector2 fallbackAnchor)
    {
        _followActor = actor;
        _fullText = text ?? string.Empty;
        _typewriterSpeed = typewriterSpeed > 0.0f ? typewriterSpeed : DefaultTypewriterSpeed;
        _bubbleOffset = bubbleOffset == Vector2.Zero ? DefaultBubbleOffset : bubbleOffset;
        _bubbleWidth = Mathf.Clamp(bubbleWidth, MinBubbleWidth, MaxBubbleWidth);
        _showSpeakerName = showSpeakerName && !string.IsNullOrWhiteSpace(speakerName);
        _fallbackAnchor = fallbackAnchor;

        _speakerLabel.Text = speakerName ?? string.Empty;
        _speakerLabel.Visible = _showSpeakerName;
        _textLabel.Text = _fullText;
        _textLabel.VisibleCharacters = 0;
        _visibleCharacters = 0.0;
        IsTyping = _fullText.Length > 0;

        if (!IsTyping)
        {
            _textLabel.VisibleCharacters = -1;
        }

        Visible = true;
        ApplyLayout(true);
    }

    public void RevealAll()
    {
        if (!Visible)
        {
            return;
        }

        _textLabel.VisibleCharacters = -1;
        IsTyping = false;
    }

    private void ApplyLayout(bool force = false)
    {
        Vector2 viewportSize = GetViewport().GetVisibleRect().Size;
        if (viewportSize == Vector2.Zero)
        {
            return;
        }

        if (!force && viewportSize.IsEqualApprox(_lastViewportSize) && _followActor == null)
        {
            return;
        }

        _lastViewportSize = viewportSize;

        float textAreaWidth = Mathf.Max(120.0f, _bubbleWidth - 30.0f);
        int estimatedLineCount = EstimateLineCount(_fullText, textAreaWidth);
        float textTop = _showSpeakerName ? 40.0f : 16.0f;
        float textHeight = Mathf.Max(28.0f, estimatedLineCount * 26.0f + 6.0f);
        _bodyHeight = textTop + textHeight + 18.0f;
        float totalHeight = _bodyHeight + TailHeight;

        Size = new Vector2(_bubbleWidth, totalHeight);

        _speakerLabel.Position = new Vector2(16.0f, 10.0f);
        _speakerLabel.Size = new Vector2(_bubbleWidth - 32.0f, 24.0f);

        _textLabel.Position = new Vector2(16.0f, textTop);
        _textLabel.Size = new Vector2(textAreaWidth, textHeight);

        Vector2 anchor = ResolveAnchorPosition(viewportSize);
        Vector2 desiredPosition = new Vector2(
            anchor.X - _bubbleWidth * 0.5f + _bubbleOffset.X,
            anchor.Y - _bodyHeight + _bubbleOffset.Y);

        desiredPosition.X = Mathf.Clamp(desiredPosition.X, ScreenMargin, viewportSize.X - _bubbleWidth - ScreenMargin);
        desiredPosition.Y = Mathf.Clamp(desiredPosition.Y, ScreenMargin, viewportSize.Y - totalHeight - MinimumBottomMargin);
        Position = desiredPosition.Round();

        float baseHalf = TailBaseWidth * 0.5f;
        float safeCenterMargin = Mathf.Max(TailSafeMargin, CornerRadius + baseHalf + BorderWidth + 4.0f);
        _tailCenterX = Mathf.Clamp(anchor.X - Position.X, safeCenterMargin, _bubbleWidth - safeCenterMargin);

        QueueRedraw();
    }

    private Vector2 ResolveAnchorPosition(Vector2 viewportSize)
    {
        if (_followActor != null && GodotObject.IsInstanceValid(_followActor))
        {
            return _followActor.GetGlobalTransformWithCanvas().Origin;
        }

        return _fallbackAnchor == Vector2.Zero ? viewportSize * 0.5f : _fallbackAnchor;
    }

    private Vector2[] BuildBubbleOutline(float bodyWidth, float bodyHeight, float tailCenterX)
    {
        List<Vector2> points = new();
        float radius = Mathf.Min(CornerRadius, Mathf.Min(bodyWidth, bodyHeight) * 0.25f);
        float baseHalf = TailBaseWidth * 0.5f;
        float insetHalf = TailInsetWidth * 0.5f;
        float tailMidY = bodyHeight + TailHeight * 0.36f;
        float tailTipY = bodyHeight + TailHeight;

        AddArcPoints(points, new Vector2(radius, radius), radius, 180.0f, 270.0f, 4);
        AddArcPoints(points, new Vector2(bodyWidth - radius, radius), radius, 270.0f, 360.0f, 4);
        AddArcPoints(points, new Vector2(bodyWidth - radius, bodyHeight - radius), radius, 0.0f, 90.0f, 4);

        points.Add(new Vector2(tailCenterX + baseHalf, bodyHeight));
        points.Add(new Vector2(tailCenterX + insetHalf, tailMidY));
        points.Add(new Vector2(tailCenterX, tailTipY));
        points.Add(new Vector2(tailCenterX - insetHalf, tailMidY));
        points.Add(new Vector2(tailCenterX - baseHalf, bodyHeight));

        AddArcPoints(points, new Vector2(radius, bodyHeight - radius), radius, 90.0f, 180.0f, 4);

        return points.ToArray();
    }

    private static void AddArcPoints(List<Vector2> points, Vector2 center, float radius, float startDegrees, float endDegrees, int segments)
    {
        for (int index = 0; index <= segments; index++)
        {
            float t = segments == 0 ? 1.0f : index / (float)segments;
            float angleDegrees = Mathf.Lerp(startDegrees, endDegrees, t);
            float angleRadians = Mathf.DegToRad(angleDegrees);
            Vector2 point = center + new Vector2(Mathf.Cos(angleRadians), Mathf.Sin(angleRadians)) * radius;
            if (points.Count == 0 || !points[^1].IsEqualApprox(point))
            {
                points.Add(point);
            }
        }
    }

    private static Vector2[] OffsetPoints(Vector2[] points, Vector2 offset)
    {
        Vector2[] result = new Vector2[points.Length];
        for (int index = 0; index < points.Length; index++)
        {
            result[index] = points[index] + offset;
        }

        return result;
    }

    private static Vector2[] CloseLoop(Vector2[] points)
    {
        Vector2[] closed = new Vector2[points.Length + 1];
        for (int index = 0; index < points.Length; index++)
        {
            closed[index] = points[index];
        }

        closed[^1] = points[0];
        return closed;
    }

    private static int EstimateLineCount(string text, float availableWidth)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return 1;
        }

        float charsPerLine = Mathf.Max(8.0f, availableWidth / 20.0f);
        return Mathf.Clamp(Mathf.CeilToInt(text.Length / charsPerLine), 1, 6);
    }

    private void ConfigureFonts()
    {
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

        _speakerLabel.LabelSettings = new LabelSettings
        {
            Font = systemFont,
            FontSize = 16,
            FontColor = new Color(0.96f, 0.82f, 0.46f, 1.0f),
            OutlineColor = new Color(0.10f, 0.06f, 0.02f, 0.96f),
            OutlineSize = 2
        };
        _speakerLabel.HorizontalAlignment = HorizontalAlignment.Left;
        _speakerLabel.VerticalAlignment = VerticalAlignment.Center;

        _textLabel.LabelSettings = new LabelSettings
        {
            Font = systemFont,
            FontSize = 20,
            FontColor = new Color(0.97f, 0.93f, 0.84f, 1.0f),
            OutlineColor = new Color(0.12f, 0.07f, 0.03f, 0.92f),
            OutlineSize = 1
        };
        _textLabel.HorizontalAlignment = HorizontalAlignment.Left;
        _textLabel.VerticalAlignment = VerticalAlignment.Top;
        _textLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
    }
}

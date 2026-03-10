using Godot;

public partial class SheetEffectPlayer : Sprite2D
{
    [Signal]
    public delegate void EffectFinishedEventHandler();

    [Export] public int SheetColumns { get; set; } = 5;
    [Export] public int SheetRows { get; set; } = 5;
    [Export] public int SheetOuterPadding { get; set; } = 3;
    [Export] public int FrameSpacing { get; set; } = 2;
    [Export] public int FrameInset { get; set; } = 2;
    [Export] public bool HideWhenFinished { get; set; } = false;
    [Export] public float FramesPerSecond { get; set; } = 13.0f;
    [Export] public int StartFrame { get; set; } = 1;
    [Export] public int EndFrame { get; set; } = 18;

    private bool _playing;
    private int _currentFrame;
    private double _timer;

    public override void _Ready()
    {
        Centered = true;
        TextureFilter = TextureFilterEnum.Nearest;
        RegionEnabled = true;
        RegionFilterClipEnabled = true;
        Visible = false;
        Hframes = 1;
        Vframes = 1;
        ApplyFrame(Mathf.Max(0, StartFrame));
    }

    public override void _Process(double delta)
    {
        if (!_playing || FramesPerSecond <= 0.0f)
        {
            return;
        }

        double frameDuration = 1.0 / FramesPerSecond;
        _timer += delta;
        while (_timer >= frameDuration)
        {
            _timer -= frameDuration;
            AdvanceFrame();
        }
    }

    public void PlayOnce()
    {
        _playing = true;
        _timer = 0.0;
        _currentFrame = Mathf.Max(0, StartFrame);
        Visible = true;
        ApplyFrame(_currentFrame);
    }

    private void AdvanceFrame()
    {
        if (_currentFrame < EndFrame)
        {
            _currentFrame++;
            ApplyFrame(_currentFrame);
            return;
        }

        _playing = false;
        if (HideWhenFinished)
        {
            Visible = false;
        }

        EmitSignal(SignalName.EffectFinished);
    }

    private void ApplyFrame(int frameIndex)
    {
        if (Texture == null)
        {
            return;
        }

        int frameX = frameIndex % SheetColumns;
        int frameY = frameIndex / SheetColumns;
        Vector2 textureSize = Texture.GetSize();
        int frameWidth = CalculateFrameLength(Mathf.RoundToInt(textureSize.X), SheetColumns);
        int frameHeight = CalculateFrameLength(Mathf.RoundToInt(textureSize.Y), SheetRows);
        int safeInsetX = Mathf.Clamp(FrameInset, 0, Mathf.Max(0, frameWidth / 2 - 1));
        int safeInsetY = Mathf.Clamp(FrameInset, 0, Mathf.Max(0, frameHeight / 2 - 1));

        float left = SheetOuterPadding + frameX * (frameWidth + FrameSpacing) + safeInsetX;
        float top = SheetOuterPadding + frameY * (frameHeight + FrameSpacing) + safeInsetY;
        float width = Mathf.Max(1, frameWidth - safeInsetX * 2);
        float height = Mathf.Max(1, frameHeight - safeInsetY * 2);

        RegionRect = new Rect2(left, top, width, height);
    }

    private int CalculateFrameLength(int axisLength, int segmentCount)
    {
        int totalSpacing = SheetOuterPadding * 2 + FrameSpacing * (segmentCount - 1);
        return Mathf.Max(1, (axisLength - totalSpacing) / segmentCount);
    }
}
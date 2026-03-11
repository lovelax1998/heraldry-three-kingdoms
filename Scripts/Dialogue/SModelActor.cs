using Godot;
using System;
using System.Collections.Generic;

public partial class SModelActor : Sprite2D
{
    private sealed class AnimationClipDefinition
    {
        public Vector2I[] Frames { get; init; } = Array.Empty<Vector2I>();
        public Vector2I[] FallbackFrames { get; init; } = Array.Empty<Vector2I>();
        public bool UseSubSheet { get; init; }
        public float? FramesPerSecond { get; init; }
    }

    [Export] public float AnimationFps { get; set; } = 7.0f;
    [Export] public float IdleAnimationFps { get; set; } = 1.0f;
    [Export] public int SheetColumns { get; set; } = 5;
    [Export] public int SheetRows { get; set; } = 5;
    [Export] public int SheetOuterPadding { get; set; } = 3;
    [Export] public int FrameSpacing { get; set; } = 2;
    [Export] public int FrameInset { get; set; } = 2;
    [Export] public bool SideFramesFaceRight { get; set; } = true;

    private readonly Dictionary<string, AnimationClipDefinition> _animations = new();
    private Vector2I[] _currentFrames = Array.Empty<Vector2I>();
    private int _frameIndex;
    private double _frameTimer;
    private bool _loop = true;
    private bool _holdLastFrame;
    private float _currentAnimationFps;
    private Texture2D _mainTexture;
    private Texture2D _subTexture;

    public override void _Ready()
    {
        Centered = true;
        TextureFilter = TextureFilterEnum.Nearest;
        RegionEnabled = true;
        RegionFilterClipEnabled = true;
        Hframes = 1;
        Vframes = 1;

        _mainTexture ??= Texture;

        BuildAnimations();
        PlayPresetAnimation("idle_front", "right");
    }

    public override void _Process(double delta)
    {
        Position = Position.Round();

        if (_currentFrames.Length <= 1 || _currentAnimationFps <= 0.0f)
        {
            return;
        }

        double frameDuration = 1.0 / _currentAnimationFps;
        _frameTimer += delta;
        while (_frameTimer >= frameDuration)
        {
            _frameTimer -= frameDuration;
            AdvanceFrame();
        }
    }

    public void SetAnimationSheets(Texture2D mainTexture, Texture2D subTexture = null)
    {
        _mainTexture = mainTexture;
        _subTexture = subTexture;

        if (_mainTexture != null)
        {
            Texture = _mainTexture;
        }
    }

    public void PlayIdleFacing(string facing)
    {
        PlayDirectionalAnimation("idle", facing);
    }

    public void PlayWalkFacing(string facing)
    {
        PlayDirectionalAnimation("walk", facing);
    }

    public void PlayAttackFacing(string facing, bool holdLastFrame = false)
    {
        PlayDirectionalAnimation("attack", facing, loop: false, holdLastFrame: holdLastFrame);
    }

    public float GetDirectionalAnimationDuration(string animationBaseName, string facing)
    {
        string normalizedFacing = NormalizeFacing(facing);
        string animationName = normalizedFacing switch
        {
            "back" => $"{animationBaseName}_back",
            "left" => $"{animationBaseName}_side",
            "right" => $"{animationBaseName}_side",
            _ => $"{animationBaseName}_front"
        };

        return GetAnimationDuration(animationName);
    }

    public float GetAnimationDuration(string animationName)
    {
        if (!_animations.TryGetValue(animationName, out AnimationClipDefinition clip))
        {
            return 0.0f;
        }

        Vector2I[] frames = ResolveFrames(clip, out _);
        float fps = ResolveFramesPerSecond(clip);
        if (frames.Length == 0 || fps <= 0.0f)
        {
            return 0.0f;
        }

        return frames.Length / fps;
    }

    public void PlayDirectionalAnimation(string animationBaseName, string facing, bool loop = true, bool holdLastFrame = false)
    {
        string normalizedFacing = NormalizeFacing(facing);
        string animationName = normalizedFacing switch
        {
            "back" => $"{animationBaseName}_back",
            "left" => $"{animationBaseName}_side",
            "right" => $"{animationBaseName}_side",
            _ => $"{animationBaseName}_front"
        };

        string horizontalFacing = normalizedFacing == "left" ? "left" : "right";
        PlayPresetAnimation(animationName, horizontalFacing, loop, holdLastFrame);
    }

    public static string NormalizeFacing(string facing)
    {
        if (string.IsNullOrWhiteSpace(facing))
        {
            return "front";
        }

        string normalizedFacing = facing.Trim().ToLowerInvariant();
        return normalizedFacing switch
        {
            "left" or "right" or "front" or "back" => normalizedFacing,
            "up" => "back",
            "down" => "front",
            _ => "front"
        };
    }

    public void PlayPresetAnimation(string animationName, string facing, bool loop = true, bool holdLastFrame = false)
    {
        bool facingRight = !string.Equals(facing, "left", StringComparison.OrdinalIgnoreCase);

        switch (animationName)
        {
            case "idle_side":
            case "walk_side":
            case "attack_side":
            case "guard_side":
                FlipH = SideFramesFaceRight ? !facingRight : facingRight;
                break;
            default:
                FlipH = false;
                break;
        }

        PlayAnimation(animationName, loop, holdLastFrame);
    }

    public void PlayAnimation(string animationName, bool loop = true, bool holdLastFrame = false)
    {
        if (!_animations.TryGetValue(animationName, out AnimationClipDefinition clip))
        {
            GD.PushWarning($"SModel animation '{animationName}' is not defined on {Name}.");
            return;
        }

        Vector2I[] frames = ResolveFrames(clip, out bool useSubSheet);
        if (frames.Length == 0)
        {
            GD.PushWarning($"SModel animation '{animationName}' has no valid frames on {Name}.");
            return;
        }

        Texture2D animationTexture = ResolveTextureForClip(useSubSheet);
        if (animationTexture != null)
        {
            Texture = animationTexture;
        }

        _currentFrames = frames;
        _frameIndex = 0;
        _frameTimer = 0.0;
        _loop = loop;
        _holdLastFrame = holdLastFrame;
        _currentAnimationFps = ResolveFramesPerSecond(clip);
        ApplyFrameRegion(_currentFrames[0]);
    }

    private Vector2I[] ResolveFrames(AnimationClipDefinition clip, out bool useSubSheet)
    {
        useSubSheet = clip != null && clip.UseSubSheet && _subTexture != null;
        if (useSubSheet && clip.Frames.Length > 0)
        {
            return clip.Frames;
        }

        if (clip?.FallbackFrames != null && clip.FallbackFrames.Length > 0)
        {
            return clip.FallbackFrames;
        }

        return clip?.Frames ?? Array.Empty<Vector2I>();
    }

    private float ResolveFramesPerSecond(AnimationClipDefinition clip)
    {
        if (clip?.FramesPerSecond is float clipFps && clipFps > 0.0f)
        {
            return clipFps;
        }

        return AnimationFps;
    }

    private Texture2D ResolveTextureForClip(bool useSubSheet)
    {
        if (useSubSheet && _subTexture != null)
        {
            return _subTexture;
        }

        return _mainTexture ?? Texture;
    }

    private void AdvanceFrame()
    {
        if (_currentFrames.Length == 0)
        {
            return;
        }

        if (_frameIndex < _currentFrames.Length - 1)
        {
            _frameIndex++;
            ApplyFrameRegion(_currentFrames[_frameIndex]);
            return;
        }

        if (_loop)
        {
            _frameIndex = 0;
            ApplyFrameRegion(_currentFrames[_frameIndex]);
            return;
        }

        if (_holdLastFrame)
        {
            ApplyFrameRegion(_currentFrames[_currentFrames.Length - 1]);
            return;
        }

        _frameIndex = 0;
        ApplyFrameRegion(_currentFrames[0]);
    }

    private void ApplyFrameRegion(Vector2I frameCoords)
    {
        if (Texture == null)
        {
            return;
        }

        Vector2 textureSize = Texture.GetSize();
        int frameWidth = CalculateFrameLength(Mathf.RoundToInt(textureSize.X), SheetColumns);
        int frameHeight = CalculateFrameLength(Mathf.RoundToInt(textureSize.Y), SheetRows);
        int safeInsetX = Mathf.Clamp(FrameInset, 0, Mathf.Max(0, frameWidth / 2 - 1));
        int safeInsetY = Mathf.Clamp(FrameInset, 0, Mathf.Max(0, frameHeight / 2 - 1));

        float left = SheetOuterPadding + frameCoords.X * (frameWidth + FrameSpacing) + safeInsetX;
        float top = SheetOuterPadding + frameCoords.Y * (frameHeight + FrameSpacing) + safeInsetY;
        float width = Mathf.Max(1, frameWidth - safeInsetX * 2);
        float height = Mathf.Max(1, frameHeight - safeInsetY * 2);

        RegionRect = new Rect2(left, top, width, height);
    }

    private int CalculateFrameLength(int axisLength, int segmentCount)
    {
        int totalSpacing = SheetOuterPadding * 2 + FrameSpacing * (segmentCount - 1);
        return Mathf.Max(1, (axisLength - totalSpacing) / segmentCount);
    }

    private void BuildAnimations()
    {
        _animations["idle_front"] = new AnimationClipDefinition
        {
            Frames = new[] { new Vector2I(0, 0), new Vector2I(1, 0) },
            FallbackFrames = new[] { new Vector2I(0, 0) },
            UseSubSheet = true,
            FramesPerSecond = IdleAnimationFps
        };
        _animations["idle_side"] = new AnimationClipDefinition
        {
            Frames = new[] { new Vector2I(2, 0), new Vector2I(3, 0) },
            FallbackFrames = new[] { new Vector2I(0, 1) },
            UseSubSheet = true,
            FramesPerSecond = IdleAnimationFps
        };
        _animations["idle_back"] = new AnimationClipDefinition
        {
            Frames = new[] { new Vector2I(4, 0), new Vector2I(0, 1) },
            FallbackFrames = new[] { new Vector2I(0, 2) },
            UseSubSheet = true,
            FramesPerSecond = IdleAnimationFps
        };

        _animations["walk_front"] = new AnimationClipDefinition
        {
            Frames = new[]
            {
                new Vector2I(0, 3),
                new Vector2I(0, 0),
                new Vector2I(1, 3),
                new Vector2I(0, 0)
            }
        };
        _animations["walk_side"] = new AnimationClipDefinition
        {
            Frames = new[]
            {
                new Vector2I(2, 3),
                new Vector2I(0, 1),
                new Vector2I(3, 3),
                new Vector2I(0, 1)
            }
        };
        _animations["walk_back"] = new AnimationClipDefinition
        {
            Frames = new[]
            {
                new Vector2I(4, 3),
                new Vector2I(0, 2),
                new Vector2I(0, 4),
                new Vector2I(0, 2)
            }
        };

        _animations["attack_front"] = new AnimationClipDefinition
        {
            Frames = new[]
            {
                new Vector2I(1, 0),
                new Vector2I(2, 0),
                new Vector2I(3, 0)
            }
        };
        _animations["attack_side"] = new AnimationClipDefinition
        {
            Frames = new[]
            {
                new Vector2I(1, 1),
                new Vector2I(2, 1),
                new Vector2I(3, 1)
            }
        };
        _animations["attack_back"] = new AnimationClipDefinition
        {
            Frames = new[]
            {
                new Vector2I(1, 2),
                new Vector2I(2, 2),
                new Vector2I(3, 2)
            }
        };

        _animations["guard_front"] = new AnimationClipDefinition { Frames = new[] { new Vector2I(4, 0) } };
        _animations["guard_side"] = new AnimationClipDefinition { Frames = new[] { new Vector2I(4, 1) } };
        _animations["guard_back"] = new AnimationClipDefinition { Frames = new[] { new Vector2I(4, 2) } };
        _animations["celebrate_front"] = new AnimationClipDefinition { Frames = new[] { new Vector2I(1, 4) } };
        _animations["hit_front"] = new AnimationClipDefinition { Frames = new[] { new Vector2I(2, 4) } };
        _animations["kneel_front"] = new AnimationClipDefinition { Frames = new[] { new Vector2I(3, 4) } };
        _animations["down_front"] = new AnimationClipDefinition { Frames = new[] { new Vector2I(4, 4) } };
    }
}
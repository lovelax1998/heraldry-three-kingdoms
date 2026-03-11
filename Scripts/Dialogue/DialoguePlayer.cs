using Godot;
using System;
using System.Threading.Tasks;

public partial class DialoguePlayer : CanvasLayer
{
    [Signal]
    public delegate void DialogueFinishedEventHandler();

    [Export] public float TypewriterSpeed { get; set; } = 34.0f;
    [Export] public float TransitionPauseAfterMovement { get; set; } = 0.20f;
    [Export] public float TransitionPauseAfterDisappear { get; set; } = 0.50f;

    private ColorRect _backdrop;
    private Sprite2D _portrait;
    private Panel _dialogueBox;
    private Panel _namePlate;
    private Label _speakerName;
    private Label _dialogueText;
    private Label _advanceIndicator;

    private CharacterRepository _characterRepository;
    private DialogueSceneDefinition _sceneDefinition;
    private int _lineIndex = -1;
    private string _currentText = string.Empty;
    private double _visibleCharacters;
    private bool _isTyping;
    private Tween _advanceTween;
    private Vector2 _advanceIndicatorRestPosition;
    private Vector2 _lastViewportSize = Vector2.Zero;
    private Vector2 _portraitBaseScale = new Vector2(0.34f, 0.34f);
    private IDialogueActionRunner _actionRunner;
    private bool _isBusy;
    private int _autoAdvanceToken;
    private int _scheduledAutoAdvanceLineIndex = -1;

    public override void _Ready()
    {
        _backdrop = GetNode<ColorRect>("Backdrop");
        _portrait = GetNode<Sprite2D>("Portrait");
        _dialogueBox = GetNode<Panel>("DialogueBox");
        _namePlate = GetNode<Panel>("NamePlate");
        _speakerName = GetNode<Label>("SpeakerName");
        _dialogueText = GetNode<Label>("DialogueText");
        _advanceIndicator = GetNode<Label>("AdvanceIndicator");
        InitializeBubblePresentation();

        ConfigurePanels();
        ConfigureFonts();
        ConfigureLabelBehaviors();
        SetProcessUnhandledInput(true);

        Visible = false;
        _speakerName.Text = string.Empty;
        _dialogueText.Text = string.Empty;
        ResetDialoguePresentationState();

        ApplyLayout(true);
    }

    public void SetActionRunner(IDialogueActionRunner actionRunner)
    {
        _actionRunner = actionRunner;
    }

    public override void _Process(double delta)
    {
        ApplyLayout();

        if (HandleBubbleTypingProgress())
        {
            return;
        }

        if (!_isTyping)
        {
            return;
        }

        _visibleCharacters += TypewriterSpeed * delta;
        int visibleCharacterCount = Math.Min(_currentText.Length, Mathf.RoundToInt((float)_visibleCharacters));
        _dialogueText.VisibleCharacters = visibleCharacterCount;

        if (visibleCharacterCount >= _currentText.Length)
        {
            FinishTyping();
        }
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (!Visible || _isBusy || _sceneDefinition == null || _sceneDefinition.Lines.Count == 0)
        {
            return;
        }

        bool isAdvanceInput = @event.IsActionPressed("ui_accept")
            || (@event is InputEventMouseButton mouseButton && mouseButton.Pressed && mouseButton.ButtonIndex == MouseButton.Left);

        if (!isAdvanceInput)
        {
            return;
        }

        CancelAutoAdvance();

        if (_isTyping)
        {
            RevealCurrentLine();
        }
        else
        {
            AdvanceDialogue();
        }

        GetViewport().SetInputAsHandled();
    }

    public async void StartDialogue(DialogueSceneDefinition sceneDefinition, CharacterRepository characterRepository)
    {
        _sceneDefinition = sceneDefinition ?? new DialogueSceneDefinition();
        _characterRepository = characterRepository ?? GameServices.Instance?.Characters;
        if (_characterRepository == null)
        {
            GD.PushError("CharacterRepository is not available. Check GameServices autoload.");
            return;
        }

        _lineIndex = -1;
        _isTyping = false;
        _isBusy = false;
        _currentText = string.Empty;
        _visibleCharacters = 0.0;
        _autoAdvanceToken = 0;
        _scheduledAutoAdvanceLineIndex = -1;

        KillTween(ref _advanceTween);
        ResetDialoguePresentationState();

        ApplyLayout(true);
        Visible = true;
        await AdvanceDialogueAsync(skipExitActions: true);
    }

    private async void AdvanceDialogue()
    {
        await AdvanceDialogueAsync();
    }

    private async Task AdvanceDialogueAsync(bool skipExitActions = false)
    {
        if (_isBusy)
        {
            return;
        }

        CancelAutoAdvance();
        _isBusy = true;
        try
        {
            DialogueLineDefinition previousLine = null;
            bool hideForActionTransition = false;

            if (_sceneDefinition != null
                && _lineIndex >= 0
                && _lineIndex < _sceneDefinition.Lines.Count)
            {
                previousLine = _sceneDefinition.Lines[_lineIndex];
                hideForActionTransition = !skipExitActions && HasHideTransitionAction(previousLine.ExitActions);
            }

            if (hideForActionTransition)
            {
                Visible = false;
            }

            if (!skipExitActions && previousLine != null)
            {
                await RunLineExitActionsAsync(previousLine);
            }

            float transitionPause = !skipExitActions && previousLine != null
                ? GetExitTransitionPause(previousLine.ExitActions)
                : 0.0f;
            if (transitionPause > 0.0f)
            {
                await ToSignal(GetTree().CreateTimer(transitionPause), SceneTreeTimer.SignalName.Timeout);
            }

            _lineIndex++;
            if (_sceneDefinition == null || _lineIndex >= _sceneDefinition.Lines.Count)
            {
                FinishDialogue();
                return;
            }

            DialogueLineDefinition line = _sceneDefinition.Lines[_lineIndex];
            Visible = true;
            ApplyLinePresentation(line);

            await RunLineEnterActionsAsync(line);
        }
        finally
        {
            _isBusy = false;
            TryScheduleAutoAdvance();
        }
    }

    private static bool HasHideTransitionAction(System.Collections.Generic.IReadOnlyList<DialogueActorActionDefinition> actions)
    {
        if (actions == null || actions.Count == 0)
        {
            return false;
        }

        foreach (DialogueActorActionDefinition action in actions)
        {
            if (action != null)
            {
                string type = action.Type ?? string.Empty;
                if (string.Equals(type, "move", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(type, "disappear", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(type, "appear", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(type, "teleport_appear", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(type, "attack", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private float GetExitTransitionPause(System.Collections.Generic.IReadOnlyList<DialogueActorActionDefinition> actions)
    {
        if (actions == null || actions.Count == 0)
        {
            return 0.0f;
        }

        foreach (DialogueActorActionDefinition action in actions)
        {
            if (action == null)
            {
                continue;
            }

            if (string.Equals(action.Type, "disappear", StringComparison.OrdinalIgnoreCase))
            {
                return Mathf.Max(0.0f, TransitionPauseAfterDisappear);
            }
        }

        return HasMoveAction(actions)
            ? Mathf.Max(0.0f, TransitionPauseAfterMovement)
            : 0.0f;
    }

    private static bool HasMoveAction(System.Collections.Generic.IReadOnlyList<DialogueActorActionDefinition> actions)
    {
        if (actions == null || actions.Count == 0)
        {
            return false;
        }

        foreach (DialogueActorActionDefinition action in actions)
        {
            if (action != null && string.Equals(action.Type, "move", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private Task RunLineEnterActionsAsync(DialogueLineDefinition line)
    {
        return _actionRunner?.RunLineEnterActionsAsync(line) ?? Task.CompletedTask;
    }

    private Task RunLineExitActionsAsync(DialogueLineDefinition line)
    {
        return _actionRunner?.RunLineExitActionsAsync(line) ?? Task.CompletedTask;
    }

    private void SetCurrentSpeakerPortrait(string speakerId, string expression)
    {
        Texture2D portraitTexture = _characterRepository?.GetPortrait(speakerId, expression);
        if (portraitTexture == null)
        {
            _portrait.Texture = null;
            _portrait.Visible = false;
            return;
        }

        _portrait.Texture = portraitTexture;
        float portraitScale = _characterRepository?.GetPortraitScale(speakerId) ?? 0.34f;
        _portraitBaseScale = Vector2.One * portraitScale;
        _portrait.Scale = _portraitBaseScale;
        _portrait.Modulate = Colors.White;
        _portrait.Visible = true;
    }

    private void FinishTyping()
    {
        if (HandleBubbleFinishTyping())
        {
            return;
        }

        _isTyping = false;
        _dialogueText.VisibleCharacters = -1;

        if (ShouldAutoAdvance(GetCurrentLine()))
        {
            KillTween(ref _advanceTween);
            _advanceIndicator.Visible = false;
            _advanceIndicator.Position = _advanceIndicatorRestPosition;
            TryScheduleAutoAdvance();
            return;
        }

        _advanceIndicator.Visible = true;
        AnimateAdvanceIndicator();
    }

    private void RevealCurrentLine()
    {
        if (HandleBubbleRevealCurrentLine())
        {
            return;
        }

        _dialogueText.VisibleCharacters = -1;
        FinishTyping();
    }

    private void FinishDialogue()
    {
        CancelAutoAdvance();
        KillTween(ref _advanceTween);
        ResetDialoguePresentationState();
        Visible = false;
        _sceneDefinition = null;
        EmitSignal(SignalName.DialogueFinished);
    }

    private DialogueLineDefinition GetCurrentLine()
    {
        if (_sceneDefinition == null || _lineIndex < 0 || _lineIndex >= _sceneDefinition.Lines.Count)
        {
            return null;
        }

        return _sceneDefinition.Lines[_lineIndex];
    }

    private static bool ShouldAutoAdvance(DialogueLineDefinition line)
    {
        return line != null && line.AutoAdvance;
    }

    private void CancelAutoAdvance()
    {
        _autoAdvanceToken++;
        _scheduledAutoAdvanceLineIndex = -1;
    }

    private void TryScheduleAutoAdvance()
    {
        DialogueLineDefinition line = GetCurrentLine();
        if (!ShouldAutoAdvance(line) || _isTyping || _isBusy || !Visible)
        {
            return;
        }

        if (_scheduledAutoAdvanceLineIndex == _lineIndex)
        {
            return;
        }

        _scheduledAutoAdvanceLineIndex = _lineIndex;
        int token = ++_autoAdvanceToken;
        ScheduleAutoAdvanceAsync(token, _lineIndex, Mathf.Max(0.0f, line.AutoAdvanceDelay));
    }

    private async void ScheduleAutoAdvanceAsync(int token, int lineIndex, float delay)
    {
        if (delay > 0.0f)
        {
            await ToSignal(GetTree().CreateTimer(delay), SceneTreeTimer.SignalName.Timeout);
        }

        if (token != _autoAdvanceToken
            || _scheduledAutoAdvanceLineIndex != lineIndex
            || _sceneDefinition == null
            || _lineIndex != lineIndex
            || _isTyping
            || _isBusy
            || !Visible)
        {
            return;
        }

        _scheduledAutoAdvanceLineIndex = -1;
        await AdvanceDialogueAsync();
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
        _backdrop.Position = Vector2.Zero;
        _backdrop.Size = viewportSize;

        float outerMargin = Mathf.Clamp(viewportSize.X * 0.022f, 18.0f, 28.0f);
        float portraitGap = 20.0f;
        float bottomMargin = Mathf.Clamp(viewportSize.Y * 0.045f, 18.0f, 34.0f);
        float boxHeight = Mathf.Clamp(viewportSize.Y * 0.24f, 138.0f, 178.0f);

        float portraitWidth = PreparePortraitForLayout(viewportSize);
        float portraitHeight = GetPortraitHeight();
        if (_portrait.Visible)
        {
            _portrait.Position = new Vector2(
                outerMargin + portraitWidth * 0.5f,
                viewportSize.Y - bottomMargin - portraitHeight * 0.5f);
        }

        float boxLeft = outerMargin + (_portrait.Visible ? portraitWidth + portraitGap : 0.0f);
        float boxRight = viewportSize.X - outerMargin;
        float boxWidth = Mathf.Max(viewportSize.X * 0.40f, boxRight - boxLeft);
        float boxTop = viewportSize.Y - boxHeight - bottomMargin;

        _dialogueBox.Position = new Vector2(boxLeft, boxTop);
        _dialogueBox.Size = new Vector2(boxWidth, boxHeight);

        float namePlateWidth = Mathf.Clamp(boxWidth * 0.18f, 110.0f, 160.0f);
        float namePlateHeight = 40.0f;
        _namePlate.Position = new Vector2(boxLeft + 18.0f, boxTop - namePlateHeight - 8.0f);
        _namePlate.Size = new Vector2(namePlateWidth, namePlateHeight);

        _speakerName.Position = _namePlate.Position + new Vector2(12.0f, 3.0f);
        _speakerName.Size = _namePlate.Size - new Vector2(24.0f, 6.0f);

        _dialogueText.Position = new Vector2(boxLeft + 22.0f, boxTop + 18.0f);
        _dialogueText.Size = new Vector2(boxWidth - 44.0f, boxHeight - 40.0f);

        _advanceIndicator.Size = new Vector2(28.0f, 28.0f);
        _advanceIndicatorRestPosition = new Vector2(boxRight - 34.0f, boxTop + boxHeight - 30.0f);
        _advanceIndicator.Position = _advanceIndicatorRestPosition;

        if (_advanceIndicator.Visible)
        {
            AnimateAdvanceIndicator();
        }
    }

    private float PreparePortraitForLayout(Vector2 viewportSize)
    {
        if (!_portrait.Visible || _portrait.Texture == null)
        {
            return 0.0f;
        }

        Vector2 textureSize = _portrait.Texture.GetSize();
        float preferredScale = _portraitBaseScale.X;
        float maxWidthScale = (viewportSize.X * 0.20f) / textureSize.X;
        float maxHeightScale = (viewportSize.Y * 0.66f) / textureSize.Y;
        float fittedScale = Mathf.Min(preferredScale, Mathf.Min(maxWidthScale, maxHeightScale));
        fittedScale = Mathf.Max(0.05f, fittedScale);

        _portrait.Scale = Vector2.One * fittedScale;
        return textureSize.X * fittedScale;
    }

    private float GetPortraitHeight()
    {
        return !_portrait.Visible || _portrait.Texture == null ? 0.0f : _portrait.Texture.GetSize().Y * _portrait.Scale.Y;
    }

    private string GetDisplayName(string speakerId)
    {
        if (_sceneDefinition?.Cast != null
            && _sceneDefinition.Cast.TryGetValue(speakerId, out DialogueCastEntry castEntry)
            && !string.IsNullOrWhiteSpace(castEntry.DisplayName))
        {
            return castEntry.DisplayName;
        }

        return _characterRepository?.GetDisplayName(speakerId) ?? speakerId;
    }

    private void PlayIntroReveal()
    {
        _backdrop.Color = new Color(0, 0, 0, 0.0f);
        _dialogueBox.Modulate = new Color(1, 1, 1, 0.0f);
        _namePlate.Modulate = new Color(1, 1, 1, 0.0f);
        _portrait.Modulate = new Color(1, 1, 1, 0.0f);
        _speakerName.Modulate = new Color(1, 1, 1, 0.0f);
        _dialogueText.Modulate = new Color(1, 1, 1, 0.0f);
        _advanceIndicator.Modulate = new Color(1, 1, 1, 0.0f);

        Tween tween = CreateTween();
        tween.SetParallel(true);
        tween.TweenProperty(_backdrop, "color", new Color(0, 0, 0, 0.06f), 0.18f);
        tween.TweenProperty(_dialogueBox, "modulate", Colors.White, 0.20f);
        tween.TweenProperty(_namePlate, "modulate", Colors.White, 0.18f);
        tween.TweenProperty(_portrait, "modulate", Colors.White, 0.22f);
        tween.TweenProperty(_speakerName, "modulate", Colors.White, 0.16f);
        tween.TweenProperty(_dialogueText, "modulate", Colors.White, 0.16f);
        tween.TweenProperty(_advanceIndicator, "modulate", Colors.White, 0.16f);
    }

    private void AnimateAdvanceIndicator()
    {
        KillTween(ref _advanceTween);
        _advanceIndicator.Position = _advanceIndicatorRestPosition;
        _advanceTween = CreateTween();
        _advanceTween.SetLoops();
        _advanceTween.TweenProperty(_advanceIndicator, "position:y", _advanceIndicatorRestPosition.Y - 6.0f, 0.46f)
            .SetEase(Tween.EaseType.InOut)
            .SetTrans(Tween.TransitionType.Sine);
        _advanceTween.TweenProperty(_advanceIndicator, "position:y", _advanceIndicatorRestPosition.Y, 0.46f)
            .SetEase(Tween.EaseType.InOut)
            .SetTrans(Tween.TransitionType.Sine);
    }

    private void KillTween(ref Tween tween)
    {
        if (tween != null && GodotObject.IsInstanceValid(tween))
        {
            tween.Kill();
        }

        tween = null;
    }

    private void ConfigureLabelBehaviors()
    {
        _speakerName.HorizontalAlignment = HorizontalAlignment.Center;
        _speakerName.VerticalAlignment = VerticalAlignment.Center;
        _dialogueText.HorizontalAlignment = HorizontalAlignment.Left;
        _dialogueText.VerticalAlignment = VerticalAlignment.Top;
        _dialogueText.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        _advanceIndicator.HorizontalAlignment = HorizontalAlignment.Center;
        _advanceIndicator.VerticalAlignment = VerticalAlignment.Center;
    }

    private void ConfigurePanels()
    {
        StyleBoxFlat boxStyle = new StyleBoxFlat();
        boxStyle.BgColor = new Color(0.05f, 0.03f, 0.05f, 0.88f);
        boxStyle.BorderColor = new Color(0.89f, 0.78f, 0.58f, 0.95f);
        boxStyle.BorderWidthLeft = 2;
        boxStyle.BorderWidthTop = 2;
        boxStyle.BorderWidthRight = 2;
        boxStyle.BorderWidthBottom = 2;
        boxStyle.CornerRadiusTopLeft = 10;
        boxStyle.CornerRadiusTopRight = 10;
        boxStyle.CornerRadiusBottomRight = 10;
        boxStyle.CornerRadiusBottomLeft = 10;
        _dialogueBox.AddThemeStyleboxOverride("panel", boxStyle);

        StyleBoxFlat nameStyle = new StyleBoxFlat();
        nameStyle.BgColor = new Color(0.34f, 0.12f, 0.14f, 0.95f);
        nameStyle.BorderColor = new Color(0.96f, 0.82f, 0.56f, 0.95f);
        nameStyle.BorderWidthLeft = 2;
        nameStyle.BorderWidthTop = 2;
        nameStyle.BorderWidthRight = 2;
        nameStyle.BorderWidthBottom = 2;
        nameStyle.CornerRadiusTopLeft = 8;
        nameStyle.CornerRadiusTopRight = 8;
        nameStyle.CornerRadiusBottomRight = 8;
        nameStyle.CornerRadiusBottomLeft = 8;
        _namePlate.AddThemeStyleboxOverride("panel", nameStyle);
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

        _speakerName.LabelSettings = new LabelSettings
        {
            Font = systemFont,
            FontSize = 24,
            FontColor = new Color(1.0f, 0.95f, 0.84f, 1.0f),
            OutlineColor = new Color(0.12f, 0.04f, 0.02f, 0.92f),
            OutlineSize = 5
        };

        _dialogueText.LabelSettings = new LabelSettings
        {
            Font = systemFont,
            FontSize = 22,
            FontColor = new Color(0.98f, 0.98f, 0.98f, 1.0f),
            OutlineColor = new Color(0.04f, 0.04f, 0.04f, 0.85f),
            OutlineSize = 5
        };

        _advanceIndicator.LabelSettings = new LabelSettings
        {
            Font = systemFont,
            FontSize = 20,
            FontColor = new Color(1.0f, 0.92f, 0.76f, 1.0f),
            OutlineColor = new Color(0.08f, 0.04f, 0.03f, 0.90f),
            OutlineSize = 4
        };
    }
}

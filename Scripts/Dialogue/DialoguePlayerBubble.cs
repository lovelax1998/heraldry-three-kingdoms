using Godot;
using System;

public partial class DialoguePlayer
{
    private BubbleDialogueLayer _bubbleDialogueLayer;
    private string _currentDialogType = "box";
    private bool _boxRevealPlayed;

    private void InitializeBubblePresentation()
    {
        _bubbleDialogueLayer = GetNodeOrNull<BubbleDialogueLayer>("BubbleDialogueLayer");
        _currentDialogType = "box";
        _boxRevealPlayed = false;
        SetProcessInput(true);
    }

    public override void _Input(InputEvent @event)
    {
        TryHandleAdvanceInput(@event);
    }

    private bool TryHandleAdvanceInput(InputEvent @event)
    {
        if (!Visible || _isBusy || _sceneDefinition == null || _sceneDefinition.Lines.Count == 0)
        {
            return false;
        }

        if (@event is InputEventKey keyEvent && keyEvent.Echo)
        {
            return false;
        }

        bool isAdvanceInput = @event.IsActionPressed("ui_accept")
            || (@event is InputEventMouseButton mouseButton
                && mouseButton.Pressed
                && mouseButton.ButtonIndex == MouseButton.Left);

        if (!isAdvanceInput)
        {
            return false;
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
        return true;
    }

    private void ResetDialoguePresentationState()
    {
        _currentDialogType = "box";
        _boxRevealPlayed = false;
        _bubbleDialogueLayer?.HideAll();
        HideBoxPresentation();
    }

    private void ApplyLinePresentation(DialogueLineDefinition line)
    {
        KillTween(ref _advanceTween);
        _advanceIndicator.Visible = false;
        _advanceIndicator.Position = _advanceIndicatorRestPosition;

        if (IsBubbleDialogType(line))
        {
            ApplyBubbleLine(line);
        }
        else
        {
            ApplyBoxLine(line);
        }
    }

    private void ApplyBoxLine(DialogueLineDefinition line)
    {
        _currentDialogType = "box";
        _bubbleDialogueLayer?.HideAll();

        SetCurrentSpeakerPortrait(line.Speaker, line.Expression);
        _speakerName.Text = GetDisplayName(line.Speaker);
        _currentText = line.Text ?? string.Empty;
        _dialogueText.Text = _currentText;
        _dialogueText.VisibleCharacters = 0;
        _visibleCharacters = 0.0;
        _isTyping = _currentText.Length > 0;

        ShowBoxPresentationImmediate();
        ApplyLayout(true);

        if (!_boxRevealPlayed)
        {
            PlayIntroReveal();
            _boxRevealPlayed = true;
        }

        if (!_isTyping)
        {
            FinishTyping();
        }
    }

    private void ApplyBubbleLine(DialogueLineDefinition line)
    {
        _currentDialogType = "bubble";
        _currentText = string.Empty;
        _visibleCharacters = 0.0;
        _speakerName.Text = string.Empty;
        _dialogueText.Text = string.Empty;
        _dialogueText.VisibleCharacters = -1;
        _portrait.Texture = null;

        HideBoxPresentation();
        _bubbleDialogueLayer?.ShowLine(
            line,
            _characterRepository,
            GetDisplayName,
            ResolveDialogueActorNode,
            TypewriterSpeed);
        _isTyping = _bubbleDialogueLayer?.IsTyping ?? false;

        if (!_isTyping)
        {
            FinishTyping();
        }
    }

    private Node2D ResolveDialogueActorNode(string actorId)
    {
        if (_actionRunner is IDialogueActorLocator actorLocator)
        {
            return actorLocator.ResolveDialogueActorNode(actorId);
        }

        return null;
    }

    private bool HandleBubbleTypingProgress()
    {
        if (!_isTyping || !IsBubbleDialogActive())
        {
            return false;
        }

        if (!(_bubbleDialogueLayer?.IsTyping ?? false))
        {
            FinishTyping();
        }

        return true;
    }

    private bool HandleBubbleFinishTyping()
    {
        if (!IsBubbleDialogActive())
        {
            return false;
        }

        _isTyping = false;
        _dialogueText.VisibleCharacters = -1;
        _bubbleDialogueLayer?.RevealAll();

        if (ShouldAutoAdvance(GetCurrentLine()))
        {
            KillTween(ref _advanceTween);
            _advanceIndicator.Visible = false;
            _advanceIndicator.Position = _advanceIndicatorRestPosition;
            TryScheduleAutoAdvance();
            return true;
        }

        _advanceIndicator.Visible = false;
        return true;
    }

    private bool HandleBubbleRevealCurrentLine()
    {
        if (!IsBubbleDialogActive())
        {
            return false;
        }

        _bubbleDialogueLayer?.RevealAll();
        FinishTyping();
        return true;
    }

    private bool IsBubbleDialogActive()
    {
        return string.Equals(_currentDialogType, "bubble", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsBubbleDialogType(DialogueLineDefinition line)
    {
        return string.Equals(line?.DialogType ?? "box", "bubble", StringComparison.OrdinalIgnoreCase);
    }

    private void ShowBoxPresentationImmediate()
    {
        _backdrop.Visible = true;
        _dialogueBox.Visible = true;
        _namePlate.Visible = true;
        _speakerName.Visible = true;
        _dialogueText.Visible = true;
        _advanceIndicator.Visible = false;

        _backdrop.Color = new Color(0, 0, 0, 0.06f);
        _backdrop.Modulate = Colors.White;
        _dialogueBox.Modulate = Colors.White;
        _namePlate.Modulate = Colors.White;
        if (_portrait.Visible)
        {
            _portrait.Modulate = Colors.White;
        }
        _speakerName.Modulate = Colors.White;
        _dialogueText.Modulate = Colors.White;
        _advanceIndicator.Modulate = Colors.White;
    }

    private void HideBoxPresentation()
    {
        KillTween(ref _advanceTween);
        _backdrop.Visible = false;
        _dialogueBox.Visible = false;
        _namePlate.Visible = false;
        _speakerName.Visible = false;
        _dialogueText.Visible = false;
        _portrait.Visible = false;
        _advanceIndicator.Visible = false;
    }
}

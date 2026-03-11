using Godot;
using System;
using System.Collections.Generic;

public partial class BubbleDialogueLayer : Control
{
    private const string SpeechBubbleScenePath = "res://Scene/UI/SpeechBubble.tscn";

    [Export] public float DefaultBubbleWidth { get; set; } = 280.0f;
    [Export] public Vector2 DefaultBubbleOffset { get; set; } = new Vector2(0.0f, -92.0f);
    [Export] public float SimultaneousStackSpacing { get; set; } = 20.0f;
    [Export] public float FallbackHorizontalSpread { get; set; } = 220.0f;

    private readonly List<SpeechBubble> _activeBubbles = new();
    private PackedScene _speechBubbleScene;

    public bool IsTyping
    {
        get
        {
            foreach (SpeechBubble bubble in _activeBubbles)
            {
                if (bubble != null && GodotObject.IsInstanceValid(bubble) && bubble.IsTyping)
                {
                    return true;
                }
            }

            return false;
        }
    }

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        SetAnchorsPreset(LayoutPreset.FullRect);
        _speechBubbleScene = ResourceLoader.Load<PackedScene>(SpeechBubbleScenePath);
        Visible = false;
    }

    public void ShowLine(
        DialogueLineDefinition line,
        CharacterRepository characterRepository,
        Func<string, string> displayNameResolver,
        Func<string, Node2D> actorResolver,
        float typewriterSpeed)
    {
        HideAll();

        List<DialogueBubbleEntryDefinition> entries = BuildEntries(line);
        if (entries.Count == 0)
        {
            Visible = false;
            return;
        }

        if (_speechBubbleScene == null)
        {
            GD.PushWarning($"Failed to load speech bubble scene: {SpeechBubbleScenePath}");
            return;
        }

        Vector2 viewportCenter = GetViewport().GetVisibleRect().Size * 0.5f;
        for (int index = 0; index < entries.Count; index++)
        {
            DialogueBubbleEntryDefinition entry = entries[index];
            SpeechBubble bubble = _speechBubbleScene.Instantiate<SpeechBubble>();
            if (bubble == null)
            {
                continue;
            }

            AddChild(bubble);
            _activeBubbles.Add(bubble);

            Node2D actorNode = actorResolver?.Invoke(entry.Speaker);
            bool showSpeakerName = entry.ShowSpeakerName ?? line?.ShowSpeakerName ?? true;
            string speakerName = showSpeakerName
                ? (displayNameResolver?.Invoke(entry.Speaker)
                    ?? characterRepository?.GetDisplayName(entry.Speaker)
                    ?? entry.Speaker)
                : string.Empty;

            float lineOffsetX = line?.BubbleOffsetX ?? DefaultBubbleOffset.X;
            float lineOffsetY = line?.BubbleOffsetY ?? DefaultBubbleOffset.Y;
            float entryOffsetX = entry?.BubbleOffsetX ?? 0.0f;
            float entryOffsetY = entry?.BubbleOffsetY ?? 0.0f;
            Vector2 bubbleOffset = new Vector2(
                lineOffsetX + entryOffsetX,
                lineOffsetY + entryOffsetY - index * SimultaneousStackSpacing);

            float bubbleWidth = Mathf.Clamp(entry?.BubbleWidth ?? line?.BubbleWidth ?? DefaultBubbleWidth, 180.0f, 360.0f);
            float spreadIndex = index - (entries.Count - 1) * 0.5f;
            Vector2 fallbackAnchor = viewportCenter + new Vector2(spreadIndex * FallbackHorizontalSpread, 0.0f);

            bubble.Setup(
                actorNode,
                speakerName,
                entry?.Text ?? string.Empty,
                typewriterSpeed,
                bubbleOffset,
                bubbleWidth,
                showSpeakerName,
                fallbackAnchor);
        }

        Visible = _activeBubbles.Count > 0;
    }

    public void RevealAll()
    {
        foreach (SpeechBubble bubble in _activeBubbles)
        {
            if (bubble != null && GodotObject.IsInstanceValid(bubble))
            {
                bubble.RevealAll();
            }
        }
    }

    public void HideAll()
    {
        foreach (SpeechBubble bubble in _activeBubbles)
        {
            if (bubble != null && GodotObject.IsInstanceValid(bubble))
            {
                bubble.QueueFree();
            }
        }

        _activeBubbles.Clear();
        Visible = false;
    }

    private static List<DialogueBubbleEntryDefinition> BuildEntries(DialogueLineDefinition line)
    {
        List<DialogueBubbleEntryDefinition> entries = new();
        if (line == null)
        {
            return entries;
        }

        if (line.Entries != null && line.Entries.Count > 0)
        {
            foreach (DialogueBubbleEntryDefinition entry in line.Entries)
            {
                if (entry == null || string.IsNullOrWhiteSpace(entry.Text))
                {
                    continue;
                }

                entries.Add(entry);
            }

            return entries;
        }

        if (!string.IsNullOrWhiteSpace(line.Text))
        {
            entries.Add(new DialogueBubbleEntryDefinition
            {
                Speaker = line.Speaker,
                Expression = line.Expression,
                Text = line.Text,
                BubbleWidth = line.BubbleWidth,
                BubbleOffsetX = line.BubbleOffsetX,
                BubbleOffsetY = line.BubbleOffsetY,
                ShowSpeakerName = line.ShowSpeakerName
            });
        }

        return entries;
    }
}
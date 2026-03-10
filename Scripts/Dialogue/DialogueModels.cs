using System.Collections.Generic;
using System.Text.Json.Serialization;

public sealed class DialogueSceneDefinition
{
    [JsonPropertyName("scene_id")]
    public string SceneId { get; set; } = string.Empty;

    [JsonPropertyName("background")]
    public string Background { get; set; } = string.Empty;

    [JsonPropertyName("bgm")]
    public string Bgm { get; set; } = string.Empty;

    [JsonPropertyName("dialog_box")]
    public string DialogueBox { get; set; } = string.Empty;

    [JsonPropertyName("world_intro")]
    public DialogueWorldIntroDefinition WorldIntro { get; set; } = new();

    [JsonPropertyName("intro_monologue")]
    public List<string> IntroMonologue { get; set; } = new();

    [JsonPropertyName("intro_monologue_speaker")]
    public string IntroMonologueSpeaker { get; set; } = "caocao";

    [JsonPropertyName("intro_monologue_expression")]
    public string IntroMonologueExpression { get; set; } = "normal";

    [JsonPropertyName("intro_stand_duration")]
    public float IntroStandDuration { get; set; } = 2.0f;

    [JsonPropertyName("intro_pace_steps")]
    public int IntroPaceSteps { get; set; } = 2;

    [JsonPropertyName("intro_pause_after_pacing")]
    public float IntroPauseAfterPacing { get; set; } = 0.18f;

    [JsonPropertyName("chapter_number")]
    public string ChapterNumber { get; set; } = string.Empty;

    [JsonPropertyName("chapter_title")]
    public string ChapterTitle { get; set; } = string.Empty;

    [JsonPropertyName("location")]
    public string Location { get; set; } = string.Empty;

    [JsonPropertyName("environment")]
    public string Environment { get; set; } = string.Empty;

    [JsonPropertyName("cast")]
    public Dictionary<string, DialogueCastEntry> Cast { get; set; } = new();

    [JsonPropertyName("lines")]
    public List<DialogueLineDefinition> Lines { get; set; } = new();
}

public sealed class DialogueWorldIntroDefinition
{
    [JsonPropertyName("duration")]
    public float Duration { get; set; } = 1.4f;

    [JsonPropertyName("settle_duration")]
    public float SettleDuration { get; set; } = 0.22f;

    [JsonPropertyName("dialogue_delay")]
    public float DialogueDelay { get; set; } = 0.18f;

    [JsonPropertyName("actors")]
    public List<DialogueWorldActorDefinition> Actors { get; set; } = new();
}

public sealed class DialogueWorldActorDefinition
{
    [JsonPropertyName("node")]
    public string Node { get; set; } = string.Empty;

    [JsonPropertyName("character")]
    public string Character { get; set; } = string.Empty;

    [JsonPropertyName("animation")]
    public string Animation { get; set; } = "walk_side";

    [JsonPropertyName("idle_animation")]
    public string IdleAnimation { get; set; } = "idle_side";

    [JsonPropertyName("facing")]
    public string Facing { get; set; } = "right";

    [JsonPropertyName("start_x")]
    public float StartX { get; set; }

    [JsonPropertyName("start_y")]
    public float StartY { get; set; }

    [JsonPropertyName("target_x")]
    public float TargetX { get; set; }

    [JsonPropertyName("target_y")]
    public float TargetY { get; set; }
}

public sealed class DialogueCastEntry
{
    [JsonPropertyName("side")]
    public string Side { get; set; } = "left";

    [JsonPropertyName("display_name")]
    public string DisplayName { get; set; } = string.Empty;
}

public sealed class DialogueLineDefinition
{
    [JsonPropertyName("speaker")]
    public string Speaker { get; set; } = string.Empty;

    [JsonPropertyName("expression")]
    public string Expression { get; set; } = "normal";

    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;

    [JsonPropertyName("auto_advance")]
    public bool AutoAdvance { get; set; }

    [JsonPropertyName("auto_advance_delay")]
    public float AutoAdvanceDelay { get; set; } = 0.48f;

    [JsonPropertyName("enter_actions")]
    public List<DialogueActorActionDefinition> EnterActions { get; set; } = new();

    [JsonPropertyName("exit_actions")]
    public List<DialogueActorActionDefinition> ExitActions { get; set; } = new();
}

public sealed class DialogueActorActionDefinition
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("target")]
    public string Target { get; set; } = string.Empty;

    [JsonPropertyName("facing")]
    public string Facing { get; set; } = string.Empty;

    [JsonPropertyName("x")]
    public float? X { get; set; }

    [JsonPropertyName("y")]
    public float? Y { get; set; }

    [JsonPropertyName("offset_x")]
    public float? OffsetX { get; set; }

    [JsonPropertyName("offset_y")]
    public float? OffsetY { get; set; }

    [JsonPropertyName("duration")]
    public float Duration { get; set; } = 0.32f;

    [JsonPropertyName("resource")]
    public string Resource { get; set; } = string.Empty;

    [JsonPropertyName("scale")]
    public float? Scale { get; set; }

    [JsonPropertyName("fps")]
    public float? FramesPerSecond { get; set; }

    [JsonPropertyName("start_frame")]
    public int? StartFrame { get; set; }

    [JsonPropertyName("end_frame")]
    public int? EndFrame { get; set; }

    [JsonPropertyName("sheet_columns")]
    public int? SheetColumns { get; set; }

    [JsonPropertyName("sheet_rows")]
    public int? SheetRows { get; set; }

    [JsonPropertyName("sheet_outer_padding")]
    public int? SheetOuterPadding { get; set; }

    [JsonPropertyName("frame_spacing")]
    public int? FrameSpacing { get; set; }

    [JsonPropertyName("frame_inset")]
    public int? FrameInset { get; set; }
}

public sealed class DialogueCharacterDefinition
{
    [JsonPropertyName("display_name")]
    public string DisplayName { get; set; } = string.Empty;

    [JsonPropertyName("default_portrait")]
    public string DefaultPortrait { get; set; } = "normal";

    [JsonPropertyName("portrait_scale")]
    public float PortraitScale { get; set; } = 0.42f;

    [JsonPropertyName("s_model_scale")]
    public float SModelScale { get; set; } = 0.78f;

    [JsonPropertyName("portraits")]
    public Dictionary<string, string> Portraits { get; set; } = new();

    [JsonPropertyName("s_model_sheet")]
    public string SModelSheet { get; set; } = string.Empty;
}

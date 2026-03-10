using Godot;
using System.Threading.Tasks;

public partial class GameUi : Node
{
    private const string StoryHeaderScenePath = "res://Scene/UI/StoryHeaderBar.tscn";
    private const string SceneTransitionScenePath = "res://Scene/UI/SceneTransitionLayer.tscn";

    public static GameUi Instance { get; private set; }

    private StoryHeaderBar _storyHeaderBar;
    private SceneTransitionLayer _sceneTransitionLayer;
    private bool _isSceneTransitioning;

    public bool IsSceneTransitioning => _isSceneTransitioning;

    public override void _EnterTree()
    {
        if (Instance != null && Instance != this)
        {
            GD.PushWarning("Duplicate GameUi instance detected.");
        }

        Instance = this;
    }

    public override void _Ready()
    {
        EnsureStoryHeaderBar();
        EnsureSceneTransitionLayer();
        HideStoryHeader();
        _sceneTransitionLayer?.HideImmediately();
    }

    public override void _ExitTree()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void ShowStoryHeader(string chapterText, string locationText, bool fadeIn = true)
    {
        StoryHeaderBar headerBar = EnsureStoryHeaderBar();
        if (headerBar == null)
        {
            return;
        }

        headerBar.SetContext(chapterText, locationText, fadeIn);
    }

    public void SetStoryHeaderContext(string chapterText, string locationText)
    {
        StoryHeaderBar headerBar = EnsureStoryHeaderBar();
        if (headerBar == null)
        {
            return;
        }

        headerBar.SetContext(chapterText, locationText, fadeIn: false);
    }

    public void HideStoryHeader()
    {
        StoryHeaderBar headerBar = EnsureStoryHeaderBar();
        headerBar?.HideBar();
    }

    public async Task<Error> TransitionToSceneAsync(string scenePath, string titleText = "", string subtitleText = "")
    {
        if (string.IsNullOrWhiteSpace(scenePath))
        {
            return Error.InvalidParameter;
        }

        if (_isSceneTransitioning)
        {
            return Error.Failed;
        }

        SceneTransitionLayer transitionLayer = EnsureSceneTransitionLayer();
        if (transitionLayer == null)
        {
            return Error.CantOpen;
        }

        _isSceneTransitioning = true;
        HideStoryHeader();

        Error result = Error.Ok;
        try
        {
            await transitionLayer.PlayCoverAsync(titleText, subtitleText);

            result = GetTree().ChangeSceneToFile(scenePath);
            if (result != Error.Ok)
            {
                GD.PushWarning($"Failed to change scene to: {scenePath} ({result})");
                await transitionLayer.PlayRevealAsync();
                return result;
            }

            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            await transitionLayer.PlayRevealAsync();
            return Error.Ok;
        }
        finally
        {
            _isSceneTransitioning = false;
        }
    }

    private StoryHeaderBar EnsureStoryHeaderBar()
    {
        if (IsInstanceValid(_storyHeaderBar))
        {
            return _storyHeaderBar;
        }

        PackedScene scene = ResourceLoader.Load<PackedScene>(StoryHeaderScenePath);
        if (scene == null)
        {
            GD.PushError($"Failed to load story header scene: {StoryHeaderScenePath}");
            return null;
        }

        _storyHeaderBar = scene.Instantiate<StoryHeaderBar>();
        if (_storyHeaderBar == null)
        {
            GD.PushError("Failed to instantiate StoryHeaderBar.");
            return null;
        }

        _storyHeaderBar.Name = "StoryHeaderBar";
        AddChild(_storyHeaderBar);
        _storyHeaderBar.HideBar();
        return _storyHeaderBar;
    }

    private SceneTransitionLayer EnsureSceneTransitionLayer()
    {
        if (IsInstanceValid(_sceneTransitionLayer))
        {
            return _sceneTransitionLayer;
        }

        PackedScene scene = ResourceLoader.Load<PackedScene>(SceneTransitionScenePath);
        if (scene == null)
        {
            GD.PushError($"Failed to load scene transition layer: {SceneTransitionScenePath}");
            return null;
        }

        _sceneTransitionLayer = scene.Instantiate<SceneTransitionLayer>();
        if (_sceneTransitionLayer == null)
        {
            GD.PushError("Failed to instantiate SceneTransitionLayer.");
            return null;
        }

        _sceneTransitionLayer.Name = "SceneTransitionLayer";
        AddChild(_sceneTransitionLayer);
        _sceneTransitionLayer.HideImmediately();
        return _sceneTransitionLayer;
    }
}
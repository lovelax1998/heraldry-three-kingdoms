using System.Threading.Tasks;

public interface IDialogueActionRunner
{
    Task RunLineEnterActionsAsync(DialogueLineDefinition line);
    Task RunLineExitActionsAsync(DialogueLineDefinition line);
}
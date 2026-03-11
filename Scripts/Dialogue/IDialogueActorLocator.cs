using Godot;

public interface IDialogueActorLocator
{
    Node2D ResolveDialogueActorNode(string actorId);
}
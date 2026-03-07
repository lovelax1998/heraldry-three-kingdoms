using Godot;
using System;

public partial class NodeLearn : Node2D
{
	[Export]
	public Node inputNode;

	[Export]
	public Node newParent;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		GetParent().CallDeferred(Node.MethodName.Reparent, newParent);
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}

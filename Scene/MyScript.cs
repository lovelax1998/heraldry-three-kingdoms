using Godot;
using System;

public partial class MyScript : TextureRect
{
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		if (Input.IsKeyPressed(Key.Space))
		{
			// GD.Print("Space");
		}
	}

	public override void _ExitTree()
	{
		base._ExitTree();
		GD.Print("quit");
	}

	public override void _Input(InputEvent @event)
	{
		if (@event is InputEventKey)
		{
			var key = @event as InputEventKey;
			if (key.Keycode == Key.Space)
			{
				if (key.IsPressed() && !key.IsEcho())
				{
					GD.Print("space pressed");
				}
			}
		}
	}
}

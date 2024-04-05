using Godot;
using System;
using Functory.Lang;
using System.Collections.Generic;

public partial class root : Control
{
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		string[] parameters = new string[] {"a", "b"};
		
		Dictionary<string, Application> namedParams = new Dictionary<string, Application>();
		namedParams.Add("a", new BindingOf("a"));
		namedParams.Add("b", new BindingOf("b"));
		
		Application[] positionalParams = new Application[0];
		
		Application def = new Application(new Sum(), namedParams, positionalParams);
		Function rebindAddition = new Function(new BindingOf("f"), parameters);
		
		GD.Print("rebindAdditon done");
		
		var two = new Application(new IntegerConstructor(2), null, null);
		var three = new Application(new IntegerConstructor(3), null, null);
		
		var bruh = new Dictionary<string,Application>();
		
		Application deez = new Application(rebindAddition,bruh,new Application[]{def, two, three});
		GD.Print(deez.bindings);
		
		GD.Print(Interpreter.eval(deez));
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}

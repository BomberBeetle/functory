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
		
		GD.Print("root: instance def");
		Application def = new Application(new Sum(), namedParams, positionalParams);
		GD.Print("root: def instanced with " + def.bindings.Count + " binds");
		
		Function rebind = new Function(new BindingOf("f"), parameters);
		
		GD.Print("root: Making integerConstructor apps");
		var two = new Application(new IntegerConstructor(2), null, null);
		var three = new Application(new IntegerConstructor(3), null, null);
		
		GD.Print("root: instance rebindening");
		Application theRebindening = new Application(rebind,null,null);	
		GD.Print("root: rebindening instanced with " + theRebindening.bindings.Count + " binds");
		
		GD.Print("root: instance rebindapalooza");
		Function bebinb = new Function(theRebindening, null);
		
		Application rebindapalooza = new Application(bebinb,null, new Application[]{def, three, two});
		GD.Print("root: rbpalooza instanced with " + rebindapalooza.bindings.Count + " binds");
		GD.Print("root: start eval");
		GD.Print(Interpreter.eval(rebindapalooza));
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}

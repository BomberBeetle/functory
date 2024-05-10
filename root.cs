using Godot;
using System;
using Functory.Lang;
using System.Collections.Generic;

public partial class root : Control
{
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{	
		
		Application two = new Application(new IntegerConstructor(2), null, null);
		Application three = new Application(new IntegerConstructor(3), null, null);
		
		Function sum = new Sum();
		
		Function sumRebind = new Function(new Functory.Expression(sum, new Functory.Expression[]{
			new BindingOf("a"),
			new BindingOf("b")
		},null), new string[]{"a", "b"});
		
		Application sdefApp = new Application(sumRebind, null, null);
		
		GD.Print(Interpreter.evalTwo(sdefApp));
		
		string[] parameters = new string[] {"f", "a", "b"};
		Function rebind = new Function(null, parameters, "rebind");

		rebind.def = new Functory.Expression(new BindingOf("f"), new Functory.Expression[]{new BindingOf("a"), new BindingOf("b")}, null);
		//rebind.def = new BindingOf("f", rebind, null, null, new Application[]{new BindingOf("a", rebind), new BindingOf("b", rebind)}); //what the fuck?
		 
		Application rebindApp = new Application(rebind, null, new Application[]{sdefApp, two, three});
		
		GD.Print(Interpreter.evalTwo(rebindApp));

		Function partiallyAppliedBonanza = new Function(new Functory.Expression(new Functory.Expression(rebind, new Functory.Expression[]{
			new(sumRebind, null, null),
			new(new IntegerConstructor(2), null, null)
		},null),new Functory.Expression[]{new BindingOf("a")},null), new string[]{"a"});

		Dictionary<string, Application> shenaniganParams = new Dictionary<string, Application>();
		shenaniganParams.Add("a", three);
		Application shenanigans = new Application(partiallyAppliedBonanza, shenaniganParams, null);

		GD.Print(Interpreter.evalTwo(shenanigans));
		
		
		/*
		recurse.def = new Application(new If(), null, new Application[]{
			new Application(new Equals(), null, new Application[]{new BindingOf("x", recurse), new Application(new IntegerConstructor(1), null, null)}),
			//new Application(new BooleanConstructor(true), null, null),
			new Application(new IntegerConstructor(1),null,null),
			new Application(recurse, null, new Application[]{new Application(new Sum(), null, new Application[]{
				new Application(new IntegerConstructor(-1), null, null),
				new BindingOf("x", recurse)
			})})
		});*/
		
		Function recurse = new Function(null, new string[]{"x"});

		recurse.def = new Functory.Expression(new If(), new Functory.Expression[]{
			new(new Equals(), new Functory.Expression[]{
				new BindingOf("x"),
				new(new IntegerConstructor(1), null, null)
			}, null),
			new(new IntegerConstructor(1), null, null),
			new(recurse, new Functory.Expression[]{
				new(new Sum(),new Functory.Expression[]{
					new BindingOf("x"),
					new(new IntegerConstructor(-1),null,null)
				} ,null)
			}, null)
		}, null);
		
		GD.Print(Interpreter.evalTwo(new Application(recurse, null, new Application[]{new Application(new IntegerConstructor(500), null, null)})));
		
		/* 
		string[] parameters = new string[] {"f", "a", "b"};
		
		Dictionary<string, Application> namedParams = new Dictionary<string, Application>();
		
		
		Application[] positionalParams = new Application[0];
		
		//GD.Print("root: instance def");
		Application def = new Application(new Sum(), null, positionalParams);
		namedParams.Add("a", new BindingOf("a"));
		namedParams.Add("b", new BindingOf("b"));
		def.updateNamedParams(namedParams);
		//GD.Print("root: def instanced with " + def.bindings.Count + " binds");
		
		Function rebind = new Function(new BindingOf("f"), parameters, "rebind");  //f' f a b= f a b
		
		//GD.Print("root: Making integerConstructor apps");
		var two = new Application(new IntegerConstructor(2), null, null);
		var three = new Application(new IntegerConstructor(3), null, null);
		
		//GD.Print("root: instance rebindening");
		Application theRebindening = new Application(rebind,null,null);	
		//GD.Print("root: rebindening instanced with " + theRebindening.bindings.Count + " binds");
		
		//GD.Print("root: instance rebindapalooza");
		Function bebinb = new Function(theRebindening, null,"bebinb");
		
		Application rebindapalooza = new Application(bebinb,null, new Application[]{def, three, two});
		//GD.Print("root: rbpalooza instanced with " + rebindapalooza.bindings.Count + " binds");
		//GD.Print("root: start eval");
		//GD.Print("root: rbplz result = ", Interpreter.eval(rebindapalooza));
		
		
		//GD.Print("root: Instancing application of BooleanConstructor");
		var tru = new Application(new BooleanConstructor(true), null, null);
		//GD.Print("root: instancing if_test");
		Application if_test = new Application(new If(), null, new Application[]{tru, two, rebindapalooza});
		//GD.Print("root: if_test result = " + Interpreter.eval(if_test));
		
		//GD.Print("root: start recurseDef");
		Application recurseDef = new Application(new If(), null, new Application[]{}, false);
		Function recurse = new Function(recurseDef, new string[]{"x"});
		//GD.Print("root: recurse hashcode is " + recurse.GetHashCode());
		Dictionary<string, Application> recurseDic = new Dictionary<string, Application>();
		recurseDic.Add("x", new BindingOf("x"));
		recurseDic.Add("condition", new Application(
			new Equals(), null, new Application[]{new BindingOf("x"), new Application(new IntegerConstructor(1), null, new Application[]{})}
		));
		recurseDic.Add("then", new Application(new IntegerConstructor(1), null, new Application[]{}));
		//GD.Print("root: add recursive param");
		recurseDic.Add("else", new Application(recurse,null,new Application[]{
			new Application(new Sum(), null, new Application[]{new BindingOf("x"), new Application(new IntegerConstructor(-1), null, new Application[]{})})
		}));
		
		recurseDef.updateNamedParams(recurseDic);
		//GD.Print("root: recurseDef bindings: " + recurseDef.bindings.Count);
		//GD.Print("root: recurseDef unassigned binds = " + recurseDef.getUnassignedBindings() );
		Application recurseApp = new Application(recurse, null, new Application[]{if_test});
		////GD.Print("root: recursive eval result is " + Interpreter.eval(recurseApp));
		*/
	}
	
	
	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}

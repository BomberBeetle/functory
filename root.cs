using Godot;
using System;
using Functory.Lang;
using System.Collections.Generic;
using System.Linq;
using System.Dynamic;
using System.Reflection;

public partial class root : Control
{
	HashSet<GraphNode> selectedNodes = new HashSet<GraphNode>();
	Dictionary<ulong, Function> componentMap = new Dictionary<ulong, Function>();
	GraphEdit activeGraph;
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

		Godot.Collections.Array<Godot.Node> graphs = GetTree().GetNodesInGroup("Editors");
		if(graphs.Count != 0){
			activeGraph = (GraphEdit) graphs[0];
		}
		foreach(Node n in graphs){
			GraphEdit graph = (GraphEdit) n;
			graph.ConnectionRequest += (StringName from_node, long from_port, StringName to_node, long to_port) => OnGraphConnectionRequest(from_node, (int)from_port, to_node, (int)to_port, graph); 
			graph.DisconnectionRequest += (StringName from_node, long from_port, StringName to_node, long to_port) => OnGraphDisconnectRequest(from_node, (int)from_port, to_node, (int)to_port, graph);
			graph.NodeSelected += (Node node) => OnNodeSelected((GraphNode) node);
			graph.NodeDeselected += (Node node) => OnNodeUnselected((GraphNode) node);
			graph.DeleteNodesRequest += (Godot.Collections.Array nodes) => OnDeleteNodesRequest(graph);
			graph.GuiInput += (InputEvent evt) => OnGraphGuiInput(evt, graph);
			
		}

		FunctionPackage mathPackage = new FunctionPackage("Matemática", "math");
		mathPackage.functions.Add(new Sum());

		FunctionPackage dataPackage = new FunctionPackage("Dados", "data");
		dataPackage.functions.Add(new IntegerConstructor());
		dataPackage.functions.Add(new BooleanConstructor());

		FunctionPackage logicPackage = new FunctionPackage("Lógica", "logic");
		logicPackage.functions.Add(new Equals());
		logicPackage.functions.Add(new Not());
		logicPackage.functions.Add(new If());

		Tree functionsTree = (Tree) GetNode("HSplitContainer/VBoxContainer2/Tree");

		TreeItem treeRoot = functionsTree.CreateItem(null);
		functionsTree.HideRoot = true;
		functionsTree.ScrollVerticalEnabled = false;
		functionsTree.ScrollHorizontalEnabled = false;

		functionsTree.ItemActivated += () => {
			TreeItem select = functionsTree.GetSelected();
			ulong instanceId = select.GetInstanceId();

			if(componentMap.ContainsKey(instanceId)){

				Function fn = componentMap[instanceId];

				GraphNode funNode = new GraphNode();
				funNode.Title = fn.name;

				BuiltInFunction btFn = fn as BuiltInFunction;
				if(btFn != null){

					Type fnType = fn.GetType();
					foreach(FieldInfo field in fnType.GetFields()){
						GD.Print("evaling property");
						if(Attribute.IsDefined(field, typeof(ConstructorField))){
							LineEdit propertyEditor = new LineEdit();
							propertyEditor.TextChanged += (string text) => {
								bool accept = btFn.UpdateConstructorField(field, text);
								if(accept){
									GD.Print("Property " + field.Name + " of " + fn + "changed to " + text);
									propertyEditor.RemoveThemeColorOverride("font_color");
								}
								else{
									GD.Print("\"" + text + "\" rejected for property " + field.Name + " of function " + fn);
									propertyEditor.AddThemeColorOverride("font_color", new Color(0.75f, 0.25f, 0.1f));
								}
							};
							funNode.AddChild(propertyEditor);
						}
					}
				}
				else{
					GD.Print("not a builtin ");
				}

				int paramsOffset = 0;

				if(fn.parameters.Count() != 0){

					for(int i = paramsOffset; i < fn.parameters.Count(); i++){
						string prm = fn.parameters[i];
						Label prmLabel = new Label();
						prmLabel.Text = prm;
						funNode.AddChild(prmLabel);
						funNode.SetSlotEnabledLeft(i, true);
						funNode.SetSlotEnabledRight(i, false);
					}

					if(paramsOffset == 0){
						funNode.SetSlotEnabledRight(0, true);
					}
				}
				else if(paramsOffset == 0){
					funNode.AddChild(new Control());
					funNode.SetSlotEnabledLeft(0, false);
					funNode.SetSlotEnabledRight(0, true);
				}
				

				activeGraph.AddChild(funNode);

				

				GD.Print("Added function " + fn + " to " + activeGraph.GetPath());

			}
		};

		dataPackage.CreateTree(functionsTree, treeRoot, componentMap);
		mathPackage.CreateTree(functionsTree, treeRoot, componentMap);
		logicPackage.CreateTree(functionsTree, treeRoot, componentMap);

		//LAMBDAS
		//TODO: Fazer a função de adicionar/remover parâmetros em lambdas.
		//TODO: Fazer com que Nodes de parâmetro deletem seu parâmetro correspondente.
		
		//CRIAÇÃO DE NÓS
		//TODO: Fazer a função de adicionar funções criar novas instâncias em vez de usar a mesma (pra não bugar construtores)

		

		//TODO: Toda a interpretação. lol
			
	}
	
	public void OnGraphConnectionRequest(StringName from_node, int from_port, StringName to_node, int to_port, GraphEdit graph)
	{
		var connections = graph.GetConnectionList();
		bool alreadyConnected = connections.Any((Godot.Collections.Dictionary conn) => {return conn["to_port"].As<int>()==to_port && conn["to_node"].As<StringName>()==to_node;});

		if(!alreadyConnected) graph.ConnectNode(from_node, from_port, to_node, to_port);
	}

	public void OnNodeSelected(GraphNode node){
		selectedNodes.Add(node);
		GD.Print(selectedNodes.Count);
	}

	public void OnNodeUnselected(GraphNode node){
		selectedNodes.Remove(node);
	}

	public void OnDeleteNodesRequest(GraphEdit graph){
		List<GraphNode> containedNodes = new List<GraphNode>();
		foreach(GraphNode n in selectedNodes){
			if(graph.GetChildren().Contains(n)){
				
				foreach(Godot.Collections.Dictionary con in graph.GetConnectionList()){
					StringName to_node = con["to"].As<StringName>();
					StringName from_node = con["from"].As<StringName>();
					if(to_node == n.Name || from_node == n.Name){
						int to_port = con["to_port"].As<int>();
						int from_port = con["from_port"].As<int>();
						graph.DisconnectNode(from_node, from_port, to_node, to_port);
					}

					selectedNodes.Remove(n);
				}
			}
			n.QueueFree();
			containedNodes.Add(n);
		}
		selectedNodes.RemoveWhere((GraphNode n) => containedNodes.Contains(n));
	}
	public void OnGraphDisconnectRequest(StringName from_node, int from_port, StringName to_node, int to_port, GraphEdit graph){
		graph.DisconnectNode(from_node, from_port, to_node, to_port);
	}

	public void OnGraphGuiInput(InputEvent evt, GraphEdit graph){
		InputEventMouseButton buttonEvt = evt as InputEventMouseButton;
		if(buttonEvt != null){
			if(buttonEvt.ButtonIndex == MouseButton.Left && buttonEvt.Pressed){
				activeGraph.ShowGrid = false;
				graph.ShowGrid = true;
				activeGraph = graph;
			}
		}
	}
	
	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}




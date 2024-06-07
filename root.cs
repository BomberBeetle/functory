using Godot;
using System;
using Functory.Lang;
using System.Collections.Generic;
using System.Linq;
using System.Dynamic;
using System.Reflection;
using System.Runtime.CompilerServices;

public partial class root : Control
{
	HashSet<GraphNode> selectedNodes = new HashSet<GraphNode>();

	HashSet<ulong> rejectDeletionNodes = new HashSet<ulong>();
	Dictionary<ulong, Function> componentMap = new Dictionary<ulong, Function>();
	Dictionary<ulong, Function> fnInstanceMap = new Dictionary<ulong, Function>();
	GraphEdit activeGraph;

	PackedScene lambdaTemplate;
	PackedScene lambdaParamTemplate;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{	
		
		lambdaTemplate = GD.Load<PackedScene>("res://LambdaNode.tscn");
		lambdaParamTemplate = GD.Load<PackedScene>("res://LambdaParam.tscn");

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


		Button lambdaButton = (Button) GetNode("HSplitContainer/VBoxContainer2/Button");

		lambdaButton.ButtonDown += CreateLambda;

		foreach(Node n in graphs){
			GraphEdit graph = (GraphEdit) n;
			SetupEditorSignals(graph);
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
					BuiltInFunction btFnReplica = (BuiltInFunction) System.Activator.CreateInstance(fnType);
					fn = btFnReplica;
;					foreach(FieldInfo field in fnType.GetFields()){
						if(Attribute.IsDefined(field, typeof(ConstructorField))){
							LineEdit propertyEditor = new LineEdit();
							propertyEditor.Text = field.GetValue(btFnReplica).ToString();
							propertyEditor.TextChanged += (string text) => {
								bool accept = btFnReplica.UpdateConstructorField(field, text);
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
					//TODO: Make replica of non-builtin functions
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
				
				fnInstanceMap.Add(funNode.GetInstanceId(), fn);
				activeGraph.AddChild(funNode);

				GD.Print("Added function " + fn + " to " + activeGraph.GetPath());

			}
		};

		dataPackage.CreateTree(functionsTree, treeRoot, componentMap);
		mathPackage.CreateTree(functionsTree, treeRoot, componentMap);
		logicPackage.CreateTree(functionsTree, treeRoot, componentMap);

		//LAMBDAS
		//TODO: Des-fod** a questão de passar parâmetros posicionais pra parâmetros (é argumentável que não tá fud****)

		//TODO: Toda a interpretação. lol
			
	}

	public void SetupEditorSignals(GraphEdit graph){
		graph.ConnectionRequest += (StringName from_node, long from_port, StringName to_node, long to_port) => OnGraphConnectionRequest(from_node, (int)from_port, to_node, (int)to_port, graph); 
		graph.DisconnectionRequest += (StringName from_node, long from_port, StringName to_node, long to_port) => OnGraphDisconnectRequest(from_node, (int)from_port, to_node, (int)to_port, graph);
		graph.NodeSelected += (Node node) => OnNodeSelected((GraphNode) node);
		graph.NodeDeselected += (Node node) => OnNodeUnselected((GraphNode) node);
		graph.DeleteNodesRequest += (Godot.Collections.Array nodes) => OnDeleteNodesRequest(graph);
		graph.GuiInput += (InputEvent evt) => OnGraphGuiInput(evt, graph);
	}
	public void CreateLambda(){
		GraphNode lambdaNode = (GraphNode) lambdaTemplate.Instantiate();
		activeGraph.AddChild(lambdaNode);
		Function lnFunc = new Function();
		lnFunc.name = "Lambda";
		fnInstanceMap.Add(lambdaNode.GetInstanceId(),  lnFunc);
		GraphEdit lambdaEditor = (GraphEdit) lambdaNode.GetNode("GraphEdit");
		SetupEditorSignals(lambdaEditor);
		Button addParamButton = lambdaNode.GetNode<Button>("AddParamButton");
		LineEdit titleEdit = null;

		HashSet<GraphNode> paramNodes = new HashSet<GraphNode>();
		
		lambdaNode.GuiInput += (InputEvent evt) => {
			if(evt is InputEventMouseButton eventMouse){
				if(eventMouse.DoubleClick){
					titleEdit = new LineEdit();
					titleEdit.Text = lnFunc.name;
					titleEdit.SetSize(new Vector2(200, titleEdit.Size.Y));
					titleEdit.SetPosition(new Vector2(lambdaNode.Position.X, lambdaNode.Position.Y-50));

					titleEdit.TextChanged += (String text) => {
						lnFunc.name = text;
						lambdaNode.Title = text;
					};
					lambdaNode.AddSibling(titleEdit);
				}
			}
		};

		lambdaNode.NodeDeselected += () => {
			if(titleEdit != null){
				titleEdit.QueueFree();
				titleEdit = null;
			}
		};

		lambdaEditor.ConnectionRequest += (StringName from_n, long from_p, StringName to_n, long to_p) => {
			var connections = lambdaEditor.GetConnectionList();
			GraphNode recv_node = (GraphNode) lambdaEditor.GetNode(new NodePath(to_n.ToString()));
			if(paramNodes.Contains(recv_node)){
				Label lblTest = new Label();
				lblTest.Text = "";
				recv_node.AddChild(lblTest);
				recv_node.SetSlotEnabledLeft(recv_node.GetChildCount()-1, true);
			}
		};

		Godot.GraphEdit.DisconnectionRequestEventHandler handleDisconnectLambda = (StringName from_n, long from_p, StringName to_n, long to_p) => {
			var connections = lambdaEditor.GetConnectionList();
			GraphNode recv_node = (GraphNode) lambdaEditor.GetNode(to_n.ToString());
			if(paramNodes.Contains(recv_node) && recv_node.GetChildCount() != 1){
				Node theDeleterrrrr = recv_node.GetChild((int) to_p);
				recv_node.RemoveChild(theDeleterrrrr);
				GD.Print("ParamNode children: " + recv_node.GetChildCount());
				theDeleterrrrr.QueueFree();
			}
		};
		lambdaEditor.DisconnectionRequest += handleDisconnectLambda;

		addParamButton.Pressed += () =>{

			string[] newParams = new string[lnFunc.parameters.Length+1];
			Array.Copy(lnFunc.parameters, newParams, lnFunc.parameters.Length);
			newParams[lnFunc.parameters.Length] = "param" + lnFunc.parameters.Length;
			GD.Print("Newparams length: " + newParams.Length);

			LineEdit lbdaParam = lambdaParamTemplate.Instantiate<LineEdit>();


			lbdaParam.Text = newParams[lnFunc.parameters.Length];
			lambdaNode.AddChild(lbdaParam);
			lambdaNode.MoveChild(lbdaParam, -2);
			lambdaNode.SetSlotEnabledLeft(newParams.Length+1, true); //Enables the slot at the index of the last param+2, to account for the GraphEdit and label


			GraphNode paramNode = new GraphNode();
			paramNode.Title = newParams[lnFunc.parameters.Length];
			paramNode.AddChild(new Control());
			paramNode.SetSlotEnabledLeft(0, true);
			paramNode.SetSlotEnabledRight(0, true);

			lbdaParam.TextChanged += (string text) => {
				int paramIdx = lbdaParam.GetIndex() - 2; //evil and bad copy paste
				lnFunc.parameters[paramIdx] = text;
				paramNode.Title = text;
			};

			paramNodes.Add(paramNode);
			rejectDeletionNodes.Add(paramNode.GetInstanceId()); //marks the node as un-deletable by user
			lambdaEditor.AddChild(paramNode);

			Button deleteParamButton = lbdaParam.GetNode<Button>("Button");

			deleteParamButton.Pressed += () => {

				string[] afterRemoveParams = new string[lnFunc.parameters.Length-1];
				int paramIdx = lbdaParam.GetIndex() - 2; //this is evil and bad. dont care
				if( paramIdx > 0 ) Array.Copy(lnFunc.parameters, 0, afterRemoveParams, 0, paramIdx);
				if( paramIdx < lnFunc.parameters.Length - 1 ) Array.Copy(lnFunc.parameters, paramIdx+1, afterRemoveParams, paramIdx, lnFunc.parameters.Length - paramIdx - 1);
				lnFunc.parameters = afterRemoveParams;

				lambdaNode.RemoveChild(lbdaParam);
				Godot.Collections.Array<Godot.Collections.Dictionary> connections = lambdaEditor.GetConnectionList();
				foreach(Godot.Collections.Dictionary d in connections){
					if(lambdaEditor.GetNode(d["to_node"].As<StringName>().ToString()) == paramNode || lambdaEditor.GetNode(d["from_node"].As<StringName>().ToString()) == paramNode){
						handleDisconnectLambda(d["from_node"].As<StringName>(),
							 d["from_port"].As<int>(),
							 d["to_node"].As<StringName>(),
							 d["to_port"].As<int>());
					}
				}
				lambdaEditor.RemoveChild(paramNode);
				paramNode.QueueFree();
				lambdaNode.SetSlotEnabledLeft(lambdaNode.GetChildCount()-1, false);
			};

			lnFunc.parameters = newParams;
		};

		
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


			if(graph.GetChildren().Contains(n) && !rejectDeletionNodes.Contains(n.GetInstanceId())){
				
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
				if(fnInstanceMap.ContainsKey(n.GetInstanceId())){
					fnInstanceMap.Remove(n.GetInstanceId());
				}
				n.QueueFree();
				containedNodes.Add(n);	
			}
			
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
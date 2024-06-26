using Godot;
using System;
using Functory.Lang;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;


public partial class root : Control
{
	HashSet<GraphNode> selectedNodes = new HashSet<GraphNode>();

	HashSet<ulong> rejectDeletionNodes = new HashSet<ulong>();
	Dictionary<ulong, Function> componentMap = new Dictionary<ulong, Function>();
	Dictionary<ulong, Function> fnInstanceMap = new Dictionary<ulong, Function>();
	GraphEdit activeGraph;

	GraphEdit rootEditor;
	PackedScene lambdaTemplate;
	PackedScene lambdaParamTemplate;
	TabContainer funcTabs;
	Tree projectFuncTree;
	FunctionPackage projectPackage;

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
			rootEditor = (GraphEdit) graphs[0];
		}

		funcTabs = (TabContainer) GetNode("HSplitContainer/VBoxContainer2/TabContainer");

		projectFuncTree = (Tree) GetNode("HSplitContainer/VBoxContainer2/TabContainer/Projeto");
		TreeItem prjTreeRoot = projectFuncTree.CreateItem(null);
		projectFuncTree.HideRoot = true;
		projectFuncTree.ScrollVerticalEnabled = false;
		projectFuncTree.ScrollHorizontalEnabled = false;

		Button ffwdRun = (Button) GetNode("HSplitContainer/VBoxContainer/Panel/HBoxContainer/Button2");

		ffwdRun.ButtonDown += FFWDRun;

		Button lambdaButton = (Button) GetNode("HSplitContainer/VBoxContainer2/Button");

		lambdaButton.ButtonDown += CreateLambda;

		foreach(Node n in graphs){
			GraphEdit graph = (GraphEdit) n;
			SetupEditorSignals(graph);
		}

		projectPackage = new FunctionPackage("Projeto","project");

		FunctionPackage mathPackage = new FunctionPackage("Matemática", "math");
		mathPackage.functions.Add(new Sum());

		FunctionPackage dataPackage = new FunctionPackage("Dados", "data");
		dataPackage.functions.Add(new IntegerConstructor());
		dataPackage.functions.Add(new BooleanConstructor());

		FunctionPackage logicPackage = new FunctionPackage("Lógica", "logic");
		logicPackage.functions.Add(new Equals());
		logicPackage.functions.Add(new Not());
		logicPackage.functions.Add(new If());

		Tree functionsTree = (Tree) GetNode("HSplitContainer/VBoxContainer2/TabContainer/Padrão");

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

		projectFuncTree.ItemActivated +=  () => {
			TreeItem select = projectFuncTree.GetSelected();
			ulong instanceId = select.GetInstanceId();

			if(componentMap.ContainsKey(instanceId)){

				Function fn = componentMap[instanceId];

				GraphNode funNode = new GraphNode();
				
				fn.dependentNodes.Add(funNode);

				for(int i = 0; i < fn.parameters.Length; i++){
					Label prmLabel = new Label();
					prmLabel.Text = fn.parameters[i];
					funNode.AddChild(prmLabel);
					funNode.SetSlotEnabledLeft(i, true);
					funNode.SetSlotEnabledRight(i, false);
				}

				if(fn.parameters.Length == 0){
					funNode.AddChild(new Control());
					funNode.SetSlotEnabledLeft(0, false);
				}

				funNode.SetSlotEnabledRight(0, true);
				funNode.Title = fn.name;

				fnInstanceMap.Add(funNode.GetInstanceId(), fn);
				activeGraph.AddChild(funNode);

			}
			else {
				//????
				GD.Print("?? Tried to add function that is not in component map");
			}
		};

		dataPackage.CreateTree(functionsTree, treeRoot, componentMap);
		mathPackage.CreateTree(functionsTree, treeRoot, componentMap);
		logicPackage.CreateTree(functionsTree, treeRoot, componentMap);

		//LAMBDAS
		//TODO: Fazer com que um Lambda remova suas Nodes dependentes ao ser removido e atualize o registro de funções.

		//TODO: Iniciar rotina de interpretação parcial utilizando Stack.
			
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
		lnFunc.name = "Lambda" + projectPackage.functions.Count;
		lnFunc.defNode = lambdaNode;

		lambdaNode.Title = lnFunc.name;

		fnInstanceMap.Add(lambdaNode.GetInstanceId(),  lnFunc);

		projectPackage.functions.Add(lnFunc);
		
		updatePFTree();

		GraphEdit lambdaEditor = (GraphEdit) lambdaNode.GetNode("GraphEdit");

		rejectDeletionNodes.Add(lambdaEditor.GetNode("OutputNode").GetInstanceId()); 
		SetupEditorSignals(lambdaEditor);

		Button addParamButton = lambdaNode.GetNode<Button>("AddParamButton");
		LineEdit titleEdit = null;

		funcTabs.CurrentTab = 1;

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
						updatePFTree();
						CleanUpDisposedNodes(lnFunc.dependentNodes);
						foreach(GraphNode depNode in lnFunc.dependentNodes){
							depNode.Title = text;
						}
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

			foreach(GraphNode depNode in lnFunc.dependentNodes){
				Label newPrmLabel = new Label();
				newPrmLabel.Text = lbdaParam.Text;

				if(lnFunc.parameters.Length == 0){
					Node dummy = depNode.GetChild(0);
					depNode.RemoveChild(dummy);
					dummy.QueueFree();
				}

				depNode.AddChild(newPrmLabel);
				depNode.SetSlotEnabledLeft(newParams.Length - 1, true);

				
			}

			GraphNode paramNode = new GraphNode();
			paramNode.Title = newParams[lnFunc.parameters.Length];
			paramNode.AddChild(new Control());
			paramNode.SetSlotEnabledLeft(0, true);
			paramNode.SetSlotEnabledRight(0, true);
			paramNode.AddToGroup("LambdaParamNodes");

			lbdaParam.TextChanged += (string text) => {
				int paramIdx = lbdaParam.GetIndex() - 2; //evil and bad copy paste
				lnFunc.parameters[paramIdx] = text;
				paramNode.Title = text;
				CleanUpDisposedNodes(lnFunc.dependentNodes);
				foreach(GraphNode depNode in lnFunc.dependentNodes){
					Label prmLabel = (Label) depNode.GetChild(paramIdx);
					prmLabel.Text = text;
				}
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
				

				GraphEdit parentEditor = (GraphEdit)lambdaNode.GetParent();
				foreach(Godot.Collections.Dictionary d in parentEditor.GetConnectionList()){
					if(parentEditor.GetNode(d["to_node"].As<StringName>().ToString())==lambdaNode && d["to_port"].As<int>() == paramIdx){
						StringName to_n = d["to_node"].As<StringName>();
						StringName from_n = d["from_node"].As<StringName>();
						int to_p = d["to_port"].As<int>();
						int from_p = d["from_port"].As<int>();
						parentEditor.DisconnectNode(from_n, from_p, to_n, to_p);
					}
				}


				foreach(Godot.Collections.Dictionary d in parentEditor.GetConnectionList()){
					if(parentEditor.GetNode(d["to_node"].As<StringName>().ToString())==lambdaNode && d["to_port"].As<int>() > paramIdx){
						StringName to_n = d["to_node"].As<StringName>();
						StringName from_n = d["from_node"].As<StringName>();
						int to_p = d["to_port"].As<int>();
						int from_p = d["from_port"].As<int>();

						parentEditor.DisconnectNode(from_n, from_p, to_n, to_p);

						parentEditor.ConnectNode(from_n, from_p, to_n, to_p - 1);
					}
				}

				foreach(GraphNode depNode in lnFunc.dependentNodes){
					GraphEdit parent = depNode.GetParent<GraphEdit>();
					foreach(Godot.Collections.Dictionary d in parent.GetConnectionList()){
						if(parent.GetNode(d["to_node"].As<StringName>().ToString()) == depNode && d["to_port"].As<int>() == paramIdx){
							StringName to_n = d["to_node"].As<StringName>();
							StringName from_n = d["from_node"].As<StringName>();
							int to_p = d["to_port"].As<int>();
							int from_p = d["from_port"].As<int>();

							parent.DisconnectNode(from_n, from_p, to_n, to_p);
						}
					}
					foreach(Godot.Collections.Dictionary d in parent.GetConnectionList()){
						if(parent.GetNode(d["to_node"].As<StringName>().ToString()) == depNode && d["to_port"].As<int>() > paramIdx){
							StringName to_n = d["to_node"].As<StringName>();
							StringName from_n = d["from_node"].As<StringName>();
							int to_p = d["to_port"].As<int>();
							int from_p = d["from_port"].As<int>();

							parent.DisconnectNode(from_n, from_p, to_n, to_p);

							parentEditor.ConnectNode(from_n, from_p, to_n, to_p - 1);
						}
					}
					depNode.RemoveChild(depNode.GetChild(paramIdx));
					depNode.Position = new Vector2(depNode.Position.X, depNode.Position.Y+1); //The jiggler...
				}

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

				lambdaNode.Position = new Vector2(lambdaNode.Position.X, lambdaNode.Position.Y+1); //This is stupid. It's also necessary for making the GraphEdit redraw the cables correctly :v)
			};

			lnFunc.parameters = newParams;
		};

		
	}
	
	public void CleanUpDisposedNodes(List<GraphNode> nodes){
		nodes.RemoveAll((GraphNode gn) => !IsInstanceValid(gn));
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
					StringName to_node = con["to_node"].As<StringName>();
					StringName from_node = con["from_node"].As<StringName>();
					if(to_node == n.Name || from_node == n.Name){
						int to_port = con["to_port"].As<int>();
						int from_port = con["from_port"].As<int>();
						graph.DisconnectNode(from_node, from_port, to_node, to_port);
					}

					selectedNodes.Remove(n);
				}

				if(n.IsInGroup("Lambda")){
					Function lambdaFunc = fnInstanceMap[n.GetInstanceId()];
					CleanUpDisposedNodes(lambdaFunc.dependentNodes);
					foreach(GraphNode depNode in lambdaFunc.dependentNodes){
						GraphEdit parentGraph = depNode.GetParent<GraphEdit>();
						foreach(Godot.Collections.Dictionary con in graph.GetConnectionList()){
							StringName to_node = con["to_node"].As<StringName>();
							StringName from_node = con["from_node"].As<StringName>();
							if(to_node == n.Name || from_node == n.Name){
								int to_port = con["to_port"].As<int>();
								int from_port = con["from_port"].As<int>();
								graph.DisconnectNode(from_node, from_port, to_node, to_port);
							}
						}
						depNode.QueueFree();
					}
					projectPackage.functions.Remove(lambdaFunc);
					updatePFTree();
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

	public void updatePFTree(){
		if(projectFuncTree.GetRoot().GetChildCount() > 0){
			TreeItem oldPkg = projectFuncTree.GetRoot().GetChild(0);	
			projectFuncTree.GetRoot().RemoveChild(oldPkg);
			oldPkg.Free();
		}
		projectPackage.CreateTree(projectFuncTree, projectFuncTree.GetRoot(), componentMap);
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

	public void AddFnFromTree(){

	}

	public void FFWDRun() {
		foreach(Function lf in projectPackage.functions){
			lf.def = null;
		} //clear out all old lambda defs

		Godot.Collections.Array<Godot.Collections.Dictionary> connections = rootEditor.GetConnectionList();

		IEnumerable<Node> rootNodes = rootEditor.GetChildren().Where((Node n) => {
			if(n is GraphNode gn){
				if(!connections.Any((dic)=>
					rootEditor.GetNode(dic["from_node"].As<StringName>().ToString()) == gn	
				)){
					return true;
				}
			}
			return false;
		});

		foreach(GraphNode gn in rootNodes){
			StyleBoxFlat styleBox = new StyleBoxFlat();
			styleBox.BgColor = Color.FromHtml("FF0000FF");
			gn.AddThemeStyleboxOverride("titlebar", styleBox);

			Functory.Expression rootNodeE = InterpretNode(gn, rootEditor);
			GD.Print("root node expr: " + rootNodeE);
			GD.Print("Attempting to interpret expression...");
			GD.Print("Evaluated to: " + Functory.Lang.Interpreter.evalTwo(rootNodeE.expand(null)));
			
		}
	}

	

	public Functory.Expression InterpretNode(GraphNode gn, GraphEdit graph){
		Functory.Expression resultExpr;

		if(fnInstanceMap.ContainsKey(gn.GetInstanceId())){
			Function fnIns = fnInstanceMap[gn.GetInstanceId()];
			Dictionary<string, Functory.Expression> namedParams = new Dictionary<string, Functory.Expression>();
			for(int i = 0; i < fnIns.parameters.Length; i++){
				Godot.Collections.Array<Godot.Collections.Dictionary> connections = graph.GetConnectionList();
				Godot.Collections.Dictionary paramNodeConnection = connections.FirstOrDefault((dic) => {
					return graph.GetNode(dic["to_node"].As<StringName>().ToString()) == gn && dic["to_port"].As<int>() == i;
				}, null);
				if(paramNodeConnection != null){
					GraphNode paramNode = (GraphNode) graph.GetNode(paramNodeConnection["from_node"].As<StringName>().ToString());
					namedParams.Add(fnIns.parameters[i], InterpretNode(paramNode, graph));
				}
			}
			if(fnIns.def == null && !(fnIns is BuiltInFunction) && !fnIns.interpretationInProgress){
				//User-defined function, try and find it's correspondent lambda node.
				//Check if we're not already interpreting it to avoid infinite recursion.
				fnIns.interpretationInProgress = true;
				GraphNode defNode = fnIns.defNode;
				GraphEdit lambdaGraph = (GraphEdit) defNode.GetNode("GraphEdit");
				GraphNode outputNode = (GraphNode) lambdaGraph.GetNode("OutputNode");
				fnIns.def = InterpretNode(outputNode, lambdaGraph);
				fnIns.interpretationInProgress = false;
			}
			resultExpr = new Functory.Expression(fnIns, null, namedParams);
			return resultExpr;
		}
		else{
			if(gn.IsInGroup("LambdaParamNodes")){
				Functory.Expression[] posParams = new Functory.Expression[gn.GetChildCount()-1];
				Godot.Collections.Array<Godot.Collections.Dictionary> connections = graph.GetConnectionList();
				for(int i = 0; i < gn.GetChildCount()-1; i++){
					Godot.Collections.Dictionary paramNodeConnection = connections.FirstOrDefault((dic) => {
					return graph.GetNode(dic["to_node"].As<StringName>().ToString()) == gn && dic["to_port"].As<int>() == i;
				}, null);
				   if(paramNodeConnection != null){
					 GraphNode paramNode = (GraphNode) graph.GetNode(paramNodeConnection["from_node"].As<StringName>().ToString());
					 posParams[i] = InterpretNode(paramNode, graph);
				   }
				   else{
					posParams[i] = null; //bad...
				   }
				}
				return new Functory.Expression(new BindingOf(gn.Title),posParams,null);
			}
			else if(gn.IsInGroup("LambdaOutputNode")){
				Godot.Collections.Array<Godot.Collections.Dictionary> connections = graph.GetConnectionList();
				Godot.Collections.Dictionary paramNodeConnection = connections.FirstOrDefault((dic) => {
					return graph.GetNode(dic["to_node"].As<StringName>().ToString()) == gn && dic["to_port"].As<int>() == 0;
				}, null);
				if(paramNodeConnection != null){
					GraphNode paramNode = (GraphNode) graph.GetNode(paramNodeConnection["from_node"].As<StringName>().ToString());
					return InterpretNode(paramNode, graph);
				}
				else{
					return null;
				}
			}
			return null;
		}
	}
	
	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}

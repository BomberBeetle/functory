using Godot;
using System;
using Functory.Lang;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Functory;
using System.Transactions;
using System.IO;


public partial class root : Control
{
	HashSet<GraphNode> selectedNodes = new HashSet<GraphNode>();

	Label runInfoLabel;

	HashSet<ulong> rejectDeletionNodes = new HashSet<ulong>();
	Dictionary<ulong, Function> componentMap = new Dictionary<ulong, Function>();
	Dictionary<ulong, Function> fnInstanceMap = new Dictionary<ulong, Function>();

	List<FunctionPackage> rootPackages = new List<FunctionPackage>();

	GraphEdit activeGraph;

	bool inExecution = false;

	System.Collections.Generic.List<Node> rootNodes;

	Interpreter interpreter;
	GraphEdit rootEditor;
	PackedScene lambdaTemplate;
	PackedScene lambdaParamTemplate;
	TabContainer funcTabs;
	Tree projectFuncTree;
	FunctionPackage projectPackage;

	string filename = null;

	bool modified = false;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{	
		GetTree().AutoAcceptQuit = false;
		GetWindow().Title = "Functory";


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
		
		//GD.Print(Interpreter.evalTwo(sdefApp));
		
		string[] parameters = new string[] {"f", "a", "b"};
		Function rebind = new Function(null, parameters, "rebind");

		rebind.def = new Functory.Expression(new BindingOf("f"), new Functory.Expression[]{new BindingOf("a"), new BindingOf("b")}, null);
		//rebind.def = new BindingOf("f", rebind, null, null, new Application[]{new BindingOf("a", rebind), new BindingOf("b", rebind)}); //what the fuck?
		 
		Application rebindApp = new Application(rebind, null, new Application[]{sdefApp, two, three});
		
		//GD.Print(Interpreter.evalTwo(rebindApp));

		Function partiallyAppliedBonanza = new Function(new Functory.Expression(new Functory.Expression(rebind, new Functory.Expression[]{
			new(sumRebind, null, null),
			new(new IntegerConstructor(2), null, null)
		},null),new Functory.Expression[]{new BindingOf("a")},null), new string[]{"a"});

		Dictionary<string, Application> shenaniganParams = new Dictionary<string, Application>();
		shenaniganParams.Add("a", three);
		Application shenanigans = new Application(partiallyAppliedBonanza, shenaniganParams, null);

		//GD.Print(Interpreter.evalTwo(shenanigans));

		
		
		
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

		ExecutionFrame rootFrameTest = new ExecutionFrame(new Application(recurse, null, new Application[]{new Application(new IntegerConstructor(50), null, null)}));
		Interpreter ip = new Interpreter();
		ip.currentFrame = rootFrameTest;
		object evalResult = null;
		GD.Print("Starting progressive eval test");
		//GD.Print("Root frame: " + rootFrameTest);
		evalResult = ip.EvalStep();
		//GD.Print(evalResult);
		int iters = 1000000;
		while(evalResult is ExecutionFrame && iters > 0){
			evalResult = ip.EvalStep();
			//GD.Print("\n\n" + evalResult);
			iters--;
		}
		if(iters == 0){
			GD.Print("Progressive eval exceeded iteration limit of " + iters + ", exiting early.");
		}
		GD.Print("Progressive eval result is " + evalResult);
		
		//GD.Print(Interpreter.evalTwo(new Application(recurse, null, new Application[]{new Application(new IntegerConstructor(500), null, null)})));

		runInfoLabel = new Label();

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

		Button saveButton = GetNode<Button>("HSplitContainer/VBoxContainer/Panel/Button5");

		Button newButton = GetNode<Button>("HSplitContainer/VBoxContainer/Panel/Button6");

		Button loadButton = GetNode<Button>("HSplitContainer/VBoxContainer/Panel/Button7");

		loadButton.ButtonDown += LoadButtonPressed;

		newButton.ButtonDown += NewButtonPressed;

		saveButton.ButtonDown += SaveButtonPressed;

		Button ffwdRun = (Button) GetNode("HSplitContainer/VBoxContainer/Panel/HBoxContainer/Button2");

		Button stepRunButton = (Button) GetNode("HSplitContainer/VBoxContainer/Panel/HBoxContainer/Button");

		Button resetButton = GetNode<Button>("HSplitContainer/VBoxContainer/Panel/HBoxContainer/Button4");

		ffwdRun.ButtonDown += FFWDRun;

		stepRunButton.ButtonDown += StepRun;

		resetButton.ButtonDown += ResetRun;

		Button lambdaButton = (Button) GetNode("HSplitContainer/VBoxContainer2/Button");

		lambdaButton.ButtonDown += CreateLambda;

		foreach(Node n in graphs){
			GraphEdit graph = (GraphEdit) n;
			SetupEditorSignals(graph);
		}

		projectPackage = new FunctionPackage("Projeto","project");

		FunctionPackage mathPackage = new FunctionPackage("Matemática", "math");
		mathPackage.AddFunction(new Sum());
		mathPackage.AddFunction(new Subtract());
		mathPackage.AddFunction(new Multiply());
		mathPackage.AddFunction(new Divide());
		mathPackage.AddFunction(new Modulo());
		mathPackage.AddFunction(new Abs());
		mathPackage.AddFunction(new LargerThan());
		mathPackage.AddFunction(new LargerThanEquals());
		mathPackage.AddFunction(new SmallerThan());
		mathPackage.AddFunction(new SmallerThanEquals());

		FunctionPackage dataPackage = new FunctionPackage("Dados", "data");
		dataPackage.AddFunction(new IntegerConstructor());
		dataPackage.AddFunction(new BooleanConstructor());

		FunctionPackage logicPackage = new FunctionPackage("Lógica", "logic");
		logicPackage.AddFunction(new Equals());
		logicPackage.AddFunction(new Not());
		logicPackage.AddFunction(new If());
		logicPackage.AddFunction(new Or());
		logicPackage.AddFunction(new And());

		rootPackages.Add(mathPackage);
		rootPackages.Add(logicPackage);
		rootPackages.Add(dataPackage);
		rootPackages.Add(projectPackage);

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

				AddFunctionToEditor(funNode, fn, activeGraph);

				MarkAsModified();
			}
		};

		projectFuncTree.ItemActivated +=  () => {
			TreeItem select = projectFuncTree.GetSelected();
			ulong instanceId = select.GetInstanceId();

			if(componentMap.ContainsKey(instanceId)){

				Function fn = componentMap[instanceId];

				GraphNode funNode = new GraphNode();
				
				fn.dependentNodes.Add(funNode);

				AddFunctionToEditor(funNode, fn, activeGraph);

				MarkAsModified();
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

		lambdaNode.Dragged += (x, y) => MarkAsModified();

		Function lnFunc = new Function();
		lnFunc.name = "Lambda" + projectPackage.functions.Count;
		lnFunc.defNode = lambdaNode;

		lambdaNode.Title = lnFunc.name;

		fnInstanceMap.Add(lambdaNode.GetInstanceId(),  lnFunc);

		projectPackage.AddFunction(lnFunc);
		
		UpdatePFTree();

		GraphEdit lambdaEditor = (GraphEdit) lambdaNode.GetNode("GraphEdit");

		rejectDeletionNodes.Add(lambdaEditor.GetNode("OutputNode").GetInstanceId()); 
		SetupEditorSignals(lambdaEditor);

		Button addParamButton = lambdaNode.GetNode<Button>("AddParamButton");

		LineEdit titleEdit = CreateLbdaTitleEdit(lnFunc, lambdaNode);

		funcTabs.CurrentTab = 1;

		HashSet<GraphNode> paramNodes = new HashSet<GraphNode>();
		
		lambdaNode.GuiInput += GetLambdaGuiInputHandler(titleEdit, lambdaNode, lnFunc);

		lambdaNode.NodeDeselected += GetNodeDeselectedHandler(lambdaNode, titleEdit);

		lambdaEditor.ConnectionRequest += GetLambdaConnectionHandler(lambdaEditor, paramNodes);

		Godot.GraphEdit.DisconnectionRequestEventHandler handleDisconnectLambda = GetLambdaEditorDisconnectHandler(lambdaEditor, paramNodes);
		lambdaEditor.DisconnectionRequest += handleDisconnectLambda;

		addParamButton.Pressed += GetCreateParamHandler(lnFunc, lambdaNode, paramNodes, lambdaEditor, handleDisconnectLambda);
	
		MarkAsModified();
	}
	
	public void CleanUpDisposedNodes(List<GraphNode> nodes){
		nodes.RemoveAll((GraphNode gn) => !IsInstanceValid(gn));
	}

	public void OnGraphConnectionRequest(StringName from_node, int from_port, StringName to_node, int to_port, GraphEdit graph)
	{
		var connections = graph.GetConnectionList();
		bool alreadyConnected = connections.Any((Godot.Collections.Dictionary conn) => {return conn["to_port"].As<int>()==to_port && conn["to_node"].As<StringName>()==to_node;});

		if(!alreadyConnected){
			graph.ConnectNode(from_node, from_port, to_node, to_port);
			MarkAsModified();
		}
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
				MarkAsModified();
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
					UpdatePFTree();
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
		MarkAsModified();
	}

	public void UpdatePFTree(){
		if(projectFuncTree.GetRoot().GetChildCount() > 0){
			TreeItem oldPkg = projectFuncTree.GetRoot().GetChild(0);	
			projectFuncTree.GetRoot().RemoveChild(oldPkg);
			oldPkg.Free();
		}
		projectPackage.CreateTree(projectFuncTree, projectFuncTree.GetRoot(), componentMap);
	}

	public void OnGraphGuiInput(InputEvent evt, GraphEdit graph){
		if (evt is InputEventMouseButton buttonEvt)
		{
			if (buttonEvt.ButtonIndex == MouseButton.Left && buttonEvt.Pressed)
			{
				activeGraph.ShowGrid = false;
				graph.ShowGrid = true;
				activeGraph = graph;
			}
		}
	}

	public void SetupRun(){
		foreach(Function lf in projectPackage.functions){
			lf.def = null;
		} //clear out all old lambda defs

		Godot.Collections.Array<Godot.Collections.Dictionary> connections = rootEditor.GetConnectionList();

		rootNodes = rootEditor.GetChildren().Where((Node n) => {
			if(n is GraphNode gn){
				if(!connections.Any((dic)=>
					rootEditor.GetNode(dic["from_node"].As<StringName>().ToString()) == gn	
				)){
					return true;
				}
			}
			return false;
		}).ToList();

	}

	public SerializedEditor SerializeEditor(GraphEdit editor, ProjectRegistry reg){
		SerializedEditor serialized = new SerializedEditor();
		List<SerializedNode> nodes = new List<SerializedNode>();

		foreach(Node node in editor.GetChildren()){
			if (node is GraphNode gn){
				SerializedNode sn = new SerializedNode();
				sn.X = gn.Position.X;
				sn.Y = gn.Position.Y;
				sn.nodeId = gn.GetInstanceId().ToString(); //god bless godot for UUID's
				if(gn.IsInGroup("Lambda")){
					sn.IsLambdaNode = true;
					Function lambdaFn = fnInstanceMap[gn.GetInstanceId()];
					sn.parameters = lambdaFn.parameters;
					sn.registryAddress = lambdaFn.GetAddress();
					sn.defEditor = SerializeEditor(gn.GetNode<GraphEdit>("GraphEdit"),reg);
					sn.defEditor.IsLambdaEditor = true;
					sn.defEditor.LambdaNodeId = sn.nodeId;
					reg.functions.Add(sn.nodeId, lambdaFn.name);
				}
				else if(gn.IsInGroup("LambdaOutputNode")){
					sn.IsOutputNode = true;
				}
				else if(gn.IsInGroup("LambdaParamNodes")){
					sn.IsParamNode = true;
					sn.paramName = gn.Title; //TODO STOP DOING THAT!!! NOT OKAY!!! NOT GOOD!!!! WHAT THE FUCK???
				}
				else{
					Function nodeFn = fnInstanceMap[gn.GetInstanceId()];
					sn.registryAddress = nodeFn.GetAddress();
					if(nodeFn is BuiltInFunction btFn){
						sn.constructorFields = btFn.ExportConstructorFields();
					}
				}

				nodes.Add(sn);
			}
		}

		serialized.nodes = nodes.ToArray();

		List<SerializedEditor.Connection> conns = new List<SerializedEditor.Connection>();
		foreach(Godot.Collections.Dictionary dic in editor.GetConnectionList()){
			SerializedEditor.Connection conn = new SerializedEditor.Connection();
			conn.toPort = dic["to_port"].As<int>();
			conn.toNodeId = editor.GetNode(dic["to_node"].As<StringName>().ToString()).GetInstanceId().ToString();
			conn.fromNodeId = editor.GetNode(dic["from_node"].As<StringName>().ToString()).GetInstanceId().ToString();
			conns.Add(conn);
		}

		serialized.connections = conns.ToArray();

		return serialized;
	}

	public void UnpackProjectRegistry(ProjectRegistry reg){
		projectPackage.ChildPackages.Clear();
		projectPackage.functions.Clear();

		foreach(var (nodeId , fnName) in reg.functions){
			Function nF = new Function();
			nF.name = fnName;
			nF.serializedNodeId = nodeId;
			projectPackage.AddFunction(nF);
		}
	}

	public Function GetFunctionFromAddress(string address){
		string[] splitAddress = address.Split(".");
		FunctionPackage curPack = null;
		if(splitAddress.Length > 0){
			curPack = rootPackages.First((pack) => pack.packageIdentifier == splitAddress[0]);
			int curLevel = 1;
			while(curLevel < splitAddress.Length-1){
				curPack = curPack.ChildPackages.First((pack) => pack.packageIdentifier == splitAddress[curLevel]);
				if(curPack == null){
					return null;
				}
				curLevel++;
			}
			if(curPack == null){
				return null;
			}
			else{
				Function retFn = curPack.functions.First((fun) => fun.name == splitAddress.Last());
				return retFn;
			}
		}
		else return null;
		
	}

	public Dictionary<string, GraphNode> LoadSerializedProject(SerializedEditor sEditor, ProjectRegistry reg, GraphEdit targetEditor){

		Dictionary<string, GraphNode> createdNodes = new Dictionary<string, GraphNode>(); //Serialized ID - GraphNode instance mapping
		//make sure to set up callbacks for everything
		
		

		foreach(SerializedNode sn in sEditor.nodes){
			if(sn.IsParamNode){
				//just absolutely slam that thing there. again. dont forget to set the title (which is bad)
				GraphNode prnode = CreateParamNode(sn.paramName);
				prnode.PositionOffset = new Vector2(sn.X, sn.Y);
				targetEditor.AddChild(prnode);
				createdNodes.Add(sn.nodeId, prnode);
				
			}
			else if(sn.IsOutputNode){ // we actually don't want to create a new one, since the create lambda function does that. let's just hijack the created output node.
				GraphNode prnode = (GraphNode) targetEditor.GetChildren().First((node) => node.IsInGroup("LambdaOutputNode"));
				prnode.Dragged += (x, y) => MarkAsModified();
				prnode.PositionOffset = new Vector2(sn.X, sn.Y);
				rejectDeletionNodes.Add(prnode.GetInstanceId());
				createdNodes.Add(sn.nodeId, prnode);
			}
			else if(sn.IsLambdaNode){

				Function fnFromRegistry = GetFunctionFromAddress(sn.registryAddress);
				GraphNode lambdaNode = (GraphNode) lambdaTemplate.Instantiate();

				lambdaNode.Dragged += (x, y) => MarkAsModified();

				lambdaNode.Title = reg.functions[sn.nodeId];
				fnInstanceMap.Add(lambdaNode.GetInstanceId(), fnFromRegistry);
				fnFromRegistry.parameters = sn.parameters;
				fnFromRegistry.defNode = lambdaNode;

				GraphEdit lambdaEdit = lambdaNode.GetNode<GraphEdit>("GraphEdit");
				Dictionary<string, GraphNode> lambdaNodes = LoadSerializedProject(sn.defEditor, reg, lambdaEdit);
				HashSet<GraphNode> paramNodes = new HashSet<GraphNode>(lambdaNodes.Values.Where((node)=>node.IsInGroup("LambdaParamNodes")));

				SetupEditorSignals(lambdaEdit);

				var dcHandler = GetLambdaEditorDisconnectHandler(lambdaEdit, paramNodes);
				lambdaEdit.DisconnectionRequest += dcHandler;
				lambdaEdit.ConnectionRequest += GetLambdaConnectionHandler(lambdaEdit, paramNodes);

				Button addParamButton = lambdaNode.GetNode<Button>("AddParamButton");
				addParamButton.Pressed += GetCreateParamHandler(fnFromRegistry, lambdaNode, paramNodes, lambdaEdit, dcHandler);

				HashSet<GraphNode> prNodesAssociated = new HashSet<GraphNode>(paramNodes);

				int idx = 0;
				foreach(var param in sn.parameters){
					GraphNode associatedParamNode = prNodesAssociated.First((node)=>node.Title==param);
					prNodesAssociated.Remove(associatedParamNode);
					CreateParamLabel(param, lambdaNode, associatedParamNode, idx, fnFromRegistry, lambdaEdit, dcHandler);
					AddNewParamToDepNodes(fnFromRegistry, param, idx+1);
					idx++;
				}

				LineEdit titleEdit = CreateLbdaTitleEdit(fnFromRegistry, lambdaNode);
		
				lambdaNode.GuiInput += GetLambdaGuiInputHandler(titleEdit, lambdaNode, fnFromRegistry);

				lambdaNode.NodeDeselected += GetNodeDeselectedHandler(lambdaNode, titleEdit);

				lambdaNode.PositionOffset = new Vector2(sn.X, sn.Y);

				createdNodes.Add(sn.nodeId, lambdaNode);

				targetEditor.AddChild(lambdaNode);

			}
			else{
				Function fnFromRegistry = GetFunctionFromAddress(sn.registryAddress);
				GraphNode node = new GraphNode();

				Dictionary<string, object> constructorFields = null;

				if(fnFromRegistry is BuiltInFunction btFn){
					 constructorFields = sn.constructorFields;
				}
				
				node.Title = fnFromRegistry.name;
				createdNodes.Add(sn.nodeId, node);
				AddFunctionToEditor(node, fnFromRegistry, targetEditor, constructorFields);
				node.PositionOffset = new Vector2(sn.X, sn.Y);
			}

			}

			foreach(var cn in sEditor.connections){
				var fromN = createdNodes[cn.fromNodeId];
				var toN = createdNodes[cn.toNodeId];
				if(toN.IsInGroup("LambdaParamNodes")){
					Label lblTest = new Label();
					lblTest.Text = "";
					toN.AddChild(lblTest);
					toN.SetSlotEnabledLeft(toN.GetChildCount()-1, true);
				}
				targetEditor.ConnectNode(fromN.Name, 0, toN.Name, cn.toPort);		
			}

		UpdatePFTree();
		
		return createdNodes;
	}

	public void FFWDRun() {

		if(!inExecution){

			SetupRun();
			interpreter = new Interpreter();
			if(rootNodes.Count != 0){
					Node start = rootNodes.First(); 
					rootNodes.RemoveAt(0);
					Functory.Expression startNodeE = InterpretNode((GraphNode) start, rootEditor);
					interpreter.currentFrame = new ExecutionFrame(startNodeE.expand(null));
					StyleBoxFlat styleBox = new StyleBoxFlat();
					styleBox.BgColor = Color.FromHtml("00BB00FF");
					styleBox.ShadowColor = Color.FromHtml("00BB00FF");
					interpreter.currentFrame.application.appNode.AddThemeStyleboxOverride("titlebar", styleBox);
			}
			else{
					inExecution = false;
					return;
			}
			/*foreach(GraphNode gn in rootNodes){
				StyleBoxFlat styleBox = new StyleBoxFlat();
				styleBox.BgColor = Color.FromHtml("FF0000FF");
				gn.AddThemeStyleboxOverride("titlebar", styleBox);

				Functory.Expression rootNodeE = InterpretNode(gn, rootEditor);
				GD.Print("root node expr: " + rootNodeE);
				GD.Print("Attempting to interpret expression...");
				GD.Print("Evaluated to: " + Functory.Lang.Interpreter.evalTwo(rootNodeE.expand(null)));
			
			}*/
		}

		while(interpreter.currentFrame != null){
			try{
			var res = interpreter.EvalStep();
			interpreter.currentFrame.application.appNode.RemoveThemeStyleboxOverride("titlebar");
			while(res is ExecutionFrame){
				res = interpreter.EvalStep();
				interpreter.currentFrame.application.appNode.RemoveThemeStyleboxOverride("titlebar");
			}
			AcceptDialog accept = new AcceptDialog();
			accept.DialogText = "Resultado de " + interpreter.currentFrame.application.appNode.Title + ": " + res;
			AddChild(accept);
			accept.PopupCentered();
			if(rootNodes.Count != 0){
					Node start = rootNodes.First(); 
					rootNodes.RemoveAt(0);
					Functory.Expression startNodeE = InterpretNode((GraphNode) start, rootEditor);
					interpreter.currentFrame = new ExecutionFrame(startNodeE.expand(null));
			}
			else{
				interpreter.currentFrame = null;
			}
			}
			catch(Exception e){
				AcceptDialog warn = new AcceptDialog();
				warn.DialogText = "Houve um erro durante a execução: " + e.Message + ". Interrompendo execução";
				AddChild(warn);
				warn.PopupCentered();
				ResetRun();
			}
		}

		inExecution = false;
	}

	public void AddFunctionToEditor(GraphNode funNode, Function fn, GraphEdit graph, Dictionary<string, object> constructorFields = null){
		funNode.Title = fn.name;
		funNode.Dragged += (x, y) => MarkAsModified();

		if (fn is BuiltInFunction btFn)
		{

			Type fnType = fn.GetType();
			BuiltInFunction btFnReplica = (BuiltInFunction)System.Activator.CreateInstance(fnType);
			fn = btFnReplica;
			btFnReplica.defNode = funNode;
			btFnReplica.package = btFn.package;
			if(constructorFields != null){
				btFnReplica.LoadConstructorFields(constructorFields);
			}
			foreach (FieldInfo field in fnType.GetFields())
			{
				if (Attribute.IsDefined(field, typeof(ConstructorField)))
				{
					LineEdit propertyEditor = new LineEdit();
					propertyEditor.Text = field.GetValue(btFnReplica).ToString();
					propertyEditor.TextChanged += (string text) =>
					{
						bool accept = btFnReplica.UpdateConstructorField(field, text);
						if (accept)
						{
							GD.Print("Property " + field.Name + " of " + fn + "changed to " + text);
							propertyEditor.RemoveThemeColorOverride("font_color");
						}
						else
						{
							GD.Print("\"" + text + "\" rejected for property " + field.Name + " of function " + fn);
							propertyEditor.AddThemeColorOverride("font_color", new Color(0.75f, 0.25f, 0.1f));
						}
					};
					funNode.AddChild(propertyEditor);
				}
			}
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
				graph.AddChild(funNode);
		}


	public void ResetRun(){
		if(inExecution){
			if(interpreter.currentFrame != null){
				interpreter.currentFrame.application.appNode.RemoveThemeStyleboxOverride("titlebar");
			}
		}

		inExecution = false;
		interpreter.currentFrame = null;
	}
	public void StepRun(){
		if(!inExecution){
			SetupRun();
			interpreter = new Interpreter();
			inExecution = true;
		}

		if(interpreter.currentFrame == null){
				if(rootNodes.Count != 0){
					Node start = rootNodes.First(); 
					rootNodes.RemoveAt(0);
					Functory.Expression startNodeE = InterpretNode((GraphNode) start, rootEditor);
					interpreter.currentFrame = new ExecutionFrame(startNodeE.expand(null));
					StyleBoxFlat styleBox = new StyleBoxFlat();
					styleBox.BgColor = Color.FromHtml("00BB00FF");
					styleBox.ShadowColor = Color.FromHtml("00BB00FF");
					interpreter.currentFrame.application.appNode.AddThemeStyleboxOverride("titlebar", styleBox);
				}
				else{
					inExecution = false;
					return;
				}
			}

		else{
			try{
			interpreter.currentFrame.application.appNode.RemoveThemeStyleboxOverride("titlebar");
			var res = interpreter.EvalStep();
			if(res is not ExecutionFrame){
				GD.Print(res);
				AcceptDialog accept = new AcceptDialog();
				accept.DialogText = "Resultado: " + res;
				AddChild(accept);
				accept.PopupCentered();
				if(rootNodes.Count != 0){
					Node start = rootNodes.First(); 
					rootNodes.RemoveAt(0);
					Functory.Expression startNodeE = InterpretNode((GraphNode) start, rootEditor);
					interpreter.currentFrame = new ExecutionFrame(startNodeE.expand(null));
				}
				else{
					inExecution = false;
					return;
				}
			}
			else{
				var frame = (ExecutionFrame) res;
				StyleBoxFlat styleBox = new StyleBoxFlat();
				styleBox.BgColor = Color.FromHtml("00BB00FF");
				styleBox.ShadowColor = Color.FromHtml("00BB00FF");
				frame.application.appNode.AddThemeStyleboxOverride("titlebar", styleBox);

				if(runInfoLabel.IsInsideTree()) runInfoLabel.GetParent().RemoveChild(runInfoLabel);
				frame.application.appNode.GetParent().AddChild(runInfoLabel);

				runInfoLabel.Text = String.Join("\n", interpreter.currentFrame.boundParams.Select((kp) => kp.Key.ToString() + " : " + kp.Value.result.ToString()));
				runInfoLabel.SetPosition(new Vector2(frame.application.appNode.Position.X, frame.application.appNode.Position.Y - 10));
			}
		}
		catch(Exception e){
			AcceptDialog warn = new AcceptDialog();
			warn.DialogText = "Houve um erro durante a execução: " + e.Message + ". Interrompendo execução";
			AddChild(warn);
			warn.PopupCentered();
			ResetRun();
		}
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
			resultExpr.exprNode = gn;

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

				var retNode = new Functory.Expression(new BindingOf(gn.Title),posParams,null); //BAD BAD BAD DONT USE GN.TITLE OH GOD OH FUCK ITS GONNA EXPLODE
				//TODO FIX THIS SHIT!!!!!!!!!
				//SERIOUSLY!!!!
				retNode.exprNode = gn;

				return retNode;
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

	Godot.GraphEdit.DisconnectionRequestEventHandler GetLambdaEditorDisconnectHandler(GraphEdit lambdaEditor, HashSet<GraphNode> paramNodes){
		return (StringName from_n, long from_p, StringName to_n, long to_p) => {
			var connections = lambdaEditor.GetConnectionList();
			GraphNode recv_node = (GraphNode) lambdaEditor.GetNode(to_n.ToString());
			if(paramNodes.Contains(recv_node) && recv_node.GetChildCount() != 1){
				Node theDeleterrrrr = recv_node.GetChild((int) to_p);
				recv_node.RemoveChild(theDeleterrrrr);
				GD.Print("ParamNode children: " + recv_node.GetChildCount());
				theDeleterrrrr.QueueFree();
			}
			MarkAsModified();
		};
	}

	public Godot.GraphEdit.ConnectionRequestEventHandler GetLambdaConnectionHandler(GraphEdit lambdaEditor, HashSet<GraphNode> paramNodes){   
		return (StringName from_n, long from_p, StringName to_n, long to_p) => {
			var connections = lambdaEditor.GetConnectionList();
			GraphNode recv_node = (GraphNode) lambdaEditor.GetNode(new NodePath(to_n.ToString()));
			if(paramNodes.Contains(recv_node)){
				Label lblTest = new Label();
				lblTest.Text = "";
				recv_node.AddChild(lblTest);
				recv_node.SetSlotEnabledLeft(recv_node.GetChildCount()-1, true);
			}
			MarkAsModified();
		}; 
	}

	Action GetCreateParamHandler(Function lnFunc, GraphNode lambdaNode, HashSet<GraphNode> paramNodes, GraphEdit lambdaEditor, GraphEdit.DisconnectionRequestEventHandler handleDisconnectLambda){
		return () =>{ //needless to say. add to deserialize

			string[] newParams = new string[lnFunc.parameters.Length+1];
			Array.Copy(lnFunc.parameters, newParams, lnFunc.parameters.Length);
			newParams[lnFunc.parameters.Length] = "param" + lnFunc.parameters.Length;
			GD.Print("Newparams length: " + newParams.Length);

			GraphNode paramNode = CreateParamNode(newParams[lnFunc.parameters.Length]);

			LineEdit lbdaParam = CreateParamLabel(newParams[lnFunc.parameters.Length], lambdaNode, paramNode, lnFunc.parameters.Length,lnFunc, lambdaEditor, handleDisconnectLambda);

			AddNewParamToDepNodes(lnFunc, lbdaParam.Text, newParams.Length);

			paramNodes.Add(paramNode); 
			lambdaEditor.AddChild(paramNode);

			lnFunc.parameters = newParams;

			MarkAsModified();
		};
	}

	GraphNode CreateParamNode(string paramTitle){
		GraphNode paramNode = new GraphNode();
		paramNode.Dragged += (x, y) => MarkAsModified();
		paramNode.Title = paramTitle;
		paramNode.AddChild(new Control());
		paramNode.SetSlotEnabledLeft(0, true);
		paramNode.SetSlotEnabledRight(0, true);
		paramNode.AddToGroup("LambdaParamNodes");
		rejectDeletionNodes.Add(paramNode.GetInstanceId());
		return paramNode;
	}

	LineEdit CreateParamLabel(string name, GraphNode lambdaNode, GraphNode paramNode, int idx, Function lnFunc, GraphEdit lambdaEditor, GraphEdit.DisconnectionRequestEventHandler handleDisconnectLambda){
		
		LineEdit lbdaParam = lambdaParamTemplate.Instantiate<LineEdit>();
		lambdaNode.AddChild(lbdaParam);
		lambdaNode.MoveChild(lbdaParam, -2);
		lambdaNode.SetSlotEnabledLeft(idx+2, true); //Enables the slot at the index of the last param+2, to account for the GraphEdit and label
		lbdaParam.Text = name;

		lbdaParam.TextChanged += (string text) => {
				int paramIdx = lbdaParam.GetIndex() - 2; //evil and bad copy paste
				lnFunc.parameters[paramIdx] = text;
				paramNode.Title = text;
				CleanUpDisposedNodes(lnFunc.dependentNodes);
				foreach(GraphNode depNode in lnFunc.dependentNodes){
					Label prmLabel = (Label) depNode.GetChild(paramIdx);
					prmLabel.Text = text;
				}
				MarkAsModified();
			};

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
				MarkAsModified();
			};

		return lbdaParam;
	}

	LineEdit CreateLbdaTitleEdit(Function lnFunc, GraphNode lambdaNode){
		LineEdit titleEdit = new LineEdit();

		titleEdit.TextChanged += (String text) => {
						lnFunc.name = text;
						lambdaNode.Title = text;
						UpdatePFTree();
						CleanUpDisposedNodes(lnFunc.dependentNodes);
						foreach(GraphNode depNode in lnFunc.dependentNodes){
							depNode.Title = text;
						}
						MarkAsModified();
					};
		return titleEdit;
	}

	Godot.Control.GuiInputEventHandler GetLambdaGuiInputHandler(LineEdit titleEdit, GraphNode lambdaNode, Function lnFunc){
		return (InputEvent evt) => {
			if(evt is InputEventMouseButton eventMouse){
				if(eventMouse.DoubleClick){
					
					titleEdit.Text = lnFunc.name;
					titleEdit.SetSize(new Vector2(200, titleEdit.Size.Y));
					titleEdit.SetPosition(new Vector2(lambdaNode.Position.X, lambdaNode.Position.Y-50));

					
					lambdaNode.AddSibling(titleEdit);
				}
			}
		};
	}

	void NewButtonPressed(){
		if(modified){
			var dialog = new AcceptDialog();
			dialog.AddCancelButton("Cancelar");
			dialog.AddButton("Sair sem Salvar", action: "exit_no_save");
			dialog.DialogText = "O projeto foi modificado, mas ainda não foi salvo. Salvar antes de criar novo?";

			dialog.Confirmed += ()=>{
				if(filename != null){
					Save();
					ClearProject();
					MarkAsUnmodified();
					dialog.QueueFree();
				}
				else{
					Action<bool> resultAction = (res) => {
						if(res){
							ClearProject();
							MarkAsUnmodified();
							filename = null;
						}
						dialog.QueueFree();
					};
					PickSaveFile(resultAction);
				}
			};

			dialog.CustomAction += (act) => {
				if(act == "exit_no_save"){
					ClearProject();
					MarkAsUnmodified();
				}
				dialog.QueueFree();
			};

			AddChild(dialog);
			dialog.PopupCentered();
		}
		else{
			ClearProject();
			MarkAsUnmodified();
			filename = null;
		}
	}

	void ClearProject(){
		foreach(Node n in rootEditor.GetChildren()){
			n.QueueFree();
		}
		rejectDeletionNodes.Clear();
		selectedNodes.Clear();
		fnInstanceMap.Clear();
		activeGraph = rootEditor;
		projectPackage.functions.Clear();
		MarkAsModified();
	}

	Action GetNodeDeselectedHandler(GraphNode lambdaNode, LineEdit titleEdit){
		return () => {
			if(titleEdit.IsInsideTree()) lambdaNode.GetParent().RemoveChild(titleEdit);
		}; 
	}

	void LoadButtonPressed(){

		if(modified){
			var dialog = new AcceptDialog();
			dialog.AddCancelButton("Cancelar");
			dialog.AddButton("Sair sem Salvar", action: "exit_no_save");
			dialog.DialogText = "O projeto foi modificado, mas ainda não foi salvo. Salvar antes de carregar novo?";

			dialog.Confirmed += ()=>{
				if(filename != null){
					Save();
					PickLoadFile();
					dialog.QueueFree();
				}
				else{
					PickSaveFile((saved)=>{
						if(saved){
							PickLoadFile();
							dialog.QueueFree();
						}
					});
				}
			};

			dialog.CustomAction += (act) => {
				if(act == "exit_no_save"){
					PickLoadFile();
					dialog.QueueFree();
				}
			};

			AddChild(dialog);
			dialog.PopupCentered();
		}
		else{
			PickLoadFile();
		}
	}

	void AddNewParamToDepNodes(Function lnFunc, string paramName, int prmCount){
		foreach(GraphNode depNode in lnFunc.dependentNodes){
				Label newPrmLabel = new Label();
				newPrmLabel.Text = paramName;

				if(lnFunc.parameters.Length == 0){
					Node dummy = depNode.GetChild(0);
					depNode.RemoveChild(dummy);
					dummy.QueueFree();
				}

				depNode.AddChild(newPrmLabel);
				depNode.SetSlotEnabledLeft(prmCount - 1, true);
			}
	}
	
	void MarkAsModified(){
		modified = true;
		GetWindow().Title = $"Functory - {(filename ?? "(não salvo)")} - (modificado)";
	}

	void Save() {
			ProjectRegistry reg = new ProjectRegistry();

			SerializedEditor rootEditSerialized = SerializeEditor(rootEditor, reg);

			SerializedProject serialized = new SerializedProject(rootEditSerialized, reg);

			serialized.WriteToXmlFile(filename);

			MarkAsUnmodified();
	}

	void MarkAsUnmodified(){
		modified = false;
		GetWindow().Title = $"Functory - {(filename ?? "(não salvo)")}";
	}

	void SaveButtonPressed(){
		if(filename != null){
			Save();
		}
		else{
			PickSaveFile((result)=>{});
		}
	}

	void PickSaveFile(Action<bool> saveResultAction){
		FileDialog dialog = new FileDialog();
		dialog.FileMode = FileDialog.FileModeEnum.SaveFile;
		dialog.UseNativeDialog = true;
		dialog.Access = FileDialog.AccessEnum.Filesystem;
		dialog.AddFilter("*.fty", "XML de Grafo Functory");
		dialog.CurrentFile = "novo.fty";

		dialog.FileSelected += (path) => {
			filename = path;
			GD.Print(path);
			Save();
			MarkAsUnmodified();
			saveResultAction(true);
			dialog.QueueFree();
		};

		dialog.Canceled += ()=>{
			dialog.QueueFree();
			saveResultAction(false);
		};
		AddChild(dialog);
		dialog.PopupCentered();
	}

	void PickLoadFile(){
		FileDialog fd = new FileDialog();
		fd.FileMode = FileDialog.FileModeEnum.OpenFile;
		fd.UseNativeDialog = true;
		fd.Access = FileDialog.AccessEnum.Filesystem;
		fd.AddFilter("*.fty", "XML de Grafo Functory");

		fd.FileSelected += (path) => {
			filename = path;
			var proj = SerializedProject.CreateFromXmlFile(filename);
			ClearProject();
			UnpackProjectRegistry(proj.registry);
			LoadSerializedProject(proj.rootEditor, proj.registry, rootEditor);
			MarkAsUnmodified();
			fd.QueueFree();
		};

		fd.Canceled += ()=>{
			fd.QueueFree();
		};
		AddChild(fd);
		fd.PopupCentered();
	}
	public override void _Notification(int what)
	{
		if(what == NotificationWMCloseRequest){
			if(!modified) GetTree().Quit();

			else{
				var dialog = new AcceptDialog();
				dialog.AddCancelButton("Cancelar");
				dialog.AddButton("Sair sem Salvar", action: "exit_no_save");
				dialog.DialogText = "O projeto foi modificado, mas ainda não foi salvo. Salvar antes de sair?";

				dialog.Confirmed += ()=>{
					if(filename != null){
						Save();
						dialog.QueueFree();
						GetTree().Quit();
					}
					else{
					PickSaveFile((saved)=>{
						if(saved){
							dialog.QueueFree();
							GetTree().Quit();
						}
					});
				}
				};

				dialog.CustomAction += (act) => {
					if(act == "exit_no_save"){
						GetTree().Quit();
					}
				};

				AddChild(dialog);
				dialog.PopupCentered();
			}	
		}
		else{
			base._Notification(what);
		}
	}
	public override void _Process(double delta)
	{
	}
}

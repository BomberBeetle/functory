extends Control


# Called when the node enters the scene tree for the first time.
func _ready():
	var tree = get_node("HSplitContainer/VBoxContainer2/Tree")
	
	var treeRoot = tree.create_item()
	treeRoot.set_text(0, "Todos")
	
	var it = tree.create_item()
	it.set_text(0, "Matematica")
	
	var sum = tree.create_item(it)
	sum.set_text(0, "Somar")
	var minus = tree.create_item(it)
	minus.set_text(0, "Subtrair")
	
	
	var dad = tree.create_item()
	dad.set_text(0, "Dados")
	
	var numelo = tree.create_item(dad)
	numelo.set_text(0, "Numero")
	
	var strang = tree.create_item(dad)
	strang.set_text(0, "Texto")
	
	var lisba = tree.create_item(dad)
	lisba.set_text(0, "Lista")
	
	pass # Replace with function body.


# Called every frame. 'delta' is the elapsed time since the previous frame.
func _process(delta):
	pass


func _on_graph_edit_connection_request(from_node, from_port, to_node, to_port):
		get_node("HSplitContainer/VBoxContainer/Graph").connect_node(from_node, from_port, to_node, to_port)

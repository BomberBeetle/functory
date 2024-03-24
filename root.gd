extends Node2D


# Called when the node enters the scene tree for the first time.
func _ready():
	pass # Replace with function body.


# Called every frame. 'delta' is the elapsed time since the previous frame.
func _process(delta):
	pass


func _on_graph_edit_connection_request(from_node, from_port, to_node, to_port):
		get_node("Graph").connect_node(from_node, from_port, to_node, to_port)

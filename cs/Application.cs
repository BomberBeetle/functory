using Godot;
using System;

namespace Functory.Lang{
public class Application
	{
		
	//The function which is being applied.
	Function func;
	
	//TODO: Some field here to represent the parameters being passed
	//We have a bit of a problem, though: we need to have both named and positional passing for high-order support.
	//The easy approach (with named only) would be doing a String:Parameter dict
	//The easy approach (with positional only) would just be a Parameter[] array.
	//How do we combine these two approaches?
	//Maybe something like this:
	//FreeParameters = ["name1","name2","name3"]
	//Then whenever a "name" is bound we take it off from the list
	//Or whenever a positional-argument is slotted
	//So we could have something like
	//FreeParameters = ["name1","name2","name3", "name4"]
	//Slot("name2", some parameter) -> ["name1", "name3", "name4"] named slotting
	//Slot(1, some parameter) -> ["name1", "name4"]
	//Slot() -> ["name4"]
	//What precedence these different types of slotting will have eludes me yet.
	//Maybe don't have the indexed form, only the id and paremeter-less ones.
	
	// Corresponding GraphNode in the graph.
	// Will be used in the interpreter for breakpoint/prog.eval 
	// Maybe.
	// Just do a GetParent on this sucker to get the needed GraphEdit
	GraphNode node;
	
	
	//Field: Current parameters being passed
	//This would just be a dictionary, really.
	//Free parameters can just be null.
	}
}

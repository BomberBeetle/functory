using Godot;
using System;

namespace Functory.Lang {
	public class Function {
	
		public Function(){
			
		}
	
		public Function(Application def, string[] parameters){
			this.parameters = parameters;
			this.def = def;
		}
	
		
	
		public string[] parameters;
		public Application def;
	
	
	
	
	//The definition of the function is always an application
	//A renaming, for example, would be 
	
	//rename.parameters = original.parameters
	//rename.def.func = original
	//rename.def.namedParameters = (BindingOf(parameter) for parameter in parameters)
	
	//Though, application may be used on the eval
	//Like:
	//a = 3
	//b = x + 1
	//f = (b a)
	//That would be something like
	//f = Application(b, a)
	//a = Application(integerConstant, 3)
	//b = Application(sum, a, Application(integerConstant, 1))
	//and then all of the Application gets resolved by the interpreter
	//additionally, this has the benefit of letting you use partial application
	//maybe do named parameters like
	//f = Application(f, {Param("param1", Application(something)})
	//what is worrying is how do we encode that at some point the interpreter should just eval to a certain
	//constant and such with built-ins.
	//Maybe some kind of flag with an eval() function? gotta look into this more.
	//Maybe that Constant() thing that i hypothesize later on the pattern-matching implementation
	//For pattern matching, we can have a special subset of application
	//Params = (x)
	//f 1 y= "banana"
	//f 2 y = "orange"
	//f x y = "not a fruit"
	//f = Match({Pattern({x = Constant(1), y=FreeMatch()}, Application(string, "banana")),
	// Pattern({x = Constant(2, y=FreeMatch()}, Application(string, "orange"),
	// Pattern({x = FreeMatch(), y=FreeMatch()}, Application(string, "not a fruit"))
	// }
	//)
	//This goes later, though. Pattern-matching is more of a extend goal.
	
	//The graph (main window or lambda) will get converted to this class and then ran through the evaluation protocol.
	//A "main-viable" function would be a function which takes no parameters, and as such, can be evaluated without any input.
	//Details are still fuzzy, but it's okay.
	//I'm basically just writing down stuff until it makes sense.
	
	
	}
}

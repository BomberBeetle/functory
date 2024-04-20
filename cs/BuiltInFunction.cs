using Godot;
using System;
using System.Collections.Generic;
namespace Functory.Lang {
	public abstract class BuiltInFunction : Function
		{
			public abstract object eval(Dictionary<string, Application> parameters);
		}
	
	public class Sum : BuiltInFunction{
		public Sum(){
			this.parameters = new string[]{"a", "b"};
			this.def = null;
		}
		public override object eval(Dictionary<string, Application> parameters){
			
			int a = (int) Interpreter.eval(parameters["a"]);
			int b = (int) Interpreter.eval(parameters["b"]);
			return (a + b);
		}
	}
	
	public class IntegerConstructor : BuiltInFunction {
		int value;
		public IntegerConstructor(int value){
			this.value = value;
			this.def = null;
			this.parameters = new string[]{};
		}
	
		public override object eval(Dictionary<string, Application> parameters){
			return this.value;
		}
	
	}	
	
	public class BooleanConstructor : BuiltInFunction {
		bool value;
		public BooleanConstructor(bool value){
			this.value = value;
			this.def = null;
			this.parameters = new string[]{};
		}
		
		public override object eval(Dictionary<string, Application> parameters){
			return this.value;
		}
	}
	
	public class If : BuiltInFunction {
		public If(){
			this.def = null;
			this.parameters = new string[]{"condition", "then", "else"};
		}
		
		public override object eval(Dictionary<string, Application> parameters){
			bool condition = (bool) Interpreter.eval(parameters["condition"]);
			if(condition){
				return Interpreter.eval(parameters["then"]);
			}
			else{
				return Interpreter.eval(parameters["else"]);
			}
		}
	}
	
	public class Equals : BuiltInFunction {
		public Equals(){
			this.def = null;
			this.parameters = new string[]{"a", "b"};
		}
		
		public override object eval(Dictionary<string, Application> parameters){
			object a = Interpreter.eval(parameters["a"]);
			object b = Interpreter.eval(parameters["b"]);
			return Object.Equals(a, b);
		}
	}
	
	public class Not : BuiltInFunction {
		public Not(){
			this.def = null;
			this.parameters = new string[]{"x"};
		}
		public override object eval(Dictionary<string, Application> parameters){
			bool booly = (bool) Interpreter.eval(parameters["x"]);
			return !booly;
		}
	}
}


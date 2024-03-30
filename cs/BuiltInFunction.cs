using Godot;
using System;
using System.Collections.Generic;
namespace Functory.Lang {
public abstract class BuiltInFunction : Function
	{
		public abstract object eval(Dictionary<string, object> parameters);
	}
	
public class Sum : BuiltInFunction{
		public Sum(){
			this.parameters = new string[]{"a", "b"};
			this.def = null;
		}
		public override object eval(Dictionary<string, object> parameters){
			int a = (int) parameters["a"];
			int b = (int) parameters["b"];
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
	public override object eval(Dictionary<string, object> parameters){
		return this.value;
	}
	
	}	
}



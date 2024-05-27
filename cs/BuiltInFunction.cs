using Godot;
using System;
using System.Collections.Generic;
using System.Reflection;
namespace Functory.Lang {

	public class ConstructorField : Attribute {

	}
	public abstract class BuiltInFunction : Function
		{
			public abstract object eval(Dictionary<string, Application> parameters);
			public virtual bool UpdateConstructorField(FieldInfo field, string text){
				return false;
			}
		}

	public class Sum : BuiltInFunction{
		public Sum(){
			this.parameters = new string[]{"a", "b"};
			this.def = null;
			this.name = "Somar";
		}
		public override object eval(Dictionary<string, Application> parameters){
			
			////GD.Print("BuiltIn:Sum : param a is " + parameters["a"]);
			int a = (int) Interpreter.evalTwo(parameters["a"]);
			
			int b = (int) Interpreter.evalTwo(parameters["b"]);
			
			return (a + b);
			
		}
	}
	
	public class IntegerConstructor : BuiltInFunction {
		[ConstructorField]
		public int value;

		public override bool UpdateConstructorField(FieldInfo field, string text)
		{
			if(field.Name == "value"){
				bool converted = int.TryParse(text, out value);
				return converted;
			}
			else{
				return false;
			}
		}
		public IntegerConstructor(int value){
			this.value = value;
			this.def = null;
			this.parameters = new string[]{};
			this.name = "Inteiro";
		}

		public IntegerConstructor(){
			this.def = null;
			this.parameters = new string[]{};
			this.name = "Inteiro";
		}
	
		public override object eval(Dictionary<string, Application> parameters){
			return this.value;
		}
	
	}	
	
	public class BooleanConstructor : BuiltInFunction {
		[ConstructorField]
		public bool value;

		public override bool UpdateConstructorField(FieldInfo field, string text)
		{
			if(field.Name == "value"){
				bool converted = bool.TryParse(text, out value);
				return converted;
			}
			else{
				return false;
			}
		}
		public BooleanConstructor(bool value){
			this.value = value;
			this.def = null;
			this.parameters = new string[]{};
			this.name = "Booleano";
		}
		
		public BooleanConstructor(){
			this.def = null;
			this.parameters = new string[]{};
			this.name = "Booleano";
		}
		public override object eval(Dictionary<string, Application> parameters){
			return this.value;
		}
	}
	
	public class If : BuiltInFunction {
		public If(){
			this.def = null;
			this.parameters = new string[]{"condicao", "entao", "senao"};
			this.name = "Se";
		}
		
		public override object eval(Dictionary<string, Application> parameters){
			bool condition = (bool) Interpreter.evalTwo(parameters["condicao"]);
			if(condition){
				return Interpreter.evalTwo(parameters["entao"]);
			}
			else{
				return Interpreter.evalTwo(parameters["senao"]);
			}
		}
	}
	
	public class Equals : BuiltInFunction {
		public Equals(){
			this.def = null;
			this.parameters = new string[]{"a", "b"};
			this.name = "Igual";
		}
		
		public override object eval(Dictionary<string, Application> parameters){
			object a = Interpreter.evalTwo(parameters["a"]);
			object b = Interpreter.evalTwo(parameters["b"]);
			return Object.Equals(a, b);
		}
	}
	
	public class Not : BuiltInFunction {
		public Not(){
			this.def = null;
			this.parameters = new string[]{"x"};
			this.name = "Não";
		}
		public override object eval(Dictionary<string, Application> parameters){
			bool booly = (bool) Interpreter.evalTwo(parameters["x"]);
			return !booly;
		}
	}
}


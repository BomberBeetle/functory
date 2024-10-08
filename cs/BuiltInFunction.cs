using Godot;
using System;
using System.Collections.Generic;
using System.Reflection;
namespace Functory.Lang {

	public class ConstructorField : Attribute {

	}

	public abstract class BuiltInFunction : Function
		{
			public abstract object evalProgressive(Dictionary<string, Application> boundParams);
			public abstract object eval(Dictionary<string, Application> parameters);
			public virtual bool UpdateConstructorField(FieldInfo field, string text){
				return false;
			}

			public virtual void LoadConstructorFields(Dictionary<string, object> conFields){

			}

			public virtual Dictionary<string, object> ExportConstructorFields(){
				return null;
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

		public override object evalProgressive(Dictionary<string, Application> boundParams){
			if(!boundParams.ContainsKey("a")){
				return new ParamEvaluationRequest("a");
			}

			else if(!boundParams.ContainsKey("b")){
				return new ParamEvaluationRequest("b");
			}

			else{
				return (int) boundParams["a"].result + (int) boundParams["b"].result;
			}
			
			

			/*
			yield return new ParamEvaluationRequest("a");
			int a = (int) parameter;
			yield return new ParamEvaluationRequest("b");
			int b = (int) parameter;
			yield return a+b;*/
		}
	}

	public class Subtract : BuiltInFunction{
		public Subtract(){
			this.parameters = new string[]{"a", "b"};
			this.def = null;
			this.name = "Subtrair";
		}
		public override object eval(Dictionary<string, Application> parameters){
			
			////GD.Print("BuiltIn:Sum : param a is " + parameters["a"]);
			int a = (int) Interpreter.evalTwo(parameters["a"]);
			
			int b = (int) Interpreter.evalTwo(parameters["b"]);
			
			return (a - b);
			
		}

		public override object evalProgressive(Dictionary<string, Application> boundParams){
			if(!boundParams.ContainsKey("a")){
				return new ParamEvaluationRequest("a");
			}

			else if(!boundParams.ContainsKey("b")){
				return new ParamEvaluationRequest("b");
			}

			else{
				return (int) boundParams["a"].result - (int) boundParams["b"].result;
			}
		}
	}

	public class Multiply : BuiltInFunction{
		public Multiply(){
			this.parameters = new string[]{"a", "b"};
			this.def = null;
			this.name = "Multiplicar";
		}
		public override object eval(Dictionary<string, Application> parameters){
			
			////GD.Print("BuiltIn:Sum : param a is " + parameters["a"]);
			int a = (int) Interpreter.evalTwo(parameters["a"]);
			
			int b = (int) Interpreter.evalTwo(parameters["b"]);
			
			return (a * b);
			
		}

		public override object evalProgressive(Dictionary<string, Application> boundParams){
			if(!boundParams.ContainsKey("a")){
				return new ParamEvaluationRequest("a");
			}

			else if(!boundParams.ContainsKey("b")){
				return new ParamEvaluationRequest("b");
			}

			else{
				return (int) boundParams["a"].result * (int) boundParams["b"].result;
			}
		}
	}

	public class Divide : BuiltInFunction{
		public Divide(){
			this.parameters = new string[]{"a", "b"};
			this.def = null;
			this.name = "Dividir";
		}
		public override object eval(Dictionary<string, Application> parameters){
			
			////GD.Print("BuiltIn:Sum : param a is " + parameters["a"]);
			int a = (int) Interpreter.evalTwo(parameters["a"]);
			
			int b = (int) Interpreter.evalTwo(parameters["b"]);
			
			return (a / b);
			
		}

		public override object evalProgressive(Dictionary<string, Application> boundParams){
			if(!boundParams.ContainsKey("a")){
				return new ParamEvaluationRequest("a");
			}

			else if(!boundParams.ContainsKey("b")){
				return new ParamEvaluationRequest("b");
			}

			else{
				return (int) boundParams["a"].result / (int) boundParams["b"].result;
			}
		}
	}

	public class Modulo : BuiltInFunction{
		public Modulo(){
			this.parameters = new string[]{"a", "b"};
			this.def = null;
			this.name = "Modulo";
		}
		public override object eval(Dictionary<string, Application> parameters){
			
			////GD.Print("BuiltIn:Sum : param a is " + parameters["a"]);
			int a = (int) Interpreter.evalTwo(parameters["a"]);
			
			int b = (int) Interpreter.evalTwo(parameters["b"]);
			
			return (a % b);
			
		}

		public override object evalProgressive(Dictionary<string, Application> boundParams){
			if(!boundParams.ContainsKey("a")){
				return new ParamEvaluationRequest("a");
			}

			else if(!boundParams.ContainsKey("b")){
				return new ParamEvaluationRequest("b");
			}

			else{
				return (int) boundParams["a"].result % (int) boundParams["b"].result;
			}
		}
	}

	public class Abs : BuiltInFunction{
		public Abs(){
			this.parameters = new string[]{"num"};
			this.def = null;
			this.name = "Abs";
		}
		public override object eval(Dictionary<string, Application> parameters){
			
			////GD.Print("BuiltIn:Sum : param a is " + parameters["a"]);
			int num = (int) Interpreter.evalTwo(parameters["num"]);

			return Math.Abs(num);
		}

		public override object evalProgressive(Dictionary<string, Application> boundParams){
			if(!boundParams.ContainsKey("num")){
				return new ParamEvaluationRequest("num");
			}


			else{
				return Math.Abs((int) boundParams["num"].result);
			}
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

		public override object evalProgressive(Dictionary<string, Application> boundParams){
			return this.value;
		}
		
		public override void LoadConstructorFields(Dictionary<string, object> conFields){
			this.value = (int) conFields["value"];
		}

		public override Dictionary<string,object> ExportConstructorFields(){
			return new Dictionary<string, object>{{"value", this.value}};
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

		public override object evalProgressive(Dictionary<string, Application> boundParams){
			return this.value;
		}

		public override void LoadConstructorFields(Dictionary<string, object> conFields){
			this.value = (bool) conFields["value"];
		}
		public override Dictionary<string,object> ExportConstructorFields(){
			return new Dictionary<string, object>{{"value", this.value}};
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

		public override object evalProgressive(Dictionary<string, Application> boundParams){
			
			if(!boundParams.ContainsKey("condicao")){
				return new ParamEvaluationRequest("condicao");
			}
			else{
				if((bool) boundParams["condicao"].result){
					if(boundParams.ContainsKey("entao")){
						return boundParams["entao"].result;
					}
					else return new ParamEvaluationRequest("entao");
					
				}
				else{
					if(boundParams.ContainsKey("senao")){
						return boundParams["senao"].result;
					}	
					else return new ParamEvaluationRequest("senao");
				}
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

		public override object evalProgressive(Dictionary<string, Application> boundParams){
			if(!boundParams.ContainsKey("a")){
				return new ParamEvaluationRequest("a");
			}
			else if(!boundParams.ContainsKey("b")){
				return new ParamEvaluationRequest("b");
			}
			else {
				return Object.Equals(boundParams["a"].result, boundParams["b"].result);
			}
		}
	}

	public class LargerThan : BuiltInFunction {
		public LargerThan(){
			this.def = null;
			this.parameters = new string[]{"a", "b"};
			this.name = "MaiorQue";
		}
		
		public override object eval(Dictionary<string, Application> parameters){
			object a = Interpreter.evalTwo(parameters["a"]);
			object b = Interpreter.evalTwo(parameters["b"]);
			return  (int) a >  (int) b;
		}

		public override object evalProgressive(Dictionary<string, Application> boundParams){
			if(!boundParams.ContainsKey("a")){
				return new ParamEvaluationRequest("a");
			}
			else if(!boundParams.ContainsKey("b")){
				return new ParamEvaluationRequest("b");
			}
			else {
				return (int) boundParams["a"].result >  (int) boundParams["b"].result;
			}
		}
	}

	public class LargerThanEquals : BuiltInFunction {
		public LargerThanEquals(){
			this.def = null;
			this.parameters = new string[]{"a", "b"};
			this.name = "MaiorQueOuIgual";
		}
		
		public override object eval(Dictionary<string, Application> parameters){
			object a = Interpreter.evalTwo(parameters["a"]);
			object b = Interpreter.evalTwo(parameters["b"]);
			return  (int) a >=  (int) b;
		}

		public override object evalProgressive(Dictionary<string, Application> boundParams){
			if(!boundParams.ContainsKey("a")){
				return new ParamEvaluationRequest("a");
			}
			else if(!boundParams.ContainsKey("b")){
				return new ParamEvaluationRequest("b");
			}
			else {
				return (int) boundParams["a"].result >=  (int) boundParams["b"].result;
			}
		}
	}

	public class SmallerThan : BuiltInFunction {
		public SmallerThan(){
			this.def = null;
			this.parameters = new string[]{"a", "b"};
			this.name = "MenorQue";
		}
		
		public override object eval(Dictionary<string, Application> parameters){
			object a = Interpreter.evalTwo(parameters["a"]);
			object b = Interpreter.evalTwo(parameters["b"]);
			return  (int) a < (int) b;
		}

		public override object evalProgressive(Dictionary<string, Application> boundParams){
			if(!boundParams.ContainsKey("a")){
				return new ParamEvaluationRequest("a");
			}
			else if(!boundParams.ContainsKey("b")){
				return new ParamEvaluationRequest("b");
			}
			else {
				return (int) boundParams["a"].result <  (int) boundParams["b"].result;
			}
		}
	}

	public class SmallerThanEquals : BuiltInFunction {
		public SmallerThanEquals(){
			this.def = null;
			this.parameters = new string[]{"a", "b"};
			this.name = "MenorQueOuIgual";
		}
		
		public override object eval(Dictionary<string, Application> parameters){
			object a = Interpreter.evalTwo(parameters["a"]);
			object b = Interpreter.evalTwo(parameters["b"]);
			return  (int) a <=(int) b;
		}

		public override object evalProgressive(Dictionary<string, Application> boundParams){
			if(!boundParams.ContainsKey("a")){
				return new ParamEvaluationRequest("a");
			}
			else if(!boundParams.ContainsKey("b")){
				return new ParamEvaluationRequest("b");
			}
			else {
				return (int) boundParams["a"].result <= (int) boundParams["b"].result;
			}
		}
	}

	public class Or : BuiltInFunction {
		public Or(){
			this.def = null;
			this.parameters = new string[]{"a", "b"};
			this.name = "Ou";
		}
		
		public override object eval(Dictionary<string, Application> parameters){
			bool a = (bool)Interpreter.evalTwo(parameters["a"]);
			bool b = (bool)Interpreter.evalTwo(parameters["b"]);
			return a || b;
		}

		public override object evalProgressive(Dictionary<string, Application> boundParams){
			if(!boundParams.ContainsKey("a")){
				return new ParamEvaluationRequest("a");
			}
			else if(!boundParams.ContainsKey("b")){
				return new ParamEvaluationRequest("b");
			}
			else {
				return (bool)boundParams["a"].result || (bool) boundParams["b"].result;
			}
		}
	}

	public class And : BuiltInFunction {
		public And(){
			this.def = null;
			this.parameters = new string[]{"a", "b"};
			this.name = "E";
		}
		
		public override object eval(Dictionary<string, Application> parameters){
			bool a = (bool)Interpreter.evalTwo(parameters["a"]);
			bool b = (bool)Interpreter.evalTwo(parameters["b"]);
			return a && b;
		}

		public override object evalProgressive(Dictionary<string, Application> boundParams){
			if(!boundParams.ContainsKey("a")){
				return new ParamEvaluationRequest("a");
			}
			else if(!boundParams.ContainsKey("b")){
				return new ParamEvaluationRequest("b");
			}
			else {
				return (bool)boundParams["a"].result && (bool) boundParams["b"].result;
			}
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

		public override object evalProgressive(Dictionary<string, Application> boundParams){
			if(!boundParams.ContainsKey("x")){
				return new ParamEvaluationRequest("x");
			}
			else{
				return !(bool)boundParams["x"].result;
			}
		}
	}
}


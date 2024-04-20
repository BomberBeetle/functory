using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace Functory.Lang{

public class Interpreter
{
		
	public static int resolveBindings(Application a, int offset){ //returns number of positional bindings resolved
		int positionalsUsed = 0;
		if(a.namedParams != null){
			foreach(string key in a.namedParams.Keys){
				foreach(BindingOf b in a.getUnresolvedBindings()){
					if(b.symbol == key){
						GD.Print(key);
						b.func = a.namedParams[key].func;
						b.fdef = a.namedParams[key].fdef;
						b.namedParams = a.namedParams[key].namedParams;
						b.positionalParams = a.namedParams[key].positionalParams;
						b.resolved = true;
						GD.Print("Interpreter: resolved binding with symbol " + b.symbol);
					}
				}
			}
		}
		
		if(a.positionalParams != null){
			HashSet<string> unresolvedParams = new HashSet<string>();
			
			foreach(BindingOf b in a.bindings){
				if(!b.resolved){
					unresolvedParams.Add(b.symbol);
				}
			}
			GD.Print("Interpreter: unresolved params: " + unresolvedParams.Count);
			
			string[] urParamsArray = new string[unresolvedParams.Count];
			unresolvedParams.CopyTo(urParamsArray);
			
			for(int i = 0; i < urParamsArray.Length; i++){
				bool resolvedParam = false;
				foreach(BindingOf b in a.bindings){
					if(b.symbol == urParamsArray[i]){
						GD.Print("Resolved binding with symbol " + b.symbol + " with parameter " + i);
						b.fdef = a.positionalParams[i+offset].fdef;
						b.func = a.positionalParams[i+offset].func;
						b.namedParams = a.positionalParams[i+offset].namedParams;
						b.positionalParams = a.positionalParams[i+offset].positionalParams;
						b.resolved = true;
						resolvedParam = true;
					}
				}
				if(resolvedParam) positionalsUsed++;
			}
		}
		return positionalsUsed;
	}	
		
	public static object eval(Application a){
		
		if(a is BindingOf){
			if(!((BindingOf) a).resolved){
				GD.Print("Interpreter: tried to eval unresolved BindingOf");
				return a;
			}
		}
		
		int offset = resolveBindings(a, 0);
		
		GD.Print("Interpreter: final binding count = " + a.bindings.Count);
		
		Dictionary<string, bool> paramsHandled = new Dictionary<string, bool>();
		if(a.func.parameters != null) foreach(string s in a.func.parameters){
			paramsHandled.Add(s, false);
		}
		
		Dictionary<string, Application> parameters = new Dictionary<string, Application>();
		
		if(a.namedParams != null) {
			foreach(string k in a.namedParams.Keys){
				GD.Print("Interpreter: handling param " + k);
				parameters.Add(k, a.namedParams[k]);
				//defer evaling to builtins
				if(paramsHandled.ContainsKey(k)){
					paramsHandled[k] = true;
				}
			}
		}
		
		if(a.positionalParams != null){
			SortedSet<string> resolvedParams = new SortedSet<string>();
			foreach(BindingOf b in a.bindings){
				if(b.resolved){
					resolvedParams.Add(b.symbol);
				}
			}
			
			foreach(string k in paramsHandled.Keys){
				if(!paramsHandled[k]) resolvedParams.Add(k);
			}
			
			string[] urpsArray = new string[resolvedParams.Count];
			resolvedParams.CopyTo(urpsArray);
			for(int i = 0; i < a.positionalParams.Length; i++){
				GD.Print("Interpreter: Added positional param " + i + " with symbol " + urpsArray[i]);
				parameters.Add(urpsArray[i], a.positionalParams[i]);
			}
		}
		GD.Print("Interpreter: Resulting Params:");
		GD.Print(string.Join(System.Environment.NewLine, parameters));
		
		//then the positional/ params...
		//EVAL (resolved?) PARAMETERS AND ADD THEM TO THE PARAMETERS DICTIONARY
		GD.Print("Interpreter: eval func type is " + a.func);
		
		if(a.func is BuiltInFunction){
			GD.Print("Interpreter: eval func type is builtin");
			if(!a.func.parameters.Except(parameters.Keys).Any() && a.getUnresolvedBindings().Count == 0){ 
				//paremeters contains all parameters of func
				//ALSO CHECK IF THE BINDINGS ARE RESOLVED!!!! OTHERWISE ITS GOING TO SHIT ITSELF
				GD.Print("Interpreter: about to eval builtin");
				GD.Print(a.func);
				return ((BuiltInFunction)a.func).eval(parameters);
			}
			else{
				//return Application with filled in parameters
				//or maybe new function using this  application?
				//we'll see
				return a;
			}
			
		}
		else{
			return eval(a.fdef);
		}
	}	
}

}

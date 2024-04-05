using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace Functory.Lang{

public class Interpreter
{
		
	public static void resolveBindings(Application a){
		GD.Print(a.bindings);
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
						GD.Print("resolved binding with symbol " + b.symbol);
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
			
			string[] urParamsArray = new string[unresolvedParams.Count];
			unresolvedParams.CopyTo(urParamsArray);
			
			for(int i = 0; i < a.positionalParams.Length; i++){
				foreach(BindingOf b in a.bindings){
					if(b.symbol == urParamsArray[i]){
						b.fdef = a.positionalParams[i].fdef;
						b.func = a.positionalParams[i].func;
						b.namedParams = a.positionalParams[i].namedParams;
						b.positionalParams = a.positionalParams[i].positionalParams;
						b.resolved = true;
					}
				}
			}
		}
	}	
		
	public static object eval(Application a){
		
		if(a is BindingOf){
			if(!((BindingOf) a).resolved){
				return a;
			}
		}
		
		resolveBindings(a);
		if(a.fdef is BindingOf){
			BindingOf defb = (BindingOf) a.fdef;
			foreach(string k in defb.namedParams.Keys){
				if(defb.namedParams[k] is BindingOf){
					a.bindings.Add((BindingOf)defb.namedParams[k]);
				}
			}
			resolveBindings(a);
		}
		
		Dictionary<string, object> parameters = new Dictionary<string, object>();
		if(a.namedParams != null) {
			foreach(string k in a.namedParams.Keys){
				GD.Print("handling param " + k);
				parameters.Add(k, eval(a.namedParams[k]));
				GD.Print(k + " evald to " + parameters[k]);
			}
		}
		
		if(a.positionalParams != null){
			HashSet<string> urps = new HashSet<string>();
			foreach(BindingOf b in a.bindings){
				if(!b.resolved){
					urps.Add(b.symbol);
				}
			}
			string[] urpsArray = new string[urps.Count];
			urps.CopyTo(urpsArray);
			for(int i = 0; i < a.positionalParams.Length; i++){
				parameters.Add(urpsArray[i], eval(a.positionalParams[i]));
			}
		}
		
		//then the positional params...
		//EVAL (resolved?) PARAMETERS AND ADD THEM TO THE PARAMETERS DICTIONARY
		
		if(a.func is BuiltInFunction){
			
			if(!a.func.parameters.Except(parameters.Keys).Any() && a.getUnresolvedBindings().Count == 0){ 
				//paremeters contains all parameters of func
				//ALSO CHECK IF THE BINDINGS ARE RESOLVED!!!! OTHERWISE ITS GOING TO SHIT ITSELF
				GD.Print("about to eval builtin");
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

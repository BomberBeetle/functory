using System;
using System.Collections.Generic;
using System.Linq;

namespace Functory.Lang{

public class Interpreter
{
		
	public static object eval(Application a){
		
		if(a is BindingOf){
			if(!((BindingOf) a).resolved){
				return a;
			}
		}
		
		if(a.namedParams != null){
			foreach(string key in a.namedParams.Keys){
				foreach(BindingOf b in a.bindings){
					if(b.symbol == key){
						b.func = a.namedParams[key].func;
						b.namedParams = a.namedParams[key].namedParams;
						b.positionalParams = a.namedParams[key].positionalParams;
						b.resolved = true;
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
						b.func= a.positionalParams[i].func;
						b.namedParams = a.positionalParams[i].namedParams;
						b.positionalParams = a.positionalParams[i].positionalParams;
						b.resolved = true;
					}
				}
			}
		}
		
		//resolve whatever bindings we can
		//check for named parameters first
		//then try to fill in unfulfilled ones with positoinal params\
		
		//then add the resolved ones to parameters
		//we'll probably need some else there
		
		Dictionary<string, object> parameters = new Dictionary<string, object>();
		foreach(string k in a.namedParams){
			parameters.Add(k, eval(a.namedParams[k]));
		}
		//then the positional params...
		//EVAL (resolved?) PARAMETERS AND ADD THEM TO THE PARAMETERS DICTIONARY
		
		if(a.func is BuiltInFunction){
			
			
			if(!a.func.parameters.Except(parameters.Keys).Any() && a.getUnresolvedBindings().Count == 0){ 
				//paremeters contains all parameters of func
				//ALSO CHECK IF THE BINDINGS ARE RESOLVED!!!! OTHERWISE ITS GOING TO SHIT ITSELF
				
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
			//same hat as the else really
		}
		return null;
	}	
}

}

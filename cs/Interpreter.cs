using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace Functory.Lang{

public class Interpreter
{
	public ExecutionFrame currentFrame;

	public object EvalStep(){
		if(currentFrame.application.result != null){
			//GD.Print("currentFrame result not null; passing to parent or returning");
			if(currentFrame.parent != null){
				currentFrame.parent.valueReceived = currentFrame.application.result;
				currentFrame = currentFrame.parent;
				return currentFrame;
			}
			else{
			return currentFrame.application.result;
			}
		}
		
		if(currentFrame.application.func is BuiltInFunction bfct){
			//GD.Print("currentFrame app is BuiltIn;");
			var fulfillableParams = (currentFrame.application.namedParams!=null?currentFrame.application.func.parameters.Count((par)=>currentFrame.application.namedParams.ContainsKey(par)):0) + (currentFrame.application.positionalParams!=null?currentFrame.application.positionalParams.Length:0);
			if(fulfillableParams >= currentFrame.application.func.parameters.Length){
				object res = null;

				res = bfct.evalProgressive(currentFrame.boundParams);

				if(res is ParamEvaluationRequest request){
					Application a = null;
					//GD.Print("builtin returned evaluation request for symbol " + request.ParamName);

					if(currentFrame.matchedParams.ContainsKey(request.ParamName)){
						a = currentFrame.matchedParams[request.ParamName];
						currentFrame.boundParams.Add(request.ParamName, a);
					}

					else{
						throw new Exception("Could not find appropriate parameter for ParamEvalRequest in matchedParams");
					}

					ExecutionFrame exFr = new ExecutionFrame(a, currentFrame);
					currentFrame = exFr;
					return exFr;
				}
				else{
					//GD.Print("builtin returned result: " + res);
					if(currentFrame.parent != null){
						currentFrame.parent.valueReceived = res;
						currentFrame.application.result = res;
						currentFrame = currentFrame.parent;
						return currentFrame;
					}
					else{
						return res;
					}
				}

			}
			else{
				if(currentFrame.parent != null){
					currentFrame.parent.valueReceived = currentFrame.application;
					currentFrame.application.result = currentFrame.application;
					currentFrame = currentFrame.parent;
					return currentFrame;
				}
				else{
					return currentFrame.application;
				}
			}
		}

		if(currentFrame.valueReceived != null){
			if(currentFrame.lastParamChild != null){ //Receiving from param
				//GD.Print("Frame receiving value from param " + currentFrame.lastParamSymbol);
				currentFrame.boundParams.Add(currentFrame.lastParamSymbol, currentFrame.lastParamChild.application);
				currentFrame.lastParamChild = null;
				currentFrame.lastParamSymbol = null;
				currentFrame.valueReceived = null;
			}
			else if(currentFrame.parent != null) { //Receiving from def
				//GD.Print("Frame receiving value from def, passing to parent");
				currentFrame.application.result = currentFrame.valueReceived;
				currentFrame.parent.valueReceived = currentFrame.valueReceived;
				currentFrame.valueReceived = null;
				currentFrame = currentFrame.parent;
				return currentFrame;
			}
			else{
				//GD.Print("Frame receiving value from def, returning");
				object value = currentFrame. valueReceived;
				currentFrame.valueReceived = null;
				return value;
			}
		}

		if(currentFrame.application.func.parameters.All((prm) => currentFrame.boundParams.ContainsKey(prm))){
			//GD.Print("All params bound, passing to def");
			ExecutionFrame defFrame = new ExecutionFrame(currentFrame.application.func.def.expand(currentFrame.boundParams), currentFrame);
			currentFrame = defFrame;
			return currentFrame;
		}

		if(currentFrame.application.namedParams != null) 
		if(!currentFrame.application.namedParams.Keys.All((key)=>currentFrame.boundParams.ContainsKey(key))){
			var firstUnboundNamed = currentFrame.application.namedParams.First((pair)=>!currentFrame.boundParams.ContainsKey(pair.Key));
			ExecutionFrame exFr = new ExecutionFrame(firstUnboundNamed.Value, currentFrame);
			currentFrame.lastParamChild = exFr;
			currentFrame.lastParamSymbol = firstUnboundNamed.Key;
			currentFrame = exFr;
			//GD.Print("Trying to evaluate named param " + currentFrame.lastParamSymbol);
			return currentFrame;
		}

		if(currentFrame.application.positionalParams != null) 
		if(currentFrame.pprmsBound != currentFrame.application.positionalParams.Length){
			var firstUnboundPositional = currentFrame.application.positionalParams[currentFrame.pprmsBound];
			currentFrame.pprmsBound += 1;
			ExecutionFrame exFr = new ExecutionFrame(firstUnboundPositional, currentFrame);
			currentFrame.lastParamChild = exFr;
			currentFrame.lastParamSymbol = currentFrame.application.func.parameters[currentFrame.boundParams.Count];
			currentFrame = exFr;
			//GD.Print("Trying to evaluate positional param " + currentFrame.boundParams.Count);
			return currentFrame;
		}

		if(currentFrame.parent != null){
				//GD.Print("Params not fulfilled, return application to parent");
				currentFrame.parent.valueReceived = currentFrame.application;
				currentFrame = currentFrame.parent;
				return currentFrame;
		}
		else{
				//GD.Print("Params not fulfilled, return application");
				return currentFrame.application;
			}
		}




	public static object evalTwo(Application a, Dictionary<string, Application> namedStacked=null, Application[] positionalStacked=null){
		
		Dictionary<string, Application> newStacked = new Dictionary<string, Application>();
		List<Application> newPStacked = new List<Application>();

		Dictionary<string, Application> boundParams = new Dictionary<string, Application>();


		//1. Resolve params
		List<string> unresolvedParams = new List<string>();
		foreach(string prm in a.func.parameters){
			unresolvedParams.Add(prm);				
		}

		if(a.namedParams != null){
			foreach(string key in a.namedParams.Keys){
				bool removed = unresolvedParams.Remove(key);
				if(!removed){
					newStacked.Add(key, a.namedParams[key]);
				}
				else{
					boundParams.Add(key, a.namedParams[key]);
				}
			}
		}

		if(a.positionalParams != null){
			int positionalsResolved = 0;
			for(int i = 0; i < a.positionalParams.Length && i < unresolvedParams.Count; i++){
				boundParams.Add(unresolvedParams[i], a.positionalParams[i]);
				positionalsResolved = i + 1;
				//GD.Print("Bound positional " + i + " to symbol " + unresolvedParams[i]);
			}
			unresolvedParams.RemoveRange(0, positionalsResolved);
			newPStacked.AddRange(a.positionalParams.Skip(positionalsResolved));
		}

		if(namedStacked != null){
			foreach(string key in namedStacked.Keys){
				bool removed = unresolvedParams.Remove(key);
				if(!removed){
					newStacked.Add(key, namedStacked[key]);
				}
				else{
					boundParams.Add(key, namedStacked[key]);
				}
			}
		}

		if(positionalStacked != null){
			int positionalsResolved = 0;
			for(int i = 0; i < positionalStacked.Length && i < unresolvedParams.Count; i++){
				boundParams.Add(unresolvedParams[i], positionalStacked[i]);
				positionalsResolved = i + 1;
			}
			unresolvedParams.RemoveRange(0, positionalsResolved);
			newPStacked.AddRange(positionalStacked.Skip(positionalsResolved));
		}



		if(unresolvedParams.Count != 0){
			//GD.Print("ev2: Returning Application due to unresolved params");
			//GD.Print("ev2: final unresolved params are " + String.Join(", ", unresolvedParams));
			return a;
		}
		else{
			if(a.func is BuiltInFunction){
				return ((BuiltInFunction)a.func).eval(boundParams);
			}
			else{
				//GD.Print("ev2: Going to evaluate def");
				return evalTwo(a.func.def.expand(boundParams), newStacked, newPStacked.ToArray());
			}
		}

	}	



	/*
	public static Tuple<Dictionary<string, Application>, int> resolveBindings(Application a, Dictionary<string, Application> namedStacked, Application[] positionalStacked){ //returns number of positional bindings resolved
		int positionalsUsed = 0;
		Dictionary<string, Application> unusedNamedParams = new Dictionary<string,Application>();
		
		if(namedStacked != null){
				foreach(string key in namedStacked.Keys){
					bool keyUsed = false;
					foreach(BindingOf b in a.getUnresolvedBindings()){
						if(b.symbol == key){
							//GD.Print(key);
							b.func = namedStacked[key].func;
							b.fdef = namedStacked[key].fdef;
							b.namedParams = namedStacked[key].namedParams;
							b.positionalParams = namedStacked[key].positionalParams;
							b.resolved = true;
							keyUsed = true;
							//GD.Print("Interpreter: (stacked/named) resolved binding with symbol " + b.symbol);
						}
					}
					if(!keyUsed) unusedNamedParams.Add(key, namedStacked[key]);
					
				}
			}
		
		if(a.namedParams != null){
			foreach(string key in a.namedParams.Keys){
				bool keyUsed = false;
				foreach(BindingOf b in a.getUnresolvedBindings()){
					if(b.symbol == key){
						//GD.Print(key);
						b.func = a.namedParams[key].func;
						b.fdef = a.namedParams[key].fdef;
						b.namedParams = a.namedParams[key].namedParams;
						b.positionalParams = a.namedParams[key].positionalParams;
						b.resolved = true;
						keyUsed = true;
						//GD.Print("Interpreter: (named) resolved binding with symbol " + b.symbol + " with app with func =" + a.namedParams[key].func);
					}
				}
				if(!keyUsed) unusedNamedParams.Add(key, a.namedParams[key]);
			}
			
		}
		
		
		
		if(a.positionalParams != null){
			HashSet<string> unresolvedParams = new HashSet<string>();
			
			foreach(BindingOf b in a.bindings){
				if(!b.resolved){
					unresolvedParams.Add(b.symbol);
				}
			}
			//GD.Print("Interpreter: unresolved params after named resolving: " + unresolvedParams.Count);
			
			string[] urParamsArray = new string[unresolvedParams.Count];
			unresolvedParams.CopyTo(urParamsArray);
			
			for(int i = 0; i < urParamsArray.Length && i < a.positionalParams.Length; i++){
				bool resolvedParam = false;
				foreach(BindingOf b in a.bindings){
					if(b.symbol == urParamsArray[i]){
						//GD.Print("Interpreter: (positional) Resolved binding with symbol " + b.symbol + " with parameter " + i);
						b.fdef = a.positionalParams[i].fdef;
						b.func = a.positionalParams[i].func;
						b.namedParams = a.positionalParams[i].namedParams;
						b.positionalParams = a.positionalParams[i].positionalParams;
						b.resolved = true;
						resolvedParam = true;
					}
				}
				if(resolvedParam) positionalsUsed++;
			}
			
		}
		return Tuple.Create(unusedNamedParams, positionalsUsed);
	}	
		
	public static object eval(Application a, Dictionary<string, Application> stackedNamedParams = null, Application[] stackedPositionalParams = null){
		Application.instanceBindingCopies(a, null);
		if(a is BindingOf){
			if(!((BindingOf) a).resolved){
				//GD.Print("Interpreter: tried to eval unresolved BindingOf");
				return a;
			}
		}
		
		if(stackedPositionalParams != null && a.positionalParams != null){
			Application[] newPPrms = new Application[a.positionalParams.Length+stackedPositionalParams.Length];
			Array.Copy(a.positionalParams, 0, newPPrms, 0, a.positionalParams.Length);
			Array.Copy(stackedPositionalParams, 0, newPPrms, a.positionalParams.Length, stackedPositionalParams.Length);
		}
		else if(stackedPositionalParams != null && a.positionalParams == null){
			a.positionalParams = stackedPositionalParams;
		}
		
		
		(Dictionary<string, Application> unusedNamedParams, int usedPositionalParams) = resolveBindings(a, stackedNamedParams, stackedPositionalParams);
		
		//GD.Print("Interpreter: final binding count = " + a.bindings.Count);
		
		Dictionary<string, bool> paramsHandled = new Dictionary<string, bool>();
		if(a.func.parameters != null) 
			foreach(string s in a.func.parameters){
				paramsHandled.Add(s, false);
			}
		
		Dictionary<string, Application> parameters = new Dictionary<string, Application>();
		
		if(a.namedParams != null) {
			foreach(string k in a.namedParams.Keys){
				//GD.Print("Interpreter: handling param " + k);
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
			for(int i = 0; i < a.positionalParams.Length && i < urpsArray.Length; i++){
				//GD.Print("Interpreter: Added positional param " + i + " with symbol " + urpsArray[i]);
				parameters.Add(urpsArray[i], a.positionalParams[i]);
			}
		}
		//GD.Print("Interpreter: Resulting Params:");
		//GD.Print(string.Join(System.Environment.NewLine, parameters));
		
		//then the positional/ params...
		//EVAL (resolved?) PARAMETERS AND ADD THEM TO THE PARAMETERS DICTIONARY
		//GD.Print("Interpreter: eval func type is " + a.func);
		
		if(a.func is BuiltInFunction){
			//GD.Print("Interpreter: eval func type is builtin");
			if(!a.func.parameters.Except(parameters.Keys).Any() && a.getUnresolvedBindings().Count == 0){ 
				//paremeters contains all parameters of func
				//ALSO CHECK IF THE BINDINGS ARE RESOLVED!!!! OTHERWISE ITS GOING TO SHIT ITSELF
				//GD.Print("Interpreter: about to eval builtin");
				//GD.Print(a.func);
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
			Application[] stackedParams = null;
			if(a.positionalParams != null){ 
				int paramsToPass = a.positionalParams.Length;
				stackedParams = new Application[paramsToPass];
				Array.Copy(a.positionalParams, a.positionalParams.Length-paramsToPass, stackedParams, 0, paramsToPass);
			}
			return eval(a.fdef, unusedNamedParams, stackedParams);
		}
	}	
	*/
}

}

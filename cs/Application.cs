using Godot;
using System;
using System.Collections.Generic;

namespace Functory.Lang{
public class Application
	{
		
	public Application(Function func, Dictionary<string, Application> namedParams, Application[] positionalParams){
		
		//At a technical level, we have to make a copy of each function for each Application
		//To be able to directly access the bindings related to the function def when substituting for parameters on the interpreter
		//Not a pretty solution to be sure, but right now it's the only way I can think of and this things
		//gotta be out of the door pretty soon.
		
		if(func == null) return;
		
		bindings = new List<BindingOf>();
		
		Function fcopy = new Function();
		
		if(func.def is BindingOf){
			BindingOf binddef = (BindingOf) func.def;
			
			BindingOf newbind = new BindingOf(binddef.symbol);
			fcopy.def = newbind;
			
			bindings.Add(newbind);	
		}
		
		else{
			fcopy.def = new Application(func.def.func, func.def.namedParams, func.def.positionalParams);
		}
		
		resolveParams(fcopy.def, bindings);
		
		this.fdef = fcopy.def;
	}
	
	public static void resolveParams(Application a, List<BindingOf> bindingList){
		if(a.namedParams != null){
			Dictionary<string, Application> nparams = new Dictionary<string, Application>();
			foreach(string key in a.namedParams.Keys){
				Application newAp;
				if(a.namedParams[key] is BindingOf){
					BindingOf b = (BindingOf) a.namedParams[key];
					BindingOf newPrm =  new BindingOf(b.symbol);
					bindingList.Add(newPrm);
					newAp = newPrm;
				}
				else{
					Application oldAp = a.namedParams[key];
					newAp = new Application(oldAp.func, oldAp.namedParams, oldAp.positionalParams);
				}
				nparams.Add(key, newAp);
				resolveParams(newAp, bindingList);
			}
			
			a.namedParams = nparams;
		}
		
		if(a.positionalParams != null){
			Application[] posPrms = new Application[a.positionalParams.Length];
			for(int i = 0; i < a.positionalParams.Length; i++){
				Application newAp;
				if(a.positionalParams[i] is BindingOf){
					BindingOf b = (BindingOf) a.positionalParams[i];
					BindingOf newPrm =  new BindingOf(b.symbol);
					bindingList.Add(newPrm);
					newAp = newPrm;
				}
				else{
					Application oldAp = a.positionalParams[i];
					newAp = new Application(oldAp.func, oldAp.namedParams, oldAp.positionalParams);
				}
				posPrms[i] = newAp;
				resolveParams(newAp, bindingList);
			}
		}
		
		
	}
	
	//A private copy of the applied function's definition which stores its bindings locally.
	private Application fdef;
	
	//The function which is being applied.
	public Function func;
	
	public Dictionary<string, Application> namedParams; //Used for the normal cable connecting
	
	public Application[] positionalParams; //Used for high-order passing cases where we don't have parameter names.
	
	//An Application without any parameters resolves to it's func
	//Partial application is also possible.
	
	public List<BindingOf> bindings;
	
	// Corresponding GraphNode in the graph.
	// Will be used in the interpreter for breakpoint/prog.eval 
	// Just do a GetParent on this sucker to get the needed GraphEdit
	public GraphNode node;
	
	public List<BindingOf> getUnresolvedBindings(){
		List<BindingOf> unresolvedBindings = new List<BindingOf>();
		foreach(BindingOf b in this.bindings){
			if(!b.resolved){
				unresolvedBindings.Add(b);
			}
		}
		return unresolvedBindings;
	}
	
	}
}

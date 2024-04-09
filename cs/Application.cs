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
		
		this.func = func;
		this.namedParams = namedParams;
		this.positionalParams = positionalParams;
		
		instanceBindingCopies(this);
	}
	
	public static List<BindingOf> instanceBindingCopies(Application a){
		List<BindingOf> bindingList = new List<BindingOf>();
		if(a.func != null){
			GD.Print("Application: Trying to instance binding copies for application with func =" + a.func);
			Function fcopy = new Function();
			if(a.func.def != null){
				if(a.func.def is BindingOf){
					BindingOf b = (BindingOf) a.func.def;
					BindingOf newPrm =  new BindingOf(b.symbol, b.func, b.namedParams, b.positionalParams);
					bindingList.Add(newPrm);
					fcopy.def = newPrm;
				}
				else
				{
					fcopy.def = new Application(a.func.def.func, a.func.def.namedParams, a.func.def.positionalParams);
				}
				
				bindingList.AddRange(instanceBindingCopies(fcopy.def));
				
			}
			
			a.fdef = fcopy.def;
		}
		
		if(a.namedParams != null){
			Dictionary<string, Application> nparams = new Dictionary<string, Application>();
			foreach(string key in a.namedParams.Keys){
				Application newAp;
				if(a.namedParams[key] is BindingOf){
					BindingOf b = (BindingOf) a.namedParams[key];
					BindingOf newPrm =  new BindingOf(b.symbol, b.func, b.namedParams, b.positionalParams);
					bindingList.Add(newPrm);
					newAp = newPrm;
				}
				else{
					Application oldAp = a.namedParams[key];
					newAp = new Application(oldAp.func, oldAp.namedParams, oldAp.positionalParams);
				}
				nparams.Add(key, newAp);
				bindingList.AddRange(instanceBindingCopies(newAp));
			}
			a.namedParams = nparams;
		}
		else {
			//GD.Print("resolveParams: named params null");
		}
		
		if(a.positionalParams != null){
			GD.Print("Application: now generating binding instances for pos params");
			Application[] posPrms = new Application[a.positionalParams.Length];
			for(int i = 0; i < a.positionalParams.Length; i++){
				
				Application newAp;
				if(a.positionalParams[i] is BindingOf){
					BindingOf b = (BindingOf) a.positionalParams[i];
					BindingOf newPrm =  new BindingOf(b.symbol, b.func, b.namedParams, b.positionalParams);
					bindingList.Add(newPrm);
					newAp = newPrm;
				}
				else{
					Application oldAp = a.positionalParams[i];
					newAp = new Application(oldAp.func, oldAp.namedParams, oldAp.positionalParams);
				}
				posPrms[i] = newAp;
				bindingList.AddRange(instanceBindingCopies(newAp));
			}
			a.positionalParams = posPrms;
		}
		else{
			
		}
		
		a.bindings = bindingList;
		
		return bindingList;
		
	}
	
	//A private copy of the applied function's definition which stores its bindings locally.
	public Application fdef;
	
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

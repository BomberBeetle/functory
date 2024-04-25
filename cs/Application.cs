using Godot;
using System;
using System.Collections.Generic;

namespace Functory.Lang{
public class Application
	{
	
	//A private copy of the applied function's definition which stores its bindings locally.
	public Application fdef;
	
	//A reference to the "original" application, if it is a copy made by another.
	public Application originalApplication;
	
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
	
	
		
	public Application(Function func, Dictionary<string, Application> namedParams, Application[] positionalParams, bool instanceCopies = true, Application originalApplication=null){
		
		this.func = func;
		this.namedParams = namedParams;
		this.positionalParams = positionalParams;
		this.bindings = new List<BindingOf>();
		
		
		if(this.originalApplication == null){
			this.originalApplication = this;
		}
		
		if(instanceCopies) instanceBindingCopies(this, new List<Application>());
	}
	
	public static void instanceBindingCopies(Application a, List<Application> objectsInstanced){
		
		List<BindingOf> bindingList = new List<BindingOf>();
		/*
		if(objectsInstanced.Contains(a.originalApplication)){
			GD.Print("Application: App for " +a.originalApplication.GetHashCode() + " skipped.");
			return;
		}
		else if(a.originalApplication != null){
			objectsInstanced.Add(a.originalApplication);
			GD.Print("Application: App for " + a.originalApplication.GetHashCode() + " added to instance list.");
		}
		*/
		if(a.func != null){
			GD.Print("Application: Trying to instance binding copies for application with func" + a.func.name);
			Function fcopy = new Function();
			if(a.func.def != null ){
				GD.Print("Application:  func def original hashcode is " + a.func.def.originalApplication.GetHashCode());
				if(a.func.def is BindingOf){
					BindingOf b = (BindingOf) a.func.def;
					BindingOf newPrm =  new BindingOf(b.symbol, b.func, b.namedParams, b.positionalParams, false, a.func.def.originalApplication);
					bindingList.Add(newPrm);
					fcopy.def = newPrm;
				}
				else
				{
					fcopy.def = new Application(a.func.def.func, a.func.def.namedParams, a.func.def.positionalParams, false, a.func.def.originalApplication);
				}
				
				//instanceBindingCopies(fcopy.def, new List<Application>(objectsInstanced)); //Something devious draws near...
				//bindingList.AddRange(fcopy.def.getUnassignedBindings());
				
			}
			else{
				
			}
			
			a.fdef = fcopy.def;
		}
		
		if(a.namedParams != null){
			Dictionary<string, Application> nparams = new Dictionary<string, Application>();
			foreach(string key in a.namedParams.Keys){
				//if(!objectsInstanced.Contains(a.namedParams[key].originalApplication)){
					Application newAp;
					if(a.namedParams[key] is BindingOf){
						BindingOf b = (BindingOf) a.namedParams[key];
						BindingOf newPrm =  new BindingOf(b.symbol, b.func, b.namedParams, b.positionalParams, false, a.namedParams[key].originalApplication);
						bindingList.Add(newPrm);
						newAp = newPrm;
					}
					else{
						Application oldAp = a.namedParams[key];
						newAp = new Application(oldAp.func, oldAp.namedParams, oldAp.positionalParams, false, a.namedParams[key].originalApplication);
					}
					nparams.Add(key, newAp);
					GD.Print("Application: newAp original hashcode is " + newAp.originalApplication.GetHashCode());
					//instanceBindingCopies(newAp, new List<Application>(objectsInstanced));
					//bindingList.AddRange(newAp.getUnassignedBindings());
				//} 
			}
			a.namedParams = nparams;
		}
		else {
			//GD.Print("resolveParams: named params null");
		}
		
		if(a.positionalParams != null){
			GD.Print("Application: now generating binding instances for pos params on " + a.GetHashCode());
			Application[] posPrms = new Application[a.positionalParams.Length];
			for(int i = 0; i < a.positionalParams.Length; i++){
				//if(!objectsInstanced.Contains(a.positionalParams[i].originalApplication)){
					Application newAp;
					if(a.positionalParams[i] is BindingOf){
						BindingOf b = (BindingOf) a.positionalParams[i];
						BindingOf newPrm =  new BindingOf(b.symbol, b.func, b.namedParams, b.positionalParams, false, a.positionalParams[i].originalApplication);
						bindingList.Add(newPrm);
						newAp = newPrm;
					}
					else{
						Application oldAp = a.positionalParams[i];
						newAp = new Application(oldAp.func, oldAp.namedParams, oldAp.positionalParams, false, a.positionalParams[i].originalApplication);
					}
					posPrms[i] = newAp;
					GD.Print("Application: newAp original hashcode is " + newAp.originalApplication.GetHashCode());
					//instanceBindingCopies(newAp, new List<Application>(objectsInstanced));
					//bindingList.AddRange(newAp.getUnassignedBindings());
				//}
			}
			a.positionalParams = posPrms;
		}
		else{
			
		}
		
		a.bindings = bindingList;
		
	}
	
	public void updateNamedParams(Dictionary<string, Application> newParams, bool instanceCopies = true){
		this.namedParams = newParams;
		//if(instanceCopies) instanceBindingCopies(this, new List<Application>());
	}
	
	public  void updatePositionalParams(Application[] newParams, bool instanceCopies = true){
		this.positionalParams = newParams;
		//if(instanceCopies) instanceBindingCopies(this, new List<Application>());
	}

	
	public List<BindingOf> getUnresolvedBindings(){
		List<BindingOf> unresolvedBindings = new List<BindingOf>();
		foreach(BindingOf b in this.bindings){
			if(!b.resolved){
				unresolvedBindings.Add(b);
			}
		}
		return unresolvedBindings;
	}
	
	public List<BindingOf> getUnassignedBindings(){
		//do the code to map to parameters here. only add bindings that are not mapped to a parameter instead
		//actually, i think the error is in here: any BindingOfn will be passed to a parent application.
		//we need to actually see if the binding is unassigned before adding it to the parent.
		List<BindingOf> unassignedBindings = new List<BindingOf>();
		
		SortedSet<string> unassignedSymbols = new SortedSet<string>();
		foreach(BindingOf b in bindings){
			unassignedSymbols.Add(b.symbol);
		}
		
		string[] uns = new string[unassignedSymbols.Count];
		unassignedSymbols.CopyTo(uns);
		
		if(this.namedParams != null)
		foreach(string k in uns){
			if(this.namedParams.ContainsKey(k)){
				if(this.namedParams[k] is BindingOf){
					BindingOf bprm = (BindingOf) this.namedParams[k];
					if(bprm.symbol != k){
						unassignedSymbols.Remove(k);
					}
				}
				else{
					unassignedSymbols.Remove(k);
				}
			}
			
		}
		
		string[] uSymbolArray = new string[unassignedSymbols.Count];
		unassignedSymbols.CopyTo(uSymbolArray);
		
		int ppStart = 0;
		
		if(this.positionalParams != null) ppStart = this.positionalParams.Length;
		
		for(int i = ppStart; i < uSymbolArray.Length; i++){
			foreach(BindingOf b in bindings){
				if(b.symbol == uSymbolArray[i]){
					unassignedBindings.Add(b);
				}
			}
		}
		
		return unassignedBindings;
	}
	
	
	}
}

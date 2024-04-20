using Godot;
using System;
using System.Collections.Generic;
namespace Functory.Lang {
	public class BindingOf : Application {
		public string symbol;
		public bool resolved; 
		public BindingOf(string symbol, Function func = null, Dictionary<string, Application> namedParams = null, Application[] positionalParams = null, bool instanceCopies=true, Application originalAp=null):base(func, namedParams, positionalParams,instanceCopies,originalAp){
			this.symbol = symbol;
		}
		
	}
}

using Godot;
using System;
using System.Collections.Generic;
namespace Functory.Lang {
	public class BindingOf : Application {
		public string symbol;
		public bool resolved; 
		public BindingOf(string symbol, Function func = null, Dictionary<string, Application> namedParams = null, Application[] positionalParams = null):base(func, namedParams, positionalParams){
			this.symbol = symbol;
		}
		
	}
}

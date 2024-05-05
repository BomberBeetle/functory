using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
namespace Functory.Lang {
	public class BindingOf : Expression {
		public string symbol;
		public BindingOf(string symbol):base(func: null,null,null){
			this.symbol = symbol;
		}

		public override Application expand(Dictionary<string, Application> boundParams)
		{
			if(boundParams != null)
			if(boundParams.ContainsKey(this.symbol)){
				return boundParams[this.symbol];
			}
			//GD.Print("[warning] BindingOf.expand: symbol " + this.symbol + "not found on boundParams, or boundParams null, returning null.");
			return null;
		}
	
	}
}

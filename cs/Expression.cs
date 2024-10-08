using System.Collections.Generic;
using Functory.Lang;
using Godot;

namespace Functory{
	public class Expression{

		public bool isBreak = false;
		public Function func;
		public Expression compositeFunc;
		public Expression[] positionalParams;
		public Dictionary<string, Expression> namedParams;

		public GraphNode exprNode;

		public virtual Application expand(Dictionary<string, Application> boundParams){

			if(boundParams != null) foreach(Application a in boundParams.Values){
				a.result = null; //Clear result from application when entering a new function so that partial application works correctly in the interpreter
			}

			Application returnApp = null;
			if(compositeFunc != null) returnApp = compositeFunc.expand(boundParams);
			else returnApp = new Application(func, null, null);

			if(this.positionalParams != null){
				List<Application> appPosParams = new List<Application>();

				if(returnApp.positionalParams != null){
					foreach(Application a in returnApp.positionalParams){
						appPosParams.Add(a);
					}
				}

				for(int i = 0; i < this.positionalParams.Length; i++){
					Application expApp = this.positionalParams[i].expand(boundParams);
					if(expApp != null) appPosParams.Add(expApp);
				}

				returnApp.positionalParams = appPosParams.ToArray();
			}

			if(this.namedParams != null){
				Dictionary<string, Application> appNamedParams = returnApp.namedParams!=null?returnApp.namedParams:(new Dictionary<string, Application>());       

				foreach(string key in this.namedParams.Keys){
					Application expApp = this.namedParams[key].expand(boundParams);
					if(expApp != null) appNamedParams.Add(key, expApp);
				}
				
				returnApp.namedParams = appNamedParams;
			}

			if(this.exprNode != null){
				returnApp.appNode = this.exprNode;
			}

			returnApp.isBreak = this.isBreak;

			return returnApp;
		}

		public Expression(Function func, Expression[] positionalParams, Dictionary<string, Expression> namedParams){
			this.func = func;
			this.compositeFunc = null;
			this.positionalParams = positionalParams;
			this.namedParams = namedParams;
		}

		public Expression(Expression compositeFunc, Expression[] positionalParams, Dictionary<string, Expression> namedParams){
			this.func = null;
			this.compositeFunc = compositeFunc;
			this.positionalParams = positionalParams;
			this.namedParams = namedParams;
		}

	}

	
}

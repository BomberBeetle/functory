using Functory.Lang;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Functory{
    public class ExecutionFrame{
        public Application application;
        public ExecutionFrame parent;

        public ExecutionFrame lastParamChild;
        public string lastParamSymbol;
        public Dictionary<string, Application> boundParams;

        public Dictionary<string, Application> matchedParams;

        public int pprmsBound;
        public object valueReceived;

        public ExecutionFrame(Application a){
            this.application = a;
            this.boundParams = new Dictionary<string, Application>();
            this.pprmsBound = 0;
            this.valueReceived = null;
            this.lastParamChild = null;
            this.lastParamSymbol = null;
            if(a.func is BuiltInFunction){
                this.matchedParams = new Dictionary<string, Application>(); //this shits premeditated
                List<string> paramsMatched = new List<string>(a.func.parameters);
                
                if(a.namedParams != null){
                    foreach(string par in paramsMatched){
                        if (a.namedParams.ContainsKey(par)){
                            matchedParams.Add(par, a.namedParams[par]);
                        } 
                    }
                    paramsMatched.RemoveAll((string prm) => a.namedParams.ContainsKey(prm));
                }
                if(a.positionalParams != null){
                    for(int i = 0; i < a.positionalParams.Length && i < a.func.parameters.Length; i++){
                        matchedParams.Add(a.func.parameters[i], a.positionalParams[i]);
                    }
                }
            }
        }

        public ExecutionFrame(Application a, ExecutionFrame parent):this(a){
            this.parent = parent;
        }

        public ExecutionFrame yieldNamedParam(string paramName, Application prm){
            return null;
        }

        public ExecutionFrame yieldPositionalParam(Application param){
            return null;
        }

        public object yieldFinally(){
            return null;
        }

        public override string ToString()
        {
            return $"[application.func type: {this.application.func.GetType()}, has parent? {this.parent!=null}, positionals bound = {pprmsBound}, value received = {valueReceived}\n" +
            $"bound params = {string.Join(';', this.boundParams.Keys)}, required params = {string.Join(';', this.application.func.parameters)} lastParamSymbol = {lastParamSymbol}, app result = {application.result ?? "null"}]";
        }
    }
}
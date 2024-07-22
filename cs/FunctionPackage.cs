using System;
using System.Collections.Generic;
using Functory.Lang;
using Godot;

public class FunctionPackage{
    public String packageName;
    public String packageIdentifier;

    public FunctionPackage parent;
    public List<FunctionPackage> ChildPackages;

    public List<Function> functions;

    public FunctionPackage(String packageName, String packageIdentifier){
        this.packageName = packageName;
        this.packageIdentifier = packageIdentifier;
        this.ChildPackages = new List<FunctionPackage>();
        this.functions = new List<Function>();
    }
    public void CreateTree(Tree tree, TreeItem parent, Dictionary<ulong, Function> componentMap){
        TreeItem it = tree.CreateItem(parent);
        it.SetText(0, packageName);
        foreach(Function f in functions){
            TreeItem funcItem = tree.CreateItem(it);
            funcItem.SetText(0, f.name);
            componentMap.Add(funcItem.GetInstanceId(), f);
        }
        foreach(FunctionPackage p in ChildPackages){
            p.CreateTree(tree, it, componentMap);
        }
    }

    public void AddFunction(Function f){
        f.package = this;
        functions.Add(f);
    }

    public void AddChildPackage(FunctionPackage fp){
        fp.parent = this;
        ChildPackages.Add(fp);
    }

    public string GetAddress(){
        if(parent != null){
            return parent.GetAddress() + "." + this.packageIdentifier;
        }
        else{
            return this.packageIdentifier;
        }
    }
}
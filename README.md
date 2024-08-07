# MFPrototypes

This is documentation about Unity prototypes for Modules Framework (MF).

You can read information about MF [here](https://github.com/JackLite/ModulesFramework) and about MF Unity Package [here](https://github.com/JackLite/ModulesFrameworkUnityPackage).

### Getting-started
MFPrototypes allows you to build your entity inside the Unity Editor almost without writing a lines of code.
All you need to do is to mark component as prototype and serializable:

```csharp
// mark serializable so Unity can draw it
[Serializable]
// mark that component can be added into prototype
// the "Enemy" string should be unique 
// it allows you to rename component or change it namespace without breaking serialization
[Prototype("Enemy")]
public struct Enemy
{
    public Transform transform;
    public Animator animator;
    public string configId;
}
```

Now you can add `EntityPrototype` field to any MonoBehaviour or ScriptableObject

```csharp
[SerializeField]
private EntityPrototype prototype;
```
And you will see prototype editor in inspector

![GettingStarted_emptyProto.png](Docs%2FGettingStarted_emptyProto.png)

Now you can add your component by "Add proto-component" button inside the prototype foldout.
It opens Add proto-component window

![GettingStarted_addComponent.png](Docs%2FGettingStarted_addComponent.png)

Here you can see every component that you marked as Prototype sorted by name.
You can use the search field at the top to find a specific component.
If you add several components, they will be added like a multiple components. 

You can assign any values like the usual MonoBehaviour component including links to GameObjects and ScriptableObjects.

After you have several options:

```csharp
[SerializeField]
private EntityPrototype prototype;

// you can create entity from prototype in awake using main world
private void Awake() 
{
    prototype.Create();    
}

// or you can provide world
public void Create(DataWorld world) 
{
    prototype.Create(world);        
}

// or you can fill existed entity
public void Fill(Entity entity)
{
    prototype.Fill(entity);
}
```

### Advanced information
This section for those who want to learn more about how prototypes work.

There is a process of creating new types right after unity compiling assemblies.
It finds every component that marks with `PrototypeAttribute` and creates a wrapper -
inheritor of `MonoComponent<T>` where T is a component type.
Then it stores in a list of `MonoComponent` inside `EntityPrototype` using Unity `SerializeReferenceAttribute`. 

The inspector for `EntityPrototype` look for all `MonoComponent` and add a corresponding type into the list
when you click "Add component" in popup.

There are two cases when this pipeline can be broken.

First is when you build your project from console (using `-batchmode`) and there is no Library directory
(so no assemblies are compiled).
For this (and partially for the next one) there is a hack:
special *.cs-file created with some code inside to force Unity recompile the code.

It cannot be done with requesting recompile from code,
because when you build a project, there will be no delay after compilation and Unity starts to import your assets.
If there are assets with `EntityPrototype` you get missing reference warning, and you game will not work properly.

After project is build `PostBuild` script will remove this temporary file.

The second case is when you open a project in Unity Editor and there is no Library yet
(for example, if you just clone it from repository).
In this case temporary file too created and destroyed.
In most cases, it works properly.
But sometimes not.
If you see missing reference warning in the console, you can
(and you should) click Modules->Prototypes->Force update prototypes.

![GettingStarted_forceUpdate.png](Docs%2FGettingStarted_forceUpdate.png)

Besides all of this you can inherit `MonoComponent<T>` to implements some additional logic.
It still will be shown in inspector and work fine without `PrototypeAttribute`.
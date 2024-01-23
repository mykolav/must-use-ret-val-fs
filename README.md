# Emit a diagnostic message if a method's return value is ignored

[![Build status](https://ci.appveyor.com/api/projects/status/nqx9jyt0q2hlep98?svg=true)](https://ci.appveyor.com/project/mykolav/must-use-ret-val-fs)

This project contains a Roslyn code analyzer lets you make sure a method's return value is not silently ignored/discarded.

![The MustUseRetVal analyzer in action](./must-use-ret-val-demo.gif)

## How to use it?

Install the [nuget package](https://www.nuget.org/packages/MustUseRetVal).

Introduce a `MustUseReturnValueAttribute` attribute to your solution. In other words, place the following C# code in an appropriate spot in your solution.

```csharp
[AttributeUsage(AttributeTargets.Constructor | AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Struct)]
class MustUseReturnValueAttribute : Attribute { }
```

Or use an existing `MustUseReturnValueAttribute`. For example, from [JetBrains.Annotations](https://www.nuget.org/packages/JetBrains.Annotations).

If you decide to go with `JetBrains.Annotations`, make sure to define the [`JETBRAINS_ANNOTATIONS`](https://blog.jetbrains.com/dotnet/2015/08/12/how-to-use-jetbrains-annotations-to-improve-resharper-inspections/) symbol &mdash; so that the `MustUseReturnValue` attribute is compiled into the resulting assembly.

Apply the `[MustUseReturnValue]` attribute to the methods that must not have their return values silently ignored.

```csharp
[MustUseReturnValue]
public bool IsGrantedDatabaseWritePermission() {
   // ...
}

// Elsewhere in your code:
// The value returned from `IsGrantedDatabaseWritePermission` must be
// assigned to a variable or checked in an if statement, etc.
// Otherwise, the analyzer will emit an error.
var isGranted = IsGrantedDatabaseWritePermission();
if (isGranted)
    WriteToDatabase();
```

### Supported method kinds

The analyzer supports the following method kinds  
- Regular instance and static methods
- Regular constructors
- Primary constructors 

To mark a record's primary constructor, apply `[MustUseReturnValue]` to the record itself.

```csharp
[MustUseReturnValue]
record Character(string Name, int PowerLevel) {}

[MustUseReturnValue]
record struct CharacterStruct(string Name, int PowerLevel) {}

// Elsewhere in your code:
// if the object created by primary constructor of `Character` or `CharacterStruct` is discarded,
// the analyzer will emit an error.
var character = new Character("Goku", 9001);
var characterStruct = new CharacterStruct("Goku", 9001);
```

Please note, in the code above the attribute only applies to the primary constructors of the records. If a record has additional constructors, you can mark them with this attribute individually in a usual way.

## Download and install

Install the [MustUseRetVal](https://www.nuget.org/packages/MustUseRetVal) nuget package.
For example, run the following command in the [NuGet Package Manager Console](https://docs.microsoft.com/en-us/nuget/tools/package-manager-console).

```powershell
Install-Package MustUseRetVal
```

This will download all the binaries, and add necessary analyzer references to your project.

## Configuration

Starting in Visual Studio 2019 version 16.3, you can [configure the severity of analyzer rules, or diagnostics](https://learn.microsoft.com/en-us/visualstudio/code-quality/use-roslyn-analyzers?view=vs-2022#configure-severity-levels), in an EditorConfig file, from the light bulb menu, and the error list.

You can add the following to the `[*.cs]` section of your .editorconfig.

```ini
[*.cs]
dotnet_diagnostic.MustUseReturnValue.severity = warning
```

The possible severity values are:
- `error`
- `warning`
- `suggestion`
- `silent`
- `none`
- `default` (in case of this analyzer, it's equal to `error`)

Please take a look at [the documentation](https://learn.microsoft.com/en-us/visualstudio/code-quality/use-roslyn-analyzers?view=vs-2022#configure-severity-levels) for a detailed description.

## The finishing problem of fluent interfaces

This analyzer can help mitigate [the finishing problem of fluent interfaces](https://daveaglick.com/posts/method-chaining-fluent-interfaces-and-the-finishing-problem). Quoting the relevant portions from the linked post:

> To illustrate [the finishing problem], consider a logging framework. It might allow some number of chained methods such as `Severity()`, `Source()`, `User()`, `CallSite()`, etc.:  
>  
> `Log.Message("Oh, noes!").Severity(Severity.Bad).User("jsmith");`  
>  
> Looks nice, right? The problem here is that the logging framework doesn’t know when to write the log message to the log file.  
> 
> Do I do it in the `User()` method? What if I don’t use the `User()` method or I put it before the `Severity()` method, then when do I write to the file?  
> 
> This problem occurs any time you want the entire result of a method chain to take some external action other than manipulating the context of the chain.
>
>  [...]
>
> ## Terminating Method
> 
> [Addressing the problem described above] requires the introduction of a method that serves to complete the chain and act on it’s final context. For example:
> 
> `Log.Message("Oh, noes!").Severity(Severity.Bad).User("jsmith").Write();`
> 
> See how we added the `Write()` method there at the end? That `Write()` method takes the chain context, writes it to disk, and doesn’t return anything (effectively stopping the chain).
>  
> So why is this so bad? For one, it would be very easy to forget the `Write()` method at the end of the chain. This technique requires the programmer to remember something that **the compiler can’t check** and that wouldn’t be picked up at runtime if they forgot.

Lets apply the analyzer to the logging example and see how it helps enforce a call to the terminating method.
```csharp
public class Log 
{
    // ...
    
    [MustUseReturnValue]
    public static Log Message(string message) { return new Log(message); }
    [MustUseReturnValue]
    public Log Severity(SeverityKind severity) { /* ... */ return this; }
    [MustUseReturnValue]
    public Log User(string userName) { /* ... */ return this; }
    
    // This method is supposed to be called to indicate a chain of fluent calls is complete.
    // Therefore, it does not return anything and is not marked with [MustUseReturnValue]. 
    public void Write() { /* ... */ }
}


// Elsewhere in the code:
Log.Message("Oh, noes!").Severity(Severity.Bad).User("jsmith");

// As the programmer forgets to call `Write` in the line above,
// the analyzer will emit a compile-time error: 
// "The return value of `Log.User` must be used"
```

# Thank you!

- [Richard Gibson](https://github.com/Richiban) for [ReturnValueUsageAnalyzer](https://github.com/Richiban/Richiban.Analyzer/tree/master/ReturnValueUsageAnalyzer/ReturnValueUsageAnalyzer) which [MustUseRetVal](https://github.com/mykolav/must-use-ret-val-fs) is based on.
- [John Koerner](https://github.com/johnkoerner) for [Creating a Code Analyzer using F#](https://johnkoerner.com/code-analysis/creating-a-code-analyzer-using-f/)
- [Dustin Campbell](https://github.com/DustinCampbell) for [CSharpEssentials](https://github.com/DustinCampbell/CSharpEssentials)

# License

The analyzer and code-fix provider are licensed under the MIT license.

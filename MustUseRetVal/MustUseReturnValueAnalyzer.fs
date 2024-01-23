namespace MustUseReturnValue.Analyzer


open System.Collections.Immutable
open Microsoft.CodeAnalysis
open Microsoft.CodeAnalysis.CSharp
open Microsoft.CodeAnalysis.CSharp.Syntax
open Microsoft.CodeAnalysis.Diagnostics


[<DiagnosticAnalyzer(LanguageNames.CSharp)>]
type public MustUseReturnValueAnalyzer() =
    inherit DiagnosticAnalyzer()


    override val SupportedDiagnostics =
        ImmutableArray.Create(
            DiagnosticDescriptors.MustUseReturnValue,
            DiagnosticDescriptors.InternalError)


    override this.Initialize (context: AnalysisContext) =
        // We don't want to suggest named args in generated code.
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None)
        // We can handle concurrent invocations.
        context.EnableConcurrentExecution()

        // Register ourself to get invoked to analyze
        // expression statements. Each one is potentially,
        // an invocation expression with its return value discarded.
        context.RegisterSyntaxNodeAction(
            (fun c -> this.Analyze c),
            SyntaxKind.ExpressionStatement)


    member private this.Analyze(context: SyntaxNodeAnalysisContext) =
        try
            this.DoAnalyze(context)
        with
        | ex ->
            context.ReportDiagnostic(
                Diagnostic.Create(
                    DiagnosticDescriptors.InternalError,
                    context.Node.GetLocation(),
                    // messageArgs
                    ex.ToString()))


    member private this.DoAnalyze(context: SyntaxNodeAnalysisContext) =
        match context.Node with
        | :? ExpressionStatementSyntax as ess ->
            match context.SemanticModel.GetSymbolInfo(ess.Expression).Symbol with
            | :? IMethodSymbol as methodSymbol ->
                if this.IsSupported(methodSymbol) &&
                   this.MustUseReturnValueOf(methodSymbol)
                then
                    context.ReportDiagnostic(
                        Diagnostic.Create(
                            DiagnosticDescriptors.MustUseReturnValue,
                            ess.GetLocation(),
                            // messageArgs
                            methodSymbol.ContainingType.Name + "." + methodSymbol.Name))

            | _ ->
                ()

        | _ ->
            ()


    member private this.IsSupported(methodSymbol: IMethodSymbol): bool =
        // So far we only support analyzing the four kinds of methods listed below.
        match methodSymbol.MethodKind with
        | MethodKind.Ordinary
        | MethodKind.Constructor
        | MethodKind.LocalFunction
        | MethodKind.ReducedExtension -> true
        | _                           -> false


    member private this.MustUseReturnValueOf(methodSymbol: IMethodSymbol): bool =
        if  methodSymbol.ReturnsVoid &&
            (methodSymbol.MethodKind <> MethodKind.Constructor)
        then
            // We don't inspect methods return void for obvious reasons.
            // Unless it's a constructor. As the constructor's return type
            // is considered to be void but we still want to make sure
            // the created object does not get discarded.
            false
        else

        if this.HasMustUseReturnValueAttribute(methodSymbol)
        then
            // If the method has been marked with the attribute, we're done.
            true
        else

        // The method has not been marked with the attribute.
        // See if the method is a primary constructor.
        // In case it is, we're going to check if the containing type
        // itself has been marked with the attribute.
        // (Currently, there is no way in C# to specify
        // an attribute should apply to a type's primary constructor).

        if methodSymbol.MethodKind <> MethodKind.Constructor
        then
            // Captain Obvious tells me, if it's not a constructor,
            // it cannot be a primary constructor.
            false
        else

        // Just to be on the safe side, let's check `methodSymbol` has some declaring syntaxes.
        if methodSymbol.DeclaringSyntaxReferences.Length = 0
        then
            false
        else

        // The declaring syntax of a primary constructor is its type declaration.
        match methodSymbol.DeclaringSyntaxReferences[0].GetSyntax() with
        | :? RecordDeclarationSyntax
        // The following two cover C# 12's class/struct primary constructors
        | :? ClassDeclarationSyntax
        | :? StructDeclarationSyntax ->
            // The type declaration syntax corresponds to the primary constructor's containing type.
            // See if the method's containing type has been marked with the attribute.
            this.HasMustUseReturnValueAttribute(methodSymbol.ContainingType)
        | _ -> false


    member private this.HasMustUseReturnValueAttribute(symbol: ISymbol): bool =
        symbol.GetAttributes() |> Seq.exists (fun it ->
            it.AttributeClass.Name = "MustUseReturnValueAttribute")

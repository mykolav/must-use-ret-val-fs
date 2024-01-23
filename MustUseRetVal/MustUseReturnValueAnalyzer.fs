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
        (methodSymbol.MethodKind = MethodKind.Constructor || not methodSymbol.ReturnsVoid) &&
        methodSymbol.GetAttributes() |> Seq.exists (fun it ->
            it.AttributeClass.Name = "MustUseReturnValueAttribute")

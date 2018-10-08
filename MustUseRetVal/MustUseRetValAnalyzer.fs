namespace MustUseRetVal.Analyzer

open System.Collections.Immutable
open Microsoft.CodeAnalysis
open Microsoft.CodeAnalysis.CSharp
open Microsoft.CodeAnalysis.CSharp.Syntax
open Microsoft.CodeAnalysis.Diagnostics
open MustUseRetVal.CSharpAdapters
open MustUseRetVal.MaybeBuilder
open System.Text.RegularExpressions

[<DiagnosticAnalyzer(Microsoft.CodeAnalysis.LanguageNames.CSharp)>]
type public MustUseRetValAnalyzer() = 
    inherit DiagnosticAnalyzer()

    static let diagnosticId = "UseRetVal"
    static let messageFormat = "The return value of '{0}' must be used."
    static let description = "The return value of a `[MustUseReturnValue]` method must be used."
    static let descriptor = 
        DiagnosticDescriptor(
            id=diagnosticId,
            title="A [MustUseReturnValue] method's return value must be used.",
            messageFormat=messageFormat,
            category="Functional",
            defaultSeverity=DiagnosticSeverity.Error, 
            isEnabledByDefault=true, 
            description=description,
            helpLinkUri=null)

    static member DiagnosticId = diagnosticId
    static member MessageFormat = messageFormat

    override val SupportedDiagnostics = ImmutableArray.Create(descriptor)

    override this.Initialize (context: AnalysisContext) =
        // Register ourself to get invoked to analyze 
        //   - expression statement; e. g., calling a method and discarding its return value.
        context.RegisterSyntaxNodeAction(
            (fun c -> this.Analyze c),
            SyntaxKind.ExpressionStatement)

    member private this.logInfo (message: string) = ()
            //(try
            //    System.IO.File.AppendAllText("d:\\temp\\must-use-ret-val.txt", 
            //                                 sprintf "%A: %s\r\n" System.DateTime.Now message)
            // with ex -> ()) |> ignore


    member private this.mustUseReturnValue (methodSymbol: IMethodSymbol) =
        let MustUseRetVal = "MustUseReturnValueAttribute"

        if methodSymbol.ReturnsVoid
        then false
        else methodSymbol.GetAttributes() 
             |> Seq.exists (fun attrData -> attrData.AttributeClass.Name = MustUseRetVal)

    member private this.Analyze(context: SyntaxNodeAnalysisContext) =
        maybe {
            let sema = context.SemanticModel
            let! exprStmtSyntax = context.Node |> Option.ofType<ExpressionStatementSyntax>
            let! invocationExprSyntax = exprStmtSyntax.Expression |> Option.ofType<InvocationExpressionSyntax>
            let! methodSymbol = sema.GetSymbolInfo(invocationExprSyntax).Symbol |> Option.ofType<IMethodSymbol>

            if this.mustUseReturnValue methodSymbol
            then return context.ReportDiagnostic(
                            Diagnostic.Create(
                                descriptor, 
                                exprStmtSyntax.GetLocation(),
                                // params messageArgs:
                                methodSymbol.Name))
            else return ()
        } |> ignore

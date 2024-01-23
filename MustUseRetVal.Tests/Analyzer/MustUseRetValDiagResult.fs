module MustUseRetVal.Tests.Support.MustUseRetValDiagResult

open System
open Microsoft.CodeAnalysis
open DiagnosticResult
open MustUseReturnValue.Analyzer

type MustUseRetValDiagResult() =

    static member Create(invokedMethod: string,
                         fileName: string,
                         line: uint32,
                         column: uint32) =

        let message = String.Format(DiagnosticDescriptors.MustUseReturnValue.MessageFormat.ToString(),
                                    invokedMethod)
        let diagResult = DiagResult(id       = DiagnosticDescriptors.MustUseReturnValue.Id,
                                    message  = message,
                                    severity = DiagnosticSeverity.Error,
                                    location = {Path=fileName; Line=line; Col=column})
        diagResult

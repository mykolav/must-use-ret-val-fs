module MustUseRetVal.Tests.Support.MustUseRetValDiagResult

open System
open Microsoft.CodeAnalysis
open DiagnosticResult
open MustUseRetVal.Analyzer

type MustUseRetValDiagResult() =

    static member Create(invokedMethod: string,
                         fileName: string,
                         line: uint32,
                         column: uint32) =

        let message = String.Format(MustUseRetValAnalyzer.MessageFormat, 
                                    invokedMethod)
        let diagResult = DiagResult(id       = MustUseRetValAnalyzer.DiagnosticId,
                                    message  = message,
                                    severity = DiagnosticSeverity.Error,
                                    location = {Path=fileName; Line=line; Col=column})
        diagResult

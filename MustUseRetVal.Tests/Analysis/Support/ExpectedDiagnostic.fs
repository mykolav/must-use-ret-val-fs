namespace MustUseReturnValue.Tests.Analysis.Support


open System
open Microsoft.CodeAnalysis
open MustUseReturnValue.Analyzer


type ExpectedLocation = {
    Path:   string
    Line:   int32
    Column: int32 }


type ExpectedDiagnostic = {
    Severity:            DiagnosticSeverity
    Id:                  string
    Message:             string
    Location:            ExpectedLocation option
    AdditionalLocations: ExpectedLocation[] }
    with


    static member MustUseReturnValue(invokedMethod: string,
                                     fileName: string,
                                     line: int32,
                                     column: int32)
                                    : ExpectedDiagnostic =

        let message = String.Format(
            DiagnosticDescriptors.MustUseReturnValue.MessageFormat.ToString(),
            invokedMethod)

        let expectedDiagnostic =
            { Id                  = DiagnosticDescriptors.MustUseReturnValue.Id
              Message             = message
              Severity            = DiagnosticSeverity.Error
              Location            = Some { Path=fileName; Line=line; Column=column }
              AdditionalLocations = Array.empty }

        expectedDiagnostic

namespace MustUseReturnValue.Analyzer


open Microsoft.CodeAnalysis


module DiagnosticDescriptors =


    let MustUseReturnValue =
        DiagnosticDescriptor(
            id="MustUseReturnValue",
            title="A [MustUseReturnValue] method's return value must be used",
            messageFormat="The return value of '{0}' must be used",
            category="Functional",
            defaultSeverity=DiagnosticSeverity.Error,
            isEnabledByDefault=true,
            description="A [MustUseReturnValue] method's return value must be used",
            helpLinkUri=null)


    let InternalError =
        DiagnosticDescriptor(
            id="MustUseReturnValue9999",
            title="Must use return value analysis experienced an internal error",
            messageFormat="An internal error in `{0}`",
            category="Functional",
            defaultSeverity=DiagnosticSeverity.Hidden,
            description="Must use return value analysis experienced an internal error",
            isEnabledByDefault=false,
            helpLinkUri=null)

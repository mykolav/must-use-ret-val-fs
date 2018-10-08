module MustUseRetVal.Tests.AnalyzerTests

open Expecto
open Support.MustUseRetValDiagResult
open MustUseRetVal.Analyzer

module private Expect =
    open Support.DiagnosticMatcher
    open Support.DiagnosticProvider
    open Support.DocumentFactory

    let toBeEmittedFrom code expectedDiags =
        let analyzer = MustUseRetValAnalyzer()
        expectedDiags 
        |> Expect.diagnosticsToMatch analyzer 
                                     (analyzer.GetSortedDiagnostics(CSharp, [code]))

    let emptyDiagnostics code = [||] |> toBeEmittedFrom code

[<Tests>]
let analyzerTests = 
    testList "The MustUseRetVal analyzer tests" [
        test "Empty code does not trigger diagnostics" {
            Expect.emptyDiagnostics @"";
        }
        test "Code with compile-errors does not trigger diagnostics" {
            Expect.emptyDiagnostics @"
                namespace Frobnitz
                {
                    class Wombat
                    {
                        void Bork() { Gork(); } 
                    } } 
            "
        }
        test "Method which is not annoated does not trigger diagnostics" {
            Expect.emptyDiagnostics @"
                namespace Frobnitz
                {
                    class Wombat
                    {
                        string Gork() => ""Gork!"";
                        void Bork() { Gork(); } 
                    } } 
            "
        }
        test "Void method does not trigger diagnostics" {
            Expect.emptyDiagnostics @"
                namespace Frobnitz
                {
                    class Wombat
                    {
                        [MustUseReturnValue]
                        void Gork() {}
                        void Bork() { Gork(); } 
                    }

                    [System.AttributeUsage(System.AttributeTargets.Method)]  
                    class MustUseReturnValueAttribute: System.Attribute {}
                }
            "
        }
        test "Method with the commented out attribute does not trigger diagnostics" {
            Expect.emptyDiagnostics @"
                namespace Frobnitz
                {
                    class Wombat
                    {
                        // [MustUseReturnValue]
                        string Gork() => ""Gork!"";
                        void Bork() { Gork(); } 
                    }

                    [System.AttributeUsage(System.AttributeTargets.Method)]  
                    class MustUseReturnValueAttribute: System.Attribute {}
                }
            "
        }
        test "Method annoated by the attribute when return value is used does not trigger diagnostics" {
            Expect.emptyDiagnostics @"
                namespace Frobnitz
                {
                    [System.AttributeUsage(System.AttributeTargets.Method)]  
                    class MustUseReturnValueAttribute: System.Attribute {}

                    class Wombat
                    {
                        [MustUseReturnValue]
                        string Gork() => ""Gork!"";
                        void Bork() { var _ = Gork(); } 
                    } }
            "
        }
        test "Method annoated by the attribute triggers diagnostics" {
            let snippet = @"
                namespace Frobnitz
                {
                    [System.AttributeUsage(System.AttributeTargets.Method)]  
                    class MustUseReturnValueAttribute: System.Attribute {}

                    class Wombat
                    {
                        [MustUseReturnValue]
                        string Gork() => ""Gork!"";
                        void Bork() { Gork(); } 
                    } }
            "

            let mustUseRetValDiag = MustUseRetValDiagResult
                                        .Create(invokedMethod="Gork",
                                                fileName="Test0.cs", line=11u, column=39u)

            [|mustUseRetValDiag|] |> Expect.toBeEmittedFrom snippet
        }
    ]

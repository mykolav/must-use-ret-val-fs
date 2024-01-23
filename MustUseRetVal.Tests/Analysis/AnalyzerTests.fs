namespace MustUseReturnValue.Tests.Analysis


open Microsoft.CodeAnalysis
open Xunit
open MustUseReturnValue.Analyzer
open MustUseReturnValue.Tests.Analysis.Support
open MustUseReturnValue.Tests.Support


[<RequireQualifiedAccess>]
module private Diagnostics =


    let Of(program: string): Diagnostic[] =
        let analyzer = MustUseReturnValueAnalyzer()
        analyzer.Analyze(Document.Language.CSharp, [program])


type AnalyzerTests() =


    [<Fact>]
    member _.``Empty code does not trigger diagnostics``() =
        let diagnostics = Diagnostics.Of(CSharpProgram.WithClasses(@""))
        Assert.That(diagnostics).AreEmpty()


    [<Fact>]
    member _.``Code with compile-time errors does not trigger diagnostics``() =
        let diagnostics = Diagnostics.Of(CSharpProgram.WithClasses(@"
            class Wombat
            {
                void Bork() { Gork(); }
            }
        "))

        Assert.That(diagnostics)
              .ContainNo(DiagnosticDescriptors.MustUseReturnValue.Id)


    [<Fact>]
    member _.``Unannotated method does not trigger diagnostics``() =
        let diagnostics = Diagnostics.Of(CSharpProgram.WithClasses(@"
            class Wombat
            {
                string Gork() => ""Gork!"";
                void Bork() { Gork(); }
            }
        "))

        Assert.That(diagnostics).AreEmpty()


    [<Fact>]
    member _.``Annotated void method does not trigger diagnostics``() =
        let diagnostics = Diagnostics.Of(CSharpProgram.WithClasses(@"
            class Wombat
            {
                [MustUseReturnValue]
                void Gork() {}
                void Bork() { Gork(); }
            }
        "))

        Assert.That(diagnostics).AreEmpty()


    [<Fact>]
    member _.``Method with the attribute commented out does not trigger diagnostics``() =
        let diagnostics = Diagnostics.Of(CSharpProgram.WithClasses(@"
            class Wombat
            {
                // [MustUseReturnValue]
                string Gork() => ""Gork!"";
                void Bork() { Gork(); }
            }
        "))

        Assert.That(diagnostics).AreEmpty()


    [<Fact>]
    member _.``Return value assigned to a variable does not trigger diagnostics``() =
        let diagnostics = Diagnostics.Of(CSharpProgram.WithClasses(@"
            class Wombat
            {
                [MustUseReturnValue]
                string Gork() => ""Gork!"";
                void Bork() { var _ = Gork(); }
            }
        "))

        Assert.That(diagnostics).AreEmpty()


    [<Fact>]
    member _.``Return value passed as argument does not trigger diagnostics``() =
        let diagnostics = Diagnostics.Of(CSharpProgram.WithClasses(@"
            class Wombat
            {
                [MustUseReturnValue]
                string Gork() => ""Gork!"";
                void Pork(string gork) {}
                void Bork() { Pork(Gork()); }
            }
        "))

        Assert.That(diagnostics).AreEmpty()


    [<Fact>]
    member _.``Discarding return value triggers diagnostics``() =
        let diagnostics = Diagnostics.Of(CSharpProgram.WithClasses(@"
            class Wombat
            {
                [MustUseReturnValue]
                string Gork() => ""Gork!"";
                void Bork() { Gork(); }
            }
        "))

        let expectedDiagnostic = ExpectedDiagnostic.MustUseReturnValue(
                                     invokedMethod="Wombat.Gork",
                                     fileName="Test0.cs",
                                     line=9,
                                     column=31)

        Assert.That(diagnostics).Match([ expectedDiagnostic ])


    [<Fact>]
    member _.``Discarding the object created by unannotated constructor does not trigger diagnostics``() =
        let diagnostics = Diagnostics.Of(CSharpProgram.WithClasses(@"
            class Wombat
            {
                Wombat() {}
                void Bork() { new Wombat(); }
            }
        "))

        Assert.That(diagnostics).AreEmpty()


    [<Fact>]
    member _.``Discarding the created object triggers diagnostics``() =
        let diagnostics = Diagnostics.Of(CSharpProgram.WithClasses(@"
            class Wombat
            {
                [MustUseReturnValue]
                Wombat() {}
                void Bork() { new Wombat(); }
            }
        "))

        let expectedDiagnostic = ExpectedDiagnostic.MustUseReturnValue(
                                     invokedMethod="Wombat..ctor",
                                     fileName="Test0.cs",
                                     line=9,
                                     column=31)

        Assert.That(diagnostics).Match([ expectedDiagnostic ])

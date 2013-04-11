<Query Kind="Program">
  <Namespace>System.CodeDom</Namespace>
  <Namespace>System.CodeDom.Compiler</Namespace>
  <Namespace>Microsoft.CSharp</Namespace>
  <IncludePredicateBuilder>true</IncludePredicateBuilder>
</Query>

void Main()
{
    var sourceCode = GenerateSourceCode(BuildCodeNamespace());
    
    sourceCode.Dump();
    
    var generatedAssembly = CompileAssembly(sourceCode);
    
    Assembly.Load(generatedAssembly.GetName());
    
    var generatedType = generatedAssembly.ExportedTypes.Single();
    
    var model = new Model.ProcessingModel { InputA = 10M, InputB = 5M, Factor = 0.050M };
    
    generatedType.GetMethods()
                    .Single(methodInfo => methodInfo.Name == "Test")
                    .Invoke(null, new object[]{ model }).Dump();
}

static CodeNamespace BuildCodeNamespace()
{
    var ns = new CodeNamespace("Generated");
    
    var systemImport = new CodeNamespaceImport("System");
    ns.Imports.Add(systemImport);
    
    var programClass = new CodeTypeDeclaration("GeneartedClass");
    ns.Types.Add(programClass);
    
    var methodTest = new CodeMemberMethod
    {
        Attributes = MemberAttributes.Public | MemberAttributes.Static,
        Name = "Test",
        ReturnType = new CodeTypeReference(typeof(Model.ReportModel))
    };
    
    methodTest.Parameters.Add(new CodeParameterDeclarationExpression(
                                new CodeTypeReference(typeof(Model.ProcessingModel)),
                                "model"));
    
    var modelArgument = new CodeArgumentReferenceExpression("model");
    
    // model.Result = model.InputA + model.InputB * model.Factor;
    methodTest.Statements.Add(
        new CodeAssignStatement(
            new CodePropertyReferenceExpression(modelArgument, "Result"),
            new CodeBinaryOperatorExpression(
                new CodePropertyReferenceExpression(modelArgument, "InputA"),
                CodeBinaryOperatorType.Add,
                new CodeBinaryOperatorExpression(
                    new CodePropertyReferenceExpression(modelArgument, "InputB"),
                    CodeBinaryOperatorType.Multiply,
                    new CodePropertyReferenceExpression(modelArgument, "Factor")))));
    
    // model.Delta = Math.Abs((model.Result ?? 0M) - model.InputA);
    // TODO: fix coalescing operator ??
    methodTest.Statements.Add(
        new CodeAssignStatement(
            new CodePropertyReferenceExpression(modelArgument, "Delta"),
            new CodeBinaryOperatorExpression(
                new CodeMethodInvokeExpression(
                    new CodeMethodReferenceExpression(
                        new CodeTypeReferenceExpression(typeof(Math)),
                        "Abs"),
                        
                        new CodeMethodInvokeExpression(
                            new CodeMethodReferenceExpression(
                                new CodePropertyReferenceExpression(modelArgument, "Result"),
                                "GetValueOrDefault"),
                                new [] { new CodePrimitiveExpression(0m) })),    
                        
                CodeBinaryOperatorType.Subtract,
                new CodePropertyReferenceExpression(modelArgument, "InputA"))));
    
    // model.Description = @"Some description";
    methodTest.Statements.Add(
        new CodeAssignStatement(
            new CodePropertyReferenceExpression(modelArgument, "Description"),
            new CodePrimitiveExpression(@"Some description")));
    
    methodTest.Statements.Add(
        new CodeMethodReturnStatement(
            new CodeObjectCreateExpression(typeof(Model.ReportModel))));
    
    programClass.Members.Add(methodTest);
    
    return ns;
}

static string GenerateSourceCode(CodeNamespace prgNamespace)
{
    var compilerOptions = new CodeGeneratorOptions()
    {
      IndentString = new string(' ', 4),
      BracingStyle = "C",
      BlankLinesBetweenMembers = false
    };
    var codeText = new StringBuilder();
    
    using (var codeWriter = new StringWriter(codeText))
    {
      CodeDomProvider.CreateProvider("CSharp")
        .GenerateCodeFromNamespace(
          prgNamespace, codeWriter, compilerOptions);
    }
    
    return codeText.ToString();
}

static Assembly CompileAssembly(string sourceCode)
{
    var codeProvider = CodeDomProvider.CreateProvider("CSharp");
    
    var parameters = new CompilerParameters
    {
        GenerateInMemory = true
    };
    
    parameters.ReferencedAssemblies.Add(typeof(Model.ProcessingModel).Assembly.Location);
    
    var results = codeProvider.CompileAssemblyFromSource(parameters, sourceCode);
    
    if(results.Errors.HasErrors)
    {  
        var errors = new StringBuilder("Following compilations error(s) found: ");
        
        errors.AppendLine();
        
        foreach (CompilerError error in results.Errors)
        {
            errors.AppendFormat("Message: '{0}', LineNumber: {1}", error.ErrorText, error.Line);
        }
        
        throw new Exception(errors.ToString());
    }
    
    return results.CompiledAssembly;
}
}

namespace Model
{
    public class ProcessingModel
    {
        public decimal InputA { get; set; }
        public decimal InputB { get; set; }
        public decimal Factor { get; set; }
        
        public decimal? Result { get; set; }
        public decimal? Delta { get; set; }
        public string Description { get; set; }
        public decimal? Addition { get; set; }
    }
    
    public class ReportModel
    {
        public decimal? Σ { get; set; }
        public decimal? Δ { get; set; }
        public string λ { get; set; }
    }
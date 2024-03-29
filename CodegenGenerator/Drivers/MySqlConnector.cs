using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Plugin;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace SqlcGenCsharp.Drivers;

public class MySqlConnector : IDbDriver
{
    public TypeSyntax ColumnType(string columnType, bool notNull)
    {
        var nullableSuffix = notNull ? string.Empty : "?";

        if (string.IsNullOrEmpty(columnType))
            return ParseTypeName("object" + nullableSuffix);

        switch (columnType.ToLower())
        {
            case "bigint":
                return ParseTypeName("long" + nullableSuffix);
            case "binary":
            case "bit":
            case "blob":
            case "longblob":
            case "mediumblob":
            case "tinyblob":
            case "varbinary":
                return ParseTypeName("byte[]" + nullableSuffix);
            case "char":
            case "date":
            case "datetime":
            case "decimal":
            case "longtext":
            case "mediumtext":
            case "text":
            case "time":
            case "timestamp":
            case "tinytext":
            case "varchar":
                return ParseTypeName("string");
            case "double":
            case "float":
                return ParseTypeName("double" + nullableSuffix);
            case "int":
            case "mediumint":
            case "smallint":
            case "tinyint":
            case "year":
                return ParseTypeName("int" + nullableSuffix);
            case "json":
                // Assuming JSON is represented as a string or a specific class
                return ParseTypeName("object" + nullableSuffix);
            default:
                throw new NotSupportedException($"Unsupported column type: {columnType}");
        }
    }

    private static MethodDeclarationSyntax GetMethodForQuery(Query query)
    {
        return MethodDeclaration(
                IdentifierName("Task"), query.Name)
            .AddModifiers(
                Token(SyntaxKind.PublicKeyword),
                Token(SyntaxKind.AsyncKeyword))
            .WithBody(Block(ParseStatement("var connection = new MySqlConnection();")));
    }
    
    public (UsingDirectiveSyntax[], MemberDeclarationSyntax[]) Preamble(Query[] queries)
    {
        return (
            [UsingDirective(ParseName("MySql.Data.MySqlClient"))],
            [
                FieldDeclaration(
                        VariableDeclaration(PredefinedType(Token(SyntaxKind.StringKeyword)))
                            .WithVariables(SingletonSeparatedList<VariableDeclaratorSyntax>(
                                VariableDeclarator(Identifier("ConnectionString"))
                                    .WithInitializer(EqualsValueClause(
                                        LiteralExpression(SyntaxKind.StringLiteralExpression,
                                            Literal("server=localhost;user=root;database=mydb;port=3306;password=")))))
                            ))
                    .WithModifiers(TokenList(
                        Token(SyntaxKind.PrivateKeyword),
                        Token(SyntaxKind.ConstKeyword)))
            ]
        );
    }
    
    public MemberDeclarationSyntax OneDeclare(string funcName, string queryName, string argInterface,
        string returnInterface, IEnumerable<Parameter> parameters, IEnumerable<Column> columns)
    {
        // Generating function parameters, potentially including 'args'
        var funcParams = FuncParamsDecl(argInterface, parameters); // FuncParamsDecl should be implemented as before

        // Return type is Task<ReturnType?>
        var returnType = GenericName(Identifier("Task"))
            .WithTypeArgumentList(
                TypeArgumentList(
                    SingletonSeparatedList<TypeSyntax>(
                        NullableType(IdentifierName(returnInterface)))));

        // Method declaration
        var methodDeclaration = MethodDeclaration(returnType, Identifier(funcName))
            .AddModifiers(Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.AsyncKeyword))
            .WithParameterList(ParameterList(SeparatedList(funcParams)))
            .WithBody(Block(
                // Placeholder for method body: In a real scenario, you'd include logic to execute the query
                // and return a single result or null. The following statement is a simplification
                SingletonList<StatementSyntax>(
                    ReturnStatement(
                        InvocationExpression(
                            MemberAccessExpression(
                                SyntaxKind.SimpleMemberAccessExpression,
                                IdentifierName("Task"),
                                IdentifierName("FromResult")),
                            ArgumentList(SeparatedList(new[]
                            {
                                Argument(
                                    InvocationExpression(IdentifierName("ConvertToReturnType"),
                                            ArgumentList(SingletonSeparatedList(
                                                Argument(IdentifierName("row")))))
                                        .WithLeadingTrivia(Comment("// Convert row to ReturnType instance"))
                                )
                            }))
                        ).WithLeadingTrivia(Comment("// Placeholder for actual database query execution"))
                    )
                )
            ));

        return methodDeclaration;
    }

    public MemberDeclarationSyntax ExecDeclare(string funcName, string queryName, string argInterface,
        IEnumerable<Parameter> parameters)
    {
        // Generating the parameters for the method, potentially including 'args' if specified
        var funcParams =
            FuncParamsDecl(argInterface, parameters);

        // Creating the method declaration
        var methodDeclaration = MethodDeclaration(
                GenericName(Identifier("Task"))
                    .WithTypeArgumentList(TypeArgumentList(
                        SingletonSeparatedList<TypeSyntax>(PredefinedType(Token(SyntaxKind.VoidKeyword))))),
                Identifier(funcName))
            .AddModifiers(Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.AsyncKeyword))
            .WithParameterList(ParameterList(SeparatedList(funcParams)))
            .WithBody(Block(
                // Simplified body; assuming a method call to 'client.query' with parameters.
                // The actual implementation would involve more detailed Roslyn syntax generation,
                // depending on how the 'client.query' method is defined and used.
                SingletonList<StatementSyntax>(
                    ExpressionStatement(
                        AwaitExpression(
                            InvocationExpression(
                                MemberAccessExpression(
                                    SyntaxKind.SimpleMemberAccessExpression,
                                    IdentifierName("client"),
                                    IdentifierName(
                                        "QueryAsync") // Assuming a method QueryAsync exists for simplification
                                ),
                                ArgumentList(SingletonSeparatedList(
                                    Argument(
                                        // Simplified argument; actual implementation would dynamically build this based on 'params'
                                        IdentifierName("queryParameters") // Placeholder for actual query parameters
                                    )
                                ))
                            )
                        )
                    )
                )
            ));

        return methodDeclaration;
    }

    public MemberDeclarationSyntax ManyDeclare(string funcName, string queryName, string argInterface,
        string returnInterface,
        IEnumerable<Parameter> parameters, IEnumerable<Column> columns)
    {
        // Assuming FuncParamsDecl is implemented as shown previously
        var funcParams = FuncParamsDecl(argInterface, parameters);

        // Return type is Task<List<ReturnType>>
        var returnType = GenericName(Identifier("Task"))
            .WithTypeArgumentList(
                TypeArgumentList(
                    SingletonSeparatedList<TypeSyntax>(
                        GenericName(Identifier("List"))
                            .WithTypeArgumentList(
                                TypeArgumentList(
                                    SingletonSeparatedList<TypeSyntax>(
                                        IdentifierName(returnInterface)))))));

        // Method declaration
        var methodDeclaration = MethodDeclaration(returnType, Identifier(funcName))
            .AddModifiers(Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.AsyncKeyword))
            .WithParameterList(ParameterList(SeparatedList(funcParams)))
            .WithBody(Block(
                // Simplified body: In a real scenario, you'd construct the logic to execute the query,
                // map the results to a list of `returnIface` objects, and return it.
                // The following is a placeholder to illustrate structure.
                SingletonList<StatementSyntax>(
                    ReturnStatement(
                        InvocationExpression(
                            MemberAccessExpression(
                                SyntaxKind.SimpleMemberAccessExpression,
                                IdentifierName("Task"),
                                GenericName(Identifier("FromResult"))
                                    .WithTypeArgumentList(
                                        TypeArgumentList(
                                            SingletonSeparatedList<TypeSyntax>(
                                                PredefinedType(Token(SyntaxKind.StringKeyword)))))),
                            ArgumentList(SingletonSeparatedList(
                                Argument(
                                    LiteralExpression(SyntaxKind.StringLiteralExpression,
                                        Literal("Placeholder for actual implementation"))
                                )
                            ))
                        )
                    )
                )
            ));

        return methodDeclaration;
    }

    private static IEnumerable<ParameterSyntax> FuncParamsDecl(string argInterface, IEnumerable<Parameter> parameters)
    {
        var funcParams = new List<ParameterSyntax>
        {
            Parameter(Identifier("client")).WithType(IdentifierName("Client"))
        };

        using var enumerator = parameters.GetEnumerator();
        if (!string.IsNullOrEmpty(argInterface) && enumerator.MoveNext())
            funcParams.Add(
                Parameter(Identifier("args")).WithType(IdentifierName(argInterface))
            );

        return funcParams;
    }
}
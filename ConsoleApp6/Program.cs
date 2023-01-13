using ConsoleApp6;

var examples = """

orderedAscending([1, 3, 4]) -> true
orderedDescending([5, 3, 4]) -> false

sum([12, 34]) -> 46


""";



var dataSet = new DataSetDefinition
{
    Properties = new[]
    {
        new DataPropertyDefinition{ Name = "foo.somePropertyName", DataPropertyType = DataPropertyType.Number }
    }
};


var formula = "OrderedAscending([40, foo.somePropertyName])"; // @"foo.somePropertyName";

var syntax = SyntaxVisitor.Parse(formula);

foreach (var node in syntax)
{
    Console.WriteLine($"Found: {node}: {node.Range.ApplyTo(formula)}");
}


var compiled = ExpressionCompiler.Compile(formula, syntax, dataSet);


var dataSetEvaluator = new DataSetEvaluator();
var dataRow = new DataRow();
dataRow.Values.Add("foo.somePropertyName", 42D);


var result = compiled.Invoke(dataSetEvaluator, dataRow);
Console.WriteLine($"Result: {result}");
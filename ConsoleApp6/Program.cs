using ConsoleApp6;
using System.Diagnostics;


var dataSet = new DataSetDefinition
{
    Properties = new[]
    {
        new DataPropertyDefinition{ Name = "foo.somePropertyName", DataPropertyType = new DataType(DataTypeBase.Number) }
    }
};

var formula = "OrderedAscending([Sum([3, 10, 33, 4]), foo.somePropertyName])"; 

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

Stopwatch sw = Stopwatch.StartNew();

for (int i =0; i < 1000000; i++)
{
    var result2 = compiled.Invoke(dataSetEvaluator, dataRow);
}

sw.Stop();

Console.WriteLine($"Time: {sw.Elapsed.TotalMilliseconds}");
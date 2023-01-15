using FormulaTest;


var dataSet = new DataSetDefinition
{
    Properties = new[]
    {
        new DataPropertyDefinition{ Name = "foo.somePropertyName", DataPropertyType = DataType.CreateBasic(DataTypeBase.Number) },
        new DataPropertyDefinition{ Name = "hiredate", DataPropertyType = DataType.CreateBasic(DataTypeBase.Date) },
    }
};

var formula = "Filter([1, 2, 3, 4], foo => OrderedAscending([2, foo]))";
//"[OrderedAscending([365, Diff(now(), hiredate)]), OrderedAscending([1, Diff(3, 2)])]";
// "OrderedAscending([Sum([3, Diff(10, 3), Avg([13, 4])]), foo.somePropertyName])"; 

var syntax = SyntaxVisitor.Parse(formula);

foreach (var node in syntax)
{
    Console.WriteLine($"Found: {node}: {node.Range.ApplyTo(formula)}");
}


var compiled = ExpressionCompiler.Compile(formula, syntax, dataSet);


var dataSetEvaluator = new DataSetEvaluator();
var dataRow = new DataRow();
dataRow.Values.Add("foo.somePropertyName", 42D);
dataRow.Values.Add("hiredate", DateTimeOffset.Now.AddYears(-4));

var result = compiled.Invoke(dataSetEvaluator, dataRow);
Console.WriteLine($"Result: {result}");

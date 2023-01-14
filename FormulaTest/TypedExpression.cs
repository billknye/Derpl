using System.Linq.Expressions;

namespace FormulaTest;

public record TypedExpression(Expression Expression, DataType Type);

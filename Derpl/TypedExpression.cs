using System.Linq.Expressions;

namespace Derpl;

public record TypedExpression(Expression Expression, DataType Type, IEnumerable<ParameterExpression>? Variables = null);

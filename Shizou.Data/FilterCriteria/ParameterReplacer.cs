using System.Linq.Expressions;
using Shizou.Data.Models;

namespace Shizou.Data.FilterCriteria;

public static class ParameterReplacer
{
    public static Expression Replace(Expression<Func<AniDbAnime, bool>> criterion, ParameterExpression newParam)
    {
        var oldParam = criterion.Parameters.First();
        return new ParameterReplacerVisitor(oldParam, newParam).Visit(criterion.Body);
    }

    private class ParameterReplacerVisitor : ExpressionVisitor
    {
        private readonly ParameterExpression _oldParam;
        private readonly ParameterExpression _newParam;

        public ParameterReplacerVisitor(ParameterExpression oldParam, ParameterExpression newParam)
        {
            _oldParam = oldParam;
            _newParam = newParam;
        }

        protected override Expression VisitParameter(ParameterExpression node)
        {
            return node == _oldParam ? _newParam : base.VisitParameter(node);
        }
    }
}

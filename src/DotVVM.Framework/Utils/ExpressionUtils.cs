using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;

namespace DotVVM.Framework.Utils
{
    public static class ExpressionUtils
    {
        public static Expression Indexer(Expression instance, Expression index, Type returnType)
        {
            return Expression.Property(instance,
                            instance.Type.GetProperty("Item", returnType, new[] { index.Type }),
                            index);
        }

        public static Expression Replace(LambdaExpression ex, params Expression[] parameters)
        {
            var visitor = new ReplaceVisitor();
            for (int i = 0; i < parameters.Length; i++)
            {
                visitor.Params.Add(ex.Parameters[i], parameters[i]);
            }
            var result = visitor.Visit(ex.Body);
            if (result.CanReduce) result = result.Reduce();
            return result;
        }
        #region Replace overloads

        public static Expression Replace<T1, TRes>(Expression<Func<T1, TRes>> ex, Expression p1)
        {
            return Replace(ex as LambdaExpression, p1);
        }

        public static Expression Replace<T1, T2, TRes>(Expression<Func<T1, T2, TRes>> ex, Expression p1, Expression p2)
        {
            return Replace(ex as LambdaExpression, p1, p2);
        }

        public static Expression Replace<T1, T2, T3, TRes>(Expression<Func<T1, T2, T3, TRes>> ex, Expression p1, Expression p2, Expression p3)
        {
            return Replace(ex as LambdaExpression, p1, p2, p3);
        }

        public static Expression Replace<T1, T2, T3, T4, TRes>(Expression<Func<T1, T2, T3, T4, TRes>> ex, Expression p1, Expression p2, Expression p3, Expression p4)
        {
            return Replace(ex as LambdaExpression, p1, p2, p3, p4);
        }
        public static Expression Replace(Expression<Action> ex)
        {
            return Replace(ex as LambdaExpression);
        }

        public static Expression Replace<T1>(Expression<Action<T1>> ex, Expression p1)
        {
            return Replace(ex as LambdaExpression, p1);
        }

        public static Expression Replace<T1, T2>(Expression<Action<T1, T2>> ex, Expression p1, Expression p2)
        {
            return Replace(ex as LambdaExpression, p1, p2);
        }

        public static Expression Replace<T1, T2, T3>(Expression<Action<T1, T2, T3>> ex, Expression p1, Expression p2, Expression p3)
        {
            return Replace(ex as LambdaExpression, p1, p2, p3);
        }

        public static Expression Replace<T1, T2, T3, T4>(Expression<Action<T1, T2, T3, T4>> ex, Expression p1, Expression p2, Expression p3, Expression p4)
        {
            return Replace(ex as LambdaExpression, p1, p2, p3, p4);
        }
        #endregion

        private class ReplaceVisitor : ExpressionVisitor
        {
            public Dictionary<ParameterExpression, Expression> Params = new Dictionary<ParameterExpression, Expression>();
            protected override Expression VisitParameter(ParameterExpression node)
            {
                if (Params.ContainsKey(node)) return Params[node];
                else return base.VisitParameter(node);
            }
        }

        /// <summary>
        /// will execute operators, property and field accesses on constant expression, so it will be cleaner
        /// </summary>
        public static Expression OptimizeConstants(this Expression ex)
        {
            var v = new ConstantsOptimizingVisitor();
            return v.Visit(ex);
        }

        private class ConstantsOptimizingVisitor : ExpressionVisitor
        {
            protected override Expression VisitMember(MemberExpression node)
            {
                if (node.Member is PropertyInfo)
                {
                    var i = Visit(node.Expression);
                    if (i.NodeType == ExpressionType.Constant)
                    {
                        var ce = (ConstantExpression)i;
                        var prop = ce.Type.GetProperty(node.Member.Name);
                        var val = prop.GetValue(ce.Value);
                        return Expression.Constant(val, prop.PropertyType);
                    }
                    else return node;
                }
                else if (node.Member is FieldInfo)
                {
                    var i = Visit(node.Expression);
                    if (i.NodeType == ExpressionType.Constant)
                    {
                        var ce = (ConstantExpression)i;
                        var f = node.Member as FieldInfo;
                        var val = f.GetValue(ce.Value);
                        return Expression.Constant(val, f.FieldType);
                    }
                    else return node;
                }
                else return base.VisitMember(node);
            }
            protected override Expression VisitBinary(BinaryExpression node)
            {
                var l = Visit(node.Left);
                var lc = l as ConstantExpression;
                var r = Visit(node.Right);
                var rc = r as ConstantExpression;
                if (lc != null && rc != null)
                {
                    if (node.Method != null)
                        return Expression.Constant(node.Method.Invoke(null, new object[] { lc.Value, rc.Value }));
                    else throw new NotImplementedException("special cases without method not supported now");
                }
                else return base.VisitBinary(node);
            }

            protected override Expression VisitUnary(UnaryExpression node)
            {
                var op = Visit(node.Operand);
                if (op is ConstantExpression)
                {
                    return Expression.Constant(
                        node.Method.Invoke(null, new object[] { (op as ConstantExpression).Value }), node.Type);
                }
                else return base.VisitUnary(node);
            }
        }
    }
}

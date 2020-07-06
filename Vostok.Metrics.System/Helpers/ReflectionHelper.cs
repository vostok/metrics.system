using System;
using System.Linq.Expressions;
using System.Reflection;

namespace Vostok.Metrics.System.Helpers
{
    internal static class ReflectionHelper
    {
        public static Func<TObject, TProperty> BuildInstancePropertyAccessor<TObject, TProperty>(string propertyName)
        {
            try
            {
                var property = typeof(TObject).GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
                if (property == null)
                    return _ => default;

                var parameterExpression = Expression.Parameter(typeof(TObject));
                var propertyExpression = Expression.Property(parameterExpression, property);

                return Expression.Lambda<Func<TObject, TProperty>>(propertyExpression, parameterExpression).Compile();
            }
            catch
            {
                return _ => default;
            }
        }
    }
}

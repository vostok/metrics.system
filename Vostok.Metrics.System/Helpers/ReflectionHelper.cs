using System;
using System.Linq.Expressions;
using System.Reflection;

namespace Vostok.Metrics.System.Helpers
{
    internal static class ReflectionHelper
    {
        private const BindingFlags InstanceBindingFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        private const BindingFlags StaticBindingFlags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

        public static Func<TObject, TProperty> BuildInstancePropertyAccessor<TObject, TProperty>(string propertyName)
        {
            try
            {
                var property = typeof(TObject).GetProperty(propertyName, InstanceBindingFlags);
                if (property == null)
                    return _ => default;

                var parameterExpression = Expression.Parameter(typeof(TObject));
                var propertyExpression = Expression.Property(parameterExpression, property);

                return Expression.Lambda<Func<TObject, TProperty>>(propertyExpression, parameterExpression).Compile();
            }
            catch (Exception error)
            {
                InternalErrorLogger.Warn(error);

                return _ => default;
            }
        }

        public static Func<TProperty> BuildStaticPropertyAccessor<TProperty>(Type type, string propertyName)
        {
            try
            {
                var property = type.GetProperty(propertyName, StaticBindingFlags);
                if (property == null)
                    return () => default;

                return Expression.Lambda<Func<TProperty>>(Expression.Property(null, property)).Compile();
            }
            catch (Exception error)
            {
                InternalErrorLogger.Warn(error);

                return () => default;
            }
        }

        public static Func<TResult> BuildStaticMethodInvoker<TResult>(Type type, string methodName)
        {
            try
            {
                var method = type.GetMethod(methodName, StaticBindingFlags);
                if (method == null)
                    return () => default;

                return Expression.Lambda<Func<TResult>>(Expression.Call(method)).Compile();
            }
            catch (Exception error)
            {
                InternalErrorLogger.Warn(error);

                return () => default;
            }
        }

        public static Func<TParam, TResult> BuildStaticMethodInvoker<TParam, TResult>(Type type, string methodName)
        {
            try
            {
                var method = type.GetMethod(methodName, StaticBindingFlags, null, new [] {typeof(TParam)}, null);
                if (method == null)
                    return _ => default;

                var parameterExpression = Expression.Parameter(typeof(TParam));
                var methodCallExpression = Expression.Call(method, parameterExpression);

                return Expression.Lambda<Func<TParam, TResult>>(methodCallExpression, parameterExpression).Compile();
            }
            catch (Exception error)
            {
                InternalErrorLogger.Warn(error);

                return _ => default;
            }
        }
    }
}

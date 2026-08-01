using System.Reflection;
using QZBarberShopBooking.Application.DTO.Shared;
using QZBarberShopBooking.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq.Expressions;
using System.Text;

namespace QZBarberShopBooking.Application.Helpers
{
    public class FilterHelper
    {
        public static Expression? GetStringMethodCall(Expression property, string methodName, object value)
        {
            if (property is MemberExpression memberExpr)
            {
                var propertyType = GetMemberType(memberExpr.Member);

                if (propertyType == typeof(string))
                {
                    var method = typeof(string).GetMethod(methodName, new[] { typeof(string) })
                        ?? throw new ArgumentException($"String has no method '{methodName}(string)'", nameof(methodName));
                    return Expression.Call(property, method, Expression.Constant(value.ToString()));
                }
            }
            return null;
        }
        public static Expression? GetBoolenMethodCall(Expression property, string methodName, object value)
        {
            if (property is MemberExpression memberExpr)
            {
                var propertyType = GetMemberType(memberExpr.Member);

                if (propertyType == typeof(Boolean))
                {
                    if (!bool.TryParse(value.ToString(), out var boolValue))
                        return null;
                    var constant = BuildConstantExpression(boolValue, property.Type);
                    return Expression.Equal(property, constant);
                }
            }
            return null;
        }
        public static Expression? GetNumericMethodCall(Expression property, string methodName, object value)
        {
            if (property is MemberExpression memberExpr)
            {
                var propertyType = GetMemberType(memberExpr.Member);

                if (propertyType == typeof(int) || propertyType == typeof(int?))
                {
                    if (!int.TryParse(value.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var intValue))
                        return null;
                    var constant = BuildConstantExpression(intValue, property.Type);
                    return methodName switch
                    {
                        "Equals" => Expression.Equal(property, constant),
                        "GreaterThan" => Expression.GreaterThan(property, constant),
                        "LessThan" => Expression.LessThan(property, constant),
                        _ => null
                    };
                }
                else if (propertyType == typeof(decimal) || propertyType == typeof(decimal?))
                {
                    if (!decimal.TryParse(value.ToString(), NumberStyles.Number, CultureInfo.InvariantCulture, out var decimalValue))
                        return null;
                    var constant = BuildConstantExpression(decimalValue, property.Type);
                    return methodName switch
                    {
                        "Equals" => Expression.Equal(property, constant),
                        "GreaterThan" => Expression.GreaterThan(property, constant),
                        "LessThan" => Expression.LessThan(property, constant),
                        _ => null
                    };
                }
            }
            return null;
        }
        public static Expression? GetDateTimeMethodCall(Expression property, string methodName, object value)
        {
            if (property is MemberExpression memberExpr)
            {
                var propertyType = GetMemberType(memberExpr.Member);

                if (propertyType == typeof(DateTime) || propertyType == typeof(DateTime?))
                {
                    if (!DateTime.TryParse(value.ToString(), CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var dateValue))
                        return null;

                    var dateConstant = BuildConstantExpression(dateValue, property.Type);
                    return methodName switch
                    {
                        "Equals" => Expression.Equal(property, dateConstant),
                        "GreaterThan" => Expression.GreaterThan(property, dateConstant),
                        "LessThan" => Expression.LessThan(property, dateConstant),
                        _ => null
                    };
                }
            }
            return null;
        }

        private static Expression BuildConstantExpression(object value, Type targetType)
        {
            // If targetType is nullable, get underlying
            var underlying = Nullable.GetUnderlyingType(targetType) ?? targetType;
            var constant = Expression.Constant(Convert.ChangeType(value, underlying, CultureInfo.InvariantCulture), underlying);
            if (underlying != targetType)
            {
                return Expression.Convert(constant, targetType);
            }
            return constant;
        }

        //public static IQueryable<T> ApplyFilters<T>(IQueryable<T> query, Dictionary<string, List<FilterItem>> filters)
        //{
        //    foreach (var filter in filters)
        //    {
        //        string properties = filter.Key;

        //        string field = filter.Key;
        //        var filterItems = filter.Value;

        //        foreach (var item in filterItems)
        //        {
        //            var value = item.value;
        //            MatchModeEnum matchMode = item.matchMode;

        //            var parameter = Expression.Parameter(typeof(T), "x");
        //            var property = Expression.Property(parameter, field);

        //            Expression condition = null;

        //            switch (matchMode)
        //            {
        //                case MatchModeEnum.Contains:
        //                    condition = GetStringMethodCall(property, "Contains", value);
        //                    break;
        //                case MatchModeEnum.StartsWith:
        //                    condition = GetStringMethodCall(property, "StartsWith", value) ??
        //                    GetNumericMethodCall(property, "Equals", value) ??
        //                                GetDateTimeMethodCall(property, "Equals", value);
        //                    break;
        //                case MatchModeEnum.EndsWith:
        //                    condition = GetStringMethodCall(property, "EndsWith", value);
        //                    break;
        //                case MatchModeEnum.NotContains:
        //                    condition = Expression.Not(GetStringMethodCall(property, "Contains", value));
        //                    break;
        //                case MatchModeEnum.Equals:
        //                    condition = GetStringMethodCall(property, "Equals", value) ??
        //                                GetNumericMethodCall(property, "Equals", value) ??
        //                                GetBoolenMethodCall(property, "Equals", value) ??
        //                                GetDateTimeMethodCall(property, "Equals", value);
        //                    break;
        //                case MatchModeEnum.NotEquals:
        //                    condition = Expression.Not(GetStringMethodCall(property, "Equals", value) ??
        //                                                GetNumericMethodCall(property, "Equals", value) ??
        //                                                GetDateTimeMethodCall(property, "Equals", value));
        //                    break;
        //                case MatchModeEnum.GreaterThan:
        //                    condition = GetNumericMethodCall(property, "GreaterThan", value) ??
        //                                GetDateTimeMethodCall(property, "GreaterThan", value);
        //                    break;
        //                case MatchModeEnum.LessThan:
        //                    condition = GetNumericMethodCall(property, "LessThan", value) ??
        //                                GetDateTimeMethodCall(property, "LessThan", value);
        //                    break;
        //                default:
        //                    condition = GetStringMethodCall(property, "Contains", value);
        //                    break;
        //            }
        //            if (condition != null)
        //            {
        //                var lambda = Expression.Lambda<Func<T, bool>>(condition, parameter);
        //                query = query.AsQueryable().Where(lambda);
        //            }
        //        }
        //    }

        //    return query;
        //}

        public static IQueryable<T> ApplyFilters<T>(IQueryable<T> query, Dictionary<string, List<FilterItem>> filters)
        {
            foreach (var filter in filters)
            {
                string fieldPath = filter.Key;
                var filterItems = filter.Value;

                foreach (var item in filterItems)
                {
                    var value = item.Value;
                    var matchMode = item.MatchMode;

                    var parameter = Expression.Parameter(typeof(T), "x");
                    Expression property = GetNestedPropertyExpression(parameter, fieldPath);

                    if (property == null) continue;

                    Expression condition = matchMode switch
                    {
                        MatchModeEnum.Contains => GetStringMethodCall(property, "Contains", value),
                        MatchModeEnum.StartsWith => GetStringMethodCall(property, "StartsWith", value)
                            ?? GetNumericMethodCall(property, "Equals", value)
                            ?? GetDateTimeMethodCall(property, "Equals", value),
                        MatchModeEnum.EndsWith => GetStringMethodCall(property, "EndsWith", value),
                        MatchModeEnum.NotContains => Expression.Not(GetStringMethodCall(property, "Contains", value)),
                        MatchModeEnum.Equals => GetStringMethodCall(property, "Equals", value)
                            ?? GetNumericMethodCall(property, "Equals", value)
                            ?? GetBoolenMethodCall(property, "Equals", value)
                            ?? GetDateTimeMethodCall(property, "Equals", value),
                        MatchModeEnum.NotEquals => Expression.Not(
                                GetStringMethodCall(property, "Equals", value)
                                ?? GetNumericMethodCall(property, "Equals", value)
                                ?? GetDateTimeMethodCall(property, "Equals", value)),
                        MatchModeEnum.GreaterThan => GetNumericMethodCall(property, "GreaterThan", value)
                            ?? GetDateTimeMethodCall(property, "GreaterThan", value),
                        MatchModeEnum.LessThan => GetNumericMethodCall(property, "LessThan", value)
                            ?? GetDateTimeMethodCall(property, "LessThan", value),
                        _ => GetStringMethodCall(property, "Contains", value)
                    };

                    if (condition != null)
                    {
                        var lambda = Expression.Lambda<Func<T, bool>>(condition, parameter);
                        query = query.Where(lambda);
                    }
                }
            }

            return query;
        }

        private static Expression? GetNestedPropertyExpression(Expression parameter, string propertyPath)
        {
            try
            {
                Expression property = parameter;
                foreach (var prop in propertyPath.Split('.'))
                {
                    property = Expression.PropertyOrField(property, prop);
                }
                return property;
            }
            catch
            {
                return null;
            }
        }

        private static Type GetMemberType(MemberInfo member)
        {
            return member switch
            {
                PropertyInfo p => p.PropertyType,
                FieldInfo f => f.FieldType,
                _ => typeof(object)
            };
        }
    }
}

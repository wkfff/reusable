﻿using System;
using System.Linq.Expressions;
using JetBrains.Annotations;
using Reusable.Extensions;

namespace Reusable.Flawless
{
    [PublicAPI]
    public static class ExpressValidatorBuilderExtensions
    {
        #region Ensure overloads

        public static ExpressValidationRuleBuilder<T> Ensure<T>(this ExpressValidatorBuilder<T> builder, Expression<Func<T, bool>> expression)
        {
            return builder.Policy(expression);
        }

        public static ExpressValidationRuleBuilder<T> EnsureNull<T>(this ExpressValidatorBuilder<T> builder)
        {
            return
                builder
                    .Ensure(IsNullExpression.Create<T>())
                    .WithMessage($"{typeof(T).ToPrettyString()} must be null.")
                    .BreakOnFailure();
        }

        #endregion

        #region Block overloads

        public static ExpressValidationRuleBuilder<T> IsNotValidWhen<T>(this ExpressValidatorBuilder<T> builder, Expression<Func<T, bool>> expression)
        {
            var notExpression = Expression.Lambda<Func<T, bool>>(Expression.Not(expression.Body), expression.Parameters[0]);
            return builder.Ensure(notExpression);
        }

        public static ExpressValidationRuleBuilder<T> BlockNull<T>(this ExpressValidatorBuilder<T> builder)
        {
            return
                builder
                    .IsNotValidWhen(IsNullExpression.Create<T>())
                    .WithMessage($"{typeof(T).ToPrettyString()} must not be null.")
                    .BreakOnFailure();
        }

        #endregion
    }

    internal static class IsNullExpression
    {
        public static Expression<Func<T, bool>> Create<T>(Expression<Func<T, bool>> expression)
        {
            return Create<T>(expression.Parameters[0]);
        }

        public static Expression<Func<T, bool>> Create<T>()
        {
            return Create<T>(Expression.Parameter(typeof(T)));
        }

        private static Expression<Func<T, bool>> Create<T>(ParameterExpression parameter)
        {
            //var parameter = Expression.Parameter(typeof(T));
            var equalsNullExpression =
                Expression.Lambda<Func<T, bool>>(
                    Expression.ReferenceEqual(
                        parameter,
                        Expression.Constant(default(T))),
                    parameter
                );

            return equalsNullExpression;
        }
    }
}
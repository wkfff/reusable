﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Reusable.Extensions;
using Reusable.Flexo;
using Reusable.Flexo.Abstractions;
using Reusable.Flexo.Expressions;
using Reusable.Flexo.Extensions;

namespace Reusable.OmniLog.Expressions
{
    public static class ExpressionContextExtensions
    {
        public static Log Log(this IExpressionContext context) => context.Item<Log>();

        public static TExpressionContext Log<TExpressionContext>(this TExpressionContext context, Log log) where TExpressionContext : IExpressionContext
        {
            return context.Item(log);
        }
    }

    public class GetLogLevel : Expression
    {
        public GetLogLevel() : base(nameof(GetLogLevel)) { }

        public override IExpression Invoke(IExpressionContext context) => Constant.Create(nameof(OmniLog.LogLevel), context.Log().Level());
    }

    public class GetLoggerName : Expression
    {
        public GetLoggerName() : base(nameof(GetLoggerName)) { }

        public override IExpression Invoke(IExpressionContext context) => Constant.Create(nameof(OmniLog.LogLevel), context.Log().Name());
    }

    public static class ExpressionSerializerFactory
    {
        public static IExpressionSerializer CreateSerializer(IEnumerable<Type> otherTypes = null, Action<JsonSerializer> configureSerializer = null)
        {
            var ownTypes = new[]
            {
                typeof(Reusable.OmniLog.LogLevel),
                typeof(Reusable.OmniLog.Expressions.GetLoggerName),
                typeof(Reusable.OmniLog.Expressions.GetLogLevel),
            };

            return new ExpressionSerializer
            (
                otherTypes: ownTypes.Concat(otherTypes ?? Enumerable.Empty<Type>()),
                configureSerializer: serializer =>
                {
                    serializer.Converters.Add(new LogLevelConverter());
                    configureSerializer?.Invoke(serializer);
                }
            );
        }
    }
}
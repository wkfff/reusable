﻿using System;
using System.Collections.Immutable;
using System.Linq;
using Reusable.Flexo.Abstractions;
using Reusable.Flexo.Extensions;
using Reusable.Reflection;

namespace Reusable.Flexo.Expressions
{
    public abstract class Map<TFrom, TTo> : Expression 
    {
        protected Map() : base(nameof(Map<TFrom, TTo>)) { }

        protected Map(string name) : base(name) { }

        public IImmutableList<Mapping<TFrom, TTo>> Mappings { get; set; }

        public IExpression Expression { get; set; }

        public override IExpression Invoke(IExpressionContext context)
        {
            var result = Expression.SafeInvoke(context);
            if (result is Constant<TFrom> constant)
            {
                var match = 
                    Mappings.
                        FirstOrDefault(mapping => mapping.FromValue.Equals(constant.Value))
                        ?? throw DynamicException.Factory.CreateDynamicException($"MappingNotFound{nameof(Exception)}", $"Could not find mapping for '{constant.Value}'.");

                return Constant.Create(Name, match.ToValue);
            }

            throw new InvalidExpressionException(typeof(Constant), result.GetType());
        }
    }

    public class Mapping<TFrom, TTo> 
    {
        public TFrom FromValue { get; set; }

        public TTo ToValue { get; set; }
    }

    /// <summary>
    /// Provides a mapping expression from bool to double.
    /// </summary>
    public class ToDouble : Map<bool, double>
    {
        public ToDouble() : base(nameof(ToDouble))
        {
            Mappings = ImmutableList.Create
            (
                new Mapping<bool, double> { FromValue = true, ToValue = 1.0 },
                new Mapping<bool, double> { FromValue = false, ToValue = 0.0 }
            );
        }
    }
}
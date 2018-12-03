﻿using System;
using System.ComponentModel;
using System.Text.RegularExpressions;

namespace Reusable.Flexo
{
    public class Equals : PredicateExpression
    {
        public Equals() : base(nameof(Equals)) { }

        [DefaultValue(true)]
        public bool IgnoreCase { get; set; } = true;

        public IExpression Left { get; set; }

        public IExpression Right { get; set; }

        protected override bool Calculate(IExpressionContext context)
        {
            var x = Left.InvokeWithValidation(context).ValueOrDefault();
            var y = Right.InvokeWithValidation(context).ValueOrDefault();

            if (x is string str1 && y is string str2 && IgnoreCase)
            {
                return StringComparer.OrdinalIgnoreCase.Equals(str1, str2);
            }

            return x.Equals(y);
        }
    }

    public class Matches : PredicateExpression
    {
        protected Matches() : base(nameof(Matches)) { }

        [DefaultValue(true)]
        public bool IgnoreCase { get; set; } = true;

        public IExpression Expression { get; set; }

        public string Pattern { get; set; }

        protected override bool Calculate(IExpressionContext context)
        {
            var x = Expression.InvokeWithValidation(context).Value<string>();

            return !(x is null) && Regex.IsMatch(x, Pattern, IgnoreCase ? RegexOptions.IgnoreCase : RegexOptions.None);
        }
    }
}
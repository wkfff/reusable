﻿using Reusable.Flexo.Abstractions;

namespace Reusable.Flexo.Expressions.Operators.Binary
{
    public class LessThan : ComparerExpression
    {
        public LessThan()
            : base(nameof(LessThan), x => x < 0)
        { }
    }

    public class LessThanOrEqual : ComparerExpression
    {
        public LessThanOrEqual()
            : base(nameof(LessThanOrEqual), x => x <= 0)
        { }
    }
}
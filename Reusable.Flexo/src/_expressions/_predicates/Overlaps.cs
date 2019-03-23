using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace Reusable.Flexo
{
    public class Overlaps : PredicateExpression, IExtension<List<object>>
    {
        [JsonConstructor]
        public Overlaps(string name) : base(name ?? nameof(Overlaps)) { }

        public List<IExpression> Values { get; set; } = new List<IExpression>();

        public List<IExpression> With { get; set; } = new List<IExpression>();

        public IExpression Comparer { get; set; }

        protected override Constant<bool> InvokeCore(IExpressionContext context)
        {
            var with = With.Values<object>();
            var comparer = (IEqualityComparer<object>)Comparer?.Invoke(context).Value ?? EqualityComparer<object>.Default;

            if (context.TryPopExtensionInput(out IEnumerable<object> input))
            {
                return (Name, input.Intersect(with, comparer).Any(), context);
            }
            else
            {
                var values = Values.Enabled().Select(x => x.Invoke(context)).Values<object>();
                return (Name, values.Intersect(with, comparer).Any(), context);
            }
        }
    }
}
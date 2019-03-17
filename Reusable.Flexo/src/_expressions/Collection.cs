using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace Reusable.Flexo
{
    public class Collection : Expression
    {
        [JsonConstructor]
        public Collection(SoftString name) : base(name) { }

        public List<object> Values { get; set; }

        protected override IExpression InvokeCore(IExpressionContext context)
        {
            var values = Values.Select(v => v is IExpression expression ? expression : Constant.FromValue("CollectionItem", v)).ToList();
            return Constant.FromValue(Name, values);
        }
    }
}
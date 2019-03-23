using System.ComponentModel;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Reusable.Exceptionize;
using Reusable.Extensions;

namespace Reusable.Flexo
{
    public class Matches : PredicateExpression, IExtension<string>
    {
        public Matches() : base(nameof(Matches)) { }

        public bool IgnoreCase { get; set; } = true;

        public IExpression Value { get; set; }

        public IExpression Pattern { get; set; }

        protected override Constant<bool> InvokeCore(IExpressionContext context)
        {
            var pattern = Pattern.Invoke(context).Value<string>();
            var options = IgnoreCase ? RegexOptions.IgnoreCase : RegexOptions.None;

            if (context.TryPopExtensionInput(out string input))
            {
                return (Name, Regex.IsMatch(input, pattern, options), context);
            }
            else
            {
                var value = Value.Invoke(context).Value<string>();
                return (Name, Regex.IsMatch(value, pattern, options), context);
            }
        }

//        private string GetPattern(IExpressionContext context)
//        {
//            switch (Pattern)
//            {
//                case string pattern: return pattern;
//                case IExpression expression: return expression.Invoke(context).Value<string>();
//                default:
//                    throw DynamicException.Create
//                    (
//                        "UnsupportedPattern",
//                        $"{Pattern.GetType().ToPrettyString()} is not supported. Expected '{nameof(String)}' or '{nameof(IExpression)}'."
//                    );
//            }
//        }
    }
}
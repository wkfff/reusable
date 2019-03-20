using JetBrains.Annotations;
using Reusable.Exceptionize;

namespace Reusable.Flexo
{
    [UsedImplicitly]
    [PublicAPI]
    public class Throw : Expression
    {
        public Throw(SoftString name) : base(name, ExpressionContext.Empty) { }

        //public string Exception { get; set; }
        
        public string Message { get; set; }

        protected override IExpression InvokeCore(IExpressionContext context)
        {
            throw DynamicException.Create(Name.ToString(), Message);
        }
    }
}
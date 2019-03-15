using JetBrains.Annotations;
using Newtonsoft.Json;
using Reusable.Exceptionizer;

namespace Reusable.Flexo
{
    [UsedImplicitly]
    [PublicAPI]
    public class GetContextItem : Expression
    {
        public GetContextItem() : base(nameof(GetContextItem)) { }

        [JsonRequired]
        public string Key { get; set; }

        public override IExpression Invoke(IExpressionContext context)
        {
            return
                context.TryGetValue(Key, out var value)
                    ? Constant.FromValue(Key, value)
                    : throw DynamicException.Create("KeyNotFound", $"Context does not contain an item with the key '{Key}'.");
        }
    }
}
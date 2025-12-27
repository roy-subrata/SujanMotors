namespace AutoPartsShop.Domain.Entities
{
    public sealed class CodeSequence
    {
        public string Prefix { get; set; } = null!;
        public int LastNumber { get; set; }
    }
}

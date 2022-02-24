namespace Files.Filesystem.Search
{
    public enum RangeDirections : ushort
    {
        None,
        All,
        EqualTo,
        GreaterThan,
        LessThan,
        Between,
    }

    public interface IRange<out T>
    {
        RangeDirections Direction { get; }

        T MinValue { get; }
        T MaxValue { get; }

        IRange<string> Label { get; }
    }

    internal struct RangeLabel : IRange<string>
    {
        public static RangeLabel None { get; } = new(string.Empty, string.Empty);

        public RangeDirections Direction { get; }

        public string MinValue { get; }
        public string MaxValue { get; }

        IRange<string> IRange<string>.Label => this;
        public RangeLabel Label => this;

        public RangeLabel(string value) : this(value, value) {}
        public RangeLabel(string minValue, string maxValue)
        {
            MinValue = (minValue ?? string.Empty).Trim();
            MaxValue = (maxValue ?? string.Empty).Trim();

            Direction = (MinValue, MaxValue) switch
            {
                ("", "") => RangeDirections.All,
                _ when MinValue == MaxValue => RangeDirections.EqualTo,
                (_, "") => RangeDirections.GreaterThan,
                ("", _) => RangeDirections.LessThan,
                _ => RangeDirections.Between,
            };
        }

        public void Deconstruct(out string minValue, out string maxValue)
            => (minValue, maxValue) = (MinValue, MaxValue);
        public void Deconstruct(out RangeDirections direction, out string minValue, out string maxValue)
            => (direction, minValue, maxValue) = (Direction, MinValue, MaxValue);

        public override string ToString() => Direction switch
        {
            RangeDirections.EqualTo => MinValue,
            RangeDirections.GreaterThan => $"> {MinValue}",
            RangeDirections.LessThan => $"< {MaxValue}",
            RangeDirections.Between => $"{MinValue} - {MaxValue}",
            _ => string.Empty,
        };
    }
}

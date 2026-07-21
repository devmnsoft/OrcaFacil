namespace OrcaFacil.Domain.ValueObjects;

public sealed record Money
{
    public decimal Value { get; }

    public Money(decimal value, bool allowNegative = false)
    {
        if (!allowNegative && value < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(value), "Valor monetário não pode ser negativo.");
        }

        Value = Math.Round(value, 2);
    }

    public static Money operator +(Money left, Money right) => new(left.Value + right.Value);

    public static Money operator -(Money left, Money right) => new(left.Value - right.Value, true);
}

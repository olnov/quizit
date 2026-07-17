using System.ComponentModel.DataAnnotations;

namespace Backend.Shared;

public sealed class MultipleOfAttribute : ValidationAttribute
{
    private readonly int _divisor;

    public MultipleOfAttribute(int divisor)
    {
        _divisor = divisor;
        ErrorMessage = $"The value must be a multiple of {divisor}.";
    }

    public override bool IsValid(object? value)
    {
        return value is null || value is int number && number % _divisor == 0;
    }
}

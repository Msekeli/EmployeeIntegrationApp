namespace EmployeeIntegration.Domain;

public sealed class ValidationResult
{
    public bool IsValid => Errors.Count == 0;
    public IReadOnlyList<ValidationError> Errors { get; }

    private ValidationResult(IReadOnlyList<ValidationError> errors)
    {
        Errors = errors;
    }

    public static ValidationResult Success() => new(Array.Empty<ValidationError>());

    public static ValidationResult Failure(IReadOnlyList<ValidationError> errors) => new(errors);

    public static ValidationResult Failure(string field, string message) =>
        new(new[] { new ValidationError(field, message) });
}

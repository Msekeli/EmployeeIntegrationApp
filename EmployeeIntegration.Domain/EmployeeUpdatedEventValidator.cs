using System.Text.RegularExpressions;

namespace EmployeeIntegration.Domain;

/// <summary>
/// Validates incoming EmployeeUpdated events according to domain rules.
/// </summary>
public static class EmployeeUpdatedEventValidator
{
    private static readonly Regex EmailRegex = new(
        @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public static ValidationResult Validate(EmployeeUpdatedEvent? @event)
    {
        if (@event == null)
        {
            return ValidationResult.Failure("Event", "Event payload cannot be null.");
        }

        var errors = new List<ValidationError>();

        // Event metadata validation
        if (string.IsNullOrWhiteSpace(@event.EventId))
        {
            errors.Add(new ValidationError(nameof(@event.EventId), "EventId is required."));
        }

        if (string.IsNullOrWhiteSpace(@event.EventType))
        {
            errors.Add(new ValidationError(nameof(@event.EventType), "EventType is required."));
        }
        else if (!string.Equals(@event.EventType.Trim(), "EmployeeUpdated", StringComparison.OrdinalIgnoreCase))
        {
            errors.Add(new ValidationError(nameof(@event.EventType), "EventType must be 'EmployeeUpdated'."));
        }

        var timestamp = @event.GetEffectiveTimestamp();
        if (timestamp == null || timestamp == default(DateTimeOffset) || timestamp == DateTimeOffset.MinValue)
        {
            errors.Add(new ValidationError("EffectiveDate", "EffectiveDate or Timestamp is required."));
        }

        if (@event.SequenceNumber.HasValue && @event.SequenceNumber.Value < 0)
        {
            errors.Add(new ValidationError(nameof(@event.SequenceNumber), "SequenceNumber must be non-negative if provided."));
        }

        // Employee payload validation
        var payload = @event.GetEffectivePayload();

        if (string.IsNullOrWhiteSpace(payload.EmployeeId))
        {
            errors.Add(new ValidationError(nameof(payload.EmployeeId), "EmployeeId is required."));
        }

        if (string.IsNullOrWhiteSpace(payload.FirstName))
        {
            errors.Add(new ValidationError(nameof(payload.FirstName), "FirstName is required."));
        }

        if (string.IsNullOrWhiteSpace(payload.LastName))
        {
            errors.Add(new ValidationError(nameof(payload.LastName), "LastName is required."));
        }

        if (!string.IsNullOrWhiteSpace(payload.Email) && !EmailRegex.IsMatch(payload.Email.Trim()))
        {
            errors.Add(new ValidationError(nameof(payload.Email), "Email format is invalid."));
        }

        if (string.IsNullOrWhiteSpace(payload.Department))
        {
            errors.Add(new ValidationError(nameof(payload.Department), "Department is required."));
        }

        return errors.Count > 0 ? ValidationResult.Failure(errors) : ValidationResult.Success();
    }
}

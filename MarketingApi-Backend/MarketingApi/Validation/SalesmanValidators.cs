using System.Text.RegularExpressions;
using FluentValidation;
using MarketingApi.DTOs.Salesman;

namespace MarketingApi.Validation;

public static partial class ValidationPatterns
{
    // Supports Unicode letters (e.g., accents, Tamil) alongside spaces and common punctuation (M13)
    public const string PersonNameRegex = @"^[\p{L}\p{M}\s.'-]+$";
    public const string CodeRegex = @"^[a-zA-Z0-9]+$";
    public const string MobileRegex = @"^[0-9]{7,15}$";
    public const string EmailRegex = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
    public const string TeamTextRegex = @"^[\p{L}\p{M}0-9\s&.\-/'(),!?""]+$";

    public static bool IsValidPersonName(string? value) =>
        !string.IsNullOrWhiteSpace(value) && Regex.IsMatch(value.Trim(), PersonNameRegex);

    public static bool IsValidCode(string? value) =>
        !string.IsNullOrWhiteSpace(value) && Regex.IsMatch(value.Trim(), CodeRegex);

    public static bool IsValidMobile(string? value) =>
        !string.IsNullOrWhiteSpace(value) && Regex.IsMatch(value.Trim(), MobileRegex);

    public static bool IsValidEmail(string? value) =>
        !string.IsNullOrWhiteSpace(value) && Regex.IsMatch(value.Trim(), EmailRegex);

    public static bool IsValidTeamText(string? value) =>
        !string.IsNullOrWhiteSpace(value) && Regex.IsMatch(value.Trim(), TeamTextRegex);
}

public sealed class CreateSalesmanDtoValidator : AbstractValidator<CreateSalesmanDto>
{
    public CreateSalesmanDtoValidator()
    {
        RuleFor(x => x.Salesmancode)
            .NotEmpty().WithMessage("Salesman code is required.")
            .Length(2, 20).WithMessage("Salesman code must be between 2 and 20 characters.")
            .Must(ValidationPatterns.IsValidCode).WithMessage("Salesman code must contain only alphanumeric characters (A-Z, 0-9).");

        RuleFor(x => x.Salesmanname)
            .NotEmpty().WithMessage("Salesman name is required.")
            .MaximumLength(100).WithMessage("Salesman name cannot exceed 100 characters.")
            .Must(ValidationPatterns.IsValidPersonName).WithMessage("Salesman name contains unsupported characters.");

        RuleFor(x => x.Mobileno)
            .NotEmpty().WithMessage("Mobile number is required.")
            .Must(ValidationPatterns.IsValidMobile).WithMessage("Mobile number must be between 7 and 15 digits.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .MaximumLength(100).WithMessage("Email cannot exceed 100 characters.")
            .Must(ValidationPatterns.IsValidEmail).WithMessage("Invalid email format.");

        RuleFor(x => x.Joiningdate)
            .NotNull().WithMessage("Joining date is required.")
            .Must(d => !d.HasValue || d.Value <= DateOnly.FromDateTime(DateTime.UtcNow))
            .WithMessage("Joining date cannot be in the future.");
    }
}

public sealed class UpdateSalesmanDtoValidator : AbstractValidator<UpdateSalesmanDto>
{
    public UpdateSalesmanDtoValidator()
    {
        RuleFor(x => x.Salesmanname)
            .NotEmpty().WithMessage("Salesman name is required.")
            .MaximumLength(100).WithMessage("Salesman name cannot exceed 100 characters.")
            .Must(ValidationPatterns.IsValidPersonName).WithMessage("Salesman name contains unsupported characters.");

        RuleFor(x => x.Mobileno)
            .NotEmpty().WithMessage("Mobile number is required.")
            .Must(ValidationPatterns.IsValidMobile).WithMessage("Mobile number must be between 7 and 15 digits.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .MaximumLength(100).WithMessage("Email cannot exceed 100 characters.")
            .Must(ValidationPatterns.IsValidEmail).WithMessage("Invalid email format.");

        RuleFor(x => x.Joiningdate)
            .NotNull().WithMessage("Joining date is required.")
            .Must(d => !d.HasValue || d.Value <= DateOnly.FromDateTime(DateTime.UtcNow))
            .WithMessage("Joining date cannot be in the future.");
    }
}

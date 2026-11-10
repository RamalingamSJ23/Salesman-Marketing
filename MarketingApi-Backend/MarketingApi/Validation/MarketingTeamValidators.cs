using FluentValidation;
using MarketingApi.DTOs.MarketingTeam;

namespace MarketingApi.Validation;

public sealed class CreateMarketingTeamDtoValidator : AbstractValidator<CreateMarketingTeamDto>
{
    public CreateMarketingTeamDtoValidator()
    {
        RuleFor(x => x.Teamcode)
            .NotEmpty().WithMessage("Team code is required.")
            .Length(2, 20).WithMessage("Team code must be between 2 and 20 characters.")
            .Must(ValidationPatterns.IsValidCode).WithMessage("Team code must contain only alphanumeric characters (A-Z, 0-9).");

        RuleFor(x => x.Teamname)
            .NotEmpty().WithMessage("Team name is required.")
            .Length(2, 100).WithMessage("Team name must be between 2 and 100 characters.")
            .Must(ValidationPatterns.IsValidTeamText).WithMessage("Team name contains unsupported characters.");

        RuleFor(x => x.Teamleaderid)
            .GreaterThan(0).WithMessage("Team leader is required.");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Description is required.")
            .Length(2, 500).WithMessage("Description must be between 2 and 500 characters.")
            .Must(ValidationPatterns.IsValidTeamText).WithMessage("Description contains unsupported characters.");
    }
}

public sealed class UpdateMarketingTeamDtoValidator : AbstractValidator<UpdateMarketingTeamDto>
{
    public UpdateMarketingTeamDtoValidator()
    {
        RuleFor(x => x.Teamname)
            .NotEmpty().WithMessage("Team name is required.")
            .Length(2, 100).WithMessage("Team name must be between 2 and 100 characters.")
            .Must(ValidationPatterns.IsValidTeamText).WithMessage("Team name contains unsupported characters.");

        RuleFor(x => x.Teamleaderid)
            .GreaterThan(0).WithMessage("Team leader is required.");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Description is required.")
            .Length(2, 500).WithMessage("Description must be between 2 and 500 characters.")
            .Must(ValidationPatterns.IsValidTeamText).WithMessage("Description contains unsupported characters.");
    }
}

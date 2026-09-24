using FluentValidation;

namespace algo.Application.Features.Products.Commands.UpdateProduct;

public sealed class UpdateProductCommandValidator : AbstractValidator<UpdateProductCommand>
{
    public UpdateProductCommandValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0);

        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(256);

        RuleFor(x => x.CategoryId)
            .GreaterThan(0);

        RuleFor(x => x)
            .Must(x => x.PriceUsd.HasValue || x.PriceSyp.HasValue)
            .WithMessage("At least one price (USD or SYP) is required.");

        RuleFor(x => x.PriceUsd)
            .GreaterThanOrEqualTo(0)
            .When(x => x.PriceUsd.HasValue);

        RuleFor(x => x.PriceSyp)
            .GreaterThanOrEqualTo(0)
            .When(x => x.PriceSyp.HasValue);

        RuleFor(x => x.DiscountedPriceUsd)
            .GreaterThanOrEqualTo(0)
            .When(x => x.DiscountedPriceUsd.HasValue);

        RuleFor(x => x.DiscountedPriceSyp)
            .GreaterThanOrEqualTo(0)
            .When(x => x.DiscountedPriceSyp.HasValue);

        RuleFor(x => x.ExternalGameId)
            .MaximumLength(128);

        RuleFor(x => x.Provider)
            .MaximumLength(256);

        RuleFor(x => x.ImageUrl)
            .MaximumLength(2048);
    }
}

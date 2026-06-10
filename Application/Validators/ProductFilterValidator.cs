using Application.DTOs;
using FluentValidation;

namespace Application.Validators
{
    public class ProductFilterValidator : AbstractValidator<ProductFilterDto>
    {
        public ProductFilterValidator()
        {
            RuleFor(x => x.Page)
                .GreaterThanOrEqualTo(1).WithMessage("Page must be at least 1");

            RuleFor(x => x.PageSize)
                .InclusiveBetween(1, 100).WithMessage("Page size must be between 1 and 100");

            RuleFor(x => x.MinPrice)
                .GreaterThanOrEqualTo(0).When(x => x.MinPrice.HasValue)
                .WithMessage("Min price cannot be negative");

            RuleFor(x => x.MaxPrice)
                .GreaterThanOrEqualTo(0).When(x => x.MaxPrice.HasValue)
                .WithMessage("Max price cannot be negative");

            RuleFor(x => x)
                .Must(x => !x.MinPrice.HasValue || !x.MaxPrice.HasValue || x.MinPrice <= x.MaxPrice)
                .WithMessage("Min price cannot be greater than max price");

            RuleFor(x => x.SortBy)
                .Must(s => s == null || new[] { "name", "price", "createdAt", "updatedAt" }.Contains(s))
                .WithMessage("Sort by must be name, price, createdAt, or updatedAt");
        }
    }
}

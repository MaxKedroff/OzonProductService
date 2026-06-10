using Application.DTOs;
using FluentValidation;

namespace Application.Validators
{
    public class CreateProductValidator : AbstractValidator<CreateProductDto>
    {
        public CreateProductValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Product name is required")
                .MaximumLength(200).WithMessage("Product name cannot exceed 200 characters");

            RuleFor(x => x.Description)
                .MaximumLength(2000).WithMessage("Description cannot exceed 2000 characters");

            RuleFor(x => x.Price)
                .GreaterThan(0).WithMessage("Price must be greater than 0")
                .LessThan(1_000_000_000).WithMessage("Price cannot exceed 1,000,000,000");

            RuleFor(x => x.Currency)
                .NotEmpty().WithMessage("Currency is required")
                .Length(3).WithMessage("Currency must be 3 characters")
                .Must(c => new[] { "USD", "EUR", "GBP", "RUB" }.Contains(c))
                .WithMessage("Unsupported currency");

            RuleFor(x => x.Sku)
                .NotEmpty().WithMessage("SKU is required")
                .Matches(@"^[A-Z0-9\-]+$").WithMessage("SKU must contain only uppercase letters, numbers, and hyphens")
                .MaximumLength(50).WithMessage("SKU cannot exceed 50 characters");

            RuleFor(x => x.Category)
                .NotEmpty().WithMessage("Category is required");

            RuleFor(x => x.InitialStock)
                .GreaterThanOrEqualTo(0).WithMessage("Initial stock cannot be negative");

            RuleFor(x => x.LeadTimeDays)
                .InclusiveBetween(0, 30).WithMessage("Lead time must be between 0 and 30 days");
        }
    }
}

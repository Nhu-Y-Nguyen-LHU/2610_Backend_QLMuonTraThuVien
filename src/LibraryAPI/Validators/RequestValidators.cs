using FluentValidation;
using LibraryAPI.DTOs;
using LibraryAPI.Entities;

namespace LibraryAPI.Validators;

public sealed class LoginRequestValidator : AbstractValidator<LoginRequestDto>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Username).NotEmpty();
        RuleFor(x => x.Password).NotEmpty();
    }
}

public sealed class RegisterRequestValidator : AbstractValidator<RegisterRequestDto>
{
    public RegisterRequestValidator()
    {
        RuleFor(x => x.Username).NotEmpty().Length(3, 100);
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8);
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(150);
    }
}

public sealed class CreateBookValidator : AbstractValidator<CreateBookDto>
{
    public CreateBookValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(255);
        RuleFor(x => x.Author).NotEmpty().MaximumLength(150);
        RuleFor(x => x.ISBN).NotEmpty().MaximumLength(30);
        RuleFor(x => x.TotalCopies).GreaterThanOrEqualTo(0);
    }
}

public sealed class UpdateBookValidator : AbstractValidator<UpdateBookDto>
{
    public UpdateBookValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(255);
        RuleFor(x => x.Author).NotEmpty().MaximumLength(150);
        RuleFor(x => x.ISBN).NotEmpty().MaximumLength(30);
        RuleFor(x => x.TotalCopies).GreaterThanOrEqualTo(0);
    }
}

public sealed class BorrowRequestValidator : AbstractValidator<BorrowRequestDto>
{
    public BorrowRequestValidator()
    {
        RuleFor(x => x.BookCopyIds)
            .NotEmpty()
            .Must(x => x.Count <= 5)
            .WithMessage("A maximum of 5 copies can be borrowed.");
        RuleForEach(x => x.BookCopyIds).GreaterThan(0);
        RuleFor(x => x.ExpectedReturnDate).GreaterThan(DateTime.UtcNow);
    }
}

public sealed class CreateUserValidator : AbstractValidator<CreateUserDto>
{
    public CreateUserValidator()
    {
        RuleFor(x => x.Username).NotEmpty().Length(3, 100);
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8);
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Role).Must(x => x is Roles.Admin or Roles.Librarian or Roles.Reader);
    }
}

public sealed class UpdateUserValidator : AbstractValidator<UpdateUserDto>
{
    public UpdateUserValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Role).Must(x => x is Roles.Admin or Roles.Librarian or Roles.Reader);
        RuleFor(x => x.Status).Must(x => x is RecordStatuses.Active or RecordStatuses.Inactive);
    }
}

public sealed class CreateCopyValidator : AbstractValidator<CreateCopyDto>
{
    public CreateCopyValidator()
    {
        RuleFor(x => x.BookTitleId).GreaterThan(0);
        RuleFor(x => x.Barcode).NotEmpty().MaximumLength(80);
    }
}

public sealed class ReturnBookValidator : AbstractValidator<ReturnBookDto>
{
    public ReturnBookValidator()
    {
        RuleFor(x => x.ReturnDate).LessThanOrEqualTo(DateTime.UtcNow.AddMinutes(1));
    }
}

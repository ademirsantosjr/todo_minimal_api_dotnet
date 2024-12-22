using FluentValidation;
using TodoMinimalApi.DTOs;

namespace TodoMinimalApi.Validators
{
    public class TodoValidator : AbstractValidator<TodoDto>
    {
        public TodoValidator()
        {
            RuleFor(t => t.Title).NotEmpty().WithMessage("O Título é obrigatório.");
            RuleFor(t => t.Description).MaximumLength(500).WithMessage("A Descrição não pode exceder 500 caracteres.");
            RuleFor(t => t.UserId).GreaterThan(0).WithMessage("O ID do usuário deve ser maior que zero.");
        }
    }
}

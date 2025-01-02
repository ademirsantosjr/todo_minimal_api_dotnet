using FluentValidation;
using TodoMinimalApi.DTOs;

namespace TodoMinimalApi.Validators
{
    public class TodoValidator : AbstractValidator<TodoDto>
    {
        public TodoValidator()
        {
            RuleFor(t => t.Title).NotEmpty().WithMessage("O Título é obrigatório.");
            RuleFor(t => t.Description).NotEmpty().WithMessage("A Descrição é obrigatória.");
            RuleFor(t => t.Description).MaximumLength(500).WithMessage("A Descrição não pode exceder 500 caracteres.");
        }
    }

    public class TodoUpdateValidator : AbstractValidator<TodoUpdateDto>
    {
        public TodoUpdateValidator()
        {
            RuleFor(t => t.Title).NotEmpty().WithMessage("O Título é obrigatório.");
            RuleFor(t => t.Description).NotEmpty().WithMessage("A Descrição é obrigatória.");
            RuleFor(t => t.Description).MaximumLength(500).WithMessage("A Descrição não pode exceder 500 caracteres.");
        }
    }
}

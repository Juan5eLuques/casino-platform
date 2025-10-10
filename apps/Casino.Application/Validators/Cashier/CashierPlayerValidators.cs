using Casino.Application.DTOs.Cashier;
using FluentValidation;

namespace Casino.Application.Validators.Cashier;

public class AssignPlayerToCashierRequestValidator : AbstractValidator<AssignPlayerToCashierRequest>
{
    public AssignPlayerToCashierRequestValidator()
    {
        // No hay validaciones espec�ficas para el request vac�o
        // Las validaciones de existencia se hacen en el servicio
    }
}
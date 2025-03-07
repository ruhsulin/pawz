using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Pawz.Domain.Abstractions;
using Pawz.Web.Models;
using Pawz.Web.Models.Identity;
using Pawz.Web.Models.Pet;
using Pawz.Web.Validators;

namespace Pawz.Web.Modules;

public class ValidationModule : IModule
{
    public void Load(IServiceCollection services)
    {
        services.AddValidatorsFromAssemblyContaining<RegisterViewModel>();
        services.AddScoped<IValidator<RegisterViewModel>, RegisterViewModelValidator>();
        services.AddScoped<IValidator<LoginViewModel>,LoginModelValidator>();
        services.AddScoped<IValidator<AdoptionRequestCreateModel>, AdoptionRequestModelValidator>();
        services.AddScoped<IValidator<LoginViewModel>, LoginModelValidator>();
        services.AddScoped<IValidator<PetCreateViewModel>, PetCreateViewModelValidator>();
    }
}

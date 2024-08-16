using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Pawz.Application.Interfaces;
using Pawz.Application.Models;
using Pawz.Domain.Common;
using Pawz.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Pawz.Application.Services;

public class IdentityService : IIdentityService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IValidator<RegisterRequest> _validator;

    public IdentityService(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, IValidator<RegisterRequest> validator)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _validator = validator;
    }

    public async Task<SignInResult> LoginAsync(LoginRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null)
        {
            return SignInResult.Failed;
        }

        return await _signInManager.PasswordSignInAsync(user, request.Password, false, lockoutOnFailure: false);
    }

    public async Task<Result<IdentityResult>> RegisterAsync(RegisterRequest request)
    {
        var validationResult = await _validator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            return Result<IdentityResult>.Failure(
                    validationResult.Errors.Select(e => new Error(e.PropertyName, e.ErrorMessage)).ToArray()
                    );
        }

        try
        {
            var existingUser = await _userManager.FindByEmailAsync(request.Email);
            if (existingUser != null)
            {
                return Result<IdentityResult>.Failure();
            }

            var user = new ApplicationUser
            {
                UserName = request.Email,
                Email = request.Email,
                FirstName = request.FirstName,
                LastName = request.LastName,
                CreatedAt = DateTime.UtcNow,
            };

            var result = await _userManager.CreateAsync(user, request.Password);

            if (!result.Succeeded)
            {
                return Result<IdentityResult>.Failure(result.Errors.Select(e => new Error(e.Code, e.Description)).ToArray());
            }

            List<Claim> claims = new List<Claim>
                {
                     new Claim(ClaimTypes.NameIdentifier, user.Id),
                     new Claim(ClaimTypes.Email, user.Email),
                     new Claim(ClaimTypes.Name, user.UserName),
                     new Claim(ClaimTypes.Role, "User")
                };

            var claimResult = await _userManager.AddClaimsAsync(user, claims);

            if (!claimResult.Succeeded)
            {
                return Result<IdentityResult>.Failure("Failed to set up user claims.");
            }

            return Result<IdentityResult>.Success(result);
        }
        catch (Exception ex)
        {
            return Result<IdentityResult>.Failure("An unexpected error occurred during registration. Please try again later.");
        }
    }
}

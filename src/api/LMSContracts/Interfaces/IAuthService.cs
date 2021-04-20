﻿using System.Threading.Tasks;
using LMSEntities.DataTransferObjects;
using LMSEntities.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;

namespace LMSContracts.Interfaces
{
    public interface IAuthService
    {
        Task<User> FindUserByEmail(string email);

        Task<User> FindUserById(int userId);

        Task<SignInResult> SignInUser(User user, string password);

        Task<UserForDetailedDto> AddRoleToUser(UserForDetailedDto userToReturn, User user);

        Task<string> GenerateJwtToken(User user, string token);

        Task ForgotPassword(User user, string scheme, HostString host);

        Task<User> GetUser(string email);

        Task<IdentityResult> ResetPassword(User user, string password, string code);

        Task<bool> IsResetEligible(User user);
    }
}

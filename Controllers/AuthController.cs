﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AnimeListings.Data;
using AnimeListings.Models;
using AnimeListings.Models.HTTP.Requests;
using AnimeListings.Models.Requests;
using AnimeListings.Models.Responses;
using AnimeListings.Models.Responses.impl;
using AnimeListings.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace AnimeListings.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {

        private readonly DatabaseContext _context;
        private readonly UserManager<SeriesUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly SignInManager<SeriesUser> _signInManager;
        private readonly JWTGenerator _JWTGenerator;

        public AuthController(
            UserManager<SeriesUser> userManager,
            RoleManager<IdentityRole> roleManager,
            SignInManager<SeriesUser> signInManager,
            DatabaseContext context,
            JWTGenerator jwtToken
        )
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _signInManager = signInManager;
            _context = context;
        }

        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<IActionResult> Register(RegistrationRequest request) 
        { 
            if (ModelState.IsValid)
            {
                if (request.DisplayName.Length < 4)
                {
                    return Ok(new RegisterResponse { Success = false, DisplayNameError = "Display name has to be 4 characters or longer" });
                } else if (request.DisplayName.Length > 16)
                {
                    return Ok(new RegisterResponse { Success = false, DisplayNameError = "Display name has to be 4 to 16 characters long" });
                }

                if (request.Password.Length > 32)
                {
                    return Ok(new RegisterResponse { Success = false, PasswordError = "Password has to be 8 to 32 characters long" });
                }

                var newUser = new SeriesUser()
                {
                    UserName = request.DisplayName,
                    Email = request.Email
                };

                var addUserTask = await _userManager.CreateAsync(newUser, request.Password);

                if (addUserTask.Succeeded)
                {
                    await AttemptRoleAdditionAsync(newUser, "User");
                    return Ok(new RegisterResponse { Success = true });
                }

                StringBuilder errors = new StringBuilder();
                foreach (var error in addUserTask.Errors)
                {
                    errors.Append(error.Description);
                    errors.Append(" : ");
                }
                return Ok(new RegisterResponse { Success = false, Error = errors.ToString(0, errors.Length - 3) });
            }
            return NotFound();
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login(LoginRequest request)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByEmailAsync(request.Email);

                if (user != null)
                {
                    var loginResult = await _signInManager.CheckPasswordSignInAsync(user, request.Password, false);
                    if (loginResult.Succeeded)
                    {
                        string token = _JWTGenerator.GenerateEncodedToken(user.Id);
                        string refreshToken = null;
                        if (request.RememberMe)
                        {
                            refreshToken = await GenerateRefreshToken(user.Email);
                        }

                        return Ok(new LoginResponse
                        {
                            Email = user.Email,
                            UserName = user.UserName,
                            RefreshToken = refreshToken,
                            Token = token
                        });
                    }
                }
                return Ok(new LoginResponse { Success = false, Error = "Invalid email or password." });
            }
            return NotFound();
        }

        [HttpPost("CheckUsername")]
        [AllowAnonymous]
        public async Task<IActionResult> CheckUsername(string name)
        {
            if (ModelState.IsValid)
            {
                var isNameTaken = await _userManager.FindByNameAsync(name);
                return Ok(new BasicResponse { Success = isNameTaken == null });
            }
            return NotFound();
        }

        [HttpPost("CheckEmail")]
        [AllowAnonymous]
        public async Task<IActionResult> CheckEmail(string email)
        {
            if (ModelState.IsValid)
            {
                var isEmailTaken = await _userManager.FindByEmailAsync(email);
                return Ok(new BasicResponse { Success = isEmailTaken == null });
            }
            return NotFound();
        }

        private async Task AttemptRoleAdditionAsync(SeriesUser user, string role)
        {
            try
            {
                var roleAdditionResult = await _userManager.AddToRoleAsync(user, role);
            } catch(Exception)
            {
                await _roleManager.CreateAsync(new IdentityRole("User"));
                await _userManager.AddToRoleAsync(user, role);
            }
        }

        private async Task<string> GenerateRefreshToken(string email)
        {
            RefreshToken refreshToken = new RefreshToken
            {
                Email = email,
                Token = Guid.NewGuid().ToString()
            };
            var refreshSearch = _context.RefreshTokens.FirstOrDefault(m => m.Email == email);
            if (refreshSearch != null)
            {
                refreshSearch.Token = refreshToken.Token;
                _context.RefreshTokens.Update(refreshSearch);
            }
            else
            {
                _context.RefreshTokens.Add(refreshToken);
            }
            await _context.SaveChangesAsync();
            return refreshToken.Token.ToString();
        }

    }
}
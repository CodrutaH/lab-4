﻿using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Lab2.DTOs;
using Lab2.Models;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Lab2.Servies
{
    public class UserService : IUserService
    {
        private ExpensesDbContext context;
        private readonly AppSettings appSettings;

        public UserService(ExpensesDbContext context, IOptions<AppSettings> appSettings)
        {
            this.context = context;
            this.appSettings = appSettings.Value;
        }

        public GetUserDto Authenticate(string username, string password)
        {
            var user = context.Users
                .SingleOrDefault(x => x.Username == username &&
                                        x.Password == ComputeSha256Hash(password));

            // return null if user not found
            if (user == null)
                return null;

            // authentication successful so generate jwt token
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(appSettings.Secret);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new Claim[]
                {
                    new Claim(ClaimTypes.Name, user.Username.ToString())
                }),
                Expires = DateTime.UtcNow.AddDays(7),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);
            var result = new GetUserDto
            {
                Id = user.Id,
                Email = user.Email,
                Username = user.Username,
                Token = tokenHandler.WriteToken(token)
            };

            return result;
        }

        private string ComputeSha256Hash(string rawData)
        {
            // Create a SHA256   
            // TODO: also use salt
            using (SHA256 sha256Hash = SHA256.Create())
            {
                // ComputeHash - returns byte array  
                byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(rawData));

                // Convert byte array to a string   
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }
                return builder.ToString();
            }
        }

        public GetUserDto Register(RegisterUserPostDto registerInfo)
        {
            User existing = context.Users.FirstOrDefault(u => u.Username == registerInfo.Username);
            if (existing != null)
            {
                return null;
            }

            context.Users.Add(new User
            {
                FullName = registerInfo.FullName,
                Email = registerInfo.Email,
                Username = registerInfo.Username,
                Password = ComputeSha256Hash(registerInfo.Password)

            });
            context.SaveChanges();
            return Authenticate(registerInfo.Username, registerInfo.Password);
        }


        public IEnumerable<GetUserDto> GetAll()
        {
            // return users without passwords
            return context.Users.Select(user => new GetUserDto
            {
                Id = user.Id,
                Email = user.Email,
                Username = user.Username,
                Token = null
            });
        }

    }
}

using System;
using server.Application.Interfaces;

namespace Infrastructure.Auth;

public class PasswordService : IPasswordService
{
    public string HashPassword(string password)
    {
        var hash = BCrypt.Net.BCrypt.HashPassword(password,workFactor: 10);
        return hash;
    }
    
    public bool VerifyPassword(string password, string hashedPassword)
    {
        return BCrypt.Net.BCrypt.Verify(password, hashedPassword);
    }
}


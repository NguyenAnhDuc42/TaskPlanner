using System;
namespace Application;

public static class PasswordService 
{
    public static string HashPassword(string password)
    {
        var hash = BCrypt.Net.BCrypt.HashPassword(password,workFactor: 10);
        return hash;
    }
    
    public static bool VerifyPassword(string password, string hashedPassword)
    {
        return BCrypt.Net.BCrypt.Verify(password, hashedPassword);
    }
}






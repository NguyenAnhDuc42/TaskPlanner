using System;

namespace src.Infrastructure.Abstractions.IServices;

public interface IPasswordService
{
    string HashPassword(string password);
    bool VerifyPassword(string password, string hashedPassword);

}

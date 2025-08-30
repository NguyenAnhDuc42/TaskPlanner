using System;
using Application.Contract.UserContract;
using MediatR;

namespace Application.Features.Auth.Register;

public record class RegisterCommand(string username,string email,string password) : IRequest<string>;

using System;
using Application.Features.Auth;
using MediatR;

namespace Application.Features.Auth.Register;

public record class RegisterCommand(string username,string email,string password) : IRequest<RegisterResponse>;

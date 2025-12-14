using System;

namespace Application.Features.Auth.Register;

public record RegisterResponse(Guid Id, string Username, string Email);

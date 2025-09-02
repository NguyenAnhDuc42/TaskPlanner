using Domain.Enums;

namespace Application.Common.Filters;

public record WorkspaceFilter(string? Name = null, string? Icon = null,Visibility? Visibility = null, bool Owned = false, bool isArchived = false);
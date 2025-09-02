using System;

namespace Infrastructure.Helper.Configurations;

public class CursorEncryptionOptions
{
    public const string SectionName = "CursorEncryption";
    public string Key { get; set; } = string.Empty;
}

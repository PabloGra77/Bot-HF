namespace HorusAfiliadosExtractor.App.Models;

public enum UserType
{
    Afiliado,
    Funcionario
}

public sealed class LoginCredentials
{
    public UserType Type { get; set; } = UserType.Afiliado;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;

    public bool IsValid =>
        !string.IsNullOrWhiteSpace(Email) &&
        !string.IsNullOrWhiteSpace(Password);
}

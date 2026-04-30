namespace SnapCd.Server.Core.Misc.Utils;

public static class CredentialHelper
{
    public static string GenerateRandomPassword(int length)
    {
        const string validChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890!@#$%^&*()-_=+";
        var random = new Random();
        var password = new char[length];

        for (var i = 0; i < length; i++) password[i] = validChars[random.Next(validChars.Length)];

        return new string(password);
    }
}
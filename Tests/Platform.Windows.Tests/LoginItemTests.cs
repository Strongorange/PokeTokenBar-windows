using PokeTokenBar.Platform.Windows;

namespace PokeTokenBar.Platform.Windows.Tests;

public class LoginItemTests
{
    private const string Exe = @"C:\Users\user\AppData\Local\Programs\PokeTokenBar\PokeTokenBar.exe";

    [Fact]
    public void EncodeCommandTrimsAndQuotesThePath()
    {
        Assert.Equal($"\"{Exe}\"", LoginItem.EncodeCommand($" {Exe} "));
    }

    [Fact]
    public void MatchesCommandIgnoresSurroundingWhitespaceAndCase()
    {
        Assert.True(LoginItem.MatchesCommand($"\"{Exe}\"", Exe));
        Assert.True(LoginItem.MatchesCommand($"  \"{Exe.ToUpperInvariant()}\"  ", Exe));
        Assert.False(LoginItem.MatchesCommand(null, Exe));
        Assert.False(LoginItem.MatchesCommand("\"C:\\Other\\App.exe\"", Exe));
        Assert.False(LoginItem.MatchesCommand(Exe, Exe));
    }

    [Fact]
    public void IsEnabledReadsThroughTheInjectedReader()
    {
        Assert.True(LoginItem.IsEnabled(Exe, _ => $"\"{Exe}\""));
        Assert.False(LoginItem.IsEnabled(Exe, _ => null));
        Assert.False(LoginItem.IsEnabled(Exe, _ => "\"C:\\Other\\App.exe\""));
    }

    [Fact]
    public void SetEnabledWritesEncodedCommandAndNullClears()
    {
        var written = new List<(string Name, string? Value)>();

        LoginItem.SetEnabled(true, Exe, (name, value) => written.Add((name, value)));
        LoginItem.SetEnabled(false, Exe, (name, value) => written.Add((name, value)));

        Assert.Equal(
        [
            (LoginItem.ValueName, (string?)$"\"{Exe}\""),
            (LoginItem.ValueName, (string?)null)
        ], written);
    }
}

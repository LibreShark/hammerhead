using LibreShark.Hammerhead.Cli;

namespace LibreShark.Hammerhead;

internal static class Program
{
    public static async Task<int> Main(string[] args)
    {
        var cli = new HammerheadCli(args);
        return await cli.InvokeAsync();
    }
}

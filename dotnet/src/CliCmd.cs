namespace LibreShark.Hammerhead;

internal delegate int CliCmdRunner(string[] args);

internal class CliCmd
{
    private readonly string _id;
    private readonly int _minArgCount;
    private readonly int _maxArgCount;
    private readonly CliCmdRunner _runner;

    public CliCmd(string id, int minArgCount = 0, int maxArgCount = int.MaxValue, CliCmdRunner? runner = null)
    {
        _id = id;
        _minArgCount = minArgCount;
        _maxArgCount = maxArgCount;
        _runner = runner ?? (args => 0);
    }

    public bool Is(string id)
    {
        return string.Equals(_id, id, StringComparison.InvariantCultureIgnoreCase);
    }

    public int Run(IEnumerable<string> args)
    {
        var arr = args.ToArray();
        if (arr.Length < _minArgCount)
        {
            Console.Error.WriteLine($"${_id} expected at least ${_minArgCount} argument(s), but only got ${arr.Length}.");
            return -1;
        }
        if (arr.Length > _maxArgCount - 1)
        {
            Console.Error.WriteLine($"${_id} expected at no more than ${_maxArgCount} argument(s), but got ${arr.Length}.");
            return -1;
        }
        return _runner(arr);
    }
}

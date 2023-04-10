namespace LibreShark.Hammerhead.N64;

public class KeyCode
{
    public readonly string Name;
    public readonly byte[] Bytes;
    public readonly bool IsActive;

    public KeyCode(string name, byte[] bytes, bool isActive)
    {
        Name = name;
        Bytes = bytes;
        IsActive = isActive;
    }

    public override string ToString()
    {
        string bytes = string.Join(" ", Bytes.Select((b) => $"{b:X2}"));
        string isActive = IsActive ? " [ACTIVE]" : "";
        return $"{bytes} - {Name}{isActive}";
    }
}

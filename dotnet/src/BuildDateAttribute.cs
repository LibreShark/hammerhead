using System.Globalization;

namespace LibreShark.Hammerhead;

[AttributeUsage(AttributeTargets.Assembly)]
internal class BuildDateAttribute : Attribute
{
    public BuildDateAttribute(string value)
    {
        DateTimeOffset = DateTimeOffset.ParseExact(value, "yyyyMMddHHmmssK", CultureInfo.InvariantCulture, DateTimeStyles.None);
    }

    public DateTimeOffset DateTimeOffset { get; }
}

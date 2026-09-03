using LibreShark.Hammerhead.IO;

namespace LibreShark.Hammerhead.Test.IO;

[TestFixture]
public class AbstractBinaryScribeTest
{
  private sealed class TestScribe(byte[] buffer) : AbstractBinaryScribe(buffer)
  {
    public override ushort ReadU16() => throw new NotSupportedException();

    public override AbstractBinaryScribe WriteU16(ushort value) => throw new NotSupportedException();

    public override uint ReadU32() => throw new NotSupportedException();

    public override AbstractBinaryScribe WriteU32(uint value) => throw new NotSupportedException();
  }

  [Test]
  public void WriteCString_EmptyString_WritesNullTerminator()
  {
    var scribe = new TestScribe(new byte[1]);

    scribe.WriteCString("");

    Assert.Multiple(() =>
    {
      Assert.That(scribe.Position, Is.EqualTo(1));
      Assert.That(scribe.GetBufferCopy(), Is.EqualTo(new byte[] { 0 }));
    });
  }
}

using SistemaInventarioVentas.Domain.ValueObjects;

namespace SistemaInventarioVentas.Domain.Tests;

public sealed class ProductIdentifierTests
{
    [Fact]
    public void Constructor_TrimsIdentifier()
    {
        var identifier = new ProductIdentifier("  DRINK-1  ");

        Assert.Equal("DRINK-1", identifier.Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_RejectsBlankIdentifier(string value)
    {
        Assert.Throws<ArgumentException>(() => new ProductIdentifier(value));
    }

    [Fact]
    public void Identifiers_WithSameValue_AreEqual()
    {
        var first = new ProductIdentifier("DRINK-1");
        var second = new ProductIdentifier(" DRINK-1 ");

        Assert.Equal(first, second);
    }
}

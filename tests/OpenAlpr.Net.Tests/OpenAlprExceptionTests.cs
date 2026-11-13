using Xunit;
using OpenAlprWrapper;

namespace OpenAlprWrapper.Tests;

public sealed class OpenAlprExceptionTests
{
    [Fact]
    public void Constructor_NullInner_KeepsMessageClean()
    {
        var ex = new OpenAlprException("native engine failed to load");

        Assert.Equal("native engine failed to load", ex.Message);
        Assert.Null(ex.InnerException);
    }

    [Fact]
    public void Constructor_WithInner_PreservesInnerAndKeepsMessageClean()
    {
        var inner = new InvalidOperationException("boom");
        var ex = new OpenAlprException("native engine failed to load", inner);

        // Message stays clean; the original error is preserved on InnerException for stack trace / debugging.
        Assert.Equal("native engine failed to load", ex.Message);
        Assert.Same(inner, ex.InnerException);
    }
}

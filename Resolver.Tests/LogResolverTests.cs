using Domain;

namespace Resolver.Tests;

public class LogResolverTests
{
    [Fact]
    public void ResolveWithSplit_ValidLine_ReturnsLogEntry()
    {
        const string line =
            "2|[INFO]|RequestReceived|Processing|" +
            "[INFO] RequestReceived -> Processing";

        LogEntry result =
            LogResolver.ResolveWithSplit(line);

        Assert.Equal(2, result.Index);
        Assert.Equal("[INFO]", result.Level);
        Assert.Equal(State.RequestReceived, result.From);
        Assert.Equal(State.Processing, result.To);
        Assert.Equal(
            "[INFO] RequestReceived -> Processing",
            result.Message);
    }

    [Fact]
    public void ResolveWithSplit_MessageContainsSeparator_PreservesMessage()
    {
        const string line =
            "15|[ERROR]|Processing|Error|" +
            "[ERROR] Processing -> Error | XXXXX";

        LogEntry result =
            LogResolver.ResolveWithSplit(line);

        Assert.Equal(
            "[ERROR] Processing -> Error | XXXXX",
            result.Message);
    }

    [Theory]
    [InlineData("abc|[INFO]|Idle|Processing|Message")]
    [InlineData("0|[INFO]|Idle|Processing|Message")]
    [InlineData("-1|[INFO]|Idle|Processing|Message")]
    public void ResolveWithSplit_InvalidIndex_ThrowsFormatException(
        string line)
    {
        Assert.Throws<FormatException>(() => LogResolver.ResolveWithSplit(line));
    }

    [Fact]
    public void ResolveWithSplit_InvalidLevel_ThrowsFormatException()
    {
        const string line =
            "1|[DEBUG]|Idle|Processing|Message";

        Assert.Throws<FormatException>(() => LogResolver.ResolveWithSplit(line));
    }

    [Theory]
    [InlineData("1|[INFO]|Unknown|Processing|Message")]
    [InlineData("1|[INFO]|Idle|Unknown|Message")]
    public void ResolveWithSplit_InvalidState_ThrowsFormatException(
        string line)
    {
        Assert.Throws<FormatException>(() => LogResolver.ResolveWithSplit(line));
    }

    [Fact]
    public void ResolveWithSplit_MissingField_ThrowsFormatException()
    {
        const string line =
            "1|[INFO]|Idle|Processing";

        Assert.Throws<FormatException>(() => LogResolver.ResolveWithSplit(line));
    }

    [Fact]
    public void ResolveWithSplit_EmptyMessage_ThrowsFormatException()
    {
        const string line =
            "1|[INFO]|Idle|Processing|";

        Assert.Throws<FormatException>(() => LogResolver.ResolveWithSplit(line));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ResolveWithSplit_EmptyInput_ThrowsArgumentException(
        string line)
    {
        Assert.Throws<ArgumentException>(() => LogResolver.ResolveWithSplit(line));
    }
    
    [Theory]
    [InlineData(
        "2|[INFO]|RequestReceived|Processing|" +
        "[INFO] RequestReceived -> Processing")]
    [InlineData(
        "15|[ERROR]|Processing|Error|" +
        "[ERROR] Processing -> Error | XXXXX")]
    [InlineData(
        "20|[WARNING]|Retry|Processing|" +
        "Retry attempt")]
    public void ResolveWithSpan_ValidLine_MatchesSplit(
        string line)
    {
        LogEntry split =
            LogResolver.ResolveWithSplit(line);

        LogEntry span =
            LogResolver.ResolveWithSpan(line);

        Assert.Equal(split.Index, span.Index);
        Assert.Equal(split.Level, span.Level);
        Assert.Equal(split.From, span.From);
        Assert.Equal(split.To, span.To);
        Assert.Equal(split.Message, span.Message);
    }
    [Theory]
    [InlineData("abc|[INFO]|Idle|Processing|Message")]
    [InlineData("0|[INFO]|Idle|Processing|Message")]
    [InlineData("-1|[INFO]|Idle|Processing|Message")]
    [InlineData("1|[DEBUG]|Idle|Processing|Message")]
    [InlineData("1|[INFO]|Unknown|Processing|Message")]
    [InlineData("1|[INFO]|Idle|Unknown|Message")]
    [InlineData("1|[INFO]|Idle|Processing")]
    [InlineData("1|[INFO]|Idle|Processing|")]
    public void ResolveWithSpan_InvalidLine_ThrowsFormatException(
        string line)
    {
        Assert.Throws<FormatException>(
            () => LogResolver.ResolveWithSpan(line));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ResolveWithSpan_EmptyInput_ThrowsArgumentException(
        string line)
    {
        Assert.Throws<ArgumentException>(
            () => LogResolver.ResolveWithSpan(line));
    }
    [Theory]
    [InlineData("abc|[INFO]|Idle|Processing|Message")]
    [InlineData("1|[DEBUG]|Idle|Processing|Message")]
    [InlineData("1|[INFO]|Unknown|Processing|Message")]
    [InlineData("1|[INFO]|Idle|Processing")]
    [InlineData("1|[INFO]|Idle|Processing|")]
    public void SplitAndSpan_InvalidLine_ThrowSameExceptionType(
        string line)
    {
        Exception? splitException =
            Record.Exception(
                () => LogResolver.ResolveWithSplit(line));

        Exception? spanException =
            Record.Exception(
                () => LogResolver.ResolveWithSpan(line));

        Assert.NotNull(splitException);
        Assert.NotNull(spanException);

        Assert.Equal(
            splitException.GetType(),
            spanException.GetType());
    }
}
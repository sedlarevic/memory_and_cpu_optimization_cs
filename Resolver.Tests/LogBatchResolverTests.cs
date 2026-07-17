namespace Resolver.Tests;

public class LogBatchResolverTests
{
    [Fact]
    public void AllStrategies_ReturnSameChecksum()
    {
        string[] lines =
        [
            "1|[INFO]|Idle|RequestReceived|" +
            "[INFO] Idle -> RequestReceived",

            "2|[INFO]|RequestReceived|Processing|" +
            "[INFO] RequestReceived -> Processing",

            "3|[ERROR]|Processing|Error|" +
            "[ERROR] Processing -> Error | XXXXX",

            "4|[WARNING]|Error|Retry|" +
            "[WARNING] Error -> Retry"
        ];

        long sequential =
            LogBatchResolver.ResolveSequential(lines);

        long parallelFor =
            LogBatchResolver.ResolveParallelFor(lines);

        long plinq =
            LogBatchResolver.ResolvePlinq(lines);

        Assert.Equal(sequential, parallelFor);
        Assert.Equal(sequential, plinq);
    }

    [Fact]
    public void AllStrategies_EmptyDataset_ReturnZero()
    {
        string[] lines = [];

        Assert.Equal(
            0,
            LogBatchResolver.ResolveSequential(lines));

        Assert.Equal(
            0,
            LogBatchResolver.ResolveParallelFor(lines));

        Assert.Equal(
            0,
            LogBatchResolver.ResolvePlinq(lines));
    }

    [Fact]
    public void AllStrategies_LargerDataset_ReturnSameChecksum()
    {
        string[] lines = new string[10_000];

        for (int index = 0;
             index < lines.Length;
             index++)
        {
            int logIndex = index + 1;

            lines[index] =
                $"{logIndex}|[INFO]|Idle|" +
                "RequestReceived|" +
                "[INFO] Idle -> RequestReceived";
        }

        long sequential =
            LogBatchResolver.ResolveSequential(lines);

        long parallelFor =
            LogBatchResolver.ResolveParallelFor(lines);

        long plinq =
            LogBatchResolver.ResolvePlinq(lines);

        Assert.Equal(sequential, parallelFor);
        Assert.Equal(sequential, plinq);
    }
}
using System.Security.Cryptography;
using Generator;

int targetCount = 5_000;
int? suppliedSeed = null;
GenerationMode mode = GenerationMode.Steady;

if (args.Length >= 1 &&
    int.TryParse(args[0], out int parsedCount))
{
    targetCount = parsedCount;
}

if (args.Length >= 2 &&
    int.TryParse(args[1], out int parsedSeed))
{
    suppliedSeed = parsedSeed;
}

if (args.Length >= 3 &&
    Enum.TryParse(
        args[2],
        ignoreCase: true,
        out GenerationMode parsedMode))
{
    mode = parsedMode;
}

if (targetCount <= 0)
{
    Console.Error.WriteLine(
        "Broj logova mora biti veći od nule.");

    return;
}

int seedValue = suppliedSeed ??
                RandomNumberGenerator.GetInt32(int.MaxValue);

var seed = new Seed(seedValue);
ILogFactory logFactory = new LogFactory(mode);

var engine = new GeneratorEngine(
    seed,
    targetCount,
    logFactory);

string outputPath =
    $"logs-{mode.ToString().ToLowerInvariant()}-" +
    $"{targetCount}-seed-{seedValue}.txt";

Console.WriteLine($"Mode: {mode}");
Console.WriteLine($"Target logs: {targetCount:N0}");
Console.WriteLine($"Seed: {seedValue}");
Console.WriteLine($"Output: {outputPath}");

using var writer = new StreamWriter(outputPath);

int writtenCount = 0;

foreach (var log in engine.Run())
{
    writer.WriteLine(
        $"{log.Index}|{log.Level}|" +
        $"{log.From}|{log.To}|{log.Message}");

    writtenCount++;

    if (writtenCount % 100_000 == 0)
    {
        Console.WriteLine(
            $"Generated {writtenCount:N0} logs...");
    }
}

Console.WriteLine(
    $"Generation finished. Written: {writtenCount:N0}");
using System.Security.Cryptography;
using Generator;

int targetCount = 5_000;
int? suppliedSeed = null;
GenerationProfile profile = GenerationProfile.Standard;

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
        out GenerationProfile parsedMode))
{
    profile = parsedMode;
}

if (targetCount <= 0)
{
    Console.Error.WriteLine(
        "Target count must be greater than zero.");

    return;
}

int seedValue =
    suppliedSeed ??
    RandomNumberGenerator.GetInt32(int.MaxValue);

Seed seed = new(seedValue);
ILogFactory factory = new LogFactory(profile);

GeneratorEngine engine =
    new GeneratorEngine(
        seed,
        targetCount,
        factory);

long checksum = 0;

int producedCount = engine.Run(log =>
{
    checksum += log.Message.Length;
});

Console.WriteLine($"Scenario : {profile}");
Console.WriteLine($"Seed     : {seedValue}");
Console.WriteLine($"Produced : {producedCount:N0}");
Console.WriteLine($"Checksum : {checksum:N0}");
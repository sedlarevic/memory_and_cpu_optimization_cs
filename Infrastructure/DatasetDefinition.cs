namespace Infrastructure;

public sealed record DatasetDefinition(
    string Name,
    string GenerationProfile,
    int Seed,
    int TargetCount,
    string? Description);
    
public sealed record DatasetRegistration(
    long DatasetId,
    bool Created);
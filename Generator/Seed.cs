namespace Generator;

public class Seed
{
    public int Value { get; }
    public Seed (int value)
    {
        Value = value;
    }
    
    public Seed Derive(string purpose)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(purpose);
        int hashValue = Value;
        const int prime = 31;
        unchecked
        {
            foreach (char character in purpose)
            {
                hashValue = hashValue * prime + character;
            }
        }
        return new Seed(hashValue);
    }
}

using System.Runtime.CompilerServices;
using System.Security.Cryptography;

namespace Generator;

public class Seed
{
    public int Value { get; }

    public Seed (int value)
    {
        this.Value = value;
    }

    private int GetHash(string input,int seedValue)
    {
        int hashValue = seedValue;
        const int p = 31;
        unchecked
        {
            foreach (char c in input)
            {
                hashValue = hashValue * p + c;
            }     
        }
        
        return hashValue;
    }
    public Seed Derive(string purpose)
    {
        return new Seed(GetHash(purpose, Value));
    }
}
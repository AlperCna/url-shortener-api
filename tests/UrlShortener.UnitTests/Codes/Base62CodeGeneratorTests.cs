using FluentAssertions;
using UrlShortener.Core.Codes;
using Xunit;

namespace UrlShortener.UnitTests.Codes;

public class Base62CodeGeneratorTests
{
    [Fact]
    public void Alphabet_ContainsExactlySixtyTwoUniqueCharacters()
    {
        Base62CodeGenerator.Alphabet.Should().HaveLength(62);
        Base62CodeGenerator.Alphabet.Distinct().Should().HaveCount(62);
    }

    [Fact]
    public void Alphabet_IsDigitsThenLowercaseThenUppercase()
    {
        Base62CodeGenerator.Alphabet.Should().Be(
            "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ");
    }

    [Fact]
    public void Generate_WithDefaultLength_ReturnsSevenCharacters()
    {
        var code = Base62CodeGenerator.Generate();

        code.Should().HaveLength(Base62CodeGenerator.DefaultLength);
        code.Should().HaveLength(7);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(4)]
    [InlineData(16)]
    [InlineData(32)]
    public void Generate_WithExplicitLength_ReturnsThatManyCharacters(int length)
    {
        var code = Base62CodeGenerator.Generate(length);

        code.Should().HaveLength(length);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Generate_WithNonPositiveLength_Throws(int length)
    {
        var act = () => Base62CodeGenerator.Generate(length);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Generate_OnlyUsesCharactersFromTheBase62Alphabet()
    {
        for (var i = 0; i < 200; i++)
        {
            var code = Base62CodeGenerator.Generate();

            code.Should().MatchRegex("^[0-9a-zA-Z]+$");
        }
    }

    [Fact]
    public void Generate_ProducesDifferentCodesAcrossManyCalls()
    {
        var codes = Enumerable.Range(0, 10_000)
            .Select(_ => Base62CodeGenerator.Generate())
            .ToHashSet();

        // With 62^7 possible codes, collisions across 10k draws should be
        // effectively impossible unless generation is not actually random.
        codes.Should().HaveCount(10_000);
    }

    [Fact]
    public void Generate_DistributesCharactersRoughlyUniformlyAcrossTheAlphabet()
    {
        var counts = Base62CodeGenerator.Alphabet.ToDictionary(c => c, _ => 0);

        const int sampleSize = 62_000;
        for (var i = 0; i < sampleSize; i++)
        {
            foreach (var c in Base62CodeGenerator.Generate(1))
            {
                counts[c]++;
            }
        }

        // Expected ~1000 hits per character; allow generous slack to keep
        // the test stable while still catching a badly skewed generator.
        var expected = sampleSize / (double)Base62CodeGenerator.Alphabet.Length;
        counts.Values.Should().OnlyContain(count => count > expected * 0.5 && count < expected * 1.5);
    }
}

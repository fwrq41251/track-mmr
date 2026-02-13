using Xunit;
using TrackMmr;

namespace TrackMmr.Tests;

public class HeroNamesTests
{
    [Theory]
    [InlineData(1, "Anti-Mage")]
    [InlineData(14, "Pudge")]
    [InlineData(999, "Hero#999")]
    public void Get_ShouldReturnCorrectHeroName(int id, string expectedName)
    {
        var result = HeroNames.Get(id);
        Assert.Equal(expectedName, result);
    }

    [Theory]
    [InlineData(1, "antimage")]
    [InlineData(11, "nevermore")]
    [InlineData(14, "pudge")]
    public void GetIconUrl_ShouldContainInternalName(int id, string internalName)
    {
        var url = HeroNames.GetIconUrl(id);
        Assert.Contains(internalName, url);
        Assert.StartsWith("https://", url);
        Assert.EndsWith(".png", url);
    }
}

using MacroYoutubeReport.Scheduler.Proxy;

namespace MacroYoutubeReport.Tests;

public class ProxyRotatorTests
{
    [Fact]
    public void ProxyConfig_TryParse_ParsesFormattedStrings()
    {
        var p1 = ProxyConfig.TryParse("103.77.243.55:8080");
        Assert.NotNull(p1);
        Assert.Equal("103.77.243.55", p1.Host);
        Assert.Equal(8080, p1.Port);
        Assert.Null(p1.Username);

        var p2 = ProxyConfig.TryParse("1.2.3.4:9000:admin:secret123");
        Assert.NotNull(p2);
        Assert.Equal("1.2.3.4", p2.Host);
        Assert.Equal(9000, p2.Port);
        Assert.Equal("admin", p2.Username);
        Assert.Equal("secret123", p2.Password);
        Assert.Equal("http://admin:secret123@1.2.3.4:9000", p2.ToProxyUrl());
    }

    [Fact]
    public void ProxyRotator_RotatesRoundRobin()
    {
        var rotator = new ProxyRotator();
        rotator.LoadProxiesFromLines(new[]
        {
            "1.1.1.1:8080",
            "2.2.2.2:8080",
            "3.3.3.3:8080"
        });

        var first = rotator.GetNextProxy();
        var second = rotator.GetNextProxy();
        var third = rotator.GetNextProxy();
        var fourth = rotator.GetNextProxy();

        Assert.Equal("1.1.1.1", first?.Host);
        Assert.Equal("2.2.2.2", second?.Host);
        Assert.Equal("3.3.3.3", third?.Host);
        Assert.Equal("1.1.1.1", fourth?.Host);
    }
}

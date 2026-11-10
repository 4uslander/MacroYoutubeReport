using System.Xml;
using MacroYoutubeReport.Driver.Android.Adb;
using MacroYoutubeReport.Driver.Android.Locators;
using MacroYoutubeReport.Driver.Chrome.Locators;

namespace MacroYoutubeReport.Tests;

public class DriverLocatorsTests
{
    [Fact]
    public void AndroidLocators_ContainValidPackageAndIdentifiers()
    {
        Assert.Equal("com.google.android.youtube", AndroidLocators.PackageYouTube);
        Assert.Contains("player_settings_button", AndroidLocators.PlayerSettingsGearId);
        Assert.Contains("menu_search", AndroidLocators.SearchButtonId);
        Assert.Contains("menu_item_view", AndroidLocators.SearchMenuItemViewId);
    }

    [Fact]
    public void ChromeLocators_ContainStandardYoutubeSelectors()
    {
        Assert.Equal("https://www.youtube.com/", ChromeLocators.YoutubeHomeUrl);
        Assert.Contains("input#search", ChromeLocators.SearchInput);
        Assert.Contains("ytd-menu-renderer", ChromeLocators.MoreActionsButton);
    }

    [Fact]
    public void AdbUiXmlDumper_ParseElement_ExtractsBoundsAndAttributes()
    {
        string sampleXml = @"<hierarchy rotation='0'>
            <node class='android.widget.TextView' text='Test Video Title' resource-id='com.google.android.youtube:id/title' bounds='[100,200][500,400]' clickable='true' />
        </hierarchy>";

        var doc = new XmlDocument();
        doc.LoadXml(sampleXml);

        var node = doc.SelectSingleNode("//node");
        Assert.NotNull(node);

        var elem = AdbUiXmlDumper.ParseElement(node);
        Assert.NotNull(elem);
        Assert.Equal("Test Video Title", elem.Text);
        Assert.Equal(100, elem.Bounds.X);
        Assert.Equal(200, elem.Bounds.Y);
        Assert.Equal(400, elem.Bounds.Width);
        Assert.Equal(200, elem.Bounds.Height);
        Assert.Equal(300, elem.CenterPoint.X);
        Assert.Equal(300, elem.CenterPoint.Y);
        Assert.True(elem.IsClickable);

        var jitter = elem.GetJitterCenter(5);
        Assert.InRange(jitter.X, 295, 305);
        Assert.InRange(jitter.Y, 295, 305);
    }
}

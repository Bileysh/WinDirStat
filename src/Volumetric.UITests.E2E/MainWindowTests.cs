namespace Volumetric.UITests.E2E;

[CollectionDefinition(Name)]
public class VolumetricAppCollection : ICollectionFixture<VolumetricAppFixture>
{
    public const string Name = "Volumetric App";
}

[Collection(VolumetricAppCollection.Name)]
public class MainWindowTests(VolumetricAppFixture app)
{
    [E2EFact]
    public async Task MainWindow_Launches_WithExpectedTitle()
    {
        Assert.Equal("Volumetric", await app.Session.GetTitleAsync());
    }

    [E2EFact]
    public async Task MainWindow_ShowsSearchBox()
    {
        Assert.True(await app.Session.ElementWithAccessibilityIdIsDisplayedAsync("SearchBox"));
    }

    [E2EFact]
    public async Task MainWindow_ShowsResultsTree()
    {
        Assert.True(await app.Session.ElementWithAccessibilityIdIsDisplayedAsync("ListControl"));
    }
}

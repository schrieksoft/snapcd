using SnapCd.Runner.Services.ModuleSourceRefresher;

namespace SnapCd.Runner.Tests;

public class GitModuleSourceResolverTests
{
    private readonly GitModuleSourceResolver _resolver;
    private readonly string _sourceUrl = "https://github.com/karlschriek/module-sample.git";

    public GitModuleSourceResolverTests()
    {
        _resolver = new GitModuleSourceResolver();
    }

    [Theory]
    [InlineData("2.0.1", "2.0.1", "b7873776d3c39c730fae940ba1906591816200d8")]
    [InlineData("2.0.*", "2.0.1", "b7873776d3c39c730fae940ba1906591816200d8")]
    [InlineData("2.*", "2.1.0", "380a002f204c0dffbadc7600c2b2af46790f6037")]
    [InlineData("v1.0.1", "v1.0.1", "04d74fa50a63b0451777f46ddc2a91160d7e0666")]
    [InlineData("v1.0.*", "v1.0.2", "602ad088646fd21fc2e5fa776ccf587397ca59de")]
    [InlineData("v1.*", "v1.1.1", "647ef8a023219520a06eae2702fc736e8a8e9edb")]
    public void Resolves_Correct_Version(string semverRange, string expectedTag, string expectedSha)
    {
        // Act
        var resolvedTag = _resolver.GetRemoteSemverRangeResolvedTag(_sourceUrl, semverRange);
        var sha = _resolver.GetRemoteDefaultDefinitiveRevision(_sourceUrl, resolvedTag);

        // Assert
        Assert.Equal(expectedTag, resolvedTag);
        Assert.Equal(expectedSha, sha);
    }

    [Theory]
    [InlineData("2.0.1.5")]
    [InlineData("v2")]
    [InlineData("*.1.2")]
    [InlineData("invalid")]
    public void Throws_For_Invalid_Formats(string invalidRange)
    {
        Assert.Throws<ArgumentException>(() =>
            _resolver.GetRemoteSemverRangeDefinitiveRevision(_sourceUrl, invalidRange));
    }

    [Theory]
    [InlineData("3.0.*")]
    [InlineData("v3.*")]
    public void Throws_When_No_Matching_Tags(string notFoundTag)
    {
        var ex = Assert.Throws<Exception>(() =>
            _resolver.GetRemoteSemverRangeDefinitiveRevision(_sourceUrl, notFoundTag));

        Assert.Contains("No tags in remote repository", ex.Message);
    }
}
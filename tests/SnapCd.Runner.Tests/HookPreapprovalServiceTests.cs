using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using SnapCd.Runner.Exceptions;
using SnapCd.Runner.Services;
using SnapCd.Runner.Settings;

namespace SnapCd.Runner.Tests;

public class HookPreapprovalServiceTests
{
    private readonly string _testHooksDirectory;
    private readonly string _emptyTestDirectory;
    private readonly Mock<ILogger<HookPreapprovalService>> _mockLogger;

    public HookPreapprovalServiceTests()
    {
        // Use the TestData/Hooks directory with pre-created hook files
        _testHooksDirectory = Path.Combine(AppContext.BaseDirectory, "TestData", "Hooks");

        // Create an empty temp directory for tests that need it
        _emptyTestDirectory = Path.Combine(Path.GetTempPath(), "HooksPreapprovalTests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_emptyTestDirectory);

        _mockLogger = new Mock<ILogger<HookPreapprovalService>>();
    }

    [Fact]
    public void Service_Disabled_AllHooks_Pass()
    {
        // Arrange
        var settings = Options.Create(new HooksPreapprovalSettings
        {
            Enabled = false,
            PreapprovedHooksDirectory = _emptyTestDirectory
        });

        var service = new HookPreapprovalService(_mockLogger.Object, settings);

        // Act & Assert - Should not throw
        service.ValidateHook("echo 'some random hook'", "TestHook");
        service.ValidateHook("rm -rf /", "DangerousHook");
    }

    [Fact]
    public void Service_Enabled_NullHook_Passes()
    {
        // Arrange
        var settings = Options.Create(new HooksPreapprovalSettings
        {
            Enabled = true,
            PreapprovedHooksDirectory = _testHooksDirectory
        });

        var service = new HookPreapprovalService(_mockLogger.Object, settings);

        // Act & Assert - Should not throw
        service.ValidateHook(null, "TestHook");
    }

    [Fact]
    public void Service_Enabled_EmptyHook_Passes()
    {
        // Arrange
        var settings = Options.Create(new HooksPreapprovalSettings
        {
            Enabled = true,
            PreapprovedHooksDirectory = _testHooksDirectory
        });

        var service = new HookPreapprovalService(_mockLogger.Object, settings);

        // Act & Assert - Should not throw
        service.ValidateHook("", "TestHook");
        service.ValidateHook("   ", "WhitespaceHook");
    }

    [Fact]
    public void Service_Enabled_MatchingHook_Passes()
    {
        // Arrange - Use pre-created simple-echo.sh
        var hookContent = "#!/bin/bash\necho \"Hello from pre-approved hook\"";

        var settings = Options.Create(new HooksPreapprovalSettings
        {
            Enabled = true,
            PreapprovedHooksDirectory = _testHooksDirectory
        });

        var service = new HookPreapprovalService(_mockLogger.Object, settings);

        // Act & Assert - Should not throw
        service.ValidateHook(hookContent, "TestHook");
    }

    [Fact]
    public void Service_Enabled_NonMatchingHook_Throws()
    {
        // Arrange - TestData/Hooks has pre-approved hooks
        var settings = Options.Create(new HooksPreapprovalSettings
        {
            Enabled = true,
            PreapprovedHooksDirectory = _testHooksDirectory
        });

        var service = new HookPreapprovalService(_mockLogger.Object, settings);

        // Act & Assert - Try a hook that doesn't match any pre-approved hook
        var unapprovedHook = "echo 'Not approved'";
        var exception = Assert.Throws<HookNotPreapprovedException>(() =>
            service.ValidateHook(unapprovedHook, "TestHook"));

        Assert.Equal("TestHook", exception.HookName);
        Assert.Contains("echo 'Not approved'", exception.HookContentPreview);
    }

    [Fact]
    public void Service_Enabled_MultipleHooks_AllMatch_Passes()
    {
        // Arrange - Use pre-created hook files
        var settings = Options.Create(new HooksPreapprovalSettings
        {
            Enabled = true,
            PreapprovedHooksDirectory = _testHooksDirectory
        });

        var service = new HookPreapprovalService(_mockLogger.Object, settings);

        // Act & Assert - Validate multiple pre-approved hooks
        service.ValidateHooks(
            ("#!/bin/bash\necho \"Hello from pre-approved hook\"", "SimpleEcho"),
            ("#!/bin/bash\necho \"Line 1\"\necho \"Line 2\"\necho \"Line 3\"", "MultilineHook"),
            ("#!/bin/bash\n# Check required environment variables\nif [ -z \"$WORKSPACE\" ]; then\n  echo \"ERROR: WORKSPACE not set\"\n  exit 1\nfi\necho \"Environment check passed\"", "EnvironmentCheck")
        );
    }

    [Fact]
    public void Service_Enabled_MultipleHooks_OneFails_Throws()
    {
        // Arrange - Use pre-created hook files
        var settings = Options.Create(new HooksPreapprovalSettings
        {
            Enabled = true,
            PreapprovedHooksDirectory = _testHooksDirectory
        });

        var service = new HookPreapprovalService(_mockLogger.Object, settings);

        // Act & Assert - One approved, one unapproved, one approved
        var exception = Assert.Throws<HookNotPreapprovedException>(() =>
            service.ValidateHooks(
                ("#!/bin/bash\necho \"Hello from pre-approved hook\"", "SimpleEcho"),
                ("echo 'Unapproved Hook'", "UnapprovedHook"),
                ("#!/bin/bash\necho \"Line 1\"\necho \"Line 2\"\necho \"Line 3\"", "MultilineHook")
            ));

        Assert.Equal("UnapprovedHook", exception.HookName);
    }

    [Fact]
    public void Service_Enabled_EmptyDirectory_AllNonEmptyHooksFail()
    {
        // Arrange - Use empty directory
        var settings = Options.Create(new HooksPreapprovalSettings
        {
            Enabled = true,
            PreapprovedHooksDirectory = _emptyTestDirectory
        });

        var service = new HookPreapprovalService(_mockLogger.Object, settings);

        // Act & Assert
        Assert.Throws<HookNotPreapprovedException>(() =>
            service.ValidateHook("echo 'Any hook'", "TestHook"));
    }

    [Fact]
    public void Service_Enabled_HashComparison_WorksCorrectly()
    {
        // Arrange - Use pre-created multiline-hook.sh
        var hookContent = "#!/bin/bash\necho \"Line 1\"\necho \"Line 2\"\necho \"Line 3\"";

        var settings = Options.Create(new HooksPreapprovalSettings
        {
            Enabled = true,
            PreapprovedHooksDirectory = _testHooksDirectory
        });

        var service = new HookPreapprovalService(_mockLogger.Object, settings);

        // Act & Assert - Exact match should pass
        service.ValidateHook(hookContent, "TestHook");

        // Different content should fail
        Assert.Throws<HookNotPreapprovedException>(() =>
            service.ValidateHook("#!/bin/bash\necho \"Different Line 1\"\necho \"Line 2\"\necho \"Line 3\"", "TestHook"));
    }

    [Fact]
    public void Service_Enabled_LineEndings_Normalized()
    {
        // Arrange - Use pre-created multiline-hook.sh with Unix line endings
        var settings = Options.Create(new HooksPreapprovalSettings
        {
            Enabled = true,
            PreapprovedHooksDirectory = _testHooksDirectory
        });

        var service = new HookPreapprovalService(_mockLogger.Object, settings);

        // Act & Assert - Windows line endings should also match (normalized to Unix)
        var hookContentWindows = "#!/bin/bash\r\necho \"Line 1\"\r\necho \"Line 2\"\r\necho \"Line 3\"";
        service.ValidateHook(hookContentWindows, "TestHook");
    }

    [Fact]
    public void Service_Enabled_TrailingWhitespace_Trimmed()
    {
        // Arrange - Use pre-created simple-echo.sh
        var hookContent = "#!/bin/bash\necho \"Hello from pre-approved hook\"";

        var settings = Options.Create(new HooksPreapprovalSettings
        {
            Enabled = true,
            PreapprovedHooksDirectory = _testHooksDirectory
        });

        var service = new HookPreapprovalService(_mockLogger.Object, settings);

        // Act & Assert - Content with trailing whitespace should match
        service.ValidateHook(hookContent + "   \n\n", "TestHook");
    }

    [Fact]
    public void Service_Enabled_LongHookContent_PreviewTruncated()
    {
        // Arrange - Use pre-created hooks (none will match the long hook)
        var settings = Options.Create(new HooksPreapprovalSettings
        {
            Enabled = true,
            PreapprovedHooksDirectory = _testHooksDirectory
        });

        var service = new HookPreapprovalService(_mockLogger.Object, settings);

        // Act - Create a very long unapproved hook
        var longHook = new string('x', 200);
        var exception = Assert.Throws<HookNotPreapprovedException>(() =>
            service.ValidateHook(longHook, "LongHook"));

        // Assert - Preview should be truncated to 100 characters + "..."
        Assert.Equal(103, exception.HookContentPreview.Length); // 100 chars + "..."
        Assert.EndsWith("...", exception.HookContentPreview);
    }
}

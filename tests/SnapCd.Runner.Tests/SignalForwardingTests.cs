// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using SnapCd.Contracts;
using SnapCd.Contracts.RunnerRequests.HelperClasses;
using SnapCd.Runner.Logging;
using SnapCd.Runner.Services;
using SnapCd.Runner.Settings;

namespace SnapCd.Runner.Tests;

public class SignalForwardingTests : IDisposable
{
    private readonly Mock<ILogger<TerraformEngine>> _mockLogger;
    private readonly RunnerTaskContext _taskContext;
    private readonly string _testWorkingDirectory;
    private readonly ModuleDirectoryService _moduleDirectoryService;
    private readonly TerraformEngine _engine;

    public SignalForwardingTests()
    {
        _testWorkingDirectory = Path.Combine(
            Path.GetTempPath(),
            "SnapCdSignalTests",
            Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testWorkingDirectory);

        _mockLogger = new Mock<ILogger<TerraformEngine>>();
        var mockTaskLogger = new Mock<ILogger>();

        var metadata = new JobMetadata
        {
            ModuleName = "signal-test",
            NamespaceName = "test-namespace",
            StackName = "test-stack",
            ModuleId = Guid.NewGuid(),
            SourceSubdirectory = null
        };

        _taskContext = new RunnerTaskContext(
            Guid.NewGuid(),
            "SignalForwardingTests",
            mockTaskLogger.Object,
            new NullJobLogStream(),
            metadata
        );

        var workingDirectorySettings = Options.Create(new WorkingDirectorySettings
        {
            WorkingDirectory = _testWorkingDirectory,
            TempDirectory = Path.Combine(_testWorkingDirectory, "temp")
        });

        _moduleDirectoryService = new ModuleDirectoryService(
            metadata,
            workingDirectorySettings);

        _engine = new TerraformEngine(
            _taskContext,
            _mockLogger.Object,
            _moduleDirectoryService,
            "terraform",
            new List<string>(),
        new List<EngineFlagEntry>(),
        new List<EngineArrayFlagEntry>()
        );

        Directory.CreateDirectory(_engine.GetInitDir());
        Directory.CreateDirectory(_engine.GetSnapCdDir());

        // Set up terraform files
        SetupTerraformFiles();
    }

    private void SetupTerraformFiles()
    {
        var initDir = _engine.GetInitDir();

        // Create main.tf
        File.WriteAllText(Path.Combine(initDir, "main.tf"), @"
resource ""time_sleep"" ""wait"" {
  create_duration  = ""${var.wait}s""
  destroy_duration = ""${var.wait}s""
}

resource ""time_sleep"" ""wait2"" {
  depends_on = [time_sleep.wait]
  create_duration  = ""${var.wait}s""
  destroy_duration = ""${var.wait}s""
}

resource ""random_uuid"" ""vpc_id"" {
  depends_on = [time_sleep.wait]
}

resource ""random_uuid"" ""public_subnet_id"" {
  depends_on = [time_sleep.wait]
}

resource ""random_uuid"" ""private_subnet_id"" {
  depends_on = [time_sleep.wait]
}
");

        // Create outputs.tf
        File.WriteAllText(Path.Combine(initDir, "outputs.tf"), @"
output ""vpc_id"" {
  description = ""ID of the VPC""
  value       = random_uuid.vpc_id.result
}

output ""public_subnet_id"" {
  description = ""ID of the public subnet""
  value       = random_uuid.public_subnet_id.result
}

output ""private_subnet_id"" {
  description = ""ID of the private subnet""
  value       = random_uuid.private_subnet_id.result
}

output ""public_subnet_cidr"" {
  description = ""CIDR block of the public subnet""
  value       = var.public_subnet_cidr
}

output ""private_subnet_cidr"" {
  description = ""CIDR block of the private subnet""
  value       = var.private_subnet_cidr
}
");

        // Create root.tf
        File.WriteAllText(Path.Combine(initDir, "root.tf"), @"
terraform {
  required_providers {
    random = {
      source  = ""hashicorp/random""
      version = ""3.6.0""
    }
    time = {
      source  = ""hashicorp/time""
      version = ""0.9.1""
    }
  }
}
");

        // Create variables.tf
        File.WriteAllText(Path.Combine(initDir, "variables.tf"), @"
variable ""vpc_name"" {
  description = ""Name for the VPC""
  default     = ""myvpc""
  type        = string
}

variable ""vpc_cidr_block"" {
  description = ""CIDR block for the VPC""
  type        = string
  default     = ""10.0.0.0/16""
}

variable ""public_subnet_cidr"" {
  description = ""CIDR block for the public subnet""
  type        = string
  default     = ""10.0.1.0/24""
}

variable ""private_subnet_cidr"" {
  description = ""CIDR block for the private subnet""
  type        = string
  default     = ""10.0.2.0/24""
}

variable ""wait"" {
  type        = string
  default     = ""30""
}
");

        // Create environment file
        var envFilePath = Path.Combine(_engine.GetSnapCdDir(), "snapcd.env");
        File.WriteAllText(envFilePath, "export TF_VAR_wait=\"30\"");
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_testWorkingDirectory))
            {
                Directory.Delete(_testWorkingDirectory, recursive: true);
            }
        }
        catch
        {
            // Best effort cleanup
        }
    }

    [Fact]
    public async Task TerraformApply_WithGracefulCancellation_ShouldStopQuickly()
    {
        // Arrange
        var script = await _engine.CreateScriptAsync(
            "terraform init && terraform apply -auto-approve",
            null,
            null);

        using var gracefulCts = new CancellationTokenSource();
        var killCts = new CancellationTokenSource();

        // Act - Run terraform apply and cancel it after 2 seconds
        var startTime = DateTime.UtcNow;
        var applyTask = Task.Run(async () =>
        {
            try
            {
                await _engine.RunProcess(script, killCts.Token, gracefulCts.Token);
            }
            catch (OperationCanceledException)
            {
                // Expected
            }
            catch (Exception ex)
            {
                // Process error expected due to cancellation
                Console.WriteLine($"Apply error: {ex.Message}");
            }
        });

        // Wait for terraform to start applying
        await Task.Delay(2000);

        // Send graceful cancellation (SIGINT)
        gracefulCts.Cancel();

        // Wait for completion
        var completed = await Task.WhenAny(applyTask, Task.Delay(10000)) == applyTask;
        var elapsedTime = (DateTime.UtcNow - startTime).TotalSeconds;

        // Assert
        Assert.True(completed, "Apply should have completed after receiving SIGINT");

        // If signal forwarding works correctly, terraform should receive SIGINT
        // and stop within a few seconds. If it doesn't work, the full 30+ second
        // wait time would elapse.
        Assert.True(elapsedTime < 8,
            $"Terraform should stop quickly after SIGINT (under 8s) but took {elapsedTime:F1}s. " +
            "This suggests SIGINT is not being properly forwarded to the terraform process.");

        Console.WriteLine($"Apply was gracefully cancelled in {elapsedTime:F1} seconds");
    }
}

// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using Microsoft.EntityFrameworkCore;
using Serilog.Events;
using SnapCd.Contracts;
using SnapCd.Contracts.Dto;
using SnapCd.Contracts.Dto.Misc;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Enums;
using SnapCd.Server.Core.Services;
using SnapCd.Server.Core.Tests.Infrastructure;

namespace SnapCd.Server.Core.Tests.Tests.Misc;

[Collection("NewRoleBasedSharedFixture")]
public class LogServiceTests : IAsyncLifetime
{
    private readonly Fixture _fixture;
    private SnapCdDbContext _dbContext = null!;
    private LogService _logService = null!;
    private ModuleJob _testJob1 = null!;
    private ModuleJob _testJob2 = null!;

    public LogServiceTests(Fixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        _dbContext = _fixture.CreateDbContext();
        _logService = new LogService(new FixtureDbContextFactory(_fixture));

        // Create test ModuleJobs
        _testJob1 = new ModuleJob
        {
            Id = Guid.NewGuid(),
            OrganizationId = _fixture.Organizations["0"].Id,
            ModuleId = _fixture.Modules["0000"].Id,
            TimestampStart = DateTimeOffset.UtcNow,
            Status = ExecutionStatus.Running,
            JobType = "Test"
        };

        _testJob2 = new ModuleJob
        {
            Id = Guid.NewGuid(),
            OrganizationId = _fixture.Organizations["0"].Id,
            ModuleId = _fixture.Modules["0001"].Id,
            TimestampStart = DateTimeOffset.UtcNow,
            Status = ExecutionStatus.Running,
            JobType = "Test"
        };

        _dbContext.ModuleJobs.AddRange(_testJob1, _testJob2);
        await _dbContext.SaveChangesAsync();
    }

    public Task DisposeAsync()
    {
        _dbContext?.Dispose();
        return Task.CompletedTask;
    }

    #region Basic Functionality Tests

    [Fact]
    public async Task AddLogEntries_WithNullList_ShouldReturnImmediately()
    {
        // Act
        await _logService.AddLogEntries(null!);

        // Assert - no exception thrown
        Assert.True(true);
    }

    [Fact]
    public async Task AddLogEntries_WithEmptyList_ShouldReturnImmediately()
    {
        // Arrange
        var emptyList = new List<LogEntryDto>();

        // Act
        await _logService.AddLogEntries(emptyList);

        // Assert - no exception thrown
        Assert.True(true);
    }

    [Fact]
    public async Task AddLogEntries_ToNewModuleJob_ShouldStoreLogsAsJson()
    {
        // Arrange
        var logEntries = new List<LogEntryDto>
        {
            CreateLogEntry(_testJob1.Id, "Test message 1", "Init"),
            CreateLogEntry(_testJob1.Id, "Test message 2", "Init")
        };

        // Act
        await _logService.AddLogEntries(logEntries);

        // Assert
        using var verifyContext = _fixture.CreateDbContext();
        var job = await verifyContext.ModuleJobs.FindAsync(_testJob1.Id, _testJob1.OrganizationId);
        Assert.NotNull(job);
        Assert.NotNull(job.Logs);
        Assert.Contains("Test message 1", job.Logs);
        Assert.Contains("Test message 2", job.Logs);
    }

    [Fact]
    public async Task AddLogEntries_ToExistingModuleJob_ShouldAppendLogs()
    {
        // Arrange
        var firstBatch = new List<LogEntryDto>
        {
            CreateLogEntry(_testJob1.Id, "First batch", "Init")
        };
        await _logService.AddLogEntries(firstBatch);

        var secondBatch = new List<LogEntryDto>
        {
            CreateLogEntry(_testJob1.Id, "Second batch", "Plan")
        };

        // Act
        await _logService.AddLogEntries(secondBatch);

        // Assert
        var logs = await _logService.GetLogEntries(_testJob1.Id);
        Assert.Equal(2, logs.Count);
        Assert.Contains(logs, l => l.Message == "First batch");
        Assert.Contains(logs, l => l.Message == "Second batch");
    }

    [Fact]
    public async Task GetLogEntries_ForNonExistentJob_ShouldReturnEmptyList()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var result = await _logService.GetLogEntries(nonExistentId);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetLogEntries_ForJobWithNoLogs_ShouldReturnEmptyList()
    {
        // Act
        var result = await _logService.GetLogEntries(_testJob2.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetLogEntries_ForJobWithLogs_ShouldDeserializeCorrectly()
    {
        // Arrange
        var logEntries = new List<LogEntryDto>
        {
            CreateLogEntry(_testJob1.Id, "Message 1", "Init", LogEventLevel.Information),
            CreateLogEntry(_testJob1.Id, "Message 2", "Plan", LogEventLevel.Warning)
        };
        await _logService.AddLogEntries(logEntries);

        // Act
        var result = await _logService.GetLogEntries(_testJob1.Id);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal("Message 1", result[0].Message);
        Assert.Equal("Init", result[0].TaskName);
        Assert.Equal(LogEventLevel.Information, result[0].Level);
        Assert.Equal("Message 2", result[1].Message);
        Assert.Equal("Plan", result[1].TaskName);
        Assert.Equal(LogEventLevel.Warning, result[1].Level);
    }

    [Fact]
    public async Task GetLogString_WithNoLogs_ShouldReturnEmptyString()
    {
        // Act
        var result = await _logService.GetLogString(_testJob2.Id);

        // Assert
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public async Task GetLogString_WithLogs_ShouldJoinWithNewlines()
    {
        // Arrange
        var logEntries = new List<LogEntryDto>
        {
            CreateLogEntry(_testJob1.Id, "Line 1", "Init"),
            CreateLogEntry(_testJob1.Id, "Line 2", "Init"),
            CreateLogEntry(_testJob1.Id, "Line 3", "Init")
        };
        await _logService.AddLogEntries(logEntries);

        // Act
        var result = await _logService.GetLogString(_testJob1.Id);

        // Assert
        var expected = $"Line 1{Environment.NewLine}Line 2{Environment.NewLine}Line 3";
        Assert.Equal(expected, result);
    }

    [Fact]
    public async Task GetLogStrings_WithMultipleContexts_ShouldGroupCorrectly()
    {
        // Arrange
        var logEntries = new List<LogEntryDto>
        {
            CreateLogEntry(_testJob1.Id, "Init message 1", "Init"),
            CreateLogEntry(_testJob1.Id, "Init message 2", "Init"),
            CreateLogEntry(_testJob1.Id, "Plan message 1", "Plan"),
            CreateLogEntry(_testJob1.Id, "Apply message 1", "Apply")
        };
        await _logService.AddLogEntries(logEntries);

        // Act
        var result = await _logService.GetLogStrings(_testJob1.Id);

        // Assert
        Assert.Equal(3, result.Count);
        Assert.True(result.ContainsKey("Init"));
        Assert.True(result.ContainsKey("Plan"));
        Assert.True(result.ContainsKey("Apply"));
        Assert.Contains("Init message 1", result["Init"]);
        Assert.Contains("Init message 2", result["Init"]);
        Assert.Contains("Plan message 1", result["Plan"]);
        Assert.Contains("Apply message 1", result["Apply"]);
    }

    #endregion

    #region JSON Handling Tests

    [Fact]
    public async Task AddLogEntries_WithCorruptedExistingJson_ShouldStartFresh()
    {
        // Arrange
        var job = await _dbContext.ModuleJobs.FindAsync(_testJob1.Id, _testJob1.OrganizationId);
        job!.Logs = "{ invalid json [[[";
        await _dbContext.SaveChangesAsync();

        var logEntries = new List<LogEntryDto>
        {
            CreateLogEntry(_testJob1.Id, "New message", "Init")
        };

        // Act
        await _logService.AddLogEntries(logEntries);

        // Assert
        var logs = await _logService.GetLogEntries(_testJob1.Id);
        Assert.Single(logs);
        Assert.Equal("New message", logs[0].Message);
    }

    [Fact]
    public async Task GetLogEntries_WithCorruptedJson_ShouldReturnEmptyList()
    {
        // Arrange
        var job = await _dbContext.ModuleJobs.FindAsync(_testJob1.Id, _testJob1.OrganizationId);
        job!.Logs = "{ corrupted }}}";
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _logService.GetLogEntries(_testJob1.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    #endregion

    #region Ordering Tests

    [Fact]
    public async Task AddLogEntries_ShouldSortByTimestampThenBatchTimestamp()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;
        var logEntries = new List<LogEntryDto>
        {
            CreateLogEntry(_testJob1.Id, "Third", "Init", LogEventLevel.Information, now.AddSeconds(3), now.AddSeconds(3)),
            CreateLogEntry(_testJob1.Id, "First", "Init", LogEventLevel.Information, now.AddSeconds(1), now.AddSeconds(1)),
            CreateLogEntry(_testJob1.Id, "Second", "Init", LogEventLevel.Information, now.AddSeconds(2), now.AddSeconds(2))
        };

        // Act
        await _logService.AddLogEntries(logEntries);

        // Assert
        var result = await _logService.GetLogEntries(_testJob1.Id);
        Assert.Equal("First", result[0].Message);
        Assert.Equal("Second", result[1].Message);
        Assert.Equal("Third", result[2].Message);
    }

    [Fact]
    public async Task AddLogEntries_WithOutOfOrderEntries_ShouldMaintainCorrectOrder()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;

        // Add first batch
        var firstBatch = new List<LogEntryDto>
        {
            CreateLogEntry(_testJob1.Id, "Timestamp 3", "Init", LogEventLevel.Information, now.AddSeconds(3), now.AddSeconds(3))
        };
        await _logService.AddLogEntries(firstBatch);

        // Add second batch with earlier timestamp
        var secondBatch = new List<LogEntryDto>
        {
            CreateLogEntry(_testJob1.Id, "Timestamp 1", "Init", LogEventLevel.Information, now.AddSeconds(1), now.AddSeconds(1)),
            CreateLogEntry(_testJob1.Id, "Timestamp 2", "Init", LogEventLevel.Information, now.AddSeconds(2), now.AddSeconds(2))
        };
        await _logService.AddLogEntries(secondBatch);

        // Act
        var result = await _logService.GetLogEntries(_testJob1.Id);

        // Assert - should be sorted by timestamp
        Assert.Equal(3, result.Count);
        Assert.Equal("Timestamp 1", result[0].Message);
        Assert.Equal("Timestamp 2", result[1].Message);
        Assert.Equal("Timestamp 3", result[2].Message);
    }

    [Fact]
    public async Task AddLogEntries_MultipleContexts_ShouldPreserveOrderWithinContext()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;
        var logEntries = new List<LogEntryDto>
        {
            CreateLogEntry(_testJob1.Id, "Init 2", "Init", LogEventLevel.Information, now.AddSeconds(2), now.AddSeconds(2)),
            CreateLogEntry(_testJob1.Id, "Plan 1", "Plan", LogEventLevel.Information, now.AddSeconds(3), now.AddSeconds(3)),
            CreateLogEntry(_testJob1.Id, "Init 1", "Init", LogEventLevel.Information, now.AddSeconds(1), now.AddSeconds(1))
        };

        // Act
        await _logService.AddLogEntries(logEntries);

        // Assert
        var result = await _logService.GetLogStrings(_testJob1.Id);
        var initLogs = result["Init"].Split(Environment.NewLine);
        Assert.Equal("Init 1", initLogs[0]);
        Assert.Equal("Init 2", initLogs[1]);
    }

    #endregion

    #region Concurrency Tests

    [Fact]
    public async Task AddLogEntries_ConcurrentWrites_ShouldNotLoseData()
    {
        // Arrange
        const int numberOfConcurrentWrites = 10;
        var tasks = new List<Task>();

        for (var i = 0; i < numberOfConcurrentWrites; i++)
        {
            var messageIndex = i;
            var task = Task.Run(async () =>
            {
                var service = new LogService(new FixtureDbContextFactory(_fixture));

                var logEntry = new List<LogEntryDto>
                {
                    CreateLogEntry(_testJob1.Id, $"Concurrent message {messageIndex}", "Apply")
                };
                await service.AddLogEntries(logEntry);
            });
            tasks.Add(task);
        }

        // Act
        await Task.WhenAll(tasks);

        // Assert
        var logs = await _logService.GetLogEntries(_testJob1.Id);
        Assert.Equal(numberOfConcurrentWrites, logs.Count);

        // Verify all messages are present
        for (var i = 0; i < numberOfConcurrentWrites; i++) Assert.Contains(logs, l => l.Message == $"Concurrent message {i}");
    }

    [Fact]
    public async Task AddLogEntries_ConcurrentWrites_ShouldMaintainProperOrdering()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;
        const int numberOfWrites = 5;
        var tasks = new List<Task>();

        for (var i = 0; i < numberOfWrites; i++)
        {
            var index = i;
            var task = Task.Run(async () =>
            {
                var service = new LogService(new FixtureDbContextFactory(_fixture));

                var logEntry = new List<LogEntryDto>
                {
                    CreateLogEntry(
                        _testJob1.Id,
                        $"Message {index}",
                        "Apply",
                        LogEventLevel.Information,
                        now.AddSeconds(index),
                        now.AddSeconds(index)
                    )
                };
                await service.AddLogEntries(logEntry);
            });
            tasks.Add(task);
        }

        // Act
        await Task.WhenAll(tasks);

        // Assert
        var logs = await _logService.GetLogEntries(_testJob1.Id);
        Assert.Equal(numberOfWrites, logs.Count);

        // Verify logs are sorted by timestamp
        for (var i = 0; i < numberOfWrites; i++) Assert.Equal($"Message {i}", logs[i].Message);
    }

    [Fact]
    public async Task AddLogEntries_ConcurrentWrites_ToSeparateJobs_ShouldNotBlock()
    {
        // Arrange
        var tasks = new List<Task>();

        // Write to job 1
        for (var i = 0; i < 5; i++)
        {
            var index = i;
            var task = Task.Run(async () =>
            {
                var service = new LogService(new FixtureDbContextFactory(_fixture));

                var logEntry = new List<LogEntryDto>
                {
                    CreateLogEntry(_testJob1.Id, $"Job1 Message {index}", "Apply")
                };
                await service.AddLogEntries(logEntry);
            });
            tasks.Add(task);
        }

        // Write to job 2 concurrently
        for (var i = 0; i < 5; i++)
        {
            var index = i;
            var task = Task.Run(async () =>
            {
                var service = new LogService(new FixtureDbContextFactory(_fixture));

                var logEntry = new List<LogEntryDto>
                {
                    CreateLogEntry(_testJob2.Id, $"Job2 Message {index}", "Apply")
                };
                await service.AddLogEntries(logEntry);
            });
            tasks.Add(task);
        }

        // Act
        await Task.WhenAll(tasks);

        // Assert
        var job1Logs = await _logService.GetLogEntries(_testJob1.Id);
        var job2Logs = await _logService.GetLogEntries(_testJob2.Id);

        Assert.Equal(5, job1Logs.Count);
        Assert.Equal(5, job2Logs.Count);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task AddLogEntries_WithMultipleCorrelationIds_ShouldHandleEachSeparately()
    {
        // Arrange
        var logEntries = new List<LogEntryDto>
        {
            CreateLogEntry(_testJob1.Id, "Job1 message", "Init"),
            CreateLogEntry(_testJob2.Id, "Job2 message", "Init"),
            CreateLogEntry(_testJob1.Id, "Job1 message 2", "Plan")
        };

        // Act
        await _logService.AddLogEntries(logEntries);

        // Assert
        var job1Logs = await _logService.GetLogEntries(_testJob1.Id);
        var job2Logs = await _logService.GetLogEntries(_testJob2.Id);

        Assert.Equal(2, job1Logs.Count);
        Assert.Single(job2Logs);
        Assert.Contains(job1Logs, l => l.Message == "Job1 message");
        Assert.Contains(job1Logs, l => l.Message == "Job1 message 2");
        Assert.Equal("Job2 message", job2Logs[0].Message);
    }

    [Fact]
    public async Task AddLogEntries_ToNonExistentJob_ShouldContinueGracefully()
    {
        // Arrange
        var nonExistentJobId = Guid.NewGuid();
        var logEntries = new List<LogEntryDto>
        {
            CreateLogEntry(nonExistentJobId, "Message for non-existent job", "Init"),
            CreateLogEntry(_testJob1.Id, "Message for valid job", "Init")
        };

        // Act
        await _logService.AddLogEntries(logEntries);

        // Assert - should not throw, and valid job should have logs
        var logs = await _logService.GetLogEntries(_testJob1.Id);
        Assert.Single(logs);
        Assert.Equal("Message for valid job", logs[0].Message);
    }

    [Fact]
    public async Task GetLogStrings_WithNullLogContext_ShouldUseDefaultKey()
    {
        // Arrange
        var logEntries = new List<LogEntryDto>
        {
            CreateLogEntry(_testJob1.Id, "Message with null context", null!)
        };
        await _logService.AddLogEntries(logEntries);

        // Act
        var result = await _logService.GetLogStrings(_testJob1.Id);

        // Assert
        Assert.True(result.ContainsKey("Default"));
        Assert.Contains("Message with null context", result["Default"]);
    }

    #endregion

    #region Helper Methods

    private static LogEntryDto CreateLogEntry(
        Guid correlationId,
        string message,
        string logContext,
        LogEventLevel level = LogEventLevel.Information,
        DateTimeOffset? timestamp = null,
        DateTimeOffset? batchTimestamp = null)
    {
        var now = DateTimeOffset.UtcNow;
        return new LogEntryDto
        {
            JobId = correlationId,
            Timestamp = timestamp ?? now,
            BatchTimeStamp = batchTimestamp ?? now,
            Level = level,
            Message = message,
            TaskName = logContext,
            ModuleId = Guid.NewGuid(),
            StackName = "TestStack",
            NamespaceName = "TestNamespace"
        };
    }

    #endregion

    private sealed class FixtureDbContextFactory(Fixture fixture) : IDbContextFactory<SnapCdDbContext>
    {
        public SnapCdDbContext CreateDbContext()
        {
            return fixture.CreateDbContext();
        }
    }
}

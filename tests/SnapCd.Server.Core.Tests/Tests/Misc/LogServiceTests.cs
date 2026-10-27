// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Serilog.Events;
using SnapCd.Contracts;
using SnapCd.Contracts.Dto;
using SnapCd.Contracts.Dto.Misc;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Enums;
using SnapCd.Server.Core.Services;
using SnapCd.Server.Core.Services.Logs;
using SnapCd.Server.Core.Tests.Infrastructure;

namespace SnapCd.Server.Core.Tests.Tests.Misc;

[Collection("NewRoleBasedSharedFixture")]
public class LogServiceTests : IAsyncLifetime
{
    private readonly Fixture _fixture;
    private SnapCdDbContext _dbContext = null!;
    private JobLogReader<ModuleJob, ModuleJobLogChunk, ModuleJobLogEntryDto> _logService = null!;
    private JobLogWriter<ModuleJob, ModuleJobLogChunk, ModuleJobLogEntryDto> _logWriter = null!;
    private ModuleJob _testJob1 = null!;
    private ModuleJob _testJob2 = null!;

    public LogServiceTests(Fixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        _dbContext = _fixture.CreateDbContext();
        _logService = new JobLogReader<ModuleJob, ModuleJobLogChunk, ModuleJobLogEntryDto>(
            new FixtureDbContextFactory(_fixture), NullLogger<JobLogReader<ModuleJob, ModuleJobLogChunk, ModuleJobLogEntryDto>>.Instance);
        _logWriter = new JobLogWriter<ModuleJob, ModuleJobLogChunk, ModuleJobLogEntryDto>(
            new FixtureDbContextFactory(_fixture), NullLogger<JobLogWriter<ModuleJob, ModuleJobLogChunk, ModuleJobLogEntryDto>>.Instance);

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
        await _logWriter.AddLogEntries(null!, _fixture.Organizations["0"].Id);

        // Assert - no exception thrown
        Assert.True(true);
    }

    [Fact]
    public async Task AddLogEntries_WithEmptyList_ShouldReturnImmediately()
    {
        // Arrange
        var emptyList = new List<ModuleJobLogEntryDto>();

        // Act
        await _logWriter.AddLogEntries(emptyList, _fixture.Organizations["0"].Id);

        // Assert - no exception thrown
        Assert.True(true);
    }

    [Fact]
    public async Task AddLogEntries_ToNewModuleJob_ShouldStoreAChunk()
    {
        // Arrange
        var logEntries = new List<ModuleJobLogEntryDto>
        {
            CreateLogEntry(_testJob1.Id, "Test message 1", "Init"),
            CreateLogEntry(_testJob1.Id, "Test message 2", "Init")
        };

        // Act
        await _logWriter.AddLogEntries(logEntries, _fixture.Organizations["0"].Id);

        // Assert
        using var verifyContext = _fixture.CreateDbContext();
        var job = await verifyContext.ModuleJobs.FindAsync(_testJob1.Id, _testJob1.OrganizationId);
        Assert.NotNull(job);
        Assert.Null(job.Logs);
        Assert.True(await verifyContext.ModuleJobLogChunks.AnyAsync(c => c.JobId == _testJob1.Id));

        var logs = await _logService.GetLogEntries(_testJob1.Id);
        Assert.Equal(["Test message 1", "Test message 2"], logs.Select(l => l.Message));
    }

    [Fact]
    public async Task AddLogEntries_ToExistingModuleJob_ShouldAppendLogs()
    {
        // Arrange
        var firstBatch = new List<ModuleJobLogEntryDto>
        {
            CreateLogEntry(_testJob1.Id, "First batch", "Init")
        };
        await _logWriter.AddLogEntries(firstBatch, _fixture.Organizations["0"].Id);

        var secondBatch = new List<ModuleJobLogEntryDto>
        {
            CreateLogEntry(_testJob1.Id, "Second batch", "Plan")
        };

        // Act
        await _logWriter.AddLogEntries(secondBatch, _fixture.Organizations["0"].Id);

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
        var logEntries = new List<ModuleJobLogEntryDto>
        {
            CreateLogEntry(_testJob1.Id, "Message 1", "Init", LogEventLevel.Information),
            CreateLogEntry(_testJob1.Id, "Message 2", "Plan", LogEventLevel.Warning)
        };
        await _logWriter.AddLogEntries(logEntries, _fixture.Organizations["0"].Id);

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
    public async Task GetLogStrings_WithMultipleContexts_ShouldGroupCorrectly()
    {
        // Arrange
        var logEntries = new List<ModuleJobLogEntryDto>
        {
            CreateLogEntry(_testJob1.Id, "Init message 1", "Init"),
            CreateLogEntry(_testJob1.Id, "Init message 2", "Init"),
            CreateLogEntry(_testJob1.Id, "Plan message 1", "Plan"),
            CreateLogEntry(_testJob1.Id, "Apply message 1", "Apply")
        };
        await _logWriter.AddLogEntries(logEntries, _fixture.Organizations["0"].Id);

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
    public async Task AddLogEntries_WithCorruptedExistingJson_ReportsItAndKeepsNewLines()
    {
        // Arrange
        var job = await _dbContext.ModuleJobs.FindAsync(_testJob1.Id, _testJob1.OrganizationId);
        job!.Logs = "{ invalid json [[[";
        await _dbContext.SaveChangesAsync();

        var logEntries = new List<ModuleJobLogEntryDto>
        {
            CreateLogEntry(_testJob1.Id, "New message", "Init")
        };

        // Act
        await _logWriter.AddLogEntries(logEntries, _fixture.Organizations["0"].Id);

        // Assert
        var logs = await _logService.GetLogEntries(_testJob1.Id);
        Assert.Equal(2, logs.Count);
        Assert.Equal("Part of this log could not be read.", logs[0].Message);
        Assert.Equal("New message", logs[1].Message);
    }

    [Fact]
    public async Task GetLogEntries_WithCorruptedJson_ReportsTheUnreadablePart()
    {
        // Arrange
        var job = await _dbContext.ModuleJobs.FindAsync(_testJob1.Id, _testJob1.OrganizationId);
        job!.Logs = "{ corrupted }}}";
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _logService.GetLogEntries(_testJob1.Id);

        // Assert
        var entry = Assert.Single(result);
        Assert.Equal(LogEventLevel.Error, entry.Level);
        Assert.Equal(LogSource.Server, entry.Source);
    }

    #endregion

    #region Ordering Tests

    [Fact]
    public async Task AddLogEntries_ShouldSortByTimestampThenBatchTimestamp()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;
        var logEntries = new List<ModuleJobLogEntryDto>
        {
            CreateLogEntry(_testJob1.Id, "Third", "Init", LogEventLevel.Information, now.AddSeconds(3), now.AddSeconds(3)),
            CreateLogEntry(_testJob1.Id, "First", "Init", LogEventLevel.Information, now.AddSeconds(1), now.AddSeconds(1)),
            CreateLogEntry(_testJob1.Id, "Second", "Init", LogEventLevel.Information, now.AddSeconds(2), now.AddSeconds(2))
        };

        // Act
        await _logWriter.AddLogEntries(logEntries, _fixture.Organizations["0"].Id);

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
        var firstBatch = new List<ModuleJobLogEntryDto>
        {
            CreateLogEntry(_testJob1.Id, "Timestamp 3", "Init", LogEventLevel.Information, now.AddSeconds(3), now.AddSeconds(3))
        };
        await _logWriter.AddLogEntries(firstBatch, _fixture.Organizations["0"].Id);

        // Add second batch with earlier timestamp
        var secondBatch = new List<ModuleJobLogEntryDto>
        {
            CreateLogEntry(_testJob1.Id, "Timestamp 1", "Init", LogEventLevel.Information, now.AddSeconds(1), now.AddSeconds(1)),
            CreateLogEntry(_testJob1.Id, "Timestamp 2", "Init", LogEventLevel.Information, now.AddSeconds(2), now.AddSeconds(2))
        };
        await _logWriter.AddLogEntries(secondBatch, _fixture.Organizations["0"].Id);

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
        var logEntries = new List<ModuleJobLogEntryDto>
        {
            CreateLogEntry(_testJob1.Id, "Init 2", "Init", LogEventLevel.Information, now.AddSeconds(2), now.AddSeconds(2)),
            CreateLogEntry(_testJob1.Id, "Plan 1", "Plan", LogEventLevel.Information, now.AddSeconds(3), now.AddSeconds(3)),
            CreateLogEntry(_testJob1.Id, "Init 1", "Init", LogEventLevel.Information, now.AddSeconds(1), now.AddSeconds(1))
        };

        // Act
        await _logWriter.AddLogEntries(logEntries, _fixture.Organizations["0"].Id);

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
                var service = new JobLogWriter<ModuleJob, ModuleJobLogChunk, ModuleJobLogEntryDto>(
                    new FixtureDbContextFactory(_fixture), NullLogger<JobLogWriter<ModuleJob, ModuleJobLogChunk, ModuleJobLogEntryDto>>.Instance);

                var logEntry = new List<ModuleJobLogEntryDto>
                {
                    CreateLogEntry(_testJob1.Id, $"Concurrent message {messageIndex}", "Apply")
                };
                await service.AddLogEntries(logEntry, _fixture.Organizations["0"].Id);
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
                var service = new JobLogWriter<ModuleJob, ModuleJobLogChunk, ModuleJobLogEntryDto>(
                    new FixtureDbContextFactory(_fixture), NullLogger<JobLogWriter<ModuleJob, ModuleJobLogChunk, ModuleJobLogEntryDto>>.Instance);

                var logEntry = new List<ModuleJobLogEntryDto>
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
                await service.AddLogEntries(logEntry, _fixture.Organizations["0"].Id);
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
                var service = new JobLogWriter<ModuleJob, ModuleJobLogChunk, ModuleJobLogEntryDto>(
                    new FixtureDbContextFactory(_fixture), NullLogger<JobLogWriter<ModuleJob, ModuleJobLogChunk, ModuleJobLogEntryDto>>.Instance);

                var logEntry = new List<ModuleJobLogEntryDto>
                {
                    CreateLogEntry(_testJob1.Id, $"Job1 Message {index}", "Apply")
                };
                await service.AddLogEntries(logEntry, _fixture.Organizations["0"].Id);
            });
            tasks.Add(task);
        }

        // Write to job 2 concurrently
        for (var i = 0; i < 5; i++)
        {
            var index = i;
            var task = Task.Run(async () =>
            {
                var service = new JobLogWriter<ModuleJob, ModuleJobLogChunk, ModuleJobLogEntryDto>(
                    new FixtureDbContextFactory(_fixture), NullLogger<JobLogWriter<ModuleJob, ModuleJobLogChunk, ModuleJobLogEntryDto>>.Instance);

                var logEntry = new List<ModuleJobLogEntryDto>
                {
                    CreateLogEntry(_testJob2.Id, $"Job2 Message {index}", "Apply")
                };
                await service.AddLogEntries(logEntry, _fixture.Organizations["0"].Id);
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
        var logEntries = new List<ModuleJobLogEntryDto>
        {
            CreateLogEntry(_testJob1.Id, "Job1 message", "Init"),
            CreateLogEntry(_testJob2.Id, "Job2 message", "Init"),
            CreateLogEntry(_testJob1.Id, "Job1 message 2", "Plan")
        };

        // Act
        await _logWriter.AddLogEntries(logEntries, _fixture.Organizations["0"].Id);

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
        var logEntries = new List<ModuleJobLogEntryDto>
        {
            CreateLogEntry(nonExistentJobId, "Message for non-existent job", "Init"),
            CreateLogEntry(_testJob1.Id, "Message for valid job", "Init")
        };

        // Act
        await _logWriter.AddLogEntries(logEntries, _fixture.Organizations["0"].Id);

        // Assert - should not throw, and valid job should have logs
        var logs = await _logService.GetLogEntries(_testJob1.Id);
        Assert.Single(logs);
        Assert.Equal("Message for valid job", logs[0].Message);
    }

    [Fact]
    public async Task GetLogStrings_WithNullLogContext_ShouldUseDefaultKey()
    {
        // Arrange
        var logEntries = new List<ModuleJobLogEntryDto>
        {
            CreateLogEntry(_testJob1.Id, "Message with null context", null!)
        };
        await _logWriter.AddLogEntries(logEntries, _fixture.Organizations["0"].Id);

        // Act
        var result = await _logService.GetLogStrings(_testJob1.Id);

        // Assert
        Assert.True(result.ContainsKey("Default"));
        Assert.Contains("Message with null context", result["Default"]);
    }

    #endregion

    #region Chunk Tests

    private ModuleJobLogEntryDto ModuleEntry(string message, string task, DateTimeOffset timestamp)
    {
        var entry = CreateLogEntry(_testJob1.Id, message, task, LogEventLevel.Information, timestamp, timestamp);
        entry.ModuleId = _testJob1.ModuleId;
        return entry;
    }

    [Fact]
    public async Task A_stored_log_from_before_chunks_reads_first_and_is_summarised_as_sequence_zero()
    {
        var now = DateTimeOffset.UtcNow;
        var job = await _dbContext.ModuleJobs.FindAsync(_testJob1.Id, _testJob1.OrganizationId);
        job!.Logs = System.Text.Json.JsonSerializer.Serialize(new List<ModuleJobLogEntryDto>
        {
            ModuleEntry("legacy 1", "Init", now.AddSeconds(-20)),
            ModuleEntry("legacy 2", "Init", now.AddSeconds(-10))
        });
        await _dbContext.SaveChangesAsync();

        await _logWriter.AddLogEntries([ModuleEntry("chunked", "Plan", now)], _fixture.Organizations["0"].Id);

        var summaries = await _logService.GetChunkSummaries(_testJob1.Id);
        Assert.Equal(JobLogs.LegacySequence, summaries[0].Sequence);
        Assert.Null(summaries[0].TaskNames);
        Assert.Equal(["Plan"], summaries[1].TaskNames!);

        var entries = await _logService.GetLogEntries(_testJob1.Id);
        Assert.Equal(["legacy 1", "legacy 2", "chunked"], entries.Select(e => e.Message));
    }

    [Fact]
    public async Task A_chunk_stores_only_per_line_fields_and_reads_back_the_module_context()
    {
        var entry = ModuleEntry("hello", "Init", DateTimeOffset.UtcNow);
        entry.ModuleName = "stored-name-that-must-not-be-kept";
        await _logWriter.AddLogEntries([entry], _fixture.Organizations["0"].Id);

        await using var verify = _fixture.CreateDbContext();
        var chunk = await verify.ModuleJobLogChunks.SingleAsync(c => c.JobId == _testJob1.Id);
        var json = System.Text.Encoding.UTF8.GetString(Decompress(chunk.Payload));
        Assert.DoesNotContain("stored-name-that-must-not-be-kept", json);
        Assert.DoesNotContain(_testJob1.ModuleId.ToString(), json);

        var module = await verify.Modules
            .Where(m => m.Id == _testJob1.ModuleId)
            .Select(m => new { m.Name, m.NamespaceId, NamespaceName = m.Namespace.Name, m.Namespace.StackId, StackName = m.Namespace.Stack.Name })
            .SingleAsync();

        var read = Assert.Single(await _logService.GetLogEntries(_testJob1.Id));
        Assert.Equal("hello", read.Message);
        Assert.Equal(_testJob1.Id, read.JobId);
        Assert.Equal(_testJob1.ModuleId, read.ModuleId);
        Assert.Equal(module.Name, read.ModuleName);
        Assert.Equal(module.NamespaceId, read.NamespaceId);
        Assert.Equal(module.NamespaceName, read.NamespaceName);
        Assert.Equal(module.StackId, read.StackId);
        Assert.Equal(module.StackName, read.StackName);
    }

    [Fact]
    public async Task Summaries_after_a_sequence_return_only_newer_chunks_and_reading_a_subset_returns_only_their_lines()
    {
        var now = DateTimeOffset.UtcNow;
        await _logWriter.AddLogEntries([ModuleEntry("first", "Init", now)], _fixture.Organizations["0"].Id);
        var first = (await _logService.GetChunkSummaries(_testJob1.Id)).Single();

        await _logWriter.AddLogEntries([ModuleEntry("second", "Plan", now.AddSeconds(1))], _fixture.Organizations["0"].Id);
        await _logWriter.AddLogEntries([ModuleEntry("third", "Plan", now.AddSeconds(2))], _fixture.Organizations["0"].Id);

        var newer = await _logService.GetChunkSummaries(_testJob1.Id, afterSequence: first.Sequence);
        Assert.Equal(2, newer.Count);
        Assert.All(newer, s => Assert.True(s.Sequence > first.Sequence));

        var lines = await _logService.ReadChunks(_testJob1.Id, [newer[1].Sequence]);
        Assert.Equal(["third"], lines.Select(l => l.Message));
    }

    [Fact]
    public async Task A_module_filter_reads_only_that_modules_chunks()
    {
        var now = DateTimeOffset.UtcNow;
        var other = ModuleEntry("other module", "Transfer", now);
        other.ModuleId = _testJob2.ModuleId;
        await _logWriter.AddLogEntries([ModuleEntry("own module", "Transfer", now), other], _fixture.Organizations["0"].Id);

        var own = await _logService.GetLogEntries(_testJob1.Id, _testJob1.ModuleId);
        Assert.Equal(["own module"], own.Select(e => e.Message));
        Assert.Equal(2, (await _logService.GetChunkSummaries(_testJob1.Id)).Count);
    }

    [Fact]
    public async Task An_unreadable_chunk_is_reported_and_the_rest_still_reads()
    {
        var now = DateTimeOffset.UtcNow;
        await _logWriter.AddLogEntries([ModuleEntry("broken", "Init", now)], _fixture.Organizations["0"].Id);
        await _logWriter.AddLogEntries([ModuleEntry("fine", "Init", now.AddSeconds(1))], _fixture.Organizations["0"].Id);

        await using (var db = _fixture.CreateDbContext())
        {
            var broken = await db.ModuleJobLogChunks.Where(c => c.JobId == _testJob1.Id).OrderBy(c => c.Sequence).FirstAsync();
            broken.Payload = [1, 2, 3, 4];
            await db.SaveChangesAsync();
        }

        var entries = await _logService.GetLogEntries(_testJob1.Id);
        Assert.Equal(["Part of this log could not be read.", "fine"], entries.Select(e => e.Message));
    }

    [Fact]
    public async Task A_chunk_is_stored_compressed()
    {
        var now = DateTimeOffset.UtcNow;
        var batch = Enumerable.Range(0, 200)
            .Select(i => ModuleEntry($"      + resource \"kubernetes_manifest\" \"item_{i}\" {{", "Plan", now.AddMilliseconds(i)))
            .ToList();
        batch.ForEach(e => e.BatchTimeStamp = now);
        await _logWriter.AddLogEntries(batch, _fixture.Organizations["0"].Id);

        await using var verify = _fixture.CreateDbContext();
        var chunk = await verify.ModuleJobLogChunks.SingleAsync(c => c.JobId == _testJob1.Id);
        Assert.Equal(200, chunk.LineCount);
        Assert.True(chunk.Payload.Length * 5 < chunk.UncompressedBytes,
            $"payload {chunk.Payload.Length} bytes for {chunk.UncompressedBytes} uncompressed");
    }

    [Fact]
    public async Task A_resent_batch_is_stored_once()
    {
        var batch = new List<ModuleJobLogEntryDto> { ModuleEntry("once", "Plan", DateTimeOffset.UtcNow) };

        await _logWriter.AddLogEntries(batch, _fixture.Organizations["0"].Id);
        await _logWriter.AddLogEntries(batch, _fixture.Organizations["0"].Id);

        Assert.Equal(["once"], (await _logService.GetLogEntries(_testJob1.Id)).Select(e => e.Message));
    }

    [Fact]
    public async Task Each_batch_in_one_call_is_its_own_chunk_and_keeps_its_batch_timestamp()
    {
        var now = DateTimeOffset.UtcNow;
        var later = ModuleEntry("later batch", "Plan", now.AddSeconds(1));
        await _logWriter.AddLogEntries([ModuleEntry("earlier batch", "Plan", now), later], _fixture.Organizations["0"].Id);

        Assert.Equal(2, (await _logService.GetChunkSummaries(_testJob1.Id)).Count);
        var entries = await _logService.GetLogEntries(_testJob1.Id);
        Assert.Equal(["earlier batch", "later batch"], entries.Select(e => e.Message));
        Assert.Equal(later.BatchTimeStamp, entries[1].BatchTimeStamp);
    }

    private static byte[] Decompress(byte[] payload)
    {
        using var gzip = new System.IO.Compression.GZipStream(new MemoryStream(payload), System.IO.Compression.CompressionMode.Decompress);
        using var output = new MemoryStream();
        gzip.CopyTo(output);
        return output.ToArray();
    }

    #endregion

    #region Helper Methods

    // A manual job's logs are the only channel a runner-side failure reaches the
    // operator on: the job row records no runner error.
    [Fact]
    public async Task AddLogEntries_StateMigration_AppendsAndReadsBack()
    {
        var stateMigration = new StateMigrationJob
        {
            Id = Guid.NewGuid(),
            OrganizationId = _fixture.Organizations["0"].Id,
            ModuleId = _fixture.Modules["0001"].Id,
            TimestampStart = DateTimeOffset.UtcNow,
            Status = ExecutionStatus.Running,
            JobType = "SplitProve"
        };
        _dbContext.StateMigrationJobs.Add(stateMigration);
        await _dbContext.SaveChangesAsync();

        // A manual job's logs go to its own table, so the writer and reader name that family.
        var writer = new JobLogWriter<StateMigrationJob, StateMigrationJobLogChunk, StateMigrationLogEntryDto>(
            new FixtureDbContextFactory(_fixture), NullLogger<JobLogWriter<StateMigrationJob, StateMigrationJobLogChunk, StateMigrationLogEntryDto>>.Instance);

        var line = CreateLogEntry(stateMigration.Id, "could not find the revision", "SplitProve");

        await writer.AddLogEntries(
        [
            new StateMigrationLogEntryDto
            {
                JobId = line.JobId,
                Timestamp = line.Timestamp,
                BatchTimeStamp = line.BatchTimeStamp,
                ModuleId = line.ModuleId,
                StackName = line.StackName,
                NamespaceName = line.NamespaceName,
                ModuleName = line.ModuleName,
                Level = line.Level,
                Message = line.Message,
                TaskName = line.TaskName,
                Source = line.Source
            }
        ],
            _fixture.Organizations["0"].Id);

        var reader = new JobLogReader<StateMigrationJob, StateMigrationJobLogChunk, StateMigrationLogEntryDto>(
            new FixtureDbContextFactory(_fixture), NullLogger<JobLogReader<StateMigrationJob, StateMigrationJobLogChunk, StateMigrationLogEntryDto>>.Instance);

        var entries = await reader.GetLogEntries(stateMigration.Id);
        Assert.Single(entries);
        Assert.Equal("could not find the revision", entries[0].Message);

        await using var verify = _fixture.CreateDbContext();
        Assert.True(await verify.StateMigrationJobLogChunks.AnyAsync(c => c.JobId == stateMigration.Id));
    }

    // An id in neither table is an anomaly; it must not take the other entries
    // in the same batch down with it.
    [Fact]
    public async Task AddLogEntries_UnknownJob_SkipsWithoutAffectingOthers()
    {
        await _logWriter.AddLogEntries(new List<ModuleJobLogEntryDto>
        {
            CreateLogEntry(Guid.NewGuid(), "orphan", "Apply"),
            CreateLogEntry(_testJob1.Id, "kept", "Apply")
        },
            _fixture.Organizations["0"].Id);

        var entries = await _logService.GetLogEntries(_testJob1.Id);
        Assert.Contains(entries, e => e.Message == "kept");
    }

    // Blank lines separate one block of a tool's output from the next, so they are
    // stored and returned like any other line.
    [Fact]
    public async Task AddLogEntries_BlankLines_ArePreserved()
    {
        await _logWriter.AddLogEntries(new List<ModuleJobLogEntryDto>
        {
            CreateLogEntry(_testJob2.Id, "Map under comparison:", "SplitRefactorDiff"),
            CreateLogEntry(_testJob2.Id, "", "SplitRefactorDiff"),
            CreateLogEntry(_testJob2.Id, "  app  roots/app", "SplitRefactorDiff")
        },
            _fixture.Organizations["0"].Id);

        var entries = await _logService.GetLogEntries(_testJob2.Id);
        Assert.Equal(3, entries.Count);
        Assert.Equal("", entries[1].Message);

        var byTask = await _logService.GetLogStrings(_testJob2.Id);
        Assert.Contains("Map under comparison:" + Environment.NewLine + Environment.NewLine + "  app  roots/app",
            byTask["SplitRefactorDiff"]);
    }

    [Fact]
    public async Task GetLogStrings_WithALogCorrelation_KeepsEachRunSeparate()
    {
        var firstWave = Guid.NewGuid();
        var secondWave = Guid.NewGuid();
        var moduleId = Guid.NewGuid();

        await _logWriter.AddLogEntries(
        [
            CreateLogEntry(_testJob1.Id, "first wave line", "Probe",
                logCorrelationId: firstWave,
                logCorrelationDiscriminator: LogCorrelationDiscriminator.ProbeWave,
                moduleId: moduleId),
            CreateLogEntry(_testJob1.Id, "second wave line", "Probe",
                logCorrelationId: secondWave,
                logCorrelationDiscriminator: LogCorrelationDiscriminator.ProbeWave,
                moduleId: moduleId),
            CreateLogEntry(_testJob1.Id, "plain line", "Plan", moduleId: moduleId)
        ],
            _fixture.Organizations["0"].Id);

        var result = await _logService.GetLogStrings(_testJob1.Id);

        Assert.Equal(3, result.Count);
        Assert.Contains("first wave line", result[$"Probe (ProbeWave {firstWave})"]);
        Assert.Contains("second wave line", result[$"Probe (ProbeWave {secondWave})"]);

        // A step with no correlation is still keyed by its task name alone.
        Assert.Contains("plain line", result["Plan"]);
    }

    private static ModuleJobLogEntryDto CreateLogEntry(
        Guid correlationId,
        string message,
        string logContext,
        LogEventLevel level = LogEventLevel.Information,
        DateTimeOffset? timestamp = null,
        DateTimeOffset? batchTimestamp = null,
        Guid? logCorrelationId = null,
        LogCorrelationDiscriminator? logCorrelationDiscriminator = null,
        Guid? moduleId = null)
    {
        var now = DateTimeOffset.UtcNow;
        return new ModuleJobLogEntryDto
        {
            JobId = correlationId,
            Timestamp = timestamp ?? now,
            BatchTimeStamp = batchTimestamp ?? now,
            Level = level,
            Message = message,
            TaskName = logContext,
            LogCorrelationId = logCorrelationId,
            LogCorrelationDiscriminator = logCorrelationDiscriminator,
            ModuleId = moduleId ?? Guid.NewGuid(),
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

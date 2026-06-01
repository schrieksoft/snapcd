// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using Microsoft.Extensions.Caching.Memory;
using Moq;
using SnapCd.Runner.Constants;
using SnapCd.Runner.Services;

namespace SnapCd.Runner.Tests;

public class AccessTokenCacheServiceTests
{
    [Fact]
    public void Get_ShouldReturnToken_WhenCachedTokenExists()
    {
        // Arrange
        var mockCache = new Mock<IMemoryCache>();
        var expectedToken = "test-access-token";

        object? cachedValue = expectedToken;
        mockCache.Setup(c => c.TryGetValue(MemoryCacheConstants.AccessTokenCacheKey, out cachedValue))
            .Returns(true);

        var service = new AccessTokenCacheService(mockCache.Object);

        // Act
        var result = service.Get();

        // Assert
        Assert.Equal(expectedToken, result);
        mockCache.Verify(c => c.TryGetValue(MemoryCacheConstants.AccessTokenCacheKey, out cachedValue), Times.Once);
    }

    [Fact]
    public void Get_ShouldReturnNull_WhenNoTokenInCache()
    {
        // Arrange
        var mockCache = new Mock<IMemoryCache>();

        object? cachedValue = null;
        mockCache.Setup(c => c.TryGetValue(MemoryCacheConstants.AccessTokenCacheKey, out cachedValue))
            .Returns(false);

        var service = new AccessTokenCacheService(mockCache.Object);

        // Act
        var result = service.Get();

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void Get_ShouldQueryCacheWithCorrectKey()
    {
        // Arrange
        var mockCache = new Mock<IMemoryCache>();
        object? cachedValue = null;

        mockCache.Setup(c => c.TryGetValue(It.IsAny<object>(), out cachedValue))
            .Returns(false);

        var service = new AccessTokenCacheService(mockCache.Object);

        // Act
        service.Get();

        // Assert
        mockCache.Verify(c => c.TryGetValue(MemoryCacheConstants.AccessTokenCacheKey, out cachedValue), Times.Once);
    }

    [Fact]
    public void Constructor_ShouldAcceptMemoryCache()
    {
        // Arrange
        var mockCache = new Mock<IMemoryCache>();

        // Act
        var service = new AccessTokenCacheService(mockCache.Object);

        // Assert
        Assert.NotNull(service);
    }
}

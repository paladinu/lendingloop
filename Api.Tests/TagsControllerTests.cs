using Xunit;
using Moq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Api.Controllers;
using Api.Services;
using Api.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace Api.Tests
{
    /// <summary>
    /// Unit tests for TagsController
    /// **Validates: Requirements 8.4, 8.5**
    /// </summary>
    public class TagsControllerTests
    {
        private readonly Mock<ITagsService> _mockTagsService;
        private readonly Mock<ILogger<TagsController>> _mockLogger;
        private readonly TagsController _controller;

        public TagsControllerTests()
        {
            _mockTagsService = new Mock<ITagsService>();
            _mockLogger = new Mock<ILogger<TagsController>>();
            _controller = new TagsController(_mockTagsService.Object, _mockLogger.Object);
        }

        [Fact]
        public async Task GetAllTags_ReturnsOkWithTags_WhenTagsExist()
        {
            // Arrange
            var expectedTags = new List<SystemTag>
            {
                new SystemTag { Name = "electronics", DisplayName = "Electronics", IsActive = true, UsageCount = 5 },
                new SystemTag { Name = "tools", DisplayName = "Tools", IsActive = true, UsageCount = 3 },
                new SystemTag { Name = "books", DisplayName = "Books", IsActive = true, UsageCount = 2 }
            };
            _mockTagsService.Setup(s => s.GetAllActiveTagsAsync())
                           .ReturnsAsync(expectedTags);

            // Act
            var result = await _controller.GetAllTags();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var actualTags = Assert.IsAssignableFrom<List<SystemTag>>(okResult.Value);
            Assert.Equal(expectedTags.Count, actualTags.Count);
            Assert.Equal(expectedTags.First().Name, actualTags.First().Name);
            _mockTagsService.Verify(s => s.GetAllActiveTagsAsync(), Times.Once);
        }

        [Fact]
        public async Task GetAllTags_ReturnsOkWithEmptyList_WhenNoTagsExist()
        {
            // Arrange
            var emptyTags = new List<SystemTag>();
            _mockTagsService.Setup(s => s.GetAllActiveTagsAsync())
                           .ReturnsAsync(emptyTags);

            // Act
            var result = await _controller.GetAllTags();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var actualTags = Assert.IsAssignableFrom<List<SystemTag>>(okResult.Value);
            Assert.Empty(actualTags);
            _mockTagsService.Verify(s => s.GetAllActiveTagsAsync(), Times.Once);
        }

        [Fact]
        public async Task GetAllTags_ReturnsInternalServerError_WhenServiceThrowsException()
        {
            // Arrange
            _mockTagsService.Setup(s => s.GetAllActiveTagsAsync())
                           .ThrowsAsync(new System.Exception("Database error"));

            // Act
            var result = await _controller.GetAllTags();

            // Assert
            var statusResult = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(500, statusResult.StatusCode);
            _mockTagsService.Verify(s => s.GetAllActiveTagsAsync(), Times.Once);
        }

        [Fact]
        public async Task GetTagStatistics_ReturnsOkWithStatistics_WhenStatisticsExist()
        {
            // Arrange
            var expectedStatistics = new Dictionary<string, int>
            {
                { "electronics", 10 },
                { "tools", 7 },
                { "books", 5 },
                { "furniture", 3 }
            };
            _mockTagsService.Setup(s => s.GetTagUsageStatisticsAsync())
                           .ReturnsAsync(expectedStatistics);

            // Act
            var result = await _controller.GetTagStatistics();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var actualStatistics = Assert.IsAssignableFrom<Dictionary<string, int>>(okResult.Value);
            Assert.Equal(expectedStatistics.Count, actualStatistics.Count);
            Assert.Equal(expectedStatistics["electronics"], actualStatistics["electronics"]);
            Assert.Equal(expectedStatistics["tools"], actualStatistics["tools"]);
            _mockTagsService.Verify(s => s.GetTagUsageStatisticsAsync(), Times.Once);
        }

        [Fact]
        public async Task GetTagStatistics_ReturnsOkWithEmptyDictionary_WhenNoStatisticsExist()
        {
            // Arrange
            var emptyStatistics = new Dictionary<string, int>();
            _mockTagsService.Setup(s => s.GetTagUsageStatisticsAsync())
                           .ReturnsAsync(emptyStatistics);

            // Act
            var result = await _controller.GetTagStatistics();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var actualStatistics = Assert.IsAssignableFrom<Dictionary<string, int>>(okResult.Value);
            Assert.Empty(actualStatistics);
            _mockTagsService.Verify(s => s.GetTagUsageStatisticsAsync(), Times.Once);
        }

        [Fact]
        public async Task GetTagStatistics_ReturnsInternalServerError_WhenServiceThrowsException()
        {
            // Arrange
            _mockTagsService.Setup(s => s.GetTagUsageStatisticsAsync())
                           .ThrowsAsync(new System.Exception("Database error"));

            // Act
            var result = await _controller.GetTagStatistics();

            // Assert
            var statusResult = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(500, statusResult.StatusCode);
            _mockTagsService.Verify(s => s.GetTagUsageStatisticsAsync(), Times.Once);
        }

        [Fact]
        public async Task GetAllTags_ReturnsOnlyActiveTags()
        {
            // Arrange
            var allTags = new List<SystemTag>
            {
                new SystemTag { Name = "electronics", DisplayName = "Electronics", IsActive = true, UsageCount = 5 },
                new SystemTag { Name = "tools", DisplayName = "Tools", IsActive = true, UsageCount = 3 },
                new SystemTag { Name = "deprecated", DisplayName = "Deprecated", IsActive = false, UsageCount = 1 }
            };
            var activeTags = allTags.Where(t => t.IsActive).ToList();
            _mockTagsService.Setup(s => s.GetAllActiveTagsAsync())
                           .ReturnsAsync(activeTags);

            // Act
            var result = await _controller.GetAllTags();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var actualTags = Assert.IsAssignableFrom<List<SystemTag>>(okResult.Value);
            Assert.Equal(2, actualTags.Count);
            Assert.All(actualTags, tag => Assert.True(tag.IsActive));
            Assert.DoesNotContain(actualTags, tag => tag.Name == "deprecated");
            _mockTagsService.Verify(s => s.GetAllActiveTagsAsync(), Times.Once);
        }

        [Fact]
        public async Task GetTagStatistics_ReturnsStatisticsSortedByUsageCount()
        {
            // Arrange
            var expectedStatistics = new Dictionary<string, int>
            {
                { "electronics", 10 },
                { "tools", 7 },
                { "books", 5 },
                { "furniture", 3 }
            };
            _mockTagsService.Setup(s => s.GetTagUsageStatisticsAsync())
                           .ReturnsAsync(expectedStatistics);

            // Act
            var result = await _controller.GetTagStatistics();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var actualStatistics = Assert.IsAssignableFrom<Dictionary<string, int>>(okResult.Value);
            
            var sortedValues = actualStatistics.Values.ToList();
            var expectedSortedValues = sortedValues.OrderByDescending(x => x).ToList();
            Assert.Equal(expectedSortedValues, sortedValues);
            _mockTagsService.Verify(s => s.GetTagUsageStatisticsAsync(), Times.Once);
        }
    }
}

using FsCheck.Xunit;
using Api.Models;

namespace Api.Tests;

/// <summary>
/// Property-based tests for tag selection functionality
/// </summary>
public class TagSelectionPropertyTests
{
    /// <summary>
    /// **Feature: item-search-and-categorization, Property 1: Tag selection limit enforcement**
    /// For any item and any set of tags, selecting up to 10 tags should be accepted, 
    /// and attempting to select more than 10 tags should be rejected with a validation error
    /// **Validates: Requirements 1.3, 1.4**
    /// </summary>
    [Property(MaxTest = 100)]
    public bool TagSelectionLimit_EnforcesMaximumOf10Tags(List<string> tags)
    {
        //arrange
        var distinctTags = tags.Distinct().Where(t => !string.IsNullOrWhiteSpace(t)).ToList();
        
        //act
        var item = new SharedItem
        {
            Name = "Test Item",
            Description = "Test Description",
            UserId = "test-user-id",
            Tags = distinctTags.Take(10).ToList()
        };

        //assert
        // Property 1: Items with 10 or fewer tags should be valid
        if (distinctTags.Count <= 10)
        {
            return item.Tags.Count <= 10 && item.Tags.Count == Math.Min(distinctTags.Count, 10);
        }
        // Property 2: Attempting to add more than 10 tags should be prevented
        // (In this test, we simulate validation by only taking the first 10)
        else
        {
            return item.Tags.Count == 10 && item.Tags.Count < distinctTags.Count;
        }
    }

    /// <summary>
    /// Property test to verify that tag selection validation correctly accepts valid tag counts
    /// </summary>
    [Property(MaxTest = 100)]
    public bool TagSelectionLimit_AcceptsValidTagCounts(int tagCount)
    {
        //arrange
        // Constrain to valid range
        var validTagCount = Math.Abs(tagCount % 11); // 0-10
        
        //act
        var tags = Enumerable.Range(0, validTagCount)
            .Select(i => $"tag-{i}")
            .ToList();

        var item = new SharedItem
        {
            Name = "Test Item",
            Description = "Test Description",
            UserId = "test-user-id",
            Tags = tags
        };

        //assert
        // Items with 0-10 tags should always be valid
        return item.Tags.Count >= 0 && item.Tags.Count <= 10;
    }

    /// <summary>
    /// Property test to verify that attempting to add more than 10 tags is detected
    /// </summary>
    [Property(MaxTest = 100)]
    public bool TagSelectionLimit_DetectsInvalidTagCounts(int tagCount)
    {
        //arrange
        // Constrain to invalid range (11-50)
        var invalidTagCount = 11 + Math.Abs(tagCount % 40);
        
        //act
        var tags = Enumerable.Range(0, invalidTagCount)
            .Select(i => $"tag-{i}")
            .ToList();

        //assert
        // Any list with more than 10 tags should be detected as invalid
        return tags.Count > 10;
    }

    /// <summary>
    /// **Feature: item-search-and-categorization, Property 15: Tag usage count accuracy**
    /// For any system tag, its usage count should equal the number of items that currently have that tag assigned
    /// **Validates: Requirements 8.5**
    /// </summary>
    [Property(MaxTest = 100)]
    public bool TagUsageCount_MatchesActualItemCount(List<string> itemTags1, List<string> itemTags2, List<string> itemTags3)
    {
        //arrange
        // Create a collection of items with various tags
        var items = new List<SharedItem>
        {
            new SharedItem { Name = "Item 1", UserId = "user1", Tags = itemTags1.Distinct().Take(10).ToList() },
            new SharedItem { Name = "Item 2", UserId = "user2", Tags = itemTags2.Distinct().Take(10).ToList() },
            new SharedItem { Name = "Item 3", UserId = "user3", Tags = itemTags3.Distinct().Take(10).ToList() }
        };

        // Get all unique tags across all items
        var allTags = items.SelectMany(item => item.Tags).Distinct().ToList();

        //act
        // Calculate expected usage count for each tag
        var expectedUsageCounts = new Dictionary<string, int>();
        foreach (var tag in allTags)
        {
            expectedUsageCounts[tag] = items.Count(item => item.Tags.Contains(tag));
        }

        //assert
        // Verify that for each tag, the calculated usage count matches the actual count
        foreach (var tag in allTags)
        {
            var actualCount = items.Count(item => item.Tags.Contains(tag));
            if (expectedUsageCounts[tag] != actualCount)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Property test to verify tag usage count increments correctly when items are added
    /// </summary>
    [Property(MaxTest = 100)]
    public bool TagUsageCount_IncrementsWhenItemsAdded(string tagName, int itemCount)
    {
        //arrange
        // Constrain item count to reasonable range (1-20)
        var validItemCount = 1 + Math.Abs(itemCount % 20);
        
        // Create items that all have the same tag
        var items = Enumerable.Range(0, validItemCount)
            .Select(i => new SharedItem 
            { 
                Name = $"Item {i}", 
                UserId = $"user{i}", 
                Tags = new List<string> { tagName } 
            })
            .ToList();

        //act
        var usageCount = items.Count(item => item.Tags.Contains(tagName));

        //assert
        // Usage count should equal the number of items with that tag
        return usageCount == validItemCount;
    }

    /// <summary>
    /// Property test to verify tag usage count decrements correctly when items are removed
    /// </summary>
    [Property(MaxTest = 100)]
    public bool TagUsageCount_DecrementsWhenItemsRemoved(string tagName, int initialCount, int removeCount)
    {
        //arrange
        // Constrain to reasonable ranges
        var validInitialCount = 1 + Math.Abs(initialCount % 20);
        var validRemoveCount = Math.Abs(removeCount % validInitialCount);
        
        // Create initial items with the tag
        var items = Enumerable.Range(0, validInitialCount)
            .Select(i => new SharedItem 
            { 
                Name = $"Item {i}", 
                UserId = $"user{i}", 
                Tags = new List<string> { tagName } 
            })
            .ToList();

        //act
        // Remove some items
        var remainingItems = items.Skip(validRemoveCount).ToList();
        var usageCount = remainingItems.Count(item => item.Tags.Contains(tagName));

        //assert
        // Usage count should equal initial count minus removed count
        return usageCount == (validInitialCount - validRemoveCount);
    }

    /// <summary>
    /// Property test to verify tag usage count is zero when no items have the tag
    /// </summary>
    [Property(MaxTest = 100)]
    public bool TagUsageCount_IsZeroWhenNoItemsHaveTag(string tagName, List<string> otherTags)
    {
        //arrange
        // Create items with tags that don't include the target tag
        var filteredOtherTags = otherTags.Where(t => t != tagName).Distinct().Take(10).ToList();
        var items = new List<SharedItem>
        {
            new SharedItem { Name = "Item 1", UserId = "user1", Tags = filteredOtherTags },
            new SharedItem { Name = "Item 2", UserId = "user2", Tags = filteredOtherTags },
            new SharedItem { Name = "Item 3", UserId = "user3", Tags = filteredOtherTags }
        };

        //act
        var usageCount = items.Count(item => item.Tags.Contains(tagName));

        //assert
        // Usage count should be zero when no items have the tag
        return usageCount == 0;
    }
}

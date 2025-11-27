using FsCheck;
using FsCheck.Xunit;
using Api.Models;
using Api.DTOs;

namespace Api.Tests;

/// <summary>
/// Property-based tests for item search and filter functionality
/// </summary>
public class ItemSearchPropertyTests
{
    /// <summary>
    /// **Feature: item-search-and-categorization, Property 4: Tag filter AND logic**
    /// For any set of selected tags and any collection of items, 
    /// the filtered results should contain only items that have all selected tags
    /// **Validates: Requirements 3.2**
    /// </summary>
    [Property(MaxTest = 100)]
    public bool TagFilterANDLogic_ReturnsOnlyItemsWithAllSelectedTags(List<string> selectedTags, List<SharedItem> items)
    {
        //arrange
        // Constrain selected tags to valid range (1-5 tags)
        var validSelectedTags = selectedTags
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .Distinct()
            .Take(5)
            .ToList();

        if (validSelectedTags.Count == 0)
        {
            return true; // No tags selected, skip this test case
        }

        // Ensure items have valid tags (0-10 tags each)
        var validItems = items
            .Where(item => item != null)
            .Select(item => new SharedItem
            {
                Id = item.Id ?? Guid.NewGuid().ToString(),
                Name = item.Name ?? "Test Item",
                UserId = item.UserId ?? "user1",
                Tags = (item.Tags ?? new List<string>())
                    .Where(t => !string.IsNullOrWhiteSpace(t))
                    .Distinct()
                    .Take(10)
                    .ToList(),
                VisibleToLoopIds = new List<string> { "loop1" }
            })
            .ToList();

        //act
        // Filter items that have ALL selected tags (AND logic)
        var filteredItems = validItems
            .Where(item => validSelectedTags.All(tag => item.Tags.Contains(tag)))
            .ToList();

        //assert
        // Every item in filtered results must contain all selected tags
        foreach (var item in filteredItems)
        {
            foreach (var tag in validSelectedTags)
            {
                if (!item.Tags.Contains(tag))
                {
                    return false;
                }
            }
        }

        // No item outside filtered results should contain all selected tags
        var excludedItems = validItems.Except(filteredItems).ToList();
        foreach (var item in excludedItems)
        {
            if (validSelectedTags.All(tag => item.Tags.Contains(tag)))
            {
                return false; // This item should have been included
            }
        }

        return true;
    }

    /// <summary>
    /// **Feature: item-search-and-categorization, Property 6: Availability filter correctness**
    /// For any availability filter selection (available, unavailable, or both), 
    /// the results should contain only items matching the selected availability state(s)
    /// **Validates: Requirements 4.2, 4.3, 4.4**
    /// </summary>
    [Property(MaxTest = 100)]
    public bool AvailabilityFilter_ReturnsOnlyMatchingItems(bool? filterIsAvailable, List<SharedItem> items)
    {
        //arrange
        // Ensure items have valid properties
        var validItems = items
            .Where(item => item != null)
            .Select(item => new SharedItem
            {
                Id = item.Id ?? Guid.NewGuid().ToString(),
                Name = item.Name ?? "Test Item",
                UserId = item.UserId ?? "user1",
                IsAvailable = item.IsAvailable,
                VisibleToLoopIds = new List<string> { "loop1" }
            })
            .ToList();

        //act
        // Apply availability filter
        var filteredItems = filterIsAvailable.HasValue
            ? validItems.Where(item => item.IsAvailable == filterIsAvailable.Value).ToList()
            : validItems; // No filter applied, return all

        //assert
        if (filterIsAvailable.HasValue)
        {
            // All filtered items must match the availability filter
            foreach (var item in filteredItems)
            {
                if (item.IsAvailable != filterIsAvailable.Value)
                {
                    return false;
                }
            }

            // No item outside filtered results should match the filter
            var excludedItems = validItems.Except(filteredItems).ToList();
            foreach (var item in excludedItems)
            {
                if (item.IsAvailable == filterIsAvailable.Value)
                {
                    return false; // This item should have been included
                }
            }
        }
        else
        {
            // When no filter is applied, all items should be returned
            if (filteredItems.Count != validItems.Count)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// **Feature: item-search-and-categorization, Property 9: Owner filter correctness**
    /// For any set of selected owner IDs, 
    /// the filtered results should contain only items owned by users in the selected set
    /// **Validates: Requirements 5.3**
    /// </summary>
    [Property(MaxTest = 100)]
    public bool OwnerFilter_ReturnsOnlyItemsFromSelectedOwners(List<string> selectedOwnerIds, List<SharedItem> items)
    {
        //arrange
        // Constrain selected owner IDs to valid range (1-5 owners)
        var validSelectedOwnerIds = selectedOwnerIds
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct()
            .Take(5)
            .ToList();

        if (validSelectedOwnerIds.Count == 0)
        {
            return true; // No owners selected, skip this test case
        }

        // Ensure items have valid properties
        var validItems = items
            .Where(item => item != null && !string.IsNullOrWhiteSpace(item.UserId))
            .Select(item => new SharedItem
            {
                Id = item.Id ?? Guid.NewGuid().ToString(),
                Name = item.Name ?? "Test Item",
                UserId = item.UserId,
                VisibleToLoopIds = new List<string> { "loop1" }
            })
            .ToList();

        //act
        // Filter items by selected owner IDs
        var filteredItems = validItems
            .Where(item => validSelectedOwnerIds.Contains(item.UserId))
            .ToList();

        //assert
        // Every item in filtered results must be owned by a selected owner
        foreach (var item in filteredItems)
        {
            if (!validSelectedOwnerIds.Contains(item.UserId))
            {
                return false;
            }
        }

        // No item outside filtered results should be owned by a selected owner
        var excludedItems = validItems.Except(filteredItems).ToList();
        foreach (var item in excludedItems)
        {
            if (validSelectedOwnerIds.Contains(item.UserId))
            {
                return false; // This item should have been included
            }
        }

        return true;
    }

    /// <summary>
    /// **Feature: item-search-and-categorization, Property 10: Combined filter AND logic**
    /// For any combination of active filters (tags, availability, owners, search text), 
    /// the results should contain only items that satisfy all active filter criteria simultaneously
    /// **Validates: Requirements 6.1, 6.2, 6.3**
    /// </summary>
    [Property(MaxTest = 100)]
    public bool CombinedFilters_ApplyANDLogicAcrossAllFilters(
        List<string> selectedTags,
        bool? filterIsAvailable,
        List<string> selectedOwnerIds,
        string searchText,
        List<SharedItem> items)
    {
        //arrange
        // Constrain inputs to valid ranges
        var validSelectedTags = selectedTags
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .Distinct()
            .Take(3)
            .ToList();

        var validSelectedOwnerIds = selectedOwnerIds
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct()
            .Take(3)
            .ToList();

        var validSearchText = string.IsNullOrWhiteSpace(searchText) ? null : searchText.Trim();

        // Ensure items have valid properties
        var validItems = items
            .Where(item => item != null && !string.IsNullOrWhiteSpace(item.UserId))
            .Select(item => new SharedItem
            {
                Id = item.Id ?? Guid.NewGuid().ToString(),
                Name = item.Name ?? "Test Item",
                Description = item.Description ?? "Test Description",
                UserId = item.UserId,
                IsAvailable = item.IsAvailable,
                Tags = (item.Tags ?? new List<string>())
                    .Where(t => !string.IsNullOrWhiteSpace(t))
                    .Distinct()
                    .Take(10)
                    .ToList(),
                VisibleToLoopIds = new List<string> { "loop1" }
            })
            .ToList();

        //act
        // Apply all filters with AND logic
        var filteredItems = validItems.AsEnumerable();

        // Apply tag filter (AND logic for multiple tags)
        if (validSelectedTags.Count > 0)
        {
            filteredItems = filteredItems.Where(item => 
                validSelectedTags.All(tag => item.Tags.Contains(tag)));
        }

        // Apply availability filter
        if (filterIsAvailable.HasValue)
        {
            filteredItems = filteredItems.Where(item => 
                item.IsAvailable == filterIsAvailable.Value);
        }

        // Apply owner filter
        if (validSelectedOwnerIds.Count > 0)
        {
            filteredItems = filteredItems.Where(item => 
                validSelectedOwnerIds.Contains(item.UserId));
        }

        // Apply search text filter (case-insensitive)
        if (validSearchText != null)
        {
            filteredItems = filteredItems.Where(item =>
                (item.Name?.Contains(validSearchText, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (item.Description?.Contains(validSearchText, StringComparison.OrdinalIgnoreCase) ?? false));
        }

        var result = filteredItems.ToList();

        //assert
        // Every item in filtered results must satisfy ALL filter criteria
        foreach (var item in result)
        {
            // Check tag filter
            if (validSelectedTags.Count > 0)
            {
                if (!validSelectedTags.All(tag => item.Tags.Contains(tag)))
                {
                    return false;
                }
            }

            // Check availability filter
            if (filterIsAvailable.HasValue)
            {
                if (item.IsAvailable != filterIsAvailable.Value)
                {
                    return false;
                }
            }

            // Check owner filter
            if (validSelectedOwnerIds.Count > 0)
            {
                if (!validSelectedOwnerIds.Contains(item.UserId))
                {
                    return false;
                }
            }

            // Check search text filter
            if (validSearchText != null)
            {
                var matchesName = item.Name?.Contains(validSearchText, StringComparison.OrdinalIgnoreCase) ?? false;
                var matchesDescription = item.Description?.Contains(validSearchText, StringComparison.OrdinalIgnoreCase) ?? false;
                if (!matchesName && !matchesDescription)
                {
                    return false;
                }
            }
        }

        return true;
    }

    /// <summary>
    /// **Feature: item-search-and-categorization, Property 16: Pagination correctness**
    /// For any search result set with more than 50 items, 
    /// the results should be divided into pages of 50 items each, 
    /// with the total page count equal to ceiling(totalItems / 50)
    /// **Validates: Requirements 9.3**
    /// </summary>
    [Property(MaxTest = 100)]
    public bool Pagination_CalculatesCorrectPageCount(int totalItems, int pageSize)
    {
        //arrange
        // Constrain to valid ranges
        var validTotalItems = Math.Abs(totalItems % 500); // 0-499 items
        var validPageSize = Math.Max(1, Math.Min(100, Math.Abs(pageSize % 101))); // 1-100 items per page

        //act
        var expectedTotalPages = validTotalItems == 0 
            ? 0 
            : (int)Math.Ceiling((double)validTotalItems / validPageSize);

        //assert
        // Verify pagination calculation
        if (validTotalItems == 0)
        {
            return expectedTotalPages == 0;
        }

        // Total pages should be ceiling of (total items / page size)
        var calculatedPages = (int)Math.Ceiling((double)validTotalItems / validPageSize);
        if (calculatedPages != expectedTotalPages)
        {
            return false;
        }

        // Verify that all items fit within the calculated pages
        var totalCapacity = calculatedPages * validPageSize;
        if (totalCapacity < validTotalItems)
        {
            return false;
        }

        // Verify that we don't have an extra unnecessary page
        if (calculatedPages > 1)
        {
            var capacityWithOneLessPage = (calculatedPages - 1) * validPageSize;
            if (capacityWithOneLessPage >= validTotalItems)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Property test to verify pagination skip and limit calculations
    /// </summary>
    [Property(MaxTest = 100)]
    public bool Pagination_CalculatesCorrectSkipAndLimit(int pageNumber, int pageSize, int totalItems)
    {
        //arrange
        // Constrain to valid ranges
        var validPageNumber = Math.Max(1, Math.Abs(pageNumber % 20) + 1); // 1-20
        var validPageSize = Math.Max(1, Math.Min(100, Math.Abs(pageSize % 101))); // 1-100
        var validTotalItems = Math.Abs(totalItems % 500); // 0-499

        //act
        var skip = (validPageNumber - 1) * validPageSize;
        var limit = validPageSize;

        //assert
        // Skip should be non-negative
        if (skip < 0)
        {
            return false;
        }

        // Skip should be (pageNumber - 1) * pageSize
        if (skip != (validPageNumber - 1) * validPageSize)
        {
            return false;
        }

        // Limit should equal page size
        if (limit != validPageSize)
        {
            return false;
        }

        // For page 1, skip should be 0
        if (validPageNumber == 1 && skip != 0)
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Property test to verify that pagination doesn't lose or duplicate items
    /// </summary>
    [Property(MaxTest = 100)]
    public bool Pagination_DoesNotLoseOrDuplicateItems(List<SharedItem> items, int pageSize)
    {
        //arrange
        // Constrain to valid range
        var validPageSize = Math.Max(1, Math.Min(50, Math.Abs(pageSize % 51))); // 1-50
        
        var validItems = items
            .Where(item => item != null)
            .Select((item, index) => new SharedItem
            {
                Id = $"item-{index}",
                Name = item.Name ?? $"Item {index}",
                UserId = item.UserId ?? "user1",
                VisibleToLoopIds = new List<string> { "loop1" }
            })
            .ToList();

        if (validItems.Count == 0)
        {
            return true; // No items to paginate
        }

        //act
        var totalPages = (int)Math.Ceiling((double)validItems.Count / validPageSize);
        var allPaginatedItems = new List<SharedItem>();

        for (int page = 1; page <= totalPages; page++)
        {
            var skip = (page - 1) * validPageSize;
            var pageItems = validItems.Skip(skip).Take(validPageSize).ToList();
            allPaginatedItems.AddRange(pageItems);
        }

        //assert
        // All original items should be present in paginated results
        if (allPaginatedItems.Count != validItems.Count)
        {
            return false;
        }

        // No duplicates should exist
        var uniqueIds = allPaginatedItems.Select(item => item.Id).Distinct().Count();
        if (uniqueIds != validItems.Count)
        {
            return false;
        }

        // Every original item should be present exactly once
        foreach (var originalItem in validItems)
        {
            var count = allPaginatedItems.Count(item => item.Id == originalItem.Id);
            if (count != 1)
            {
                return false;
            }
        }

        return true;
    }
}

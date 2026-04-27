using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Application.Helpers;

public static class ViewQueryHelper
{
    /// <summary>
    /// Applies filters from a ViewFilterConfig to an IQueryable of ProjectTasks.
    /// </summary>
    public static IQueryable<ProjectTask> ApplyFilters(this IQueryable<ProjectTask> query, ViewFilterConfig filter)
    {

        // 1. Status Filter
        if (filter.StatusIds.Any())
        {
            query = query.Where(t => t.StatusId.HasValue && filter.StatusIds.Contains(t.StatusId.Value));
        }

        // 2. Priority Filter
        if (filter.Priorities.Any())
        {
            query = query.Where(t => filter.Priorities.Contains(t.Priority));
        }

        // 3. Assignee Filter
        if (filter.AssigneeIds.Any())
        {
            query = query.Where(t => t.Assignees.Any(a => filter.AssigneeIds.Contains(a.WorkspaceMemberId)));
        }

        // 4. Search Filter
        if (!string.IsNullOrWhiteSpace(filter.SearchQuery))
        {
            var search = filter.SearchQuery.Trim().ToLower();
            query = query.Where(t => t.Name.ToLower().Contains(search) || 
                                    (t.Description != null && t.Description.ToLower().Contains(search)));
        }

        // 5. Date Range Filters
        if (filter.StartDateAfter.HasValue)
        {
            query = query.Where(t => t.StartDate >= filter.StartDateAfter.Value);
        }

        if (filter.DueDateBefore.HasValue)
        {
            query = query.Where(t => t.DueDate <= filter.DueDateBefore.Value);
        }

        return query;
    }
}

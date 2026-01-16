using Application.Contract.UserContract;
using Application.Features.WorkspaceFeatures.GetWorkspaceList;
using Domain.Entities;
using Domain.Entities.ProjectEntities;
using Domain.Entities.Relationship;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Features.WorkspaceFeatures.MemberManage.GetMembers;

//public static class GetMembersCursorExtensions
//{
//    public static IQueryable<MemberDto> ApplyFilter(this IQueryable<WorkspaceMember> members, IQueryable<User> users, GetMembersFilter filter)
//    {
//        var query = from wm in members
//                    join u in users on wm.UserId equals u.Id
//                    where wm.ProjectWorkspaceId == filter.WorkspaceId
//                    select new { wm, u };

//        if (!string.IsNullOrEmpty(filter.Name)) { 
//            var nameLower = filter.Name.ToLower();
//            query = query.Where(x => EF.Functions.Like(x.u.Name, $"%{nameLower}"));
//        }
//        if (filter.Owned) query = query.Where(w => w.CreatorId == currentUserId);
//        if (filter.isArchived) query = query.Where(w => w.IsArchived == filter.isArchived);
//        if (filter.Variant != null) query = query.Where(w => w.Variant == filter.Variant);
//        return query;
//    }
//}

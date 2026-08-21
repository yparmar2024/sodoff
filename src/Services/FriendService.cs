using Microsoft.EntityFrameworkCore;
using sodoff.Model;
using sodoff.Schema;
using sodoff.Controllers.Common;
using System.Linq;
using System;
using System.Collections.Generic;

namespace sodoff.Services;

public class FriendService {
    private readonly DBContext _ctx;

    // MMO Event Codes for buddy relationships
    public const string MMOEventAddBuddy = "1";
    public const string MMOEventRemoveBuddy = "2";
    public const string MMOEventBlockBuddy = "3";
    public const string MMOEventApproveBuddy = "4";

    public FriendService(DBContext ctx) {
        _ctx = ctx;
    }

    /// <summary>
    /// Gets a single row representing the relationship between two vikings, regardless of order.
    /// </summary>
    public BuddyRelationship? GetRelationship(int vikingA, int vikingB) {
        int id1 = Math.Min(vikingA, vikingB);
        int id2 = Math.Max(vikingA, vikingB);
        return _ctx.BuddyRelationships.FirstOrDefault(b => b.VikingId1 == id1 && b.VikingId2 == id2);
    }

    /// <summary>
    /// Adds a relationship or updates an existing one if not blocked.
    /// </summary>
    public BuddyActionResult AddBuddy(Viking viking, Viking buddyViking) {
        if (viking.Id == buddyViking.Id) {
            return new BuddyActionResult { Result = BuddyActionResultType.CannotAddSelf };
        }

        int id1 = Math.Min(viking.Id, buddyViking.Id);
        int id2 = Math.Max(viking.Id, buddyViking.Id);

        var existing = _ctx.BuddyRelationships.FirstOrDefault(b => b.VikingId1 == id1 && b.VikingId2 == id2);

        if (existing != null) {
            if (existing.Status == BuddyStatus.BlockedByBoth || 
                (existing.Status == BuddyStatus.BlockedBySelf && existing.InitiatorId == viking.Id) ||
                (existing.Status == BuddyStatus.BlockedByOther && existing.InitiatorId != viking.Id)) {
                return new BuddyActionResult { Result = BuddyActionResultType.Success }; // Silently fail block
            }
            return new BuddyActionResult { Result = BuddyActionResultType.AlreadyInList };
        }

        var rel = new BuddyRelationship {
            VikingId1 = id1,
            VikingId2 = id2,
            InitiatorId = viking.Id,
            Status = BuddyStatus.PendingApprovalFromOther,
            CreateDate = DateTime.UtcNow,
            Viking1BestBuddy = false,
            Viking2BestBuddy = false
        };

        _ctx.BuddyRelationships.Add(rel);
        _ctx.SaveChanges();

        return new BuddyActionResult { 
            Result = BuddyActionResultType.Success, 
            Status = BuddyStatus.PendingApprovalFromOther, 
            BuddyUserID = buddyViking.Uid.ToString() 
        };
    }

    public bool ApproveBuddy(Viking viking, Viking buddyViking) {
        var rel = GetRelationship(viking.Id, buddyViking.Id);
        
        // Ensure it's pending and the other person sent it
        if (rel != null && rel.Status == BuddyStatus.PendingApprovalFromOther && rel.InitiatorId == buddyViking.Id) {
            rel.Status = BuddyStatus.Approved;
            _ctx.SaveChanges();
            return true;
        }
        return false;
    }

    public bool RemoveBuddy(Viking viking, Viking buddyViking) {
        var rel = GetRelationship(viking.Id, buddyViking.Id);
        if (rel != null) {
            _ctx.BuddyRelationships.Remove(rel);
            _ctx.SaveChanges();
            return true;
        }
        return false;
    }

    public bool BlockBuddy(Viking viking, Viking buddyViking) {
        var rel = GetRelationship(viking.Id, buddyViking.Id);
        if (rel == null) {
            rel = new BuddyRelationship {
                VikingId1 = Math.Min(viking.Id, buddyViking.Id),
                VikingId2 = Math.Max(viking.Id, buddyViking.Id),
                InitiatorId = viking.Id,
                CreateDate = DateTime.UtcNow
            };
            _ctx.BuddyRelationships.Add(rel);
        }

        if (rel.Status == BuddyStatus.BlockedByBoth) {
            // Already blocked by both
        } else if (rel.Status == BuddyStatus.BlockedByOther && rel.InitiatorId != viking.Id) {
            rel.Status = BuddyStatus.BlockedByBoth;
        } else if (rel.Status == BuddyStatus.BlockedBySelf && rel.InitiatorId == viking.Id) {
            // Already blocked by self
        } else {
            rel.InitiatorId = viking.Id;
            rel.Status = BuddyStatus.BlockedBySelf;
        }
        
        _ctx.SaveChanges();
        return true;
    }

    public bool UpdateBestBuddy(Viking viking, Viking buddyViking, bool isBestBuddy) {
        var rel = GetRelationship(viking.Id, buddyViking.Id);
        if (rel != null) {
            if (rel.VikingId1 == viking.Id) rel.Viking1BestBuddy = isBestBuddy;
            else rel.Viking2BestBuddy = isBestBuddy;
            _ctx.SaveChanges();
            return true;
        }
        return false;
    }

    public List<Buddy> GetBuddyListForViking(Viking viking) {
        var relationships = _ctx.BuddyRelationships
            .Include(b => b.Viking1)
            .Include(b => b.Viking2)
            .Where(b => b.VikingId1 == viking.Id || b.VikingId2 == viking.Id)
            .ToList();

        var buddies = new List<Buddy>();

        foreach (var rel in relationships) {
            bool isViking1 = rel.VikingId1 == viking.Id;
            var buddyUser = isViking1 ? rel.Viking2 : rel.Viking1;
            bool isInitiator = rel.InitiatorId == viking.Id;

            // Map status based on perspective
            BuddyStatus mappedStatus = rel.Status;
            if (rel.Status == BuddyStatus.PendingApprovalFromOther) {
                mappedStatus = isInitiator ? BuddyStatus.PendingApprovalFromOther : BuddyStatus.PendingApprovalFromSelf;
            } else if (rel.Status == BuddyStatus.BlockedBySelf) {
                mappedStatus = isInitiator ? BuddyStatus.BlockedBySelf : BuddyStatus.BlockedByOther;
            }

            // Map BestBuddy
            bool bestBuddy = isViking1 ? rel.Viking1BestBuddy : rel.Viking2BestBuddy;

            buddies.Add(new Buddy {
                UserID = buddyUser.Uid.ToString(),
                DisplayName = buddyUser.Name,
                Status = mappedStatus,
                BestBuddy = bestBuddy,
                Online = buddyUser.IsOnline,
                OnMobile = false,
                CreateDate = rel.CreateDate
            });
        }
        return buddies;
    }
}

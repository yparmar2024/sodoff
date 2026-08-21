using sodoff.Schema;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace sodoff.Model;

public class BuddyRelationship {
    public int VikingId1 { get; set; }
    public int VikingId2 { get; set; }
    
    /// <summary>
    /// The ID of the viking who sent the friend request.
    /// </summary>
    public int InitiatorId { get; set; }
    
    public BuddyStatus Status { get; set; }
    public DateTime CreateDate { get; set; }
    
    public bool Viking1BestBuddy { get; set; }
    public bool Viking2BestBuddy { get; set; }

    [ForeignKey("VikingId1")]
    public virtual Viking Viking1 { get; set; } = null!;

    [ForeignKey("VikingId2")]
    public virtual Viking Viking2 { get; set; } = null!;
}

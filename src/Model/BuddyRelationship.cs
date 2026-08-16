using sodoff.Schema;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace sodoff.Model;

public class BuddyRelationship {
    public int VikingId { get; set; }
    public int BuddyId { get; set; }
    public BuddyStatus Status { get; set; }
    public DateTime CreateDate { get; set; }
    public bool BestBuddy { get; set; }

    [ForeignKey("VikingId")]
    public virtual Viking Viking { get; set; } = null!;

    [ForeignKey("BuddyId")]
    public virtual Viking Buddy { get; set; } = null!;
}

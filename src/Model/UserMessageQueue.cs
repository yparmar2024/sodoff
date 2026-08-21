using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace sodoff.Model;

public class UserMessageQueue
{
    [Key]
    public int Id { get; set; }

    public int VikingId { get; set; }
    public virtual Viking Viking { get; set; } = null!;

    public int MessageId { get; set; }
    public virtual Message Message { get; set; } = null!;

    public bool IsRead { get; set; }
    public bool IsDeleted { get; set; }
}

using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace sodoff.Model;

public class Message
{
    [Key]
    public int Id { get; set; }
    
    public string MessageTypeName { get; set; } = "System Message";
    public string MemberMessage { get; set; } = "";
    
    public DateTime CreateDate { get; set; } = DateTime.UtcNow;
}

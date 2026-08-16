using System.Xml.Serialization;

namespace sodoff.Schema;

public enum BuddyActionResultType {
    [XmlEnum("0")]
    Unknown = 0,
    [XmlEnum("1")]
    Success = 1,
    [XmlEnum("2")]
    BuddyListFull = 2,
    [XmlEnum("3")]
    FriendBuddyListFull = 3,
    [XmlEnum("4")]
    AlreadyInList = 4,
    [XmlEnum("5")]
    InvalidFriendCode = 5,
    [XmlEnum("6")]
    CannotAddSelf = 6
}

[XmlRoot(ElementName = "BuddyActionResult", Namespace = "")]
public class BuddyActionResult {
    [XmlElement(ElementName = "Result")]
    public BuddyActionResultType Result { get; set; }

    [XmlElement(ElementName = "Status")]
    public BuddyStatus Status { get; set; }

    [XmlElement(ElementName = "BuddyUserID")]
    public string? BuddyUserID { get; set; }
}

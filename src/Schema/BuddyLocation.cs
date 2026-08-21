using System;
using System.Xml.Serialization;

namespace sodoff.Schema;

[Serializable]
[XmlRoot(ElementName = "BuddyLocation", Namespace = "")]
public class BuddyLocation
{
    [XmlElement(ElementName = "UserID")]
    public string UserID;

    [XmlElement(ElementName = "Server")]
    public string Server;

    [XmlElement(ElementName = "Zone")]
    public string Zone;

    [XmlElement(ElementName = "Room")]
    public string Room;

    [XmlElement(ElementName = "MultiplayerID")]
    public int MultiplayerID;

    [XmlElement(ElementName = "ServerVersion")]
    public string ServerVersion;

    [XmlElement(ElementName = "AppName")]
    public string AppName;
}

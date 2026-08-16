using System.Xml.Serialization;

namespace sodoff.Schema;

[XmlRoot(Namespace = "http://api.jumpstart.com/", IsNullable = true)]
[Serializable]
public class ArrayOfCombinedListMessage {
    [XmlElement(ElementName = "CombinedListMessage")]
    public CombinedListMessage[] CombinedListMessage;
}

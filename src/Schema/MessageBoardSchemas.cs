using System;
using System.Xml.Serialization;

namespace sodoff.Schema;

public enum MessageLevel
{
    Canned = 1,
    WhiteList
}

public enum MessageListType
{
    MessageBoard = 1,
    UserStatus,
    News
}

public enum MessageType
{
    Chat = 1,
    Post,
    News
}

[Serializable]
public class Message
{
    public int? MessageID;
    public string Creator;
    public MessageLevel MessageLevel;
    public MessageType MessageType;
    public string Content;
    public int? ReplyToMessageID;
    public DateTime CreateTime;
    public DateTime? UpdateDate;
    public int ConversationID;
    public string DisplayAttribute;
    public bool isPrivate;
}

[Serializable]
[XmlRoot(ElementName = "MessageList", IsNullable = true, Namespace = "")]
public class MessageList
{
    public int? ID;
    public string UserID;
    public MessageListType Type;
    public int LastMessageID;
    public Message[] Messages;
}

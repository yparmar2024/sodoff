using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using sodoff.Model;
using sodoff.Schema;
using sodoff.Attributes;

namespace sodoff.Controllers.Common;

[ApiController]
public class MessagingController : ControllerBase
{
    [HttpGet, HttpPost]
    [Produces("application/xml")]
    [Route("MessagingWebService.asmx/GetUserMessageQueue")]
    [VikingSession]
    public IActionResult GetUserMessageQueue(Viking viking, [FromServices] DBContext ctx) {
        return Ok(new ArrayOfMessageInfo { MessageInfo = FetchMessages(viking, ctx).ToArray() });
    }

    [HttpPost]
    [Produces("application/xml")]
    [Route("MessagingWebService.asmx/SendMessage")]
    public IActionResult SendMessage() {
        // Disabled P2P messaging to avoid moderation.
        return Ok(false);
    }

    [HttpPost]
    [Produces("application/xml")]
    [Route("MessagingWebService.asmx/SaveMessage")]
    [VikingSession]
    public IActionResult SaveMessage([FromForm] int userMessageQueueID, [FromForm] bool isNew, [FromForm] bool isDeleted, Viking viking, [FromServices] DBContext ctx) {
        var state = ctx.UserMessageQueues.FirstOrDefault(q => q.Id == userMessageQueueID && q.VikingId == viking.Id);
        if (state != null) {
            state.IsRead = !isNew;
            state.IsDeleted = isDeleted;
            ctx.SaveChanges();
            return Ok(true);
        }
        
        // Return true anyway so the client UI doesn't hang for buddy requests (since they use fake hack IDs)
        return Ok(true);
    }

    [HttpPost]
    [Produces("application/xml")]
    [Route("MessageWebService.asmx/GetCombinedListMessage")]
    public IActionResult GetCombinedListMessage([FromForm] string? userId, [FromForm] string? filterName, [FromServices] DBContext ctx)
    {
        if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var parsedGuid)) {
            return Content("<?xml version=\"1.0\" encoding=\"utf-8\"?><ArrayOfCombinedListMessage />", "application/xml");
        }

        var viking = ctx.Vikings.FirstOrDefault(v => v.Uid == parsedGuid);
        if (viking == null) {
            return Content("<?xml version=\"1.0\" encoding=\"utf-8\"?><ArrayOfCombinedListMessage />", "application/xml");
        }
        var combined = new List<CombinedListMessage>();

        var userQueue = FetchMessages(viking, ctx);
        foreach (var msg in userQueue) {
            combined.Add(new CombinedListMessage {
                MessageType = 3, // USER_MESSAGE_QUEUE
                MessageDate = DateTime.UtcNow,
                MessageBody = SerializeToXml(msg)
            });
        }

        var dbMessages = ctx.Messages.ToList();
        foreach (var msg in dbMessages) {
            var boardMsg = new sodoff.Schema.Message {
                MessageID = msg.Id,
                Creator = "00000000-0000-0000-0000-000000000000",
                MessageLevel = sodoff.Schema.MessageLevel.WhiteList,
                MessageType = sodoff.Schema.MessageType.Post,
                Content = msg.MemberMessage,
                CreateTime = msg.CreateDate
            };
            combined.Add(new CombinedListMessage {
                MessageType = 2, // MESSAGE_BOARD
                MessageDate = msg.CreateDate,
                MessageBody = SerializeToXml(boardMsg)
            });
        }

        var wrapper = new ArrayOfCombinedListMessage { CombinedListMessage = combined.ToArray() };
        var xml = SerializeToXml(wrapper);
        xml = xml.Replace(" xmlns=\"http://api.jumpstart.com/\"", "");

        return Content(xml, "application/xml");
    }

    private string SerializeToXml<T>(T obj)
    {
        var serializer = new System.Xml.Serialization.XmlSerializer(typeof(T));
        using var stringWriter = new System.IO.StringWriter();
        var namespaces = new System.Xml.Serialization.XmlSerializerNamespaces();
        namespaces.Add("", "");
        serializer.Serialize(stringWriter, obj, namespaces);
        return stringWriter.ToString();
    }

    [HttpPost]
    [Produces("application/xml")]
    [Route("MessageWebService.asmx/GetMessageBoard")]
    public IActionResult GetMessageBoard([FromForm] string userId, [FromServices] DBContext ctx)
    {
        var dbMessages = ctx.Messages.ToList();
        var messageList = new List<sodoff.Schema.Message>();

        foreach (var msg in dbMessages) {
            messageList.Add(new sodoff.Schema.Message {
                MessageID = msg.Id,
                Creator = "00000000-0000-0000-0000-000000000000",
                MessageLevel = sodoff.Schema.MessageLevel.WhiteList,
                MessageType = sodoff.Schema.MessageType.Post,
                Content = msg.MemberMessage,
                CreateTime = msg.CreateDate
            });
        }

        return Ok(new sodoff.Schema.MessageList {
            Type = sodoff.Schema.MessageListType.MessageBoard,
            Messages = messageList.ToArray()
        });
    }

    private List<MessageInfo> FetchMessages(Viking viking, DBContext ctx) {
        var messages = new List<MessageInfo>();

        // 1. Buddy Requests
        var buddyReqs = ctx.BuddyRelationships
            .Include(b => b.Buddy)
            .Where(b => b.VikingId == viking.Id && b.Status == BuddyStatus.PendingApprovalFromSelf)
            .ToList();
            
        foreach (var req in buddyReqs) {
            messages.Add(new MessageInfo {
                MessageID = req.BuddyId, // hack: use BuddyId as MessageID so we know who it is
                UserMessageQueueID = req.BuddyId,
                FromUserID = req.Buddy.Uid.ToString(),
                MessageTypeID = 5,
                MessageTypeName = "Buddy Request",
                MemberMessage = "[[Line1]]=[[Wants to be your buddy!]]",
                NonMemberMessage = "[[Line1]]=[[Wants to be your buddy!]]"
            });
        }

        // 2. System Messages (Broadcast + Lazy Tracking)
        var globalMsgs = ctx.Messages.ToList();
        var userStates = ctx.UserMessageQueues.Where(q => q.VikingId == viking.Id).ToList();
        var stateDict = userStates.ToDictionary(q => q.MessageId);

        bool addedNewStates = false;
        foreach (var msg in globalMsgs) {
            if (!stateDict.TryGetValue(msg.Id, out var state)) {
                state = new UserMessageQueue {
                    VikingId = viking.Id,
                    MessageId = msg.Id,
                    IsRead = false,
                    IsDeleted = false
                };
                ctx.UserMessageQueues.Add(state);
                stateDict[msg.Id] = state;
                addedNewStates = true;
            }

            if (!state.IsDeleted) {
                messages.Add(new MessageInfo {
                    MessageID = msg.Id,
                    UserMessageQueueID = state.Id,
                    MessageTypeID = 1,
                    MessageTypeName = msg.MessageTypeName,
                    MemberMessage = msg.MemberMessage,
                    NonMemberMessage = msg.MemberMessage,
                    FromUserID = "00000000-0000-0000-0000-000000000000"
                });
            }
        }

        if (addedNewStates) {
            ctx.SaveChanges();
        }

        return messages;
    }
}

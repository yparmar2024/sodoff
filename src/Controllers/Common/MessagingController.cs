using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using sodoff.Attributes;
using sodoff.Model;
using sodoff.Schema;

namespace sodoff.Controllers.Common;
public class MessagingController : Controller {

    [HttpGet, HttpPost]
    [Produces("application/xml")]
    [Route("MessagingWebService.asmx/GetUserMessageQueue")]
    [VikingSession]
    public IActionResult GetUserMessageQueue(Viking viking, [FromServices] DBContext ctx) {
        var requests = ctx.BuddyRelationships
            .Include(b => b.Buddy)
            .Where(b => b.VikingId == viking.Id && b.Status == BuddyStatus.PendingApprovalFromSelf)
            .ToList();
            
        var messages = new List<MessageInfo>();
        foreach (var req in requests) {
            messages.Add(new MessageInfo {
                MessageID = req.BuddyId, // hack: use BuddyId (sender) as MessageID
                FromUserID = req.Buddy.Uid.ToString(),
                MessageTypeID = 5,
                MessageTypeName = "Buddy Request"
            });
        }
        
        return Ok(new ArrayOfMessageInfo { MessageInfo = messages.ToArray() });
    }

    [HttpPost]
    [Produces("application/xml")]
    [Route("MessagingWebService.asmx/SendMessage")]
    public IActionResult SendMessage() {
        // TODO: this is a placeholder
        return Ok(false);
    }

    [HttpPost]
    [Produces("application/xml")]
    [Route("MessagingWebService.asmx/SaveMessage")]
    public IActionResult SaveMessage() {
        // TODO: this is a placeholder
        return Ok(false);
    }

    [HttpPost]
    [Produces("application/xml")]
    [Route("MessageWebService.asmx/GetCombinedListMessage")]
    public IActionResult GetCombinedListMessage()
    {
        // TODO - placeholder
        return Ok(new ArrayOfMessageInfo());
    }

    [HttpPost]
    [Produces("application/xml")]
    [Route("MessageWebService.asmx/GetMessageBoard")]
    [VikingSession]
    public IActionResult GetMessageBoard(Viking viking, [FromServices] DBContext ctx)
    {
        // Mock friend requests from the BuddyRelationships table
        var requests = ctx.BuddyRelationships
            .Include(b => b.Buddy)
            .Where(b => b.VikingId == viking.Id && b.Status == BuddyStatus.PendingApprovalFromSelf)
            .ToList();
            
        var messages = new List<MessageInfo>();
        foreach (var req in requests) {
            messages.Add(new MessageInfo {
                MessageID = req.BuddyId, // hack: use BuddyId as MessageID so we know who it is
                FromUserID = req.Buddy.Uid.ToString(),
                MessageTypeID = 5,
                MessageTypeName = "Buddy Request"
            });
        }
        
        return Ok(new ArrayOfMessageInfo { MessageInfo = messages.ToArray() });
    }
}

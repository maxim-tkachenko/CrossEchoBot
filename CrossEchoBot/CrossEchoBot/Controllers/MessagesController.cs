using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using Microsoft.Bot.Connector;

namespace CrossEchoBot
{
    [BotAuthentication]
    public class MessagesController : ApiController
    {
        private const string BotShortName = "@cebot";
        private const string BotName = "@CrossEchoBot";
        private const string BotNameLowerCase = "@crossechobot";

        /// <summary>
        /// POST: api/Messages
        /// Receive a message from a user and reply to it
        /// </summary>
        public async Task<HttpResponseMessage> Post([FromBody]Activity activity)
        {
            Activity reply = null;

            try
            {
                if (activity.Type == ActivityTypes.Message)
                {
                    if (activity.Text.ToLower().Contains(BotNameLowerCase) ||
                        activity.Text.ToLower().Contains(BotShortName))
                    {
                        if (activity.Text.ToLower().Contains("/getchatid"))
                        {
                            Storage.Data.Add(new ChatTunnel(
                                new ChatIdentity(activity.Conversation.Id, activity.ServiceUrl),
                                null));

                            reply = activity.CreateReply(activity.Conversation.Id);
                        }
                        else if (activity.Text.ToLower().Contains("/pair"))
                        {
                            var text = activity.Text
                                .Replace(BotNameLowerCase, string.Empty)
                                .Replace(BotShortName, string.Empty)
                                .Replace(BotName, string.Empty)
                                .Replace("/pair", string.Empty)
                                .Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                            if (text.Length > 0)
                            {
                                var fromId = text[0];

                                // add check for url
                                var pair = Storage.Data.FirstOrDefault(x => x.From.Id == fromId);
                                if (pair != null)
                                {
                                    // add check if To is not empty (re-pair operation)
                                    pair.To = new ChatIdentity(activity.Conversation.Id, activity.ServiceUrl);
                                    reply = activity.CreateReply("Conversations successfully paired! :)");
                                }
                                else
                                {
                                    reply = activity.CreateReply("Sorry, I can't find a conversation with that id :(");
                                }
                            }
                        }
                        else if (activity.Text.ToLower().Contains("/skip"))
                        {
                            // nothing to do
                        }
                        else if (activity.Text.ToLower().Contains("/help"))
                        {
                            reply = activity.CreateReply(
                                "Hello friend! I'm a CrossEcho Bot or cebot. " +
                                "For communicating with me you need to mention me " + BotName + " and use one of the next commands: /getchatid, /pair, /skip, /delete, /help. " +
                                "I'll automatically resend any message from conversation which contains 'http' text. " +
                                "Also I'll resend any message if you mention me without additional commands.");
                        }
                        else if (activity.Text.ToLower().Contains("/delete"))
                        {
                            var count = RemoveChatTunnels(activity.Conversation.Id);
                            if (count > 0)
                                reply = activity.CreateReply(
                                    "This conversation was successfully disconnected from " + count + " conversation(s).");
                            else
                                reply = activity.CreateReply(
                                        "This conversation isn't connected to another conversations.");
                        }
                        else
                        {
                            // resend manually
                            var chat = FindChatTunnel(activity.Conversation.Id);
                            if (chat != null)
                                // remove mention from text
                                await Resend(
                                    activity.Recipient,
                                    chat,
                                    activity.Text
                                        .Replace(BotShortName, string.Empty)
                                        .Replace(BotName, string.Empty)
                                        .Replace(BotNameLowerCase, string.Empty));
                        }
                    }
                    else
                    {
                        if (activity.Text.ToLower().Contains("http"))
                        {
                            var chat = FindChatTunnel(activity.Conversation.Id);
                            if (chat != null)
                                await Resend(activity.Recipient, chat, activity.Text);
                        }
                    }
                }
                else
                {
                    HandleSystemMessage(activity);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                reply = activity.CreateReply(
                    "An error occured while processing your request :(. Please contact to developer.");
            }

            if (reply != null)
            {
                var connector = new ConnectorClient(new Uri(reply.ServiceUrl));
                await connector.Conversations.ReplyToActivityAsync(reply);
            }

            return Request.CreateResponse(HttpStatusCode.OK);
        }

        private async Task Resend(ChannelAccount recipient, ChatIdentity chat, string message)
        {
            var reply = new Activity
            {
                From = recipient,
                Recipient = new ChannelAccount(chat.Id),
                ServiceUrl = chat.ServiceUrl,
                // add /skip command to avoid recursion
                Text = message + " " + BotName + " /skip",
                Type = ActivityTypes.Message
            };

            var connector = new ConnectorClient(new Uri(reply.ServiceUrl));
            await connector.Conversations.SendToConversationAsync(reply, chat.Id);
        }

        private ChatIdentity FindChatTunnel(string chatId)
        {
            var tunnel = Storage.Data.FirstOrDefault(x => x.From.Id == chatId);
            if (tunnel == null)
            {
                tunnel = Storage.Data.FirstOrDefault(x => x.To.Id == chatId);
                if (tunnel != null)
                    return tunnel.From;
            }
            else
            {
                return tunnel.To;
            }

            return null;
        }

        private int RemoveChatTunnels(string chatId)
        {
            // handle case if one of properties is null
            return
                Storage.Data.RemoveAll(
                    x => x.From.Id == chatId ||
                    x.To.Id == chatId);
        }

        private Activity HandleSystemMessage(Activity activity)
        {
            if (activity.Type == ActivityTypes.DeleteUserData)
            {
                // Implement user deletion here
                // If we handle user deletion, return a real message
            }
            else if (activity.Type == ActivityTypes.ConversationUpdate)
            {
                // Handle conversation state changes, like members being added and removed
                // Use Activity.MembersAdded and Activity.MembersRemoved and Activity.Action for info
                // Not available in all channels
            }
            else if (activity.Type == ActivityTypes.ContactRelationUpdate)
            {
                // Handle add/remove from contact lists
                // Activity.From + Activity.Action represent what happened
            }
            else if (activity.Type == ActivityTypes.Typing)
            {
                // Handle knowing that the user is typing
            }
            else if (activity.Type == ActivityTypes.Ping)
            {
            }

            return null;
        }
    }
}

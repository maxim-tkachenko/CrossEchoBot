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
        private const string BotName = "@cebot";

        /// <summary>
        /// POST: api/Messages
        /// Receive a message from a user and reply to it
        /// </summary>
        public async Task<HttpResponseMessage> Post([FromBody]Activity activity)
        {
            try
            {
                if (activity.Type == ActivityTypes.Message)
                {
                    Activity reply = null;

                    if (activity.Text.ToLower().Contains(BotName))
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
                                "For communicating with me you need to mention me " + BotName + " and use one of the next commands: /getchatid, /pair, /skip, /help. " +
                                "I'll automatically resend any message from conversation which contains 'http' text. " +
                                "Also I'll resend any message if you mention me without additional commands.");
                        }
                        else
                        {
                            // resend manually
                            var chat = FindChatTunnel(activity.Conversation.Id);
                            if (chat != null)
                                // remove mention from text
                                await Resend(activity.Recipient, chat, activity.Text.Replace(BotName, string.Empty));
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

                    if (reply != null)
                    {
                        var connector = new ConnectorClient(new Uri(reply.ServiceUrl));
                        await connector.Conversations.ReplyToActivityAsync(reply);
                    }
                }
                else
                {
                    HandleSystemMessage(activity);
                }

                return Request.CreateResponse(HttpStatusCode.OK);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                return Request.CreateResponse(HttpStatusCode.InternalServerError);
            }
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

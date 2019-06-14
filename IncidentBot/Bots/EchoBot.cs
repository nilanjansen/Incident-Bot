// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
//
// Generated with Bot Builder V4 SDK Template for Visual Studio EchoBot v4.3.0

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using System.Linq;
using Microsoft.Bot.Builder.AI.QnA;

namespace EchoBot1.Bots
{
    public class EchoBot : ActivityHandler
    {
        public QnAMaker EchoBotQnA { get; private set; }
        public EchoBot(QnAMakerEndpoint endpoint)
        {
            // connects to QnA Maker endpoint for each turn
            EchoBotQnA = new QnAMaker(endpoint);
        }
        protected override async Task OnMessageActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            //await turnContext.SendActivityAsync(MessageFactory.Text($"Echo: {turnContext.Activity.Text}"), cancellationToken);
            // First send the user input to your QnA Maker knowledgebase
            await AccessQnAMaker(turnContext, cancellationToken);
        }

        protected override async Task OnMembersAddedAsync(IList<ChannelAccount> membersAdded, ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken)
        {
            foreach (var member in membersAdded)
            {
                if (member.Id != turnContext.Activity.Recipient.Id)
                {
                    await turnContext.SendActivityAsync(MessageFactory.Text($"Hello and Welcome!"), cancellationToken);
                }
            }
        }

        private async Task AccessQnAMaker(ITurnContext turnContext, CancellationToken cancellationToken)
        {
            var results = await EchoBotQnA.GetAnswersAsync(turnContext);
            if (results.Any())
            {
                var userInput = results.First().Answer;
                var reply = turnContext.Activity.CreateReply(results.First().Answer);
                if (userInput.Contains('<'))
                {
                    reply = turnContext.Activity.CreateReply(userInput.Substring(0, userInput.IndexOf('<')));
                    string[] suggestions = userInput.Substring(userInput.IndexOf('<') + 1, userInput.Length - userInput.IndexOf('<') - 2).Split(',');
                    reply.SuggestedActions = new SuggestedActions();
                    reply.SuggestedActions.Actions = new List<CardAction>();
                    foreach (var item in suggestions)
                    {
                        CardAction cAction = new CardAction
                        {
                            Title = item,
                            Type = ActionTypes.ImBack,
                            Value = item
                        };
                        reply.SuggestedActions.Actions.Add(cAction);
                    }
                }
                else
                {
                    reply = turnContext.Activity.CreateReply(userInput);
                }

                await turnContext.SendActivityAsync(reply, cancellationToken);

                //await turnContext.SendActivityAsync(MessageFactory.Text("QnA Maker Returned: " + results.First().Answer), cancellationToken);
            }
            else
            {
                await turnContext.SendActivityAsync(MessageFactory.Text("Sorry, could not find an answer in the Q and A system."), cancellationToken);
            }
        }
    }
}

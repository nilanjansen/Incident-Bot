// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
//
// Generated with Bot Builder V4 SDK Template for Visual Studio EchoBot v4.3.0

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Builder.Azure;
using System.Linq;
using Microsoft.Bot.Builder.AI.QnA;
using System;

namespace EchoBot1.Bots
{
    public class EchoBot : ActivityHandler
    {

        private const string CosmosServiceEndpoint = "https://deltacosmos.documents.azure.com:443/";
        private const string CosmosDBKey = "0L1ffafNohh46vJSvtA9aNh25EPHnfnngPqZyYt9NLAYDoiINa3F5BxldU2duYuawYfgs9L2ns3zPmV14NjeZQ==";
        private const string CosmosDBDatabaseName = "incident-bot-cosmos-db";
        private const string CosmosDBContainerName = "bot-storage";
        public QnAMaker EchoBotQnA { get; private set; }
        private static readonly CosmosDbStorage _myStorage = new CosmosDbStorage(new CosmosDbStorageOptions
        {
            AuthKey = CosmosDBKey,
            CollectionId = CosmosDBContainerName,
            CosmosDBEndpoint = new Uri(CosmosServiceEndpoint),
            DatabaseId = CosmosDBDatabaseName,
        });
        public EchoBot(QnAMakerEndpoint endpoint)
        {
            // connects to QnA Maker endpoint for each turn
            EchoBotQnA = new QnAMaker(endpoint);
        }
        public class UtteranceLog : IStoreItem
        {
            // A list of things that users have said to the bot
            public List<string> UtteranceList { get; } = new List<string>();

            // The number of conversational turns that have occurred        
            public int TurnNumber { get; set; } = 0;

            // Create concurrency control where this is used.
            public string ETag { get; set; } = "*";
        }
        protected override async Task OnMessageActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            //await turnContext.SendActivityAsync(MessageFactory.Text($"Echo: {turnContext.Activity.Text}"), cancellationToken);
            // preserve user input.
            var utterance = turnContext.Activity.Text;
            // make empty local logitems list.
            UtteranceLog logItems = null;
            // First send the user input to your QnA Maker knowledgebase
            try
            {
                string[] utteranceList = { "UtteranceLog" };
                logItems = _myStorage.ReadAsync<UtteranceLog>(utteranceList).Result?.FirstOrDefault().Value;
            }
            catch
            {
                // Inform the user an error occured.
                await turnContext.SendActivityAsync("Sorry, something went wrong reading your stored messages!");
            }
            if (logItems is null)
            {
                // add the current utterance to a new object.
                logItems = new UtteranceLog();
                logItems.UtteranceList.Add(utterance);
                // set initial turn counter to 1.
                logItems.TurnNumber++;

                
                // Create Dictionary object to hold received user messages.
                var changes = new Dictionary<string, object>();
                {
                    changes.Add("UtteranceLog", logItems);
                }
                try
                {
                    // Save the user message to your Storage.
                    await _myStorage.WriteAsync(changes, cancellationToken);
                }
                catch
                {
                    // Inform the user an error occured.
                    await turnContext.SendActivityAsync("Sorry, something went wrong storing your message!");
                }

            }
            else
            {
                // add new message to list of messages to display.
                logItems.UtteranceList.Add(utterance);
                // increment turn counter.
                logItems.TurnNumber++;

                

                // Create Dictionary object to hold new list of messages.
                var changes = new Dictionary<string, object>();
                {
                    changes.Add("UtteranceLog", logItems);
                };

                try
                {
                    // Save new list to your Storage.
                    await _myStorage.WriteAsync(changes, cancellationToken);
                }
                catch
                {
                    // Inform the user an error occured.
                    await turnContext.SendActivityAsync("Sorry, something went wrong storing your message!");
                }
            }

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

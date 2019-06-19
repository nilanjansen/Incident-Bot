// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using IncidentBot.Model;
using IncidentBot.ServiceReference;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Connector.Authentication;

namespace IncidentBot
{
    public class IncidentDialog : ComponentDialog
    {
        private readonly IStatePropertyAccessor<Incident> _incidentAccessor;

        public IncidentDialog(UserState userState)
            : base(nameof(IncidentDialog))
        {
            _incidentAccessor = userState.CreateProperty<Incident>("Incident");

            // This array defines how the Waterfall will execute.
            var waterfallSteps = new WaterfallStep[]
            {
                LocationStepAsync,
                ProblemStepAsync,
                AttachmentStepAsync,
                ContactStepAsync,
                SummaryStepAsync,
            };

            // Add named dialogs to the DialogSet. These names are saved in the dialog state.
            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), waterfallSteps));
            AddDialog(new TextPrompt(nameof(TextPrompt)));
            AddDialog(new ChoicePrompt(nameof(ChoicePrompt)));
            AddDialog(new AttachmentPrompt(nameof(AttachmentPrompt)));

            // The initial child Dialog to run.
            InitialDialogId = nameof(WaterfallDialog);
        }

        private static async Task<DialogTurnResult> LocationStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            // WaterfallStep always finishes with the end of the Waterfall or with another dialog; here it is a Prompt Dialog.
            // Running a prompt here means the next WaterfallStep will be run when the users response is received.
            return await stepContext.PromptAsync(nameof(ChoicePrompt),
                new PromptOptions
                {
                    Prompt = MessageFactory.Text("Please select your location"),
                    Choices = ChoiceFactory.ToChoices(new List<string> { "Thiruvanmiyur", "Navalur", "Sholinganallur" }),

                }, cancellationToken);
        }

        private static async Task<DialogTurnResult> ProblemStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            stepContext.Values["location"] = ((FoundChoice)stepContext.Result).Value;
            // WaterfallStep always finishes with the end of the Waterfall or with another dialog; here it is a Prompt Dialog.
            // Running a prompt here means the next WaterfallStep will be run when the users response is received.
            return await stepContext.PromptAsync(nameof(ChoicePrompt),
                new PromptOptions
                {
                    Prompt = MessageFactory.Text("Please select the problem you want to report"),
                    Choices = ChoiceFactory.ToChoices(new List<string> { "Road", "Drinage", "Street Light", "Foot Path" }),

                }, cancellationToken);
        }

        private static async Task<DialogTurnResult> AttachmentStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            stepContext.Values["problem"] = ((FoundChoice)stepContext.Result).Value;


            return await stepContext.PromptAsync(nameof(AttachmentPrompt), new PromptOptions { Prompt = MessageFactory.Text("Please upload image") }, cancellationToken);
        }

        private static async Task<DialogTurnResult> ContactStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var web = new WebClient();
            var apiKey = await new MicrosoftAppCredentials("434106c0-774c-4b6f-a29c-fe4c28d5ef6c", "{v&VD4A8kea.P9$+^HW2%KfG9#").GetTokenAsync();
            web.Headers[HttpRequestHeader.Authorization] = "Bearer " + apiKey;
            byte[] image = web.DownloadData(((List<Microsoft.Bot.Schema.Attachment>)stepContext.Result)[0].ContentUrl);
            stepContext.Values["attachment"] = image;

            return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = MessageFactory.Text("Please enter your contact number") }, cancellationToken);
        }

        private async Task<DialogTurnResult> SummaryStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            // Get the current profile object from user state.
            var incident = await _incidentAccessor.GetAsync(stepContext.Context, () => new Incident(), cancellationToken);
            string msg;
            Random random = new Random();

            incident.IncidentId = random.Next(1000, 9999);
            incident.Location = (string)stepContext.Values["location"];
            incident.IssueType = (string)stepContext.Values["problem"];
            incident.Media = (byte[])stepContext.Values["attachment"];
            incident.CreatorContact = (string)stepContext.Result;
            var result = PostDataToAPI.AddIncident(incident);
            if (!result.IsSuccessStatusCode)
            {
                msg = $"Please try again later";
            }
            else
            {
                msg = $"Thank you. Your incident number is INC{incident.IncidentId}. You will be contacted soon";
            }



            await stepContext.Context.SendActivityAsync(MessageFactory.Text(msg), cancellationToken);

            // WaterfallStep always finishes with the end of the Waterfall or with another dialog, here it is the end.
            return await stepContext.EndDialogAsync(cancellationToken: cancellationToken);
        }
    }
}


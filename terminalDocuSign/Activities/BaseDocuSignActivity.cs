﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Fr8.Infrastructure.Data.Control;
using Fr8.Infrastructure.Data.Crates;
using Fr8.Infrastructure.Data.DataTransferObjects;
using Fr8.Infrastructure.Data.Managers;
using Fr8.Infrastructure.Data.Manifests;
using Fr8.Infrastructure.Data.States;
using Fr8.TerminalBase.BaseClasses;
using Fr8.TerminalBase.Errors;
using terminalDocuSign.Services.New_Api;

namespace terminalDocuSign.Activities
{
    public abstract class BaseDocuSignActivity : ExplicitTerminalActivity
    {
        protected IDocuSignManager DocuSignManager;


        protected BaseDocuSignActivity(ICrateManager crateManager, IDocuSignManager docuSignManager)
            : base(crateManager)
        {
            DocuSignManager = docuSignManager;
        }

        public override async Task Initialize()
        {
            await Configure(InitializeDS);
        }

        public override async Task FollowUp()
        {
            await Configure(FollowUpDS);
        }

        public async Task Configure(Func<Task> configFunc)
        {
            try
            {
                await configFunc();
            }
            catch (Exception ex)
            {
                if (!string.IsNullOrEmpty(ex.Message) && ex.Message.Contains("AUTHORIZATION_INVALID_TOKEN"))
                {
                    AddAuthenticationCrate(true);
                    return;
                }

                throw;
            }
        }

        protected List<KeyValueDTO> CreateDocuSignEventValues(DocuSignEnvelopeCM_v2 envelope, string label = null)
        {
            string curRecipientEmail = "";
            string curRecipientUserName = "";

            if (envelope != null)
            {
                var current_recipient = envelope.GetCurrentRecipient();
                curRecipientEmail = current_recipient.Email;
                curRecipientUserName = current_recipient.Name;
            }

            return new List<KeyValueDTO>{
                new KeyValueDTO("CurrentRecipientEmail", curRecipientEmail) { Tags = "EmailAddress"},
                new KeyValueDTO("CurrentRecipientUserName", curRecipientUserName) { Tags = "UserName" },
                new KeyValueDTO("Status", envelope?.Status),
                new KeyValueDTO("CreateDate",  envelope?.CreateDate?.ToString()) { Tags = "Date" },
                new KeyValueDTO("SentDate", envelope?.SentDate?.ToString()) { Tags = "Date" },
                new KeyValueDTO("Subject", envelope?.Subject),
                new KeyValueDTO("EnvelopeId", envelope?.EnvelopeId),
            };
        }

        protected List<FieldDTO> CreateDocuSignEventFieldsDefinitions(string label = null)
        {
            return new List<FieldDTO>{
                new FieldDTO("CurrentRecipientEmail", AvailabilityType.RunTime) { Tags = "EmailAddress",SourceCrateLabel = label },
                new FieldDTO("CurrentRecipientUserName",  AvailabilityType.RunTime) { Tags = "UserName", SourceCrateLabel = label },
                new FieldDTO("Status", AvailabilityType.RunTime) { SourceCrateLabel = label},
                new FieldDTO("CreateDate") { Tags = "Date",SourceCrateLabel = label },
                new FieldDTO("SentDate", AvailabilityType.RunTime) { Tags = "Date", SourceCrateLabel = label },
                new FieldDTO("Subject", AvailabilityType.RunTime) { SourceCrateLabel = label},
                new FieldDTO("EnvelopeId",  AvailabilityType.RunTime) { SourceCrateLabel = label},
            };
        }
        
        public static DropDownList CreateDocuSignTemplatePicker(bool addOnChangeEvent,
            string name = "Selected_DocuSign_Template",
            string label = "Select DocuSign Template")
        {
            var control = new DropDownList()
            {
                Label = label,
                Name = name,
                Required = true,
                Source = null
            };

            if (addOnChangeEvent)
            {
                control.Events = new List<ControlEvent>()
                {
                    new ControlEvent("onChange", "requestConfig")
                };
            }

            return control;
        }

        public IEnumerable<FieldDTO> GetTemplateUserDefinedFields(string templateId, string envelopeId = null)
        {
            if (String.IsNullOrEmpty(templateId))
            {
                throw new ArgumentNullException(nameof(templateId));
            }
            var conf = DocuSignManager.SetUp(AuthorizationToken);
            return DocuSignManager.GetTemplateRecipientsAndTabs(conf, templateId).Select(x => new FieldDTO(x.Key) {Tags =  x.Tags});
        }

        public IEnumerable<KeyValueDTO> GetEnvelopeData(string templateId, string envelopeId = null)
        {
            if (String.IsNullOrEmpty(templateId))
            {
                throw new ArgumentNullException(nameof(templateId));
            }
            var conf = DocuSignManager.SetUp(AuthorizationToken);
            return DocuSignManager.GetEnvelopeRecipientsAndTabs(conf, templateId);
        }

        public void AddOrUpdateUserDefinedFields(string templateId, string envelopeId = null, List<KeyValueDTO> allFields = null)
        {
            Storage.RemoveByLabel("DocuSignTemplateUserDefinedFields");
            if (!String.IsNullOrEmpty(templateId))
            {
                var conf = DocuSignManager.SetUp(AuthorizationToken);
                var userDefinedFields = DocuSignManager.GetTemplateRecipientsAndTabs(conf, templateId);
                if (allFields != null)
                {
                    allFields.AddRange(userDefinedFields);
                }
                Storage.Add(CrateManager.CreateDesignTimeFieldsCrate("DocuSignTemplateUserDefinedFields",  userDefinedFields.ToArray()));
            }
        }

        public void FillDocuSignTemplateSource(Crate configurationCrate, string controlName)
        {
            var configurationControl = configurationCrate.Get<StandardConfigurationControlsCM>();
            var control = configurationControl.FindByNameNested<DropDownList>(controlName);
            if (control != null)
            {
                var conf = DocuSignManager.SetUp(AuthorizationToken);
                var templates = DocuSignManager.GetTemplatesList(conf);
                control.ListItems = templates.Select(x => new ListItem() { Key = x.Key, Value = x.Value }).ToList();
            }
            }

        public override async Task Run()
            {
            try
            {
                await RunDS();
                Success();
            }
            catch (AuthorizationTokenExpiredOrInvalidException ex)
            {
                RaiseInvalidTokenError(ex.Message);
            }
            catch (DocuSign.eSign.Client.ApiException ex)
            {
                if (ex.ErrorCode == 401)
                {
                    RaiseInvalidTokenError();
                }
                else
                {
                    throw;
                }
            }
        }

        protected abstract string ActivityUserFriendlyName { get; }

        protected abstract Task RunDS();
        protected abstract Task InitializeDS();
        protected abstract Task FollowUpDS();
    }
}
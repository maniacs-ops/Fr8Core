﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fr8Data.Control;
using Fr8Data.Crates;
using Fr8Data.DataTransferObjects;
using Fr8Data.Manifests;
using Fr8Data.States;
using PhoneNumbers;
using StructureMap;
using terminalTwilio.Services;
using TerminalBase.BaseClasses;
using TerminalBase.Infrastructure;
using Twilio;

namespace terminalTwilio.Activities
{
    public class Send_Via_Twilio_v1 : BaseTerminalActivity
    {
        public static ActivityTemplateDTO ActivityTemplateDTO = new ActivityTemplateDTO
        {
            Name = "Send_Via_Twilio",
            Label = "Send SMS",
            Tags = "Twillio,Notifier",
            Category = ActivityCategory.Forwarders,
            Version = "1",
            MinPaneWidth = 330,
            Terminal = TerminalData.TerminalDTO,
            WebService = TerminalData.WebServiceDTO
        };
        protected override ActivityTemplateDTO MyTemplate => ActivityTemplateDTO;


        protected ITwilioService _twilio;

        public Send_Via_Twilio_v1() : base(false)
        {
            _twilio = ObjectFactory.GetInstance<ITwilioService>();
        }

        public override async Task Initialize()
        {
            Storage.Clear();
            Storage.Add(PackCrate_ConfigurationControls());
            Storage.Add(await CreateAvailableFieldsCrate());
        }

        private async Task<Crate> CreateAvailableFieldsCrate()
        {
            var curUpstreamFields = await HubCommunicator.GetDesignTimeFieldsByDirection(ActivityId, CrateDirection.Upstream, AvailabilityType.RunTime, CurrentUserId) ?? new FieldDescriptionsCM();

            var availableFieldsCrate = CrateManager.CreateDesignTimeFieldsCrate(
                    "Upstream Terminal-Provided Fields",
                    curUpstreamFields.Fields,
                    AvailabilityType.Configuration);

            return availableFieldsCrate;
        }

        private Crate PackCrate_ConfigurationControls()
        {
            var fieldsDTO = new List<ControlDefinitionDTO>()
            {
                ControlHelper.CreateSpecificOrUpstreamValueChooser("SMS Number", "SMS_Number", "Upstream Terminal-Provided Fields", "", true),
                ControlHelper.CreateSpecificOrUpstreamValueChooser("SMS Body", "SMS_Body", "Upstream Terminal-Provided Fields", "", true)
            };

            return CrateManager.CreateStandardConfigurationControlsCrate("Configuration_Controls", fieldsDTO.ToArray());
        }

        private List<FieldDTO> GetRegisteredSenderNumbersData()
        {
            return _twilio.GetRegisteredSenderNumbers().Select(number => new FieldDTO() { Key = number, Value = number }).ToList();
        }

        public override async Task FollowUp()
        {
            Storage.RemoveByLabel("Upstream Terminal-Provided Fields");
            Storage.Add(await CreateAvailableFieldsCrate());
            }

        public override async Task Run()
        {
            Message curMessage;
            if (ConfigurationControls == null)
            {
                PackCrate_WarningMessage("No StandardConfigurationControlsCM crate provided", "No Controls");
                RaiseError("No StandardConfigurationControlsCM crate provided");
                return;
            }
            try
            {
                FieldDTO smsFieldDTO = ParseSMSNumberAndMsg();
                string smsNumber = smsFieldDTO.Key;
                string smsBody = smsFieldDTO.Value + "\nThis message was generated by Fr8. http://www.fr8.co";

                try
                {
                    curMessage = _twilio.SendSms(smsNumber, smsBody);
                    SendEventReport($"Twilio SMS Sent -> SMSBody: {smsBody} smsNumber: {smsNumber}");
                    var curFieldDTOList = CreateKeyValuePairList(curMessage);
                    Payload.Add(PackCrate_TwilioMessageDetails(curFieldDTOList));
                }
                catch (Exception ex)
                {
                    SendEventReport($"TwilioSMSSendFailure -> SMSBody: {smsBody} smsNumber: {smsNumber}, Exception {ex.Message}");
                    PackCrate_WarningMessage(ex.Message, "Twilio Service Failure");
                    RaiseError("Twilio Service Failure");
                    return;
                }
            }
            catch (ArgumentException appEx)
            {
                PackCrate_WarningMessage(appEx.Message, "SMS Number");
                RaiseError(appEx.Message);
                return;
            }

            Success();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="crateDTO"></param>
        /// <returns>Key = SMS Number; Value = SMS Body</returns>
        private FieldDTO ParseSMSNumberAndMsg()
        {
            var smsNumber = GetSMSNumber((TextSource)ConfigurationControls.Controls[0], Payload);
            var smsBody = GetSMSBody((TextSource)ConfigurationControls.Controls[1], Payload);
            return new FieldDTO(smsNumber, smsBody);
        }

        protected override Task<bool> Validate()
        {
            ValidationManager.Reset();
            if (ConfigurationControls != null)
            {
                var numberControl = (TextSource)ConfigurationControls.Controls[0];
                var bodyControl = (TextSource)ConfigurationControls.Controls[1];

                if (numberControl != null)
                {
                    if (numberControl.HasValue)
                    {
                        if (numberControl.HasSpecificValue)
                        {
                            ValidationManager.ValidatePhoneNumber(GeneralisePhoneNumber(numberControl.TextValue), numberControl);
                        }
                    }
                    else ValidationManager.SetError("No SMS Number Provided", numberControl);
                }
                if (bodyControl != null)
                {
                    if (!bodyControl.HasValue)
                    {
                        ValidationManager.SetError("SMS body can not be null.", bodyControl);
                    }
                }
            }
            return Task.FromResult(true);
        }


        private string GetSMSNumber(TextSource control, ICrateStorage payloadCrates)
        {
            string smsNumber = "";
            if (control == null)
            {
                throw new ApplicationException("TextSource control was expected but not found.");
            }
            smsNumber = control.GetValue(payloadCrates).Trim();

            smsNumber = GeneralisePhoneNumber(smsNumber);

            return smsNumber;
        }

        private string GetSMSBody(TextSource control, ICrateStorage payloadCrates)
        {
            string smsBody = "";
            if (control == null)
            {
                throw new ApplicationException("TextSource control was expected but not found.");
            }

            smsBody = control.GetValue(payloadCrates);
            if (smsBody == null)
            {
                throw new ArgumentException("SMS body can not be null.");
            }


            return smsBody;
        }

        private List<FieldDTO> CreateKeyValuePairList(Message curMessage)
        {
            List<FieldDTO> returnList = new List<FieldDTO>();
            returnList.Add(new FieldDTO("Status", curMessage.Status));
            returnList.Add(new FieldDTO("ErrorMessage", curMessage.ErrorMessage));
            returnList.Add(new FieldDTO("Body", curMessage.Body));
            returnList.Add(new FieldDTO("ToNumber", curMessage.To));
            return returnList;
        }
        private Crate PackCrate_TwilioMessageDetails(List<FieldDTO> curTwilioMessage)
        {
            return Fr8Data.Crates.Crate.FromContent("Message Data", new StandardPayloadDataCM(curTwilioMessage));
        }

        private void PackCrate_WarningMessage(string warningMessage, string warningLabel)
            {
            var textBlock = ControlHelper.GenerateTextBlock(warningLabel, warningMessage, "alert alert-warning");
            Storage.Clear();
            Storage.Add(PackControlsCrate(textBlock));
        }

        private string GeneralisePhoneNumber(string smsNumber)
        {
            PhoneNumberUtil phoneUtil = PhoneNumberUtil.GetInstance();
            smsNumber = new string(smsNumber.Where(s => char.IsDigit(s) || s == '+' || (phoneUtil.IsAlphaNumber(smsNumber) && char.IsLetter(s))).ToArray());
            if (smsNumber.Length == 10 && !smsNumber.Contains("+"))
                smsNumber = "+1" + smsNumber; //we assume that default region is USA
            return smsNumber;
        }
    }
}
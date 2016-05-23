﻿using System;
using System.Threading.Tasks;
using Data.Entities;
using Fr8Data.Crates;
using Salesforce.Common;
using TerminalBase.Models;
using Fr8Data.DataTransferObjects;

namespace terminalSalesforceTests.Fixtures
{
    public class FixtureData
    {
        public static async Task<AuthorizationToken>  Salesforce_AuthToken()
        {
            var auth = new AuthenticationClient();
            await auth.UsernamePasswordAsync(
                "3MVG9KI2HHAq33RzZO3sQ8KU8JPwmpiZBpe_fka3XktlR5qbCWstH3vbAG.kLmaldx8L1V9OhqoAYUedWAO_e",
                "611998545425677937",
                "alex@dockyard.company",
                "thales@123");

            return new AuthorizationToken()
            {
                Token = auth.AccessToken,
                AdditionalAttributes = $"refresh_token=;instance_url={auth.InstanceUrl};api_version={auth.ApiVersion}"
            };
        }

        public static ActivityTemplateDTO GetDataActivityTemplateDTO()
        {
            return new ActivityTemplateDTO
            {
                Version = "1",
                Name = "Get_Data",
                Label = "Get Data from Salesforce.com",
                NeedsAuthentication = true
            };
        }

        public static ActivityTemplateDTO SaveToSalesforceActivityTemplateDTO()
        {
            return new ActivityTemplateDTO
            {
                Version = "1",
                Name = "Save_To_SalesforceDotCom",
                Label = "Save To Salesforce.Com",
                NeedsAuthentication = true
            };
        }

        public static ActivityTemplateDO PostToChatterActivityTemplateDO()
        {
            return new ActivityTemplateDO
            {
                Version = "1",
                Name = "Post_To_Chatter",
                Label = "Post To Chatter",
                NeedsAuthentication = true
            };
        }

        public static async Task<ActivityContext> GetFileListTestActivityContext1()
        {
            var actionTemplate = GetDataActivityTemplateDTO();

            var activityPayload = new ActivityPayload()
            {
                Id = new Guid("8339DC87-F011-4FB1-B47C-FEC406E4100A"),
                ActivityTemplate = actionTemplate,
                CrateStorage = new CrateStorage()
            };
            var activityContext = new ActivityContext()
            {
                ActivityPayload = activityPayload,
                AuthorizationToken = await FixtureData.Salesforce_AuthToken()
            };
            return activityContext;
        }
        public static async Task<ActivityContext> GetFileListTestActivityContext2()
        {
            var actionTemplate = GetDataActivityTemplateDTO();

            var activityPayload = new ActivityPayload()
            {
                Id = new Guid("8339DC87-F011-4FB1-B47C-FEC406E4100A"),
                ActivityTemplate = actionTemplate,
                CrateStorage = new CrateStorage()
            };
            var activityContext = new ActivityContext()
            {
                ActivityPayload = activityPayload,
                AuthorizationToken = await FixtureData.Salesforce_AuthToken()
            };
            return activityContext;
        }

        public static ActivityContext SaveToSalesforceTestActivityContext1()
        {
            var actionTemplate = SaveToSalesforceActivityTemplateDTO();

            var activityPayload = new ActivityPayload()
            {
                Id = new Guid("8339DC87-F011-4FB1-B47C-FEC406E4100A"),
                ActivityTemplate = actionTemplate,
                CrateStorage = new CrateStorage(),
            };
            var activityContext = new ActivityContext()
            {
                ActivityPayload = activityPayload
            };

            return activityContext;
        }

        public static async Task<ActivityContext> SaveToSalesforceTestActivityDO1()
        {
            var activityTemplate = SaveToSalesforceActivityTemplateDTO();
            return new ActivityContext
            {
                AuthorizationToken = await Salesforce_AuthToken(),
                ActivityPayload = new ActivityPayload
                {
                    CrateStorage = new CrateStorage(),
                    Id = new Guid("8339DC87-F011-4FB1-B47C-FEC406E4100A"),
                    ActivityTemplate = activityTemplate
                },
            };
        }

        public static ActivityDO PostToChatterTestActivityDO1()
        {
            var actionTemplate = PostToChatterActivityTemplateDO();

            var activityDO = new ActivityDO()
            {
                Id = new Guid("8339DC87-F011-4FB1-B47C-FEC406E4100A"),
                ActivityTemplateId = actionTemplate.Id,
                ActivityTemplate = actionTemplate,
                CrateStorage = "",

            };
            return activityDO;
        }
    }
}

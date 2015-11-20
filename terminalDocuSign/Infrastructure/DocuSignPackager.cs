﻿using System;
using DocuSign.Integrations.Client;
using Utilities.Configuration.Azure;

namespace terminalDocuSign.Infrastructure
{
    public class DocuSignPackager
    {
        public string CurrentEmail; //these are used to populate the Login object in the DocuSign Library
        public string CurrentApiPassword;

        public DocuSignPackager()
        {
            ConfigureDocuSignIntegration();
        }

        public DocuSignAccount Login()
        {
           var curDocuSignAccount = new DocuSignAccount
            {					
                Email = CurrentEmail,
                ApiPassword = CurrentApiPassword,
					 BaseUrl = RestSettings.Instance.WebServiceUrl,
            };

            if (curDocuSignAccount.Login())
                return curDocuSignAccount;

            throw new InvalidOperationException(
                "Cannot log in to DocuSign. Please check the authentication information on web.config.");
        }

        public DocuSignAccount Login(string email, string apiPassword)
        {
            var curDocuSignAccount = new DocuSignAccount
            {
                Email = email,
                ApiPassword = apiPassword,
                BaseUrl = RestSettings.Instance.WebServiceUrl,
            };

            if (curDocuSignAccount.Login())
                return curDocuSignAccount;

            throw new InvalidOperationException(
                "Cannot log in to DocuSign. " + (curDocuSignAccount.RestError != null ? curDocuSignAccount.RestError.Serialize() : ""));
        }
        
        private void ConfigureDocuSignIntegration()
        {
            RestSettings.Instance.DistributorCode = CloudConfigurationManager.GetSetting("DocuSignDistributorCode");
            RestSettings.Instance.DistributorPassword =
                CloudConfigurationManager.GetSetting("DocuSignDistributorPassword");
            RestSettings.Instance.IntegratorKey = CloudConfigurationManager.GetSetting("DocuSignIntegratorKey");
            RestSettings.Instance.DocuSignAddress = CloudConfigurationManager.GetSetting("environment");
            RestSettings.Instance.WebServiceUrl = RestSettings.Instance.DocuSignAddress + "/restapi/v2";

            CurrentEmail = CloudConfigurationManager.GetSetting("DocuSignLoginEmail") ?? "Not Found";
            CurrentApiPassword = CloudConfigurationManager.GetSetting("DocuSignLoginPassword") ?? "Not Found";
        }
    }
}
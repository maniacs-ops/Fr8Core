﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Xml;
using Data.Entities;
using Data.Interfaces.DataTransferObjects;
using DevDefined.OAuth.Consumer;
using DevDefined.OAuth.Framework;
using DevDefined.OAuth.Storage.Basic;
using Hub.Managers.APIManagers.Transmitters.Restful;
using StructureMap;
using terminalQuickBooks.Interfaces;
using Utilities.Configuration.Azure;

namespace terminalQuickBooks.Infrastructure
{
    public class Authenticator : IAuthenticator
    {
        public const string TokenSeparator = ";;;;;;;";

        public static readonly string AccessToken =
          CloudConfigurationManager.GetSetting("QuickBooksRequestTokenUrl").ToString(CultureInfo.InvariantCulture);

        public static readonly ConcurrentDictionary<string, string> TokenSecrets =
           new ConcurrentDictionary<string, string>();

        public static readonly string ConsumerKey =
            CloudConfigurationManager.GetSetting("QuickBooksConsumerKey").ToString(CultureInfo.InvariantCulture);

        public static readonly string ConsumerSecret =
           CloudConfigurationManager.GetSetting("QuickBooksConsumerSecret").ToString(CultureInfo.InvariantCulture);

        /// <summary>
        /// Build external QuickBooks OAuth url.
        /// </summary>
        public string CreateAuthUrl()
        {
            var oauthSession = CreateSession();
            var requestToken = oauthSession.GetRequestToken();
            TokenSecrets.TryAdd(requestToken.Token, requestToken.TokenSecret);
            return oauthSession.GetUserAuthorizationUrlForToken(requestToken);
        }

        /// <summary>
        /// Create a session.
        /// </summary>
        /// <returns></returns>
        private IOAuthSession CreateSession()
        {
            var consumerContext = new OAuthConsumerContext
            {
                ConsumerKey = ConsumerKey,
                ConsumerSecret = ConsumerSecret,
                SignatureMethod = SignatureMethod.HmacSha1
            };
            return new OAuthSession(
                consumerContext,
                AccessToken,
                CloudConfigurationManager.GetSetting("QuickBooksOAuthAuthorizeUrl")
                    .ToString(CultureInfo.InvariantCulture),
                CloudConfigurationManager.GetSetting("QuickBooksOAuthAccessUrl").ToString(CultureInfo.InvariantCulture));
        }

        public async Task<AuthorizationTokenDTO> GetAuthToken(string oauthToken, string oauthVerifier, string realmId)
        {
            var oauthSession = CreateSession();
            string tokenSecret;
            TokenSecrets.TryRemove(oauthToken, out tokenSecret);

            IToken reqToken = new RequestToken
            {
                Token = oauthToken,
                TokenSecret = tokenSecret,
                ConsumerKey =
                    CloudConfigurationManager.GetSetting("QuickBooksConsumerKey").ToString(CultureInfo.InvariantCulture)
            };
            var accToken = oauthSession.ExchangeRequestTokenForAccessToken(reqToken, oauthVerifier);

            return new AuthorizationTokenDTO()
            {
                Token = accToken.Token + TokenSeparator + accToken.TokenSecret + TokenSeparator + realmId,
                ExternalAccountId = realmId,
                ExternalStateToken = null
            };
        }

        public Task<AuthorizationTokenDO> RefreshAuthToken(AuthorizationTokenDO authorizationToken)
        {
            var tokenSecret = authorizationToken.Token.Split(new[] {TokenSeparator}, StringSplitOptions.RemoveEmptyEntries)[1];
           
            var reconnectResult = ReconnectRealm(ConsumerKey, ConsumerSecret, AccessToken, tokenSecret);
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(reconnectResult);

            string xpath = "ReconnectResponse";
            var root = xmlDoc.SelectSingleNode(xpath);
            var newToken = root.SelectSingleNode("//OAuthToken");
            var newTokenSecret = root.SelectSingleNode("//OAuthTokenSecret")?.Value;
            var authToken = new AuthorizationTokenDO
            {
                Token = newTokenSecret
            };

            return Task.FromResult(authToken);
        }

        private string ReconnectRealm(string consumerKey, string consumerSecret, string accessToken, string accessTokenSecret)
        {
            HttpWebRequest httpWebRequest = WebRequest.Create("https://appcenter.intuit.com/api/v1/Connection/Reconnect") as HttpWebRequest;
            httpWebRequest.Method = "GET";
            httpWebRequest.Headers.Add("Authorization", GetDevDefinedOAuthHeader(httpWebRequest, consumerKey, consumerSecret, accessToken, accessTokenSecret));
            UTF8Encoding encoding = new UTF8Encoding();
            HttpWebResponse httpWebResponse = httpWebRequest.GetResponse() as HttpWebResponse;
            using (Stream data = httpWebResponse.GetResponseStream())
            {
                //return XML response
                return new StreamReader(data).ReadToEnd();
            }
        }

        private string GetDevDefinedOAuthHeader(HttpWebRequest webRequest, string consumerKey, string consumerSecret, string accessToken, string accessTokenSecret)
        {

            OAuthConsumerContext consumerContext = new OAuthConsumerContext
            {
                ConsumerKey = consumerKey,
                ConsumerSecret = consumerSecret,
                SignatureMethod = SignatureMethod.HmacSha1,
                UseHeaderForOAuthParameters = true
            };

            consumerContext.UseHeaderForOAuthParameters = true;

            //URIs not used - we already have Oauth tokens
            OAuthSession oSession = new OAuthSession(consumerContext, "https://www.example.com",
                                    "https://www.example.com",
                                    "https://www.example.com");


            oSession.AccessToken = new TokenBase
            {
                Token = accessToken,
                ConsumerKey = consumerKey,
                TokenSecret = accessTokenSecret
            };

            IConsumerRequest consumerRequest = oSession.Request();
            consumerRequest = ConsumerRequestExtensions.ForMethod(consumerRequest, webRequest.Method);
            consumerRequest = ConsumerRequestExtensions.ForUri(consumerRequest, webRequest.RequestUri);
            consumerRequest = consumerRequest.SignWithToken();
            return consumerRequest.Context.GenerateOAuthParametersForHeader();
        }
    }
}
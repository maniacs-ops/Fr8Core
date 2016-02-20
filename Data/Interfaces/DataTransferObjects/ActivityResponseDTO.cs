﻿using System.Web.Caching;
using Data.Constants;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Data.Interfaces.DataTransferObjects.Helpers;
using Data.Control;

namespace Data.Interfaces.DataTransferObjects
{
    /// <summary>
    /// Data structure intended to enable Fr8 services to return useful data as part of the http response
    /// </summary>
    public class ActivityResponseDTO
    {
        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("body")]
        public string Body { get; set; }

        public static ActivityResponseDTO Create(ActivityResponse activityResponseType)
        {
            return new ActivityResponseDTO()
            {
                Type = activityResponseType.ToString()
            };
        }

        public static ActivityResponseDTO CreateDocumentationResponse(string displayMechanism, string contentPath = "")
        {
            return Create(ActivityResponse.Null)
                .AddDocumentationDTO(new DocumentationDTO(displayMechanism, contentPath));
        }
    }
}

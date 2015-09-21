﻿using Core.Interfaces;
using Data.Interfaces.DataTransferObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Data.Interfaces.ManifestSchemas;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Utilities;
using JsonSerializer = Utilities.Serializers.Json.JsonSerializer;

namespace Core.Services
{
    public class Crate : ICrate
    {
        private readonly JsonSerializer _serializer;
        public Crate()
        {
            _serializer = new JsonSerializer();
        }

        public CrateDTO Create(string label, string contents, string manifestType = "", int manifestId = 0)
        {
            var crateDTO = new CrateDTO() 
            { 
                Id = Guid.NewGuid().ToString(), 
                Label = label, 
                Contents = contents, 
                ManifestType = manifestType, 
                ManifestId = manifestId 
            };
            return crateDTO;
        }

        public CrateDTO CreateDesignTimeFieldsCrate(string label, List<FieldDTO> fields)
        {    
            return Create(label, 
                JsonConvert.SerializeObject(new StandardDesignTimeFieldsMS() {Fields = fields}),
                manifestType: "Standard Design-Time Fields", 
                manifestId: 3);
        }

        public CrateDTO CreateStandardConfigurationControlsCrate(string label, List<FieldDefinitionDTO> controls)
        {
            return Create(label, 
                JsonConvert.SerializeObject(new StandardConfigurationControlsMS() { Controls = controls }),
                manifestType: "Standard Configuration Controls",
                manifestId: 6);
        }

        public T GetContents<T>(CrateDTO crate)
        {
            return _serializer.Deserialize<T>(crate.Contents);
        }

        public IEnumerable<JObject> GetElementByKey<TKey>(IEnumerable<CrateDTO> searchCrates, TKey key, string keyFieldName)
        {

            List<JObject> resultsObjects = new List<JObject>();
            foreach (var curCrate in searchCrates.Where(c => !string.IsNullOrEmpty(c.Contents)))
            {
                JContainer curCrateJSON = JsonConvert.DeserializeObject<JContainer>(curCrate.Contents);
                var results = curCrateJSON.Descendants()
                    .OfType<JObject>()
                    .Where(x => x[keyFieldName] != null && x[keyFieldName].Value<TKey>().Equals(key));
                resultsObjects.AddRange(results); ;
            }
            return resultsObjects;


        }
    }
}

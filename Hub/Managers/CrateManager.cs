﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Data.Crates;
using Data.Interfaces.DataTransferObjects;
using Data.Interfaces.Manifests;
using Hub.Managers.Crates;
using Newtonsoft.Json.Linq;

namespace Hub.Managers
{
    public partial class CrateManager : ICrateManager
    {
       

        public CrateManager()
        {
        }




//        public CrateDTO Create(string label, string contents, string manifestType = "", int manifestId = 0)
//        {
//            var crateDTO = new CrateDTO() 
//            { 
//                Id = Guid.NewGuid().ToString(), 
//                Label = label, 
//                Contents = contents, 
//                ManifestType = manifestType, 
//                ManifestId = manifestId 
//            };
//            return crateDTO;
//        }

        public ICrateStorageUpdater UpdateStorage(Expression<Func<JToken>> storageAccessExpression)
        {
            return new CrateStorageStorageUpdater(storageAccessExpression);
        }

        public ICrateStorageUpdater UpdateStorage(Expression<Func<string>> storageAccessExpression)
        {
            return new CrateStorageStorageUpdater(storageAccessExpression);
        }

        public CrateStorage GetStorage(string rawStorage)
        {
            if (string.IsNullOrWhiteSpace(rawStorage))
            {
                return new CrateStorage();
            }

            return CrateStorageSerializer.Default.Load(rawStorage);
        }

        public CrateStorage GetStorage(JToken rawStorage)
        {
            if (rawStorage == null)
            {
                return new CrateStorage();
            }

            return CrateStorageSerializer.Default.Load(rawStorage);
        }

        public string EmptyStorageAsStr()
        {
            return CrateStorageSerializer.Default.SaveToString(new CrateStorage());
        }

        public JToken EmptyStorageAsJtoken()
        {
            return CrateStorageSerializer.Default.SaveToJson(new CrateStorage());
        }

        public Crate CreateAuthenticationCrate(string label, AuthenticationMode mode)
        {
            return Crate.FromContent(label, new StandardAuthenticationCM()
            {
                Mode = mode
            });
//
//            var manifestSchema = new StandardAuthenticationCM()
//            {
//                Mode = mode
//            };
//
//            return Create(
//                label,
//                JsonConvert.SerializeObject(manifestSchema),
//                manifestType: CrateManifests.STANDARD_AUTHENTICATION_NAME,
//                manifestId: CrateManifests.STANDARD_AUTHENTICATION_ID);
        }

        public Crate CreateDesignTimeFieldsCrate(string label, params FieldDTO[] fields)
        {
            return Crate.FromContent(label, new StandardDesignTimeFieldsCM() { Fields = fields.ToList() });
//
//            return Create(label, 
//                JsonConvert.SerializeObject(new StandardDesignTimeFieldsCM() { Fields = fields.ToList() }),
//                manifestType: CrateManifests.DESIGNTIME_FIELDS_MANIFEST_NAME, 
//                manifestId: CrateManifests.DESIGNTIME_FIELDS_MANIFEST_ID);
        }

        public Crate CreateStandardConfigurationControlsCrate(string label, params ControlDefinitionDTO[] controls)
        {
            return Crate.FromContent(label, new StandardConfigurationControlsCM() {Controls = controls.ToList()});
//            
//            return Create(label, 
//                JsonConvert.SerializeObject(new StandardConfigurationControlsCM() { Controls = controls.ToList() }),
//                manifestType: CrateManifests.STANDARD_CONF_CONTROLS_NANIFEST_NAME,
//                manifestId: CrateManifests.STANDARD_CONF_CONTROLS_MANIFEST_ID);
        }

        public Crate CreateStandardEventSubscriptionsCrate(string label, params string[] subscriptions)
        {
            return Crate.FromContent(label, new EventSubscriptionCM() {Subscriptions = subscriptions.ToList()});
//
//            return Create(label,
//                JsonConvert.SerializeObject(new EventSubscriptionCM() { Subscriptions = subscriptions.ToList() }),
//                manifestType: CrateManifests.STANDARD_EVENT_SUBSCRIPTIONS_NAME,
//                manifestId: CrateManifests.STANDARD_EVENT_SUBSCRIPTIONS_ID);
        }

        public Crate CreateStandardEventReportCrate(string label, EventReportCM eventReport)
        {
            return Crate.FromContent(label, eventReport);
//
//            return Create(label,
//                JsonConvert.SerializeObject(eventReport),
//                manifestType: CrateManifests.STANDARD_EVENT_REPORT_NAME,
//                manifestId: CrateManifests.STANDARD_EVENT_REPORT_ID);
        }

        public Crate CreateStandardTableDataCrate(string label, bool firstRowHeaders, params TableRowDTO[] table)
        {
            return Crate.FromContent(label, new StandardTableDataCM() { Table = table.ToList(), FirstRowHeaders = firstRowHeaders });
//
//            return Create(label,
//                JsonConvert.SerializeObject(new StandardTableDataCM() { Table = table.ToList(), FirstRowHeaders = firstRowHeaders }),
//                manifestType: CrateManifests.STANDARD_TABLE_DATA_MANIFEST_NAME,
//                manifestId: CrateManifests.STANDARD_TABLE_DATA_MANIFEST_ID);
        }

//        public T GetContents<T>(CrateDTO crate)
//        {
//            return JsonConvert.DeserializeObject<T>(crate.Contents);
//        }

//        public StandardConfigurationControlsCM GetStandardConfigurationControls(CrateDTO crate)
//        {
//            return JsonConvert.DeserializeObject<StandardConfigurationControlsCM>(crate.Contents, new ControlDefinitionDTOConverter());
//        }
//
//        public StandardDesignTimeFieldsCM GetStandardDesignTimeFields(CrateDTO crate)
//        {
//            return JsonConvert.DeserializeObject<StandardDesignTimeFieldsCM>(crate.Contents);
//        }

        /// <summary>
        /// Retrieves all JObject elements that have a key field equal to a key value
        /// </summary>
        /// <typeparam name="TKey"></typeparam>
        /// <param name="searchCrates">Crates collection where to search through. CrateDTO.Contents property is used.</param>
        /// <param name="key">Key field value to search</param>
        /// <param name="keyFieldName">Key field name</param>
        /// <returns>Returns JSON descendants with specified key field value.</returns>
        /// <remarks>This method iterates through all JSON descendants (entire JSON tree).</remarks>
        /// <example>
        /// var crates = new[] { new CrateDTO { Contents: "[{key: 'example1', value: 'some value'}, {key: 'example2', value: 'another value'}, {name: 'example1', value: 'note there is no key field'}]" } };
        /// var elements = GetElementByKey(crates, "example1", "key");
        /// // elements will contain the only JObject: {key: 'example1', value: 'some value'}
        /// </example>
//        public IEnumerable<JObject> GetElementByKey<TKey>(IEnumerable<CrateDTO> searchCrates, TKey key, string keyFieldName)
//        {
//            List<JObject> resultsObjects = new List<JObject>();
//            foreach (var curCrate in searchCrates.Where(c => !string.IsNullOrEmpty(c.Contents)))
//            {
//                JContainer curCrateJSON = JsonConvert.DeserializeObject<JContainer>(curCrate.Contents);
//                var results = curCrateJSON.Descendants()
//                    .OfType<JObject>()
//                    // where (object has a key field) && (key field value equals to key argument)
//                    .Where(x => x[keyFieldName] != null && Object.Equals(x[keyFieldName].Value<TKey>(), key));
//                resultsObjects.AddRange(results);
//            }
//            return resultsObjects;
//        }

//        public void RemoveCrateByManifestId(IList<CrateDTO> crates, int manifestId)
//        {
//            var curCrates = crates.Where(c => c.ManifestId == manifestId).ToList();
//            if (curCrates.Count() > 0)
//            {
//                foreach (CrateDTO crate in curCrates)
//                {
//                    crates.Remove(crate);
//                }
//            }
//        }
//
//        public void RemoveCrateByLabel(IList<CrateDTO> crates, string label)
//        {
//            var curCrates = crates.Where(c => c.Label == label).ToList();
//            if (curCrates.Count() > 0)
//            {
//                foreach (CrateDTO crate in curCrates)
//                {
//                    crates.Remove(crate);
//                }
//            }
//        }
//
//        public void RemoveCrateByManifestType(IList<CrateDTO> crates, string manifestType)
//        {
//            var curCrates = crates.Where(c => c.ManifestType == manifestType).ToList();
//            if (curCrates.Count() > 0)
//            {
//                foreach (CrateDTO crate in curCrates)
//                {
//                    crates.Remove(crate);
//                }
//            }
//        }
//
//        public void ReplaceCratesByManifestType(IList<CrateDTO> sourceCrates, string manifestType, IList<CrateDTO> newCratesContent)
//        {
//            //remove existing crates with the manifest type
//            RemoveCrateByManifestType(sourceCrates, manifestType);
//            
//            //add the new content to the source crates
//            newCratesContent.ToList().ForEach(sourceCrates.Add);
//        }

//        public void ReplaceCratesByLabel(IList<CrateDTO> sourceCrates, string label, IList<CrateDTO> newCratesContent)
//        {
//            var curMatchedCrates = GetCratesByLabel(label, new CrateStorageDTO {CrateDTO = sourceCrates.ToList()});
//
//            foreach (CrateDTO curMatchedCrate in curMatchedCrates)
//            {
//                ReplaceCratesByManifestType(sourceCrates, curMatchedCrate.ManifestType, newCratesContent);
//            }
//        }

        public Crate CreatePayloadDataCrate(string payloadDataObjectType, string crateLabel, StandardTableDataCM tableDataMS)
        {
            return Crate.FromContent(crateLabel, TransformStandardTableDataToStandardPayloadData(payloadDataObjectType, tableDataMS));
//
//            return Create(crateLabel,
//                            JsonConvert.SerializeObject(TransformStandardTableDataToStandardPayloadData(payloadDataObjectType, tableDataMS)),
//                            manifestType: CrateManifests.STANDARD_PAYLOAD_MANIFEST_NAME,
//                            manifestId: CrateManifests.STANDARD_PAYLOAD_MANIFEST_ID);
        }

//        public Crate CreatePayloadDataCrate(List<KeyValuePair<string,string>> curFields)
//        {            
//            List<FieldDTO> crateFields = new List<FieldDTO>();
//            foreach(var field in curFields)
//            {
//                crateFields.Add(new FieldDTO() { Key = field.Key, Value = field.Value });             
//            }
//
//            var crate = new Crate(CrateManifestType.FromEnum(MT.StandardPayloadData));
//
//            crate.Put(crateFields);
//
//            return ;
//
//            return Create("Payload Data", JsonConvert.SerializeObject(crateFields));            
//        }

        private StandardPayloadDataCM TransformStandardTableDataToStandardPayloadData(string curObjectType, StandardTableDataCM tableDataMS)
        {
            var payloadDataMS = new StandardPayloadDataCM()
            {
                PayloadObjects = new List<PayloadObjectDTO>(),
                ObjectType = curObjectType,
            };

            // Rows containing column names
            var columnHeadersRowDTO = tableDataMS.Table[0];

            for (int i = 1; i < tableDataMS.Table.Count; ++i) // Since first row is headers; hence i starts from 1
            {
                var tableRowDTO = tableDataMS.Table[i];
                var fields = new List<FieldDTO>();
                for (int j = 0; j < tableRowDTO.Row.Count; ++j)
                {
                    var tableCellDTO = tableRowDTO.Row[j];
                    var listFieldDTO = new FieldDTO()
                    {
                        Key = columnHeadersRowDTO.Row[j].Cell.Value,
                        Value = tableCellDTO.Cell.Value,
                    };
                    fields.Add(listFieldDTO);
                }
                payloadDataMS.PayloadObjects.Add(new PayloadObjectDTO() { PayloadObject = fields, });
            }

            return payloadDataMS;
        }

//        public IEnumerable<CrateDTO> GetCratesByManifestType(string curManifestType, CrateStorageDTO curCrateStorageDTO)
//        {
//            if (String.IsNullOrEmpty(curManifestType))
//                throw new ArgumentNullException("Parameter Manifest Type is empty");
//            if (curCrateStorageDTO == null)
//                throw new ArgumentNullException("Parameter CrateStorageDTO is null.");
//
//            IEnumerable<CrateDTO> crateDTO = null;
//
//            crateDTO = curCrateStorageDTO.CrateDTO.Where(crate => crate.ManifestType == curManifestType);
//
//            return crateDTO;
//        }
//
//        public IEnumerable<CrateDTO> GetCratesByLabel(string curLabel, CrateStorageDTO curCrateStorageDTO)
//        {
//            if (String.IsNullOrEmpty(curLabel))
//                throw new ArgumentNullException("Parameter Label is empty");
//            if (curCrateStorageDTO == null)
//                throw new ArgumentNullException("Parameter CrateStorageDTO is null.");
//
//            IEnumerable<CrateDTO> crateDTOList = null;
//
//            crateDTOList = curCrateStorageDTO.CrateDTO.Where(crate => crate.Label == curLabel);
//
//            return crateDTOList;
//        }

        //-----------------------------------------------------------------------------------------------------

//        public void AddCrate(ActionDO curActionDO, List<CrateDTO> curCrateDTOLists)
//        {
//            if (curCrateDTOLists == null)
//                throw new ArgumentNullException("CrateDTO is null");
//            if (curActionDO == null)
//                throw new ArgumentNullException("ActionDO is null");
//
//            if (curCrateDTOLists.Count > 0)
//            {
//                curActionDO.UpdateCrateStorageDTO(curCrateDTOLists);
//            }
//        }
//
//        public void AddCrate(ActionDO curActionDO, CrateDTO curCrateDTO)
//        {
//            AddCrate(curActionDO, new List<CrateDTO>() { curCrateDTO });
//        }
//
//        public void AddCrate(PayloadDTO payload, List<CrateDTO> curCrateDTOLists)
//        {
//            if (curCrateDTOLists == null)
//                throw new ArgumentNullException("CrateDTO is null");
//            if (payload == null)
//                throw new ArgumentNullException("PayloadDTO is null");
//
//            if (curCrateDTOLists.Count > 0)
//            {
//                payload.UpdateCrateStorageDTO(curCrateDTOLists);
//            }
//        }

//        public void AddCrate(PayloadDTO payload, CrateDTO curCrateDTO)
//        {
//            AddCrate(payload, new List<CrateDTO>() { curCrateDTO });
//        }
//
//        public void AddOrReplaceCrate(string label, ActionDO curActionDO, CrateDTO curCrateDTO)
//        {
//            var existingCratesWithLabelInActionDO = GetCratesByLabel(label, curActionDO.CrateStorageDTO());
//            if (!existingCratesWithLabelInActionDO.Any()) // no existing crates with user provided label found, then add the crate
//            {
//                AddCrate(curActionDO, curCrateDTO);
//            }
//            else
//            {
//                // Remove the existing crate for this label
//                RemoveCrateByLabel(curActionDO.CrateStorageDTO().CrateDTO, label);
//
//                // Add the newly created crate for this label to action's crate storage
//                AddCrate(curActionDO, curCrateDTO);
//            }
//        }

//        public List<CrateDTO> GetCrates(ActionDO curActionDO)
//        {
//            return curActionDO.CrateStorageDTO().CrateDTO;
//        }

//        public StandardConfigurationControlsCM GetConfigurationControls(ActionDO curActionDO)
//        {
//            var curActionDTO = Mapper.Map<ActionDTO>(curActionDO);
//            var confControls = GetCratesByManifestType(MT.StandardConfigurationControls.GetEnumDisplayName(), curActionDTO.CrateStorage).ToList();
//            if (confControls.Count() != 0 && confControls.Count() != 1)
//                throw new ArgumentException("Expected number of CrateDTO is 0 or 1. But got '{0}'".format(confControls.Count()));
//            if (!confControls.Any())
//                return null;
//            var standardCfgControlsMs = JsonConvert.DeserializeObject<StandardConfigurationControlsCM>(confControls.First().Contents);
//            return standardCfgControlsMs;
//        }

    }
}

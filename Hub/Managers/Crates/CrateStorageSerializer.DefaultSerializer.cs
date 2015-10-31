﻿using System;
using Data.Crates;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Hub.Managers.Crates
{
    partial class CrateStorageSerializer
    {
        class DefaultSerializer : IManifestSerializer
        {
            private readonly Type _targetType;

            private static readonly JsonSerializer Serializer = JsonSerializer.Create(new JsonSerializerSettings
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore
            });

            public DefaultSerializer(Type targetType)
            {
                _targetType = targetType;
            }

            public void Initialize(ICrateStorageSerializer storageSerializer)
            {
            }

            public object Deserialize(JToken crateContent)
            {
                return crateContent.ToObject(_targetType, Serializer);
            }

            public JToken Serialize(object content)
            {
                using (JTokenWriter writer = new JTokenWriter())
                {
                    Serializer.Serialize(writer, content);
                    return writer.Token;
                }
            }
        }
    }
}


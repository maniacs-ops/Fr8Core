﻿using System.Linq;
using System.Web.Http.Description;
using Swashbuckle.Swagger;

namespace HubWeb
{
    public class RemoveDuplicatesDocumentFilter : IDocumentFilter
    {
        public void Apply(SwaggerDocument swaggerDoc, SchemaRegistry schemaRegistry, IApiExplorer apiExplorer)
        {
            var duplicates1 = apiExplorer.ApiDescriptions.Where(x => x.RelativePath.ToLower().Contains("/" + x.HttpMethod.Method.ToLower() + "/")).ToList();
            var duplicates2 = apiExplorer.ApiDescriptions.Where(x => x.RelativePath.EndsWith(x.HttpMethod.Method, System.StringComparison.OrdinalIgnoreCase)).ToList();
            duplicates1.AddRange(duplicates2);
            foreach (var item in duplicates1)
            {
                apiExplorer.ApiDescriptions.Remove(item);
                swaggerDoc.paths.Remove("/" + item.RelativePath);
            }
        }
    }
}
﻿using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Data.Repositories;
using Fr8.Infrastructure.Data.Manifests;
using PlanDirectory.CategoryPages;

namespace PlanDirectory.Infrastructure
{
    public class PlanCategoryPageGenerator : IPageGenerator
    {
        private const string TagsSeparator = "-";
        private const string PageExtension = ".html";

        private readonly IPageDefinitionRepository _pageDefinitionRepository;

        public PlanCategoryPageGenerator(IPageDefinitionRepository pageDefinitionRepository)
        {
            _pageDefinitionRepository = pageDefinitionRepository;
        }

        public Task Generate(IEnumerable<TemplateTag> tags, PlanTemplateCM planTemplate)
        {
            var path = @"D:\\Dev\\fr8company\\Services\\PlanDirectory\\CategoryPages\\";

            foreach (var tag in tags)
            {
                if (!(tag is WebServiceTemplateTag))
                    continue;
                var webServiceTemplateTag = tag as WebServiceTemplateTag;
                var fileName = GeneratePageNameFromTags(webServiceTemplateTag.TagsWithIcons.Select(x => x.Key));
                var template = new PlanCategoryTemplate();
                template.Session = new Dictionary<string, object>
                {
                    ["Name"] = fileName,
                    ["Tags"] = webServiceTemplateTag.TagsWithIcons,
                    ["RelatedPlans"] = new Dictionary<string, string>()
                    {
                        {planTemplate.Name, planTemplate.Description }
                    }
                };
                // Must call this to transfer values.
                template.Initialize();

                string pageContent = template.TransformText();
                File.WriteAllText(path + fileName + PageExtension, pageContent);
            }
            return Task.FromResult(0);
        }

        /// <summary>
        /// Generates pageName from tagsTitles
        /// </summary>
        /// <param name="tagsTitles"></param>
        /// <returns></returns>
        private static string GeneratePageNameFromTags(IEnumerable<string> tagsTitles)
        {
            return string.Join(
                TagsSeparator,
                tagsTitles.Select(x => x.ToLower()).OrderBy(x => x));
        }
    }
}
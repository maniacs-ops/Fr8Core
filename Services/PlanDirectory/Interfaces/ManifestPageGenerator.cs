﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Data.Entities;
using Data.Interfaces;
using Fr8.Infrastructure.Data.Manifests;
using Hub.Interfaces;
using Hub.Services;
using PlanDirectory.Exceptions;
using PlanDirectory.ManifestPages;

namespace PlanDirectory.Interfaces
{
    public class ManifestPageGenerator : IManifestPageGenerator
    {
        private const string PagePrefix = "Manifest description of ";
        private const string PageType = "Manifest Description";
        private const string PageExtension = ".html";

        private readonly ITemplateGenerator _templateGenerator;
        private readonly IPageDefinition _pageDefinitionService;
        private readonly Fr8Account _fr8AccountService;
        private readonly IUnitOfWorkFactory _uowFactory;

        public ManifestPageGenerator(ITemplateGenerator templateGenerator, IPageDefinition pageDefinitionService, Fr8Account fr8AccountService, IUnitOfWorkFactory uowFactory)                                           
        {
            _templateGenerator = templateGenerator;
            _pageDefinitionService = pageDefinitionService;
            _fr8AccountService = fr8AccountService;
            _uowFactory = uowFactory;
        }

        public async Task<Uri> Generate(string manifestName, GenerateMode generateMode)
        {
            if (string.IsNullOrWhiteSpace(manifestName))
            {
                throw new ArgumentException("Value can't be empty", nameof(manifestName));
            }
            var normalizedManifestName = manifestName.ToLower();
            var pageName = $"{PagePrefix}{manifestName}";
            var fileName = $"{pageName}{PageExtension}";
            var pageDefinition = _pageDefinitionService.Get(x => x.Type == PageType && x.PageName == pageName).FirstOrDefault();
            switch (generateMode)
            {
                case GenerateMode.RetrieveExisting:
                    if (pageDefinition == null)
                    {
                        throw new ManifestPageNotFoundException(manifestName);
                    }
                    return pageDefinition.Url;
                case GenerateMode.GenerateIfNotExists:
                    if (pageDefinition != null)
                    {
                        return pageDefinition.Url;
                    }
                    break;
            }
            var systemUser = _fr8AccountService.GetSystemUser()?.EmailAddress?.Address;
            if (string.IsNullOrEmpty(systemUser))
            {
                throw new ManifestGenerationException("Failed to generate manifest description page. System user doesn't exist or not configured properly");
            }
            using (var uow = _uowFactory.Create())
            {
                var manifests = uow.MultiTenantObjectRepository.Query<ManifestDescriptionCM>(systemUser, x => x.Name.ToLower() == normalizedManifestName);
                if (manifests.Count == 0)
                {
                    throw new ManifestNotFoundException(manifestName);
                }
                manifests.Sort((x, y) => int.Parse(y.Version).CompareTo(int.Parse(x.Version)));
                if (pageDefinition == null)
                {
                    pageDefinition = new PageDefinitionDO
                    {
                        Type = PageType,
                        PageName = pageName,
                    };
                }
                pageDefinition.Title = $"Manifest description - {manifestName}";
                pageDefinition.Description = $"Basic information and sample JSON of all registered version of '{manifestName}' manifest";
                pageDefinition.Url = new Uri($"{_templateGenerator.BaseUrl}/{fileName}");
                await _templateGenerator.Generate(new ManifestDescriptionTemplate(), fileName, new Dictionary<string, object>
                {
                    ["Manifests"] = manifests
                });
                _pageDefinitionService.CreateOrUpdate(pageDefinition);
                return pageDefinition.Url;
            }
        }
    }
}
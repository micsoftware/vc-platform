﻿using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Web.Hosting;
using System.Web.Http;
using System.Web.Http.Description;
using CacheManager.Core;
using Microsoft.Practices.Unity;
using Swashbuckle.Application;
using Swashbuckle.Swagger;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.ExportImport;
using VirtoCommerce.Platform.Core.Modularity;
using VirtoCommerce.Platform.Core.Packaging;
using VirtoCommerce.Platform.Core.Settings;

namespace SwashbuckleModule.Web
{
    public class Module : ModuleBase, ISupportExportImportModule
    {
        private readonly IUnityContainer _container;

        public Module(IUnityContainer container)
        {
            _container = container;
        }

        #region IModule Members

        public override void Initialize()
        {
            var moduleInitializerOptions = _container.Resolve<IModuleInitializerOptions>();
            var routePrefix = moduleInitializerOptions.RoutePrefix;

            var xmlCommentsDirectoryPaths = new[]
            {
                moduleInitializerOptions.VirtualRoot + "/App_Data/Modules",
                moduleInitializerOptions.VirtualRoot + "/bin"
            };
            var xmlCommentsFilePaths = xmlCommentsDirectoryPaths.SelectMany(GetXmlFilesPaths).ToArray();

            Func<TagsFilter> tagsFilterFactory = () => new TagsFilter(_container.Resolve<IPackageService>(), _container.Resolve<ISettingsManager>());

            var httpConfiguration = _container.Resolve<HttpConfiguration>();

            // Add full swagger generator
            httpConfiguration.EnableSwagger(routePrefix + "docs/{apiVersion}", c =>
            {
                c.SingleApiVersion("v1", "VirtoCommerce Solution REST API documentation");
                c.DocumentFilter(tagsFilterFactory);
                c.OperationFilter(tagsFilterFactory);
                ApplyCommonSwaggerConfiguration(c, string.Empty, xmlCommentsFilePaths, tagsFilterFactory);
            })
            .EnableSwaggerUi(routePrefix + "docs/ui/{*assetPath}", c =>
            {
                var assembly = Assembly.GetExecutingAssembly();
                c.CustomAsset("index", assembly, "SwashbuckleModule.Web.SwaggerUi.CustomAssets.index.html");
                c.CustomAsset("images/logo_small-png", assembly, "SwashbuckleModule.Web.SwaggerUi.CustomAssets.logo_small.png");
                c.CustomAsset("css/vc-css", assembly, "SwashbuckleModule.Web.SwaggerUi.CustomAssets.vc.css");
                c.CustomAsset("swagger-ui-js", assembly, "SwashbuckleModule.Web.SwaggerUi.CustomAssets.swagger-ui.js");
            });

            // Add separate swagger generator for each installed module
            var moduleCatalog = _container.Resolve<IModuleCatalog>();

            var modules = moduleCatalog.Modules
                .Where(m => m.ModuleInstance != null)
                .ToArray();

            foreach (var module in modules)
            {
                var routeName = string.Concat("swagger_", module.ModuleName);
                var routeTemplate = string.Concat(routePrefix, "docs/", module.ModuleName, "/{apiVersion}");

                httpConfiguration.EnableSwagger(routeName, routeTemplate, c =>
                {
                    // Include only APIs from current module
                    c.MultipleApiVersions(
                        (apiDescription, apiVersion) => module.ModuleInstance != null && apiDescription.ActionDescriptor.ControllerDescriptor.ControllerType.Assembly == module.ModuleInstance.GetType().Assembly,
                        versionInfoBuilder => versionInfoBuilder.Version("v1", module.ModuleName + " REST API documentation"));
                    ApplyCommonSwaggerConfiguration(c, module.ModuleName, xmlCommentsFilePaths, tagsFilterFactory);
                });
            }
        }

        private void ApplyCommonSwaggerConfiguration(SwaggerDocsConfig c, string cacheKey, string[] xmlCommentsFilePaths, Func<TagsFilter> tagsFilterFactory)
        {
            var cacheManager = _container.Resolve<ICacheManager<object>>();

            c.CustomProvider(defaultProvider => new CachingSwaggerProvider(defaultProvider, cacheManager, cacheKey));
            c.MapType<object>(() => new Schema { type = "object" });
            c.IgnoreObsoleteProperties();
            c.UseFullTypeNameInSchemaIds();
            c.DescribeAllEnumsAsStrings();
            c.OperationFilter(() => new OptionalParametersFilter());
            c.ResolveConflictingActions(apiDescriptions => apiDescriptions.First());
            c.RootUrl(GetRootUrl);
            c.PrettyPrint();
            c.ApiKey("apiKey")
                .Description("API Key Authentication")
                .Name("api_key")
                .In("header");

            foreach (var path in xmlCommentsFilePaths)
            {
                c.IncludeXmlComments(path);
            }
        }

        #endregion

        private static string GetRootUrl(HttpRequestMessage message)
        {
            var result = new Uri(message.RequestUri, message.GetRequestContext().VirtualPathRoot).ToString();
            return result;
        }

        private static string[] GetXmlFilesPaths(string xmlRelativePath)
        {
            var path = HostingEnvironment.MapPath(xmlRelativePath);
            var files = Directory.GetFiles(path, "*.Web.XML");
            return files;
        }

        private class OptionalParametersFilter : IOperationFilter
        {
            #region Implementation of IOperationFilter

            public void Apply(Operation operation, SchemaRegistry schemaRegistry, ApiDescription apiDescription)
            {
                var optionalParameters = apiDescription.ParameterDescriptions
                    .Where(p => p.ParameterDescriptor != null && p.ParameterDescriptor.GetCustomAttributes<SwaggerOptionalAttribute>().Any())
                    .ToList();

                foreach (var apiParameter in optionalParameters)
                {
                    var parameter = operation.parameters.FirstOrDefault(p => p.name == apiParameter.Name);
                    if (parameter != null)
                    {
                        parameter.required = false;
                    }
                }
            }

            #endregion
        }

        private class TagsFilter : IDocumentFilter, IOperationFilter
        {
            private readonly IPackageService _packageService;
            private readonly ISettingsManager _settingManager;

            public TagsFilter(IPackageService packageService, ISettingsManager settingManager)
            {
                _packageService = packageService;
                _settingManager = settingManager;
            }

            #region IDocumentFilter Members

            public void Apply(SwaggerDocument swaggerDoc, SchemaRegistry schemaRegistry, IApiExplorer apiExplorer)
            {
                var defaultApiKey = _settingManager.GetValue("Swashbuckle.DefaultApiKey", string.Empty);

                swaggerDoc.info.description = string.Format(CultureInfo.InvariantCulture, "For this sample, you can use the `{0}` key to test the authorization filters.", defaultApiKey);
                swaggerDoc.info.contact = new Contact
                {
                    email = "support@virtocommerce.com",
                    name = "VirtoCommerce",
                    url = "http://virtocommerce.com"
                };
                swaggerDoc.info.termsOfService = "";
                swaggerDoc.info.license = new License
                {
                    name = "Virto Commerce Open Software License 3.0",
                    url = "http://virtocommerce.com/opensourcelicense"
                };
                var tags = _packageService.GetModules().Select(x => new Tag
                {
                    name = x.Title,
                    description = x.Description
                }).ToList();
                tags.Add(new Tag
                {
                    name = "VirtoCommerce platform",
                    description = "Platform functionality represent common resources and operations"
                });
                swaggerDoc.tags = tags;
            }

            #endregion

            #region IOperationFilter Members

            public void Apply(Operation operation, SchemaRegistry schemaRegistry, ApiDescription apiDescription)
            {
                var module = _packageService.GetModules().Where(x => x.ModuleInfo.ModuleInstance != null).FirstOrDefault(x => apiDescription.ActionDescriptor.ControllerDescriptor.ControllerType.Assembly == x.ModuleInfo.ModuleInstance.GetType().Assembly);
                if (module != null)
                {
                    operation.tags = new[] { module.Title };
                }
                else if (apiDescription.ActionDescriptor.ControllerDescriptor.ControllerType.Assembly.GetName().Name == "VirtoCommerce.Platform.Web")
                {
                    operation.tags = new[] { "VirtoCommerce platform" };
                }
            }

            #endregion
        }

        #region ISupportExportImportModule Members

        public string ExportDescription
        {
            get
            {
                var settingManager = _container.Resolve<ISettingsManager>();
                return settingManager.GetValue("Swashbuckle.ExportImport.Description", string.Empty);
            }
        }

        public void DoExport(Stream outStream, PlatformExportManifest manifest, Action<ExportImportProgressInfo> progressCallback)
        {
        }

        public void DoImport(Stream inputStream, PlatformExportManifest manifest, Action<ExportImportProgressInfo> progressCallback)
        {
        }

        #endregion
    }
}

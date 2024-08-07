using Sdl.Community.GroupShareKit.Clients;
using Sdl.Community.GroupShareKit.Models.Response;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Sdl.Community.GroupShareKit.Tests.Integration
{
    public static class Helper
    {
        public static GroupShareClient GsClient { get; }

        public static Uri BaseUri => new Uri(GetVariable("GROUPSHAREKIT_BASEURI"));

        public static string GsUser => GetVariable("GROUPSHAREKIT_USERNAME");

        public static Guid GsUserId { get; }

        public static string GsPassword => GetVariable("GROUPSHAREKIT_PASSWORD");

        public static string Organization => GetVariable("GROUPSHAREKIT_TESTORGANIZATION");

        public static string GsServerName = GetVariable("GROUPSHAREKIT_SERVERNAME");

        public static string OrganizationId { get; }

        public static string OrganizationPath { get; }

        public static string OrganizationTag { get; }

        public static string PowerUserRoleId { get; }

        static Helper()
        {
            var token = GroupShareClient.GetRequestToken(
                GsUser,
                GsPassword,
                BaseUri,
                GroupShareClient.AllScopes).Result;

            GsClient = GroupShareClient.AuthenticateClient(
                token,
                GsUser,
                GsPassword,
                "",
                BaseUri,
                GroupShareClient.AllScopes).Result;

            GsUserId = GsClient.User.Get(new UserRequest(GsUser)).Result.UniqueId;

            var organization = GsClient
                 .Organization
                 .GetAll(new OrganizationRequest(true)).Result
                 .FirstOrDefault(o => o.Path == Organization);

            if (organization != null)
            {
                OrganizationId = organization.UniqueId.ToString();
                OrganizationPath = organization.Path;
                OrganizationTag = organization.Tags.FirstOrDefault();
            }

            var role = GsClient
                .Role
                .GetRoles().Result
                .FirstOrDefault(r => r.Name == "Power User");
            if (role != null)
            {
                PowerUserRoleId = role.UniqueId.ToString();
            }
        }

        public static string GetVariable(string key)
        {
            // by default it gets a process variable. Allow getting user as well
            return
                Environment.GetEnvironmentVariable(key) ??
                Environment.GetEnvironmentVariable(key, EnvironmentVariableTarget.User);
        }

        public static async Task<Guid> CreateOrganizationAsync()
        {
            var uniqueId = Guid.NewGuid();
            var organization = new Organization()
            {
                UniqueId = uniqueId,
                Name = $"Organization - {uniqueId}",
                IsLibrary = false,
                Description = null,
                Path = null,
                ParentOrganizationId = new Guid(OrganizationId),
                ChildOrganizations = null
            };

            var organizationId = await GsClient.Organization.CreateOrganization(organization);
            return organizationId;
        }

        public static async Task<Guid> CreateTemplateResourceAsync(Guid organizationId)
        {
            var templateRequest = new ProjectTemplate
            {
                Name = $"Project template - {Guid.NewGuid()}",
                OrganizationId = organizationId
            };

            var rawData = System.IO.File.ReadAllBytes(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Resources\SampleTemplate.sdltpl"));
            var templateId = await GsClient.Project.CreateProjectTemplate(templateRequest, rawData);

            return templateId;
        }

        public static async Task<Guid> CreateProjectAsync(Guid projectTemplateId)
        {
            var rawData = System.IO.File.ReadAllBytes(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Resources\Grammar.zip"));
            var projectName = $"Project - { Guid.NewGuid() }";

            var projectId = await GsClient.Project.CreateProject(new CreateProjectRequest(
                projectName,
                OrganizationId,
                null,
                DateTime.Now.AddDays(2),
                projectTemplateId.ToString(),
                rawData));

            await WaitForProjectCreated(Guid.Parse(projectId));

            return Guid.Parse(projectId);
        }

        public static async Task DeleteProjectAsync(Guid projectId)
        {
            await GsClient.Project.DeleteProject(projectId);
        }

        public static async Task DeleteProjectTemplateAsync(Guid projectTemplateId)
        {
            await GsClient.Project.DeleteProjectTemplate(projectTemplateId);
        }

        private static async Task<bool> WaitForProjectCreated(Guid projectId, int retryInterval = 3, int maxTryCount = 15)
        {
            for (var i = 0; i < maxTryCount; i++)
            {
                var statusInfo = await GsClient.Project.GetPublishingStatus(projectId);
                switch (statusInfo.Status)
                {
                    case PublishProjectStatus.Uploading:
                    case PublishProjectStatus.Scheduled:
                    case PublishProjectStatus.Publishing:
                        break;
                    case PublishProjectStatus.Completed:
                        return true;
                    case PublishProjectStatus.Error:
                        throw new Exception(statusInfo.Description);
                }

                await Task.Delay(retryInterval * 1000);
            }

            return false;
        }
    }
}

﻿/*
 * SonarQube Client
 * Copyright (C) 2016-2017 SonarSource SA
 * mailto:info AT sonarsource DOT com
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public
 * License as published by the Free Software Foundation; either
 * version 3 of the License, or (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with this program; if not, write to the Free Software Foundation,
 * Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.
 */

using System;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Moq.Protected;
using SonarQube.Client.Messages;

namespace SonarQube.Client.Services.Tests
{
    [TestClass]
    public class SonarQubeClientTests
    {
        #region Ctor checks
        [TestMethod]
        public void Ctor_WithNullConnection_ThrowsArgumentNullException()
        {
            // Arrange & Act
            Action action = () => new SonarQubeClient(null, new HttpClientHandler(), TimeSpan.MaxValue);

            // Assert
            action.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("connection");
        }

        [TestMethod]
        public void Ctor_WithNullMessageHandler_ThrowsArgumentNullException()
        {
            // Arrange & Act
            Action action = () => new SonarQubeClient(new ConnectionRequest(), null, TimeSpan.MaxValue);

            // Assert
            action.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("messageHandler");
        }

        [TestMethod]
        public void Ctor_WithZeroTimeout_ThrowsArgumentException()
        {
            // Arrange & Act
            Action action = () => new SonarQubeClient(new ConnectionRequest(), new HttpClientHandler(), TimeSpan.Zero);

            // Assert
            action.ShouldThrow<ArgumentException>()
                .WithMessage("Doesn't expect a zero or negative timeout.\r\nParameter name: requestTimeout")
                .And.ParamName.Should().Be("requestTimeout");
        }

        [TestMethod]
        public void Ctor_WithNegativeTimeout_ThrowsArgumentException()
        {
            // Arrange & Act
            Action action = () => new SonarQubeClient(new ConnectionRequest(), new HttpClientHandler(), TimeSpan.MinValue);

            // Assert
            action.ShouldThrow<ArgumentException>()
                .WithMessage("Doesn't expect a zero or negative timeout.\r\nParameter name: requestTimeout")
                .And.ParamName.Should().Be("requestTimeout");
        }
        #endregion

        #region Check called URLs
        [TestMethod]
        public async Task GetComponentsSearchProjectsAsync_CallsTheExpectedUri()
        {
            var request = new ComponentRequest { OrganizationKey = "org", Page = 42, PageSize = 25 };
            await Method_CallsTheExpectedUri(
                new Uri("api/components/search_projects?p=42&ps=25&organization=org&asc=true", UriKind.RelativeOrAbsolute),
                @"{""components"":[]}", (c, t) => c.GetComponentsSearchProjectsAsync(request, t));
        }
        [TestMethod]
        public async Task GetIssuesAsync_CallsTheExpectedUri()
        {
            await Method_CallsTheExpectedUri(
                new Uri("batch/issues?key=key", UriKind.RelativeOrAbsolute), "", (c, t) => c.GetIssuesAsync("key", t));
        }
        [TestMethod]
        public async Task GetOrganizationsAsync_CallsTheExpectedUri()
        {
            var request = new OrganizationRequest { Page = 42, PageSize = 25 };
            await Method_CallsTheExpectedUri(new Uri("api/organizations/search?p=42&ps=25", UriKind.RelativeOrAbsolute),
                @"{""organizations"":[]}", (c, t) => c.GetOrganizationsAsync(request, t));
        }
        [TestMethod]
        public async Task GetPluginsAsync_CallsTheExpectedUri()
        {
            await Method_CallsTheExpectedUri(new Uri("api/updatecenter/installed_plugins", UriKind.RelativeOrAbsolute),
                "", (c, t) => c.GetPluginsAsync(t));
        }
        [TestMethod]
        public async Task GetProjectsAsync_CallsTheExpectedUri()
        {
            await Method_CallsTheExpectedUri(new Uri("api/projects/index", UriKind.RelativeOrAbsolute),
                "", (c, t) => c.GetProjectsAsync(t));
        }
        [TestMethod]
        public async Task GetPropertiesAsync_CallsTheExpectedUri()
        {
            await Method_CallsTheExpectedUri(new Uri("api/properties/", UriKind.RelativeOrAbsolute),
                "", (c, t) => c.GetPropertiesAsync(t));
        }
        [TestMethod]
        public async Task GetQualityProfileChangeLogAsync_CallsTheExpectedUri()
        {
            var request = new QualityProfileChangeLogRequest { QualityProfileKey = "qp", PageSize = 25 };
            await Method_CallsTheExpectedUri(new Uri("api/qualityprofiles/changelog?profileKey=qp&ps=25", UriKind.RelativeOrAbsolute),
                "", (c, t) => c.GetQualityProfileChangeLogAsync(request, t));
        }
        [TestMethod]
        public async Task GetQualityProfilesAsync_CallsTheExpectedUri()
        {
            var request = new QualityProfileRequest { ProjectKey = "project" };
            await Method_CallsTheExpectedUri(new Uri("api/qualityprofiles/search?projectKey=project", UriKind.RelativeOrAbsolute),
                @"{""profiles"":[]}", (c, t) => c.GetQualityProfilesAsync(request, t));

            request = new QualityProfileRequest { ProjectKey = null };
            await Method_CallsTheExpectedUri(new Uri("api/qualityprofiles/search?defaults=true", UriKind.RelativeOrAbsolute),
                @"{""profiles"":[]}", (c, t) => c.GetQualityProfilesAsync(request, t));
        }
        [TestMethod]
        public async Task GetRoslynExportProfileAsync_CallsTheExpectedUri()
        {
            var request = new RoslynExportProfileRequest { QualityProfileName = "qp", LanguageKey = "cs" };
            await Method_CallsTheExpectedUri(
                new Uri("api/qualityprofiles/export?language=cs&name=qp&exporterKey=roslyn-cs", UriKind.RelativeOrAbsolute),
                @"<?xml version=""1.0"" encoding=""utf-8""?>
<RoslynExportProfile Version=""1.0"">
</RoslynExportProfile>", (c, t) => c.GetRoslynExportProfileAsync(request, t));

            request = new RoslynExportProfileRequest { QualityProfileName = "qp", LanguageKey = "vbnet" };
            await Method_CallsTheExpectedUri(
                new Uri("api/qualityprofiles/export?language=vbnet&name=qp&exporterKey=roslyn-vbnet", UriKind.RelativeOrAbsolute),
                @"<?xml version=""1.0"" encoding=""utf-8""?>
<RoslynExportProfile Version=""1.0"">
</RoslynExportProfile>", (c, t) => c.GetRoslynExportProfileAsync(request, t));
        }
        [TestMethod]
        public async Task GetVersionAsync_CallsTheExpectedUri()
        {
            await Method_CallsTheExpectedUri(new Uri("api/server/version", UriKind.RelativeOrAbsolute),
                "", (c, t) => c.GetVersionAsync(t));
        }
        [TestMethod]
        public async Task ValidateCredentialsAsync_CallsTheExpectedUri()
        {
            await Method_CallsTheExpectedUri(new Uri("api/authentication/validate", UriKind.RelativeOrAbsolute),
                @"{""valid"": true}", (c, t) => c.ValidateCredentialsAsync(t));
        }

        private async Task Method_CallsTheExpectedUri<T>(Uri expectedRelativeUri, string resultContent,
            Func<SonarQubeClient, CancellationToken, Task<Result<T>>> call)
        {
            // Arrange
            var httpHandler = new Mock<HttpMessageHandler>();
            var serverUri = new Uri("http://mysq.com/");
            var connection = new ConnectionRequest { ServerUri = serverUri };
            var client = new SonarQubeClient(connection, httpHandler.Object, TimeSpan.FromSeconds(10));

            httpHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .Returns(Task.FromResult(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(resultContent)
                }))
                .Callback<HttpRequestMessage, CancellationToken>((request, token) =>
                {
                    request.Method.Should().Be(HttpMethod.Get);
                    request.RequestUri.Should().Be(new Uri(serverUri, expectedRelativeUri));
                });

            // Act
            await call(client, CancellationToken.None);
        }
        #endregion

        #region Check successful requests
        [TestMethod]
        public async Task GetComponentsSearchProjectsAsync_WhenRequestIsSuccesful_ReturnsIsSuccessAndNotNullData()
        {
            var request = new ComponentRequest { OrganizationKey = "org", Page = 42, PageSize = 25 };
            await Method_WhenRequestIsSuccesful_ReturnsIsSuccessAndNotNullData(
                (c, t) => c.GetComponentsSearchProjectsAsync(request, t),
                @"{""components"":[{""organization"":""my - org - key - 1"",""id"":""AU - Tpxb--iU5OvuD2FLy"",""key"":""my_project"",""name"":""My Project 1"",""isFavorite"":true,""tags"":[""finance"",""java""],""visibility"":""public""},{""organization"":""my-org-key-1"",""id"":""AU-TpxcA-iU5OvuD2FLz"",""key"":""another_project"",""name"":""My Project 2"",""isFavorite"":false,""tags"":[],""visibility"":""public""}]}",
                result => result.Length.Should().Be(2));
        }
        [TestMethod]
        public async Task GetIssuesAsync_WhenRequestIsSuccesful_ReturnsIsSuccessAndNotNullData()
        {
            await Method_WhenRequestIsSuccesful_ReturnsIsSuccessAndNotNullData(
                (c, t) => c.GetIssuesAsync("key", t),
                new StreamReader(@"TestResources\IssuesProtobufResponse").ReadToEnd(),
                result => result.Length.Should().Be(1));
        }
        [TestMethod]
        public async Task GetOrganizationsAsync_WhenRequestIsSuccesful_ReturnsIsSuccessAndNotNullData()
        {
            var request = new OrganizationRequest { Page = 42, PageSize = 25 };
            await Method_WhenRequestIsSuccesful_ReturnsIsSuccessAndNotNullData(
                (c, t) => c.GetOrganizationsAsync(request, t),
                @"{""organizations"":[{""key"":""foo - company"",""name"":""Foo Company""},{""key"":""bar - company"",""name"":""Bar Company""}]}",
                result => result.Length.Should().Be(2));
        }
        [TestMethod]
        public async Task GetPluginsAsync_WhenRequestIsSuccesful_ReturnsIsSuccessAndNotNullData()
        {
            await Method_WhenRequestIsSuccesful_ReturnsIsSuccessAndNotNullData(
                (c, t) => c.GetPluginsAsync(t),
                @"[{""key"":""findbugs"",""name"":""Findbugs"",""version"":""2.1""},{""key"":""l10nfr"",""name"":""French Pack"",""version"":""1.10""},{""key"":""jira"",""name"":""JIRA"",""version"":""1.2""}]",
                result => result.Length.Should().Be(3));
        }
        [TestMethod]
        public async Task GetProjectsAsync_WhenRequestIsSuccesful_ReturnsIsSuccessAndNotNullData()
        {
            await Method_WhenRequestIsSuccesful_ReturnsIsSuccessAndNotNullData(
                (c, t) => c.GetProjectsAsync(t),
                @"[{""id"":""5035"",""k"":""org.jenkins-ci.plugins:sonar"",""nm"":""Jenkins Sonar Plugin"",""sc"":""PRJ"",""qu"":""TRK""},{""id"":""5146"",""k"":""org.codehaus.sonar-plugins:sonar-ant-task"",""nm"":""Sonar Ant Task"",""sc"":""PRJ"",""qu"":""TRK""},{""id"":""15964"",""k"":""org.codehaus.sonar-plugins:sonar-build-breaker-plugin"",""nm"":""Sonar Build Breaker Plugin"",""sc"":""PRJ"",""qu"":""TRK""}]",
                result => result.Length.Should().Be(3));
        }
        [TestMethod]
        public async Task GetPropertiesAsync_WhenRequestIsSuccesful_ReturnsIsSuccessAndNotNullData()
        {
            await Method_WhenRequestIsSuccesful_ReturnsIsSuccessAndNotNullData(
                (c, t) => c.GetPropertiesAsync(t),
                @"[{""key"":""sonar.demo.1.text"",""value"":""foo""},{""key"":""sonar.demo.1.boolean"",""value"":""true""},{""key"":""sonar.demo.2.text"",""value"":""bar""}]",
                result => result.Length.Should().Be(3));
        }
        [TestMethod]
        public async Task GetQualityProfileChangeLogAsync_WhenRequestIsSuccesful_ReturnsIsSuccessAndNotNullData()
        {
            var request = new QualityProfileChangeLogRequest { QualityProfileKey = "qp", PageSize = 25 };
            await Method_WhenRequestIsSuccesful_ReturnsIsSuccessAndNotNullData(
                (c, t) => c.GetQualityProfileChangeLogAsync(request, t),
                @"{""events"":[{""date"":""2015-02-23T17:58:39+0100"",""action"":""ACTIVATED"",""authorLogin"":""anakin.skywalker"",""authorName"":""Anakin Skywalker"",""ruleKey"":""squid:S2438"",""ruleName"":""\""Threads\"" should not be used where \""Runnables\"" are expected"",""params"":{""severity"":""CRITICAL""}}]}",
                result => result.Events.Length.Should().Be(1));
        }
        [TestMethod]
        public async Task GetQualityProfilesAsync_WhenRequestIsSuccesful_ReturnsIsSuccessAndNotNullData()
        {
            var request = new QualityProfileRequest { ProjectKey = "project" };
            await Method_WhenRequestIsSuccesful_ReturnsIsSuccessAndNotNullData(
                (c, t) => c.GetQualityProfilesAsync(request, t),
                @"{""profiles"":[{""key"":""AU-TpxcA-iU5OvuD2FL3"",""name"":""Sonar way"",""language"":""cs"",""languageName"":""C#"",""isInherited"":false,""activeRuleCount"":37,""activeDeprecatedRuleCount"":0,""isDefault"":true,""ruleUpdatedAt"":""2016-12-22T19:10:03+0100"",""lastUsed"":""2016-12-01T19:10:03+0100""}]}",
                result => result.Length.Should().Be(1));
        }
        [TestMethod]
        public async Task GetRoslynExportProfileAsync_WhenRequestIsSuccesful_ReturnsIsSuccessAndNotNullData()
        {
            var request = new RoslynExportProfileRequest { QualityProfileName = "qp", LanguageKey = "cs" };
            await Method_WhenRequestIsSuccesful_ReturnsIsSuccessAndNotNullData(
                (c, t) => c.GetRoslynExportProfileAsync(request, t),
                @"<?xml version=""1.0"" encoding=""utf-8""?>
<RoslynExportProfile Version=""1.0"">
  <Configuration>
    <RuleSet Name=""Rules for SonarQube"" Description=""This rule set was automatically generated from SonarQube."" ToolsVersion=""14.0"">
      <Rules AnalyzerId=""SonarAnalyzer.CSharp"" RuleNamespace=""SonarAnalyzer.CSharp"">
        <Rule Id=""S121"" Action=""Warning"" />
      </Rules>
    </RuleSet>
    <AdditionalFiles>
      <AdditionalFile FileName=""SonarLint.xml"" />
    </AdditionalFiles>
  </Configuration>
  <Deployment>
    <Plugins>
      <Plugin Key=""csharp"" Version=""6.4.0.3322"" StaticResourceName=""SonarAnalyzer-6.4.0.3322.zip"" />
    </Plugins>
    <NuGetPackages>
      <NuGetPackage Id=""SonarAnalyzer.CSharp"" Version=""6.4.0.3322"" />
    </NuGetPackages>
  </Deployment>
</RoslynExportProfile>",
                result => { });
        }
        [TestMethod]
        public async Task GetVersionAsync_WhenRequestIsSuccesful_ReturnsIsSuccessAndNotNullData()
        {
            await Method_WhenRequestIsSuccesful_ReturnsIsSuccessAndNotNullData(
                (c, t) => c.GetVersionAsync(t),
                "6.3.0.1234",
                result => result.Version.Should().Be("6.3.0.1234"));
        }
        [TestMethod]
        public async Task ValidateCredentialsAsync_WhenRequestIsSuccesful_ReturnsIsSuccessAndNotNullData()
        {
            await Method_WhenRequestIsSuccesful_ReturnsIsSuccessAndNotNullData(
                (c, t) => c.ValidateCredentialsAsync(t),
                "{\"valid\": true}",
                value => value.IsValid.Should().BeTrue());
        }

        private async Task Method_WhenRequestIsSuccesful_ReturnsIsSuccessAndNotNullData<T>(
            Func<SonarQubeClient, CancellationToken, Task<Result<T>>> call, string resultContent, Action<T> extraAssertions)
        {
            // Arrange
            var httpHandler = new Mock<HttpMessageHandler>();
            httpHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .Returns(Task.FromResult(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(resultContent)
                }));
            var connection = new ConnectionRequest { ServerUri = new Uri("http://mysq.com/") };
            var client = new SonarQubeClient(connection, httpHandler.Object, TimeSpan.FromSeconds(10));

            // Act
            var result = await call(client, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().NotBeNull();
            result.StatusCode.Should().Be(HttpStatusCode.OK);
            extraAssertions(result.Value);
        }
        #endregion

        #region Check cancellation
        [TestMethod]
        public void GetComponentsSearchProjectsAsync_WhenCancellationRequested_ThrowsException()
        {
            var request = new ComponentRequest { OrganizationKey = "org", Page = 42, PageSize = 25 };
            Method_WhenCancellationRequested_ThrowsException((c, t) => c.GetComponentsSearchProjectsAsync(request, t));
        }
        [TestMethod]
        public void GetIssuesAsync_WhenCancellationRequested_ThrowsException()
        {
            Method_WhenCancellationRequested_ThrowsException((c, t) => c.GetIssuesAsync("key", t));
        }
        [TestMethod]
        public void GetOrganizationsAsync_WhenCancellationRequested_ThrowsException()
        {
            var request = new OrganizationRequest { Page = 42, PageSize = 25 };
            Method_WhenCancellationRequested_ThrowsException((c, t) => c.GetOrganizationsAsync(request, t));
        }
        [TestMethod]
        public void GetPluginsAsync_WhenCancellationRequested_ThrowsException()
        {
            Method_WhenCancellationRequested_ThrowsException((c, t) => c.GetPluginsAsync(t));
        }
        [TestMethod]
        public void GetProjectsAsync_WhenCancellationRequested_ThrowsException()
        {
            Method_WhenCancellationRequested_ThrowsException((c, t) => c.GetProjectsAsync(t));
        }
        [TestMethod]
        public void GetPropertiesAsync_WhenCancellationRequested_ThrowsException()
        {
            Method_WhenCancellationRequested_ThrowsException((c, t) => c.GetPropertiesAsync(t));
        }
        [TestMethod]
        public void GetQualityProfileChangeLogAsync_WhenCancellationRequested_ThrowsException()
        {
            var request = new QualityProfileChangeLogRequest { QualityProfileKey = "qp", PageSize = 25 };
            Method_WhenCancellationRequested_ThrowsException((c, t) => c.GetQualityProfileChangeLogAsync(request, t));
        }
        [TestMethod]
        public void GetQualityProfilesAsync_WhenCancellationRequested_ThrowsException()
        {
            var request = new QualityProfileRequest { ProjectKey = "project" };
            Method_WhenCancellationRequested_ThrowsException((c, t) => c.GetQualityProfilesAsync(request, t));
        }
        [TestMethod]
        public void GetRoslynExportProfileAsync_WhenCancellationRequested_ThrowsException()
        {
            var request = new RoslynExportProfileRequest { QualityProfileName = "qp", LanguageKey = "cs" };
            Method_WhenCancellationRequested_ThrowsException((c, t) => c.GetRoslynExportProfileAsync(request, t));
        }
        [TestMethod]
        public void GetVersionAsync_WhenCancellationRequested_ThrowsException()
        {
            Method_WhenCancellationRequested_ThrowsException((c, t) => c.GetVersionAsync(t));
        }
        [TestMethod]
        public void ValidateCredentialsAsync_WhenCancellationRequested_ThrowsException()
        {
            Method_WhenCancellationRequested_ThrowsException((c, t) => c.ValidateCredentialsAsync(t));
        }

        private void Method_WhenCancellationRequested_ThrowsException<T>(
            Func<SonarQubeClient, CancellationToken, Task<Result<T>>> call)
        {
            // Arrange
            var httpHandler = new Mock<HttpMessageHandler>();
            httpHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .Returns(Task.FromResult(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent("")
                }));
            var connection = new ConnectionRequest { ServerUri = new Uri("http://mysq.com/") };
            var client = new SonarQubeClient(connection, httpHandler.Object, TimeSpan.FromSeconds(10));
            var cancellationToken = new CancellationTokenSource();
            cancellationToken.Cancel();

            // Act & Assert
            Func<Task<Result<T>>> funct = async () => await call(client, cancellationToken.Token);

            // Assert
            funct.ShouldThrow<OperationCanceledException>();
        }
        #endregion

        #region Check thrown exception is propagated
        [TestMethod]
        public void GetComponentsSearchProjectsAsync_WhenExceptionThrown_PropagateIt()
        {
            var request = new ComponentRequest { OrganizationKey = "org", Page = 42, PageSize = 25 };
            Method_WhenExceptionThrown_PropagateIt((c, t) => c.GetComponentsSearchProjectsAsync(request, t));
        }
        [TestMethod]
        public void GetIssuesAsync_WhenExceptionThrown_PropagateIt()
        {
            Method_WhenExceptionThrown_PropagateIt((c, t) => c.GetIssuesAsync("key", t));
        }
        [TestMethod]
        public void GetOrganizationsAsync_WhenExceptionThrown_PropagateIt()
        {
            var request = new OrganizationRequest { Page = 42, PageSize = 25 };
            Method_WhenExceptionThrown_PropagateIt((c, t) => c.GetOrganizationsAsync(request, t));
        }
        [TestMethod]
        public void GetPluginsAsync_WhenExceptionThrown_PropagateIt()
        {
            Method_WhenExceptionThrown_PropagateIt((c, t) => c.GetPluginsAsync(t));
        }
        [TestMethod]
        public void GetProjectsAsync_WhenExceptionThrown_PropagateIt()
        {
            Method_WhenExceptionThrown_PropagateIt((c, t) => c.GetProjectsAsync(t));
        }
        [TestMethod]
        public void GetPropertiesAsync_WhenExceptionThrown_PropagateIt()
        {
            Method_WhenExceptionThrown_PropagateIt((c, t) => c.GetPropertiesAsync(t));
        }
        [TestMethod]
        public void GetQualityProfileChangeLogAsync_WhenExceptionThrown_PropagateIt()
        {
            var request = new QualityProfileChangeLogRequest { QualityProfileKey = "qp", PageSize = 25 };
            Method_WhenExceptionThrown_PropagateIt((c, t) => c.GetQualityProfileChangeLogAsync(request, t));
        }
        [TestMethod]
        public void GetQualityProfilesAsync_WhenExceptionThrown_PropagateIt()
        {
            var request = new QualityProfileRequest { ProjectKey = "project" };
            Method_WhenExceptionThrown_PropagateIt((c, t) => c.GetQualityProfilesAsync(request, t));
        }
        [TestMethod]
        public void GetRoslynExportProfileAsync_WhenExceptionThrown_PropagateIt()
        {
            var request = new RoslynExportProfileRequest { QualityProfileName = "qp", LanguageKey = "cs" };
            Method_WhenExceptionThrown_PropagateIt((c, t) => c.GetRoslynExportProfileAsync(request, t));
        }
        [TestMethod]
        public void GetVersionAsync_WhenExceptionThrown_PropagateIt()
        {
            Method_WhenExceptionThrown_PropagateIt((c, t) => c.GetVersionAsync(t));
        }
        [TestMethod]
        public void ValidateCredentialsAsync_WhenExceptionThrown_PropagateIt()
        {
            Method_WhenExceptionThrown_PropagateIt((c, t) => c.ValidateCredentialsAsync(t));
        }

        private void Method_WhenExceptionThrown_PropagateIt<T>(Func<SonarQubeClient, CancellationToken, Task<Result<T>>> call)
        {
            // Arrange
            var expectedException = new Exception("foo text.");

            var httpHandler = new Mock<HttpMessageHandler>();
            httpHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .Returns(() => { throw expectedException; });
            var connection = new ConnectionRequest { ServerUri = new Uri("http://mysq.com/") };
            var client = new SonarQubeClient(connection, httpHandler.Object, TimeSpan.FromSeconds(10));

            // Act & Assert
            Func<Task<Result<T>>> funct = async () => await call(client, CancellationToken.None);

            // Assert
            funct.ShouldThrow<Exception>().And.Message.Should().Be(expectedException.Message);
        }
        #endregion

        #region Notifications

        [TestMethod]
        public async Task GetNotificationEventsAsync_CallsExpectedUrl()
        {
            var testDate = new DateTimeOffset(2000, 1, 1, 0, 0, 0, TimeSpan.FromHours(1));
            var request = new NotificationsRequest { ProjectKey = "project", EventsSince = testDate };
            var expectedUri = new Uri("api/developers/search_events?projects=project&from=2000-01-01T00:00:00%2b0100", UriKind.RelativeOrAbsolute);
            await Method_CallsTheExpectedUri(expectedUri, @"{""events"": [] }",
                (c, t) => c.GetNotificationEventsAsync(request, t));
        }

        [TestMethod]
        public async Task GetNotificationEventsAsync_GetEventsFromTheServer()
        {
            // Arrange
            var client = SetupClientWithHttpResponse(HttpStatusCode.OK,
                @"{""events"":
[{""category"":""QUALITY_GATE"",
""message"":""Quality Gate of project 'test' is now Red (was Green)"",
""link"":""http://localhost:9000/dashboard?id=test"",
""project"":""test"",
""date"":""2017-09-14T10:55:19+0200""}]}");

            var request = new NotificationsRequest
            {
                ProjectKey = "test",
                EventsSince = DateTimeOffset.Parse("2017-09-14T10:00:00+0200", CultureInfo.InvariantCulture)
            };

            var expected = new NotificationsResponse[]
            {
                new NotificationsResponse
                {
                    Category = "QUALITY_GATE",
                    Message =  "Quality Gate of project 'test' is now Red (was Green)",
                    Link = new Uri("http://localhost:9000/dashboard?id=test"),
                    Date = new DateTimeOffset(2017, 9, 14, 10, 55, 19, 0, TimeSpan.FromHours(2)),
                    Project = "test"
                }
            };

            // Act
            var result = await client
                .GetNotificationEventsAsync(request, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            AssertEqual(expected, result.Value);
        }

        private static async Task<Result<NotificationsResponse[]>> GetNotificationEventsAsync_WithHttpStatusCode(
            HttpStatusCode returnedStatusCode)
        {
            // Arrange
            var responseContent =
                returnedStatusCode == HttpStatusCode.OK ? "{\"events\":[]}" : null;

            var client = SetupClientWithHttpResponse(returnedStatusCode, responseContent);
            var request = new NotificationsRequest { ProjectKey = "project", EventsSince = DateTimeOffset.Now };

            // Act
            return await client.GetNotificationEventsAsync(request, CancellationToken.None);
        }

        private static SonarQubeClient SetupClientWithHttpResponse(HttpStatusCode code, string responseText)
        {
            var response = new HttpResponseMessage(code);
            if (responseText != null)
            {
                response.Content = new StringContent(responseText);
            }

            var httpHandler = new Mock<HttpMessageHandler>();
            httpHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .Returns(Task<HttpResponseMessage>.Factory.StartNew(() => response));

            var connection = new ConnectionRequest { ServerUri = new Uri("http://mysq.com/") };
            return new SonarQubeClient(connection, httpHandler.Object, TimeSpan.FromSeconds(10));
        }

        private void AssertEqual(NotificationsResponse x, NotificationsResponse y, int itemIndex)
        {
            Assert.AreEqual(x.Category, y.Category, string.Format("Category, item {0}", itemIndex));
            Assert.AreEqual(x.Date, y.Date, string.Format("Date, item {0}", itemIndex));
            Assert.AreEqual(x.Link, y.Link, string.Format("Link, item {0}", itemIndex));
            Assert.AreEqual(x.Message, y.Message, string.Format("Message, item {0}", itemIndex));
            Assert.AreEqual(x.Project, y.Project, string.Format("Project, item {0}", itemIndex));
        }

        private void AssertEqual(NotificationsResponse[] expected, NotificationsResponse[] actual)
        {
            Assert.AreEqual(expected.Length, actual.Length);

            for (int i = 0; i < expected.Length; i++)
            {
                AssertEqual(expected[i], actual[i], i);
            }
        }

        #endregion

        #region Dispose

        [TestMethod]
        public void Dispose_ShouldDisposeTheRightObject()
        {
            // Arrange
            var httpHandler = new Mock<HttpMessageHandler>();
            httpHandler.Protected().Setup("Dispose", true).Verifiable();
            var connection = new ConnectionRequest { ServerUri = new Uri("http://mysq.com/") };
            var client = new SonarQubeClient(connection, httpHandler.Object, TimeSpan.FromSeconds(2));

            // Act
            client.Dispose();

            // Assert
            httpHandler.VerifyAll();
        }

        [TestMethod]
        public void Dispose_WhenCalledMultipleTimes_DoesNotThrow()
        {
            // Arrange
            var httpHandler = new Mock<HttpMessageHandler>();
            int callCount = 0;
            httpHandler.Protected().Setup("Dispose", true).Callback(() => callCount++);
            var connection = new ConnectionRequest { ServerUri = new Uri("http://mysq.com/") };
            var client = new SonarQubeClient(connection, httpHandler.Object, TimeSpan.FromSeconds(2));
            client.Dispose();

            // Act
            client.Dispose();

            // Assert
            httpHandler.VerifyAll();
            callCount.Should().Be(1);
        }

        #endregion // Dispose

        #region Check returns default if not success code
        [TestMethod]
        public async Task GetComponentsSearchProjectsAsync_WhenRequestIsNotSuccess_ReturnsDefaultAndErrorCode()
        {
            var request = new ComponentRequest { OrganizationKey = "org", Page = 42, PageSize = 25 };
            await Method_WhenRequestIsNotSuccess_ReturnsDefaultAndErrorCode(
                (c, t) => c.GetComponentsSearchProjectsAsync(request, t), HttpStatusCode.InternalServerError);
        }
        [TestMethod]
        public async Task GetIssuesAsync_WhenRequestIsNotSuccess_ReturnsDefaultAndErrorCode()
        {
            await Method_WhenRequestIsNotSuccess_ReturnsDefaultAndErrorCode(
                (c, t) => c.GetIssuesAsync("key", t), HttpStatusCode.InternalServerError);
        }
        [TestMethod]
        public async Task GetOrganizationsAsync_WhenRequestIsNotSuccess_ReturnsDefaultAndErrorCode()
        {
            var request = new OrganizationRequest { Page = 42, PageSize = 25 };
            await Method_WhenRequestIsNotSuccess_ReturnsDefaultAndErrorCode(
                (c, t) => c.GetOrganizationsAsync(request, t), HttpStatusCode.InternalServerError);
        }
        [TestMethod]
        public async Task GetPluginsAsync_WhenRequestIsNotSuccess_ReturnsDefaultAndErrorCode()
        {
            await Method_WhenRequestIsNotSuccess_ReturnsDefaultAndErrorCode(
                (c, t) => c.GetPluginsAsync(t), HttpStatusCode.InternalServerError);
        }
        [TestMethod]
        public async Task GetProjectsAsync_WhenRequestIsNotSuccess_ReturnsDefaultAndErrorCode()
        {
            await Method_WhenRequestIsNotSuccess_ReturnsDefaultAndErrorCode(
                (c, t) => c.GetProjectsAsync(t), HttpStatusCode.InternalServerError);
        }
        [TestMethod]
        public async Task GetPropertiesAsync_WhenRequestIsNotSuccess_ReturnsDefaultAndErrorCode()
        {
            await Method_WhenRequestIsNotSuccess_ReturnsDefaultAndErrorCode(
                (c, t) => c.GetPropertiesAsync(t), HttpStatusCode.InternalServerError);
        }
        [TestMethod]
        public async Task GetQualityProfileChangeLogAsync_WhenRequestIsNotSuccess_ReturnsDefaultAndErrorCode()
        {
            var request = new QualityProfileChangeLogRequest { QualityProfileKey = "qp", PageSize = 25 };
            await Method_WhenRequestIsNotSuccess_ReturnsDefaultAndErrorCode(
                (c, t) => c.GetQualityProfileChangeLogAsync(request, t), HttpStatusCode.InternalServerError);
        }
        [TestMethod]
        public async Task GetQualityProfilesAsync_WhenRequestIsNotSuccess_ReturnsDefaultAndErrorCode()
        {
            var request = new QualityProfileRequest { ProjectKey = "project" };
            await Method_WhenRequestIsNotSuccess_ReturnsDefaultAndErrorCode(
                (c, t) => c.GetQualityProfilesAsync(request, t), HttpStatusCode.InternalServerError);
        }
        [TestMethod]
        public async Task GetRoslynExportProfileAsync_WhenRequestIsNotSuccess_ReturnsDefaultAndErrorCode()
        {
            var request = new RoslynExportProfileRequest { QualityProfileName = "qp", LanguageKey = "cs" };
            await Method_WhenRequestIsNotSuccess_ReturnsDefaultAndErrorCode(
                (c, t) => c.GetRoslynExportProfileAsync(request, t), HttpStatusCode.InternalServerError);
        }
        [TestMethod]
        public async Task GetVersionAsync_WhenRequestIsNotSuccess_ReturnsDefaultAndErrorCode()
        {
            await Method_WhenRequestIsNotSuccess_ReturnsDefaultAndErrorCode(
                (c, t) => c.GetVersionAsync(t), HttpStatusCode.InternalServerError);
        }
        [TestMethod]
        public async Task ValidateCredentialsAsync_WhenRequestIsNotSuccess_ReturnsDefaultAndErrorCode()
        {
            await Method_WhenRequestIsNotSuccess_ReturnsDefaultAndErrorCode(
                (c, t) => c.ValidateCredentialsAsync(t), HttpStatusCode.InternalServerError);
        }

        private async Task Method_WhenRequestIsNotSuccess_ReturnsDefaultAndErrorCode<T>(
            Func<SonarQubeClient, CancellationToken, Task<Result<T>>> call, HttpStatusCode errorStatusCode)
        {
            var httpHandler = new Mock<HttpMessageHandler>();
            httpHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .Returns(Task.FromResult(new HttpResponseMessage
                {
                    StatusCode = errorStatusCode,
                    Content = null
                }));

            var connection = new ConnectionRequest { ServerUri = new Uri("http://mysq.com/") };
            var client = new SonarQubeClient(connection, httpHandler.Object, TimeSpan.FromSeconds(10));

            // Act
            var result = await call(client, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Value.Should().Be(default(T));
            result.StatusCode.Should().Be(errorStatusCode);
        }
        #endregion
    }
}

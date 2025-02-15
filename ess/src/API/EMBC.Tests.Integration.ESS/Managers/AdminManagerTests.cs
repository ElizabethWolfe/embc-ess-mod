﻿using System.Threading.Tasks;
using EMBC.ESS.Managers.Admin;
using EMBC.ESS.Shared.Contracts.Metadata;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace EMBC.Tests.Integration.ESS.Managers
{
    public class AdminManagerTests : DynamicsWebAppTestBase
    {
        public AdminManagerTests(ITestOutputHelper output, DynamicsWebAppFixture fixture) : base(output, fixture)
        { }

        [Fact(Skip = RequiresVpnConnectivity)]
        public async Task CanGetCountries()
        {
            var manager = Services.GetRequiredService<AdminManager>();

            var reply = await manager.Handle(new CountriesQuery());

            reply.ShouldNotBeNull().Items.ShouldNotBeEmpty();
            reply.Items.ShouldAllBe(c => c.Code != null && c.Name != null);
        }

        [Fact(Skip = RequiresVpnConnectivity)]
        public async Task CanGetStateProvinces()
        {
            var manager = Services.GetRequiredService<AdminManager>();

            var reply = await manager.Handle(new StateProvincesQuery());

            reply.ShouldNotBeNull().Items.ShouldNotBeEmpty();
            reply.Items.ShouldAllBe(c => c.Code != null && c.Name != null && c.CountryCode != null);
        }

        [Fact(Skip = RequiresVpnConnectivity)]
        public async Task CanGetCommunities()
        {
            var manager = Services.GetRequiredService<AdminManager>();

            var reply = await manager.Handle(new CommunitiesQuery());

            reply.ShouldNotBeNull().Items.ShouldNotBeEmpty();
            reply.Items.ShouldAllBe(c => c.Code != null && c.Name != null && c.CountryCode != null);
        }

        [Fact(Skip = RequiresVpnConnectivity)]
        public async Task CanGetSecurityQuestions()
        {
            var manager = Services.GetRequiredService<AdminManager>();

            var reply = await manager.Handle(new SecurityQuestionsQuery());

            reply.ShouldNotBeNull().Items.ShouldNotBeEmpty();
            reply.Items.ShouldAllBe(c => !string.IsNullOrEmpty(c));
        }
    }
}

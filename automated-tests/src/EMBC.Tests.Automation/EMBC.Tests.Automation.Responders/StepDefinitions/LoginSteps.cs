﻿using Microsoft.Extensions.Configuration;

namespace EMBC.Tests.Automation.Responders.StepDefinitions
{
    [Binding]
    public class Steps
    {
        private readonly IEnumerable<User> users;
        private readonly Bceid bceid;

        public Steps(BrowserDriver browserDriver)
        {
            users = browserDriver.Configuration.GetSection("users").Get<IEnumerable<User>>();
            bceid = new Bceid(browserDriver.Current);
        }

        [StepDefinition(@"I log in with BCeID user (.*)")]
        public void Bcsc(string userName)
        {
            var user = users.SingleOrDefault(u => u.UserName.Equals(userName, StringComparison.OrdinalIgnoreCase));

            if (user == null) throw new InvalidOperationException($"User {userName} not found in the test configuration");
            bceid.Wait(5000);
            bceid.Login(user.UserName, user.Password);
            bceid.Wait(5000);
        }
    }

    public class User
    {
        public string UserName { get; set; } = null!;
        public string Password { get; set; } = null!;
        public string Role { get; set; } = null!;
    }
}
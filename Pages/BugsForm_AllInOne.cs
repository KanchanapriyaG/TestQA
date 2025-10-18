using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Playwright;
using NUnit.Framework;

namespace QaBugsForm.Tests
{
    internal class BugsFormPage
    {
        private readonly IPage _page;

        public BugsFormPage(IPage page) => _page = page;

        public ILocator FirstName => _page.GetByLabel("First Name", new() { Exact = false });
        public ILocator LastName => _page.GetByLabel("Last Name", new() { Exact = false });
        public ILocator Phone => _page.Locator("input[type='tel'], input[name*='phone' i], input[placeholder*='phone' i]").First;
        public ILocator Email => _page.Locator("input[type='email'], input[placeholder*='email' i], input[name*='email' i]").First;
        public ILocator Password => _page.Locator("input[type='password'], input[name*='pass' i], input[placeholder*='pass' i]").First;
        public ILocator Country => _page.Locator("select").First;
        public ILocator Terms => _page.GetByRole(AriaRole.Checkbox, new() { NameRegex = new Regex("terms", RegexOptions.IgnoreCase) });
        public ILocator RegisterBtn => _page.GetByRole(AriaRole.Button, new() { NameRegex = new Regex("register", RegexOptions.IgnoreCase) });
        public ILocator Errors => _page.Locator("[role='alert'], .error, .invalid-feedback, .error-message");

        public async Task Open()
        {
            await _page.GotoAsync("https://qa-practice.netlify.app/bugs-form");
            await _page.GetByRole(AriaRole.Heading, new() { NameRegex = new Regex("spot the bugs", RegexOptions.IgnoreCase) }).WaitForAsync();
            await Country.WaitForAsync(new() { State = WaitForSelectorState.Visible });
            await Email.WaitForAsync(new() { State = WaitForSelectorState.Visible });
        }

        public async Task AgreeTerms()
        {
            if (await Terms.IsEnabledAsync())
            {
                await Terms.CheckAsync();
                return;
            }
            await Terms.EvaluateAsync("el => el.removeAttribute('disabled')");
            await Terms.CheckAsync();
        }

        public async Task FillForm(string first = "Priya", string last = "Ganesh", string phone = "0212345678", string country = "New Zealand", string email = "priya@example.com", string pwd = "GoodPwd1", bool agree = false)
        {
            await FirstName.FillAsync(first);
            await LastName.FillAsync(last);
            await Phone.FillAsync(phone);
            await Country.SelectOptionAsync(new SelectOptionValue { Label = country });
            await Email.FillAsync(email);
            await Password.FillAsync(pwd);
            if (agree) await AgreeTerms();
        }

        public Task Submit() => RegisterBtn.ClickAsync();
        public Task NoErrors() => Microsoft.Playwright.Assertions.Expect(Errors).ToHaveCountAsync(0);
    }

    [TestFixture]
    public class BugsFormTests
    {
        private IPlaywright _pw = default!;
        private IBrowser _browser = default!;
        private IBrowserContext _context = default!;
        private IPage _page = default!;
        private BugsFormPage _form = default!;

        private static ILocatorAssertions Expect(ILocator locator) => Microsoft.Playwright.Assertions.Expect(locator);

        [OneTimeSetUp]
        public async Task Init()
        {
            _pw = await Playwright.CreateAsync();
            _browser = await _pw.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = false, SlowMo = 200 });
        }

        [SetUp]
        public async Task SetUp()
        {
            _context = await _browser.NewContextAsync(new() { BaseURL = "https://qa-practice.netlify.app", ViewportSize = new() { Width = 1366, Height = 768 } });
            _page = await _context.NewPageAsync();
            _form = new BugsFormPage(_page);
            await _form.Open();
        }

        [TearDown]
        public async Task TearDown()
        {
            try
            {
                var status = TestContext.CurrentContext.Result.Outcome.Status;
                if (status == NUnit.Framework.Interfaces.TestStatus.Failed)
                {
                    var dir = Path.Combine(TestContext.CurrentContext.WorkDirectory, "TestResults", "screenshots");
                    Directory.CreateDirectory(dir);
                    var name = Regex.Replace(TestContext.CurrentContext.Test.Name, @"[^\w\-\.]+", "_");
                    var path = Path.Combine(dir, $"{name}.png");
                    await _page.ScreenshotAsync(new() { Path = path, FullPage = true });
                    TestContext.AddTestAttachment(path, "Screenshot on failure");
                }
            }
            catch { }
            await _context.CloseAsync();
        }

        [OneTimeTearDown]
        public async Task CleanUp()
        {
            await _browser.CloseAsync();
            _pw.Dispose();
        }

        [Test, Category("Critical")]
        public async Task EmptySubmitShowsErrors()
        {
            await _form.Submit();
            await Expect(_form.Errors.First).ToBeVisibleAsync();
        }

        [Test, Category("Critical")]
        public async Task PhoneTooShortShowsError()
        {
            await _form.FillForm(phone: "123456789", agree: false);
            await _form.Submit();
            await Expect(_form.Errors).ToContainTextAsync(new Regex("10"));
        }

        [Test, Category("Critical")]
        public async Task NonDigitsInPhoneShowsError()
        {
            await _form.FillForm(phone: "12ab!@#456", agree: false);
            await _form.Submit();
            await Expect(_form.Errors).Not.ToHaveCountAsync(0);
        }

        [TestCase("a@b"), TestCase("noatsign"), TestCase("a@b."), TestCase("user@domain..com"), Category("Critical")]
        public async Task InvalidEmailFormatsShowError(string badEmail)
        {
            await _form.FillForm(email: badEmail, agree: false);
            await _form.Submit();
            await Expect(_form.Errors).Not.ToHaveCountAsync(0);
        }

        [Test, Category("Critical")]
        public async Task PasswordTooShortShowsError()
        {
            await _form.FillForm(pwd: "Abc12", agree: false);
            await _form.Submit();
            await Expect(_form.Errors).Not.ToHaveCountAsync(0);
        }

        [Test, Category("Critical")]
        public async Task PasswordTooLongShowsError()
        {
            await _form.FillForm(pwd: "A234567890B234567890x", agree: false);
            await _form.Submit();
            await Expect(_form.Errors).Not.ToHaveCountAsync(0);
        }

        [Test, Category("Critical")]
        public async Task UntickedTermsBlocksSubmit()
        {
            await _form.FillForm(agree: false);
            await _form.Submit();
            await Expect(_form.Errors).Not.ToHaveCountAsync(0);
        }

        [Test, Category("Critical")]
        public async Task DefaultCountryNotAccepted()
        {
            await _form.FirstName.FillAsync("Priya");
            await _form.LastName.FillAsync("Ganesh");
            await _form.Phone.FillAsync("0212345678");
            await _form.Email.FillAsync("priya@example.com");
            await _form.Password.FillAsync("GoodPwd1");
            await _form.Submit();
            await Expect(_form.Errors).Not.ToHaveCountAsync(0);
        }

        [Test, Category("High")]
        public async Task PhoneNumberTypoIsVisible()
        {
            await Microsoft.Playwright.Assertions.Expect(_page.GetByText(new Regex(@"phone\s*nunber", RegexOptions.IgnoreCase))).ToBeVisibleAsync();
        }

        [Test, Category("High")]
        public async Task LastNamePersistsAfterError()
        {
            await _form.FillForm(email: "bad", agree: false);
            await _form.Submit();
            await Microsoft.Playwright.Assertions.Expect(_form.LastName).ToHaveValueAsync("Ganesh");
        }
    }
}

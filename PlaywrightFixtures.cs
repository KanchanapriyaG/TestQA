using System.Threading.Tasks;
using Microsoft.Playwright;
using NUnit.Framework;


public class PlaywrightFixtures
{
    public static IPlaywright PW = default!;
    public static IBrowser Browser = default!;

 
    public async Task GlobalSetup()
    {
        PW = await Microsoft.Playwright.Playwright.CreateAsync();
        Browser = await PW.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = false,
        
        });
    }

    public async Task GlobalTeardown()
    {
        await Browser.CloseAsync();
        PW.Dispose();
    }
}


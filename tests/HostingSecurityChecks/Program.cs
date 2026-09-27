using System.Net;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Rincon.Infrastructure;

var keys = Path.Combine(Path.GetTempPath(), "rincon-security-checks-" + Guid.NewGuid().ToString("N"));
WebApplication CreateApp()
{
    var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Production" });
    builder.Logging.ClearProviders();
    builder.Configuration["DataProtection:KeysPath"] = keys;
    builder.WebHost.UseUrls("http://127.0.0.1:0");
    builder.AddHostingSecurity();
    builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
        .AddCookie(options => options.LoginPath = "/Identity/Account/Login");
    builder.Services.AddAuthorization();
    builder.Services.AddDistributedMemoryCache();
    builder.Services.AddSession();
    return builder.Build();
}
void Check(bool condition, string name)
{
    if (!condition) throw new Exception("FAIL: " + name);
    Console.WriteLine("PASS: " + name);
}

await using (var app = CreateApp())
{
    app.VerifyDataProtection();
    // Test-only simulation of a non-local peer; never part of the application.
    app.Use(async (context, next) =>
    {
        if (context.Request.Path == "/untrusted")
            context.Connection.RemoteIpAddress = IPAddress.Parse("203.0.113.10");
        await next(context);
    });
    app.UseForwardedHeaders();
    app.UseProductionErrorHandler();
    app.UseHttpsRedirection();
    app.UseRouting();
    var errorPasses = 0;
    app.Use(async (context, next) =>
    {
        if (context.Request.Path == "/error")
        {
            errorPasses++;
            throw new System.Security.Cryptography.CryptographicException("test-secret");
        }
        await next(context);
    });
    app.UseSession();
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapGet("/", () => "protected").RequireAuthorization();
    app.MapGet("/untrusted", () => "must redirect");
    await app.StartAsync();
    using var client = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false });
    async Task<HttpResponseMessage> Get(string path)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, app.Urls.Single() + path);
        request.Headers.Host = "rinconweb.online";
        request.Headers.Add("X-Forwarded-Proto", "https");
        return await client.SendAsync(request);
    }
    using var login = await Get("/");
    Check(login.StatusCode == HttpStatusCode.Redirect &&
        login.Headers.Location?.ToString() == "https://rinconweb.online/Identity/Account/Login?ReturnUrl=%2F",
        "Nginx HTTPS challenge preserves HTTPS and ReturnUrl");
    using var untrusted = await Get("/untrusted");
    Check(untrusted.StatusCode == HttpStatusCode.TemporaryRedirect,
        "Untrusted peer cannot spoof HTTPS");
    using var error = await Get("/error");
    var body = await error.Content.ReadAsStringAsync();
    Check(error.StatusCode == HttpStatusCode.InternalServerError && errorPasses == 1 &&
        !body.Contains("test-secret") && body.Contains("Referencia:"),
        "Error branch does not reexecute failing middleware or disclose exception");

    var protectedValue = app.Services.GetRequiredService<IDataProtectionProvider>()
        .CreateProtector("restart-check").Protect("persisted");
    var manager = app.Services.GetRequiredService<IKeyManager>();
    var oldKeyCount = manager.GetAllKeys().Count;
    manager.CreateNewKey(DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(90));
    Check(manager.GetAllKeys().Count == oldKeyCount + 1,
        "Key rotation writes a new key without deleting old keys");
    await app.StopAsync();
    await using var restarted = CreateApp();
    restarted.VerifyDataProtection();
    Check(restarted.Services.GetRequiredService<IDataProtectionProvider>()
        .CreateProtector("restart-check").Unprotect(protectedValue) == "persisted",
        "New application instance reads the existing key ring");
}
var invalid = WebApplication.CreateBuilder();
invalid.Configuration["DataProtection:KeysPath"] = "relative-keys";
try
{
    invalid.AddHostingSecurity();
    throw new Exception("FAIL: relative path accepted");
}
catch (InvalidOperationException)
{
    Console.WriteLine("PASS: ambiguous relative key path rejected");
}
Console.WriteLine("All hosting security checks passed. Temporary test keys: " + keys);

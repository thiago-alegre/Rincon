using System.Net;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;

namespace Rincon.Infrastructure;

public static class HostingSecurity
{
    public static void AddHostingSecurity(this WebApplicationBuilder builder)
    {
        builder.Services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            options.ForwardLimit = 1;
            // Nginx connects locally. Never trust forwarded headers from arbitrary clients.
            options.KnownNetworks.Clear();
            options.KnownProxies.Clear();
            options.KnownProxies.Add(IPAddress.Loopback);
            options.KnownProxies.Add(IPAddress.IPv6Loopback);
            options.KnownProxies.Add(IPAddress.Loopback.MapToIPv6());
        });

        if (builder.Environment.IsProduction())
            builder.Services.AddHttpsRedirection(options => options.HttpsPort = 443);

        var configuredPath = builder.Configuration["DataProtection:KeysPath"];
        if (!string.IsNullOrWhiteSpace(configuredPath) && !Path.IsPathFullyQualified(configuredPath))
            throw new InvalidOperationException("DataProtection:KeysPath must be an absolute path.");

        // Preserve the existing key ring until deployment explicitly migrates it.
        var keysPath = string.IsNullOrWhiteSpace(configuredPath)
            ? Path.Combine(builder.Environment.ContentRootPath, "DataProtectionKeys")
            : configuredPath;
        Directory.CreateDirectory(keysPath);
        // Existing keys can be readable even when the next rotation cannot write.
        using (var probe = new FileStream(
            Path.Combine(keysPath, ".write-check-" + Guid.NewGuid().ToString("N")),
            FileMode.CreateNew, FileAccess.Write, FileShare.None, 1, FileOptions.DeleteOnClose))
        {
            probe.WriteByte(0);
            probe.Flush(flushToDisk: true);
        }
        builder.Services.AddDataProtection()
            .PersistKeysToFileSystem(new DirectoryInfo(keysPath))
            .SetApplicationName("Rincon");
    }

    public static void VerifyDataProtection(this WebApplication app)
    {
        // Exercise assembly loading and key protection before accepting requests.
        // This neither deletes old keys nor changes their normal rotation schedule.
        var protector = app.Services.GetRequiredService<IDataProtectionProvider>()
            .CreateProtector("Rincon.StartupVerification.v1");
        var value = Guid.NewGuid().ToString("N");
        if (protector.Unprotect(protector.Protect(value)) != value)
            throw new InvalidOperationException("Data Protection startup verification failed.");
    }

    public static void UseProductionErrorHandler(this WebApplication app)
    {
        // A terminal branch avoids running Session again when Data Protection fails.
        app.UseExceptionHandler(errorApp => errorApp.Run(async context =>
        {
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "text/plain; charset=utf-8";
            context.Response.Headers.CacheControl = "no-store";
            await context.Response.WriteAsync(
                "No se pudo completar la operación. Contactá al administrador. Referencia: "
                + context.TraceIdentifier);
        }));
    }
}

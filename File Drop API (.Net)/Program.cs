using Microsoft.AspNetCore.Http.Features;
using System.Text.RegularExpressions;

var builder = WebApplication.CreateBuilder(args);

var filesRoot = builder.Configuration["FilesRootFolder"]
    ?? throw new Exception("FilesRootFolder missing in appsettings.json");

var apiKey = builder.Configuration["ApiKey"]
    ?? throw new Exception("ApiKey missing in appsettings.json");

// Optional: allow large uploads
builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 1024L * 1024L * 1024L; // 1GB
});

var app = builder.Build();

// Ensure root exists
Directory.CreateDirectory(filesRoot);

// API key middleware
app.Use(async (ctx, next) =>
{
    if (ctx.Request.Path.StartsWithSegments("/upload") ||
        ctx.Request.Path.StartsWithSegments("/download"))
    {
        if (!ctx.Request.Headers.TryGetValue("x-api-key", out var provided) ||
            provided != apiKey)
        {
            ctx.Response.StatusCode = 401;
            await ctx.Response.WriteAsync("Unauthorized: invalid x-api-key");
            return;
        }
    }

    await next();
});

string Sanitize(string? input)
{
    if (string.IsNullOrWhiteSpace(input))
        return string.Empty;

    var clean = input.Replace("\\", "/");

    if (clean.Contains(".."))
        throw new Exception("Invalid path");

    clean = Regex.Replace(clean, @"[^a-zA-Z0-9\-_.\/ ]", "_");
    return clean.Trim('/').Trim();
}

string GetFullPath(string? folder, string filename)
{
    var f = Sanitize(folder);
    var n = Sanitize(filename);

    var baseDir = string.IsNullOrWhiteSpace(f)
        ? filesRoot
        : Path.Combine(filesRoot, f);

    Directory.CreateDirectory(baseDir);

    return Path.Combine(baseDir, n);
}

// -------------------------
// POST /upload
// -------------------------
app.MapPost("/upload", async (HttpRequest request) =>
{
    try
    {
        string? folder = request.Query["folder"];
        string? filename = request.Query["filename"];

        if (request.HasFormContentType)
        {
            var form = await request.ReadFormAsync();
            folder ??= form["folder"].FirstOrDefault();
            filename ??= form["filename"].FirstOrDefault();

            var file = form.Files.FirstOrDefault();
            if (file != null)
            {
                if (string.IsNullOrWhiteSpace(filename))
                    filename = file.FileName;

                var path = GetFullPath(folder, filename!);

                using var fs = File.Create(path);
                await file.CopyToAsync(fs);

                return Results.Ok(new { message = "Uploaded (multipart)", filename, folder });
            }
        }

        if (string.IsNullOrWhiteSpace(filename))
            return Results.BadRequest(new { error = "Missing ?filename=" });

        var fullPath = GetFullPath(folder, filename);

        using (var fs = File.Create(fullPath))
        {
            await request.Body.CopyToAsync(fs);
        }

        return Results.Ok(new { message = "Uploaded (raw)", filename, folder });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

// -------------------------
// GET /download
// -------------------------
app.MapGet("/download", async (HttpRequest req, HttpResponse res) =>
{
    try
    {
        var filename = req.Query["filename"].FirstOrDefault();
        var folder = req.Query["folder"].FirstOrDefault();

        if (string.IsNullOrWhiteSpace(filename))
        {
            res.StatusCode = 400;
            await res.WriteAsJsonAsync(new { error = "filename is required" });
            return;
        }

        var fullPath = GetFullPath(folder, filename);

        if (!File.Exists(fullPath))
        {
            res.StatusCode = 404;
            await res.WriteAsJsonAsync(new { error = "File not found" });
            return;
        }

        res.ContentType = "application/octet-stream";
        res.Headers.Append("Content-Disposition", $"attachment; filename=\"{filename}\"");

        await res.SendFileAsync(fullPath);
    }
    catch (Exception ex)
    {
        res.StatusCode = 400;
        await res.WriteAsJsonAsync(new { error = ex.Message });
    }
});

app.Run();
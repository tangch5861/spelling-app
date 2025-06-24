using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

var builder = WebApplication.CreateBuilder(args);

// Retrieve OpenAI API key from environment
var openAiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
if (string.IsNullOrWhiteSpace(openAiKey))
{
    throw new InvalidOperationException("OPENAI_API_KEY environment variable not configured.");
}

builder.Services.AddHttpClient();
var app = builder.Build();

// Simple in-memory store for lessons
var lessons = new Dictionary<int, Lesson>();

app.MapPost("/api/lessons/upload", async (HttpRequest request, IHttpClientFactory factory) =>
{
    if (!request.HasFormContentType)
        return Results.BadRequest("Form data expected.");

    var form = await request.ReadFormAsync();
    var file = form.Files["image"];
    if (file is null || file.Length == 0)
        return Results.BadRequest("Image file missing.");

    using var ms = new MemoryStream();
    await file.CopyToAsync(ms);
    var base64 = Convert.ToBase64String(ms.ToArray());

    try
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", openAiKey);

        var payload = new
        {
            model = "gpt-4-vision-preview",
            messages = new object[]
            {
                new { role = "system", content = "Extract all words from the image and respond with a JSON array." },
                new
                {
                    role = "user",
                    content = new object[] { new { type = "image_url", image_url = new { url = $"data:image/png;base64,{base64}" } } }
                }
            },
            max_tokens = 50
        };

        var response = await client.PostAsync(
            "https://api.openai.com/v1/chat/completions",
            new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json"));

        if (!response.IsSuccessStatusCode)
        {
            var detail = await response.Content.ReadAsStringAsync();
            return Results.Problem($"OpenAI vision failed: {response.StatusCode} {detail}");
        }

        var json = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json);
        var content = doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();

        string[] words;
        try
        {
            words = JsonSerializer.Deserialize<string[]>(content ?? "[]") ?? Array.Empty<string>();
        }
        catch
        {
            words = content?.Split(new[] { ' ', '\\n', '\\r', ',', ';' }, StringSplitOptions.RemoveEmptyEntries) ?? Array.Empty<string>();
        }

        return Results.Ok(new { words });
    }
    catch (Exception ex)
    {
        return Results.Problem(ex.Message);
    }
});

app.MapPost("/api/lessons", (Lesson lesson) =>
{
    lessons[lesson.Id] = lesson;
    return Results.Ok(lesson);
});

app.MapGet("/api/lessons/{id}", (int id) =>
{
    if (lessons.TryGetValue(id, out var lesson))
    {
        return Results.Ok(lesson);
    }
    return Results.NotFound();
});

app.MapPost("/api/lessons/{id}/speech", async (int id, SpeechResult result, IHttpClientFactory factory) =>
{
    if (!lessons.TryGetValue(id, out var lesson))
        return Results.NotFound();

    byte[] audio;
    try
    {
        audio = Convert.FromBase64String(result.AudioBase64);
    }
    catch
    {
        return Results.BadRequest("Invalid audio format");
    }

    try
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", openAiKey);

        var content = new MultipartFormDataContent();
        var bytes = new ByteArrayContent(audio);
        bytes.Headers.ContentType = new MediaTypeHeaderValue("audio/mpeg");
        content.Add(bytes, "file", "audio.mp3");
        content.Add(new StringContent("whisper-1"), "model");

        var response = await client.PostAsync("https://api.openai.com/v1/audio/transcriptions", content);
        if (!response.IsSuccessStatusCode)
        {
            var detail = await response.Content.ReadAsStringAsync();
            return Results.Problem($"OpenAI transcription failed: {response.StatusCode} {detail}");
        }

        var json = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json);
        var transcription = doc.RootElement.GetProperty("text").GetString() ?? string.Empty;

        var correct = lesson.Words.Count(w => transcription.Contains(w, StringComparison.OrdinalIgnoreCase));
        var score = lesson.Words.Length == 0 ? 0 : (double)correct / lesson.Words.Length;

        return Results.Ok(new { transcription, score });
    }
    catch (Exception ex)
    {
        return Results.Problem(ex.Message);
    }
});

app.MapPost("/api/lessons/{id}/handwriting", async (int id, HandwritingResult result, IHttpClientFactory factory) =>
{
    if (!lessons.TryGetValue(id, out var lesson))
        return Results.NotFound();

    byte[] img;
    try
    {
        img = Convert.FromBase64String(result.ImageBase64);
    }
    catch
    {
        return Results.BadRequest("Invalid image data");
    }

    try
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", openAiKey);

        var payload = new
        {
            model = "gpt-4-vision-preview",
            messages = new object[]
            {
                new { role = "system", content = $"Grade the handwriting for these words: {string.Join(", ", lesson.Words)}. Provide feedback as JSON." },
                new
                {
                    role = "user",
                    content = new object[] { new { type = "image_url", image_url = new { url = $"data:image/png;base64,{Convert.ToBase64String(img)}" } } }
                }
            },
            max_tokens = 100
        };

        var response = await client.PostAsync(
            "https://api.openai.com/v1/chat/completions",
            new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json"));

        if (!response.IsSuccessStatusCode)
        {
            var detail = await response.Content.ReadAsStringAsync();
            return Results.Problem($"OpenAI vision failed: {response.StatusCode} {detail}");
        }

        var json = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json);
        var feedback = doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();

        return Results.Ok(new { feedback });
    }
    catch (Exception ex)
    {
        return Results.Problem(ex.Message);
    }
});

app.Run();

record Lesson
{
    public int Id { get; set; }
    public string[] Words { get; set; } = Array.Empty<string>();
    public string? Metadata { get; set; }
}

record SpeechResult
{
    public string AudioBase64 { get; set; } = string.Empty;
}

record HandwritingResult
{
    public string ImageBase64 { get; set; } = string.Empty;
}

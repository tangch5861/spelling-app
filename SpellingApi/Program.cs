using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapPost("/api/lessons/upload", async (HttpRequest request) =>
{
    // Placeholder for OpenAI OCR integration
    return Results.Ok(new { words = new[] { "example" } });
});

app.MapPost("/api/lessons", (Lesson lesson) =>
{
    // Placeholder for saving lesson
    return Results.Ok(lesson);
});

app.MapGet("/api/lessons/{id}", (int id) =>
{
    // Placeholder for retrieving lesson
    return Results.Ok(new Lesson { Id = id, Words = new[] { "example" } });
});

app.MapPost("/api/lessons/{id}/speech", (int id, SpeechResult result) =>
{
    // Placeholder for OpenAI speech grading
    return Results.Ok();
});

app.MapPost("/api/lessons/{id}/handwriting", (int id, HandwritingResult result) =>
{
    // Placeholder for OpenAI handwriting grading
    return Results.Ok();
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

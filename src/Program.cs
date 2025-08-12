using Azure; // AzureKeyCredential
using Azure.AI.OpenAI;
using Microsoft.Extensions.Configuration;
using OpenAI.Chat;
using Spectre.Console;
using System.Linq;

// Load configuration (appsettings + user-secrets + env)
var config = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
    .AddUserSecrets(typeof(Program).Assembly, optional: true, reloadOnChange: false)
    .AddEnvironmentVariables()
    .Build();

var endpoint = config["AzureOpenAI:Endpoint"];
var apiKey = config["AzureOpenAI:ApiKey"];            // filled by user-secrets/env/appsettings
var deployment = config["AzureOpenAI:Deployment"];

if (string.IsNullOrWhiteSpace(endpoint) ||
    string.IsNullOrWhiteSpace(apiKey) ||
    string.IsNullOrWhiteSpace(deployment))
{
    AnsiConsole.MarkupLine("[red]Missing AzureOpenAI config. Check Endpoint/ApiKey/Deployment.[/]");
    return;
}

// Prompt (or take args)
var prompt = args.Length > 0
    ? string.Join(" ", args)
    : AnsiConsole.Prompt(new TextPrompt<string>("Enter your prompt:").PromptStyle("green"));

AnsiConsole.MarkupLine("[grey]Sending to Azure OpenAI...[/]");

// Use AzureOpenAIClient with AzureKeyCredential (correct for Azure.AI.OpenAI)
var client = new AzureOpenAIClient(new Uri(endpoint), new AzureKeyCredential(apiKey));
var chat = client.GetChatClient(deployment);

try
{
    var result = chat.CompleteChat(
    [
        new SystemChatMessage("You are a concise assistant."),
        new UserChatMessage(prompt)
    ]);

    var text = string.Join("", result.Value.Content
        .Where(p => p.Text is not null)
        .Select(p => p.Text));

    AnsiConsole.MarkupLine("\n[bold]Response:[/]");
    AnsiConsole.WriteLine(string.IsNullOrWhiteSpace(text) ? "(no text content)" : text);
}
catch (Exception ex)
{
    AnsiConsole.MarkupLine($"[red]Request failed:[/] {ex.Message}");
}

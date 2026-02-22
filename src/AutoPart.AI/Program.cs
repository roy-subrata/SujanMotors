using System.ComponentModel;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Agents.Orchestration;
using Microsoft.SemanticKernel.Agents.Orchestration.Sequential;
using Microsoft.SemanticKernel.Agents.Runtime.InProcess;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Azure.AI.OpenAI;
using OpenAI;
using Microsoft.VisualBasic; // ✅ Correct OpenAIClient namespace

// Load configuration
IConfiguration configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .Build();

var settingConfig = configuration.GetSection("SettingConfig").Get<SettingConfig>()
    ?? throw new ArgumentException("Failed to load SettingConfig from configuration.");

// Build kernel
var builder = Kernel.CreateBuilder()
    .AddOpenAIChatCompletion(settingConfig.OpenAIModel, new OpenAIClient(settingConfig.OpenAIKey));

builder.Services.AddSingleton<ProductInformation>();
var kernel = builder.Build();


// Create agent thread
ChatHistoryAgentThread agentThread = new();
var saleAgent = SaleAgent(kernel);




// var arguments = new KernelArguments() { { "name", "Subrata Roy" } };
// string userMessage = $"Hi My Name is {arguments["name"]}";


// await foreach (var response in saleAgent.InvokeAsync(userMessage, agentThread))
// {
//     Console.WriteLine($"Agent: {response.Message.Content}");
// }


// Initial greeting
await foreach (var response in saleAgent.InvokeAsync("Hi", agentThread))
{
    Console.WriteLine($"Agent: {response.Message.Content}");
}

// Conversation loop
do
{
    Console.Write("> ");
    var input = Console.ReadLine();
    if (string.IsNullOrWhiteSpace(input) || input.Equals("exit", StringComparison.OrdinalIgnoreCase))
    {
        break;
    }

    var message = new ChatMessageContent(AuthorRole.User, input);
    await foreach (ChatMessageContent response in saleAgent.InvokeAsync(message, agentThread))
    {
        Console.WriteLine($"Agent: {response.Content}");
    }
} while (true);

// Print conversation history
Console.WriteLine("Agent Conversation Thread List.");
await foreach (var msg in agentThread.GetMessagesAsync())
{
    Console.WriteLine($"{msg.Role}: {msg.Content}");
}

// Agent definition
ChatCompletionAgent SaleAgent(Kernel kernel)
{
    Kernel agentKernel = kernel.Clone();
    agentKernel.Plugins.AddFromObject(new ProductInformation());

    // Define the template configuration
    var templateConfig = new PromptTemplateConfig()
    {
        Template = """
        You are a sales assistant for an auto parts store.
        Always begin the very first response by greeting the user politely.
        The greeting must include the user's name, which is provided as {{$name}}.
        Do not making response halusuation if you dot not know the factual information.
        """,
        TemplateFormat = PromptTemplateConfig.SemanticKernelTemplateFormat // Key: enable template rendering
    };

    return new ChatCompletionAgent(templateConfig: templateConfig, templateFactory: new KernelPromptTemplateFactory())
    {
        Kernel = agentKernel,
        Name = "SaleAgent",
        Description = "An agent that assists customers in finding and purchasing auto parts.",

        // Provide the dynamic value here
        Arguments = new KernelArguments() { { "name", "John Doe" } }
    };
}

// Product plugin
public class ProductInformation
{
    [KernelFunction("GetProductDetailsByName")]
    [Description("Get details about a product given its name.")]
    public string GetProductDetails(string productName)
    {
        return productName.ToLower() switch
        {
            "brake pads" => "Brake Pads: High-quality ceramic brake pads that offer excellent stopping power and durability.",
            "oil filter" => "Oil Filter: Premium oil filter designed to keep your engine clean and running smoothly.",
            "air filter" => "Air Filter: High-efficiency air filter that improves engine performance and fuel efficiency.",
            _ => "Product not found."
        };
    }

    [KernelFunction("ListAvailableProducts")]
    [Description("List all available products.")]
    public List<string> ListAvailableProducts()
    {
        return new List<string>
        {
            "Brake Pads",
            "Oil Filter",
            "Air Filter"
        };
    }
}



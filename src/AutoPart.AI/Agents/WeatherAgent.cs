


using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;

public class WeatherAgent
{
    public ChatCompletionAgent Agent;
    public WeatherAgent(Kernel kernel)
    {
        Agent = new ChatCompletionAgent()
        {
            Instructions = "You are a weather assistant. Use the WeatherPlugin to get current weather information.",
            Kernel = kernel,
            Name = "WeatherAgent",
            Description = "An agent that provides weather information.",
        };


        
      //  kernel.Plugins.AddFromType<WeatherPlugin>();


   //   Agent.Kernel.Plugins.AddFromType<WeatherPlugin>();

    }
}



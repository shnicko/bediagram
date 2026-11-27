using System.Text;
using Microsoft.Extensions.Configuration;
using BeDiagram.Mcp;
using BeDiagram.Services;

namespace BeDiagram.Cli;

/// <summary>
/// <c>mcp</c>: runs BeDiagram as an MCP server on standard input / output, for Claude Desktop or Claude Code to start.
/// It reads and writes the same storage folder as the web app (<c>Storage:Folder</c> in appsettings.json, or the
/// <c>Storage__Folder</c> environment variable), or the one given with <c>--folder</c>. Runs until the client closes it.
/// </summary>
public class McpCommand : ICliCommand
{
    public string Name
    {
        get
        {
            return "mcp";
        }
    }

    public string Summary
    {
        get
        {
            return "Run as an MCP server for Claude (started by Claude Desktop or Claude Code, not by hand)";
        }
    }

    public string Usage
    {
        get
        {
            return "[--folder <diagrams folder>]   (default: the app's storage folder)";
        }
    }

    public async Task<int> RunAsync(CliArguments args, TextWriter output)
    {
        // Same settings as the web app: appsettings.json next to the program, then environment variables.
        ConfigurationBuilder builder = new ConfigurationBuilder();
        builder.SetBasePath(AppContext.BaseDirectory);
        builder.AddJsonFile("appsettings.json", true);
        builder.AddEnvironmentVariables();
        string? folder = args.Option("folder");
        if (folder is not null)
        {
            Dictionary<string, string?> overrides = new Dictionary<string, string?>();
            overrides["Storage:Folder"] = folder;
            builder.AddInMemoryCollection(overrides);
        }
        IConfiguration config = builder.Build();

        DiagramStore store = new DiagramStore(config);
        DiagramWorkspace workspace = new DiagramWorkspace(store);
        TextWriter log = Console.Error;
        McpServer server = new McpServer(McpToolRegistry.CreateDefault(workspace), McpInstructions.Text, log);
        log.WriteLine("BeDiagram MCP server: diagrams in " + store.Folder);

        // Standard output is the protocol channel, so open both streams as plain UTF-8 (no byte-order mark, whatever
        // the console's code page) and never write anything else to them.
        UTF8Encoding utf8 = new UTF8Encoding(false);
        using StreamReader input = new StreamReader(Console.OpenStandardInput(), utf8);
        using StreamWriter protocol = new StreamWriter(Console.OpenStandardOutput(), utf8);
        await server.RunAsync(input, protocol);
        log.WriteLine("BeDiagram MCP server: client closed the connection.");
        return 0;
    }
}

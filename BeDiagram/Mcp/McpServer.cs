using System.Text.Json;
using System.Text.Json.Nodes;

namespace BeDiagram.Mcp;

/// <summary>
/// A Model Context Protocol server over stdio, written without a library so every message is visible here.
/// <para>
/// The client (Claude Desktop, Claude Code) starts BeDiagram as a child process and talks to it through standard
/// input and output. Each message is one line of JSON-RPC 2.0:
/// a <b>request</b> has an <c>id</c> and a <c>method</c> and gets exactly one reply with the same <c>id</c>
/// (a <c>result</c> or an <c>error</c>); a <b>notification</b> has a <c>method</c> but no <c>id</c> and gets no reply.
/// </para>
/// <para>
/// A session goes: <c>initialize</c> (agree the protocol version, say we offer tools) → the
/// <c>notifications/initialized</c> notification → any number of <c>tools/list</c> and <c>tools/call</c> → the client
/// closes standard input, which ends <see cref="RunAsync"/>.
/// </para>
/// Standard output carries only protocol messages: anything else written there would break the client. Logging goes
/// to <see cref="log"/> (standard error), which Claude Desktop keeps in its MCP log files.
/// </summary>
public class McpServer
{
    // JSON-RPC error codes.
    const int ParseError = -32700;
    const int InvalidRequest = -32600;
    const int MethodNotFound = -32601;
    const int InvalidParams = -32602;
    const int InternalError = -32603;

    /// <summary>Protocol versions this server can speak, newest last. The tools part is the same in all of them.</summary>
    static readonly List<string> KnownVersions = new List<string> { "2024-11-05", "2025-03-26", "2025-06-18", "2025-11-25" };

    readonly List<IMcpTool> tools;
    readonly string instructions;
    readonly TextWriter log;

    public McpServer(List<IMcpTool> tools, string instructions, TextWriter log)
    {
        this.tools = tools;
        this.instructions = instructions;
        this.log = log;
    }

    /// <summary>Reads messages until the client closes <paramref name="input"/>, answering each request on
    /// <paramref name="output"/>. One message at a time, in order.</summary>
    public async Task RunAsync(TextReader input, TextWriter output)
    {
        while (true)
        {
            string? line = await input.ReadLineAsync();
            if (line is null)
            {
                // The client has gone: end the session.
                break;
            }
            if (line.Trim().Length == 0)
            {
                continue;
            }
            JsonObject? reply = await HandleAsync(line);
            if (reply is not null)
            {
                await output.WriteLineAsync(reply.ToJsonString());
                await output.FlushAsync();
            }
        }
    }

    /// <summary>Handles one line from the client: the reply to send, or null for a notification.</summary>
    public async Task<JsonObject?> HandleAsync(string line)
    {
        JsonNode? message;
        try
        {
            message = JsonNode.Parse(line);
        }
        catch (JsonException)
        {
            return Error(null, ParseError, "The message isn't valid JSON.");
        }

        JsonObject? request = message as JsonObject;
        if (request is null)
        {
            // A batch (an array of messages) was dropped from the protocol in 2025-06-18; not supported here.
            return Error(null, InvalidRequest, "Send one JSON-RPC message per line.");
        }

        string? method = TextOf(request["method"]);
        bool hasId = request.ContainsKey("id");
        JsonNode? id = request["id"];
        if (method is null)
        {
            // A reply from the client. This server never sends requests, so there is nothing to match it to.
            return null;
        }
        if (!hasId)
        {
            // A notification: initialized, cancelled, … Nothing to answer.
            log.WriteLine("Notification: " + method);
            return null;
        }

        JsonObject? parameters = request["params"] as JsonObject;
        try
        {
            switch (method)
            {
                case "initialize":
                    return Result(id, Initialize(parameters));
                case "ping":
                    return Result(id, new JsonObject());
                case "tools/list":
                    return Result(id, ListTools());
                case "tools/call":
                    return await CallToolAsync(id, parameters);
                default:
                    return Error(id, MethodNotFound, "This server doesn't handle \"" + method + "\".");
            }
        }
        catch (Exception ex)
        {
            log.WriteLine("Error handling " + method + ": " + ex);
            return Error(id, InternalError, ex.Message);
        }
    }

    // ───────────── Methods ─────────────

    /// <summary>
    /// The handshake. The client says which protocol version it wants; we answer with the same one when we know it
    /// (otherwise our newest, and the client decides whether to carry on), say that we offer tools, and give
    /// instructions the client adds to the model's context.
    /// </summary>
    JsonObject Initialize(JsonObject? parameters)
    {
        string? asked = null;
        if (parameters is not null)
        {
            asked = TextOf(parameters["protocolVersion"]);
        }
        string version = KnownVersions[KnownVersions.Count - 1];
        if (asked is not null && KnownVersions.Contains(asked))
        {
            version = asked;
        }
        log.WriteLine("Initialize: client asked for " + (asked ?? "no version") + ", using " + version);

        JsonObject toolCapability = new JsonObject();
        toolCapability["listChanged"] = false;
        JsonObject capabilities = new JsonObject();
        capabilities["tools"] = toolCapability;

        JsonObject serverInfo = new JsonObject();
        serverInfo["name"] = "bediagram";
        serverInfo["title"] = "BeDiagram";
        serverInfo["version"] = typeof(McpServer).Assembly.GetName().Version?.ToString() ?? "1.0";

        JsonObject result = new JsonObject();
        result["protocolVersion"] = version;
        result["capabilities"] = capabilities;
        result["serverInfo"] = serverInfo;
        result["instructions"] = instructions;
        return result;
    }

    /// <summary>Every tool's name, description, argument schema and read-only hint.</summary>
    JsonObject ListTools()
    {
        JsonArray list = new JsonArray();
        foreach (IMcpTool tool in tools)
        {
            JsonObject annotations = new JsonObject();
            annotations["readOnlyHint"] = tool.ReadOnly;

            JsonObject entry = new JsonObject();
            entry["name"] = tool.Name;
            entry["description"] = tool.Description;
            entry["inputSchema"] = tool.InputSchema();
            entry["annotations"] = annotations;
            list.Add(entry);
        }
        JsonObject result = new JsonObject();
        result["tools"] = list;
        return result;
    }

    /// <summary>
    /// Runs a tool. An unknown tool name is a protocol error; anything that goes wrong inside the tool comes back as
    /// a result with <c>isError</c>, so the model reads the message and can correct itself.
    /// </summary>
    async Task<JsonObject> CallToolAsync(JsonNode? id, JsonObject? parameters)
    {
        string? name = null;
        JsonObject? arguments = null;
        if (parameters is not null)
        {
            name = TextOf(parameters["name"]);
            arguments = parameters["arguments"] as JsonObject;
        }
        IMcpTool? tool = Find(name);
        if (tool is null)
        {
            return Error(id, InvalidParams, "There is no tool called \"" + (name ?? "") + "\".");
        }

        McpToolResult outcome;
        try
        {
            outcome = await tool.CallAsync(new ToolArguments(arguments));
        }
        catch (ToolArgumentException ex)
        {
            outcome = McpToolResult.Error(ex.Message);
        }
        catch (Exception ex)
        {
            log.WriteLine("Tool " + tool.Name + " failed: " + ex);
            outcome = McpToolResult.Error("BeDiagram couldn't do that: " + ex.Message);
        }
        log.WriteLine("Tool " + tool.Name + (outcome.IsError ? " (error)" : ""));

        JsonObject text = new JsonObject();
        text["type"] = "text";
        text["text"] = outcome.Text;
        JsonArray content = new JsonArray();
        content.Add(text);

        JsonObject result = new JsonObject();
        result["content"] = content;
        result["isError"] = outcome.IsError;
        return Result(id, result);
    }

    IMcpTool? Find(string? name)
    {
        foreach (IMcpTool t in tools)
        {
            if (t.Name == name)
            {
                return t;
            }
        }
        return null;
    }

    // ───────────── Messages ─────────────

    /// <summary>A reply carrying a result. The id is copied: a JSON node can only sit in one message.</summary>
    static JsonObject Result(JsonNode? id, JsonObject result)
    {
        JsonObject reply = new JsonObject();
        reply["jsonrpc"] = "2.0";
        reply["id"] = id?.DeepClone();
        reply["result"] = result;
        return reply;
    }

    static JsonObject Error(JsonNode? id, int code, string message)
    {
        JsonObject error = new JsonObject();
        error["code"] = code;
        error["message"] = message;
        JsonObject reply = new JsonObject();
        reply["jsonrpc"] = "2.0";
        reply["id"] = id?.DeepClone();
        reply["error"] = error;
        return reply;
    }

    /// <summary>The text of a JSON string value, or null for anything else.</summary>
    static string? TextOf(JsonNode? node)
    {
        if (node is JsonValue v && v.GetValueKind() == JsonValueKind.String)
        {
            return v.GetValue<string>();
        }
        return null;
    }
}

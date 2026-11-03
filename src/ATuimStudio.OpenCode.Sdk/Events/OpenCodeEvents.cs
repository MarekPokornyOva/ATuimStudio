using System.Text.Json;
using System.Text.Json.Serialization;

namespace ATuimStudio.OpenCode.Sdk;

// Base envelope. Wire shape (from @opencode/client SSE):
//   data: {"id":"evt_...","type":"session.text.delta","created":123,"data":{...},"location":{...}}
// The SSE `event:` field is ignored — `type` inside the JSON is authoritative.
public abstract record OpenCodeEvent(
	[property: JsonPropertyName("id")] string Id,
	[property: JsonPropertyName("type")] string Type,
	[property: JsonPropertyName("created")] long Created
);

public sealed record LocationRef(
	[property: JsonPropertyName("directory")] string? Directory = null,
	[property: JsonPropertyName("projectID")] string? ProjectId = null
);

public sealed record DurableEnvelope(
	[property: JsonPropertyName("aggregateID")] string AggregateId,
	[property: JsonPropertyName("seq")] int Seq,
	[property: JsonPropertyName("version")] int Version
);

public static class EventTypes
{
	public const string ServerConnected = "server.connected";
	public const string SessionStatus = "session.status";
	public const string SessionIdle = "session.idle"; // deprecated
	public const string SessionCreated = "session.created";
	public const string SessionDeleted = "session.deleted";
	public const string SessionRenamed = "session.renamed";
	public const string SessionExecutionStarted = "session.execution.started";
	public const string SessionExecutionSucceeded = "session.execution.succeeded";
	public const string SessionExecutionFailed = "session.execution.failed";
	public const string SessionExecutionInterrupted = "session.execution.interrupted";
	public const string SessionStepStarted = "session.step.started";
	public const string SessionStepStreamed = "session.step.streamed";
	public const string SessionStepEnded = "session.step.ended";
	public const string SessionStepFailed = "session.step.failed";
	public const string SessionTextStarted = "session.text.started";
	public const string SessionTextDelta = "session.text.delta";
	public const string SessionTextEnded = "session.text.ended";
	public const string SessionReasoningStarted = "session.reasoning.started";
	public const string SessionReasoningDelta = "session.reasoning.delta";
	public const string SessionReasoningEnded = "session.reasoning.ended";
	public const string SessionToolInputStarted = "session.tool.input.started";
	public const string SessionToolInputDelta = "session.tool.input.delta";
	public const string SessionToolInputEnded = "session.tool.input.ended";
	public const string SessionToolCalled = "session.tool.called";
	public const string SessionToolProgress = "session.tool.progress";
	public const string SessionToolSuccess = "session.tool.success";
	public const string SessionToolFailed = "session.tool.failed";
	public const string SessionCompactionStarted = "session.compaction.started";
	public const string SessionCompactionDelta = "session.compaction.delta";
	public const string SessionCompactionEnded = "session.compaction.ended";
	public const string SessionCompactionFailed = "session.compaction.failed";
	public const string SessionShellStarted = "session.shell.started";
	public const string SessionShellEnded = "session.shell.ended";
	public const string SessionUsageUpdated = "session.usage.updated";
	public const string PermissionAsked = "permission.asked";
	public const string PermissionReplied = "permission.replied";
	public const string RpcPrefix = "rpc.";
}

// ---- Data payloads (camelCase on the wire) ----

public sealed record SessionRefData(
	[property: JsonPropertyName("sessionID")] string SessionId
);

public sealed record SessionStatusData(
	[property: JsonPropertyName("sessionID")] string SessionId,
	[property: JsonPropertyName("status")] SessionStatusInfo Status
);

public sealed record SessionStatusInfo(
	[property: JsonPropertyName("type")] string Kind,
	[property: JsonPropertyName("attempt")] int? Attempt = null,
	[property: JsonPropertyName("message")] string? Message = null,
	[property: JsonPropertyName("next")] long? Next = null
);

public sealed record TextDeltaData(
	[property: JsonPropertyName("sessionID")] string SessionId,
	[property: JsonPropertyName("assistantMessageID")] string AssistantMessageId,
	[property: JsonPropertyName("ordinal")] int Ordinal,
	[property: JsonPropertyName("delta")] string Delta
);

public sealed record TextBoundaryData(
	[property: JsonPropertyName("sessionID")] string SessionId,
	[property: JsonPropertyName("assistantMessageID")] string AssistantMessageId,
	[property: JsonPropertyName("ordinal")] int Ordinal,
	[property: JsonPropertyName("text")] string? Text = null
);

public sealed record StepStartedData(
	[property: JsonPropertyName("sessionID")] string SessionId,
	[property: JsonPropertyName("assistantMessageID")] string AssistantMessageId,
	[property: JsonPropertyName("agent")] string Agent,
	[property: JsonPropertyName("model")] ModelRefData? Model = null,
	[property: JsonPropertyName("started")] long Started = 0
);

public sealed record ModelRefData(
	[property: JsonPropertyName("providerID")] string ProviderId,
	[property: JsonPropertyName("modelID")] string ModelId
);

public sealed record StepEndedData(
	[property: JsonPropertyName("sessionID")] string SessionId,
	[property: JsonPropertyName("assistantMessageID")] string AssistantMessageId,
	[property: JsonPropertyName("finish")] string? Finish = null,
	[property: JsonPropertyName("cost")] double? Cost = null
);

public sealed record ToolBaseData(
	[property: JsonPropertyName("sessionID")] string SessionId,
	[property: JsonPropertyName("assistantMessageID")] string AssistantMessageId,
	[property: JsonPropertyName("id")] string ToolId
);

public sealed record ToolDeltaData(
	[property: JsonPropertyName("sessionID")] string SessionId,
	[property: JsonPropertyName("assistantMessageID")] string AssistantMessageId,
	[property: JsonPropertyName("id")] string ToolId,
	[property: JsonPropertyName("delta")] string Delta
);

public sealed record ToolCalledData(
	[property: JsonPropertyName("sessionID")] string SessionId,
	[property: JsonPropertyName("assistantMessageID")] string AssistantMessageId,
	[property: JsonPropertyName("id")] string ToolId,
	[property: JsonPropertyName("input")] Dictionary<string, JsonElement>? Input = null,
	[property: JsonPropertyName("executed")] bool Executed = false
);

public sealed record ExecutionFailedData(
	[property: JsonPropertyName("sessionID")] string SessionId,
	[property: JsonPropertyName("error")] JsonElement Error = default
);

public sealed record CompactionDeltaData(
	[property: JsonPropertyName("sessionID")] string SessionId,
	[property: JsonPropertyName("text")] string Text
);

// ---- Concrete events ----

public sealed record ServerConnectedEvent(string Id, long Created)
	: OpenCodeEvent(Id, EventTypes.ServerConnected, Created);

public sealed record SessionStatusEvent(string Id, long Created, SessionStatusData Data)
	: OpenCodeEvent(Id, EventTypes.SessionStatus, Created);

public sealed record SessionIdleEvent(string Id, long Created, SessionRefData Data)
	: OpenCodeEvent(Id, EventTypes.SessionIdle, Created);

public sealed record SessionExecutionStartedEvent(string Id, long Created, SessionRefData Data)
	: OpenCodeEvent(Id, EventTypes.SessionExecutionStarted, Created);

public sealed record SessionExecutionSucceededEvent(string Id, long Created, SessionRefData Data)
	: OpenCodeEvent(Id, EventTypes.SessionExecutionSucceeded, Created);

public sealed record SessionExecutionFailedEvent(string Id, long Created, ExecutionFailedData Data)
	: OpenCodeEvent(Id, EventTypes.SessionExecutionFailed, Created);

public sealed record SessionStepStartedEvent(string Id, long Created, StepStartedData Data)
	: OpenCodeEvent(Id, EventTypes.SessionStepStarted, Created);

public sealed record SessionStepStreamedEvent(string Id, long Created, SessionRefData Data)
	: OpenCodeEvent(Id, EventTypes.SessionStepStreamed, Created);

public sealed record SessionStepEndedEvent(string Id, long Created, StepEndedData Data)
	: OpenCodeEvent(Id, EventTypes.SessionStepEnded, Created);

public sealed record SessionTextStartedEvent(string Id, long Created, ToolBaseData Data)
	: OpenCodeEvent(Id, EventTypes.SessionTextStarted, Created);

public sealed record SessionTextDeltaEvent(string Id, long Created, TextDeltaData Data)
	: OpenCodeEvent(Id, EventTypes.SessionTextDelta, Created);

public sealed record SessionTextEndedEvent(string Id, long Created, TextBoundaryData Data)
	: OpenCodeEvent(Id, EventTypes.SessionTextEnded, Created);

public sealed record SessionReasoningDeltaEvent(string Id, long Created, TextDeltaData Data)
	: OpenCodeEvent(Id, EventTypes.SessionReasoningDelta, Created);

public sealed record SessionReasoningStartedEvent(string Id, long Created, TextBoundaryData Data)
	: OpenCodeEvent(Id, EventTypes.SessionReasoningStarted, Created);

public sealed record SessionReasoningEndedEvent(string Id, long Created, TextBoundaryData Data)
	: OpenCodeEvent(Id, EventTypes.SessionReasoningEnded, Created);

public sealed record SessionToolInputDeltaEvent(string Id, long Created, ToolDeltaData Data)
	: OpenCodeEvent(Id, EventTypes.SessionToolInputDelta, Created);

public sealed record SessionToolCalledEvent(string Id, long Created, ToolCalledData Data)
	: OpenCodeEvent(Id, EventTypes.SessionToolCalled, Created);

public sealed record SessionCompactionDeltaEvent(string Id, long Created, CompactionDeltaData Data)
	: OpenCodeEvent(Id, EventTypes.SessionCompactionDelta, Created);

public sealed record RpcEvent(string Id, string Type, long Created, LocationRef Location, Dictionary<string, JsonElement> Data)
	: OpenCodeEvent(Id, Type, Created);

/// <summary>Fallback for known-shape envelope with unmapped <c>type</c>. Keeps raw <c>data</c>.</summary>
public sealed record UnknownOpenCodeEvent(string Id, string Type, long Created, JsonElement Data)
	: OpenCodeEvent(Id, Type, Created);

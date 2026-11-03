using System.Text.Json;

namespace ATuimStudio.OpenCode.Sdk;

public static class OpenCodeEventParser
{
	public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
	{
		PropertyNameCaseInsensitive = true,
	};

	public static OpenCodeEvent Parse(string json) =>
		Parse(JsonDocument.Parse(json).RootElement);

	public static OpenCodeEvent Parse(JsonElement root)
	{
		string type = root.TryGetProperty("type", out var t) ? t.GetString() ?? "" : "";
		string id = root.TryGetProperty("id", out var i) ? i.GetString() ?? "" : "";
		long created = root.TryGetProperty("created", out var c) && c.TryGetInt64(out var v) ? v : 0;
		JsonElement data = root.TryGetProperty("data", out var d) ? d : default;

		// RPC events: type "rpc.<rpcID>.<event>", location required, data is free-form object.
		if (type.StartsWith(EventTypes.RpcPrefix, StringComparison.Ordinal))
		{
			var location = root.TryGetProperty("location", out var loc)
				? loc.Deserialize<LocationRef>(Options) ?? new LocationRef()
				: new LocationRef();
			var dict = data.ValueKind == JsonValueKind.Object
				? JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(data.GetRawText(), Options) ?? new()
				: new Dictionary<string, JsonElement>();
			return new RpcEvent(id, type, created, location, dict);
		}

		return type switch
		{
			EventTypes.ServerConnected => new ServerConnectedEvent(id, created),
			EventTypes.SessionStatus => new SessionStatusEvent(id, created, data.Deserialize<SessionStatusData>(Options) ?? new("", new(""))),
			EventTypes.SessionIdle => new SessionIdleEvent(id, created, data.Deserialize<SessionRefData>(Options) ?? new("")),
			EventTypes.SessionExecutionStarted => new SessionExecutionStartedEvent(id, created, data.Deserialize<SessionRefData>(Options) ?? new("")),
			EventTypes.SessionExecutionSucceeded => new SessionExecutionSucceededEvent(id, created, data.Deserialize<SessionRefData>(Options) ?? new("")),
			EventTypes.SessionExecutionFailed => new SessionExecutionFailedEvent(id, created, data.Deserialize<ExecutionFailedData>(Options) ?? new("")),
			EventTypes.SessionStepStarted => new SessionStepStartedEvent(id, created, data.Deserialize<StepStartedData>(Options) ?? new("", "", "")),
			EventTypes.SessionStepStreamed => new SessionStepStreamedEvent(id, created, data.Deserialize<SessionRefData>(Options) ?? new("")),
			EventTypes.SessionStepEnded => new SessionStepEndedEvent(id, created, data.Deserialize<StepEndedData>(Options) ?? new("", "")),
			EventTypes.SessionTextStarted => new SessionTextStartedEvent(id, created, data.Deserialize<ToolBaseData>(Options) ?? new("", "", "")),
			EventTypes.SessionTextDelta => new SessionTextDeltaEvent(id, created, data.Deserialize<TextDeltaData>(Options) ?? new("", "", 0, "")),
			EventTypes.SessionTextEnded => new SessionTextEndedEvent(id, created, data.Deserialize<TextBoundaryData>(Options) ?? new("", "", 0)),
			EventTypes.SessionReasoningDelta => new SessionReasoningDeltaEvent(id, created, data.Deserialize<TextDeltaData>(Options) ?? new("", "", 0, "")),
			EventTypes.SessionReasoningStarted => new SessionReasoningStartedEvent(id, created, data.Deserialize<TextBoundaryData>(Options) ?? new("", "", 0)),
			EventTypes.SessionReasoningEnded => new SessionReasoningEndedEvent(id, created, data.Deserialize<TextBoundaryData>(Options) ?? new("", "", 0)),
			EventTypes.SessionToolInputDelta => new SessionToolInputDeltaEvent(id, created, data.Deserialize<ToolDeltaData>(Options) ?? new("", "", "", "")),
			EventTypes.SessionToolCalled => new SessionToolCalledEvent(id, created, data.Deserialize<ToolCalledData>(Options) ?? new("", "", "")),
			EventTypes.SessionCompactionDelta => new SessionCompactionDeltaEvent(id, created, data.Deserialize<CompactionDeltaData>(Options) ?? new("", "")),
			// Envelope is valid but type has no dedicated record yet — keep raw data.
			_ => new UnknownOpenCodeEvent(id, type, created, data.ValueKind == JsonValueKind.Undefined ? default : data.Clone()),
		};
	}
}

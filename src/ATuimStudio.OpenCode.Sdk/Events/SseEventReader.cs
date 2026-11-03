using System.Runtime.CompilerServices;

namespace ATuimStudio.OpenCode.Sdk;

/// <summary>
/// Minimal SSE reader matching @opencode/client behavior:
/// split on blank line, join lines starting with "data:", JSON-parse the result.
/// "event:"/"id:"/"retry:" and ":keepalive" lines are ignored; the event type
/// comes from the JSON payload's "type" property.
/// </summary>
public static class SseEventReader
{
	public static async IAsyncEnumerable<OpenCodeEvent> ReadAsync(
		Stream stream,
		[EnumeratorCancellation] CancellationToken cancellationToken = default,
		Action? onActivity = null)
	{
		using var reader = new StreamReader(stream);
		List<string> dataLines = new();

		// Buffer dispatches so exceptions keep reader usable; yield outside lock-free path.
		while (true)
		{
			string? line = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);
			if (line is null)
			{
				if (dataLines.Count > 0)
					yield return OpenCodeEventParser.Parse(string.Join("\n", dataLines));
				yield break;
			}

			onActivity?.Invoke();

			if (line.Length == 0)
			{
				// blank line = dispatch
				if (dataLines.Count > 0)
				{
					OpenCodeEvent evt = OpenCodeEventParser.Parse(string.Join("\n", dataLines));
					dataLines.Clear();
					yield return evt;
				}
				continue;
			}

			if (line[0] == ':')
				continue; // comment / keepalive

			if (line.StartsWith("data:", StringComparison.Ordinal))
			{
				string value = line.Substring(5);
				if (value.Length > 0 && value[0] == ' ')
					value = value.Substring(1);
				dataLines.Add(value);
			}
			// "event:", "id:", "retry:" intentionally ignored (type lives in JSON).
		}
	}
}

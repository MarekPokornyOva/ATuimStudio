namespace ATuimStudio.Extensions.Debug
{
	public sealed class BreakpointEventArgs : EventArgs
	{
		public BreakpointEventArgs(IReadOnlyList<IStackFrame> callStack)
		{
			CallStack = callStack;
		}

		public IReadOnlyList<IStackFrame> CallStack { get; }
	}

	public readonly record struct SourceRange(SourcePosition Start, SourcePosition End)
	{
		public SourceRange(int startLine, int startColumn, int endLine, int endColumn)
			 : this(
					 new SourcePosition(Line: startLine, Column: startColumn),
					 new SourcePosition(Line: endLine, Column: endColumn))
		{
		}

		/// <summary>
		/// Checks if the given position is within this range.
		/// </summary>
		/// <param name="position">The position to check.</param>
		/// <returns>True, if this range contains <paramref name="position"/>.</returns>
		public bool Contains(SourcePosition position) => this.Start <= position && position < this.End;
	}

	public readonly record struct SourcePosition(int Line, int Column) : IComparable<SourcePosition>
	{
		public int CompareTo(SourcePosition other)
		{
			var cmp = this.Line.CompareTo(other.Line);
			return cmp == 0
				 ? this.Column.CompareTo(other.Column)
				 : cmp;
		}

		public static bool operator <(SourcePosition left, SourcePosition right) => left.CompareTo(right) < 0;
		public static bool operator <=(SourcePosition left, SourcePosition right) => left.CompareTo(right) <= 0;
		public static bool operator >(SourcePosition left, SourcePosition right) => left.CompareTo(right) > 0;
		public static bool operator >=(SourcePosition left, SourcePosition right) => left.CompareTo(right) >= 0;
	}

	public interface IStackFrame
	{
		IReadOnlyCollection<IDebugItem> GetArguments();
		IReadOnlyCollection<IDebugItem> GetLocals();
		string SourceFilePath { get; }
		SourceRange? Range { get; }
		string FullStackFrameText { get; }
	}

	public interface IDebugItem
	{
		string Name { get; }
		string Value { get; }
		string TypeName { get; }

		bool HasChildren { get; }
		IReadOnlyCollection<IDebugItem> GetAllChildren();
	}
}

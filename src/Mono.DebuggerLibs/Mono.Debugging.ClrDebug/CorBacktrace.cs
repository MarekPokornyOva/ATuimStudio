using ClrDebug;
using Microsoft.Samples.Debugging.CorDebug;
using Microsoft.Samples.Debugging.CorMetadata;
using Mono.Debugging.Client;
using Mono.Debugging.Evaluation;
using System.Diagnostics;
using System.Diagnostics.SymbolStore;
using System.Reflection;

namespace Mono.Debugging.ClrDebug
{
	internal class CorBacktrace : BaseBacktrace
	{
		private CorThread thread;

		private readonly int threadId;

		private readonly CorDebuggerSession session;

		private List<CorFrame> frames;

		private int evalTimestamp;

		private const int SpecialSequencePoint = 16707566;

		internal List<CorFrame> FrameList
		{
			get
			{
				if (evalTimestamp != CorDebuggerSession.EvaluationTimestamp)
				{
					thread = session.GetThread(threadId);
					frames = new List<CorFrame>(GetFrames(thread));
					evalTimestamp = CorDebuggerSession.EvaluationTimestamp;
				}
				return frames;
			}
		}

		public override int FrameCount => FrameList.Count;

		public CorBacktrace(CorThread thread, CorDebuggerSession session)
			: base(session.ObjectAdapter)
		{
			this.session = session;
			this.thread = thread;
			threadId = thread.Id;
			frames = new List<CorFrame>(GetFrames(thread));
			evalTimestamp = CorDebuggerSession.EvaluationTimestamp;
		}

		internal static IEnumerable<CorFrame> GetFrames(CorThread thread)
		{
			List<CorFrame> list = new List<CorFrame>();
			try
			{
				foreach (CorChain chain in thread.Chains)
				{
					if (!chain.IsManaged)
					{
						continue;
					}
					try
					{
						list.AddRange(chain.Frames);
					}
					catch (DebugException ex)
					{
						DebuggerLoggingService.LogMessage("Failed to enumerate frames of chain: {0}", ex.Message);
					}
				}
			}
			catch (DebugException ex2)
			{
				DebuggerLoggingService.LogMessage("Failed to enumerate chains of thread: {0}", ex2.Message);
			}
			return list;
		}

		protected override EvaluationContext GetEvaluationContext(int frameIndex, EvaluationOptions options)
		{
			return new CorEvaluationContext(session, this, frameIndex, options)
			{
				Thread = thread
			};
		}

		public override AssemblyLine[] Disassemble(int frameIndex, int firstLine, int count)
		{
			return new AssemblyLine[0];
		}

		public override Mono.Debugging.Client.StackFrame[] GetStackFrames(int firstIndex, int lastIndex)
		{
			if (lastIndex >= FrameList.Count)
			{
				lastIndex = FrameList.Count - 1;
			}
			Mono.Debugging.Client.StackFrame[] array = new Mono.Debugging.Client.StackFrame[lastIndex - firstIndex + 1];
			for (int i = 0; i < array.Length; i++)
			{
				array[i] = CreateFrame(session, FrameList[i + firstIndex]);
			}
			return array;
		}

		public static SequencePoint GetSequencePoint(CorDebuggerSession session, CorFrame frame)
		{
			ISymbolReader readerForModule = session.GetReaderForModule(frame.Function.Module);
			if (readerForModule == null)
			{
				return null;
			}
			ISymbolMethod method = readerForModule.GetMethod(new SymbolToken(frame.Function.Token));
			if (method == null)
			{
				return null;
			}
			int sequencePointCount = method.SequencePointCount;
			if (sequencePointCount <= 0)
			{
				return null;
			}
			frame.GetIP(out var offset, out var mappingResult);
			if (mappingResult == CorDebugMappingResult.MAPPING_NO_INFO || mappingResult == CorDebugMappingResult.MAPPING_UNMAPPED_ADDRESS)
			{
				return null;
			}
			int[] array = new int[sequencePointCount];
			int[] array2 = new int[sequencePointCount];
			int[] array3 = new int[sequencePointCount];
			int[] array4 = new int[sequencePointCount];
			int[] array5 = new int[sequencePointCount];
			ISymbolDocument[] array6 = new ISymbolDocument[sequencePointCount];
			method.GetSequencePoints(array, array6, array2, array4, array3, array5);
			if (sequencePointCount > 0 && array[0] <= offset)
			{
				int i;
				for (i = 0; i < sequencePointCount && array[i] < offset; i++)
				{
				}
				if (i == sequencePointCount || array[i] != offset)
				{
					i--;
				}
				if (array2[i] == 16707566)
				{
					int num = i;
					while (num > 0)
					{
						num--;
						if (array2[num] != 16707566)
						{
							return new SequencePoint
							{
								IsSpecial = true,
								Offset = array[num],
								StartLine = array2[num],
								EndLine = array3[num],
								StartColumn = array4[num],
								EndColumn = array5[num],
								Document = array6[num]
							};
						}
					}
					num = i;
					while (++num < sequencePointCount)
					{
						if (array2[num] != 16707566)
						{
							return new SequencePoint
							{
								IsSpecial = true,
								Offset = array[num],
								StartLine = array2[num],
								EndLine = array3[num],
								StartColumn = array4[num],
								EndColumn = array5[num],
								Document = array6[num]
							};
						}
					}
					return null;
				}
				return new SequencePoint
				{
					IsSpecial = false,
					Offset = array[i],
					StartLine = array2[i],
					EndLine = array3[i],
					StartColumn = array4[i],
					EndColumn = array5[i],
					Document = array6[i]
				};
			}
			return null;
		}

		internal static Mono.Debugging.Client.StackFrame CreateFrame(CorDebuggerSession session, CorFrame frame)
		{
			int offset = 0;
			string addressSpace = "";
			string fileName = "";
			int line = 0;
			int endLine = 0;
			int column = 0;
			int endColumn = 0;
			string methodName = "[Unknown]";
			string language = "";
			string fullModuleName = "";
			string fullTypeName = "";
			bool hasDebugInfo = false;
			bool isDebuggerHidden = false;
			bool isExternalCode = true;
			if (frame.FrameType == CorFrameType.ILFrame)
			{
				if (frame.Function != null)
				{
					fullModuleName = frame.Function.Module.Name;
					MethodInfo methodInfo = new CorMetadataImport(frame.Function.Module.CreateMetaDataImport()).GetMethodInfo(frame.Function.Token);
					Type declaringType = methodInfo.DeclaringType;
					if (declaringType != null)
					{
						methodName = declaringType.FullName + "." + methodInfo.Name;
						fullTypeName = declaringType.FullName;
					}
					else
					{
						methodName = methodInfo.Name;
					}
					addressSpace = methodInfo.Name;
					SequencePoint sequencePoint = GetSequencePoint(session, frame);
					if (sequencePoint != null)
					{
						line = sequencePoint.StartLine;
						column = sequencePoint.StartColumn;
						endLine = sequencePoint.EndLine;
						endColumn = sequencePoint.EndColumn;
						fileName = sequencePoint.Document.URL;
						offset = sequencePoint.Offset;
					}
					isExternalCode = session.IsExternalCode(fileName) || ((!session.Options.ProjectAssembliesOnly) ? methodInfo.GetCustomAttributes(inherit: true).Any((object v) => v is DebuggerHiddenAttribute) : methodInfo.GetCustomAttributes(inherit: true).Any((object v) => v is DebuggerHiddenAttribute || v is DebuggerNonUserCodeAttribute));
					isDebuggerHidden = methodInfo.GetCustomAttributes(inherit: true).Any((object v) => v is DebuggerHiddenAttribute);
				}
				language = "Managed";
				hasDebugInfo = true;
			}
			else if (frame.FrameType == CorFrameType.NativeFrame)
			{
				frame.GetNativeIP(out offset);
				methodName = "[Native frame]";
				language = "Native";
			}
			else if (frame.FrameType == CorFrameType.InternalFrame)
			{
				switch (frame.InternalFrameType)
				{
					case CorDebugInternalFrameType.STUBFRAME_M2U:
					methodName = "[Managed to Native Transition]";
					break;
					case CorDebugInternalFrameType.STUBFRAME_U2M:
					methodName = "[Native to Managed Transition]";
					break;
					case CorDebugInternalFrameType.STUBFRAME_LIGHTWEIGHT_FUNCTION:
					methodName = "[Lightweight Method Call]";
					break;
					case CorDebugInternalFrameType.STUBFRAME_APPDOMAIN_TRANSITION:
					methodName = "[Application Domain Transition]";
					break;
					case CorDebugInternalFrameType.STUBFRAME_FUNC_EVAL:
					methodName = "[Function Evaluation]";
					break;
				}
			}
			SourceLocation location = new SourceLocation(methodName, fileName, line, column, endLine, endColumn);
			return new Mono.Debugging.Client.StackFrame(offset, addressSpace, location, language, isExternalCode, hasDebugInfo, isDebuggerHidden, fullModuleName, fullTypeName);
		}
	}
}

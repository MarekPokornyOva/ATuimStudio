using System;
using System.Collections.Generic;
using System.Diagnostics.SymbolStore;
using System.Text;

namespace Mono.Debugging.ClrDebug
{
	sealed class SymbolDocument : ISymbolDocument
	{
		internal SymbolDocument(Uri uri, byte[] hash, Guid hashAlgorithm, Guid language)
		{
			Language = language;
			_checkSum = hash;
			CheckSumAlgorithmId = hashAlgorithm;
			URL = uri.ToString();
		}

		public Guid CheckSumAlgorithmId { get; }

		public Guid DocumentType => throw new NotImplementedException();

		public bool HasEmbeddedSource => throw new NotImplementedException();

		public Guid Language { get; }

		public Guid LanguageVendor => throw new NotImplementedException();

		public int SourceLength => throw new NotImplementedException();

		public string URL { get; }

		public int FindClosestLine(int line)
			=> line;

		readonly byte[] _checkSum;
		public byte[] GetCheckSum()
			=> _checkSum;

		public byte[] GetSourceRange(int startLine, int startColumn, int endLine, int endColumn)
		{
			throw new NotImplementedException();
		}
	}
}

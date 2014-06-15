using System;
using System.Diagnostics.SymbolStore;
using System.Text;

namespace PdbConvert.Symbols
{
	internal class SymbolDocument : ISymbolDocument
	{
		private ISymUnmanagedDocument unmanagedDocument;

		internal SymbolDocument(ISymUnmanagedDocument unmanagedDocument)
		{
			if (unmanagedDocument == null)
			{
				throw new ArgumentNullException("unmanagedDocument");
			}
			this.unmanagedDocument = unmanagedDocument;
		}

		public string URL
		{
			get
			{
				StringBuilder url;
				int cchUrl;
				unmanagedDocument.GetURL(0, out cchUrl, null);
				url = new StringBuilder(cchUrl);
				unmanagedDocument.GetURL(cchUrl, out cchUrl, url);
				return url.ToString();
			}
		}

		public Guid DocumentType
		{
			get
			{
				Guid guid = new Guid();
				unmanagedDocument.GetDocumentType(ref guid);
				return guid;
			}
		}

		public Guid Language
		{
			get
			{
				Guid guid = new Guid();
				unmanagedDocument.GetLanguage(ref guid);
				return guid;
			}
		}

		public Guid LanguageVendor
		{
			get
			{
				Guid guid = new Guid();
				unmanagedDocument.GetLanguageVendor(ref guid);
				return guid;
			}
		}

		public Guid CheckSumAlgorithmId
		{
			get
			{
				Guid guid = new Guid();
				unmanagedDocument.GetCheckSumAlgorithmId(ref guid);
				return guid;
			}
		}

		public byte[] GetCheckSum()
		{
			byte[] data;
			int cData;
			unmanagedDocument.GetCheckSum(0, out cData, null);
			data = new byte[cData];
			unmanagedDocument.GetCheckSum(cData, out cData, data);
			return data;
		}


		public int FindClosestLine(int line)
		{
			int closestLine;
			unmanagedDocument.FindClosestLine(line, out closestLine);
			return closestLine;
		}

		public bool HasEmbeddedSource
		{
			get
			{
				bool retVal;
				unmanagedDocument.HasEmbeddedSource(out retVal);
				return retVal;
			}
		}

		public int SourceLength
		{
			get
			{
				int retVal;
				unmanagedDocument.GetSourceLength(out retVal);
				return retVal;
			}
		}

		public byte[] GetSourceRange(int startLine, int startColumn, int endLine, int endColumn)
		{
			byte[] data;
			int count;
			unmanagedDocument.GetSourceRange(startLine, startColumn, endLine, endColumn, 0, out count, null);
			data = new byte[count];
			unmanagedDocument.GetSourceRange(startLine, startColumn, endLine, endColumn, count, out count, data);
			return data;
		}

		internal ISymUnmanagedDocument InternalDocument
		{
			get
			{
				return unmanagedDocument;
			}
		}
	}
}

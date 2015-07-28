using System;
using System.Diagnostics.SymbolStore;
using System.Text;

namespace PdbConvert.Symbols
{
	internal class SymbolMethod : ISymbolMethod
	{
		private ISymUnmanagedMethod unmanagedMethod;

		public SymbolMethod(ISymUnmanagedMethod unmanagedMethod)
		{
			if (unmanagedMethod == null)
			{
				throw new ArgumentNullException("unmanagedMethod");
			}
			this.unmanagedMethod = unmanagedMethod;
		}

		public SymbolToken Token
		{
			get
			{
				SymbolToken token;
				unmanagedMethod.GetToken(out token);
				return token;
			}
		}

		public int SequencePointCount
		{
			get
			{
				int retval;
				unmanagedMethod.GetSequencePointCount(out retval);
				return retval;
			}
		}

		public void GetSequencePoints(
			int[] offsets,
			ISymbolDocument[] documents,
			int[] lines,
			int[] columns,
			int[] endLines,
			int[] endColumns)
		{
			int spCount = 0;
			if (offsets != null)
				spCount = offsets.Length;
			else if (documents != null)
				spCount = documents.Length;
			else if (lines != null)
				spCount = lines.Length;
			else if (columns != null)
				spCount = columns.Length;
			else if (endLines != null)
				spCount = endLines.Length;
			else if (endColumns != null)
				spCount = endColumns.Length;

			// Don't do anything if they're not really asking for anything.
			if (spCount == 0)
				return;

			// Make sure all arrays are the same length.
			if ((offsets != null) && (spCount != offsets.Length))
				throw new ArgumentException();

			if ((lines != null) && (spCount != lines.Length))
				throw new ArgumentException();

			if ((columns != null) && (spCount != columns.Length))
				throw new ArgumentException();

			if ((endLines != null) && (spCount != endLines.Length))
				throw new ArgumentException();

			if ((endColumns != null) && (spCount != endColumns.Length))
				throw new ArgumentException();

			ISymUnmanagedDocument[] unmanagedDocuments = new ISymUnmanagedDocument[documents.Length];
			int cPoints;
			uint i;
			unmanagedMethod.GetSequencePoints(
				documents.Length, out cPoints,
				offsets, unmanagedDocuments,
				lines, columns,
				endLines, endColumns);

			// Create the SymbolDocument form the IntPtr's
			for (i = 0; i < documents.Length; i++)
			{
				documents[i] = new SymbolDocument(unmanagedDocuments[i]);
			}
			return;
		}

		public ISymbolScope RootScope
		{
			get
			{
				ISymUnmanagedScope retval;
				unmanagedMethod.GetRootScope(out retval);
				return new SymbolScope(retval);
			}
		}

		public ISymbolScope GetScope(int offset)
		{
			ISymUnmanagedScope retVal;
			unmanagedMethod.GetScopeFromOffset(offset, out retVal);
			return new SymbolScope(retVal);
		}

		public int GetOffset(ISymbolDocument document, int line, int column)
		{
			int retVal;
			unmanagedMethod.GetOffset(((SymbolDocument)document).InternalDocument, line, column, out retVal);
			return retVal;
		}

		public int[] GetRanges(ISymbolDocument document, int line, int column)
		{
			int cRanges;
			unmanagedMethod.GetRanges(((SymbolDocument)document).InternalDocument, line, column, 0, out cRanges, null);
			int[] ranges = new int[cRanges];
			unmanagedMethod.GetRanges(((SymbolDocument)document).InternalDocument, line, column, cRanges, out cRanges, ranges);
			return ranges;
		}

		public ISymbolVariable[] GetParameters()
		{
			int cVariables;
			uint i;
			unmanagedMethod.GetParameters(0, out cVariables, null);
			ISymUnmanagedVariable[] unmanagedVariables = new ISymUnmanagedVariable[cVariables];
			unmanagedMethod.GetParameters(cVariables, out cVariables, unmanagedVariables);

			ISymbolVariable[] variables = new ISymbolVariable[cVariables];
			for (i = 0; i < cVariables; i++)
			{
				variables[i] = new SymbolVariable(unmanagedVariables[i]);
			}
			return variables;
		}

		public ISymbolNamespace GetNamespace()
		{
			ISymUnmanagedNamespace retVal;
			unmanagedMethod.GetNamespace(out retVal);
			return new SymbolNamespace(retVal);
		}

		public bool GetSourceStartEnd(ISymbolDocument[] docs, int[] lines, int[] columns)
		{
			uint i;
			bool retVal;
			int spCount = 0;
			if (docs != null)
				spCount = docs.Length;
			else if (lines != null)
				spCount = lines.Length;
			else if (columns != null)
				spCount = columns.Length;

			// If we don't have at least 2 entries then return an error
			if (spCount < 2)
				throw new ArgumentException();

			// Make sure all arrays are the same length.
			if ((docs != null) && (spCount != docs.Length))
				throw new ArgumentException();

			if ((lines != null) && (spCount != lines.Length))
				throw new ArgumentException();

			if ((columns != null) && (spCount != columns.Length))
				throw new ArgumentException();

			ISymUnmanagedDocument[] unmanagedDocuments = new ISymUnmanagedDocument[docs.Length];
			unmanagedMethod.GetSourceStartEnd(unmanagedDocuments, lines, columns, out retVal);
			if (retVal)
			{
				for (i = 0; i < docs.Length; i++)
				{
					docs[i] = new SymbolDocument(unmanagedDocuments[i]);
				}
			}
			return retVal;
		}

		public String GetFileNameFromOffset(int dwOffset)
		{
			int cchName;
			((ISymENCUnmanagedMethod)unmanagedMethod).GetFileNameFromOffset(dwOffset, 0, out cchName, null);
			StringBuilder name = new StringBuilder(cchName);
			((ISymENCUnmanagedMethod)unmanagedMethod).GetFileNameFromOffset(dwOffset, cchName, out cchName, name);
			return name.ToString();
		}

		public int GetLineFromOffset(
			int dwOffset,
			out int pcolumn,
			out int pendLine,
			out int pendColumn,
			out int pdwStartOffset)
		{
			int line;
			((ISymENCUnmanagedMethod)unmanagedMethod).GetLineFromOffset(
				dwOffset, out line, out pcolumn, out pendLine, out pendColumn, out pdwStartOffset);
			return line;
		}

		public ISymUnmanagedMethod InternalMethod
		{
			get
			{
				return unmanagedMethod;
			}
		}
	}
}

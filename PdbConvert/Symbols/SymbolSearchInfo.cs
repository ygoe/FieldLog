using System;
using System.Text;

namespace PdbConvert.Symbols
{
	internal class SymbolSearchInfo : ISymbolSearchInfo
	{
		private ISymUnmanagedSymbolSearchInfo target;

		public SymbolSearchInfo(ISymUnmanagedSymbolSearchInfo target)
		{
			this.target = target;
		}

		public int SearchPathLength
		{
			get
			{
				int length;
				target.GetSearchPathLength(out length);
				return length;
			}
		}

		public string SearchPath
		{
			get
			{
				int length;
				target.GetSearchPath(0, out length, null);
				StringBuilder path = new StringBuilder(length);
				target.GetSearchPath(length, out length, path);
				return path.ToString();
			}
		}

		public int HResult
		{
			get
			{
				int hr;
				target.GetHRESULT(out hr);
				return hr;
			}
		}
	}
}

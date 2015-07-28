using System;

namespace Unclassified.LogSubmit.Views
{
	internal interface IView
	{
		void Activate(bool forward);

		void Deactivate(bool forward);
	}
}

using System;
using System.Linq;
using System.Windows;

namespace Unclassified.FieldLogViewer.ViewModel
{
	internal class DetailsMessageViewModel : ViewModelBase
	{
		public string Title { get; set; }
		public string Message { get; set; }
		public string IconName { get; set; }

		public DetailsMessageViewModel(string title, string message, string iconName)
		{
			this.Title = title;
			this.Message = message;
			this.IconName = iconName;
		}

		public DetailsMessageViewModel(string title, string message)
		{
			this.Title = title;
			this.Message = message;
		}

		public DetailsMessageViewModel(string title)
		{
			this.Title = title;
		}

		public Visibility ArrowLeftIconVisibility { get { return this.IconName == "ArrowLeft" ? Visibility.Visible : Visibility.Collapsed; } }
		public Visibility ArrowUpIconVisibility { get { return this.IconName == "ArrowUp" ? Visibility.Visible : Visibility.Collapsed; } }
		public Visibility FlashIconVisibility { get { return this.IconName == "Flash" ? Visibility.Visible : Visibility.Collapsed; } }
	}
}

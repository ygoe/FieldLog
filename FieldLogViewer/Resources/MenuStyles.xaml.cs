using System;
using System.Windows;
using System.Windows.Controls;

namespace Unclassified.FieldLogViewer.Resources
{
	/// <summary>
	/// Menu styles.
	/// </summary>
	partial class MenuStyles : ResourceDictionary
	{
		/// <summary>
		/// Initialises the menu styles in the application.
		/// </summary>
		static MenuStyles()
		{
			Application.Current.Dispatcher.BeginInvoke((Action)UpdateditorContextMenuStyle);
		}

		/// <summary>
		/// Initialises a new instance of the <see cref="MenuStyles"/> class.
		/// </summary>
		public MenuStyles()
		{
			InitializeComponent();
		}

		/// <summary>
		/// Updates the default style of the framework-private EditorMenuItem class to use the same
		/// customisations as defined for the public MenuItem class.
		/// </summary>
		public static void UpdateditorContextMenuStyle()
		{
			// Source: http://stackoverflow.com/a/34975032/143684
			Style menuItemStyle = Application.Current.TryFindResource(typeof(MenuItem)) as Style;
			if (menuItemStyle != null)
			{
				var menuItemAssembly = typeof(MenuItem).Assembly;
				Type editorMenuItemType = menuItemAssembly.GetType("System.Windows.Documents.TextEditorContextMenu+EditorMenuItem", false);
				if (editorMenuItemType != null)
				{
					Application.Current.Resources.Add(editorMenuItemType, menuItemStyle);
				}
				Type reconversionMenuItemType = menuItemAssembly.GetType("System.Windows.Documents.TextEditorContextMenu+ReconversionMenuItem", false);
				if (reconversionMenuItemType != null)
				{
					Application.Current.Resources.Add(reconversionMenuItemType, menuItemStyle);
				}
			}
		}
	}
}

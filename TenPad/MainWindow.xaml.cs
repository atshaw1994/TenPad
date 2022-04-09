using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace TenPad
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
		private string loadedFilePath;

        public MainWindow()
        {
            InitializeComponent();
			loadedFilePath = "";
		}

		public MainWindow(string arg)
		{
			InitializeComponent();
			LoadFile(arg);
			loadedFilePath = arg;
		}

		private void Window_Loaded(object sender, RoutedEventArgs e)
		{
			FindReplacePopup.Opacity = 0.0;
			GoToPopup.Opacity = 0.0;
            Application.Current.Resources.MergedDictionaries[0].Source = new Uri($"/Themes/{GetWindowsTheme()}.xaml", UriKind.Relative);
		}

		#region BorderlessMethods
		private void CaptionBar_CloseButton_Click(object sender, RoutedEventArgs e)
		{
			Close();
		}

		private void CaptionBar_RestoreButton_Click(object sender, RoutedEventArgs e)
		{
			WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
		}

		private void CaptionBar_MinimizeButton_Click(object sender, RoutedEventArgs e)
		{
			WindowState = WindowState.Minimized;
		}

		private void RefreshMaximizeRestoreButton()
		{
			if (WindowState == WindowState.Maximized)
			{
				CaptionBar_MaximizeButton.Visibility = Visibility.Collapsed;
				CaptionBar_RestoreButton.Visibility = Visibility.Visible;
			}
			else
			{
				CaptionBar_MaximizeButton.Visibility = Visibility.Visible;
				CaptionBar_RestoreButton.Visibility = Visibility.Collapsed;
			}
		}
		private void Window_StateChanged(object sender, EventArgs e)
		{
			RefreshMaximizeRestoreButton();
		}

		private void Window_SourceInitialized(object sender, EventArgs e)
		{
			((HwndSource)PresentationSource.FromVisual(this)).AddHook(HookProc);
		}

		public static IntPtr HookProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
		{
			if (msg == WM_GETMINMAXINFO)
			{
				// We need to tell the system what our size should be when maximized. Otherwise it will cover the whole screen,
				// including the task bar.
				MINMAXINFO mmi = (MINMAXINFO)Marshal.PtrToStructure(lParam, typeof(MINMAXINFO));

				// Adjust the maximized size and position to fit the work area of the correct monitor
				IntPtr monitor = MonitorFromWindow(hwnd, MONITOR_DEFAULTTONEAREST);

				if (monitor != IntPtr.Zero)
				{
					MONITORINFO monitorInfo = new();
					monitorInfo.cbSize = Marshal.SizeOf(typeof(MONITORINFO));
					GetMonitorInfo(monitor, ref monitorInfo);
					RECT rcWorkArea = monitorInfo.rcWork;
					RECT rcMonitorArea = monitorInfo.rcMonitor;
					mmi.ptMaxPosition.X = Math.Abs(rcWorkArea.Left - rcMonitorArea.Left);
					mmi.ptMaxPosition.Y = Math.Abs(rcWorkArea.Top - rcMonitorArea.Top);
					mmi.ptMaxSize.X = Math.Abs(rcWorkArea.Right - rcWorkArea.Left);
					mmi.ptMaxSize.Y = Math.Abs(rcWorkArea.Bottom - rcWorkArea.Top);
				}

				Marshal.StructureToPtr(mmi, lParam, true);
			}

			return IntPtr.Zero;
		}

		private const int WM_GETMINMAXINFO = 0x0024;

		private const uint MONITOR_DEFAULTTONEAREST = 0x00000002;

		[DllImport("user32.dll")]
		private static extern IntPtr MonitorFromWindow(IntPtr handle, uint flags);

		[DllImport("user32.dll")]
		private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);

		[Serializable]
		[StructLayout(LayoutKind.Sequential)]
		public struct RECT
		{
			public int Left;
			public int Top;
			public int Right;
			public int Bottom;

			public RECT(int left, int top, int right, int bottom)
			{
				this.Left = left;
				this.Top = top;
				this.Right = right;
				this.Bottom = bottom;
			}
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct MONITORINFO
		{
			public int cbSize;
			public RECT rcMonitor;
			public RECT rcWork;
			public uint dwFlags;
		}

		[Serializable]
		[StructLayout(LayoutKind.Sequential)]
		public struct POINT
		{
			public int X;
			public int Y;

			public POINT(int x, int y)
			{
				this.X = x;
				this.Y = y;
			}
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct MINMAXINFO
		{
			public POINT ptReserved;
			public POINT ptMaxSize;
			public POINT ptMaxPosition;
			public POINT ptMinTrackSize;
			public POINT ptMaxTrackSize;
		}
		#endregion

		#region ThemeSwitching
		private const string RegistryKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";

		private const string RegistryValueName = "AppsUseLightTheme";

		private enum WindowsTheme
		{
			Light,
			Dark
		}

		public enum AppTheme
		{
			Light,
			Dark
		}

		public static void WatchTheme()
		{
			var currentUser = WindowsIdentity.GetCurrent();
			string query = string.Format(
				CultureInfo.InvariantCulture,
				@"SELECT * FROM RegistryValueChangeEvent WHERE Hive = 'HKEY_USERS' AND KeyPath = '{0}\\{1}' AND ValueName = '{2}'",
				currentUser.User.Value,
				RegistryKeyPath.Replace(@"\", @"\\"),
				RegistryValueName);

			try
			{
				var watcher = new ManagementEventWatcher(query);
				watcher.EventArrived += (sender, args) =>
				{
					WindowsTheme newWindowsTheme = GetWindowsTheme();
					// React to new theme
				};

				// Start listening for events
				watcher.Start();
			}
			catch (Exception)
			{
				// This can fail on Windows 7
			}

			WindowsTheme initialTheme = GetWindowsTheme();
		}

		private static WindowsTheme GetWindowsTheme()
		{
            using RegistryKey key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath);
            object registryValueObject = key?.GetValue(RegistryValueName);
            if (registryValueObject == null)
            {
                return WindowsTheme.Light;
            }

            int registryValue = (int)registryValueObject;

            return registryValue > 0 ? WindowsTheme.Light : WindowsTheme.Dark;
        }
		#endregion

		private void LoadFile(string filePath)
		{
			baseTextBox.Document.Blocks.Clear();
			TextRange range;
			FileStream fStream;

			if (File.Exists(filePath))
			{
				range = new TextRange(baseTextBox.Document.ContentStart, 
					baseTextBox.Document.ContentEnd);
				fStream = new FileStream(filePath, FileMode.OpenOrCreate);
				range.Load(fStream, System.Windows.DataFormats.Text);
				fStream.Close();
			}
			Title = $"{System.IO.Path.GetFileName(filePath)} - TenPad";
			loadedFilePath = filePath;
			if (Title.Contains("*") == true)
				Title = Title.Remove(Title.Length - 1, 1);
		}

		private void UpdateTitleAsterisk()
        {
			if (File.Exists(loadedFilePath))
			{

			}
		}

        #region MenuBar_File
        private void MenuBar_New_Click(object sender, RoutedEventArgs e)
        {
			TextRange textRange = new(baseTextBox.Document.ContentStart,
				baseTextBox.Document.ContentEnd);
			MessageBoxResult result = MessageBox.Show($"Do you want to save changes to {Title[..Title.IndexOf(" - TenPad")]}", "TenPad", MessageBoxButton.YesNoCancel);
			if (result == MessageBoxResult.Yes)
			{
				SaveFileDialog saveFileDialog = new()
				{
					Filter = "txt files (*.txt)|*.txt|All files (*.*)|*.*",
					FilterIndex = 2,
					RestoreDirectory = true
				};
				if (saveFileDialog.ShowDialog() == true)
                {
					File.WriteAllText(saveFileDialog.FileName, textRange.Text);
					Title = $"Untitled - TenPad";
					baseTextBox.Document.Blocks.Clear();
				}
			}
			else if(result == MessageBoxResult.No)
			{
				Title = $"Untitled - TenPad";
				baseTextBox.Document.Blocks.Clear();
			}
        }

        private void MenuBar_NewWindow_Click(object sender, RoutedEventArgs e)
        {
			new MainWindow().Show();
        }

        private void MenuBar_Open_Click(object sender, RoutedEventArgs e)
        {
			OpenFileDialog openFileDialog = new()
			{
				Filter = "txt files (*.txt)|*.txt|All files (*.*)|*.*",
				FilterIndex = 2,
				RestoreDirectory = true
			};
			if (openFileDialog.ShowDialog() == true)
				LoadFile(openFileDialog.FileName);
		}

        private void MenuBar_Save_Click(object sender, RoutedEventArgs e)
        {
			TextRange textRange = new(baseTextBox.Document.ContentStart,
				baseTextBox.Document.ContentEnd);
			if (File.Exists(loadedFilePath))
				File.WriteAllText(loadedFilePath, textRange.Text);
			else
            {
				SaveFileDialog saveFileDialog = new()
				{
					Filter = "txt files (*.txt)|*.txt|All files (*.*)|*.*",
					FilterIndex = 2,
					RestoreDirectory = true
				};
				if (saveFileDialog.ShowDialog() == true)
				{
					File.WriteAllText(saveFileDialog.FileName, textRange.Text);
					LoadFile(saveFileDialog.FileName);
				}
			}
			if (Title.Contains("*") == true)
				Title = Title.Remove(Title.Length - 1, 1);
		}

        private void MenuBar_SaveAs_Click(object sender, RoutedEventArgs e)
        {
			TextRange textRange = new(baseTextBox.Document.ContentStart,
				baseTextBox.Document.ContentEnd);
			SaveFileDialog saveFileDialog = new()
			{
				Filter = "txt files (*.txt)|*.txt|All files (*.*)|*.*",
				FilterIndex = 2,
				RestoreDirectory = true
			};
			if (saveFileDialog.ShowDialog() == true)
            {
				File.WriteAllText(saveFileDialog.FileName, textRange.Text);
				LoadFile(saveFileDialog.FileName);
			}
			if (Title.Contains("*") == true)
				Title = Title.Remove(Title.Length - 1, 1);
		}

        private void MenuBar_PageSetup_Click(object sender, RoutedEventArgs e)
        {
			
        }

        private void MenuBar_Print_Click(object sender, RoutedEventArgs e)
        {

        }

        private void MenuBar_Close_Click(object sender, RoutedEventArgs e)
        {
			Close();
        }
        #endregion

        #region MenuBar_Edit
        private void MenuItem_Cut_Click(object sender, RoutedEventArgs e)
        {
			Clipboard.SetText(baseTextBox.Selection.Text);
		}

        private void MenuItem_Copy_Click(object sender, RoutedEventArgs e)
        {
			Clipboard.SetText(baseTextBox.Selection.Text);
			baseTextBox.Selection.Text = "";
		}

        private void MenuItem_Paste_Click(object sender, RoutedEventArgs e)
        {
			baseTextBox.Paste();
		}

        private void MenuItem_Delete_Click(object sender, RoutedEventArgs e)
        {
			baseTextBox.Selection.Text = "";
		}

        private void MenuItem_Search_Online_Click(object sender, RoutedEventArgs e)
        {
			string uri = string.Format("http://www.google.com/search?q={0}", baseTextBox.Selection);
			Process.Start(new ProcessStartInfo
			{
				FileName = uri,
				UseShellExecute = true
			});
		}

        private void MenuItem_Find_Click(object sender, RoutedEventArgs e)
        {
			ReplaceRow.Height = new GridLength(0);
			Storyboard sb = new();
			var animation = new DoubleAnimation
			{
				Duration = TimeSpan.FromSeconds(0.1875),
				From = 0,
				To = .95
			};
			Storyboard.SetTarget(animation, FindReplacePopup);
			Storyboard.SetTargetProperty(animation, new PropertyPath("Opacity"));
			sb.Children.Clear();
			sb.Children.Add(animation);
			sb.Begin();
		}

		private void ExtendFindPanel_Click(object sender, RoutedEventArgs e)
		{
			Storyboard sb = new();
			if (ReplaceRow.Height.Value < 23)
            {
				var animation = new DoubleAnimation
				{
					Duration = TimeSpan.FromSeconds(0.1875),
					From = 23,
					To = 50
				};
				Storyboard.SetTarget(animation, FindReplacePopup);
				Storyboard.SetTargetProperty(animation, new PropertyPath("Height"));
				sb.Children.Clear();
				sb.Children.Add(animation);
				sb.Begin();
				ReplaceRow.Height = new GridLength(23.0);
				ExtendFindPanel.Content = FindResource("ExtendFRPanel_Up");
			}
			else
            {
				var animation = new DoubleAnimation
				{
					Duration = TimeSpan.FromSeconds(0.1875),
					From = 50,
					To = 23
				};
				Storyboard.SetTarget(animation, FindReplacePopup);
				Storyboard.SetTargetProperty(animation, new PropertyPath("Height"));
				sb.Children.Clear();
				sb.Children.Add(animation);
				sb.Begin();
				ReplaceRow.Height = new GridLength(0);
				ExtendFindPanel.Content = FindResource("ExtendFRPanel_Dn");
			}
		}

		private void PerformQuery_Click(object sender, RoutedEventArgs e)
		{
			
		}

		private void MenuItem_Find_Replace_Click(object sender, RoutedEventArgs e)
        {
			FindReplacePopup.Height = 50.0;
			ExtendFindPanel.Content = FindResource("ExtendFRPanel_Up");
			ReplaceRow.Height = new GridLength(25);
			Storyboard sb = new();
			var animation = new DoubleAnimation
			{
				Duration = TimeSpan.FromSeconds(0.1875),
				From = 0,
				To = 1.0
			};
			Storyboard.SetTarget(animation, FindReplacePopup);
			Storyboard.SetTargetProperty(animation, new PropertyPath("Opacity"));
			sb.Children.Clear();
			sb.Children.Add(animation);
			sb.Begin();
		}

		private void CloseFRPanel_Click(object sender, RoutedEventArgs e)
		{
			Storyboard sb = new();
			var animation = new DoubleAnimation
			{
				Duration = TimeSpan.FromSeconds(0.1875),
				From = 0.95,
				To = 0.0
			};
			Storyboard.SetTarget(animation, FindReplacePopup);
			Storyboard.SetTargetProperty(animation, new PropertyPath("Opacity"));
			sb.Children.Clear();
			sb.Children.Add(animation);
			sb.Begin();
			ExtendFindPanel.Content = FindResource("ExtendFRPanel_Dn");
		}

		private void MenuItem_GoTo_Click(object sender, RoutedEventArgs e)
        {
			Storyboard sb = new();
			var animation = new DoubleAnimation
			{
				Duration = TimeSpan.FromSeconds(0.1875),
				From = 0,
				To = .95
			};
			Storyboard.SetTarget(animation, GoToPopup);
			Storyboard.SetTargetProperty(animation, new PropertyPath("Opacity"));
			sb.Children.Clear();
			sb.Children.Add(animation);
			sb.Begin();
		}

        private void MenuItem_SelectAll_Click(object sender, RoutedEventArgs e)
        {
			baseTextBox.SelectAll();
        }

        private void MenuItem_TimeDate_Click(object sender, RoutedEventArgs e)
        {
			var tr = new TextRange(baseTextBox.Document.ContentStart, baseTextBox.Document.ContentEnd);

			if (tr.Text.Length == 2)
			{
				if (tr.Text == "\r\n")
				{
					tr.Text = tr.Text.TrimStart(new[] { '\r', '\n' });
				}
			}

			/* Changing the text is the only way I can get the date to insert at the beginning */
			tr.Text = "I need a beer at ";

			baseTextBox.CaretPosition.InsertTextInRun(DateTime.Now.ToString());
		}

		#endregion

		#region MenuBar_Format
		private void MenuItem_WordWrap_Checked(object sender, RoutedEventArgs e)
        {
			baseTextBox.Document.PageWidth = baseTextBox.Width;
		}

		private void MenuItem_WordWrap_Unchecked(object sender, RoutedEventArgs e)
		{
			baseTextBox.Document.PageWidth = double.PositiveInfinity;
		}

		private void MenuItem_Font_Click(object sender, RoutedEventArgs e)
		{
			if (baseTextBox.Selection is not null)
            {
				new FontDialog(this, true).ShowDialog();
			}
			else
            {
				new FontDialog(this, false).ShowDialog();
			}
		}

        #endregion

        #region MenuBar_View
        private void MenuItem_ZoomIn_Click(object sender, RoutedEventArgs e)
        {
			baseTextBox.FontSize++;
			ZoomDisplay.Text = $"{Math.Round(baseTextBox.FontSize / 11 *100, 0)}%";
        }

        private void MenuItem_ZoomOut_Click(object sender, RoutedEventArgs e)
        {
			baseTextBox.FontSize--;
			ZoomDisplay.Text = $"{Math.Round(baseTextBox.FontSize / 11 * 100, 0)}%";
		}

        private void MenuItem_ZoomReset_Click(object sender, RoutedEventArgs e)
        {
			baseTextBox.FontSize = 11;
			ZoomDisplay.Text = $"100%";
		}

		private void MenuBar_StatusBar_Click(object sender, RoutedEventArgs e)
		{
			if (MenuBar_StatusBar.IsChecked)
            {
				StatusBarRow.Height = new GridLength(22);
			}
            else 
			{ 
				StatusBarRow.Height = new GridLength(0);
			}
				
		}
		#endregion

		#region MenuBar_Help

		private void MenuItem_ViewHelp_Click(object sender, RoutedEventArgs e)
		{

		}

		private void MenuItem_About_Click(object sender, RoutedEventArgs e)
		{

		}

		#endregion

		private void PerformGoTo_Click(object sender, RoutedEventArgs e)
		{ 
			baseTextBox.CaretPosition = baseTextBox.CaretPosition.GetLineStartPosition(Convert.ToInt32(GoToLineNumber.Text));
			GoToPopup.Opacity = 0;
		}

        private void CloseGTPanel_Click(object sender, RoutedEventArgs e)
        {
			Storyboard sb = new();
			var animation = new DoubleAnimation
			{
				Duration = TimeSpan.FromSeconds(0.1875),
				From = 0.95,
				To = 0.0
			};
			Storyboard.SetTarget(animation, GoToPopup);
			Storyboard.SetTargetProperty(animation, new PropertyPath("Opacity"));
			sb.Children.Clear();
			sb.Children.Add(animation);
			sb.Begin();
		}
    }
}

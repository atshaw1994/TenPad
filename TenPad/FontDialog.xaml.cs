using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace TenPad
{
    /// <summary>
    /// Interaction logic for FontDialog.xaml
    /// </summary>
    public partial class FontDialog : Window
    {
        private readonly MainWindow _mainWindow;
		private readonly bool HasSelection;
        public FontDialog()
        {
            InitializeComponent();
            _mainWindow = new MainWindow();
			HasSelection = false;
        }
        public FontDialog(MainWindow mainWindow, bool Selection)
        {
            InitializeComponent();
            _mainWindow = mainWindow;
			HasSelection = Selection;
		}
		private void Window_Loaded(object sender, RoutedEventArgs e)
		{
            foreach (FontFamily item in FontSelection.Items)
				if (item.ToString().Equals("Consolas")) FontSelection.SelectedItem = item;
			PopulateFontSizeListBox();
			FontStyleSelection.SelectedIndex = 0;
			FontSizeSelection.SelectedIndex = 9;
		}

		private void PopulateFontSizeListBox()
        {
			FontSizeSelection.Items.Add(8);
			FontSizeSelection.Items.Add(9);
			FontSizeSelection.Items.Add(10);
			FontSizeSelection.Items.Add(11);
			FontSizeSelection.Items.Add(12);
			FontSizeSelection.Items.Add(14);
			FontSizeSelection.Items.Add(16);
			FontSizeSelection.Items.Add(18);
			FontSizeSelection.Items.Add(20);
			FontSizeSelection.Items.Add(22);
			FontSizeSelection.Items.Add(24);
			FontSizeSelection.Items.Add(26);
			FontSizeSelection.Items.Add(28);
			FontSizeSelection.Items.Add(36);
			FontSizeSelection.Items.Add(48);
			FontSizeSelection.Items.Add(72);
		}

		#region BorderlessMethods
		private void CaptionBar_CloseButton_Click(object sender, RoutedEventArgs e)
		{
			Close();
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

        private void FontSizeSelection_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
			if (FontSizeSelection.Items.Count > 0 && FontSizeSelection.SelectedItem is not null)
				SampleText.FontSize = Convert.ToDouble(FontSizeSelection.SelectedItem);
		}

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
			Close();
        }

        private void OkayButton_Click(object sender, RoutedEventArgs e)
        {
			if (HasSelection)
            {
				_mainWindow.baseTextBox.Selection.ApplyPropertyValue(FontFamilyProperty, SampleText.FontFamily);
				_mainWindow.baseTextBox.Selection.ApplyPropertyValue(FontStyleProperty, SampleText.FontStyle);
				_mainWindow.baseTextBox.Selection.ApplyPropertyValue(FontSizeProperty, SampleText.FontSize);
			}
			else
            {
				_mainWindow.baseTextBox.FontFamily = SampleText.FontFamily;
				_mainWindow.baseTextBox.FontStyle =  SampleText.FontStyle;
				_mainWindow.baseTextBox.FontSize = SampleText.FontSize;
			}
			Close();
        }
    }
}

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

using ToggleButton = System.Windows.Controls.Primitives.ToggleButton;

namespace BmsDisplayConfig
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        ISystemModel _sysModel;
        IDataModel _datModel;

        RectXYWH _desktopRect;

        public MainWindow()
        {
            InitializeComponent();

            _datModel = DataModelFactory.CreateDataModel();
            _datModel.LoadDspFileOrDefault();

            RectXYWH modelRect = _datModel.Get3dSimWindowPlacement();

            _sysModel = SystemModelFactory.CreateSystemModel();
            _desktopRect = _sysModel.GetVirtualDesktopRect();

            x_desktopCanvas.Width = _desktopRect.width / 8;
            x_desktopCanvas.Height = _desktopRect.height / 8;

            RectXYWH[] monitorRects = _sysModel.GetMonitorRects();

            x_desktopCanvas.Children.Clear();

            int i = 0;
            foreach (RectXYWH r in monitorRects)
            {
                ++i;

                ToggleButton clickableRect = _CreateToggleButton(i, r);
                clickableRect.Content = i.ToString();
                clickableRect.IsChecked = (modelRect.Contains(r));
                clickableRect.Foreground = new SolidColorBrush((modelRect.Contains(r)) ? Colors.WhiteSmoke : Colors.DarkSlateGray);

                x_desktopCanvas.Children.Add(clickableRect);
            }

            x_saveButton.IsEnabled = _datModel.DirtyFlag = false;
        }

        ToggleButton _CreateToggleButton(int i, RectXYWH r)
        {
            ToggleButton tb = new ToggleButton();

            tb.DataContext = (object)(r);//boxed copy of the desktop-space rect struct

            tb.FontSize = 48;
            tb.FontWeight = FontWeights.Bold;
            tb.BorderThickness = new System.Windows.Thickness(5);

            Rect rWpf = _TranslateDesktopToCanvasSpace(r);
            tb.SetValue(Canvas.LeftProperty, rWpf.Left);
            tb.SetValue(Canvas.TopProperty, rWpf.Top);
            tb.Width = rWpf.Width;
            tb.Height = rWpf.Height;

            tb.Click += x_OnClickRectangleButton;

            return tb;
        }

        RectXYWH _unused_FindClosestSysModelRect(RectXYWH dspRect)
        {
            RectXYWH[] monitorRects = _sysModel.GetMonitorRects();

            int min_diff = Int32.MaxValue;
            RectXYWH min_rect = monitorRects[0];
            foreach (RectXYWH r in monitorRects)
            {
                int diff = _unused_MeasureDifference(r, dspRect);
                if (diff < min_diff)
                {
                    min_rect = r;
                    min_diff = diff;
                }
            }

            return min_rect;
        }

        static int _unused_MeasureDifference(RectXYWH a, RectXYWH b)
        {
            // Simple fitness function: sum of diffs of the left, top, right, and bottom edges.
            int dX = a.x - b.x;
            int dY = a.y - b.y;
            int dW = a.width - b.width;
            int dH = a.height - b.height;
            return Math.Abs(dX) + Math.Abs(dY) + Math.Abs(dW) + Math.Abs(dH);
        }

        Rect _TranslateDesktopToCanvasSpace(RectXYWH rDesktop)
        {
            rDesktop.x -= _desktopRect.x;
            rDesktop.y -= _desktopRect.y;

            rDesktop.x /= 8;
            rDesktop.y /= 8;
            rDesktop.width /= 8;
            rDesktop.height /= 8;

            Rect rWpf = new Rect(rDesktop.x, rDesktop.y, rDesktop.width, rDesktop.height);
            return rWpf;
        }

        private void x_OnClickSave(object sender, RoutedEventArgs e)
        {
            _datModel.SaveDspFile();
            x_saveButton.IsEnabled = _datModel.DirtyFlag = false;
        }

        private void x_OnClickClose(object sender, RoutedEventArgs e)
        {
            // Invoke the OnClosing override, below.
            this.Close();
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);
            if (!_datModel.DirtyFlag) return;

            MessageBoxResult mbr = System.Windows.MessageBox.Show(this, "Save changes before exiting?", "WARNING", MessageBoxButton.YesNoCancel, MessageBoxImage.Warning);
            switch (mbr)
            {
                case MessageBoxResult.Yes:
                    _datModel.SaveDspFile();
                    break;
                case MessageBoxResult.No:
                    e.Cancel = false;//continue closing
                    break;
                case MessageBoxResult.Cancel:
                    e.Cancel = true;
                    break;
                default:
                    throw new InvalidProgramException();
            }

            return;
        }

        private void x_OnClickRectangleButton(object sender, RoutedEventArgs e)
        {
            ToggleButton button = sender as ToggleButton;
            if (button == null) throw new InvalidProgramException();

            if (button.IsChecked.Value)
                button.Foreground = new SolidColorBrush(Colors.WhiteSmoke);
            else
                button.Foreground = new SolidColorBrush(Colors.DarkSlateGray);

            // Set the effective rect to be the union encompassing all selected displays.
            int minL = Int32.MaxValue, minT = Int32.MaxValue;
            int maxR = Int32.MinValue, maxB = Int32.MinValue;

            int numSelected = 0;
            foreach (ToggleButton child in x_desktopCanvas.Children)
            {
                if (child.IsChecked.HasValue && child.IsChecked.Value)
                {
                    ++numSelected;

                    RectXYWH r = (RectXYWH)(child.DataContext);
                    minL = Math.Min(minL, r.x);
                    minT = Math.Min(minT, r.y);
                    maxR = Math.Max(maxR, r.GetRight());
                    maxB = Math.Max(maxB, r.GetBottom());
                }
            }
            System.Diagnostics.Debug.Assert(maxR > minL);
            System.Diagnostics.Debug.Assert(maxB > minT);

            // Default to primary monitor rect, if none selected.
            if (numSelected == 0)
            {
                RectXYWH[] monitorRects = _sysModel.GetMonitorRects();
                RectXYWH primaryMon = monitorRects[0];

                minL = primaryMon.x;
                minT = primaryMon.y;
                maxR = primaryMon.GetRight();
                maxB = primaryMon.GetBottom();
            }

            RectXYWH rUnion = RectXYWH.FromLTRB(minL, minT, maxR, maxB);
            _datModel.Set3dSimWindowPlacement(rUnion);
            x_saveButton.IsEnabled = _datModel.DirtyFlag = true;
        }

    }
}

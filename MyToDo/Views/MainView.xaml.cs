using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace MyToDo.Views
{
    /// <summary>
    /// MainView.xaml 的交互逻辑
    /// </summary>
    public partial class MainView : Window
    {
        public MainView()
        {
            InitializeComponent();
            btnMin.Click += (s, e) => { this.WindowState = WindowState.Minimized; };
            btnMax.Click += (s, e) => { this.WindowState = WindowState.Maximized; };
            btnClose.Click += (s, e) => { this.Close(); };
            this.Closing += (s, e) => { App.Current.Shutdown(); };
            this.ColorZone.MouseDoubleClick += (s, e) =>
            {
                if (this.WindowState == WindowState.Maximized)
                {
                    this.WindowState = WindowState.Normal;
                }
                else
                {
                    this.WindowState = WindowState.Maximized;
                }
            };
            this.ColorZone.MouseMove += (s, e) =>
            {
                if (e.LeftButton == MouseButtonState.Pressed)
                {
                    this.DragMove();
                }
            };

            menuBar.SelectionChanged += (s, e) =>
            {
                DrawerHost.IsLeftDrawerOpen = false;
            };
        }
    }
}

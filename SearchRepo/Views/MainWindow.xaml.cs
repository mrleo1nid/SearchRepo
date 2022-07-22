using SearchRepo.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace SearchRepo.Views
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void ResultDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var grid = (DataGrid)sender;
            var row = (SearchResult) grid.SelectedItem;
            if (row != null)
                FocusFile(row.Path);

        }
        private void FocusFile(string file)
        {
          try
          {
            System.Diagnostics.Process.Start("explorer.exe", @"/select, " + file);
          }
          catch(Exception exc){}
        }
    }
}

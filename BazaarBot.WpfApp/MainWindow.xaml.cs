﻿using System;
using System.Collections.Generic;
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

namespace BazaarBot.WpfApp
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

        private void DataGrid_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            var dataGrid = sender as DataGrid;
            string[][] array = dataGrid.DataContext as string[][];
            if (!dataGrid.Columns.Any())
            {
                if (array != null)
                {
                    for (int i = 0; i < array[0].Length; i++)
                    {
                        var column = new DataGridTextColumn();
                        column.Binding = new Binding(string.Format("[{0}]", i));
                        dataGrid.Columns.Add(column);
                    }
                }
                
            }
            dataGrid.ItemsSource = array;
        }
    }
}

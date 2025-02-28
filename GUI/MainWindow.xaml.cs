﻿using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using EngineLayer;

namespace GUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private string fileDialogFilter = "Proteoform Results(*.tsv;*.txt)|*.tsv;*.txt";
        public List<DataForDataGrid> ValidationFilePath = new List<DataForDataGrid>();
        public List<DataForDataGrid> ResultFilePaths = new List<DataForDataGrid>();

        public MainWindow()
        {
            InitializeComponent();
            Title = "Proteoform Classifier";
            LoadParams();
            dataGridValidationResults.DataContext = ValidationFilePath;
            dataGridProteoformResults.DataContext = ResultFilePaths;

            WriteOutput.NotificationHandler += NotificationHandler;

        }

        private void LoadParams()
        {
            proteoformFormatDelimitedRadioButtonv.IsChecked = true;
            proteoformFormatDelimitedRadioButton.IsChecked = true;
            proteoformFormaParentheticalRadioButtonv.IsChecked = false;
            proteoformFormaParentheticalRadioButton.IsChecked = false;
            proteoformAndGeneDelimiterTextBoxv.Text = "|";
            proteoformAndGeneDelimiterTextBox.Text = "|";
            HeaderCheckBox.IsChecked = true;
            HeaderCheckBoxv.IsChecked = true;
            UpdateExample();
        }

        private void NotificationHandler(object sender, StringEventArgs e)
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.BeginInvoke(new Action(() => NotificationHandler(sender, e)));
            }
            else
            {
                notificationsTextBox.AppendText(e.S);
                notificationsTextBox.AppendText(Environment.NewLine);
            }
        }

        private void AddValidationResults_Click(object sender, RoutedEventArgs e)
        {
            var openPicker = StartOpenFileDialog(fileDialogFilter, false);

            if (openPicker.ShowDialog() == true)
            {
                ValidationFilePath.Clear();
                ValidationFilePath.Add(new DataForDataGrid(openPicker.FileNames.First()));
            }
            RefreshFileGrid();
        }

        private OpenFileDialog StartOpenFileDialog(string filter, bool multiselect)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = filter,
                FilterIndex = 1,
                RestoreDirectory = true,
                Multiselect = multiselect
            };

            return openFileDialog;
        }

        private void AnalyzeValidationResults_Click(object sender, RoutedEventArgs e)
        {
            if (ValidationFilePath.Count == 1)
            {
                ToggleButtons(false);
                Validate.ValidateResults(ValidationFilePath.First().FilePath);
                ToggleButtons(true);
            }
            else
            {
                WriteOutput.Notify("Attempted to validate, but no result file was found. Please add a file and try again.");
            }
        }

        private void RefreshFileGrid()
        {
            dataGridValidationResults.CommitEdit(DataGridEditingUnit.Row, true);
            dataGridProteoformResults.CommitEdit(DataGridEditingUnit.Row, true);

            dataGridValidationResults.Items.Refresh();
            dataGridProteoformResults.Items.Refresh();
        }

        private string GetPathOfItem(object sender, RoutedEventArgs e)
        {
            DataForDataGrid item = (DataForDataGrid)sender;
            return item.FilePath;
        }

        private void DeleteAll_Click(object sender, RoutedEventArgs e)
        {
            ValidationFilePath.Clear();
            ResultFilePaths.Clear();
            RefreshFileGrid();
        }

        private void AddProteoformResults_Click(object sender, RoutedEventArgs e)
        {
            var openPicker = StartOpenFileDialog(fileDialogFilter, true);

            if (openPicker.ShowDialog() == true)
            {
                foreach (string filename in openPicker.FileNames)
                {
                    ResultFilePaths.Add(new DataForDataGrid(filename));
                }

                RefreshFileGrid();
            }
        }

        /// <summary>
        /// Opens the requested URL with the user's web browser.
        /// </summary>
        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            string path = e.Uri.ToString();
            OpenFile(path);
        }

        private void OpenFile(string path)
        {
            var p = new Process();

            p.StartInfo = new ProcessStartInfo()
            {
                UseShellExecute = true,
                FileName = path
            };

            p.Start();
        }

        /// <summary>
        /// Event fires when a file is dragged-and-dropped into the GUI.
        /// </summary>
        private void Window_Drop(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            List<string> validatedFiles = new List<string>();

            if (files != null)
            {
                foreach (string file in files)
                {
                    var filename = Path.GetFileName(file);
                    var extension = Path.GetExtension(filename).ToLowerInvariant();
                    if(extension.Equals(".txt")||extension.Equals(".tsv"))
                    {
                        validatedFiles.Add(file);
                    }
                }
            }
            if (validatedFiles.Count!=0)
            {
                var selectedItem = (TabItem)MainWindowTabControl.SelectedItem;
                if (selectedItem.Header.Equals("Validate Software"))
                {
                    foreach (string file in files)
                    {
                        ValidationFilePath.Clear();
                        ValidationFilePath.Add(new DataForDataGrid(file));
                    }
                }
                else if (selectedItem.Header.Equals("Classify PrSMs"))
                {
                    foreach (string file in files)
                    {
                        ResultFilePaths.Add(new DataForDataGrid(file));
                    }
                }
                else
                {
                    WriteOutput.Notify("Please select a tab on the left side before dragging and dropping files.");
                }
                RefreshFileGrid();
            }
        }

        //needed to build, doesn't serve a function
        private void TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void AnalyzeProteoformResults_Click(object sender, RoutedEventArgs e)
        {
            if (ResultFilePaths.Count > 0)
            {
                ToggleButtons(false);
                Classifier.ClassifyResultFiles(ResultFilePaths.Select(x => x.FilePath).ToList(), aggregateOutputCheckBox.IsChecked.Value);
                ToggleButtons(true);
            }
            else
            {
                WriteOutput.Notify("Attempted to classify, but no result files were found. Please add a file and try again.");
            }
        }

        private void ToggleButtons(bool enabled)
        {
            aggregateOutputCheckBox.IsEnabled = enabled;
            AnalyzeProteoformResults.IsEnabled = enabled;
            AnalyzeValidationResults.IsEnabled = enabled;
        }

        private void Delimited_Click(object sender, RoutedEventArgs e)
        {
            proteoformAndGeneDelimiterTextBox.IsEnabled = proteoformFormatDelimitedRadioButton.IsChecked.Value;
            proteoformAndGeneDelimiterTextBoxv.IsEnabled = proteoformFormatDelimitedRadioButton.IsChecked.Value;
            ReadResults.ModifyProteoformFormat(GetSelectedAmbiguityFormat(false));
            proteoformFormatDelimitedRadioButtonv.IsChecked = proteoformFormatDelimitedRadioButton.IsChecked;
            proteoformFormaParentheticalRadioButtonv.IsChecked = proteoformFormaParentheticalRadioButton.IsChecked;
            proteoformFormatMultiRowRadioButtonv.IsChecked = proteoformFormatMultiRowRadioButton.IsChecked;
            UpdateExample();
        }

        private void Delimited_Clickv(object sender, RoutedEventArgs e)
        {
            proteoformAndGeneDelimiterTextBoxv.IsEnabled = proteoformFormatDelimitedRadioButtonv.IsChecked.Value;
            proteoformAndGeneDelimiterTextBox.IsEnabled = proteoformFormatDelimitedRadioButtonv.IsChecked.Value;
            ReadResults.ModifyProteoformFormat(GetSelectedAmbiguityFormat(true));
            proteoformFormatDelimitedRadioButton.IsChecked = proteoformFormatDelimitedRadioButtonv.IsChecked;
            proteoformFormaParentheticalRadioButton.IsChecked = proteoformFormaParentheticalRadioButtonv.IsChecked;
            proteoformFormatMultiRowRadioButton.IsChecked = proteoformFormatMultiRowRadioButtonv.IsChecked;
            UpdateExample();
        }

        private ProteoformFormat GetSelectedAmbiguityFormat(bool validation)
        {
            if(validation)
            {
                if (proteoformFormatDelimitedRadioButtonv.IsChecked.Value)
                {
                    return ProteoformFormat.Delimited;
                }
                else if (proteoformFormaParentheticalRadioButtonv.IsChecked.Value)
                {
                    return ProteoformFormat.Parenthetical;
                }
                else //if(proteoformFormatMultiRowRadioButtonv.IsChecked.Value)
                {
                    return ProteoformFormat.MultipleRows;
                } 
            }
            else //classification
            {
                if (proteoformFormatDelimitedRadioButton.IsChecked.Value)
                {
                    return ProteoformFormat.Delimited;
                }
                else if (proteoformFormaParentheticalRadioButton.IsChecked.Value)
                {
                    return ProteoformFormat.Parenthetical;
                }
                else //if(proteoformFormatMultiRowRadioButton.IsChecked.Value)
                {
                    return ProteoformFormat.MultipleRows;
                }
            }
        }

        private void Delimiter_TextChange(object sender, TextChangedEventArgs e)
        {
            if (proteoformAndGeneDelimiterTextBox.Text.Length != 0)
            {
                proteoformAndGeneDelimiterTextBox.Text = proteoformAndGeneDelimiterTextBox.Text[0].ToString();//only use first char
                proteoformAndGeneDelimiterTextBoxv.Text = proteoformAndGeneDelimiterTextBox.Text;
                ReadResults.ModifySequenceAndGeneDelimiter(proteoformAndGeneDelimiterTextBox.Text[0]);
            }
            else
            {
                proteoformAndGeneDelimiterTextBox.Text = ReadResults.GetProteoformDelimiter().ToString();
                proteoformAndGeneDelimiterTextBoxv.Text = ReadResults.GetProteoformDelimiter().ToString();
            }
            UpdateExample();
        }

        private void Delimiter_TextChangev(object sender, TextChangedEventArgs e)
        {
            if (proteoformAndGeneDelimiterTextBoxv.Text.Length != 0)
            {
                proteoformAndGeneDelimiterTextBoxv.Text = proteoformAndGeneDelimiterTextBoxv.Text[0].ToString();
                proteoformAndGeneDelimiterTextBox.Text = proteoformAndGeneDelimiterTextBoxv.Text;
                ReadResults.ModifySequenceAndGeneDelimiter(proteoformAndGeneDelimiterTextBoxv.Text[0]);
            }
            else
            {
                proteoformAndGeneDelimiterTextBoxv.Text = ReadResults.GetProteoformDelimiter().ToString();
                proteoformAndGeneDelimiterTextBox.Text = ReadResults.GetProteoformDelimiter().ToString();
            }
            UpdateExample();
        }

        private void UpdateExample()
        {
            const string a = "XM[Oxidation]AMX";
            const string b = "XMAM[Oxidation]X";
            string thingy = "XM[Oxidation]AMX";
            if (ReadResults.GetProteoformFormat() == ProteoformFormat.Delimited)
            {
                thingy = a + ReadResults.GetProteoformDelimiter().ToString() + b;
            }
            else if (ReadResults.GetProteoformFormat() == ProteoformFormat.Parenthetical)
            {
                thingy = "X(MAM)[Oxidation]X";
            }
            exampleTextBox.Text = thingy;
            exampleTextBoxv.Text = thingy;
            exampleTextBox.FontSize = 9;
            exampleTextBoxv.FontSize = 9;
        }

        private void ClearProteoformResults_Click(object sender, RoutedEventArgs e)
        {
            ResultFilePaths.Clear();
            RefreshFileGrid();
        }

        private void ClearValidationResults_Click(object sender, RoutedEventArgs e)
        {
            ValidationFilePath.Clear();
            RefreshFileGrid();
        }

        private void Header_Click(object sender, RoutedEventArgs e)
        {
            HeaderCheckBoxv.IsChecked = HeaderCheckBox.IsChecked;
            ReadResults.ModifyHeader(HeaderCheckBox.IsChecked.Value);
        }

        private void Header_Clickv(object sender, RoutedEventArgs e)
        {
            HeaderCheckBox.IsChecked = HeaderCheckBoxv.IsChecked;
            ReadResults.ModifyHeader(HeaderCheckBox.IsChecked.Value);
        }
    }
}
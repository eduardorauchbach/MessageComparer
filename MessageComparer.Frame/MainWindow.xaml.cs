using CsvHelper;
using CsvHelper.Configuration;
using MessageComparer.Engine.Helpers;
using MessageComparer.Engine.Models;
using Microsoft.Win32;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Xml.Linq;

namespace MessageComparer.Frame
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private List<KeysConfigData> KeysConfig;

        public MainWindow()
        {
            KeysConfig = new();

            InitializeComponent();

            LoadGrid();
        }

        private void Arrange(object sender, RoutedEventArgs e)
        {
            ExpandoObject? obj1;
            ExpandoObject? obj2;

            try
            {
                obj1 = GetExpando(txtMessage1);
                obj2 = GetExpando(txtMessage2);

                var b = ObjectComparer.GetDiff(obj1, obj2, KeysConfig);

                dtCompare.ItemsSource = LoadComparison(b).ToList();
                dtCompare.ScrollIntoView(dtCompare.Items[0]);

                if (((IEnumerable<CompareData>)dtCompare.ItemsSource).All(x => x.IsEqual))
                {
                    MessageBox.Show("The messages match!");
                }

                ShowLabel(lblArrangeMessage, "Done");
            }
            catch (Exception ex)
            {
                if(ex.Message.ToLower().Contains("failed to compare two elements in the array"))
                {
                    MessageBox.Show("Distinct list detected, please fill the keys so it's possible to compare");
                }
                else
                {
                    MessageBox.Show(ex.Message);
                }
            }
        }

        private ExpandoObject? GetExpando(TextBox textBox)
        {
            string message = textBox.Text.Trim();
            string name = (textBox.ToolTip as string) ?? string.Empty;

            try
            {
                if (message.StartsWith("<"))
                {
                    message = Regex.Replace(message, @"<([\/]?)[\S]*:([^> ]*)( xmlns:[^>]*)?>", "<$1$2>");

                    XDocument doc = XDocument.Parse(message);
                    message = JsonConvert.SerializeXNode(doc);
                    message = Regex.Replace(message, @"({)(((""[^""]*""[ :]*){""Key""[ :]*(""[^""]*""),""Value""[ :]*(""[^""]*"")}[,]*)*)(})([,}])?", "[$2]$7");
                    message = Regex.Replace(message, @"(""[^""]*""[ :]*){""Key""[ :]*(""[^""]*""),""Value""[ :]*(""[^""]*"")}", "{\"Key\":$2,\"Value\":$3}");
                }
                else if (!message.StartsWith("{"))
                {
                    try
                    {
                        if (!(message.StartsWith("{") || message.StartsWith("[")))
                        {
                            var delimiter = Regex.Match(message, "[\\S][^|,;\\t]*([|,;\\t])")?.Groups[1]?.Value?[0];

                            if (!delimiter.HasValue)
                            {
                                throw new Exception("No delimiter found in the possible CSV file");
                            }

                            FixFormating(delimiter.Value);

                            var textReader = new StringReader(message);
                            var csvr = new CsvReader(textReader, new CsvConfiguration(CultureInfo.InvariantCulture)
                            {
                                HasHeaderRecord = CheckForHeader(delimiter.Value),
                                HeaderValidated = null,
                                Delimiter = delimiter.ToString(),
                                Mode = CsvMode.RFC4180,
                                TrimOptions = TrimOptions.Trim,
                            });

                            var obj = csvr.GetRecords<dynamic>();

                            message = JsonConvert.SerializeObject(obj);
                            message = $"{{\"Rows\":{message}}}";
                        }
                    }
                    catch
                    {
                        message = textBox.Text;
                    }
                }

                return JsonConvert.DeserializeObject<ExpandoObject>(message);
            }
            catch (Exception ex)
            {
                throw new Exception($"Invalid {name}:\n\n{ex.Message}");
            }

            void FixFormating(char delimiter)
            {
                var rows = message.Split('\n');
                var count = rows.Max(x => x.Count(y => y == delimiter));

                for (int i = 0; i < rows.Length - 1; i++)
                {
                    var diff = count - rows[i].Count(y => y == delimiter);
                    if (diff > 0)
                    {
                        rows[i] += new String(delimiter, diff);
                    }
                }

                message = string.Join('\n', rows);
            }

            bool CheckForHeader(char delimiter)
            {
                var l1 = txtMessage1.Text.Split('\n')[0].Trim();
                var l2 = txtMessage2.Text.Split('\n')[0].Trim();

                var l1a = l1.Split(delimiter);
                var l2a = l2.Split(delimiter);

                if (l1a.Distinct().Count() != l1a.Count() ||
                    l2a.Distinct().Count() != l2a.Count() ||
                    l1a.Any(x => x == "") || l2a.Any(x => x == ""))
                {
                    return false;
                }
                return l1 == l2;
            }
        }

        private static IEnumerable<CompareData> LoadComparison((string, string) value)
        {
            var v1 = value.Item1.Split('\n');
            var v2 = value.Item2.Split('\n');

            for (int i = 0; i < v1.Length; i++)
            {
                yield return new CompareData(i + 1, v1[i], v2[i]);
            }
        }

        private void PreviousDiff(object sender, RoutedEventArgs e)
        {
            CompareData? currentItem = dtCompare.SelectedItem as CompareData;
            List<CompareData>? currentComp = dtCompare.ItemsSource as List<CompareData>;

            if (currentComp != null)
            {
                currentComp.Reverse();

                if (currentItem == null)
                {
                    currentItem = currentComp.FirstOrDefault(x => !x.IsEqual);
                }
                else
                {
                    currentItem = currentComp.Skip(currentComp.Count - currentItem.RowNumber + 1).FirstOrDefault(x => !x.IsEqual);
                }

                currentComp.Reverse();

                if (currentItem != null)
                {
                    dtCompare.SelectedItem = currentItem;
                    dtCompare.UpdateLayout();
                    dtCompare.ScrollIntoView(dtCompare.SelectedItem);
                    dtCompare.Focus();
                }
                else
                {
                    MessageBox.Show("No previous divergence found");
                }
            }
            else
            {
                MessageBox.Show("No comparison to search");
            }
        }

        private void NextDiff(object sender, RoutedEventArgs e)
        {
            CompareData? currentItem = dtCompare.SelectedItem as CompareData;
            List<CompareData>? currentComp = dtCompare.ItemsSource as List<CompareData>;

            if (currentComp != null)
            {
                if (currentItem == null)
                {
                    currentItem = currentComp.FirstOrDefault(x => !x.IsEqual);
                }
                else
                {
                    currentItem = currentComp.Skip(currentItem.RowNumber).FirstOrDefault(x => !x.IsEqual);
                }

                if (currentItem != null)
                {
                    dtCompare.SelectedItem = currentItem;
                    dtCompare.UpdateLayout();
                    dtCompare.ScrollIntoView(dtCompare.SelectedItem);
                    dtCompare.Focus();
                }
                else
                {
                    MessageBox.Show("No next divergence found");
                }
            }
            else
            {
                MessageBox.Show("No comparison to search");
            }
        }

        #region Key Configurations

        private void LoadGrid(List<KeysConfigData>? sorters = null)
        {
            KeysConfig = sorters ?? new List<KeysConfigData>();

            dtKeysConfig.ItemsSource = KeysConfig;
        }

        #endregion

        #region Form Saving/Loading

        private void Save(object sender, RoutedEventArgs e)
        {
            var dlg = new SaveFileDialog()
            {
                Filter = "Saved Setup |*.json"
            };

            try
            {
                if (dlg.ShowDialog() ?? false)
                {
                    string filename = dlg.FileName;

                    var data = new SetupData
                    {
                        KeysConfigurations = (List<KeysConfigData>)dtKeysConfig.ItemsSource,

                        Message1 = txtMessage1.Text,
                        Message2 = txtMessage2.Text,
                        MessageTitle1 = txtMessageTitle1.Text,
                        MessageTitle2 = txtMessageTitle2.Text,
                    };

                    File.WriteAllText(filename, JsonConvert.SerializeObject(data));

                    MessageBox.Show("Saved");
                }
            }
            catch
            {
                MessageBox.Show("Error");
            }
        }
        private void Load(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog()
            {
                Filter = "Saved Setup |*.json"
            };

            try
            {
                if (dlg.ShowDialog() ?? false)
                {
                    string filename = dlg.FileName;

                    var fileData = File.ReadAllText(filename);

                    var data = JsonConvert.DeserializeObject<SetupData>(fileData);

                    if (data != null)
                    {
                        KeysConfig = data.KeysConfigurations ?? new List<KeysConfigData>();

                        dtKeysConfig.ItemsSource = KeysConfig;
                        txtMessage1.Text = data.Message1;
                        txtMessage2.Text = data.Message2;
                        txtMessageTitle1.Text = data.MessageTitle1 ?? "Message 1";
                        txtMessageTitle2.Text = data.MessageTitle2 ?? "Message 2";

                        ShowLabel(lblLoadMessage, "Done");
                    }
                    else
                    {
                        MessageBox.Show("Invalid file");
                    }
                }
            }
            catch
            {
                MessageBox.Show("Invalid file");
            }
        }

        #endregion

        #region Misc

        private static async void ShowLabel(Label label, string value)
        {
            label.Content = value;

            for (var i = 200; i >= 0; i--)
            {
                label.Opacity = ((double)i) / 100;
                await Task.Delay(10);
            }

            label.Content = "";
            label.Opacity = 1;
        }

        #endregion
    }
}

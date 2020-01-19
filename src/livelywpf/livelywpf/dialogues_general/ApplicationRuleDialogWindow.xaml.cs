using System;
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
using System.Windows.Forms;
using System.IO;
using System.Collections.ObjectModel;
using System.ComponentModel;
using MahApps.Metro.Controls;

namespace livelywpf.Dialogues
{
    /// <summary>
    /// Interaction logic for ApplicationRuleDialogWindow.xaml
    /// </summary>
    public partial class ApplicationRuleDialogWindow : MetroWindow
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        private ObservableCollection<SaveData.ApplicationRules> appRulesCollection = new ObservableCollection<SaveData.ApplicationRules>(SaveData.appRules);

        public ApplicationRuleDialogWindow()
        {
            InitializeComponent();
            listView.ItemsSource = appRulesCollection;
        }

        private void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "application (*.exe) |*.exe";
            if (openFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                try
                {
                    string fileName = Path.GetFileNameWithoutExtension(openFileDialog.FileName);
  
                    // ignore if appname already exists
                    foreach (var item in appRulesCollection)
                    {
                        if (item.AppName.Equals(fileName, StringComparison.OrdinalIgnoreCase))
                        {
                            return;
                        }
                    }

                    appRulesCollection.Add(new SaveData.ApplicationRules { AppName = fileName, Rule = SaveData.AppRulesEnum.pause });
                    try
                    {
                        listView.SelectedIndex = listView.Items.Count - 1;
                    }
                    catch(ArgumentOutOfRangeException)
                    {
                        listView.SelectedIndex = -1;
                    }
                }
                catch (Exception ex)
                {
                    openFileDialog.Dispose();
                    Logger.Error("Failure to add apprule app:- "+ ex.ToString());
                }
                openFileDialog.Dispose();
            }
        }

        private void BtnRemove_Click(object sender, RoutedEventArgs e)
        {
            if(listView.SelectedIndex != -1)
            {
                appRulesCollection.RemoveAt(listView.SelectedIndex);
            }
        }

        private void ListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (listView.SelectedIndex != -1)
            {
                btnRemove.IsEnabled = true;
                comboBox.IsEnabled = true;
                comboBox.SelectedIndex = (int)appRulesCollection[listView.SelectedIndex].Rule;
            }
            else
            {
                btnRemove.IsEnabled = false;
                comboBox.IsEnabled = false;
                comboBox.SelectedIndex = -1;
            }
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (comboBox.SelectedIndex == -1 || listView.SelectedIndex == -1)
                return;
            if(comboBox.SelectedIndex == 0)
                appRulesCollection[listView.SelectedIndex].Rule = SaveData.AppRulesEnum.pause;
            else if(comboBox.SelectedIndex == 1)
                appRulesCollection[listView.SelectedIndex].Rule = SaveData.AppRulesEnum.ignore;
            else
                appRulesCollection[listView.SelectedIndex].Rule = SaveData.AppRulesEnum.kill;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            SaveData.appRules = appRulesCollection.ToList();
            SaveData.SaveApplicationRules();
        }
    }
}

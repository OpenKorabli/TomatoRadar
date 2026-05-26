using System.Collections.Generic;
using System.Windows;
using TomatoRadar.Models;

namespace TomatoRadar
{
    public partial class LanguagePriorityWindow : Window
    {
        private bool _saved;

        public LanguagePriorityWindow(string currentPriority)
        {
            InitializeComponent();
            PopulateList(currentPriority);
        }

        private void PopulateList(string priority)
        {
            ListBoxPriority.Items.Clear();
            List<Language> languages = LanguageExt.ParsePriority(priority);
            bool isAuto = priority == "AUTO";

            foreach (Language lang in languages)
            {
                if (lang == Models.Language.AUTO)
                    continue;
                ListBoxPriority.Items.Add(new ListItem
                {
                    Content = System.Windows.Application.Current.FindResource($"ComboBoxItemLanguageName{LanguageExt.GetNameByLanguage(lang)}"),
                    Value = LanguageExt.GetNameByLanguage(lang),
                });
            }

            BtnAuto.IsEnabled = !isAuto;
        }

        private string GetCurrentPriority()
        {
            List<Language> languages = new();
            foreach (ListItem item in ListBoxPriority.Items)
            {
                languages.Add(LanguageExt.GetLanguageByName(item.Value.ToString()!));
            }
            return LanguageExt.FormatPriority(languages);
        }

        public string? ResultPriority { get; private set; }
        public bool WasSaved => _saved;

        private void BtnUp_Click(object sender, RoutedEventArgs e)
        {
            int idx = ListBoxPriority.SelectedIndex;
            if (idx > 0)
            {
                object item = ListBoxPriority.Items[idx];
                ListBoxPriority.Items.RemoveAt(idx);
                ListBoxPriority.Items.Insert(idx - 1, item);
                ListBoxPriority.SelectedIndex = idx - 1;
            }
        }

        private void BtnDown_Click(object sender, RoutedEventArgs e)
        {
            int idx = ListBoxPriority.SelectedIndex;
            if (idx >= 0 && idx < ListBoxPriority.Items.Count - 1)
            {
                object item = ListBoxPriority.Items[idx];
                ListBoxPriority.Items.RemoveAt(idx);
                ListBoxPriority.Items.Insert(idx + 1, item);
                ListBoxPriority.SelectedIndex = idx + 1;
            }
        }

        private void BtnAuto_Click(object sender, RoutedEventArgs e)
        {
            PopulateList("AUTO");
        }

        private void BtnOK_Click(object sender, RoutedEventArgs e)
        {
            ResultPriority = GetCurrentPriority();
            _saved = true;
            Close();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}

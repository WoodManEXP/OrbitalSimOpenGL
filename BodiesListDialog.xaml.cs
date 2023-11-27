using System;
using System.Collections.Generic;
using System.Windows;

namespace OrbitalSimOpenGL
{
    /// <summary>
    /// Interaction logic for BodiesListDialog.xaml
    /// </summary>
    public partial class BodiesListDialog : Window
    {
        #region Properties
        private JPL_BodyList JPL_BodyList { get; set; }
        #endregion

        public BodiesListDialog(JPL_BodyList bodyList)
        {
            InitializeComponent();

            JPL_BodyList = bodyList;

            PopulateList();
        }

        private class ListEntry
        {
            public Boolean Selected { get; set; }
            public String? Text { get; set; }
        }
        private void PopulateList()
        {
            List<JPL_Body> bodies = JPL_BodyList.BodyList;
            List<ListEntry> listEntries = new List<ListEntry>();

            foreach (JPL_Body b in bodies)
            {
                listEntries.Add(new ListEntry() { Selected = b.Selected, Text = b.Name });
            }

            bodiesList.ItemsSource = listEntries;
        }

        private void Button_OK(object sender, RoutedEventArgs e)
        {
            // Save state of BodiesList

            // Transfer current selected state into BodiesList
            JPL_BodyList.SelectAll(false);

            List<ListEntry> listEntries = (List<ListEntry>)bodiesList.ItemsSource;
            int i = -1;
            foreach (ListEntry entry in listEntries)
            {
                JPL_BodyList.SetSelected(++i, entry.Selected);
            }

            JPL_BodyList.SaveBodyList(); // Write to SavedJPLBodiesFile
            DialogResult = true;
        }

        private void Button_Cancel(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        // Reload from init
        private void Button_Reload(object sender, RoutedEventArgs e)
        {
            JPL_BodyList.LoadList(true);
            PopulateList();
        }

        private void Button_Clear(object sender, RoutedEventArgs e)
        {
            JPL_BodyList.SelectAll(false);
            PopulateList();
        }

        private void Button_SelAll(object sender, RoutedEventArgs e)
        {
            JPL_BodyList.SelectAll(true);
            PopulateList();
        }
    }
}

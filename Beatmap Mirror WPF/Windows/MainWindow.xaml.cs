﻿using Beatmap_Mirror.Code;
using Beatmap_Mirror.Code.Api;
using Beatmap_Mirror.Code.Api.Requests;
using Beatmap_Mirror.Code.Structures;
using Beatmap_Mirror.Code.Tools;
using Beatmap_Mirror_WPF.Code.Tools;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Beatmap_Mirror_WPF.Windows
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private ListSortDirection LastSort = ListSortDirection.Ascending;
        private List<Beatmap> RawSearchResultList = new List<Beatmap>();

        public MainWindow()
        {
            InitializeComponent();

            string temp = RegistryHelper.GetKey("ParrarelDownloads");
            if (temp != null)
                Configuration.ParrarelDownloads = int.Parse(temp);
            else
                RegistryHelper.SetKey("ParrarelDownloads", "2");


            temp = RegistryHelper.GetKey("BeatmapLocation");
            if (temp != null)
            {
                Configuration.BeatmapDownloadLocation = temp;
                this.BeatmapLocation.Text = temp;
            }

            temp = RegistryHelper.GetKey("MP3Location");
            if (temp != null)
            {
                Configuration.Mp3DownloadLocation = temp;
                this.MP3Location.Text = temp;
            }
        }

        private void PerformBeatmapSearch()
        {
            List<string> Filters = new List<string>();

            if (!string.IsNullOrWhiteSpace(this.SearchTitle.Text))
                Filters.Add(string.Format("maps.title.like.{0}", this.SearchTitle.Text));

            if (!string.IsNullOrWhiteSpace(this.SearchCreator.Text))
                Filters.Add(string.Format("metadata.m-creator.like.{0}", this.SearchCreator.Text));

            if (!string.IsNullOrWhiteSpace(this.SearchSource.Text))
                Filters.Add(string.Format("metadata.m_source.like.{0}", this.SearchSource.Text));

            if (!string.IsNullOrWhiteSpace(this.SearchTags.Text))
                Filters.Add(string.Format("metadata.m_tags.like.{0}", this.SearchTags.Text));

            if (this.SearchSizeMin.Value > 0)
                Filters.Add(string.Format("maps.size.gteq.{0}", this.SearchSizeMin.Value));

            if (this.SearchSizeMax.Value > 0)
                Filters.Add(string.Format("maps.size.lteq.{0}", this.SearchSizeMax.Value));

            if (this.SearchVersion.Value > 0)
                Filters.Add(string.Format("metadata.version.eq.{0}", this.SearchVersion.Value));

            ApiRequestSearch search = ApiBase.Create<ApiRequestSearch>(Filters.ToArray());
            ApiSearch data = search.GetData<ApiSearch>();

            this.SearchResults.Items.Clear();
            this.RawSearchResultList.Clear();

            this.RawSearchResultList.AddRange(data.Beatmaps);

            if (data == null)
                return;

            foreach (Beatmap bm in data.Beatmaps)
            {
                this.SearchResults.Items.Add(bm);
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.PerformBeatmapSearch();
        }

        private void MenuItemDetails_Click(object sender, RoutedEventArgs e)
        {
            foreach (Beatmap bm in this.SearchResults.SelectedItems)
                new BeatmapDetails(bm.Ranked_ID).Show();
        }

        private void MenuItemDownloadBeatmap_Click(object sender, RoutedEventArgs e)
        {
            foreach (Beatmap bm in this.SearchResults.SelectedItems)
                DownloadQueueManager.AddToQueue(bm, DownloadQueueManager.DownloadType.Beatmap);

            this.SearchResults.SelectedItem = null;
        }

        private void MenuItemDownloadMP3_Click(object sender, RoutedEventArgs e)
        {
            foreach (Beatmap bm in this.SearchResults.SelectedItems)
                DownloadQueueManager.AddToQueue(bm, DownloadQueueManager.DownloadType.MP3);

            this.SearchResults.SelectedItem = null;
        }

        private void SearchResults_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            if (this.SearchResults.SelectedItems.Count > 1)
                this.MenuItemDetails.IsEnabled = false;
            else
                this.MenuItemDetails.IsEnabled = true;
        }

        private void BeatmapBrowse_Click(object sender, RoutedEventArgs e)
        {
            FolderBrowserDialog dialog = new FolderBrowserDialog();

            DialogResult result = dialog.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                Configuration.BeatmapDownloadLocation = dialog.SelectedPath;
                this.BeatmapLocation.Text = dialog.SelectedPath;
                RegistryHelper.SetKey("BeatmapLocation", dialog.SelectedPath);
            }
        }

        private void MP3Browse_Click(object sender, RoutedEventArgs e)
        {
            FolderBrowserDialog dialog = new FolderBrowserDialog();

            DialogResult result = dialog.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                Configuration.Mp3DownloadLocation = dialog.SelectedPath;
                this.MP3Location.Text = dialog.SelectedPath;
                RegistryHelper.SetKey("MP3Location", dialog.SelectedPath);
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (System.Windows.MessageBox.Show("You sure you want to quit?", "Are you sure?", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No)
                e.Cancel = true;
            else
                Process.GetCurrentProcess().Kill();
        }

        private void SearchResults_Click(object sender, RoutedEventArgs e)
        {
            GridViewColumnHeader c = e.OriginalSource as GridViewColumnHeader;

            if (c != null)
            {
                if (this.LastSort == ListSortDirection.Ascending)
                    this.LastSort = ListSortDirection.Descending;
                else
                    this.LastSort = ListSortDirection.Ascending;
              
                switch(c.Column.Header.ToString())
                {
                    case "ID":
                        this.RawSearchResultList.Sort(delegate(Beatmap a, Beatmap b)
                        {
                            int xdiff = this.LastSort == ListSortDirection.Ascending ? a.Ranked_ID.CompareTo(b.Ranked_ID) : b.Ranked_ID.CompareTo(a.Ranked_ID);
                            if (xdiff != 0) 
                                return xdiff;

                            return 0;
                        });
                        break;

                    case "Title":
                        this.RawSearchResultList.Sort(delegate(Beatmap a, Beatmap b)
                        {
                            int xdiff = this.LastSort == ListSortDirection.Ascending ? a.Title.CompareTo(b.Title) : b.Title.CompareTo(a.Title);
                            if (xdiff != 0) 
                                return xdiff;

                            return 0;
                        });
                        break;

                    case "Size":
                        this.RawSearchResultList.Sort(delegate(Beatmap a, Beatmap b)
                        {
                            int xdiff = this.LastSort == ListSortDirection.Ascending ? a.Size.CompareTo(b.Size) : b.Size.CompareTo(a.Size);
                            if (xdiff != 0)
                                return xdiff;

                            return 0;
                        });
                        break;
                }

                this.SearchResults.Items.Clear();
                foreach (Beatmap bm in this.RawSearchResultList)
                    this.SearchResults.Items.Add(bm);
            }
        }

        private void SearchTitle_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if(e.Key == Key.Enter)
                this.PerformBeatmapSearch();
        }

        private void SearchCreator_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                this.PerformBeatmapSearch();
        }

        private void SearchSource_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                this.PerformBeatmapSearch();
        }

        private void SearchTags_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                this.PerformBeatmapSearch();
        }

        private void SearchSizeMin_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                this.PerformBeatmapSearch();
        }

        private void SearchSizeMax_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                this.PerformBeatmapSearch();
        }

        private void SearchVersion_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                this.PerformBeatmapSearch();
        }
    }
}

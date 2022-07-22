using SearchRepo.Models;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Configuration;
using System;
using System.Threading;
using System.Linq;
using System.Collections.Generic;

namespace SearchRepo.ViewModels
{
    public class MainVM : BaseVM
    {
        private ObservableCollection<string> files;
        private ObservableCollection<string> folders;
        private ObservableCollection<SearchResult> searchResults = new ObservableCollection<SearchResult>();
        public ObservableCollection<SearchResult> SearchResults
        {
            get { return searchResults; }
            set { searchResults = value; OnPropertyChanged(nameof(SearchResults)); }
        }
        private CancellationTokenSource searchcancellationToken;
        public string FilesCountLabelText
        {
            get { return $"Папок: {folders.Count}, Файлов : {files.Count}"; }
        }
        private bool searchButtonEnabled;
        public bool SearchButtonEnabled
        {
            get { return searchButtonEnabled; }
            set
            {
                searchButtonEnabled = value;
                OnPropertyChanged(nameof(SearchButtonEnabled));
            }
        }
        private bool stopButtonEnabled;
        public bool StopButtonEnabled
        {
            get { return stopButtonEnabled; }
            set
            {
                stopButtonEnabled = value;
                OnPropertyChanged(nameof(StopButtonEnabled));
            }
        }
        private string searchText;
        public string SearchText
        {
            get { return searchText; }
            set
            {
                searchText = value;
                OnPropertyChanged(nameof(SearchText));
            }
        }
        private int currentProcessedFiles;
        public int CurrentProcessedFiles
        {
            get { return currentProcessedFiles; }
            set
            {
                currentProcessedFiles = value;
                OnPropertyChanged(nameof(CurrentProcessedFiles));
            }
        }
        public int FilesCount
        {
            get { return files.Count; }
        }
        private string searchPath;
        public MainVM()
        {
            CurrentProcessedFiles = 0;
            GetSearchFolder();
            SearchButtonEnabled = false;
            StopButtonEnabled=false;
            SearchResults = new ObservableCollection<SearchResult>();
            files = new ObservableCollection<string>();
            files.CollectionChanged += (s, e) => OnPropertyChanged(nameof(FilesCountLabelText)); OnPropertyChanged(nameof(FilesCount));
            folders = new ObservableCollection<string>();
            folders.CollectionChanged += (s, e) => OnPropertyChanged(nameof(FilesCountLabelText));
            SearchFiles();
        }

        /// <summary>
        /// Ищет файлы для дальнейшего поиска в них
        /// </summary>
        private void SearchFiles()
        {
            Task.Factory.StartNew(StartSearchFilesParallel,
                        CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
        }
        /// <summary>
        /// Ищет файлы для дальнейшего поиска в них
        /// </summary>
        private void StartSearchFilesParallel()
        {
            List<string> exts = new List<string>();
            if (!string.IsNullOrEmpty(ConfigurationManager.AppSettings["SearchFileExtension"]))
                exts = ConfigurationManager.AppSettings["SearchFileExtension"].Split(',').ToList();

            foreach (var dir in Directory.GetDirectories(searchPath, "*", SearchOption.AllDirectories))
            {
                folders.Add(dir);
            }
            Parallel.ForEach(this.folders, par =>
            {
                if (exts.Count>0)
                {
                    foreach (string file in Directory.GetFiles(par, "*.*", SearchOption.TopDirectoryOnly)
                   .Where(file => exts.Any(x => file.EndsWith(x, StringComparison.OrdinalIgnoreCase))))
                    {
                        lock (files)
                            files.Add(file);
                    }
                }
                else
                {
                    foreach (string file in Directory.GetFiles(par, "*.*", SearchOption.TopDirectoryOnly))
                    {
                        lock (files)
                            files.Add(file);
                    }
                }
            });
            SearchButtonEnabled = true;
        }
        /// <summary>
        /// Ищет строки в файлах 
        /// </summary>
        private void StartSearchRowsParallel()
        {
            Parallel.ForEach(this.files, par =>
            {
               if (searchcancellationToken.IsCancellationRequested) return;
               var lines = File.ReadAllLines(par);
                Parallel.For(0, lines.Count(), i => {

                    if (lines[i].Contains(SearchText))
                    {
                        var result = new SearchResult() { Path = par, Row = i, Directory = new FileInfo(par).Directory.FullName, Name = Path.GetFileName(par) };
                        if(result!=null)
                        {
                            App.Current.Dispatcher.Invoke(() =>
                            {
                                SearchResults.Add(result);
                            });
                        }
                          
                    }
                });
                CurrentProcessedFiles++;
            });

            SearchButtonEnabled = true;
            StopButtonEnabled = false;
        }
        /// <summary>
        /// Ищет строки в файлах 
        /// </summary>
        private void StartSearch()
        {
            if (string.IsNullOrEmpty(SearchText)) return;
                
            SearchResults.Clear();
            searchcancellationToken = new CancellationTokenSource();
            Task.Factory.StartNew(StartSearchRowsParallel,
                        searchcancellationToken.Token, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
            StopButtonEnabled = true;
            SearchButtonEnabled= false;
        }
        /// <summary>
        /// Остановка поиска
        /// </summary>
        private void StopSearch()
        {
            searchcancellationToken.Cancel();
            StopButtonEnabled= false;
            SearchButtonEnabled = true;
        }

        /// <summary>
        /// Начальный выбор папки для поиска
        /// </summary>
        private void GetSearchFolder()
        {
            if(!string.IsNullOrEmpty(ConfigurationManager.AppSettings["SearchDirrectory"]) 
                 && Directory.Exists(ConfigurationManager.AppSettings["SearchDirrectory"]))
            {
                searchPath = ConfigurationManager.AppSettings["SearchDirrectory"];
                return;
            }

            using (var fbd = new FolderBrowserDialog())
            {
                DialogResult result = fbd.ShowDialog();

                if (result == DialogResult.OK && !string.IsNullOrWhiteSpace(fbd.SelectedPath))
                {
                   searchPath = fbd.SelectedPath;
                }
                else { Environment.Exit(0); }
            }
        }

        private RelayCommand _startSearchCommand;
        public RelayCommand StartSearchCommand => _startSearchCommand ?? (_startSearchCommand = new RelayCommand(StartSearch));
        private RelayCommand _stopSearchCommand;
        public RelayCommand StopSearchCommand => _stopSearchCommand ?? (_stopSearchCommand = new RelayCommand(StopSearch));
    }
}

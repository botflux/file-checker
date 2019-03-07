using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Forms;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using MessageBox = System.Windows.Forms.MessageBox;
using System.IO;
using System.Text.RegularExpressions;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading;
using System.Windows.Threading;
using Application = System.Windows.Application;

namespace FileChecker
{
    class ApplicationViewModel : INotifyPropertyChanged
    {
        #region Fields
        /// <summary>
        /// Throw when a reactive property has changed.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// The folder selected by the user
        /// </summary>
        private string selectedPath = "";

        /// <summary>
        /// Processed file count
        /// </summary>
        private int processedFile = 0;

        private FileInformation currentFile;

        /// <summary>
        /// Represents the missing files inside the selected folder.
        /// </summary>
        private ObservableCollection<FileInformation> missingLines = new ObservableCollection<FileInformation>();

        /// <summary>
        /// Represents the number of fle contains inside the selected folder.
        /// </summary>
        private int fileCount = 0;
        #endregion

        #region Computed Properties
        /// <summary>
        /// Percent of analyse files
        /// </summary>
        public float AnalyseProgress
        {
            get
            {
                if (ProcessedFile != 0 && FileCount != 0)
                {
                    return ((float)ProcessedFile / (float)FileCount) * 100;
                }

                return 0;
            }
        }

        /// <summary>
        /// The number of files contains inside the selected folder
        /// </summary>
        public int FileCount
        {
            get => fileCount;
            set
            {
                if (fileCount != value)
                {
                    fileCount = value;
                    NotifyProperty();
                    NotifyProperty("AnalyseProgress");
                }
            }
        }

        /// <summary>
        /// The folder path selected by the user
        /// </summary>
        public string SelectedPath
        {
            get => this.selectedPath;

            set
            { 
                this.selectedPath = value;
                NotifyProperty();
            }
        }

        public FileInformation CurrentFile
        {
            get => currentFile;
            set
            {
                if (currentFile != value)
                {
                    currentFile = value;
                    NotifyProperty();
                }
            }
        }

        /// <summary>
        /// Count of processed files
        /// </summary>
        public int ProcessedFile
        {
            get => processedFile;
            set
            {
                if (processedFile != value)
                {
                    processedFile = value;
                    NotifyProperty();
                    NotifyProperty("AnalyseProgress");
                }
            }
        }

        /// <summary>
        /// Represents all the missings files
        /// </summary>
        public ObservableCollection<FileInformation> MissingLines
        {
            get => this.missingLines;
            set
            {
                if (this.missingLines != value)
                {
                    this.missingLines = value;
                    NotifyProperty();
                }
            }
        }
        #endregion

        #region Commands
        /// <summary>
        /// Explore a directory and look for missing files
        /// </summary>
        public ICommand ExploreDirectory
        {
            get
            {
                if (this.exploreDirectory == null)
                    this.exploreDirectory = new RelayCommand<object>((o) =>
                    {
                        FolderBrowserDialog dialog = new FolderBrowserDialog();

                        if (dialog.ShowDialog() == DialogResult.OK)
                        {
                            this.SelectedPath = dialog.SelectedPath;
                        }
                    });

                return this.exploreDirectory;
            }
        }

        /// <summary>
        /// Explore a given directory
        /// </summary>
        private ICommand exploreDirectory;
        #endregion

        #region Constructors
        public ApplicationViewModel ()
        {
            this.PropertyChanged += OnPropertyChanged;
        }
        #endregion

        #region Methods
        private void Explore ()
        {
            if (!Directory.Exists(this.selectedPath)) MessageBox.Show("Le dossier spécifié n'existe pas !");

            string[] files = Directory.GetFiles(this.selectedPath, "*.tif");


            FileCount = files.Length;
            Regex rx = new Regex(@"(?<year>\d{4})(?<type>\w)(?<id>\d{3})", RegexOptions.Compiled | RegexOptions.IgnoreCase);

            Thread th = new Thread(new ThreadStart(() => {

                FileInformation previousFile = FileInformation.GetFileInformation(
                    files.First()
                );

                foreach (string file in files)
                {
                    FileInformation currentFile = FileInformation.GetFileInformation(file);

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        this.CurrentFile = currentFile;
                    });

                    if (currentFile.Year == previousFile.Year)
                    {
                        int deltaId = currentFile.Id - previousFile.Id;

                        if (deltaId > 1)
                        {
                            Console.WriteLine(string.Format("{0} - {1} - {2}", previousFile.ToString(), currentFile.ToString(), deltaId));

                            for (int i = 1; i < currentFile.Id - previousFile.Id; i++)
                            {
                                Application.Current.Dispatcher.Invoke(() =>
                                {
                                    MissingLines.Add(new FileInformation(
                                        currentFile.Year, currentFile.Type, i + previousFile.Id
                                    ));
                                });

                            }
                        }
                    }

                    Application.Current.Dispatcher.Invoke(() => {
                        ProcessedFile++;
                    });
                    previousFile = currentFile;
                }

            }));

            th.Start();

        }
        #endregion

        #region Event related methods
        private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "SelectedPath")
            {
                this.MissingLines.Clear();
                this.FileCount = 0;
                this.ProcessedFile = 0;


                Explore();
            }
            else if (e.PropertyName == "AnalyseProgress")
            {
                if (AnalyseProgress >= 100)
                {
                    MessageBox.Show("Le dossier a été analysé !");
                }
            }
        }

        private void NotifyProperty([CallerMemberName] string str = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(str));
        }
        #endregion
    }
}

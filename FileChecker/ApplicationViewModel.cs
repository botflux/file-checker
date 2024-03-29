﻿using Microsoft.Win32;
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
using System.Drawing.Printing;
using System.Drawing;

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

        /// <summary>
        /// Current file that is check
        /// </summary>
        private FileInformation currentFile;

        /// <summary>
        /// View
        /// </summary>
        private MainWindow currentWindow;

        /// <summary>
        /// Represents the missing files inside the selected folder.
        /// </summary>
        private ObservableCollection<FileInformation> missingLines = new ObservableCollection<FileInformation>();

        /// <summary>
        /// Represents the number of fle contains inside the selected folder.
        /// </summary>
        private int fileCount = 0;

        private int parsedCount = 0;
        #endregion

        #region Computed Properties
        public int ParsedCount
        {
            get => parsedCount;
            set
            {
                if (value != parsedCount)
                {
                    NotifyProperty();
                    NotifyProperty("AnalyseProgress");
                    parsedCount = value;
                }
            }
        }

        /// <summary>
        /// Percent of analyse files
        /// </summary>
        public float AnalyseProgress
        {
            get
            {
                return
                    ((ParsedCount != 0 && FileCount != 0) ? (((float)ParsedCount / (float)FileCount) * 50) : 0) +
                    ((ProcessedFile != 0 && FileCount != 0) ? (((float)ProcessedFile / (float)FileCount) * 50) : 0);   
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
        /// View
        /// </summary>
        public MainWindow CurrentWindow
        {
            get => currentWindow;
            set
            {
                if (currentWindow != value)
                {
                    currentWindow = value;
                    NotifyProperty();
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
        
        /// <summary>
        /// Current file that is check
        /// </summary>
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

        #region Thread locks
        private object _locker = new object();
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

        /// <summary>
        /// Close the application
        /// </summary>
        public ICommand CloseApplication
        {
            get
            {
                if (closeApplication == null)
                {
                    closeApplication = new RelayCommand<object>((o) =>
                    {
                        Application.Current.Shutdown();
                    });
                }

                return closeApplication;
            }
        }

        /// <summary>
        /// Close the application
        /// </summary>
        private ICommand closeApplication;

        /// <summary>
        /// Minimize the application
        /// </summary>
        public ICommand MinimizeApplication
        {
            get
            {
                if (minimizeApplication == null)
                {
                    minimizeApplication = new RelayCommand<object>((o) =>
                    {
                        Application.Current.MainWindow.WindowState = WindowState.Minimized;
                    });
                }

                return minimizeApplication;
            }
        }

        /// <summary>
        /// Minimize the application
        /// </summary>
        private ICommand minimizeApplication;

        /// <summary>
        /// Maximize the application
        /// </summary>
        public ICommand MaximizeApplication
        {
            get
            {
                if (maximizeApplication == null)
                {
                    maximizeApplication = new RelayCommand<object>((o) =>
                    {
                        Application.Current.MainWindow.WindowState = WindowState.Maximized;
                    });
                }

                return maximizeApplication;
            }
        }

        /// <summary>
        /// Maximize the application
        /// </summary>
        private ICommand maximizeApplication;

        /// <summary>
        /// Print found missing lines
        /// </summary>
        public ICommand PrintMissingLines
        {
            get
            {
                if (printMissingLines == null)
                {
                    printMissingLines = new RelayCommand<object>((o) => 
                    {
                        using (PrintDocument pd = new PrintDocument())
                        {
                            using (PrintDialog printDialog = new PrintDialog())
                            {
                                if (printDialog.ShowDialog() == DialogResult.OK)
                                {
                                    Queue<FileInformation> missingLinesQueue = new Queue<FileInformation>(MissingLines);

                                    pd.PrinterSettings = printDialog.PrinterSettings;

                                    pd.PrintPage += (object sender, PrintPageEventArgs ev) =>
                                    {
                                        float linesPerPage = 0;
                                        float yPos = 0;
                                        int count = 0;
                                        float leftMargin = ev.MarginBounds.Left;
                                        float topMargin = ev.MarginBounds.Top;
                                        Font f = new Font("Arial", 15);

                                        linesPerPage = ev.MarginBounds.Height / f.GetHeight(ev.Graphics);

                                        while (count < linesPerPage && missingLinesQueue.Count != 0)
                                        {
                                            FileInformation current = missingLinesQueue.Dequeue();
                                            yPos = topMargin + (count * f.GetHeight(ev.Graphics));
                                            ev.Graphics.DrawString(
                                                current.FullName, f, 
                                                Brushes.Black, ev.MarginBounds.Left, 
                                                yPos, new StringFormat()
                                            );

                                            count++;
                                        }
                                        
                                        if (missingLinesQueue.Count == 0)
                                        {
                                            ev.HasMorePages = false;
                                        } else
                                        {
                                            ev.HasMorePages = true;
                                        }
                                    };
                                    pd.Print();
                                }
                            }
                        }
                    });
                }

                return printMissingLines;
            }
        }

        /// <summary>
        /// Print found missing lines
        /// </summary>
        private ICommand printMissingLines;
        #endregion

        #region Constructors
        /// <summary>
        /// Initialize a new instance of ApplicationViewModel
        /// </summary>
        public ApplicationViewModel ()
        {
            this.PropertyChanged += OnPropertyChanged;
        }
        #endregion

        #region Methods
        /// <summary>
        /// Explore the selected path folder
        /// </summary>
        private void Explore ()
        {
            if (!Directory.Exists(this.selectedPath))
            {
                this.CurrentWindow.snackbar.MessageQueue.Enqueue(
                    "Le dossier spécifié n'existe pas !",
                    "OK",
                    () => Trace.WriteLine("Actioned")
                );
            }

            string[] files = Directory.GetFiles(this.selectedPath, "*.tif", SearchOption.AllDirectories);

            Thread th = new Thread(new ThreadStart(() => {
                
                Regex rx = new Regex(@"(?<year>\d{4})(?<type>\w)(?<id>\d{3})", RegexOptions.Compiled | RegexOptions.IgnoreCase);

                // exclude filename that doesn't match the regex
                files = files.Where(s => rx.Match(s).Success).ToArray();

                // Set the file count
                Application.Current.Dispatcher.Invoke(() =>
                {
                    FileCount = files.Length;
                });

                // Transform each filename into a file information object
                List<FileInformation> filesInformations = files.Select(file => 
                {
                    Application.Current.Dispatcher.Invoke(() => 
                    {
                        Console.WriteLine("ParsedCount" + ParsedCount);
                        ParsedCount++;
                    });
                    return FileInformation.GetFileInformation(file);
                }).ToList();

                // sort all file information objects
                filesInformations.Sort(delegate (FileInformation x, FileInformation y) {
                    if (x == null && y == null) return 0;
                    else if (x == null) return 1;
                    else if (y == null) return -1;

                    if (x.Year.CompareTo(y.Year) == 0) return x.Id.CompareTo(y.Id);
                    else return x.Year.CompareTo(y.Year);
                });

                FileInformation previousFile = filesInformations.First();

                foreach (FileInformation currentFile in filesInformations)
                {
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
                    CurrentFile = null;

                    this.CurrentWindow.snackbar.MessageQueue.Enqueue(
                        this.FileCount + " fichiers ont été analysés",
                        "OK",
                        param => Trace.WriteLine("Actioned: " + param),
                        this.FileCount + " fichiers ont été analysés"
                    );
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

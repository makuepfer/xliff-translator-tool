﻿using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Xml;
using XliffTranslatorTool.Parser;

namespace XliffTranslatorTool
{
    public partial class MainWindow : Window
    {
        private enum State
        {
            Loaded,
            FileOpened
        }

        private string OpenedFileName { get; set; }
        private XliffParser XliffParser { get; set; } = new XliffParser();
        private State _currentState;
        private State CurrentState
        {
            get => _currentState;
            set
            {
                _currentState = value;
                OnStateChanged();
            }
        }

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            SetState(State.Loaded);
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            if (MainDataGrid.HasItems)
            {
                MessageBoxResult messageBoxResult = MessageBox.Show("Save as new file ?", "Question", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
                switch (messageBoxResult)
                {
                    case MessageBoxResult.None:
                    case MessageBoxResult.Cancel:
                        {
                            e.Cancel = true;
                            break;
                        }
                    case MessageBoxResult.Yes:
                        {
                            SaveAs();
                            break;
                        }
                    case MessageBoxResult.No:
                        {
                            e.Cancel = false;
                            break;
                        }
                }
            }
        }

        private void OnStateChanged()
        {
            switch (CurrentState)
            {
                case State.Loaded:
                    {
                        ImportFileMenuOption.IsEnabled = false;
                        SaveAsMenuOption.IsEnabled = false;
                        MainDataGrid.Visibility = Visibility.Hidden;
                        break;
                    }
                case State.FileOpened:
                    {
                        ImportFileMenuOption.IsEnabled = true;
                        SaveAsMenuOption.IsEnabled = true;
                        MainDataGrid.Visibility = Visibility.Visible;
                        break;
                    }
                default:
                    throw new NotImplementedException($"WindowState '{CurrentState.ToString()}' not implemented");
            }
        }

        private void SetState(State state)
        {
            CurrentState = state;
        }

        private void OpenFileMenuOption_Click(object sender, RoutedEventArgs e)
        {
            if (CurrentState == State.FileOpened)
            {
                MessageBoxResult messageBoxResult = MessageBox.Show("Opening a new file will overwrite your current data and your changes will be lost.\nContinue ?", "Warning", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (messageBoxResult == MessageBoxResult.No)
                {
                    return;
                }
            }

            OpenFile();
        }

        private void OpenFile()
        {
            OpenFileDialog openFileDialog = CreateOpenFileDialog();
            bool? result = openFileDialog.ShowDialog();

            string filePath = openFileDialog.FileName;
            OpenedFileName = openFileDialog.SafeFileName;

            if (result == true)
            {
                ObservableCollection<TranslationUnit> translationUnits = XliffParser.GetTranslationUnitsFromFile(filePath);
                if (AreTranslationUnitsValid(translationUnits))
                {
                    MainDataGrid.ItemsSource = translationUnits;
                    SetState(State.FileOpened);
                }
            }
        }

        private void ImportFileMenuOption_Click(object sender, RoutedEventArgs e)
        {
            ImportFile();
        }

        private void ImportFile()
        {
            OpenFileDialog openFileDialog = CreateOpenFileDialog();
            openFileDialog.Title = "Import";
            bool? result = openFileDialog.ShowDialog();

            string filePath = openFileDialog.FileName;

            if (result == true)
            {
                ObservableCollection<TranslationUnit> newTranslationUnits = XliffParser.GetTranslationUnitsFromFile(filePath);
                if (AreTranslationUnitsValid(newTranslationUnits))
                {
                    ObservableCollection<TranslationUnit> list = (ObservableCollection<TranslationUnit>)MainDataGrid.ItemsSource;
                    foreach (TranslationUnit translationUnit in newTranslationUnits)
                    {
                        if (!list.Contains(translationUnit))
                        {
                            list.Add(translationUnit);
                        }
                    }
                }
            }
        }

        private bool AreTranslationUnitsValid(IList<TranslationUnit> translationUnits)
        {
            if (translationUnits == null)
            {
                MessageBox.Show($"XLIFF version was not recognized. Supported versions are: {String.Join(", ", Constants.XLIFF_VERSION_V12, Constants.XLIFF_VERSION_V20)}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            else if (translationUnits.Count == 0)
            {
                MessageBox.Show("0 translations found", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                return true;
            }

            return false;
        }

        private void SaveAsMenuOption_Click(object sender, RoutedEventArgs e)
        {
            SaveAs();
        }

        private void SaveAs()
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                FileName = OpenedFileName,
                DefaultExt = Constants.FILE_DIALOG_DEFAULT_EXT,
                Filter = Constants.FILE_DIALOG_FILTER,
                CheckPathExists = true,
                OverwritePrompt = true,
                AddExtension = true
            };

            bool? result = saveFileDialog.ShowDialog();

            if (result == true)
            {
                XmlDocument xmlDocument = null;
                MessageBoxResult messageBoxResult = MessageBox.Show("YES - XLIFF version 1.2 (default)\nNO - XLIFF version 2.0", "XLIFF version", MessageBoxButton.YesNo, MessageBoxImage.Question);
                switch (messageBoxResult)
                {
                    case MessageBoxResult.Yes:
                        {
                            xmlDocument = XliffParser.CreateXliffDocument(XliffParser.XliffVersion.V12, MainDataGrid.ItemsSource);
                            break;
                        }
                    case MessageBoxResult.No:
                        {
                            xmlDocument = XliffParser.CreateXliffDocument(XliffParser.XliffVersion.V20, MainDataGrid.ItemsSource);
                            break;
                        }
                    case MessageBoxResult.None:
                        return;
                    default:
                        throw new NotImplementedException($"Not implemented MessageBoxResult '{messageBoxResult.ToString()}'");
                }

                xmlDocument.Save(saveFileDialog.FileName);
            }
        }

        private static OpenFileDialog CreateOpenFileDialog()
        {
            return new OpenFileDialog
            {
                DefaultExt = Constants.FILE_DIALOG_DEFAULT_EXT,
                Filter = Constants.FILE_DIALOG_FILTER,
                Multiselect = false
            };
        }
    }
}

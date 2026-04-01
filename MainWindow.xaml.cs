using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Threading;
using DragEventArgs = System.Windows.DragEventArgs;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
using Forms = System.Windows.Forms;

namespace MediaDateRenamer;

public partial class MainWindow : Window
{
    private readonly ObservableCollection<MediaItem> _items = new();
    private readonly Stopwatch _workStopwatch = new();
    private readonly Dispatcher _dispatcher = Dispatcher.CurrentDispatcher;

    public MainWindow()
    {
        InitializeComponent();
        FilesGrid.ItemsSource = _items;
    }

    private void AddFilesButton_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog
        {
            Multiselect = true,
            Filter = "Supported media|*.jpg;*.jpeg;*.png;*.bmp;*.tif;*.tiff;*.webp;*.heic;*.mp4;*.mov;*.m4v;*.avi;*.mts;*.m2ts;*.3gp;*.wmv;*.mkv|All files|*.*"
        };

        if (dlg.ShowDialog(this) == true)
            AddPaths(dlg.FileNames);
    }

    private void AddFolderButton_Click(object sender, RoutedEventArgs e)
    {
        using var dlg = new Forms.FolderBrowserDialog
        {
            Description = "Select a folder to scan recursively"
        };

        if (dlg.ShowDialog() == Forms.DialogResult.OK &&
            !string.IsNullOrWhiteSpace(dlg.SelectedPath))
        {
            AddPaths(new[] { dlg.SelectedPath });
        }
    }

    private void UpdateProgress(int completed, int total, Stopwatch stopwatch)
    {
        if (total <= 0)
        {
            WorkProgressBar.Value = 0;
            StatusText.Text = "Ready";
            return;
        }

        double percent = completed * 100.0 / total;
        WorkProgressBar.Value = percent;

        TimeSpan elapsed = stopwatch.Elapsed;
        TimeSpan remaining = TimeSpan.Zero;

        if (completed > 0)
        {
            double secondsPerFile = elapsed.TotalSeconds / completed;
            double secondsRemaining = secondsPerFile * (total - completed);
            remaining = TimeSpan.FromSeconds(secondsRemaining);
        }

        StatusText.Text =
            $"{completed} / {total} files | " +
            $"{percent:0}% complete | " +
            $"Elapsed: {elapsed:hh\\:mm\\:ss} | " +
            $"Remaining: {remaining:hh\\:mm\\:ss}";
    }

    private void RemoveSelectedButton_Click(object sender, RoutedEventArgs e)
    {
        var toRemove = _items.Where(x => x.Selected).ToList();

        foreach (var item in toRemove)
            _items.Remove(item);

        StatusText.Text = $"Removed {toRemove.Count} item(s).";
    }

    private void ClearButton_Click(object sender, RoutedEventArgs e)
    {
        _items.Clear();
        WorkProgressBar.Value = 0;
        StatusText.Text = "Cleared list.";
    }

    private void PreviewButton_Click(object sender, RoutedEventArgs e)
    {
        PreviewAll();
    }

    private void RenameButton_Click(object sender, RoutedEventArgs e)
    {
        PreviewAll();

        int renamed = 0;
        int skipped = 0;
        int total = _items.Count;
        int completed = 0;

        _workStopwatch.Restart();
        UpdateProgress(0, total, _workStopwatch);

        foreach (var item in _items)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(item.NewFileName) || !File.Exists(item.OriginalPath))
                {
                    item.Status = "Skipped";
                    skipped++;
                }
                else
                {
                    var dir = Path.GetDirectoryName(item.OriginalPath);
                    if (string.IsNullOrWhiteSpace(dir))
                    {
                        item.Status = "Skipped: Invalid path";
                        skipped++;
                    }
                    else
                    {
                        var dest = Path.Combine(dir, item.NewFileName);

                        if (string.Equals(dest, item.OriginalPath, StringComparison.OrdinalIgnoreCase))
                        {
                            item.Status = "Already correct";
                            skipped++;
                        }
                        else
                        {
                            File.Move(item.OriginalPath, dest);
                            item.OriginalPath = dest;
                            item.Status = "Renamed";
                            renamed++;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                item.Status = "Error: " + ex.Message;
                skipped++;
            }

            completed++;
            UpdateProgress(completed, total, _workStopwatch);
            _dispatcher.Invoke(() => { }, DispatcherPriority.Background);
        }

        _workStopwatch.Stop();

        double percent = total > 0 ? completed * 100.0 / total : 0;
        WorkProgressBar.Value = percent;

        StatusText.Text =
            $"Rename complete. {completed} / {total} files | " +
            $"{percent:0}% complete | " +
            $"Renamed: {renamed} | Skipped/Errors: {skipped} | " +
            $"Elapsed: {_workStopwatch.Elapsed:hh\\:mm\\:ss}";
    }

    private void AddPaths(IEnumerable<string> paths)
    {
        var existing = new HashSet<string>(_items.Select(x => x.OriginalPath), StringComparer.OrdinalIgnoreCase);
        int added = 0;

        foreach (var file in FolderScanner.ExpandPaths(paths))
        {
            if (!existing.Add(file))
                continue;

            _items.Add(new MediaItem
            {
                OriginalPath = file,
                DetectedDateText = string.Empty,
                SourceUsed = string.Empty,
                NewFileName = string.Empty,
                Status = "Queued"
            });

            added++;
        }

        StatusText.Text = $"Added {added} item(s). Total: {_items.Count}.";
    }

    private void PreviewAll()
    {
        var reserved = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        int total = _items.Count;
        int completed = 0;

        _workStopwatch.Restart();
        UpdateProgress(0, total, _workStopwatch);

        foreach (var item in _items)
        {
            try
            {
                if (!File.Exists(item.OriginalPath))
                {
                    item.DetectedDateText = string.Empty;
                    item.SourceUsed = string.Empty;
                    item.NewFileName = string.Empty;
                    item.Status = "Missing file";
                }
                else
                {
                    var result = MetadataService.GetBestTimestamp(item.OriginalPath);

                    if (!result.Timestamp.HasValue)
                    {
                        item.DetectedDateText = string.Empty;
                        item.SourceUsed = result.Source;
                        item.NewFileName = string.Empty;
                        item.Status = "No usable date";
                    }
                    else
                    {
                        item.DetectedDateText = result.Timestamp.Value.ToString("yyyy-MM-dd HH:mm:ss");
                        item.SourceUsed = result.Source;

                        var uniqueName = RenameService.BuildUniqueFileName(
                            item.OriginalPath,
                            result.Timestamp.Value,
                            reserved);

                        item.NewFileName = uniqueName;

                        var fullReserved = Path.Combine(
                            Path.GetDirectoryName(item.OriginalPath) ?? string.Empty,
                            uniqueName);

                        reserved.Add(fullReserved);
                        item.Status = "Preview ready";
                    }
                }
            }
            catch (Exception ex)
            {
                item.DetectedDateText = string.Empty;
                item.SourceUsed = string.Empty;
                item.NewFileName = string.Empty;
                item.Status = "Error: " + ex.Message;
            }

            completed++;
            UpdateProgress(completed, total, _workStopwatch);
            _dispatcher.Invoke(() => { }, DispatcherPriority.Background);
        }

        _workStopwatch.Stop();
        UpdateProgress(completed, total, _workStopwatch);
    }

    private void Window_DragEnter(object sender, DragEventArgs e)
    {
        e.Effects = e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop)
            ? System.Windows.DragDropEffects.Copy
            : System.Windows.DragDropEffects.None;

        e.Handled = true;
    }

    private void Window_DragOver(object sender, DragEventArgs e)
    {
        e.Effects = e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop)
            ? System.Windows.DragDropEffects.Copy
            : System.Windows.DragDropEffects.None;

        e.Handled = true;
    }

    private void Window_Drop(object sender, DragEventArgs e)
    {
        if (!e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop))
            return;

        if (e.Data.GetData(System.Windows.DataFormats.FileDrop) is string[] dropped && dropped.Length > 0)
            AddPaths(dropped);
    }
}
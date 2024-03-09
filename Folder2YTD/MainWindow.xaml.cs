using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using AutoUpdaterDotNET;
using MaterialDesignThemes.Wpf;
using Ookii.Dialogs.Wpf;
using MessageBox = System.Windows.MessageBox;

namespace Folder2YTD
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private List<string> _foldersList = [];
        private readonly PaletteHelper _paletteHelper = new();
        private List<string> _parentFolders = [];

        private static readonly List<string> validExtensions = new List<string>
            { ".png", ".jpg", ".bmp", ".tiff", ".tif", ".jpeg", ".dds" };

        public MainWindow()
        {
            InitializeComponent();
            InitProgram();
        }

        public void InitProgram()
        {
            var version = Assembly.GetExecutingAssembly().GetName().Version;
            AppVersionLabel.Content = $"v{version}";
            FormatOutput.ItemsSource = new List<string> { "GTA V (.YTD)", "DDS Files", "YTD per image" };
            FormatOutput.SelectedIndex = 0;
            ShowFilesAfter.IsChecked = true;
            ShowFinishMS.IsChecked = true;
            AutoUpdater.Start("https://raw.githubusercontent.com/Hancapo/Folder2YTD/master/Folder2YTD/updateinfo.xml");
        }

        private async Task YtdPerImage(IReadOnlyList<string> allFolders)
        {
            await Task.Run(() =>
            {
                _parentFolders = allFolders.Select(x => Directory.GetParent(x)!.ToString()).Distinct().ToList();

                Parallel.For(0, allFolders.Count, i =>
                {
                    bool thereIsAnyConvertedDdsFile = false;
                    List<string> ConvertedDDS = [];
                    List<string> AlreadyDDSs = [];
                    List<string> AllDDSmerged = [];
                    string? folder = allFolders[i];
                    var imgFiles = Directory.EnumerateFiles(folder, "*.*", SearchOption.TopDirectoryOnly).Where(x =>
                        x.EndsWith(".png", StringComparison.InvariantCultureIgnoreCase) ||
                        x.EndsWith(".dds", StringComparison.InvariantCultureIgnoreCase) ||
                        x.EndsWith(".jpg", StringComparison.InvariantCultureIgnoreCase) ||
                        x.EndsWith(".tga", StringComparison.InvariantCultureIgnoreCase) ||
                        x.EndsWith(".bmp", StringComparison.InvariantCultureIgnoreCase) ||
                        x.EndsWith(".tiff", StringComparison.InvariantCultureIgnoreCase) ||
                        x.EndsWith(".tif", StringComparison.InvariantCultureIgnoreCase) ||
                        x.EndsWith(".jpeg", StringComparison.InvariantCultureIgnoreCase)).ToList();

                    int imgCount = imgFiles.Count;
                    Parallel.ForEach(imgFiles, imgfile =>
                    {
                        if (!imgFiles.Any()) return;
                        string imgFileName = Path.GetFileName(imgfile);
                        string imgFileExtension = Path.GetExtension(imgfile).ToLowerInvariant();

                        if (imgFileExtension is ".png" or ".tga" or ".jpg" or ".bmp" or ".tiff" or ".tif" or ".jpeg")
                        {
                            string newFolder = $"{Path.GetDirectoryName(imgfile)}/converted_dds/";
                            if (!Directory.Exists(newFolder))
                            {
                                Directory.CreateDirectory(newFolder);
                            }
                            var movedImageName = $"{newFolder}{imgFileName}";

                            File.Copy(imgfile, movedImageName, true);
                            ImageHelper.ConvertImageToDds(movedImageName);
                            File.Delete(movedImageName);
                        }

                        if (imgFileExtension != ".dds") return;
                        AlreadyDDSs.Add(imgfile);
                    });


                    List<string> checkConvertedDds = new();

                    if (Directory.Exists(folder + "/converted_dds/"))
                    {
                        checkConvertedDds = Directory
                            .GetFiles(folder + "/converted_dds/", "*.dds", SearchOption.TopDirectoryOnly).ToList();
                    }

                    thereIsAnyConvertedDdsFile = checkConvertedDds.Count != 0;

                    ConvertedDDS = !thereIsAnyConvertedDdsFile
                        ? []
                        : Directory.GetFiles(folder + "/converted_dds/", "*.dds", SearchOption.TopDirectoryOnly)
                            .ToList();


                    AllDDSmerged.AddRange(ConvertedDDS);
                    AllDDSmerged.AddRange(AlreadyDDSs);
                    
                    YtdHelper.CreateYtdFilesFromSingleImage(AllDDSmerged, folder);


                    if (thereIsAnyConvertedDdsFile)
                    {
                        Directory.Delete(folder + "/converted_dds", true);
                    }
                });

                ShowFinishMS.Dispatcher.Invoke(() =>
                {
                    if (ShowFinishMS.IsChecked == true)
                    {
                        MessageBox.Show($"Done, {allFolders.Count} folder(s) processed.", "Information",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                });

                bool isFilesAfterChecked = false;

                ShowFilesAfter.Dispatcher.Invoke(() => isFilesAfterChecked = (bool)ShowFilesAfter.IsChecked!);

                if (!isFilesAfterChecked) return;
                foreach (var parentFolder in _parentFolders)
                {
                    Process.Start("explorer.exe", parentFolder);
                }
            });
        }

        private async Task YtdFromFolders(IReadOnlyList<string> allFolders)
        {
            await Task.Run(() =>
            {
                _parentFolders = allFolders.Select(x => Directory.GetParent(x).ToString()).Distinct().ToList();

                Parallel.For(0, allFolders.Count, i =>
                {
                    bool thereIsAnyConvertedDdsFile = false;
                    List<string> convertedDds;
                    List<string> alreadyDdSs = [];
                    List<string> allDdsMerged = [];
                    string? folder = allFolders[i];
                    var imgFiles = Directory.EnumerateFiles(folder, "*.*", SearchOption.TopDirectoryOnly)
                        .Where(x => validExtensions.Contains(Path.GetExtension(x).ToLowerInvariant())).ToList();

                    int imgCount = imgFiles.Count;

                    Parallel.ForEach(imgFiles, imgfile =>
                    {
                        if (imgFiles.Count != 0)
                        {
                            string imgFileName = Path.GetFileName(imgfile);
                            string imgFileExtension = Path.GetExtension(imgfile).ToLowerInvariant();
                            string imgFileNameWithoutExt = Path.GetFileNameWithoutExtension(imgfile);

                            if (imgFileExtension != ".dds")
                            {

                                string newFolder = $"{Path.GetDirectoryName(imgfile)}/converted_dds/";

                                if (!Directory.Exists(newFolder))
                                {
                                    Directory.CreateDirectory(newFolder);
                                }

                                string movedImageName = $"{newFolder}{imgFileName}";

                                File.Copy(imgfile, movedImageName, true);

                                ImageHelper.ConvertImageToDds(movedImageName);

                                File.Delete(movedImageName);
                            }
                            else
                            {
                                alreadyDdSs.Add(imgfile);
                            }
                        }
                    });

                    List<string> checkConvertedDds = [];

                    if (Directory.Exists(folder + "/converted_dds/"))
                    {
                        checkConvertedDds = Directory
                            .GetFiles(folder + "/converted_dds/", "*.dds", SearchOption.TopDirectoryOnly).ToList();
                    }

                    thereIsAnyConvertedDdsFile = checkConvertedDds.Count != 0;

                    convertedDds = !thereIsAnyConvertedDdsFile
                        ? []
                        : Directory.GetFiles(folder + "/converted_dds/", "*.dds", SearchOption.TopDirectoryOnly)
                            .ToList();

                    allDdsMerged.AddRange(convertedDds);

                    allDdsMerged.AddRange(alreadyDdSs);

                    YtdHelper.CreateYtdFilesFromFolders(allDdsMerged, folder);

                    if (thereIsAnyConvertedDdsFile)
                    {
                        Directory.Delete(folder + "/converted_dds", true);
                    }
                });

                ShowFinishMS.Dispatcher.Invoke(() =>
                {
                    if (ShowFinishMS.IsChecked == true)
                    {
                        MessageBox.Show($"Done, {allFolders.Count} folder(s) processed.", "Information",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                });

                bool isFilesAfterChecked = false;

                ShowFilesAfter.Dispatcher.Invoke(() => isFilesAfterChecked = (bool)ShowFilesAfter.IsChecked);

                if (isFilesAfterChecked)
                {
                    foreach (var parentFolder in _parentFolders)
                    {
                        Process.Start("explorer.exe", parentFolder);
                    }
                }
            });
        }

        private async Task DdsFromFolder(IReadOnlyCollection<string> allFolders)
        {
            await Task.Run(() =>
            {
                Parallel.ForEach(allFolders, folder =>
                {
                    var imgFiles = Directory.EnumerateFiles(folder, "*.*", SearchOption.TopDirectoryOnly).Where(x =>
                        x.EndsWith(".png", StringComparison.InvariantCultureIgnoreCase) ||
                        x.EndsWith(".dds", StringComparison.InvariantCultureIgnoreCase) ||
                        x.EndsWith(".jpg", StringComparison.InvariantCultureIgnoreCase) ||
                        x.EndsWith(".tga", StringComparison.InvariantCultureIgnoreCase) ||
                        x.EndsWith(".bmp", StringComparison.InvariantCultureIgnoreCase) ||
                        x.EndsWith(".tiff", StringComparison.InvariantCultureIgnoreCase) ||
                        x.EndsWith(".tif", StringComparison.InvariantCultureIgnoreCase) ||
                        x.EndsWith(".jpeg", StringComparison.InvariantCultureIgnoreCase)).ToList();

                    if (imgFiles.Count != 0)
                    {
                        Parallel.ForEach(imgFiles, imgfile =>
                        {
                            var imgFileName = Path.GetFileName(imgfile);

                            ImageHelper.ConvertImageToDds(imgfile);
                        });
                    }
                });

                ShowFinishMS.Dispatcher.Invoke(() =>
                {
                    if (ShowFinishMS.IsChecked == true)
                    {
                        MessageBox.Show($"Done, {allFolders.Count} folder(s) processed.", "Information",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                });
            });
        }

        private void btnClear_Click(object sender, RoutedEventArgs e)
        {
            _foldersList.Clear();
            lbFolderView.ItemsSource = null;
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void btnHelpabout_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(
                "This program was created to convert one or more folders with textures in its interior to a fully working .YTD" +
                "\n\n● Quality settings are intended for its use in non-DDS files to convert them to .DDS with the selected quality." +
                "\n\n● Transparency detection works to determinate transparency for non-DDS files, in order to create a .DDS with proper compression, particularly useful when you are trying to bring textures to memory constrained enviroments such as FiveM servers." +
                "\n\n● During conversion your textures will be resized to be power of two, this doesn't mean that your texture will become seamless, if your texture wasn't like that before the resizing, it won't make any difference." +
                "\n\n● The threshold slider works if you need to manually adjust the threshold (a hidden value which determinates how the resized image will stretch out), if your resulting texture(s) are oddly stretched, you should change this value and try again but beware, this value applies to every texture." +
                "\n\n● The format option lets you to choose between just converting the images directly to .DDS or, conversely, create YTD(s) out of the folders you have selected." +
                "\n\n● Generate MipMaps? is responsible for generating mipmaps for your textures, this option should be left as checked.",
                "Information", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void spToolbar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                DragMove();
            }
        }

        private void lbFolderView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            lbFolderView.ScrollIntoView(lbFolderView.SelectedItem);
        }

        private void btnToggleDarkMode_Click(object sender, RoutedEventArgs e)
        {
            var theme = _paletteHelper.GetTheme();
            IBaseTheme temabase;

            if (theme.GetBaseTheme() == BaseTheme.Dark)
            {
                temabase = new MaterialDesignLightTheme();
            }
            else
            {
                temabase = new MaterialDesignDarkTheme();
            }

            theme.SetBaseTheme(temabase);
            _paletteHelper.SetTheme(theme);
        }

        private void lbFolderView_Drop(object sender, System.Windows.DragEventArgs e)
        {
            List<string> folderpaths = ((string[])e.Data.GetData(System.Windows.DataFormats.FileDrop, true)).ToList();
            List<string> validFolders = folderpaths.Where(Directory.Exists).ToList();
            _foldersList = _foldersList.Concat(validFolders.ToList()).Distinct().ToList();
            lbFolderView.ItemsSource = _foldersList;
        }

        private void btnMinimize_Click_1(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void FormatOutput_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ShowFilesAfter.IsEnabled = FormatOutput.SelectedIndex != 1;
        }

        private void ToggleControls(bool state)
        {
            //Parte de arriba
            btnClose.Dispatcher.Invoke(() => { btnClose.IsEnabled = state; });
            btnMinimize.Dispatcher.Invoke(() => { btnMinimize.IsEnabled = state; });
            btnToggleDarkMode.Dispatcher.Invoke(() => { btnToggleDarkMode.IsEnabled = state; });
            btnHelpabout.Dispatcher.Invoke(() => { btnHelpabout.IsEnabled = state; });

            //Parte 1
            btnSelectFolders.Dispatcher.Invoke(() => { btnSelectFolders.IsEnabled = state; });
            lbFolderView.Dispatcher.Invoke(() => { lbFolderView.IsHitTestVisible = state; });

            //Parte del medio
            SpCenter.Dispatcher.Invoke(() => { SpCenter.IsEnabled = state; });
        }


        private void btnSelectFolders_Click(object sender, RoutedEventArgs e)
        {
            VistaFolderBrowserDialog vfbd = new()
            {
                Multiselect = true
            };

            if (!(bool)vfbd.ShowDialog()!) return;
            _foldersList = _foldersList.Concat(vfbd.SelectedPaths.ToList()).Distinct().ToList();

            lbFolderView.ItemsSource = _foldersList;
            lbFolderView.ScrollIntoView(lbFolderView.Items[0]!);
        }

        private async void btnConvert_Click(object sender, RoutedEventArgs e)
        {

            if (_foldersList.Count == 0)
            {
                MessageBox.Show("Add some folders before converting.", "Error", MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
            else
            {
                switch (FormatOutput.SelectedIndex)
                {
                    case 0:
                        ToggleControls(false);
                        await YtdFromFolders(_foldersList).ConfigureAwait(false);
                        ToggleControls(true);
                        break;
                    case 1:
                        ToggleControls(false);
                        await DdsFromFolder(_foldersList).ConfigureAwait(false);
                        ToggleControls(true);
                        break;
                    case 2:
                        ToggleControls(false);
                        await YtdPerImage(_foldersList).ConfigureAwait(false);
                        ToggleControls(true);
                        break;
                    default:
                        break;
                }
            }
        }
    }
}
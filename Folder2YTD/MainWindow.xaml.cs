using AutoUpdaterDotNET;
using BCnEncoder.Encoder;
using BCnEncoder.ImageSharp;
using BCnEncoder.Shared;
using CodeWalker.GameFiles;
using CodeWalker.Utils;
using MaterialDesignThemes.Wpf;
using Ookii.Dialogs.Wpf;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Application = System.Windows.Application;
using Image = SixLabors.ImageSharp.Image;
using MessageBox = System.Windows.MessageBox;
using Path = System.IO.Path;

namespace Folder2YTD
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        private List<string> _foldersList = new();
        private readonly PaletteHelper _paletteHelper = new();
        private List<string> _parentFolders = new();
       
        //Just for command mode
        public bool VichoToolsMode = false;
        
        
        public MainWindow()
        {
            InitializeComponent();
            InitProgram();
            
        }

        public void InitProgram()
        {
            var version = Assembly.GetExecutingAssembly().GetName().Version;
            AppVersionLabel.Content = $"v{version}";
            TransparencyTypes.ItemsSource = new List<string>() { "Off", "By pixels" };
            FormatOutput.ItemsSource = new List<string>() { "GTA V (.YTD)", "DDS Files", "YTD per image" };
            QualitySettings.ItemsSource = new List<string>() { "Fast", "Balanced", "Best Quality" };
            FormatOutput.SelectedIndex = 0;
            TransparencyTypes.SelectedIndex = 1;
            QualitySettings.SelectedIndex = 1;
            GenerateMipMaps.IsChecked = true;
            ShowFilesAfter.IsChecked = true;
            AutoUpdater.Start("https://raw.githubusercontent.com/Hancapo/Folder2YTD/master/Folder2YTD/updateinfo.xml");
            if (File.Exists("./config.ini"))
            {
                VichoToolsMode = true;
                SetSettingsFromArgs();
            }
        }

        public void SetSettingsFromArgs()
        {
            IniFile iniF = new("./config.ini");
            bool _SilentMode = bool.Parse(iniF.ReadValue("Settings", "silentMode"));
            bool _Mipmaps = bool.Parse(iniF.ReadValue("Settings", "mipmaps"));
            bool _Transp = bool.Parse(iniF.ReadValue("Settings", "transparency"));
            string _Folders = iniF.ReadValue("Settings", "folder");
            string _Quality = iniF.ReadValue("Settings", "quality");
            string _Format = iniF.ReadValue("Settings", "format");

            if (_SilentMode)
            {
                Visibility = Visibility.Hidden;
            }

            GenerateMipMaps.IsChecked = _Mipmaps;

            TransparencyTypes.SelectedIndex = _Transp switch
            {
                false => 0,
                true => 1
            };

            QualitySettings.SelectedIndex = _Quality switch
            {
                "fast" => 0,
                "balanced" => 1,
                "hq" => 2,
                _ => QualitySettings.SelectedIndex
            };

            if (_Format == "ytd")
            {
                FormatOutput.SelectedIndex = 0;
            }

            if (_Folders != string.Empty && Directory.Exists(_Folders))
            {
                lbFolderView.ItemsSource = Directory.GetDirectories(_Folders);
                _foldersList = Directory.GetDirectories(_Folders).ToList();

                var eventArgs = new RoutedEventArgs(System.Windows.Controls.Primitives.ButtonBase.ClickEvent);

                btnConvert.RaiseEvent(eventArgs);
                File.Delete("./config.ini");

            }

        }

        public bool IsTransparent(Image<Rgba32> image)
        {
            var isTransp = false;
            image.ProcessPixelRows(amigo =>
            {
                for (var y = 0; y < amigo.Height; y++)
                {
                    var pixelRow = amigo.GetRowSpan(y);

                    foreach (ref var pixel in pixelRow)
                    {
                        if (pixel.A >= 255) continue;
                        isTransp = true;
                        break;
                    }
                }
            });
            return isTransp;
        }

        private void CreateYTDfilesFromFolders(List<string> images, string forceoutput)
        {
            if (images.Count == 0) return;
            var ytd = new YtdFile
            {
                TextureDict = new TextureDictionary()
            };

            var fname = Path.GetDirectoryName(forceoutput);

            ytd.TextureDict.Textures = new ResourcePointerList64<Texture>();
            ytd.TextureDict.TextureNameHashes = new ResourceSimpleList64_uint();
            var data = ytd.Save();

            ytd = TexturesToYTD(TextureListFromDDSFiles(images.ToArray()), ytd);

            data = ytd.Save();

            var outfpath = forceoutput + ".ytd";
            File.WriteAllBytes(outfpath, data);

        }

        private void CreateYTDFilesFromSingleImage(IReadOnlyList<string> images, string outputfolder)
        {
            if (images.Count == 0) return;

            Parallel.For(0, images.Count, i =>
            {
                var ytd = new YtdFile
                {
                    TextureDict = new TextureDictionary
                    {
                        Textures = new ResourcePointerList64<Texture>(),
                        TextureNameHashes = new ResourceSimpleList64_uint()
                    }
                };

                var data = ytd.Save();

                ytd = TexturesToYTD(TextureListFromDDSFiles(new[] { images[i] }), ytd);

                data = ytd.Save();

                var outfpath = outputfolder + "\\" + Path.GetFileNameWithoutExtension(images[i]) + ".ytd";
                File.WriteAllBytes(outfpath, data);
            });


        }

        public YtdFile TexturesToYTD(List<Texture> texturesList, YtdFile ytdFile)
        {
            var textureDictionary = ytdFile.TextureDict;
            textureDictionary.BuildFromTextureList(texturesList);
            return ytdFile;
        }

        public List<Texture> TextureListFromDDSFiles(string[] DdsFiles)
        {
            List<Texture> TextureList = new();

            foreach (var DdsFile in DdsFiles)
            {
                var fn = DdsFile;

                if (!File.Exists(fn)) return null;

                try
                {
                    var dds = File.ReadAllBytes(fn);
                    var tex = DDSIO.GetTexture(dds);
                    tex.Name = Path.GetFileNameWithoutExtension(fn);
                    tex.NameHash = JenkHash.GenHash(tex.Name?.ToLowerInvariant());
                    JenkIndex.Ensure(tex.Name?.ToLowerInvariant());
                    TextureList.Add(tex);

                }
                catch
                {
                    MessageBox.Show($"Unable to load {fn}.\nAre you sure it's a valid .dds file?\nThis .dds file will be skipped for now.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    continue;
                }
            }

            return TextureList;

        }

        private async Task YTDperImage(List<string> allFolders)
        {
            await Task.Run(() =>
            {
                _parentFolders = allFolders.Select(x => Directory.GetParent(x).ToString()).Distinct().ToList();

                Parallel.For(0, allFolders.Count, i =>
                {
                    bool ThereIsAnyConvertedDDSFile = false;
                    List<string> ConvertedDDS = new();
                    List<string> AlreadyDDSs = new();
                    List<string> AllDDSmerged = new();
                    string? folder = allFolders[i];
                    lbFolderView.Dispatcher.Invoke(() => { lbFolderView.SelectedIndex = i; });
                    var imgFiles = Directory.EnumerateFiles(folder, "*.*", SearchOption.TopDirectoryOnly).Where(x =>
                    x.EndsWith(".png", StringComparison.InvariantCultureIgnoreCase) ||
                    x.EndsWith(".dds", StringComparison.InvariantCultureIgnoreCase) ||
                    x.EndsWith(".jpg", StringComparison.InvariantCultureIgnoreCase) ||
                    x.EndsWith(".tga", StringComparison.InvariantCultureIgnoreCase) ||
                    x.EndsWith(".bmp", StringComparison.InvariantCultureIgnoreCase) ||
                    x.EndsWith(".webp", StringComparison.InvariantCultureIgnoreCase) ||
                    x.EndsWith(".tiff", StringComparison.InvariantCultureIgnoreCase) ||
                    x.EndsWith(".tif", StringComparison.InvariantCultureIgnoreCase) ||
                    x.EndsWith(".jpeg", StringComparison.InvariantCultureIgnoreCase)).ToList();

                    int imgCount = imgFiles.Count;

                    LbProgressLog.Dispatcher.Invoke(() => { LbProgressLog.Text += ($"\n\nWorking folder: {Path.GetFileName(folder)}"); });


                    if (imgCount <= 0)
                    {


                        LbProgressLog.Dispatcher.Invoke(() => { LbProgressLog.Text += ("\n0 compatible textures, skipping..."); });

                    }
                    else
                    {
                        LbProgressLog.Dispatcher.Invoke(() => { LbProgressLog.Text += ("\n" + imgCount + " compatible textures."); });

                    }

                    Parallel.ForEach(imgFiles, imgfile =>
                    {
                        if (!imgFiles.Any()) return;
                        string imgFileName = Path.GetFileName(imgfile);
                        string imgFileExtension = Path.GetExtension(imgfile).ToLowerInvariant();

                        if (imgFileExtension is ".png" or ".tga" or ".jpg" or ".bmp" or ".webp" or ".tiff" or ".tif" or ".jpeg")
                        {

                            LbProgressLog.Dispatcher.Invoke(() => { LbProgressLog.Text += ($"\nImage file {imgFileName} found..."); });

                            string NewFolder = $"{Path.GetDirectoryName(imgfile)}/converted_dds/";

                            if (!Directory.Exists(NewFolder))
                            {
                                Directory.CreateDirectory(NewFolder);
                            }


                            var MovedImageName = $"{NewFolder}{imgFileName}";

                            File.Copy(imgfile, MovedImageName, true);

                            LbProgressLog.Dispatcher.Invoke(() => { LbProgressLog.Text += ($"\nConverting {imgFileName} to DDS..."); });


                            if (ConvertImageToDds(MovedImageName))
                            {
                                LbProgressLog.Dispatcher.Invoke(() => { LbProgressLog.Text += ($"\nDDS conversion of {Path.GetFileName(imgFileName)} was successful..."); });

                            }
                            else
                            {
                                LbProgressLog.Dispatcher.Invoke(() => { LbProgressLog.Text += ($"\nDDS conversion of {Path.GetFileName(imgFileName)} has failed, skipping, this SHOULDN'T happen..."); });

                            }




                            File.Delete(MovedImageName);




                        }

                        if (imgFileExtension != ".dds") return;
                        AlreadyDDSs.Add(imgfile);
                        LbProgressLog.Dispatcher.Invoke(() => { LbProgressLog.Text += ($"\nDDS file {imgFileName} found..."); });
                    });


                    List<string> CheckConvertedDDS = new();

                    if (Directory.Exists(folder + "/converted_dds/"))
                    {
                        CheckConvertedDDS = Directory.GetFiles(folder + "/converted_dds/", "*.dds", SearchOption.TopDirectoryOnly).ToList();

                    }

                    ThereIsAnyConvertedDDSFile = CheckConvertedDDS.Any();

                    if (!ThereIsAnyConvertedDDSFile)
                    {
                        ConvertedDDS = new List<string>();

                    }
                    else
                    {
                        ConvertedDDS = Directory.GetFiles(folder + "/converted_dds/", "*.dds", SearchOption.TopDirectoryOnly).ToList();
                    }

                    if (ConvertedDDS.Count != 0 || ConvertedDDS != null)
                    {
                        AllDDSmerged.AddRange(ConvertedDDS);

                    }

                    if (AlreadyDDSs.Count != 0 || AlreadyDDSs != null)
                    {
                        AllDDSmerged.AddRange(AlreadyDDSs);

                    }
                    //Create one YTD file per image inside the folder
                    CreateYTDFilesFromSingleImage(AllDDSmerged, folder);


                    if (ThereIsAnyConvertedDDSFile)
                    {
                        Directory.Delete(folder + "/converted_dds", true);
                    }
                });

                MessageBox.Show($"Done, {allFolders.Count} folder(s) processed.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);

                bool IsFilesAfterChecked = false;

                ShowFilesAfter.Dispatcher.Invoke(() => IsFilesAfterChecked = (bool)ShowFilesAfter.IsChecked);

                if (!IsFilesAfterChecked) return;
                foreach (var parentFolder in _parentFolders)
                {
                    Process.Start("explorer.exe", parentFolder);

                }

            });
        }

        private async Task YTDfromFolders(List<string> allFolders)
        {
            await Task.Run(() =>
            {
                _parentFolders = allFolders.Select(x => Directory.GetParent(x).ToString()).Distinct().ToList();

                Parallel.For(0, allFolders.Count, i =>
                {
                    bool ThereIsAnyConvertedDDSFile = false;
                    List<string> ConvertedDDS = new();
                    List<string> AlreadyDDSs = new();
                    List<string> AllDDSmerged = new();
                    string? folder = allFolders[i];
                    //lbFolderView.Dispatcher.Invoke(() => { lbFolderView.SelectedIndex = i; });
                    var ImgFiles = Directory.EnumerateFiles(folder, "*.*", SearchOption.TopDirectoryOnly).Where(x =>
                    x.EndsWith(".png", StringComparison.InvariantCultureIgnoreCase) ||
                    x.EndsWith(".dds", StringComparison.InvariantCultureIgnoreCase) ||
                    x.EndsWith(".jpg", StringComparison.InvariantCultureIgnoreCase) ||
                    x.EndsWith(".tga", StringComparison.InvariantCultureIgnoreCase) ||
                    x.EndsWith(".bmp", StringComparison.InvariantCultureIgnoreCase) ||
                    x.EndsWith(".webp", StringComparison.InvariantCultureIgnoreCase) ||
                    x.EndsWith(".tiff", StringComparison.InvariantCultureIgnoreCase) ||
                    x.EndsWith(".tif", StringComparison.InvariantCultureIgnoreCase) ||
                    x.EndsWith(".jpeg", StringComparison.InvariantCultureIgnoreCase)).ToList();

                    int ImgCount = ImgFiles.Count;

                    LbProgressLog.Dispatcher.Invoke(() => { LbProgressLog.Text += ($"\n\nWorking folder: {Path.GetFileName(folder)}"); });


                    if (ImgCount <= 0)
                    {


                        LbProgressLog.Dispatcher.Invoke(() => { LbProgressLog.Text += ("\n0 compatible textures, skipping..."); });

                    }
                    else
                    {
                        LbProgressLog.Dispatcher.Invoke(() => { LbProgressLog.Text += ("\n" + ImgCount + " compatible textures."); });

                    }

                    Parallel.ForEach(ImgFiles, imgfile =>
                    {
                        if (ImgFiles.Any())
                        {
                            string ImgFileName = Path.GetFileName(imgfile);
                            string ImgFileExtension = Path.GetExtension(imgfile).ToLowerInvariant();
                            string ImgFileNameWithoutExt = Path.GetFileNameWithoutExtension(imgfile);

                            if (ImgFileExtension is ".png" or ".tga" or ".jpg" or ".bmp" or ".webp" or ".tiff" or ".tif" or ".jpeg")
                            {

                                LbProgressLog.Dispatcher.Invoke(() => { LbProgressLog.Text += ($"\nImage file {ImgFileName} found..."); });

                                string NewFolder = $"{Path.GetDirectoryName(imgfile)}/converted_dds/";

                                if (!Directory.Exists(NewFolder))
                                {
                                    Directory.CreateDirectory(NewFolder);
                                }


                                string MovedImageName = $"{NewFolder}{ImgFileName}";

                                File.Copy(imgfile, MovedImageName, true);

                                LbProgressLog.Dispatcher.Invoke(() => { LbProgressLog.Text += ($"\nConverting {ImgFileName} to DDS..."); });


                                if (ConvertImageToDds(MovedImageName))
                                {
                                    LbProgressLog.Dispatcher.Invoke(() => { LbProgressLog.Text += ($"\nDDS conversion of {Path.GetFileName(ImgFileName)} was successful..."); });

                                }
                                else
                                {
                                    LbProgressLog.Dispatcher.Invoke(() => { LbProgressLog.Text += ($"\nDDS conversion of {Path.GetFileName(ImgFileName)} has failed, skipping, this SHOULDN'T happen..."); });

                                }




                                File.Delete(MovedImageName);




                            }

                            if (ImgFileExtension != ".dds") return;
                            AlreadyDDSs.Add(imgfile);
                            LbProgressLog.Dispatcher.Invoke(() => { LbProgressLog.Text += ($"\nDDS file {ImgFileName} found..."); });




                        }
                    });


                    List<string> CheckConvertedDDS = new();

                    if (Directory.Exists(folder + "/converted_dds/"))
                    {
                        CheckConvertedDDS = Directory.GetFiles(folder + "/converted_dds/", "*.dds", SearchOption.TopDirectoryOnly).ToList();

                    }

                    ThereIsAnyConvertedDDSFile = CheckConvertedDDS.Any();

                    if (!ThereIsAnyConvertedDDSFile)
                    {
                        ConvertedDDS = new List<string>();

                    }
                    else
                    {
                        ConvertedDDS = Directory.GetFiles(folder + "/converted_dds/", "*.dds", SearchOption.TopDirectoryOnly).ToList();
                    }

                    if (ConvertedDDS.Count != 0 || ConvertedDDS != null)
                    {
                        AllDDSmerged.AddRange(ConvertedDDS);

                    }

                    if (AlreadyDDSs.Count != 0 || AlreadyDDSs != null)
                    {
                        AllDDSmerged.AddRange(AlreadyDDSs);

                    }
                    LbProgressLog.Dispatcher.Invoke(() => { LbProgressLog.Text += ($"\nCreating {Path.GetFileName(folder)}.ytd file..."); });
                    CreateYTDfilesFromFolders(AllDDSmerged, folder);

                    if (ThereIsAnyConvertedDDSFile)
                    {
                        Directory.Delete(folder + "/converted_dds", true);
                    }
                });

                if (!VichoToolsMode)
                {
                    MessageBox.Show($"Done, {allFolders.Count} folder(s) processed.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                    bool IsFilesAfterChecked = false;

                    ShowFilesAfter.Dispatcher.Invoke(() => IsFilesAfterChecked = (bool)ShowFilesAfter.IsChecked);

                    if (IsFilesAfterChecked)
                    {
                        foreach (var parentFolder in _parentFolders)
                        {
                            Process.Start("explorer.exe", parentFolder);

                        }
                    }
                }
                else
                {
                    Dispatcher.Invoke(() => {Close();});
                }

                

            });
        }

        private async Task DDSfromFolder(List<string> allFolders)
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
                    x.EndsWith(".webp", StringComparison.InvariantCultureIgnoreCase) ||
                    x.EndsWith(".tiff", StringComparison.InvariantCultureIgnoreCase) ||
                    x.EndsWith(".tif", StringComparison.InvariantCultureIgnoreCase) ||
                    x.EndsWith(".jpeg", StringComparison.InvariantCultureIgnoreCase)).ToList();
                    int? imgCount = imgFiles.Count;

                    LbProgressLog.Dispatcher.Invoke(() => { LbProgressLog.Text += ($"\n\nWorking folder: {Path.GetFileName(folder)}"); });


                    if (imgCount <= 0)
                    {

                        LbProgressLog.Dispatcher.Invoke(() => { LbProgressLog.Text += ("\n0 compatible textures, skipping..."); });
                    }
                    else
                    {
                        LbProgressLog.Dispatcher.Invoke(() => { LbProgressLog.Text += ("\n" + imgCount + " compatible textures."); });

                    }
                    if (imgFiles.Any())
                    {

                        Parallel.ForEach(imgFiles, imgfile =>
                        {
                            string ImgFileName = Path.GetFileName(imgfile);


                            LbProgressLog.Dispatcher.Invoke(() => { LbProgressLog.Text += ($"\nImage file {ImgFileName} found..."); });

                            LbProgressLog.Dispatcher.Invoke(() => { LbProgressLog.Text += ($"\nConverting {ImgFileName} to DDS..."); });

                            if (ConvertImageToDds(imgfile))
                            {
                                LbProgressLog.Dispatcher.Invoke(() => { LbProgressLog.Text += ($"\nThe image was successfully converted to DDS..."); });

                            }
                            else
                            {
                                LbProgressLog.Dispatcher.Invoke(() => { LbProgressLog.Text += ($"\nThe image couldn't be converted to DDS, skipping..."); });

                            }
                        });
                    }
                });

                MessageBox.Show($"Done, {allFolders.Count} folder(s) processed.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);

            });
        }
        private void btnClear_Click(object sender, RoutedEventArgs e)
        {
            _foldersList.Clear();
            lbFolderView.ItemsSource = null;
            LbProgressLog.Text = String.Empty;
        }
        private bool ConvertImageToDds(string filename)
        {
            bool isMipMapChecked = false;

            int QualitySelectedIndex = -1;

            int TransparencyDetection = -1;

            Image<Rgba32> image;

            try
            {
                image = Image.Load<Rgba32>(filename);

            }
            catch (Exception)
            {

                return false;
            }

            image = ResizedImage(image);

            BcEncoder bcEncoder = new();

            bcEncoder.OutputOptions.FileFormat = OutputFileFormat.Dds;
            QualitySettings.Dispatcher.Invoke(() => { QualitySelectedIndex = QualitySettings.SelectedIndex; });

            TransparencyTypes.Dispatcher.Invoke(() => { TransparencyDetection = TransparencyTypes.SelectedIndex; });

            GenerateMipMaps.Dispatcher.Invoke(() => { isMipMapChecked = (bool)GenerateMipMaps.IsChecked; });

            if (isMipMapChecked)
            {
                bcEncoder.OutputOptions.GenerateMipMaps = true;
            }
            else
            {
                bcEncoder.OutputOptions.GenerateMipMaps = false;
            }

            bcEncoder.OutputOptions.Quality = QualitySelectedIndex switch
            {
                0 => CompressionQuality.Fast,
                1 => CompressionQuality.Balanced,
                2 => CompressionQuality.BestQuality,
                _ => CompressionQuality.Balanced,
            };
            if (TransparencyDetection == 1)
            {
                if (IsTransparent(image))
                {
                    bcEncoder.OutputOptions.Format = CompressionFormat.Bc3;
                }
                else
                {
                    bcEncoder.OutputOptions.Format = CompressionFormat.Bc1;

                }
            }
            else
            {
                bcEncoder.OutputOptions.Format = CompressionFormat.Bc3;

            }
            string GetImageName = Path.GetFileNameWithoutExtension(filename);
            try
            {
                var SaveDDS = bcEncoder.EncodeToDds(image);

                MemoryStream ms = new();

                SaveDDS.Write(ms);

                byte[] ddsbytes = ms.ToArray();


                File.WriteAllBytes(Path.GetDirectoryName(filename) + "/" + GetImageName + ".dds", ddsbytes);
                return true;
            }
            catch
            {
                return false;
            }


        }
        private Image<Rgba32> ResizedImage(Image<Rgba32> image)
        {
            double thresholdA = 0;
            ThresPower.Dispatcher.Invoke(() => { thresholdA = ThresPower.Value; });
            
            var height = ConvertHeightAndWidthToPowerOfTwo(image.Width, image.Height, thresholdA).Item2;
            var width = ConvertHeightAndWidthToPowerOfTwo(image.Width, image.Height, thresholdA).Item1;

            image.Mutate(x => x.Resize(width, height, KnownResamplers.Lanczos3));
            return image;
        }
        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void btnHelpabout_Click(object sender, RoutedEventArgs e)
        {

            MessageBox.Show("This program was created to convert one or more folders with textures in its interior to a fully working .YTD" +
                "\n\n● Quality settings are intended for its use in non-DDS files to convert them to .DDS with the selected quality." +
                "\n\n● Transparency detection works to determinate transparency for non-DDS files, in order to create a .DDS with proper compression, particularly useful when you are trying to bring textures to memory constrained enviroments such as FiveM servers." +
                "\n\n● During conversion your textures will be resized to be power of two, this doesn't mean that your texture will become seamless, if your texture wasn't like that before the resizing, it won't make any difference." +
                "\n\n● The threshold slider works if you need to manually adjust the threshold (a hidden value which determinates how the resized image will stretch out), if your resulting texture(s) are oddly stretched, you should change this value and try again but beware, this value applies to every texture." +
                "\n\n● The format option lets you to choose between just converting the images directly to .DDS or, conversely, create YTD(s) out of the folders you have selected." +
                "\n\n● Generate MipMaps? is responsible for generating mipmaps for your textures, this option should be left as checked.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
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

        private void LbProgressLog_TextChanged_1(object sender, TextChangedEventArgs e)
        {
            LbProgressLog.ScrollToEnd();

        }

        private void btnToggleDarkMode_Click(object sender, RoutedEventArgs e)
        {
            var theme = _paletteHelper.GetTheme();
            IBaseTheme Temabase;

            if (theme.GetBaseTheme() == BaseTheme.Dark)
            {
                Temabase = new MaterialDesignLightTheme();
                LbProgressLog.Foreground = new SolidColorBrush(Colors.DarkGreen);
            }
            else
            {
                Temabase = new MaterialDesignDarkTheme();
                LbProgressLog.Foreground = new SolidColorBrush(Colors.LightGreen);

            }

            theme.SetBaseTheme(Temabase);
            _paletteHelper.SetTheme(theme);

        }

        private void lbFolderView_Drop(object sender, System.Windows.DragEventArgs e)
        {
            List<string> folderpaths = ((string[])e.Data.GetData(System.Windows.DataFormats.FileDrop, true)).ToList();
            List<string> validFolders = folderpaths.Where(folderpath => Directory.Exists(folderpath)).ToList();
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
            spCenter.Dispatcher.Invoke(() => { spCenter.IsEnabled = state; });
            spThreshold.Dispatcher.Invoke(() => { spThreshold.IsEnabled = state; });
        }

        public (int, int) ConvertHeightAndWidthToPowerOfTwo(int width, int height, double threshold)
        {


            if ((Math.Log2(width) % 1) == 0 && (Math.Log2(height) % 1) == 0)
            {
                return (width, height);
            }
            else
            {
                if (Math.Abs(height - width) < threshold)
                {
                    if (height < width)
                    {
                        width = height;

                    }
                    else
                    {
                        height = width;
                    }
                }

                height = (int)Math.Pow(2, Math.Round(Math.Log2(height)));
                width = (int)Math.Pow(2, Math.Round(Math.Log2(width)));

                return (width, height);
            }

        }

        private void btnSelectFolders_Click(object sender, RoutedEventArgs e)
        {

            VistaFolderBrowserDialog vfbd = new()
            {
                Multiselect = true
            };

            if ((bool)vfbd.ShowDialog())
            {
                _foldersList = _foldersList.Concat(vfbd.SelectedPaths.ToList()).Distinct().ToList();

                lbFolderView.ItemsSource = _foldersList;
                LbProgressLog.Text = null;
                lbFolderView.ScrollIntoView(lbFolderView.Items[0]);
            }

        }

        private async void btnConvert_Click(object sender, RoutedEventArgs e)
        {
            LbProgressLog.Dispatcher.Invoke(() => { LbProgressLog.Text = string.Empty; });

            if (_foldersList.Count == 0)
            {
                MessageBox.Show("Add some folders before converting.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            else
            {

                switch (FormatOutput.SelectedIndex)
                {
                    case 0:
                        ToggleControls(false);
                        await YTDfromFolders(_foldersList).ConfigureAwait(false);
                        if (!VichoToolsMode)
                        {
                            ToggleControls(true);
                        }
                        break;
                    case 1:
                        ToggleControls(false);
                        await DDSfromFolder(_foldersList).ConfigureAwait(false);
                        ToggleControls(true);
                        break;
                    case 2:
                        ToggleControls(false);
                        await YTDperImage(_foldersList).ConfigureAwait(false);
                        ToggleControls(true);
                        break;
                    default:
                        break;
                }
            }
        }
    }
}

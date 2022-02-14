using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Ookii.Dialogs.Wpf;
using System.IO;
using Path = System.IO.Path;
using BCnEncoder.Encoder;
using BCnEncoder.Shared;
using BCnEncoder.ImageSharp;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using MessageBox = System.Windows.MessageBox;
using Image = SixLabors.ImageSharp.Image;
using CodeWalker.GameFiles;
using CodeWalker.Utils;
using BCnEncoder.Decoder;
using Microsoft.Toolkit.HighPerformance;
using SixLabors.ImageSharp.Processing;
using MaterialDesignThemes.Wpf;
using System.Windows.Media;

namespace Folder2YTD
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        
        private List<string> FoldersList = new();
        private readonly PaletteHelper _paletteHelper = new PaletteHelper();

        public MainWindow()
        {
            InitializeComponent();
            TransparencyTypes.ItemsSource = new List<string>() { "Off", "By pixels"};
            FormatOutput.ItemsSource = new List<string>() { "GTA V (.YTD)" };
            QualitySettings.ItemsSource = new List<string>() { "Fast", "Balanced", "Best Quality" };
            FormatOutput.SelectedIndex = 0;
            TransparencyTypes.SelectedIndex = 1;
            QualitySettings.SelectedIndex = 1;
            GenerateMipMaps.IsChecked = true;

        }

        private void btnTesteo_Click(object sender, RoutedEventArgs e)
        {


        }

        public bool IsTransparent(Image<Rgba32> image)
        {
            bool IsTransp = false;

            image.ProcessPixelRows(amigo =>
            {
                for (int y = 0; y < amigo.Height; y++)
                {
                    Span<Rgba32> pixelRow = amigo.GetRowSpan(y);

                    foreach (ref Rgba32 pixel in pixelRow)
                    {
                        if (pixel.A < 255)
                        {
                            IsTransp = true;
                            break;
                        }
                    }
                }
            });

            return IsTransp;

            

        }

        private void CreateYTDfilesFromFolders(List<string> Images, string forceoutput)
        {


            if (Images.Count != 0)
            {

                var ytd = new YtdFile();
                ytd.TextureDict = new TextureDictionary();

                string fname = Path.GetDirectoryName(forceoutput);

                ytd.TextureDict.Textures = new ResourcePointerList64<Texture>();
                ytd.TextureDict.TextureNameHashes = new ResourceSimpleList64_uint();
                var data = ytd.Save();

                ytd = TexturesToYTD(TextureListFromDDSFiles(Images.ToArray()), ytd);

                data = ytd.Save();

                var outfpath = forceoutput + ".ytd";
                File.WriteAllBytes(outfpath, data);
            }

        }

        public YtdFile TexturesToYTD(List<Texture> TexturesList, YtdFile ytdFile)
        {
            var textureDictionary = ytdFile.TextureDict;

            textureDictionary.BuildFromTextureList(TexturesList);

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

        private void btnSelectFolders_Click(object sender, RoutedEventArgs e)
        {

            VistaFolderBrowserDialog vfbd = new()
            {
                Multiselect = true
            };

            if ((bool)vfbd.ShowDialog())
            {
                FoldersList = FoldersList.Concat(vfbd.SelectedPaths.ToList()).Distinct().ToList();

                lbFolderView.ItemsSource = FoldersList;
                LbProgressLog.Text = null;
                lbFolderView.ScrollIntoView(lbFolderView.Items[0]);

            }

        }

        private async void btnConvert_Click(object sender, RoutedEventArgs e)
        {
            LbProgressLog.Dispatcher.Invoke(() => { LbProgressLog.Text = String.Empty; });

            if (FoldersList.Count == 0)
            {
                MessageBox.Show("Select some folders before converting.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            else
            {
                await YTDfromFolders(FoldersList).ConfigureAwait(false);
            }
        }

        private async Task YTDfromFolders(List<string> AllFolders)
        {
            

            await Task.Run(() =>
            {
                for (int i = 0; i < AllFolders.Count; i++)
                {

                    List<string> ConvertedDDS = new();
                    List<string> AlreadyDDSs = new();

                    List<string> AllDDSmerged = new();

                    string? folder = AllFolders[i];

                    lbFolderView.Dispatcher.Invoke(() => { lbFolderView.SelectedIndex = i; });

                    
                    var ImgFiles = (List<string>)Directory.EnumerateFiles(folder, "*.*", SearchOption.TopDirectoryOnly).Where(x => x.EndsWith(".png") || x.EndsWith(".dds") || x.EndsWith(".jpg") || x.EndsWith(".tga") || x.EndsWith(".bmp") || x.EndsWith(".webp") || x.EndsWith(".tiff") || x.EndsWith(".jpeg")).ToList();

                    int ImgCount = ImgFiles.Count();

                    LbProgressLog.Dispatcher.Invoke(() => { LbProgressLog.Text += ($"\n\nWorking folder: {Path.GetFileName(folder)}"); });


                    if (ImgCount <= 0)
                    {

                        
                        LbProgressLog.Dispatcher.Invoke(() => { LbProgressLog.Text += ("\n0 compatible textures, skipping..."); });

                    }
                    else
                    {
                        LbProgressLog.Dispatcher.Invoke(() => { LbProgressLog.Text += ("\n" + ImgCount + " compatible textures."); });

                    }


                    foreach (var imgfile in ImgFiles)
                    {


                        if (ImgFiles.Any())
                        {
                            string ImgFileName = Path.GetFileName(imgfile);
                            string ImgFileNameWithoutExt = Path.GetFileNameWithoutExtension(imgfile);



                            //if (ImgFileName.Contains(" "))
                            //{
                            //    LbProgressLog.Dispatcher.Invoke(() => { LbProgressLog.AppendText($"\nImage file {ImgFileName} contains one or more blank characters, please rename it manually, skipping..."); });
                            //    continue;
                            //}




                            if (ImgFileName.EndsWith(".png") || ImgFileName.EndsWith(".tga") || ImgFileName.EndsWith(".jpg") || ImgFileName.EndsWith(".bmp") || ImgFileName.EndsWith(".webp") || ImgFileName.EndsWith(".tiff") || ImgFileName.EndsWith(".tif") || ImgFileName.EndsWith(".tif") || ImgFileName.EndsWith(".jpeg"))
                            {

                                LbProgressLog.Dispatcher.Invoke(() => { LbProgressLog.Text += ($"\nImage file {ImgFileName} found..."); }) ;

                                string NewFolder = $"{Path.GetDirectoryName(imgfile)}/converted_dds/";

                                if (!Directory.Exists(NewFolder))
                                {
                                    Directory.CreateDirectory(NewFolder);
                                }


                                string MovedImageName = $"{NewFolder}{ImgFileName}";

                                File.Copy(imgfile, MovedImageName, true);

                                ConvertImageToDDS(MovedImageName);

                                LbProgressLog.Dispatcher.Invoke(() => { LbProgressLog.Text += ($"\nConverting {ImgFileName} to DDS..."); });


                                File.Delete(MovedImageName);




                            }
                            if (ImgFileName.EndsWith(".dds"))
                            {
                                AlreadyDDSs.Add(imgfile);
                                LbProgressLog.Dispatcher.Invoke(() => { LbProgressLog.Text += ($"\nDDS file {ImgFileName} found..."); });

                            }




                        }
                    }

                    try
                    {
                        ConvertedDDS = Directory.GetFiles(folder + "/converted_dds/", "*.dds", SearchOption.TopDirectoryOnly).ToList();

                    }
                    catch
                    {

                        ConvertedDDS = new List<string>();
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

                    try
                    {
                        Directory.Delete(folder + "/converted_dds", true);

                    }
                    catch
                    {

                    }


                }

                MessageBox.Show($"Done, {AllFolders.Count} folder(s) processed.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
            });
        }

        private void btnClear_Click(object sender, RoutedEventArgs e)
        {
            FoldersList.Clear();
            lbFolderView.ItemsSource = null;
            LbProgressLog.Text = String.Empty;
        }
        private void ConvertImageToDDS(string filename)
        {

            bool isMipMapChecked = false;

            int QualitySelectedIndex = -1;

            int TransparencyDetection = -1;

            
            Image<Rgba32> image = Image.Load<Rgba32>(filename);
            
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

            switch (QualitySelectedIndex)
            {
                case 0:
                    bcEncoder.OutputOptions.Quality = CompressionQuality.Fast;
                    break;
                case 1:
                    bcEncoder.OutputOptions.Quality = CompressionQuality.Balanced;
                    break;

                case 2:
                    bcEncoder.OutputOptions.Quality = CompressionQuality.BestQuality;
                    break;

                default:
                    bcEncoder.OutputOptions.Quality = CompressionQuality.Balanced;
                    break;
            }


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

            //FileStream fs = File.Create(Path.GetDirectoryName(filename) + "/" + GetImageName + ".dds");


            try
            {
                var SaveDDS = bcEncoder.EncodeToDds(image);

                MemoryStream ms = new();

                SaveDDS.Write(ms);

                byte[] ddsbytes = ms.ToArray();


                File.WriteAllBytes(Path.GetDirectoryName(filename) + "/" + GetImageName + ".dds", ddsbytes);
                LbProgressLog.Dispatcher.Invoke(() => { LbProgressLog.Text += ($"\nDDS conversion of {Path.GetFileName(GetImageName)} was sucessful..."); });
            }
            catch
            {



                LbProgressLog.Dispatcher.Invoke(() => { LbProgressLog.Text += ($"\nDDS conversion of {Path.GetFileName(GetImageName)} has failed, skipping, this SHOULDN'T happen...");});
                


            }


        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void btnMinimize_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void btnHelpabout_Click(object sender, RoutedEventArgs e)
        {

            MessageBox.Show("This program was created to convert one or more folder with textures in its interior to a fully working .YTD\n\n● Quality settings are intended for its use in non-DDS files to convert them to .DDS with the selected quality.\n\n● Transparency detection works to determinate transparency for non-DDS files, in order to create a .DDS with proper compression, particularly useful when you are trying to bring textures to memory constrained enviroments such as FiveM servers.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
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
            ITheme theme = _paletteHelper.GetTheme();
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
    }
}

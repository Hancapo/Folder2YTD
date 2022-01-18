using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Forms;
using Ookii.Dialogs.Wpf;
using System.Drawing;
using System.IO;
using Path = System.IO.Path;

namespace Folder2YTD
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            TransparencyTypes.ItemsSource = new List<string>() {"Off", "By flags (PNG only)", "By pixels" };
            FormatOutput.ItemsSource = new List<string>() {"GTA V (.YTD)" };
            FormatOutput.SelectedIndex = 0;
            TransparencyTypes.SelectedIndex = 0;    
        }

        private void btnTesteo_Click(object sender, RoutedEventArgs e)
        {

            
            
        }

        public bool IsTransparent(Bitmap image)
        {
            bool IsTransp = false;

            if (TransparencyTypes.SelectedIndex == 1)
            {
                //if (image.ToString().Contains(".tga"))
                //{

                //}
                if ((image.Flags & 0x2) != 0)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            if (TransparencyTypes.SelectedIndex == 2)
            {
                for (int y = 0; y < image.Height; ++y)
                {
                    for (int x = 0; x < image.Width; ++x)
                    {
                        if (image.GetPixel(x, y).A < 255)
                        {
                            return true;
                        }
                    }
                }
                return false;
            }

            if (TransparencyTypes.SelectedIndex == 0)
            {
                return false;
            }


            return IsTransp;

        }

        private void btnSelectFolders_Click(object sender, RoutedEventArgs e)
        {
           
            VistaFolderBrowserDialog dialog = new VistaFolderBrowserDialog();
            dialog.Multiselect = true;

            if ((bool)dialog.ShowDialog())
            {
                lbFolderView.ItemsSource = dialog.SelectedPaths;
            }
            
        }

        private void btnConvert_Click(object sender, RoutedEventArgs e)
        {

        }

        private async Task YTDfromFolders(List<string> AllFolders)
        {
            string DXTFormat = string.Empty;
            int MipMapCount = 0;

            await Task.Run(() =>
            {
                foreach (var folder in AllFolders)
                {
                    var ImgFiles = Directory.EnumerateFiles(folder, "*.*", SearchOption.TopDirectoryOnly).Where(x => x.EndsWith(".png") || x.EndsWith(".dds"));

                    int ImgCount = ImgFiles.Count();

                    if (ImgCount <= 0)
                    {
                        LbProgressLog.AppendText("\n0 compatible textures, skipping...");
                    }
                    else
                    {
                        LbProgressLog.AppendText("\n" + ImgCount + " compatible textures.");

                    }

                    foreach (var imgfile in ImgFiles)
                    {
                        if (ImgFiles.Any())
                        {
                            string ImgFileName = Path.GetFileName(imgfile);
                            string ImgFileNameWithoutExt = Path.GetFileNameWithoutExtension(imgfile);
                        }
                    }
                }
            });
        }
    }
}

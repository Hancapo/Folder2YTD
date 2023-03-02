using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Windows;
using static System.Windows.Forms.DataFormats;
using Application = System.Windows.Application;
using MessageBox = System.Windows.MessageBox;

namespace Folder2YTD
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        bool silentMode = false;
        string folder = "";
        bool mipmaps = false;
        string quality = "";
        bool transparency = false;
        string format = "";
        public void Application_Startup(object sender, StartupEventArgs e)
        {
            string[] commandLineArgs = Environment.GetCommandLineArgs();

            if (commandLineArgs.Length > 1)
            {
                for (int i = 1; i < commandLineArgs.Length; i++)
                {
                    switch (commandLineArgs[i])
                    {
                        case "-silentmode":
                            silentMode = true;
                            break;
                        case "-folder":
                            folder = commandLineArgs[++i];
                            break;
                        case "-mipmaps":
                            mipmaps = true;
                            break;
                        case "-quality":
                            quality = commandLineArgs[++i];
                            break;
                        case "-transparency":
                            transparency = true;
                            break;
                        case "-format":
                            format = commandLineArgs[++i];
                            break;
                        default:
                            Console.WriteLine("Argumento no reconocido: " + commandLineArgs[i]);
                            break;
                    }
                }

                IniFile ini = new("./config.ini");
                ini.WriteValue("Settings", "silentMode", silentMode.ToString());
                ini.WriteValue("Settings", "folder", folder);
                ini.WriteValue("Settings", "mipmaps", mipmaps.ToString());
                ini.WriteValue("Settings", "quality", quality);
                ini.WriteValue("Settings", "transparency", transparency.ToString());
                ini.WriteValue("Settings", "format", format);

                
            }

            MainWindow mw = new();
            mw.Show();


        }
    }
}

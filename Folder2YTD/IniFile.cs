using System.Runtime.InteropServices;
using System.Text;

namespace Folder2YTD
{
    public class IniFile
    {
        private string filePath;

        [DllImport("kernel32")]
        private static extern int GetPrivateProfileString(string section, string key, string def, StringBuilder retVal, int size, string filePath);

        public IniFile(string filePath)
        {
            this.filePath = filePath;
        }

        public string ReadValue(string section, string key)
        {
            StringBuilder retVal = new(255);
            GetPrivateProfileString(section, key, "", retVal, 255, filePath);
            return retVal.ToString();
        }
        
        public void WriteValue(string section, string key, string value)
        {
            WritePrivateProfileString(section, key, value, filePath);
        }

        [DllImport("kernel32")]
        private static extern long WritePrivateProfileString(string section, string key, string val, string filePath);
    }
}

using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace BingDesktopImageApp
{
    public class DesktopUtility
    {
        private const int SPI_SETDESKWALLPAPER = 20;
        private const int SPIF_UPDATEINIFILE = 0x01;
        private const int SPIF_SENDWININICHANGE = 0x02;

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        static extern int SystemParametersInfo(int uAction, int uParam, string lpvParam, int fuWinIni);

        public static void SetDesktopBackground(string path, BackgroundStyle style = BackgroundStyle.Stretched)
        {
            var key = Registry.CurrentUser.OpenSubKey(@"Control Panel\Desktop", true);

            var styleValue = Math.Max(1, (int)style);
            var tileValue = Math.Max(0, 1 - (int)style);

            key.SetValue(@"WallpaperStyle", styleValue.ToString());
            key.SetValue(@"TileWallpaper", tileValue.ToString());

            SystemParametersInfo(SPI_SETDESKWALLPAPER, 0, path, SPIF_UPDATEINIFILE | SPIF_SENDWININICHANGE);
        }
    }
}
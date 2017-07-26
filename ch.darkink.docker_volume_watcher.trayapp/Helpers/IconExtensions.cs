using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ch.darkink.docker_volume_watcher.trayapp.Helpers {
    public static class IconExtensions {

        public static ImageSource ToImageSource(this Icon icon) {

            using (MemoryStream stream = new MemoryStream()) {
                icon.Save(stream);
                stream.Seek(0, SeekOrigin.Begin);
                return BitmapFrame.Create(stream);
            }
        }

    }
}

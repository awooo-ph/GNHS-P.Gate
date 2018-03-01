using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace GNHSP.Gate.Converters {
    class PathToBitmapConverter : ConverterBase {

        public PathToBitmapConverter()
        {
     
        }
        
        private static Dictionary<string, BitmapImage> Cache = new Dictionary<string, BitmapImage>();

        protected override object Convert(object value, Type targetType, object parameter)
        {
            if (value == null) return Binding.DoNothing;
             
            if (!File.Exists((string) value))
                return null;
            
                try
                {
                    var bmp = new BitmapImage();
                    
                        using (var stream = File.OpenRead((string) value))
                        {
                            bmp.BeginInit();
                            bmp.CacheOption = BitmapCacheOption.OnLoad;
                            bmp.StreamSource = stream;
                            //bmp.UriSource = File.Exists((string)value) ? new Uri($"file://{(string)value}") : defUri;
                            bmp.EndInit();
                            bmp.Freeze();
                        }
                        return bmp;
                    
                }
                catch 
                {
                        return null;
                }
                
             
            
           
        }

        
    }
}

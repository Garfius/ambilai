using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.IO.Ports;
using System.Runtime.InteropServices;
using System.Threading;
using System.Xml;
using System.Xml.Serialization;

namespace screenToArduino
{
    public class ScreenCapture
    {
        static string arxiuConfig = "serialConfig.xml";

        static void Main(string[] args)
        {
            arduinoConfig configActual = new arduinoConfig();
            aplicacio treballador = new aplicacio();
            
            try {

                carregaObjecteDesdeXML(ref configActual,Path.Combine(AppDomain.CurrentDomain.BaseDirectory, arxiuConfig));

                treballador = new aplicacio(configActual);
                
            } catch {
                if (System.IO.File.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, arxiuConfig)))
                {
                    Console.Out.WriteLine("la teva config no funciona, borra arxiu XML per crear de nou");
                    return;
                }
                configActual.port = "COM3";
                configActual.speed = 57600;
                configActual.rtsEnable = true;
                configActual.databits = 8;
                configActual.parity = (int)Parity.None;
                configActual.StopBits = (int)StopBits.One;
                configActual.refreshRateMs = 300;
                configActual.border = 4;
                configActual.resizeX = 45;
                configActual.resizeY = 50;
                configActual.waitFade = 1;
                desaObjecteEnXML(configActual, Path.Combine(AppDomain.CurrentDomain.BaseDirectory, arxiuConfig));
                Console.Out.WriteLine("Error recuperant configuració, desada configuració per defecte en: "+ arxiuConfig +" Ja el pots editar");
                Console.Out.WriteLine("Valors possibles: ");
                Console.Out.WriteLine("Speed: 300-115200 rtsEnable: true||false DataBits 7-8 Parity: 0-4 StopBits: 0-3 Port: COMX <-- X és el número de port");
                Console.Out.WriteLine("Polsa qualsevol tecla per acabar");
                Console.ReadKey();
            }

            long diferencia, ultimTemps = milliseconds();
            System.Diagnostics.Process.GetCurrentProcess().PriorityClass = System.Diagnostics.ProcessPriorityClass.RealTime;
            try {
                
                while (true)
                {
                    
                    treballador.enviaHexSerial((byte)configActual.waitFade);

                    diferencia = ((ultimTemps+ configActual.refreshRateMs)- milliseconds());
                    if (diferencia < 0)
                    {
                        Console.Out.WriteLine("%Massa rapid" + diferencia);
                    }
                    else {
                        Thread.Sleep((int)diferencia);
                    }
                    ultimTemps = milliseconds();

                }
            } catch (Exception ex){
                Console.Out.WriteLine(ex.Message);
                Console.Out.WriteLine("ApaSiau");
                Console.ReadKey();
            }
            

        }
        public static long milliseconds() { return DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond; }  
        static private void carregaObjecteDesdeXML(ref arduinoConfig objectOut, string fileName)
        {
            string attributeXml = string.Empty;

            XmlDocument xmlDocument = new XmlDocument();
            xmlDocument.Load(fileName);
            string xmlString = xmlDocument.OuterXml;

            using (StringReader read = new StringReader(xmlString))
            {
                Type outType = typeof(arduinoConfig);

                XmlSerializer serializer = new XmlSerializer(outType);
                using (XmlReader reader = new XmlTextReader(read))
                {
                    objectOut = (arduinoConfig)serializer.Deserialize(reader);
                    reader.Close();
                }

                read.Close();
            }
            
        }
        static private void desaObjecteEnXML(arduinoConfig serializableObject, string fileName)
        {

            if (File.Exists(fileName))
            {
                File.Delete(fileName);
            }

            XmlDocument xmlDocument = new XmlDocument();
            XmlSerializer serializer = new XmlSerializer(serializableObject.GetType());
            using (MemoryStream stream = new MemoryStream())
            {
                serializer.Serialize(stream, serializableObject);
                stream.Position = 0;
                xmlDocument.Load(stream);
                xmlDocument.Save(fileName);
                stream.Close();
            }
            
        }
    }
    public struct arduinoConfig
    {
        public string port;
        public int speed;
        public bool rtsEnable;
        public int databits;
        public int parity;
        public int StopBits;
        public int refreshRateMs;
        public int border;
        public int resizeX;
        public int resizeY;
        public int waitFade;
    }
    public class aplicacio
    {
        arduinoConfig parametres;
        SerialPort serialPort;
        public aplicacio() {
        }
        public aplicacio(arduinoConfig parametres) {
            
            this.parametres = parametres;
            serialPort = new SerialPort(parametres.port, parametres.speed, (Parity)parametres.parity, parametres.databits, (StopBits)parametres.StopBits);
            serialPort.DataReceived += new SerialDataReceivedEventHandler(rebDadesSerie);
            serialPort.RtsEnable = parametres.rtsEnable;
            if (!serialPort.IsOpen) serialPort.Open();
            serialPort.DiscardInBuffer();
            serialPort.DiscardOutBuffer();
        }
        private void rebDadesSerie(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                while (serialPort.BytesToRead > 0)
                {
                    Console.Out.WriteLine(serialPort.ReadLine());
                }
            }
            catch
            {
                Console.Out.WriteLine("Serial Error");
            }
            
        }
        public void enviaHexSerial(byte esperaExtra) {
            serialPort.Write("0x" + colorsMitjans() + esperaExtra.ToString("X2") + '\n');// gestió serie amb arduino
        }
        public string colorsMitjans()
        {
            colorRGB resultat;

            Image img = CaptureScreen();
            
            img = resizeImage(parametres.resizeX, parametres.resizeY, img);

            resultat.r = 0;
            resultat.g = 0;
            resultat.b = 0;

            //img.Save(@"c:\users\garf\a.png", ImageFormat.Png);// per testeig

            resultat = mitjanaImatge(img, parametres.border);

            return (((byte)resultat.r).ToString("X2") + ((byte)resultat.g).ToString("X2") + ((byte)resultat.b).ToString("X2"));

        }
        public struct colorRGB
        {
            public ulong r; public ulong g; public ulong b;
        }
        public colorRGB mitjanaImatge(Image img, int marge)
        {
            Bitmap bmp = new Bitmap(img);
            
            colorRGB resultat;
            resultat.r = 0;
            resultat.g = 0;
            resultat.b = 0;

            for (int x = (1 + marge); x < (img.Width - marge); x++)
            {
                for (int y = (1 + marge); y < (img.Height - marge); y++)
                {
                    Color clr = bmp.GetPixel(x, y);
                    
                    resultat.r += clr.R;
                    resultat.g += clr.G;
                    resultat.b += clr.B;
                }

            }

            resultat.r = resultat.r / (ulong)(bmp.Width * bmp.Height);
            resultat.g = resultat.g / (ulong)(bmp.Width * bmp.Height);
            resultat.b = resultat.b / (ulong)(bmp.Width * bmp.Height);

            return resultat;


        }
        /// <summary>
        /// Redimensiona uma imatge, tret de https://stackoverflow.com/questions/1922040/how-to-resize-an-image-c-sharp per Vinzz
        /// https://stackoverflow.com/questions/19550114/c-sharp-quickest-way-to-get-average-colors-of-screen
        /// TO-DO  millorar la eficiencia usant el web aquest
        /// </summary>
        /// <param name="newWidth"></param>
        /// <param name="newHeight"></param>
        /// <param name="imgPhoto"></param>
        /// <returns></returns>
        public Image resizeImage(int newWidth, int newHeight, Image imgPhoto)
        {
            int sourceWidth = imgPhoto.Width;
            int sourceHeight = imgPhoto.Height;

            //Consider vertical pics
            if (sourceWidth < sourceHeight)
            {
                int buff = newWidth;

                newWidth = newHeight;
                newHeight = buff;
            }

            int sourceX = 0, sourceY = 0, destX = 0, destY = 0;
            float nPercent = 0, nPercentW = 0, nPercentH = 0;

            nPercentW = ((float)newWidth / (float)sourceWidth);
            nPercentH = ((float)newHeight / (float)sourceHeight);
            if (nPercentH < nPercentW)
            {
                nPercent = nPercentH;
                destX = System.Convert.ToInt16((newWidth -
                          (sourceWidth * nPercent)) / 2);
            }
            else
            {
                nPercent = nPercentW;
                destY = System.Convert.ToInt16((newHeight -
                          (sourceHeight * nPercent)) / 2);
            }

            int destWidth = (int)(sourceWidth * nPercent);
            int destHeight = (int)(sourceHeight * nPercent);


            Bitmap bmPhoto = new Bitmap(newWidth, newHeight,
                          PixelFormat.Format24bppRgb);

            bmPhoto.SetResolution(imgPhoto.HorizontalResolution,
                         imgPhoto.VerticalResolution);

            Graphics grPhoto = Graphics.FromImage(bmPhoto);
            grPhoto.Clear(Color.Black);
            grPhoto.InterpolationMode =
                System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;

            grPhoto.DrawImage(imgPhoto,
                new Rectangle(destX, destY, destWidth, destHeight),
                new Rectangle(sourceX, sourceY, sourceWidth, sourceHeight),
                GraphicsUnit.Pixel);

            grPhoto.Dispose();
            imgPhoto.Dispose();
            return bmPhoto;
        }
        /// <summary>
        /// https://stackoverflow.com/questions/19550114/c-sharp-quickest-way-to-get-average-colors-of-screen
        /// TO-DO  millorar la eficiencia usant el web aquest
        /// </summary>
        /// <returns></returns>
        public Image CaptureScreen()
        {
            return CaptureWindow(User32.GetDesktopWindow());
        }
        /// <summary>
        /// Creates an Image object containing a screen shot of a specific window
        /// </summary>
        /// <param name="handle">The handle to the window. (In windows forms, this is obtained by the Handle property)</param>
        /// <returns></returns>
        public Image CaptureWindow(IntPtr handle)
        {
            // get te hDC of the target window
            IntPtr hdcSrc = User32.GetWindowDC(handle);
            // get the size
            User32.RECT windowRect = new User32.RECT();
            User32.GetWindowRect(handle, ref windowRect);
            int width = windowRect.right - windowRect.left;
            int height = windowRect.bottom - windowRect.top;
            // create a device context we can copy to
            IntPtr hdcDest = GDI32.CreateCompatibleDC(hdcSrc);
            // create a bitmap we can copy it to,
            // using GetDeviceCaps to get the width/height
            IntPtr hBitmap = GDI32.CreateCompatibleBitmap(hdcSrc, width, height);
            // select the bitmap object
            IntPtr hOld = GDI32.SelectObject(hdcDest, hBitmap);
            // bitblt over
            GDI32.BitBlt(hdcDest, 0, 0, width, height, hdcSrc, 0, 0, GDI32.SRCCOPY);
            // restore selection
            GDI32.SelectObject(hdcDest, hOld);
            // clean up 
            GDI32.DeleteDC(hdcDest);
            User32.ReleaseDC(handle, hdcSrc);
            // get a .NET image object for it
            Image img = Image.FromHbitmap(hBitmap);
            // free up the Bitmap object
            GDI32.DeleteObject(hBitmap);
            return img;
        }
        /// <summary>
        /// Captures a screen shot of a specific window, and saves it to a file
        /// </summary>
        /// <param name="handle"></param>
        /// <param name="filename"></param>
        /// <param name="format"></param>
        public void CaptureWindowToFile(IntPtr handle, string filename, ImageFormat format)
        {
            Image img = CaptureWindow(handle);
            img.Save(filename, format);
        }
        /// <summary>
        /// Captures a screen shot of the entire desktop, and saves it to a file
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="format"></param>
        public void CaptureScreenToFile(string filename, ImageFormat format)
        {
            Image img = CaptureScreen();
            img.Save(filename, format);
        }

        /// <summary>
        /// Helper class containing Gdi32 API functions
        /// </summary>
        private class GDI32
        {

            public const int SRCCOPY = 0x00CC0020; // BitBlt dwRop parameter
            [DllImport("gdi32.dll")]
            public static extern bool BitBlt(IntPtr hObject, int nXDest, int nYDest,
                int nWidth, int nHeight, IntPtr hObjectSource,
                int nXSrc, int nYSrc, int dwRop);
            [DllImport("gdi32.dll")]
            public static extern IntPtr CreateCompatibleBitmap(IntPtr hDC, int nWidth,
                int nHeight);
            [DllImport("gdi32.dll")]
            public static extern IntPtr CreateCompatibleDC(IntPtr hDC);
            [DllImport("gdi32.dll")]
            public static extern bool DeleteDC(IntPtr hDC);
            [DllImport("gdi32.dll")]
            public static extern bool DeleteObject(IntPtr hObject);
            [DllImport("gdi32.dll")]
            public static extern IntPtr SelectObject(IntPtr hDC, IntPtr hObject);
        }

        /// <summary>
        /// Helper class containing User32 API functions
        /// </summary>
        private class User32
        {
            [StructLayout(LayoutKind.Sequential)]
            public struct RECT
            {
                public int left;
                public int top;
                public int right;
                public int bottom;
            }
            [DllImport("user32.dll")]
            public static extern IntPtr GetDesktopWindow();
            [DllImport("user32.dll")]
            public static extern IntPtr GetWindowDC(IntPtr hWnd);
            [DllImport("user32.dll")]
            public static extern IntPtr ReleaseDC(IntPtr hWnd, IntPtr hDC);
            [DllImport("user32.dll")]
            public static extern IntPtr GetWindowRect(IntPtr hWnd, ref RECT rect);
        }
    }
}



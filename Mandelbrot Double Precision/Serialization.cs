using System;
using System.Drawing;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

namespace Mandelbrot_Double_Precision {
    static class Serialization {

        private static readonly string dir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\Plexico\\Mandelbrot Renderer Double Precision\\";
        private const string settingsdir = "Settings\\";
        private const string settingsfile = "settings.plex";
        private const string screenshotsdir = "Screenshots\\";

        static Serialization() {
            string newdir = "E" + dir.Substring(1);
            try {
                Directory.CreateDirectory(newdir);
            } catch (Exception) { return; }
            dir = newdir;
        }

        [Serializable]
        public class FractalPair {
            public Fractal mandelbrot, julia;
            public Fractal.FractalType activeFractal;
            public Program.FractalsToRender fractalsToRender;
            public int screenshotCount;
            public FractalPair(Fractal mandelbrot, Fractal julia, Fractal.FractalType activeFractal, Program.FractalsToRender fractalsToRender, int screenshotCount) {
                this.mandelbrot = mandelbrot;
                this.julia = julia;
                this.activeFractal = activeFractal;
                this.fractalsToRender = fractalsToRender;
                this.screenshotCount = screenshotCount;
            }
        }
        public static FractalPair Load() {
            using (BufferedStream stream = new BufferedStream(new FileStream(dir + settingsfile, FileMode.Open, FileAccess.Read, FileShare.None))) {
                IFormatter formatter = new BinaryFormatter();
                object data = formatter.Deserialize(stream);
                return (FractalPair)data;
            }
        }

        public static void Save(Fractal mandelbrot, Fractal julia, Fractal.FractalType fractaltype, Program.FractalsToRender fractalsToRender, int screenshotCount) {
            FractalPair pair = new FractalPair(mandelbrot, julia, fractaltype, fractalsToRender, screenshotCount);
            if (!Directory.Exists(dir)) {
                Directory.CreateDirectory(dir);
            }
            using (BufferedStream stream = new BufferedStream(new FileStream(dir + settingsfile, FileMode.Create, FileAccess.Write, FileShare.None))) {
                IFormatter formatter = new BinaryFormatter();
                formatter.Serialize(stream, pair);
            }
        }


        public static void SaveScreenshot(string interndir, Bitmap bmp) {
            Directory.CreateDirectory(dir + screenshotsdir + interndir + "\\");

            string no = String.Format("{0:000000000000}", Program.ScreenshotCount);
            string file = dir + screenshotsdir + interndir + "\\" + no + ".png";
            bmp.Save(file, System.Drawing.Imaging.ImageFormat.Png);
        }
    }
}

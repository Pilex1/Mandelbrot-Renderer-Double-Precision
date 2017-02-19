using Pencil.Gaming;
using Pencil.Gaming.Graphics;
using Pencil.Gaming.MathUtils;
using System;
using System.Drawing;
using System.Threading;

namespace Mandelbrot_Double_Precision {

    static class Program {

        public enum FractalsToRender {
            Mandelbrot, Julia, MandelbrotJulia
        }

        private static bool view = true;

        // public static int width = 640, height = 360;
        public static int width = 1280, height = 720;
        static GlfwWindowPtr window;
        internal static ShaderProgram shader;
        internal static ShaderProgram guishader;

        static Fractal mandelbrot, julia;

        static Fractal.FractalType activeFractal;
        static CooldownTimer activeFractalTimer;

        static FractalsToRender fractalsToRender;
        static CooldownTimer fractalsToRenderTimer;

        static CooldownTimer screenshotTimer;

        public static int ScreenshotCount;

        private static bool shouldClose;

        private static Thread consoleThread;

        private static DateTime TimeStart;

        static void Main(string[] args) {
            TimeStart = DateTime.Now;
            Console.WriteLine("Realtime render / Image sequence render : y / n");
            while (true) {
                string input = Console.ReadLine();
                if (input == "y") {
                    view = true;
                    break;
                } else if (input == "n") {
                    view = false;
                    break;
                }
            }
            view = true;

            if (!Glfw.Init())
                return;

            window = Glfw.CreateWindow(width, height, "GLFW OpenGL", GlfwMonitorPtr.Null, GlfwWindowPtr.Null);
            Glfw.MakeContextCurrent(window);
            Glfw.SetErrorCallback(OnError);
            Input.Init(window);
            if (!view) {
                Glfw.HideWindow(window);
            }

            Init();
            Update();

            consoleThread = new Thread(new ThreadStart(delegate () {
                while (true) {
                    string input = Console.ReadLine();
                    if (input == "s") {
                        SetShouldClose(true);
                    }
                }
            }));
            consoleThread.Start();

            while (!Glfw.WindowShouldClose(window))
                MainLoop();

            Glfw.DestroyWindow(window);
            Glfw.Terminate();

            consoleThread.Abort();
            CleanUp();
        }

        private static void CleanUp() {
            if (view)
                Serialization.Save(mandelbrot, julia, activeFractal, fractalsToRender, ScreenshotCount);
        }

        private static void OnError(GlfwError code, string desc) {
            throw new ArgumentException("OpenGL Error: " + code + " - " + desc);
        }

        private static void Init() {
            shader = new ShaderProgram("Assets/Shaders/Shader.vert", "Assets/Shaders/Shader.frag");

            shader.AddUniform("vposoffset");
            shader.AddUniform("vsize");

            shader.AddUniform("aspectRatio");
            shader.AddUniform("rot");
            shader.AddUniform("maxIter");
            shader.AddUniform("clrRatio");
            shader.AddUniform("cursorClr");
            shader.AddUniform("julia_mode");
            shader.AddUniform("crosshair");

            shader.AddUniform("fractalType");
            shader.AddUniform("julia_c");
            shader.AddUniform("pos");
            shader.AddUniform("zoom");

            //guishader = new ShaderProgram("Asset/Shaders/Gui.vert", "Asset/Shaders/Gui.frag");
            //guishader.AddUniform("pos");
            //guishader.AddUniform("size");

            try {
                Serialization.FractalPair pair = Serialization.Load();
                mandelbrot = pair.mandelbrot;
                julia = pair.julia;
                activeFractal = pair.activeFractal;
                fractalsToRender = pair.fractalsToRender;
                //  ScreenshotCount = pair.screenshotCount;
                ScreenshotCount = 0;
            } catch (Exception) {
                mandelbrot = Fractal.CreateMandelbrot();
                julia = Fractal.CreateJulia();
            }
            mandelbrot.Load();
            julia.Load();

            activeFractalTimer = new CooldownTimer(60);
            activeFractalTimer.SetTime(activeFractalTimer.GetCooldown());
            screenshotTimer = new CooldownTimer(20);
            fractalsToRenderTimer = new CooldownTimer(20);

            if (!view) {
                mandelbrot.pos = new Vector2d(-0.129078142625295, 0.98765122402576);
                mandelbrot.maxIter = 2000;
                fractalsToRender = FractalsToRender.Mandelbrot;
                mandelbrot.crosshair = false;
                mandelbrot.zoom = 4;
            } else {
                //Random rand = new Random();
                //mandelbrot.pos = new Vector2d(rand.NextDouble() * 2 - 1, rand.NextDouble() * 2 - 1);
                //mandelbrot.zoom = 2;
            }

            //    mandelbrot.zoom = 1E-10;
        }


        private static void MainLoop() {

            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit | ClearBufferMask.StencilBufferBit);

            GameTime.Update();
            Glfw.SetWindowTitle(window, "FPS: " + GameTime.FPS);

            CooldownTimer.Update();
            Render(mandelbrot);
            Render(julia);

            Update();
            if (!view) {
                Animate();
                SaveScreenshot();
                Console.WriteLine("Pos: " + mandelbrot.pos + " Zoom: " + mandelbrot.zoom);
            }

            Glfw.PollEvents();

            Glfw.SwapBuffers(window);

            if (shouldClose) {
                Glfw.SetWindowShouldClose(window, shouldClose);
            }

        }

        private static void Animate() {
            mandelbrot.zoom *= 0.95;
            if (mandelbrot.zoom < 1E-13)
                Glfw.SetWindowShouldClose(window, true);
        }

        private static void Update() {
            if (Input.Keys(Key.Tab)) {
                if (activeFractalTimer.Ready()) {
                    activeFractal = activeFractal == Fractal.FractalType.Mandelbrot ? Fractal.FractalType.Julia : Fractal.FractalType.Mandelbrot;
                    activeFractalTimer.Reset();
                }
            }
            if (Input.Keys(Key.B)) {
                if (fractalsToRenderTimer.Ready()) {
                    int f = (int)(fractalsToRender) + 1;
                    f %= 3;
                    fractalsToRender = (FractalsToRender)f;
                    fractalsToRenderTimer.Reset();
                }
            }

            if (activeFractal == Fractal.FractalType.Mandelbrot) {
                HandleKeys(mandelbrot);
            } else {
                HandleKeys(julia);
            }

            float clr_amt = GameTime.DeltaTime / 1000 * (Input.Keys(Key.LeftShift) ? 1 : -1);
            if (Input.Keys(Key.Z)) {
                mandelbrot.clrRatio.R += clr_amt;
                julia.clrRatio.R += clr_amt;
            }
            if (Input.Keys(Key.X)) {
                mandelbrot.clrRatio.G += clr_amt;
                julia.clrRatio.G += clr_amt;
            }
            if (Input.Keys(Key.C)) {
                mandelbrot.clrRatio.B += clr_amt;
                julia.clrRatio.B += clr_amt;
            }

            if (Input.Keys(Key.J)) {
                mandelbrot.rot -= GameTime.DeltaTime / 200;
                julia.rot -= GameTime.DeltaTime / 200;
            }
            if (Input.Keys(Key.L)) {
                mandelbrot.rot += GameTime.DeltaTime / 200;
                julia.rot += GameTime.DeltaTime / 200;
            }

            if (Input.Keys(Key.One)) {
                julia.SetMode(Fractal.Mode.Normal);
            }
            if (Input.Keys(Key.Two)) {
                julia.SetMode(Fractal.Mode.Reciprocal);
            }
            if (Input.Keys(Key.Three)) {
                julia.SetMode(Fractal.Mode.SquaredReciprocal);
            }
            if (Input.Keys(Key.Four)) {
                julia.SetMode(Fractal.Mode.t1);
            }
            if (Input.Keys(Key.Five)) {
                julia.SetMode(Fractal.Mode.t2);
            }
            if (Input.Keys(Key.Six)) {
                julia.SetMode(Fractal.Mode.t3);
            }

            if (Input.Keys(Key.Escape)) {
                Glfw.SetWindowShouldClose(window, true);
            }

            if (Input.Keys(Key.F1)) {
                if (screenshotTimer.Ready()) {
                    screenshotTimer.Reset();
                    SaveScreenshot();
                }
            }



            switch (fractalsToRender) {
                case FractalsToRender.Mandelbrot:
                    mandelbrot.quad.pos = Vector2.Zero;
                    mandelbrot.quad.model.size = new Vector2(1, 1);
                    julia.quad.model.size = Vector2.Zero;
                    activeFractal = Fractal.FractalType.Mandelbrot;
                    break;
                case FractalsToRender.Julia:
                    julia.quad.pos = Vector2.Zero;
                    julia.quad.model.size = new Vector2(1, 1);
                    mandelbrot.quad.model.size = Vector2.Zero;
                    activeFractal = Fractal.FractalType.Julia;
                    break;
                case FractalsToRender.MandelbrotJulia:
                    mandelbrot.quad.pos = new Vector2(-0.5f, 0f);
                    mandelbrot.quad.model.size = new Vector2(0.5f, 1);

                    julia.quad.pos = new Vector2(0.5f, 0f);
                    julia.quad.model.size = new Vector2(0.5f, 1f);
                    break;
            }
        }

        private static void HandleKeys(Fractal fractal) {
            double x = GameTime.DeltaTime / 60 * fractal.zoom * Math.Sin(fractal.rot) * (Input.Keys(Key.LeftShift) ? 0.05 : 1) * (Input.Keys(Key.LeftControl) ? 0.05 : 1) * (Input.Keys(Key.LeftAlt) ? 0.05 : 1);
            double y = GameTime.DeltaTime / 60 * fractal.zoom * Math.Cos(fractal.rot) * (Input.Keys(Key.LeftShift) ? 0.05 : 1) * (Input.Keys(Key.LeftControl) ? 0.05 : 1) * (Input.Keys(Key.LeftAlt) ? 0.05 : 1);
            if (Input.Keys(Key.W)) {
                fractal.pos.x += x;
                fractal.pos.y += y;
            }
            if (Input.Keys(Key.S)) {
                fractal.pos.x -= x;
                fractal.pos.y -= y;
            }
            if (Input.Keys(Key.D)) {
                fractal.pos.x += y;
                fractal.pos.y -= x;
            }
            if (Input.Keys(Key.A)) {
                fractal.pos.x -= y;
                fractal.pos.y += x;
            }

            if (Input.Keys(Key.I)) {
                fractal.zoom *= (float)Math.Pow(0.99, GameTime.DeltaTime);
            }
            if (Input.Keys(Key.K)) {
                fractal.zoom /= (float)Math.Pow(0.99, GameTime.DeltaTime);
            }

            if (Input.Keys(Key.Y)) fractal.maxIter /= (float)Math.Pow(0.99, GameTime.DeltaTime);
            if (Input.Keys(Key.H)) fractal.maxIter *= (float)Math.Pow(0.99, GameTime.DeltaTime);
            if (fractal.maxIter < 1) fractal.maxIter = 1;

            if (Input.Keys(Key.T)) {
                fractal.ToggleCrosshair();
            }

            if (Input.Keys(Key.R)) {
                fractal.Reset();
            }
        }

        private static void Render(Fractal fractal) {
            Model model = fractal.quad.model;
            GL.UseProgram(shader.id);
            GL.BindVertexArray(model.vao.ID);

            shader.SetUniform2f("vposoffset", fractal.quad.pos);
            shader.SetUniform2f("vsize", fractal.quad.model.size);

            shader.SetUniform1f("aspectRatio", (float)width / height);
            shader.SetUniform4m("rot", Matrix.CreateRotationZ(fractal.rot));
            shader.SetUniform1i("maxIter", (int)fractal.maxIter);
            shader.SetUniform4c("clrRatio", fractal.clrRatio);
            if (activeFractal == fractal.fractalType) {
                float factor = activeFractalTimer.GetTime() / activeFractalTimer.GetCooldown();
                if (factor > 1) factor = 1;
                shader.SetUniform3f("cursorClr", new Vector3(factor, factor, factor));
            } else {
                shader.SetUniform3f("cursorClr", new Vector3(1f, 1f, 1f));
            }
            shader.SetUniform1b("crosshair", fractal.crosshair);
            shader.SetUniform1i("julia_mode", (int)fractal.mode);

            shader.SetUniform1i("fractalType", (int)fractal.fractalType);
            if (fractal.fractalType == Fractal.FractalType.Mandelbrot) {
                shader.SetUniform2d("julia_c", fractal.pos);
            }
            shader.SetUniform2d("pos", fractal.pos);
            shader.SetUniform1d("zoom", fractal.zoom);

            GL.DrawElements(model.drawmode, model.vao.count, DrawElementsType.UnsignedInt, 0);
            GL.BindVertexArray(0);
            GL.UseProgram(0);
        }

        private static void SaveScreenshot() {
            string dir = "";
            dir = TimeStart.ToShortDateString() + "_" + TimeStart.ToShortTimeString();
            dir = "_" + dir.Replace(' ', '_');
            dir = dir.Replace(':', '_');
            dir = dir.Replace('/', '_');
            GL.PixelStore(PixelStoreParameter.UnpackAlignment, 1);
            byte[] pixels = new byte[3 * width * height];
            GL.ReadPixels(0, 0, width, height, PixelFormat.Rgb, PixelType.UnsignedByte, pixels);
            Bitmap bitmap = new Bitmap(width, height);
            for (int i = 0; i < width; i++) {
                for (int j = 0; j < height; j++) {
                    byte r = pixels[3 * (j * width + i)];
                    byte g = pixels[3 * (j * width + i) + 1];
                    byte b = pixels[3 * (j * width + i) + 2];
                    bitmap.SetPixel(i, j, Color.FromArgb(r, g, b));
                }
            }
            Serialization.SaveScreenshot(dir, bitmap);
            bitmap.Dispose();
            ScreenshotCount++;
        }

        private static void SetShouldClose(bool b) {
            shouldClose = b;
        }
    }
}

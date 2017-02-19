using Pencil.Gaming.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mandelbrot_Double_Precision {
    class FBO {

        public int id { get; private set; }

        public FBO() {
            id = GL.GenFramebuffer();
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, id);

            int texture = 0;
            texture = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, texture);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgb, Program.width, Program.height, 0, PixelFormat.Rgb, PixelType.UnsignedByte, IntPtr.Zero);
            int param;
            param = (int)TextureMinFilter.Linear;
            GL.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, ref param);
            param = (int)TextureMagFilter.Linear;
            GL.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, ref param);
            GL.BindTexture(TextureTarget.Texture2D, 0);

            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, texture, 0);
            //stuff

            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        }

        public void Dispose() {
            GL.DeleteFramebuffer(id);
        }
    }
}

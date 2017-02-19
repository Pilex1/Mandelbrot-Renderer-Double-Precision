using Pencil.Gaming.MathUtils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mandelbrot_Double_Precision {
    class Entity {
        public Model model { get; private set; }
        public Vector2 pos;

        public Entity(Model model, Vector2 pos) {
            this.model = model;
            this.pos = pos;
        }

    }
}

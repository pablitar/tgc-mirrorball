using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TgcViewer.Utils.TgcSceneLoader;
using Microsoft.DirectX;
using TgcViewer.Utils.TgcGeometry;

namespace AlumnoEjemplos.MiGrupo
{
    class NoMover : Movimiento
    {
        public void mover(LightData luz, TgcMesh meshLuz, float elapsedTime)
        {
            meshLuz.render();
        }
    }
}

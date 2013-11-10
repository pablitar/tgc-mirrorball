using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TgcViewer.Utils.TgcSceneLoader;
using Microsoft.DirectX;
using TgcViewer.Utils.TgcGeometry;

namespace AlumnoEjemplos.MiGrupo
{
    class RotarEjeX : Movimiento
    {
        public void mover(LightData luz, TgcMesh meshLuz, float elapsedTime)
        {
            float rot = FastMath.QUARTER_PI * elapsedTime;
            Matrix directionRotationMatrix = Matrix.RotationX(rot);
            luz.pos.TransformCoordinate(directionRotationMatrix);
            meshLuz.rotateX(rot);
        }
    }
}

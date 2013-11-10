using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TgcViewer.Utils.TgcSceneLoader;
using Microsoft.DirectX;
using TgcViewer.Utils.TgcGeometry;

namespace AlumnoEjemplos.MiGrupo
{
    class RotarEjeY : Movimiento
    {
        public void mover(LightData luz, TgcMesh meshLuz, float elapsedTime)
        {
            Matrix directionRotationMatrix = Matrix.RotationY(FastMath.QUARTER_PI * elapsedTime);
            luz.direccion.TransformCoordinate(directionRotationMatrix);
            meshLuz.rotateY(FastMath.QUARTER_PI * elapsedTime);          
        }
    }
}

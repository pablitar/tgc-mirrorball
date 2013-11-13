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
            Matrix directionRotationMatrix = Matrix.RotationX(FastMath.QUARTER_PI * elapsedTime);
            luz.direccion.TransformCoordinate(directionRotationMatrix);

            Matrix trans = Matrix.Translation(meshLuz.Position);
            Matrix trasp = Matrix.Invert(trans);

            meshLuz.Transform = Matrix.Multiply(trasp, directionRotationMatrix) * trans * meshLuz.Transform;

            meshLuz.render();
        }
    }
}

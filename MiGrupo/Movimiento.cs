using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TgcViewer.Utils.TgcSceneLoader;

namespace AlumnoEjemplos.MiGrupo
{
    interface Movimiento
    {

        void mover(LightData luz, TgcMesh meshLuz, float elapsedTime);
    }
}

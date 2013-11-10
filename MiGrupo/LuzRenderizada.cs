using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TgcViewer.Utils.TgcSceneLoader;

namespace AlumnoEjemplos.MiGrupo
{
    class LuzRenderizada
    {
        public TgcMesh mesh;
        public LightData luz;
        public Movimiento movimiento;

        public LuzRenderizada(TgcMesh me, LightData l , Movimiento m)
        {
            this.mesh = me;
            this.luz = l;
            this.movimiento = m;
        }

        public void mover(float elapsedTime)
        {
            movimiento.mover(this.luz, this.mesh, elapsedTime);
        }
    }
}

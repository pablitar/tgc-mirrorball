using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text;
using TgcViewer.Utils.TgcSceneLoader;

namespace AlumnoEjemplos.MiGrupo
{
   
        /// <summary>
        /// Estructura auxiliar para guardar un mesh y sus tres luces mas cercanas
        /// </summary>
        public class MeshLightData
        {
            public TgcMesh mesh;
            public List<LightData> lights;

            public MeshLightData() { }
        }
}

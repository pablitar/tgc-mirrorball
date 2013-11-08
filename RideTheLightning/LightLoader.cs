using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TgcViewer.Utils.TgcSceneLoader;

namespace AlumnoEjemplos.RideTheLightning
{
    class LightLoader
    {

        private static Dictionary<string, LightLoader> loaders = new Dictionary<string, LightLoader>
        {

        };

        public static bool isLight(TgcMeshData data)
        {
            return loaders.ContainsKey(data.layerName);
        }

        public static RTLLight loadLight(TgcMeshData data)
        {
            return new RTLLight(); 
        }
    }
}

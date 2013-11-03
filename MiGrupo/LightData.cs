using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TgcViewer.Example;
using TgcViewer;
using System.Drawing;
using TgcViewer.Utils.TgcGeometry;
using Microsoft.DirectX.Direct3D;
using Microsoft.DirectX;

namespace AlumnoEjemplos.MiGrupo
{
    public class LightData
    {
        public Vector3 pos;
        public TgcBoundingBox aabb;
        public Color color;
        public Boolean spot;
        public Vector3 direccion;
    }
}

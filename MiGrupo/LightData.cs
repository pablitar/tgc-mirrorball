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
using TgcViewer.Utils.TgcSceneLoader;
using System.Globalization;

namespace AlumnoEjemplos.MiGrupo
{
    public class LightData
    {
        public Vector3 pos;
        public TgcBoundingBox aabb;
        public Color color;
        public Boolean spot;
        public Vector3 direccion;
        public float intencidad;
        public float atenuacion;
        public float angleCos;
        public float exp;
        
            public LightData(TgcMeshData meshData){

                    this.color = parserColor(meshData.userProperties["color"]);
                    this.aabb = new TgcBoundingBox(TgcParserUtils.float3ArrayToVector3(meshData.pMin), TgcParserUtils.float3ArrayToVector3(meshData.pMax));
                    this.pos = this.aabb.calculateBoxCenter();
                    this.spot = meshData.userProperties["esSpot"].Equals("SI");
                    this.direccion = convertirDireccion(meshData.userProperties["dir"]);
                    this.intencidad = float.Parse(meshData.userProperties["inten"]);
                    this.atenuacion = float.Parse(meshData.userProperties["atenua"])/10;
                    this.angleCos = float.Parse(meshData.userProperties["angleCos"]);
                    this.exp = float.Parse(meshData.userProperties["exp"]);
            }

            public Color parserColor(String colores)
            {
                String[] vector = colores.Split(',');
                return Color.FromArgb(int.Parse(vector[0]), int.Parse(vector[1]), int.Parse(vector[2]));
            }

         public Vector3 convertirDireccion(String dir)
        {
            Vector3 result = new Vector3();
            String[] vector = dir.Split(',');
            result.X = float.Parse(vector[0]);
            result.Y = float.Parse(vector[1]);
            result.Z = float.Parse(vector[2]);
            return Vector3.Normalize(result);
        }
    }
}

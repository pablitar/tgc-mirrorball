using System;
using System.Collections.Generic;
using System.Text;
using TgcViewer.Example;
using TgcViewer;
using Microsoft.DirectX.Direct3D;
using Microsoft.DirectX;
using TgcViewer.Utils.TgcSceneLoader;
using System.Drawing;
using TgcViewer.Utils.TgcGeometry;
using System.IO;
using TgcViewer.Utils.Shaders;
using TgcViewer.Utils.Interpolation;

namespace AlumnoEjemplos.MiGrupo
{
    class ParseadorDeEscena
    {

        String zona1 = "";
        String zona2 = "";
        String zona3 = "";
        String constLuz = "light";
        String shaderUrl = GuiController.Instance.AlumnoEjemplosMediaDir + "Shaders\\shaderIntegrador.fx";

        TgcScene scene;
        Effect effect;

        List<MeshLightData> meshesZona1;
        List<LightData> lucesZona1 = new List<LightData>();
        List<MeshLightData> meshesZona2;
        List<LightData> lucesZona2 = new List<LightData>();
        List<MeshLightData> meshesZona3;
        List<LightData> lucesZona3 = new List<LightData>();


        public void cargarEscena(String zona1, String zona2, String zona3){

            this.zona1 = zona1;
            this.zona2 = zona2;
            this.zona3 = zona3;

            //Cargar escenario, pero inicialmente solo hacemos el parser, para separar los objetos que son solo luces y no meshes
            string scenePath = GuiController.Instance.AlumnoEjemplosDir + "AlumnoMedia\\RideTheLightning\\Scenes\\Deposito\\Deposito-TgcScene.xml";
            string mediaPath = GuiController.Instance.AlumnoEjemplosDir + "AlumnoMedia\\RideTheLightning\\Scenes\\Deposito\\";
            TgcSceneParser parser = new TgcSceneParser();
            TgcSceneData sceneData = parser.parseSceneFromString(File.ReadAllText(scenePath));

            //Separar modelos reales de las luces, y las luces según el layeral que pertenecen
          
            List<TgcMeshData> realMeshData = new List<TgcMeshData>();
            for (int i = 0; i < sceneData.meshesData.Length; i++)
            {
                TgcMeshData meshData = sceneData.meshesData[i];

                //Es una luz, no cargar mesh, solo importan sus datos
                if (meshData.layerName.Equals(zona1+constLuz))
                {
                    //Guardar datos de luz de zona 1
                    LightData light = new LightData();
                    light.color = Color.FromArgb((int)meshData.color[0], (int)meshData.color[1], (int)meshData.color[2]);
                    light.aabb = new TgcBoundingBox(TgcParserUtils.float3ArrayToVector3(meshData.pMin), TgcParserUtils.float3ArrayToVector3(meshData.pMax));
                    light.pos = light.aabb.calculateBoxCenter();
                    light.spot = meshData.userProperties["esSpot"].Equals("SI");
                    light.direccion = convertirDireccion(meshData.userProperties["dir"]);
                    lucesZona1.Add(light);
                }else if(meshData.layerName.Equals(zona2+constLuz)){
                    //Guardar datos de luz de zona 2
                    LightData light = new LightData();
                    light.color = Color.FromArgb((int)meshData.color[0], (int)meshData.color[1], (int)meshData.color[2]);
                    light.aabb = new TgcBoundingBox(TgcParserUtils.float3ArrayToVector3(meshData.pMin), TgcParserUtils.float3ArrayToVector3(meshData.pMax));
                    light.pos = light.aabb.calculateBoxCenter();
                    light.spot = meshData.userProperties["esSpot"].Equals("SI");
                    light.direccion = convertirDireccion(meshData.userProperties["dir"]);
                    lucesZona2.Add(light);
                }else if(meshData.layerName.Equals(zona3+constLuz)){
                    //Guardar datos de luz de zona 3
                    LightData light = new LightData();
                    light.color = Color.FromArgb((int)meshData.color[0], (int)meshData.color[1], (int)meshData.color[2]);
                    light.aabb = new TgcBoundingBox(TgcParserUtils.float3ArrayToVector3(meshData.pMin), TgcParserUtils.float3ArrayToVector3(meshData.pMax));
                    light.pos = light.aabb.calculateBoxCenter();
                    light.spot = meshData.userProperties["esSpot"].Equals("SI");
                    light.direccion = convertirDireccion(meshData.userProperties["dir"]);
                    lucesZona3.Add(light);
                } //Es un mesh real, agregar a array definitivo
                else
                {
                    realMeshData.Add(meshData);
                }
            }

            //Quedaron separados los meshes de las luces. Las luces están separadas por zona.

            //Reemplazar array original de meshData de sceneData por el definitivo
            sceneData.meshesData = realMeshData.ToArray();

            //Ahora si cargar meshes reales            
            TgcSceneLoader loader = new TgcSceneLoader();
            scene = loader.loadScene(sceneData, mediaPath);
            Effect shader = TgcShaders.loadEffect(shaderUrl);

            //Pre-calculamos las 3 luces mas cercanas de cada mesh y cargamos el Shader
            
            foreach (TgcMesh mesh in scene.Meshes)
            {
                
                MeshLightData meshData = new MeshLightData();
                meshData.mesh = mesh;

                Vector3 meshCenter = mesh.BoundingBox.calculateBoxCenter();
                meshData.lights = lucesMasCercanas(meshCenter, 3, mesh.Layer);

                meshData.mesh.Effect = shader;
                //esto debe ser modificado una vez que tengamos la escena para elegir bien las tecnicas
                meshData.mesh.Effect.Technique = elegirTecnica(meshData.lights);
                //separados por zona, no se porqué, capaz para optimizar, no se
                if (mesh.Layer.Equals(this.zona1))
                {                    
                    meshesZona1.Add(meshData);
                }
                else if (mesh.Layer.Equals(this.zona2))
                {
                    meshesZona2.Add(meshData);
                }
                else
                {
                    meshesZona3.Add(meshData);
                }
            }


        }

        public Vector3 convertirDireccion(String dir)
        {
            Vector3 result = new Vector3();
            String[] vector = dir.Split(',');
            result.X = float.Parse(vector[0]);
            result.Y = float.Parse(vector[1]);
            result.Z = float.Parse(vector[2]);
            return result;
        }
        private List<LightData> seleccionarListaLuces(String layer)
        {
            if(layer.Equals(this.zona1 + constLuz)){
                return lucesZona1;
            }else if(layer.Equals(this.zona2 + constLuz)){
                return lucesZona2;
            }else{
                return lucesZona3;
            }
        }

        private List<LightData> lucesMasCercanas(Vector3 pos, int cant, String layer)
        {
            float minDist = float.MaxValue;
            LightData minLight = null;
            List<LightData> result = new List<LightData>();
            List<LightData> lights = seleccionarListaLuces(layer);

            for (int i = 0; i < cant; i++)
            {
                foreach (LightData light in lights)
                {
                    //Ignorar las luces ya agregadas
                    if (result.Contains(light))
                        continue;

                    float distSq = Vector3.LengthSq(pos - light.pos);
                    if (distSq < minDist)
                    {
                        minDist = distSq;
                        minLight = light;
                    }
                }
                result.Add(minLight);
                minDist = float.MaxValue;
            }

            return result;
        }

        private String elegirTecnica(List<LightData> luces)
        {
            int cantSpot = 0;
            String resultado;
            foreach(LightData ld in luces){
                if (ld.spot)
                {
                    cantSpot++;
                }
            }
            switch (cantSpot){
                case 0: resultado = "3Diffuse";
                    break;
                case 1:resultado = "1Spot2Diffuse";
                    break;
                case 2:resultado = "2Spot1Diffuse";
                    break;
                default: resultado = "3Spot";
                    break;
            }
            return resultado;
        }
        

    }
}

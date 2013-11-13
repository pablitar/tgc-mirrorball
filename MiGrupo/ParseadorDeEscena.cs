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
        String constLuz = "lights";
        
       // String shaderUrl = GuiController.Instance.AlumnoEjemplosMediaDir + "Shaders\\shaderIntegrador.fx";
       String shaderUrl = GuiController.Instance.AlumnoEjemplosMediaDir + "RideTheLightning\\Shaders\\shaderFinal.fx";
       public TgcScene scene;
       public Effect shader;

      public List<MeshLightData> meshesZona1 = new List<MeshLightData>();
      public List<LightData> lucesZona1 = new List<LightData>();
      public List<MeshLightData> meshesZona2 = new List<MeshLightData>();
      public List<LightData> lucesZona2 = new List<LightData>();
      public List<MeshLightData> meshesZona3 = new List<MeshLightData>();
      public List<LightData> lucesZona3 = new List<LightData>();
      public List<LuzRenderizada> lucesARenderizar = new List<LuzRenderizada>();


        public void cargarEscena(String zona1, String zona2, String zona3, String dirEscena, String nombreEscena){

            this.zona1 = zona1;
            this.zona2 = zona2;
            this.zona3 = zona3;

            //Cargar escenario, pero inicialmente solo hacemos el parser, para separar los objetos que son solo luces y no meshes
            string scenePath = GuiController.Instance.AlumnoEjemplosDir + dirEscena + nombreEscena;            
            string mediaPath = GuiController.Instance.AlumnoEjemplosDir + dirEscena;
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
                    LightData light = new LightData(meshData);                     
                    lucesZona1.Add(light);
                }else if(meshData.layerName.Equals(zona2+constLuz)){
                    //Guardar datos de luz de zona 2
                    LightData light = new LightData(meshData);                    
                    lucesZona2.Add(light);
                }else if(meshData.layerName.Equals(zona3+constLuz)){
                    //Guardar datos de luz de zona 3
                    LightData light = new LightData(meshData);                   
                    lucesZona3.Add(light);
                } //Es un mesh real, agregar a array definitivo
                
                realMeshData.Add(meshData);
                
            }

            //Quedaron separados los meshes de las luces. Las luces están separadas por zona.

            //Reemplazar array original de meshData de sceneData por el definitivo
            sceneData.meshesData = realMeshData.ToArray();

            //Ahora si cargar meshes reales            
            TgcSceneLoader loader = new TgcSceneLoader();            
            scene = loader.loadScene(sceneData, mediaPath);
            GuiController.Instance.Logger.log("Empieza compilacion de shader", Color.Red);
            shader = TgcShaders.loadEffect(shaderUrl);
            GuiController.Instance.Logger.log("Fin compilacion de shader", Color.Red);
            shader.SetValue("mirrorBallTexture", TextureLoader.FromFile(GuiController.Instance.D3dDevice, GuiController.Instance.AlumnoEjemplosMediaDir + "\\mirrorBallLights.png"));

            //Pre-calculamos las 3 luces mas cercanas de cada mesh y cargamos el Shader
            
            foreach (TgcMesh mesh in scene.Meshes)
            {
                
                MeshLightData meshData = new MeshLightData();
                meshData.mesh = mesh;

                Vector3 meshCenter = mesh.BoundingBox.calculateBoxCenter();
                meshData.lights = lucesMasCercanas(meshCenter, 3, mesh.Layer);
                

                meshData.mesh.Effect = shader;
                             
                //separados por zona, no se porqué, capaz para optimizar, no se
                if (mesh.Layer.Equals(this.zona1))
                {                    
                    meshesZona1.Add(meshData);
                }
                else if (mesh.Layer.Contains(this.zona2))
                {
                    meshesZona2.Add(meshData);
                }
                else
                {
                    meshesZona3.Add(meshData);
                }
               if(mesh.Layer.Contains(constLuz)){
                   luzEnMovimiento(mesh, lucesMasCercanas(meshCenter, 1, mesh.Layer));
                   meshData.mesh.Position = meshCenter;
               }
            }
            
            


        }

       
        private List<LightData> seleccionarListaLuces(String layer)
        {
            if (layer.Contains(this.zona1))
            {
                return lucesZona1;
            }
            else if (layer.Contains(this.zona2))
            {
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
            List<LightData> omni = new List<LightData>();
            List<LightData> spot = new List<LightData>();
            

            for (int i = 0; i < cant; i++)
            {
                foreach (LightData light in lights)
                {
                    //Ignorar las luces ya agregadas
                    if (spot.Contains(light) || omni.Contains(light))
                        continue;

                    float distSq = Vector3.LengthSq(pos - new Vector3(light.pos.X, light.pos.Y, light.pos.Z));
                    if (distSq < minDist)
                    {
                        minDist = distSq;
                        minLight = light;
                    }
                }
                if (minLight != null &&  minLight.spot)
                {
                    spot.Add(minLight);
                }
                else
                {
                    omni.Add(minLight);
                }               
                
                minDist = float.MaxValue;
            }
            result.AddRange(spot);
            result.AddRange(omni);

            return result;
        }

        public void ordenarLuces(List<LightData> lista)
        {
            List<LightData> omni = new List<LightData>();
            List<LightData> spot = new List<LightData>();

        }

        public String elegirTecnica(MeshLightData mesh)
        {
            int cantSpot = 0;
            String resultado;
            foreach (LightData ld in mesh.lights)
            {
                if (ld.spot)
                {
                    cantSpot++;
                }
            }
            switch (cantSpot)
            {
                case 0:
                    if (mesh.mesh.Layer.Contains(zona1) || mesh.mesh.Layer.Contains(zona3))
                    {
                        resultado = "TRES_DIFFUSE_Y_BOLA";
                    }
                    else
                    {
                        resultado = "TRES_DIFFUSE";
                    }

                    break;
                case 1: if (mesh.mesh.Layer.Contains(zona1) || mesh.mesh.Layer.Contains(zona3))
                    {
                        resultado = "SPOT_DOS_DIFFUSE_Y_BOLA";
                    }
                    else
                    {
                        resultado = "UN_SPOT_DOS_DIFFUSE";
                    }
                    break;
                case 2: if (mesh.mesh.Layer.Contains(zona1) || mesh.mesh.Layer.Contains(zona3))
                    {
                        resultado = "DOS_SPOT_DIFFUSE_Y_BOLA";
                    }
                    else
                    {
                        resultado = "DOS_SPOT_UN_DIFFUSE";
                    }
                    break;
                default: if (mesh.mesh.Layer.Contains(zona1) || mesh.mesh.Layer.Contains(zona3))
                    {
                        resultado = "TRES_SPOT_Y_BOLA";
                    }
                    else
                    {
                        resultado = "TRES_SPOT";
                    }
                    break;
            }
            return resultado;
        }

        public void luzEnMovimiento(TgcMesh mesh, List<LightData> luz)
        {
            String nombreMov;
            mesh.AutoTransformEnable = false;
            try
            {
                nombreMov = mesh.UserProperties["mov"];
            }
            catch (Exception e)
            {
                lucesARenderizar.Add(new LuzRenderizada(mesh, luz[0], new NoMover()));
                return;
            }
            if (nombreMov.Equals("NO"))
            {
                return ;
            }
            else if (nombreMov.Equals("RotarEjeX"))
            {
                
                lucesARenderizar.Add(new LuzRenderizada(mesh, luz[0], new RotarEjeX()));
                
            }
            else if (nombreMov.Equals("RotarEjeY"))
            {                
                lucesARenderizar.Add(new LuzRenderizada(mesh, luz[0], new RotarEjeY()));
                
            }
            lucesARenderizar.Add(new LuzRenderizada(mesh, luz[0], new NoMover()));
           
        }
        

    }
}

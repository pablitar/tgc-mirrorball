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
using AlumnoEjemplos.MiGrupo;

namespace Examples.Lights
{
    /// <summary>
    /// Ejemplo EjemploSpotLight:
    /// Unidades Involucradas:
    ///     # Unidad 4 - Texturas e Iluminación - Iluminación dinámica
    ///     # Unidad 8 - Adaptadores de Video - Shaders
    /// 
    /// Ejemplo avanzado. Ver primero ejemplo "Lights/EjemploPointLight"
    /// 
    /// Muestra como aplicar iluminación dinámica con PhongShading por pixel en un Pixel Shader, para un tipo
    /// de luz "Spot Light".
    /// Permite una única luz por objeto.
    /// Calcula todo el modelo de iluminación completo (Ambient, Diffuse, Specular)
    /// Las luces poseen atenuación por la distancia.
    /// 
    /// 
    /// Autor: Matías Leone, Leandro Barbagallo
    /// 
    /// </summary>
    public class EjemploSpotLight : TgcExample
    {
        TgcScene scene;
        Effect effect;
        CubeTexture cubeMap;
        List<LightData> lights;
        List<MeshLightData> meshesYLuces;

        public override string getCategory()
        {
            return "AlumnoEjemplos";
        }

        public override string getName()
        {
            return "Spot light guardas en escena";
        }

        public override string getDescription()
        {
            return "Tres luces más cercanas precalculadas";
        }

        public override void init()
        {
            Device d3dDevice = GuiController.Instance.D3dDevice;

            //Cargar escenario
            TgcSceneLoader loader = new TgcSceneLoader();

            //Cargar textura de CubeMap para Environment Map, fijo para todos los meshes
            cubeMap = TextureLoader.FromCubeFile(d3dDevice, GuiController.Instance.ExamplesMediaDir + "Shaders\\CubeMap.dds");

            //Cargar Shader personalizado de EnvironmentMap
            effect = TgcShaders.loadEffect(GuiController.Instance.AlumnoEjemplosDir + "AlumnoMedia\\RideTheLightning\\Shaders\\EnvironmentMap_Integrador2.fx");

            //Configurar MeshFactory customizado
            //scene = loader.loadSceneFromFile(GuiController.Instance.ExamplesMediaDir + "MeshCreator\\Scenes\\DepositoMin\\Deposito-TgcScene.xml");
           // scene = loader.loadSceneFromFile(GuiController.Instance.ExamplesMediaDir + "MeshCreator\\Scenes\\Deposito2\\Deposito-TgcScene.xml");
            //Cargar escenario, pero inicialmente solo hacemos el parser, para separar los objetos que son solo luces y no meshes
            string scenePath = GuiController.Instance.AlumnoEjemplosDir + "AlumnoMedia\\RideTheLightning\\Scenes\\Deposito\\Deposito-TgcScene.xml";
            string mediaPath = GuiController.Instance.AlumnoEjemplosDir + "AlumnoMedia\\RideTheLightning\\Scenes\\Deposito\\";
            TgcSceneParser parser = new TgcSceneParser();
            TgcSceneData sceneData = parser.parseSceneFromString(File.ReadAllText(scenePath));

            //Separar modelos reales de las luces, segun layer "Lights"
            lights = new List<LightData>();
            List<TgcMeshData> realMeshData = new List<TgcMeshData>();
            for (int i = 0; i < sceneData.meshesData.Length; i++)
            {
                TgcMeshData meshData = sceneData.meshesData[i];

                //Es una luz, no cargar mesh, solo importan sus datos
                if (meshData.layerName == "lights")
                {
                    //Guardar datos de luz
                    LightData light = new LightData();
                    light.color = Color.FromArgb((int)meshData.color[0], (int)meshData.color[1], (int)meshData.color[2]);
                    light.aabb = new TgcBoundingBox(TgcParserUtils.float3ArrayToVector3(meshData.pMin), TgcParserUtils.float3ArrayToVector3(meshData.pMax));
                    light.pos = light.aabb.calculateBoxCenter();
                    light.spot = meshData.userProperties["esSpot"].Equals("SI");
                    light.direccion = convertirDireccion(meshData.userProperties["dir"]);
                    lights.Add(light);
                }
                //Es un mesh real, agregar a array definitivo
                else
                {
                    realMeshData.Add(meshData);
                }
            }

            //Reemplazar array original de meshData de sceneData por el definitivo
            sceneData.meshesData = realMeshData.ToArray();

            //Ahora si cargar meshes reales
            
            scene = loader.loadScene(sceneData, mediaPath);

            //Pre-calculamos las 3 luces mas cercanas de cada mesh
            meshesYLuces = new List<MeshLightData>();
            foreach (TgcMesh mesh in scene.Meshes)
            {
                MeshLightData meshData = new MeshLightData();
                meshData.mesh = mesh;
                Vector3 meshCeter = mesh.BoundingBox.calculateBoxCenter();
                meshData.lights = lucesMasCercanas(meshCeter, 3);               
                meshesYLuces.Add(meshData);
            }



            //Camara en 1ra persona
            
            GuiController.Instance.FpsCamera.Enable = true;
            GuiController.Instance.FpsCamera.MovementSpeed = 400f;
            GuiController.Instance.FpsCamera.JumpSpeed = 300f;
            GuiController.Instance.FpsCamera.setCamera(new Vector3(80, 80, 0), new Vector3(0, 80, 1));


            //Mesh para la luz      

            //Modifiers de la luz
            GuiController.Instance.Modifiers.addBoolean("lightEnable", "lightEnable", true);
            GuiController.Instance.Modifiers.addBoolean("linterna", "linterna", true);
            GuiController.Instance.Modifiers.addBoolean("foco", "foco", true);
                    
            GuiController.Instance.Modifiers.addColor("lightColor", Color.White);
            GuiController.Instance.Modifiers.addFloat("lightIntensity", 0, 150, 35);
            GuiController.Instance.Modifiers.addFloat("lightAttenuation", 0.1f, 2, 0.3f);
            GuiController.Instance.Modifiers.addFloat("specularEx", 0, 20, 9f);
            GuiController.Instance.Modifiers.addFloat("spotAngle", 0, 180, 39f);
            GuiController.Instance.Modifiers.addFloat("spotExponent", 0, 20, 7f);

            //Modifiers de material
            GuiController.Instance.Modifiers.addColor("mEmissive", Color.Black);
            GuiController.Instance.Modifiers.addColor("mAmbient", Color.White);
            GuiController.Instance.Modifiers.addColor("mDiffuse", Color.White);
            GuiController.Instance.Modifiers.addColor("mSpecular", Color.White);
            
        }

        


        public override void render(float elapsedTime)
        {
            Device device = GuiController.Instance.D3dDevice;


            //Habilitar luz
            bool lightEnable = (bool)GuiController.Instance.Modifiers["lightEnable"];
            Effect currentShader;
            String currentTechnique;
            if (lightEnable)
            {
                //Con luz: Cambiar el shader actual por el shader default que trae el framework para iluminacion dinamica con SpotLight
                //currentShader = TgcShaders.loadEffect(GuiController.Instance.AlumnoEjemplosMediaDir + "\\Shaders\\TgcMeshSpotLightShader2.fx");
                currentShader = this.effect;
                currentTechnique = "ThreeLightsTechnique";
            }
            else
            {
                //Sin luz: Restaurar shader default
                currentShader = GuiController.Instance.Shaders.TgcMeshShader;
                currentTechnique = GuiController.Instance.Shaders.getTgcMeshTechnique(TgcMesh.MeshRenderType.DIFFUSE_MAP);
            }

            //Aplicar a cada mesh el shader actual
            foreach (MeshLightData mesh in meshesYLuces)
            {
                mesh.mesh.Effect = currentShader;
                //El Technique depende del tipo RenderType del mesh
                mesh.mesh.Technique = currentTechnique;
            }

            Vector3 lightPos = GuiController.Instance.FpsCamera.Position;
            ColorValue[] lightColors = new ColorValue[2];
            Vector4[] pointLightPositions = new Vector4[2];
            float[] pointLightIntensity = new float[2];
            float[] pointLightAttenuation = new float[2];
            Vector4[] spotLightDir = new Vector4[2];           
          
            
            Plane v = GuiController.Instance.Frustum.NearPlane;
            /*
            for (int i = 0; i < lightMeshes.Length; i++)
            {
                TgcBox lightMesh = lightMeshes[i];
                lightMesh.Position = origLightPos[i] + Vector3.Scale(move, i + 1);

                lightColors[i] = ColorValue.FromColor(lightMesh.Color);
                pointLightPositions[i] = TgcParserUtils.vector3ToVector4(lightMesh.Position);
                pointLightIntensity[i] = (float)GuiController.Instance.Modifiers["lightIntensity"];
                pointLightAttenuation[i] = (float)GuiController.Instance.Modifiers["lightAttenuation"];
            }

            */
            //luz 1 linterna
            lightColors[0] = ColorValue.FromColor((Color)GuiController.Instance.Modifiers["lightColor"]);
            /*pointLightPositions[0] = TgcParserUtils.vector3ToVector4(lightPos);
            spotLightDir[0] =  new Vector4( v.A, v.B, v.C, 0);*/
            pointLightPositions[0] = new Vector4(0, 200, 300, 0);
            spotLightDir[0] = new Vector4(0, -1, 0, 0);
            if ((bool)GuiController.Instance.Modifiers["linterna"])
            {
                pointLightIntensity[0] = (float)GuiController.Instance.Modifiers["lightIntensity"];
            }
            else
            {
                pointLightIntensity[0] = (float) 0 ;
            }
            pointLightAttenuation[0] = (float)GuiController.Instance.Modifiers["lightAttenuation"];
            //luz 2 arriba
            lightColors[1] = ColorValue.FromColor(Color.Yellow);
            pointLightPositions[1] = new Vector4(0, 200, 0, 0);
            spotLightDir[1] = new Vector4(0, -1, 0, 0);
            if ((bool)GuiController.Instance.Modifiers["foco"])
            {
                pointLightIntensity[1] = (float)GuiController.Instance.Modifiers["lightIntensity"];
            }
            else
            {
                pointLightIntensity[1] = (float)0;
            }
            pointLightAttenuation[1] = (float)GuiController.Instance.Modifiers["lightAttenuation"];


            //Renderizar meshes
            /*
            foreach (TgcMesh mesh in scene.Meshes)
            {
                if (lightEnable)
                {
                    //Cargar variables shader de la luz

                    mesh.Effect.SetValue("lightColor", lightColors);
                    mesh.Effect.SetValue("lightPosition", pointLightPositions);
                    mesh.Effect.SetValue("spotLightDir", spotLightDir);
                    mesh.Effect.SetValue("lightIntensity", pointLightIntensity);
                    mesh.Effect.SetValue("lightAttenuation", pointLightAttenuation);
                   
                  
                    mesh.Effect.SetValue("spotLightAngleCos", FastMath.ToRad((float)GuiController.Instance.Modifiers["spotAngle"]));
                    mesh.Effect.SetValue("spotLightExponent", (float)GuiController.Instance.Modifiers["spotExponent"]);

                    //Cargar variables de shader de Material. El Material en realidad deberia ser propio de cada mesh. Pero en este ejemplo se simplifica con uno comun para todos
                    mesh.Effect.SetValue("materialEmissiveColor", ColorValue.FromColor((Color)GuiController.Instance.Modifiers["mEmissive"]));
                    mesh.Effect.SetValue("materialAmbientColor", ColorValue.FromColor((Color)GuiController.Instance.Modifiers["mAmbient"]));
                    mesh.Effect.SetValue("materialDiffuseColor", ColorValue.FromColor((Color)GuiController.Instance.Modifiers["mDiffuse"]));
                    mesh.Effect.SetValue("materialSpecularColor", ColorValue.FromColor((Color)GuiController.Instance.Modifiers["mSpecular"]));
                    mesh.Effect.SetValue("materialSpecularExp", (float)GuiController.Instance.Modifiers["specularEx"]);
                }

                //Renderizar modelo
                mesh.render();
            }*/

            foreach (MeshLightData ml in meshesYLuces)
            {
                TgcMesh mesh = ml.mesh;

                if (lightEnable)
                {
                    mesh.Effect.SetValue("eyePosition", TgcParserUtils.vector3ToFloat4Array(GuiController.Instance.FpsCamera.getPosition()));
                    mesh.Effect.SetValue("bumpiness", (float)1);
                    mesh.Effect.SetValue("reflection", (float)1);

                    //Cargar variables de shader del Material
                    mesh.Effect.SetValue("materialEmissiveColor", ColorValue.FromColor((Color)GuiController.Instance.Modifiers["mEmissive"]));
                    mesh.Effect.SetValue("materialAmbientColor", ColorValue.FromColor((Color)GuiController.Instance.Modifiers["mAmbient"]));
                    mesh.Effect.SetValue("materialDiffuseColor", ColorValue.FromColor((Color)GuiController.Instance.Modifiers["mDiffuse"]));
                    mesh.Effect.SetValue("materialSpecularColor", ColorValue.FromColor((Color)GuiController.Instance.Modifiers["mSpecular"]));
                    mesh.Effect.SetValue("materialSpecularExp", (float)GuiController.Instance.Modifiers["specularEx"]);

                    //CubeMap
                    mesh.Effect.SetValue("texCubeMap", cubeMap);

                    //Cargar variables de shader de las 3 luces
                    //Intensidad y atenuacion deberian ser atributos propios de cada luz
                    float lightIntensity = (float)GuiController.Instance.Modifiers["lightIntensity"];
                    float lightAttenuation = (float)GuiController.Instance.Modifiers["lightAttenuation"];
                    mesh.Effect.SetValue("lightIntensity", new float[] { lightIntensity, lightIntensity, lightIntensity });
                    mesh.Effect.SetValue("lightAttenuation", new float[] { lightAttenuation, lightAttenuation, lightAttenuation });

                    mesh.Effect.SetValue("lightColor", new ColorValue[] { ColorValue.FromColor(ml.lights[0].color), ColorValue.FromColor(ml.lights[1].color), ColorValue.FromColor(ml.lights[2].color) });
                    mesh.Effect.SetValue("lightPosition", new Vector4[] { TgcParserUtils.vector3ToVector4(ml.lights[0].pos), TgcParserUtils.vector3ToVector4(ml.lights[1].pos), TgcParserUtils.vector3ToVector4(ml.lights[2].pos) });
                }


                //Renderizar modelo
                mesh.render();
            }


            foreach (LightData ld in lights)
            {
                TgcBox caja = TgcBox.fromSize(new Vector3(50, 50, 50), ld.color);
                caja.Position = ld.pos;
                caja.render();
            }

            //Renderizar mesh de luz

          
        }


        /// <summary>
        /// Devuelve la luz mas cercana a la posicion especificada
        /// </summary>
        private List<LightData> lucesMasCercanas(Vector3 pos, int cant)
        {
            float minDist = float.MaxValue;
            LightData minLight = null;
            List<LightData> result = new List<LightData>();

            for(int i = 0; i< cant; i++){
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

        public Vector3 convertirDireccion(String dir)
        {
            Vector3 result = new Vector3();
           String[] vector = dir.Split(',');
           result.X = float.Parse(vector[0]);
           result.Y = float.Parse(vector[1]);
           result.Z = float.Parse(vector[2]);
           return result ;
        }


        public override void close()
        {
            scene.disposeAll();            
        }


    }

    

}

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

using TgcViewer.Utils.Shaders;
using TgcViewer.Utils.Interpolation;
using AlumnoEjemplos.MiGrupo;

namespace AlumnoEjemplos.RideTheLightning.Lights
{
    
    public class EjemploEscenaNueva : TgcExample
    {
        ParseadorDeEscena parseador;
        float mirrorBallRowCount;
        int numberOfProjections = 25;
        float mirrorBallFov;
        Matrix mirrorBallProjection;
        Vector3 mirrorBallDirectionVector = new Vector3(1, 0, 0);


        public override string getCategory()
        {
            return "AlumnoEjemplos";
        }

        public override string getName()
        {
            return "NuevaEscena";
        }

        public override string getDescription()
        {
            return "Primer Intento de escena final";
        }

        public override void init()
        {
            Device d3dDevice = GuiController.Instance.D3dDevice;

            //Cargar escenario
            TgcSceneLoader loader = new TgcSceneLoader();
            //Configurar MeshFactory customizado
            parseador = new ParseadorDeEscena();
            parseador.cargarEscena("hall", "banio", "nada", "AlumnoMedia\\ScenesParts\\", "boxes-TgcScene.xml");

            //Camara en 1ra persona
            
            GuiController.Instance.FpsCamera.Enable = true;
            GuiController.Instance.FpsCamera.MovementSpeed = 400f;
            GuiController.Instance.FpsCamera.JumpSpeed = 300f;
            GuiController.Instance.FpsCamera.setCamera(new Vector3(80, 80, 0), new Vector3(0, 80, 1));

            //Modifiers de la luz
                         
            GuiController.Instance.Modifiers.addColor("colorLinterna", Color.White);
            GuiController.Instance.Modifiers.addFloat("intensidadLinterna", 0, 150, 35);
            GuiController.Instance.Modifiers.addFloat("atenuacionLinterna", 0.1f, 2, 0.3f);
            GuiController.Instance.Modifiers.addFloat("angleCosLinterna", 0, 180, 39f);
            GuiController.Instance.Modifiers.addFloat("exponentLinterna", 0, 20, 7f);

            //Modifiers de material
            GuiController.Instance.Modifiers.addColor("mEmissive", Color.Black);
            GuiController.Instance.Modifiers.addColor("mAmbient", Color.White);
            GuiController.Instance.Modifiers.addColor("mDiffuse", Color.White);
            GuiController.Instance.Modifiers.addColor("mSpecular", Color.White);
            GuiController.Instance.Modifiers.addFloat("mspecularEx", 0, 20, 9f);

            //Bola de espejos
            GuiController.Instance.Modifiers.addFloat("mirrorBallIntensity", 0f, 10f, 5f);
            GuiController.Instance.Modifiers.addFloat("mirrorBallAttenuation", 0f, 10f, 0.2f);
            GuiController.Instance.Modifiers.addVertex3f("mirrorBallPosition", new Vector3(-1000, -1000, -1000), new Vector3(1000, 1000, 1000), new Vector3(0, 100, 0));
            mirrorBallRowCount = FastMath.Sqrt(numberOfProjections);

            mirrorBallFov = FastMath.PI / mirrorBallRowCount;
            mirrorBallProjection = Matrix.PerspectiveFovLH(mirrorBallFov, 1.0f, 0.1f, 300f);
        }


        public void configurarLuces(MeshLightData mld)
        {
            Effect effect =  mld.mesh.Effect;
            ColorValue[] lightColors = new ColorValue[3];
            Vector4[] pointLightPositions = new Vector4[3];
            float[] pointLightIntensity = new float[3];
            float[] pointLightAttenuation = new float[3];
            float[] spotLightAngleCos = new float[3];
            float[] spotLightExponent = new float[3];
            Vector4[] spotLightDir = new Vector4[3];
            
            for (int i = 0; i < 3; i++)
            {
                lightColors[i] = ColorValue.FromColor(mld.lights[i].color);
                pointLightPositions[i] = mld.lights[i].pos;
                spotLightDir[i] = mld.lights[i].direccion;
                pointLightIntensity[i] = mld.lights[i].intencidad;
                pointLightAttenuation[i] = mld.lights[i].atenuacion;
                spotLightExponent[i] = mld.lights[i].exp;
                spotLightAngleCos[i] = FastMath.ToRad(mld.lights[i].angleCos);
            }
            effect.SetValue("spotLightAngleCos", spotLightAngleCos);
            effect.SetValue("spotLightExponent", spotLightExponent);
            effect.SetValue("lightIntensity", pointLightIntensity);
            effect.SetValue("lightAttenuation", pointLightAttenuation);
            effect.SetValue("lightColor", lightColors);
            effect.SetValue("spotLightDir", spotLightDir);
            effect.SetValue("lightPosition", pointLightPositions);
        }
        public void configurarLinterna(Effect shader)
        {
            Plane v = GuiController.Instance.Frustum.NearPlane;
            Vector3 lightPos = GuiController.Instance.FpsCamera.Position;
            Vector3 direccionLinterna = Vector3.Normalize(new Vector3(v.A, v.B, v.C));

            shader.SetValue("angleCosLinterna", FastMath.ToRad((float)GuiController.Instance.Modifiers["angleCosLinterna"]));
            shader.SetValue("exponentLinterna", (float)GuiController.Instance.Modifiers["exponentLinterna"]);
            shader.SetValue("intensidadLinterna", (float)GuiController.Instance.Modifiers["intensidadLinterna"]);
            shader.SetValue("atenuacionLinterna", (float)GuiController.Instance.Modifiers["atenuacionLinterna"]);
            shader.SetValue("colorLinterna", ColorValue.FromColor((Color)GuiController.Instance.Modifiers["colorLinterna"]));
            shader.SetValue("direccionLinterna", TgcParserUtils.vector3ToVector4(direccionLinterna));
            shader.SetValue("posicionLinterna", TgcParserUtils.vector3ToVector4(lightPos));
        }
        private Vector3 getDirectionVector(int i, int u)
        {
            Vector3 directionVector = new Vector3(mirrorBallDirectionVector.X, mirrorBallDirectionVector.Y, mirrorBallDirectionVector.Z);

            directionVector.TransformCoordinate(Matrix.RotationY(mirrorBallFov * i + (mirrorBallFov - FastMath.PI) / 2));

            Vector3 xAxis = new Vector3(directionVector.X, directionVector.Y, directionVector.Z);
            xAxis.TransformCoordinate(Matrix.RotationY(FastMath.PI_HALF));

            directionVector.TransformCoordinate(Matrix.RotationAxis(xAxis, mirrorBallFov * u + (mirrorBallFov - FastMath.PI) / 2));

            return directionVector;
        }

        private Matrix[] getMirrorBallViewProjMatrix()
        {
            Matrix[] result = new Matrix[numberOfProjections];

            int currentIndex = 0;

            for (int i = 0; i < mirrorBallRowCount; i++)
            {
                for (int u = 0; u < mirrorBallRowCount; u++)
                {
                    result[currentIndex] =
                        Matrix.LookAtLH((Vector3)GuiController.Instance.Modifiers["mirrorBallPosition"],
                        (Vector3)GuiController.Instance.Modifiers["mirrorBallPosition"] + getDirectionVector(i, u), new Vector3(0, 1, 0)) * mirrorBallProjection;
                    currentIndex++;
                }
            }

            return result;
        }

        void configurarBola(Effect shader, Matrix[] viewProjMatrix)
        {
            shader.SetValue("matViewProjMirrorBall", viewProjMatrix);

            shader.SetValue("mirrorBallPosition", TgcParserUtils.vector3ToFloat4Array((Vector3)GuiController.Instance.Modifiers["mirrorBallPosition"]));

            shader.SetValue("mirrorBallAttenuation", (float)GuiController.Instance.Modifiers["mirrorBallAttenuation"]);
            shader.SetValue("mirrorBallIntensity", (float)GuiController.Instance.Modifiers["mirrorBallIntensity"]);
        }

        public override void render(float elapsedTime)
        {
            Device device = GuiController.Instance.D3dDevice;
            

             //Cargar variables de shader de Material. es el mismo para todos
            parseador.shader.SetValue("materialEmissiveColor", ColorValue.FromColor((Color)GuiController.Instance.Modifiers["mEmissive"]));
            parseador.shader.SetValue("materialAmbientColor", ColorValue.FromColor((Color)GuiController.Instance.Modifiers["mAmbient"]));
            parseador.shader.SetValue("materialDiffuseColor", ColorValue.FromColor((Color)GuiController.Instance.Modifiers["mDiffuse"]));
            parseador.shader.SetValue("materialSpecularColor", ColorValue.FromColor((Color)GuiController.Instance.Modifiers["mSpecular"]));
            parseador.shader.SetValue("materialSpecularExp", (float)GuiController.Instance.Modifiers["mspecularEx"]);
            parseador.shader.SetValue("eyePosition", TgcParserUtils.vector3ToFloat4Array(GuiController.Instance.FpsCamera.getPosition()));

            Matrix directionRotationMatrix = Matrix.RotationY(FastMath.QUARTER_PI * elapsedTime);

            mirrorBallDirectionVector.TransformCoordinate(directionRotationMatrix);

            Matrix[] viewProjMatrix = getMirrorBallViewProjMatrix();
            configurarBola(parseador.shader, viewProjMatrix);
            configurarLinterna(parseador.shader);

            //Renderizar meshes
            foreach (MeshLightData mld in parseador.meshesZona1)
            {
                configurarLuces(mld);
                configurarLinterna(mld.mesh.Effect);
                mld.mesh.Technique = parseador.elegirTecnica(mld);
                
                //Renderizar modelo
                mld.mesh.render();
            }/*
            foreach (MeshLightData mld in parseador.meshesZona2)
            {
                configurarLuces(mld);
                mld.mesh.Technique = parseador.elegirTecnica(mld);
                
                //Renderizar modelo
                mld.mesh.render();
            }*/

            //Renderizar mesh de luz
          
        }




        public override void close()
        {
            parseador.scene.disposeAll();
        }



    }

    

}

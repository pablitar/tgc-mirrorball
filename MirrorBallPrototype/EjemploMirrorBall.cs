using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;
using TgcViewer;
using TgcViewer.Example;
using TgcViewer.Utils.Modifiers;
using TgcViewer.Utils.TgcSceneLoader;
using TgcViewer.Utils.TgcGeometry;
using TgcViewer.Utils.Shaders;

namespace AlumnoEjemplos.RideTheLightning.MirrorBall
{
    /// <summary>
    /// Ejemplo del alumno
    /// </summary>
    public class MirrorBall : TgcExample
    {

        //Device de DirectX para crear primitivas
        Device d3dDevice = GuiController.Instance.D3dDevice;
        //Carpeta de archivos Media del alumno
        string alumnoMediaFolder = GuiController.Instance.AlumnoEjemplosMediaDir;

        TgcMesh mirrorBall;

        TgcFrustum mirrorBallFrustum;

        //Este valor debe modificarse también en el shader. También, debe ser un cuadrado perfecto (16, 25, 36, 49, 64)
        int numberOfProjections = 25;

        //Matrix mirrorBallProjection = Matrix.OrthoLH(2f, 2f, 0.1f, 300f);

        Vector3 mirrorBallDirectionVector = new Vector3(1, 0, 0);
        
        float mirrorBallFov;
        
        float mirrorBallRowCount;

        Matrix mirrorBallProjection;

        Effect mirrorBallEffect;

        List<TgcMesh> meshes = new List<TgcMesh>();

        /// <summary>
        /// Categoría a la que pertenece el ejemplo.
        /// Influye en donde se va a haber en el árbol de la derecha de la pantalla.
        /// </summary>
        public override string getCategory()
        {
            return "AlumnoEjemplos";
        }

        /// <summary>
        /// Completar nombre del grupo en formato Grupo NN
        /// </summary>
        public override string getName()
        {
            return "Ride The Lightning";
        }

        /// <summary>
        /// Completar con la descripción del TP
        /// </summary>
        public override string getDescription()
        {
            return "Prototipo para incluir en el ejemplo final - Una bola de espejos que proyecta sobre 4 paredes.";
        }

        /// <summary>
        /// Método que se llama una sola vez,  al principio cuando se ejecuta el ejemplo.
        /// Escribir aquí todo el código de inicialización: cargar modelos, texturas, modifiers, uservars, etc.
        /// Borrar todo lo que no haga falta
        /// </summary>
        public override void init()
        {
            //GuiController.Instance: acceso principal a todas las herramientas del Framework
            
            mirrorBallEffect = TgcShaders.loadEffect(GuiController.Instance.AlumnoEjemplosMediaDir + "\\Shaders\\MirrorBallEffectShader.fx");

            GuiController.Instance.FpsCamera.Enable = true;

            GuiController.Instance.FpsCamera.setCamera(new Vector3(0, 0, -20), new Vector3(200, 0, 0));

            configureModifiers();

            configureWalls();

            configureExtraMesh();

            configureMirrorBall();

            configureFrustum();
        }

        private void configureExtraMesh()
        {
            TgcSphere aSphere = new TgcSphere(80, Color.Gray, new Vector3(190, 0, 0));

            aSphere.Radius = 80;

            aSphere.updateValues();

            TgcMesh mesh = aSphere.toMesh("extraMesh");

            configureMirrorBallReception(mesh);

            addMesh(mesh);
        }

        private void configureModifiers()
        {
            mirrorBallRowCount = FastMath.Sqrt(numberOfProjections);

            mirrorBallFov = FastMath.PI / mirrorBallRowCount;
            mirrorBallProjection = Matrix.PerspectiveFovLH(mirrorBallFov, 1.0f, 0.1f, 300f);

            GuiController.Instance.Modifiers.addFloat("mirrorBallIntensity", 0f, 10f, 5f);
            GuiController.Instance.Modifiers.addFloat("mirrorBallAttenuation", 0f, 10f, 0.2f);
            GuiController.Instance.Modifiers.addInt("frustumFollowsX", 0, (int)mirrorBallRowCount - 1, 2);
            GuiController.Instance.Modifiers.addInt("frustumFollowsY", 0, (int)mirrorBallRowCount - 1, 2);
            GuiController.Instance.Modifiers.addBoolean("showFrustum", "Show frustum", true);
            GuiController.Instance.Modifiers.addVertex3f("mirrorBallPosition", new Vector3(-1000, -1000, -1000), new Vector3(1000, 1000, 1000), new Vector3(0, 0, 0));
        }

        private void configureFrustum()
        {
            mirrorBallFrustum = new TgcFrustum();
            mirrorBallFrustum.updateVolume(getMirrorBallViewMatrix(), mirrorBallProjection);
        }

        private void configureWalls()
        {
            addMesh(createWall(new Vector3(200, 0, 0), new Vector3(20,200,400)));
            addMesh(createWall(new Vector3(0, 0, 200), new Vector3(400,200,20)));
            addMesh(createWall(new Vector3(0, 0, -200), new Vector3(400,200,20)));
            addMesh(createWall(new Vector3(-200, 0, 0), new Vector3(20,200,400)));
            addMesh(createWall(new Vector3(0, -100, 0), new Vector3(400, 20, 400)));
            
        }

        private void addMesh(TgcMesh mesh)
        {
            meshes.Add(mesh);
        }

        private TgcMesh createWall(Vector3 center, Vector3 size)
        {
            TgcMesh wallMesh = TgcBox.fromSize(center, size, TgcTexture.createTexture(alumnoMediaFolder + "\\Wall.jpg")).toMesh("outerBox");
            configureMirrorBallReception(wallMesh);

            wallMesh.Effect.SetValue("mirrorBallTexture", TextureLoader.FromFile(d3dDevice, alumnoMediaFolder + "\\mirrorBallLights.png"));

            return wallMesh; 
        }

        private void configureMirrorBallReception(TgcMesh wallMesh)
        {
            wallMesh.Effect = mirrorBallEffect;
            wallMesh.Technique = "MIRROR_BALL_MAP";

            configureLight(wallMesh.Effect);
        }

        private T getModifierValue<T>(string key)
        {
            return (T)GuiController.Instance.Modifiers.getValue(key);
        }

        private void configureMirrorBall()
        {
            TgcSphere aSphere = new TgcSphere(20, Color.White, getModifierValue<Vector3>("mirrorBallPosition"));

            aSphere.Radius = 20.0f;

            aSphere.BasePoly = TgcSphere.eBasePoly.CUBE;
            
            aSphere.updateValues();

            mirrorBall = aSphere.toMesh("MirrorBall");

            mirrorBall.Effect = GuiController.Instance.Shaders.TgcMeshPointLightShader;

            mirrorBall.Technique = GuiController.Instance.Shaders.getTgcMeshTechnique(mirrorBall.RenderType);

            configureLight(mirrorBall.Effect);
        }

        private void configureLight(Effect effect)
        {
            effect.SetValue("lightColor", ColorValue.FromColor(Color.LightYellow));
            effect.SetValue("lightPosition", TgcParserUtils.vector3ToFloat4Array(new Vector3(100, 200, 0)));

            effect.SetValue("lightIntensity", 50.0f);
            effect.SetValue("lightAttenuation", 1.0f);

            //Cargar variables de shader de Material. El Material en realidad deberia ser propio de cada mesh. Pero en este ejemplo se simplifica con uno comun para todos
            effect.SetValue("materialEmissiveColor", ColorValue.FromColor(Color.Black));
            effect.SetValue("materialAmbientColor", ColorValue.FromColor(Color.White));
            effect.SetValue("materialDiffuseColor", ColorValue.FromColor(Color.White));
            effect.SetValue("materialSpecularColor", ColorValue.FromColor(Color.LightGray));
            effect.SetValue("materialSpecularExp", 1.0f);
        }


        /// <summary>
        /// Método que se llama cada vez que hay que refrescar la pantalla.
        /// Escribir aquí todo el código referido al renderizado.
        /// Borrar todo lo que no haga falta
        /// </summary>
        /// <param name="elapsedTime">Tiempo en segundos transcurridos desde el último frame</param>
        public override void render(float elapsedTime)
        {

            mirrorBall.rotateY(FastMath.QUARTER_PI * elapsedTime);
            mirrorBall.Position = getModifierValue<Vector3>("mirrorBallPosition");

            Matrix directionRotationMatrix = Matrix.RotationY(FastMath.QUARTER_PI * elapsedTime);

            mirrorBallDirectionVector.TransformCoordinate(directionRotationMatrix);


            updateEyePosition(mirrorBall.Effect);

            Matrix[] viewProjMatrix = getMirrorBallViewProjMatrix();
            
            mirrorBall.render();

            foreach(TgcMesh wall in meshes) {

                updateEyePosition(wall.Effect);
                updateMirrorBallValues(wall.Effect, viewProjMatrix);

                wall.render();
            }

            if (getModifierValue<Boolean>("showFrustum"))
            {
                mirrorBallFrustum.updateMesh(mirrorBall.Position, mirrorBall.Position + getFrustumLookAt());
                mirrorBallFrustum.render();
            }
        }

        private void updateMirrorBallValues(Effect effect, Matrix[] viewProjMatrix)
        {
            effect.SetValue("matViewProjMirrorBall", viewProjMatrix);
            //effect.SetValue("matProjMirrorBall", mirrorBallProjection);
            effect.SetValue("mirrorBallPosition", TgcParserUtils.vector3ToFloat4Array(mirrorBall.Position));

            effect.SetValue("mirrorBallAttenuation", getModifierValue<float>("mirrorBallAttenuation"));
            effect.SetValue("mirrorBallIntensity", getModifierValue<float>("mirrorBallIntensity"));
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
                        Matrix.LookAtLH(mirrorBall.Position, mirrorBall.Position + getDirectionVector(i, u), new Vector3(0, 1, 0)) * mirrorBallProjection;
                    currentIndex++;
                }
            }

            return result;
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

        private Matrix getMirrorBallViewMatrix()
        {
            return Matrix.LookAtLH(mirrorBall.Position, mirrorBall.Position +
                getFrustumLookAt(),
                new Vector3(0, 1, 0));
        }

        private Vector3 getFrustumLookAt()
        {
            return getDirectionVector(getModifierValue<int>("frustumFollowsX"), getModifierValue<int>("frustumFollowsY"));
        }

        private static void updateEyePosition(Effect e)
        {
            e.SetValue("eyePosition", TgcParserUtils.vector3ToFloat4Array(GuiController.Instance.FpsCamera.getPosition()));
        }

        /// <summary>
        /// Método que se llama cuando termina la ejecución del ejemplo.
        /// Hacer dispose() de todos los objetos creados.
        /// </summary>
        public override void close()
        {

        }

    }
}

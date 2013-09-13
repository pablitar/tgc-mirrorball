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

        TgcMesh outerBox;
        TgcMesh mirrorBall;

        Matrix mirrorBallProjection = Matrix.PerspectiveFovLH(FastMath.PI_HALF, 1.0f, 1.0f, 100f);
        Vector3 mirrorBallLookAtVector = new Vector3(30, 0, 0);

        /// <summary>
        /// Categor�a a la que pertenece el ejemplo.
        /// Influye en donde se va a haber en el �rbol de la derecha de la pantalla.
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
        /// Completar con la descripci�n del TP
        /// </summary>
        public override string getDescription()
        {
            return "Prototipo para incluir en el ejemplo final - Una bola de espejos que proyecta sobre 4 paredes.";
        }

        /// <summary>
        /// M�todo que se llama una sola vez,  al principio cuando se ejecuta el ejemplo.
        /// Escribir aqu� todo el c�digo de inicializaci�n: cargar modelos, texturas, modifiers, uservars, etc.
        /// Borrar todo lo que no haga falta
        /// </summary>
        public override void init()
        {
            //GuiController.Instance: acceso principal a todas las herramientas del Framework

            GuiController.Instance.FpsCamera.Enable = true;

            GuiController.Instance.FpsCamera.setCamera(new Vector3(0, 0, -20), new Vector3(0, 0, 0));


            configureWall();

            configureMirrorBall();


        }

        private void configureWall()
        {
            outerBox = TgcBox.fromSize(new Vector3(200, 0, 0), new Vector3(20, 200, 200), TgcTexture.createTexture(alumnoMediaFolder + "\\Wall.jpg")).toMesh("outerBox");
            outerBox.Effect = TgcShaders.loadEffect(alumnoMediaFolder + "\\Shaders\\MirrorBallEffectShader.fx");
            outerBox.Technique = "MIRROR_BALL_MAP";

            outerBox.Effect.SetValue("mirrorBallTexture", TextureLoader.FromFile(d3dDevice, alumnoMediaFolder + "\\mirrorBallLights.png"));

            configureLight(outerBox.Effect);
        }

        private void configureMirrorBall()
        {
            TgcSphere aSphere = new TgcSphere(1000, Color.White, new Vector3(20, 0, 0));
            aSphere.BasePoly = TgcSphere.eBasePoly.CUBE;
            
            aSphere.updateValues();

            mirrorBall = aSphere.toMesh("MirrorBall");

            mirrorBall.Effect = GuiController.Instance.Shaders.TgcMeshPointLightShader;

            mirrorBall.Technique = GuiController.Instance.Shaders.getTgcMeshTechnique(mirrorBall.RenderType);

            mirrorBall.Scale = new Vector3(0.02f, 0.02f, 0.02f);
            configureLight(mirrorBall.Effect);
        }

        private void configureLight(Effect effect)
        {
            effect.SetValue("lightColor", ColorValue.FromColor(Color.LightYellow));
            effect.SetValue("lightPosition", TgcParserUtils.vector3ToFloat4Array(new Vector3(0, 200, 0)));

            effect.SetValue("lightIntensity", 30.0f);
            effect.SetValue("lightAttenuation", 1.0f);

            //Cargar variables de shader de Material. El Material en realidad deberia ser propio de cada mesh. Pero en este ejemplo se simplifica con uno comun para todos
            effect.SetValue("materialEmissiveColor", ColorValue.FromColor(Color.Black));
            effect.SetValue("materialAmbientColor", ColorValue.FromColor(Color.White));
            effect.SetValue("materialDiffuseColor", ColorValue.FromColor(Color.White));
            effect.SetValue("materialSpecularColor", ColorValue.FromColor(Color.LightGray));
            effect.SetValue("materialSpecularExp", 1.0f);
        }


        /// <summary>
        /// M�todo que se llama cada vez que hay que refrescar la pantalla.
        /// Escribir aqu� todo el c�digo referido al renderizado.
        /// Borrar todo lo que no haga falta
        /// </summary>
        /// <param name="elapsedTime">Tiempo en segundos transcurridos desde el �ltimo frame</param>
        public override void render(float elapsedTime)
        {

            mirrorBall.rotateY(FastMath.QUARTER_PI * elapsedTime);

            Matrix lookAtRotationMatrix = Matrix.RotationY(FastMath.QUARTER_PI / 2 * elapsedTime);

            mirrorBallLookAtVector.TransformCoordinate(lookAtRotationMatrix);

            updateEyePosition(mirrorBall.Effect);
            updateEyePosition(outerBox.Effect);

            updateMirrorBallPosition(outerBox.Effect);

            mirrorBall.render();
            outerBox.render();

        }

        private void updateMirrorBallPosition(Effect effect)
        {
            Matrix viewMatrix = Matrix.LookAtLH(mirrorBall.Position, mirrorBallLookAtVector, new Vector3(0,1,0));

            effect.SetValue("matViewMirrorBall", viewMatrix * Matrix.Scaling(0.02f, 0.02f, 0.02f));
            effect.SetValue("matProjMirrorBall", mirrorBallProjection);
        }

        private static void updateEyePosition(Effect e)
        {
            e.SetValue("eyePosition", TgcParserUtils.vector3ToFloat4Array(GuiController.Instance.FpsCamera.getPosition()));
        }

        /// <summary>
        /// M�todo que se llama cuando termina la ejecuci�n del ejemplo.
        /// Hacer dispose() de todos los objetos creados.
        /// </summary>
        public override void close()
        {

        }

    }
}

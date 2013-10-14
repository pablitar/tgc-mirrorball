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
using AlumnoEjemplos.MultipassPrototype;

namespace AlumnoEjemplos.RideTheLightning.MultipassAndMirrorball
{
    /// <summary>
    /// Ejemplo del alumno
    /// </summary>
    public class MultipassAndMirrorball : TgcExample
    {

        //Device de DirectX para crear primitivas
        Device d3dDevice = GuiController.Instance.D3dDevice;
        //Carpeta de archivos Media del alumno
        string alumnoMediaFolder = GuiController.Instance.AlumnoEjemplosMediaDir;

        Color ambientColor = Color.FromArgb(255, 20, 20, 20);

        List<PointLight> lights = new List<PointLight>();
        Dictionary<SpotLight, Action<SpotLight, float>> spotLightTransformations = new Dictionary<SpotLight, Action<SpotLight, float>>();

        List<TgcRTLMesh> meshes = new List<TgcRTLMesh>();
        private Effect multipassEffect;

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
            return "Prototipo para experimentar con un shader multipass pero con un pase por luz";
        }

        /// <summary>
        /// Método que se llama una sola vez,  al principio cuando se ejecuta el ejemplo.
        /// Escribir aquí todo el código de inicialización: cargar modelos, texturas, modifiers, uservars, etc.
        /// Borrar todo lo que no haga falta
        /// </summary>
        public override void init()
        {
            //GuiController.Instance: acceso principal a todas las herramientas del Framework

            multipassEffect = TgcShaders.loadEffect(GuiController.Instance.AlumnoEjemplosMediaDir + "\\Shaders\\MultipassLightning.fx");

            GuiController.Instance.FpsCamera.Enable = true;

            GuiController.Instance.FpsCamera.setCamera(new Vector3(0, 0, -20), new Vector3(200, 0, 0));

            configureModifiers();

            configureLights();

            configureWalls();

        }

        private Action<SpotLight, float> rotateLeft(float speed)
        {
            return (spotLight, elapsedTime) =>
            {
                Matrix directionRotationMatrix = Matrix.RotationY(speed * elapsedTime);

                spotLight.transformDirection(directionRotationMatrix);
            };
        }

        private void configureLights()
        {

            lights.Add(new PointLight(Color.White, 0.5f, 1, new Vector3(-200, 200, 0)));
            //lights.Add(new PointLight(Color.Yellow, 0.5f, 1, new Vector3(200, 200, 0)));
            Random r = new Random();

            Vector3 directionVector = new Vector3(1, -1, 0);

            directionVector = createSpotLight(Color.Red, r, directionVector);
            directionVector = createSpotLight(Color.Green, r, directionVector);
            directionVector = createSpotLight(Color.Yellow, r, directionVector);
            directionVector = createSpotLight(Color.Beige, r, directionVector);
            createSpotLight(Color.Blue, r, directionVector);
        }

        private Vector3 createSpotLight(Color color, Random r, Vector3 directionVector)
        {
            addSpotLight(new SpotLight(color, 9f, 2, new Vector3(0, 200, 0), directionVector, FastMath.QUARTER_PI, 3), rotateLeft((float)(FastMath.TWO_PI * r.NextDouble() + 0.1)));
            directionVector.TransformCoordinate(Matrix.RotationY(FastMath.TWO_PI / 3));
            return directionVector;
        }

        private void addSpotLight(SpotLight light, Action<SpotLight, float> transformation)
        {
            lights.Add(light);

            spotLightTransformations.Add(light, transformation);
        }

        private void configureModifiers()
        {
        }

        private void configureWalls()
        {
            addMesh(createWall(new Vector3(200, 0, 0), new Vector3(20, 200, 400)));
            addMesh(createWall(new Vector3(0, 0, 200), new Vector3(400, 200, 20)));
            addMesh(createWall(new Vector3(0, 0, -200), new Vector3(400, 200, 20)));
            addMesh(createWall(new Vector3(-200, 0, 0), new Vector3(20, 200, 400)));
            addMesh(createWall(new Vector3(0, -100, 0), new Vector3(400, 20, 400)));

        }

        private void addMesh(TgcRTLMesh mesh)
        {
            meshes.Add(mesh);
        }

        private TgcRTLMesh createWall(Vector3 center, Vector3 size)
        {


            TgcRTLMesh wallMesh = new TgcRTLMesh(
                TgcBox.fromSize(center, size, TgcTexture.createTexture(alumnoMediaFolder + "\\Wall.jpg")).toMesh("outerBox"), ambientColor, Color.LightGray, Color.White, 2.0f);

            wallMesh.Lights = lights;

            wallMesh.Effect = multipassEffect;
            wallMesh.Technique = "MultiPassLight";

            return wallMesh;
        }

        private T getModifierValue<T>(string key)
        {
            return (T)GuiController.Instance.Modifiers.getValue(key);
        }


        /// <summary>
        /// Método que se llama cada vez que hay que refrescar la pantalla.
        /// Escribir aquí todo el código referido al renderizado.
        /// Borrar todo lo que no haga falta
        /// </summary>
        /// <param name="elapsedTime">Tiempo en segundos transcurridos desde el último frame</param>
        public override void render(float elapsedTime)
        {

            updateLights(elapsedTime);

            foreach (TgcRTLMesh wall in meshes)
            {

                updateEyePosition(wall.Effect);

                wall.render();
            }

        }

        private void updateLights(float elapsed)
        {
            foreach (KeyValuePair<SpotLight, Action<SpotLight, float>> entry in spotLightTransformations)
            {
                entry.Value.Invoke(entry.Key, elapsed);
            }
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

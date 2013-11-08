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
using System.IO;
using AlumnoEjemplos.RideTheLightning;

namespace AlumnoEjemplos.RideTheLightning.ManyLightsExample
{

    public class ManyLights : TgcExample
    {

        //Device de DirectX para crear primitivas
        Device d3dDevice = GuiController.Instance.D3dDevice;
        //Carpeta de archivos Media del alumno
        string ejemploMediaFolder = GuiController.Instance.AlumnoEjemplosMediaDir + "\\RideTheLightning\\";
        private Effect effect;

        TgcScene scene;

        List<RTLLight> lights;

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
            return "Ride The Lightning - Final";
        }

        /// <summary>
        /// Completar con la descripci�n del TP
        /// </summary>
        public override string getDescription()
        {
            return "2C2013 - Ejemplo de Muchas Luces de acuerdo a la consigna del trabajo pr�ctico, incorporando el agregado de una bola de espejos";
        }

        /// <summary>
        /// M�todo que se llama una sola vez,  al principio cuando se ejecuta el ejemplo.
        /// Escribir aqu� todo el c�digo de inicializaci�n: cargar modelos, texturas, modifiers, uservars, etc.
        /// Borrar todo lo que no haga falta
        /// </summary>
        public override void init()
        {
            //Se carga aqu� el efecto para compilarlo una sola vez.
            effect = TgcShaders.loadEffect(ejemploMediaFolder + "Shaders\\MultipassLightning.fx");

            GuiController.Instance.FpsCamera.Enable = true;
            GuiController.Instance.FpsCamera.MovementSpeed = 400f;
            GuiController.Instance.FpsCamera.JumpSpeed = 300f;
            GuiController.Instance.FpsCamera.setCamera(new Vector3(80, 80, 0), new Vector3(0, 80, 1));

            loadScene();
        }

        private void loadScene()
        {
            TgcSceneParser parser = new TgcSceneParser();
            TgcSceneData sceneData = parser.parseSceneFromString(File.ReadAllText(ejemploMediaFolder + "Scenes\\BarScene\\BarSceneV3-TgcScene.xml"));

            loadAndRemoveLights(sceneData);

            scene = new TgcSceneLoader().loadScene(sceneData, ejemploMediaFolder + "Scenes\\BarScene\\");
        }

        private void loadAndRemoveLights(TgcSceneData sceneData)
        {
            List<TgcMeshData> realMeshData = new List<TgcMeshData>();
            foreach (TgcMeshData data in sceneData.meshesData)
            {
                if (LightLoader.isLight(data))
                {
                    lights.Add(LightLoader.loadLight(data));
                }
                else
                {
                    realMeshData.Add(data);
                }
            }

            sceneData.meshesData = realMeshData.ToArray();
        }

        private void configureModifiers()
        {
        }

        private T getModifierValue<T>(string key)
        {
            return (T)GuiController.Instance.Modifiers.getValue(key);
        }


        /// <summary>
        /// M�todo que se llama cada vez que hay que refrescar la pantalla.
        /// Escribir aqu� todo el c�digo referido al renderizado.
        /// Borrar todo lo que no haga falta
        /// </summary>
        /// <param name="elapsedTime">Tiempo en segundos transcurridos desde el �ltimo frame</param>
        public override void render(float elapsedTime)
        {
            Effect currentShader;
            String currentTechnique;
            
            //Sin luz: Restaurar shader default
            currentShader = GuiController.Instance.Shaders.TgcMeshShader;

            foreach (TgcMesh m in scene.Meshes)
            {
                m.Effect = currentShader;
                m.Technique = currentTechnique = GuiController.Instance.Shaders.getTgcMeshTechnique(m.RenderType);
               
                m.render();
            }
        }

        /// <summary>
        /// M�todo que se llama cuando termina la ejecuci�n del ejemplo.
        /// Hacer dispose() de todos los objetos creados.
        /// </summary>
        public override void close()
        {
            effect.Dispose();
            scene.disposeAll();
        }

    }
}

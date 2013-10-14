using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TgcViewer.Utils.TgcSceneLoader;
using TgcViewer;
using Microsoft.DirectX.Direct3D;
using System.Drawing;
using Microsoft.DirectX;

namespace AlumnoEjemplos.MultipassPrototype
{

    public class TgcRTLMesh : TgcMesh
    {

        public static readonly int AMBIENT = 0;
        public static readonly int POINT = 1;
        public static readonly int SPOT = 2;
        public static readonly int MIRROR = 3;

        private List<PointLight> lights = new List<PointLight>();

        public List<PointLight> Lights
        {
            get { return lights; }
            set { lights = value; }
        }

        //TODO: Esto no admite múltiples materials
        Material material = new Material();


        public TgcRTLMesh(TgcMesh parent, Color ambientColor, Color diffuseColor, Color specularColor, float shininess)
            : base(parent.Name, parent, parent.Position, parent.Rotation, parent.Scale)
        {
            this.Enabled = true;

            material.Ambient = ambientColor;
            material.Diffuse = diffuseColor;
            material.Specular = specularColor;
            material.SpecularSharpness = shininess;

            //TODO: Autoinicializar el efecto
        }

        public void renderPass(int pass)
        {

            TgcTexture.Manager texturesManager = GuiController.Instance.TexturesManager;


            switch (renderType)
            {
                case MeshRenderType.VERTEX_COLOR:

                    texturesManager.clear(0);
                    texturesManager.clear(1);

                    drawSubset(0, pass);

                    break;

                case MeshRenderType.DIFFUSE_MAP:

                    //Hacer reset de Lightmap
                    texturesManager.clear(1);

                    drawDiffuseMap(pass, texturesManager);

                    break;

                case MeshRenderType.DIFFUSE_MAP_AND_LIGHTMAP:

                    //Cargar lightmap
                    texturesManager.shaderSet(effect, "texLightMap", lightMap);

                    drawDiffuseMap(pass, texturesManager);

                    break;
            }


        }

        //TODO: Horrible copy pasta. Hay que ver si hay alguna forma mejor de hacerlo, pero en principio necesito demasiado control.
        public new void render()
        {
            if (!enabled)
                return;

            Device device = GuiController.Instance.D3dDevice;
            TgcTexture.Manager texturesManager = GuiController.Instance.TexturesManager;

            //Aplicar transformaciones
            updateMeshTransform();

            //Cargar VertexDeclaration
            device.VertexDeclaration = vertexDeclaration;

            //Activar AlphaBlending si corresponde
            activateAlphaBlend();

            //Cargar matrices para el shader
            setShaderMatrix();
            effect.Technique = this.technique;
            effect.Begin(0);
            applyMaterial();

            renderPass(AMBIENT);

            foreach (PointLight l in this.lights)
            {
                renderLight(l);
            }


            //Finalizar shader
            effect.End();

            //Desactivar alphaBlend
            resetAlphaBlend();
        }

        private void applyMaterial()
        {
            //TODO: Emissive
            effect.SetValue("materialAmbientColor", ColorValue.FromColor(material.Ambient));
            effect.SetValue("materialDiffuseColor", ColorValue.FromColor(material.Diffuse));
            effect.SetValue("materialSpecularColor", ColorValue.FromColor(material.Specular));
            effect.SetValue("materialSpecularExp", material.SpecularSharpness);
        }

        private void renderLight(PointLight l)
        {
            l.applyToEffect(this.Effect);
            renderPass(l.kind());
        }

        private void drawDiffuseMap(int pass, TgcTexture.Manager texturesManager)
        {
            //Dibujar cada subset con su DiffuseMap correspondiente
            for (int i = 0; i < materials.Length; i++)
            {
                //Setear textura en shader
                texturesManager.shaderSet(effect, "texDiffuseMap", diffuseMaps[i]);
                //Iniciar pasada de shader
                drawSubset(i, pass);
            }
        }

        private void drawSubset(int subset, int pass)
        {
            effect.BeginPass(pass);
            d3dMesh.DrawSubset(subset);
            effect.EndPass();
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;
using TgcViewer.Utils.TgcSceneLoader;

namespace AlumnoEjemplos.MultipassPrototype
{
    public class PointLight
    {
        private Color color;

        public Color Color
        {
            get { return color; }
            set { color = value; }
        }

        private float intensity;

        public float Intensity
        {
            get { return intensity; }
            set { intensity = value; }
        }
        private float attenuation;

        public float Attenuation
        {
            get { return attenuation; }
            set { attenuation = value; }
        }
        private Vector3 position;

        public Vector3 Position
        {
            get { return position; }
            set { position = value; }
        }

        public PointLight(Color c, float intensity, float attenuation, Vector3 position)
        {
            this.color = c;
            this.intensity = intensity;
            this.attenuation = attenuation;
            this.position = position;
        }

        public virtual void applyToEffect(Effect effect)
        {
            effect.SetValue("lightColor", ColorValue.FromColor(this.color));
            effect.SetValue("lightPosition", TgcParserUtils.vector3ToFloat4Array(this.position));
            effect.SetValue("lightIntensity", this.intensity);
            effect.SetValue("lightAttenuation", this.attenuation);
        }

        public virtual int kind()
        {
            return TgcRTLMesh.POINT;
        }
    }

    public class SpotLight : PointLight
    {
        private Vector3 direction;

        public Vector3 Direction
        {
            get { return direction; }
            set { direction = value; }
        }

        private float angle;

        public float Angle
        {
            get { return angle; }
            set { angle = value; }
        }

        private float exponent;

        public float Exponent
        {
            get { return exponent; }
            set { exponent = value; }
        }

        public SpotLight(Color c, float intensity, float attenuation, Vector3 position, Vector3 direction, float angle, float exponent)
            : base(c, intensity, attenuation, position)
        {
            this.direction = direction;
            this.angle = angle;
            this.exponent = exponent;
        }

        public override void applyToEffect(Effect effect)
        {
            base.applyToEffect(effect);
            effect.SetValue("spotLightDirection", TgcParserUtils.vector3ToFloat4Array(this.direction));
            effect.SetValue("spotLightAngle", this.angle);
            effect.SetValue("spotLightExponent", this.exponent);
        }

        public void transformDirection(Matrix transformationMatrix)
        {
            direction.TransformCoordinate(transformationMatrix);
        }

        public override int kind()
        {
            return TgcRTLMesh.SPOT;
        }


    }
}

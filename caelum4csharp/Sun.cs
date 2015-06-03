using System;
using OpenGL;

namespace caelum4csharp
{
    public class Sun : IDisposable, ICameraBound
    {
        #region Fields
        private VAO sun;
        private Texture sunTexture;
        private Matrix4 modelMatrix;
        #endregion

        #region Properties
        /// <summary>
        /// The angular size of the sun.
        /// </summary>
        public float AngularSize { get; set; }

        /// <summary>
        /// Base distance of the light.
        /// </summary>
        public float Radius { get; set; }

        /// <summary>
        /// Direction of the light.
        /// </summary>
        public Vector3 Direction { get; set; }

        /// <summary>
        /// The body color of the sun.
        /// </summary>
        public Vector3 Color { get; set; }
        #endregion

        #region Constructor
        /// <summary>
        /// Create a new instance of a moon object, which could really represent
        /// nearly any billboarded celestial object.
        /// </summary>
        public Sun(float zfar, float znear)
        {
            this.AngularSize = 3.77f;
            this.Radius = (zfar + znear) / 2;

            sun = Utilities.CreateQuad(Shaders.SunShader);
            sunTexture = new Texture("tex/world/sun_disc.png");
        }
        #endregion

        #region Methods (Draw)
        /// <summary>
        /// Draw the sun, which consists of a simple billboarded quad.
        /// </summary>
        public void Draw()
        {
            float sunDistance = Radius - Radius * (float)Math.Tan(AngularSize);
            Vector3 position = -Direction * sunDistance;
            float scale = sunDistance * (float)Math.Tan(AngularSize) / 22f;

            // create a model matrix (replace this with Matrix4.CreateTranslation * Matrix4.CreateScale)
            modelMatrix = Utilities.FastMatrix4(position + cameraPosition, new Vector3(scale, scale, scale));

            Gl.ActiveTexture(TextureUnit.Texture0);
            Gl.BindTexture(sunTexture);

            Shaders.SunShader.Use();
            Shaders.SunShader["model_matrix"].SetValue(modelMatrix);
            Shaders.SunShader["color"].SetValue(Color);
            sun.Draw();
        }
        #endregion

        #region IDisposable
        protected virtual void Dispose(bool disposing)
        {
            sun.DisposeChildren = true;
            sun.Dispose();
            sunTexture.Dispose();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion

        #region ICameraBound
        private Vector3 cameraPosition;

        public void UpdatePosition(Vector3 position)
        {
            cameraPosition = new Vector3(position.x, 0, position.z);
        }
        #endregion
    }
}

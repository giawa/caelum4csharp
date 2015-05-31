using System;
using OpenGL;

namespace caelum4csharp
{
    public class Moon : IDisposable, ICameraBound
    {
        #region Fields
        private VAO moon, moonBackground;
        private Texture moonTexture;
        private Matrix4 modelMatrix;
        #endregion

        #region Properties
        /// <summary>
        /// The angular size of the moon.
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
        /// The phase of the moon (a value from 0 to 4 is permissable).
        /// </summary>
        public float Phase { get; set; }

        public Matrix4 ModelMatrix { get { return modelMatrix; } }
        #endregion

        #region Constructor
        /// <summary>
        /// Create a new instance of a moon object, which could really represent
        /// nearly any billboarded celestial object.
        /// </summary>
        public Moon(float zfar, float znear)
        {
            this.AngularSize = 3.77f;
            this.Radius = (zfar + znear) / 2;

            moonBackground = Utilities.CreateQuad(Shaders.MoonBackgroundShader);
            moon = Utilities.CreateQuad(Shaders.MoonShader);
            moonTexture = new Texture("tex/world/Moon.png");
        }
        #endregion

        #region Methods (DrawBackground and Draw)
        /// <summary>
        /// Draw the moon itself to block any stars or other celestial bodies
        /// that are behind the moon.  This pass is simply black, but is necessary
        /// to call, because it performs all of the updates for the model matrix.
        /// </summary>
        public void DrawBackground()
        {
            float moonDistance = Radius - Radius * (float)Math.Tan(AngularSize);
            Vector3 position = -Direction * moonDistance;
            float scale = moonDistance * (float)Math.Tan(AngularSize) / 16f;

            // create a model matrix (replace this with Matrix4.CreateTranslation * Matrix4.CreateScale)
            modelMatrix = Utilities.FastMatrix4(position + cameraPosition, new Vector3(scale, scale, scale));

            Gl.ActiveTexture(TextureUnit.Texture0);
            Gl.BindTexture(moonTexture);

            Shaders.MoonBackgroundShader.Use();
            Shaders.MoonBackgroundShader["model_matrix"].SetValue(modelMatrix);
            moonBackground.Draw();
        }

        /// <summary>
        /// Draw the actual moon, which consists of the moon texture and a phase.
        /// </summary>
        public void Draw()
        {
            Gl.ActiveTexture(TextureUnit.Texture0);
            Gl.BindTexture(moonTexture);

            Shaders.MoonShader.Use();
            Shaders.MoonShader["model_matrix"].SetValue(modelMatrix);
            Shaders.MoonShader["phase"].SetValue(Phase);
            moon.Draw();
        }
        #endregion

        #region IDisposable
        protected virtual void Dispose(bool disposing)
        {
            moon.DisposeChildren = true;
            moon.Dispose();
            moonBackground.DisposeChildren = true;
            moonBackground.Dispose();
            moonTexture.Dispose();
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

using System;
using System.Drawing;

using OpenGL;

namespace caelum4csharp
{
    public class SkyDome : ICameraBound, IDisposable
    {
        #region Fields
        private VAO skydome;
        private Texture gradientMap, atmRelativeDepth;
        private Bitmap skyBitmap, sunGradient;
        private float[] modelMatrix;
        private Vector3 sunDirection;
        #endregion

        #region Properties
        public float Scale { get; set; }
        #endregion

        #region Constructor
        /// <summary>
        /// Create a sky dome based on the Caelum open source plug-in for Ogre.
        /// </summary>
        /// <param name="activeCamera">The camera to which this object is bound.</param>
        public SkyDome()
        {
            // create the shader object first to make sure we didn't get any errors
            Shaders.SkydomeShader.Use();
            Shaders.SkydomeShader["gradientMap"].SetValue(0);
            Shaders.SkydomeShader["atmRelativeDepth"].SetValue(1);

            skyBitmap = new Bitmap("tex/world/EarthClearSky2.png");
            sunGradient = new Bitmap("tex/world/SunGradient.png");

            gradientMap = new Texture("tex/world/EarthClearSky2.png");
            Gl.BindTexture(gradientMap);
            Gl.TexParameteri(gradientMap.TextureTarget, TextureParameterName.TextureMagFilter, TextureParameter.Linear);
            Gl.TexParameteri(gradientMap.TextureTarget, TextureParameterName.TextureMinFilter, TextureParameter.Linear);

            atmRelativeDepth = new Texture("tex/world/AtmosphereDepth.png");
            Gl.BindTexture(atmRelativeDepth);
            Gl.TexParameteri(atmRelativeDepth.TextureTarget, TextureParameterName.TextureMagFilter, TextureParameter.Linear);
            Gl.TexParameteri(atmRelativeDepth.TextureTarget, TextureParameterName.TextureMinFilter, TextureParameter.Linear);

            // create the skydome itself
            skydome = Utilities.GradientDome(Shaders.SkydomeShader, 12);
        }
        #endregion

        #region Methods (UpdateSunDirection, Draw, GetColors)
        /// <summary>
        /// Updates the sun direction and computes the new haze color.
        /// </summary>
        /// <param name="direction"></param>
        public void UpdateSunDirection(Vector3 direction)
        {
            sunDirection = direction;

            float elevation = sunDirection.Dot(Vector3.UnitY);
            elevation = elevation * 0.5f + 0.5f;

            // update the sun direction for all shaders
            Shaders.SkydomeShader.Use();
            Shaders.SkydomeShader["sun_direction"].SetValue(direction);
            Shaders.SkydomeShader["hazeColor"].SetValue(GetFogColor().ToVector4());
        }

        /// <summary>
        /// Draw the skydome, ensuring we bind both textures that are required.
        /// </summary>
        public void Draw()
        {
            Shaders.SkydomeShader.Use();

            Gl.ActiveTexture(TextureUnit.Texture1);
            Gl.BindTexture(atmRelativeDepth);
            Gl.ActiveTexture(TextureUnit.Texture0);
            Gl.BindTexture(gradientMap);

            skydome.Draw();
        }

        public Color GetFogColor()
        {
            if (skyBitmap == null) return Color.Black;

            float elevation = sunDirection.Dot(Vector3.UnitY) * 0.5f + 0.5f;
            return Utilities.GetInterpolatedColor(elevation, 1, skyBitmap, false);
        }

        public float GetFogDensity()
        {
            if (skyBitmap == null) return 0;

            float elevation = sunDirection.Dot(Vector3.UnitY) * 0.5f + 0.5f;
            return Utilities.GetInterpolatedColor(elevation, 1, skyBitmap, false).A / 255.0f;
        }

        public Color GetSunSphereColor()
        {
            if (sunGradient == null) return Color.White;

            float elevation = sunDirection.Dot(Vector3.UnitY) * 2 + .4f;
            return Utilities.GetInterpolatedColor(elevation, 1, sunGradient, false);
        }

        public Color GetSunLightColor()
        {
            if (skyBitmap == null) return Color.White;

            float elevation = sunDirection.Dot(Vector3.UnitY) * 0.5f + 0.5f;
            Color col = Utilities.GetInterpolatedColor(elevation, elevation, skyBitmap, false);
            int val = (int)Math.Floor((col.R + col.G + col.B) / 3.0f);
            return Color.FromArgb(val, val, val, 1);
        }
        #endregion

        #region ICameraBound
        public void UpdatePosition(Vector3 position)
        {
            // update the position of the skydome
            modelMatrix = Utilities.FastMatrix4(new Vector3(position.x, position.y, position.z), Vector3.UnitScale * Scale).ToFloat();

            Shaders.SkydomeShader.Use();
            Shaders.SkydomeShader["model_matrix"].SetValue(modelMatrix);
        }
        #endregion

        #region IDisposable
        protected virtual void Dispose(bool disposing)
        {
            sunGradient.Dispose();

            skydome.DisposeChildren = true;
            skydome.Dispose();

            gradientMap.Dispose();
            atmRelativeDepth.Dispose();

            skyBitmap.Dispose();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}

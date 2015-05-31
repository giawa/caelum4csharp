using System;
using System.Drawing;

using OpenGL;

namespace caelum4csharp
{
    public class Clouds : ICameraBound, IDisposable
    {
        #region Fields
        private Texture[] noiseTextures;    // list of the noise textures
        private VAO cloudVAO;               // contains the vbo plane object
        private Vector3 sunDirection;       // direction of the sun
        private int currentTextureIndex;    // current noise texture
        private Matrix4 modelMatrix;
        private Texture cloud_shape1, cloud_shape2;
        #endregion

        #region Properties
        public Vector2 CloudSpeed { get; set; }

        public Vector2 CloudMassOffset { get; private set; }

        public Vector2 CloudDetailOffset { get; private set; }

        public Vector3 SunLightColor { get; private set; }

        public Vector3 SunSphereColor { get; private set; }

        public Vector3 FogColor { get; private set; }

        public float MeshWidth { get; private set; }

        public float MeshHeight { get; private set; }

        public int MeshWidthSegments { get; private set; }

        public int MeshHeightSegments { get; private set; }

        public Bitmap CloudCoverLookup { get; private set; }

        public float Height { get; private set; }

        public float CloudBlendPos { get; private set; }

        public float CloudBlendTime { get; set; }

        public float CloudUVFactor { get; private set; }

        public float HeightRedFactor { get; private set; }

        public float NearFadeDistance { get; private set; }

        public float FarFadeDistance { get; private set; }

        public Vector3 FadeDistanceMeasurement { get; private set; }

        public float CloudCover { get; private set; }
        #endregion

        #region Shader Program Parameters
        private ProgramParam pCloudCoverageThreshold;
        private ProgramParam pCloudMassOffset;
        private ProgramParam pCloudDetailOffset;
        private ProgramParam pCloudMassBlend;
        private ProgramParam pVPSunDirection;
        private ProgramParam pFPSunDirection;
        private ProgramParam pSunLightColor;
        private ProgramParam pSunSphereColor;
        private ProgramParam pFogColor;
        private ProgramParam pCloudUVFactor;
        private ProgramParam pHeightRedFactor;
        private ProgramParam pCloudMassInvScale;
        private ProgramParam pCloudDetailInvScale;
        private ProgramParam pCloudDetailBlend;
        private ProgramParam pCloudSharpness;
        private ProgramParam pCloudThickness;
        #endregion

        public Clouds()
        {
            Shaders.CloudShader.Use();

            // Cache all the program param locations
            pCloudMassInvScale = Shaders.CloudShader["cloudMassInvScale"];
            pCloudDetailInvScale = Shaders.CloudShader["cloudDetailInvScale"];
            pCloudCoverageThreshold = Shaders.CloudShader["cloudCoverageThreshold"];
            pCloudDetailOffset = Shaders.CloudShader["cloudDetailOffset"];
            pCloudMassBlend = Shaders.CloudShader["cloudMassBlend"];
            pCloudMassOffset = Shaders.CloudShader["cloudMassOffset"];
            pCloudUVFactor = Shaders.CloudShader["cloudUVFactor"];
            pFogColor = Shaders.CloudShader["fogColour"];
            pFPSunDirection = Shaders.CloudShader["sunDirection"];
            pHeightRedFactor = Shaders.CloudShader["heightRedFactor"];
            pSunLightColor = Shaders.CloudShader["sunLightColour"];
            pSunSphereColor = Shaders.CloudShader["sunSphereColour"];
            pVPSunDirection = Shaders.CloudShader["vSunDirection"];
            pCloudDetailBlend = Shaders.CloudShader["cloudDetailBlend"];
            pCloudSharpness = Shaders.CloudShader["cloudSharpness"];
            pCloudThickness = Shaders.CloudShader["cloudThickness"];

            Shaders.CloudShader["cloud_shape1"].SetValue(0);
            Shaders.CloudShader["cloud_shape2"].SetValue(1);
            Shaders.CloudShader["cloud_detail"].SetValue(2);

            noiseTextures = new Texture[4];
            noiseTextures[0] = new Texture("tex/world/noise1.png");
            noiseTextures[1] = new Texture("tex/world/noise2.png");
            noiseTextures[2] = new Texture("tex/world/noise3.png");
            noiseTextures[3] = new Texture("tex/world/noise4.png");

            for (int i = 0; i < noiseTextures.Length; i++)
            {
                Gl.BindTexture(noiseTextures[i]);
                Gl.TexParameteri(noiseTextures[i].TextureTarget, TextureParameterName.TextureMagFilter, TextureParameter.Linear);
                Gl.TexParameteri(noiseTextures[i].TextureTarget, TextureParameterName.TextureMinFilter, TextureParameter.Linear);
            }

            SetCloudCoverLookup("tex/world/CloudCoverLookup.png");

            currentTextureIndex = -1;

            SetHeight(150);
            Reset();
        }

        #region Methods
        /// <summary>
        /// Resets the cloud to the default settings.  This overrides all shader uniform values,
        /// rebuilds the geometry of the CloudMesh and resets the current noise texture (causing
        /// texture snapping).  This will not, however, reload the CloudCoverLookup or NoiseTextures.
        /// </summary>
        public void Reset()
        {
            Shaders.CloudShader.Use();

            SetMeshParameters(1000000, 1000000, 10, 10);

            pCloudMassInvScale.SetValue(1.2f);
            pCloudDetailInvScale.SetValue(4.8f);
            pCloudDetailBlend.SetValue(0.5f);
            pCloudSharpness.SetValue(4f);
            pCloudThickness.SetValue(1f);

            SetCloudCover(0.7f);

            SetCloudMassOffset(new Vector2(0, 0));
            SetCloudDetailOffset(new Vector2(0.5, 0.5));
            SetCloudBlendPos(0.9f);

            this.CloudBlendTime = 3600 * 24;
            this.CloudSpeed = new Vector2(0.000005, -0.000009);

            SetCloudUVFactor(50);
            SetHeightRedFactor(100000);

            SetSunDirection(Vector3.UnitY);
            SetFogColor(Vector3.UnitScale);
            SetSunLightColor(Vector3.UnitScale);
            SetSunSphereColor(Vector3.UnitScale);
        }

        /// <summary>
        /// Normally called once per frame, this updates the cloud with new colors and animation.
        /// </summary>
        /// <param name="sunDirection">The direction of the sun for computing cloud color.</param>
        /// <param name="sunLightColor">The color of the sun for computing cloud color.</param>
        /// <param name="fogColor">The color of the fog for computing cloud color.</param>
        /// <param name="sunSphereColor">The color of the sun sprite/sphere for computing cloud color.</param>
        public void Update(float time, Vector3 sunDirection, Vector3 sunLightColor, Vector3 fogColor, Vector3 sunSphereColor)
        {
            Shaders.CloudShader.Use();

            // Advance the animation
            AdvanceAnimation(time);

            // set params
            SetSunDirection(sunDirection);
            SetSunLightColor(sunLightColor);
            SetSunSphereColor(sunSphereColor);
            SetFogColor(fogColor);
        }

        /// <summary>
        /// Advances the animation by moving the clouds and setting a new blend position.
        /// </summary>
        private void AdvanceAnimation(float timePassed)
        {
            Shaders.CloudShader.Use();

            // Move clouds
            SetCloudMassOffset(CloudMassOffset + timePassed * CloudSpeed);
            SetCloudDetailOffset(CloudDetailOffset - timePassed * CloudSpeed);

            // Animate cloud blending
            SetCloudBlendPos(CloudBlendPos + timePassed / CloudBlendTime);
        }

        /// <summary>
        /// Disables looking up cloud cover via a bitmap.
        /// </summary>
        public void DisableCloudCoverLookup()
        {
            if (CloudCoverLookup != null) CloudCoverLookup.Dispose();
            CloudCoverLookup = null;
        }

        private void EnsureGeometry()
        {
            if (cloudVAO != null)
            {
                cloudVAO.DisposeChildren = true;
                cloudVAO.Dispose();
            }

            cloudVAO = Utilities.CreateMesh(Shaders.CloudShader, MeshWidth, MeshHeight, MeshWidthSegments, MeshHeightSegments, 1, 1);
        }

        public void Draw()
        {
            Shaders.CloudShader.Use();
            Shaders.CloudShader["model_matrix"].SetValue(modelMatrix);
            pCloudCoverageThreshold.SetValue(cloudCoverThreshold);

            Gl.ActiveTexture(TextureUnit.Texture2);
            Gl.BindTexture(noiseTextures[3]);
            Gl.ActiveTexture(TextureUnit.Texture1);
            Gl.BindTexture(cloud_shape2);
            Gl.ActiveTexture(TextureUnit.Texture0);
            Gl.BindTexture(cloud_shape1);

            cloudVAO.Draw();
        }
        #endregion

        #region Public Setters
        /// <summary>
        /// Sets the variables for the plane to be loaded as a VBO mesh.  Will call ensureGeometry() with MeshDirty=true.
        /// </summary>
        public void SetMeshParameters(float meshWidth, float meshHeight, int meshWidthSegments, int meshHeightSegments)
        {
            bool invalidate = (this.MeshWidth != meshWidth || this.MeshHeight != meshHeight ||
                this.MeshWidthSegments != meshWidthSegments || this.MeshHeightSegments != meshHeightSegments);
            this.MeshWidth = meshWidth;
            this.MeshHeight = meshHeight;
            this.MeshWidthSegments = meshWidthSegments;
            this.MeshHeightSegments = meshHeightSegments;

            if (invalidate) EnsureGeometry();
        }

        /// <summary>
        /// Disposes of the old CloudCoverLookup bitmap and loads a new one.
        /// </summary>
        public void SetCloudCoverLookup(string FileName)
        {
            try
            {
                if (CloudCoverLookup != null) DisableCloudCoverLookup();
                CloudCoverLookup = (Bitmap)Image.FromFile(FileName);
            }
            catch (Exception ex)
            {
                //Logger.Instance.WriteLine(LogFlags.Error | LogFlags.FileSystem, "Error loading CloudCoverLookup " + FileName + ".  " + ex.Message);
            }
        }

        private float cloudCoverThreshold;

        /// <summary>
        /// Sets the cloudCover variable, which determines how thick the clouds are.
        /// 0.0 are no cloud, 0.25 is lightly cloudy, 1.0 is very cloudy.
        /// </summary>
        public void SetCloudCover(float cloudCover)
        {
            this.CloudCover = cloudCover;
            if (CloudCoverLookup != null) cloudCoverThreshold = Utilities.GetInterpolatedColor(cloudCover, 1, CloudCoverLookup, false).R / 255f;
            else cloudCoverThreshold = 1 - cloudCover;
        }

        /// <summary>
        /// Sets the CloudMassOffset, which is used in animating the movement of the mass of the clouds.
        /// </summary>
        public void SetCloudMassOffset(Vector2 cloudMassOffset)
        {
            this.CloudMassOffset = cloudMassOffset;
            pCloudMassOffset.SetValue(cloudMassOffset);
        }

        /// <summary>
        /// Sets the CloudDetailOffset, which is used in animating the movement of the clouds.
        /// </summary>
        /// <param name="cloudDetailOffset"></param>
        public void SetCloudDetailOffset(Vector2 cloudDetailOffset)
        {
            this.CloudDetailOffset = cloudDetailOffset;
            pCloudDetailOffset.SetValue(cloudDetailOffset);
        }

        /// <summary>
        /// The clouds are constantly a blend between two different noise textures.
        /// This blend position ranges from 0.0 (full texture0) to 1.0 (full texture1).
        /// When a blend position hits 1.0, a new texture is loaded and the blend is reset.
        /// This can be overridden, but will cause the clouds to 'snap'.  Leaving this alone
        /// is wisest, unless you want to speed up the blending to a new noise texture.
        /// </summary>
        public void SetCloudBlendPos(float cloudBlendPos)
        {
            this.CloudBlendPos = cloudBlendPos;
            int textureCount = noiseTextures.Length;

            // Convert to int and bring to [0, textureCount]
            int currentTextureIndex = (int)Math.Floor(CloudBlendPos);
            currentTextureIndex = ((currentTextureIndex % textureCount) + textureCount) % textureCount;
            if (0 > currentTextureIndex || currentTextureIndex >= textureCount)
            {
                //Logger.Instance.WriteLine(LogFlags.Error, "Error while setting CloudBlendPos.  CurrentTextureIndex=" + currentTextureIndex + ", TextureCount=" + textureCount);
                return;
            }

            // Check if we have to change textures
            if (currentTextureIndex != this.currentTextureIndex)
            {
                cloud_shape1 = noiseTextures[currentTextureIndex];
                cloud_shape2 = noiseTextures[(currentTextureIndex + 1) % textureCount];
                this.currentTextureIndex = currentTextureIndex;
            }

            float cloudMassBlend = CloudBlendPos % 1f;
            pCloudMassBlend.SetValue(cloudMassBlend);
        }

        /// <summary>
        /// Sets the sun direction, which affects the cloud color via the cloud lookup bitmap.
        /// </summary>
        public void SetSunDirection(Vector3 sunDirection)
        {
            this.sunDirection = sunDirection;
            pFPSunDirection.SetValue(sunDirection);
            pVPSunDirection.SetValue(sunDirection);
        }

        /// <summary>
        /// Sets the sunlight color, which affects the cloud color if the sun is glowing on the clouds.
        /// </summary>
        public void SetSunLightColor(Vector3 sunLightColor)
        {
            //pSunLightColor.SetValue(this.SunLightColor = sunLightColor);
        }

        /// <summary>
        /// Sets the color of the sunlight sprite or sphere, which affects the cloud color.
        /// </summary>
        public void SetSunSphereColor(Vector3 sunSphereColor)
        {
            pSunSphereColor.SetValue(this.SunSphereColor = sunSphereColor);
        }

        /// <summary>
        /// Sets the color of the cloud, which is then mixed with the sunlight colors for a final cloud color.
        /// </summary>
        public void SetFogColor(Vector3 fogColor)
        {
            pFogColor.SetValue(this.FogColor = fogColor);
        }

        /// <summary>
        /// Set the height of the cloud, rain clouds are lower, cirrus (puffy) clouds are higher.
        /// </summary>
        public void SetHeight(float height)
        {
            modelMatrix = Matrix4.CreateTranslation(new Vector3(0, height, 0));
            this.Height = height;
        }

        /// <summary>
        /// Set the factor to multiply the UV co-ordinates by.  This allows more detail in the clouds.
        /// </summary>
        public void SetCloudUVFactor(float cloudUVFactor)
        {
            pCloudUVFactor.SetValue(this.CloudUVFactor = cloudUVFactor);
        }

        /// <summary>
        /// Set the cloud red factor, which gives the clouds a red hue the higher they are (due to atmospheric scattering).
        /// </summary>
        public void SetHeightRedFactor(float heightRedFactor)
        {
            pHeightRedFactor.SetValue(this.HeightRedFactor = heightRedFactor);
        }
        #endregion

        #region IDisposable
        protected virtual void Dispose(bool disposing)
        {
            for (int i = 0; i < noiseTextures.Length; i++) noiseTextures[i].Dispose();

            cloudVAO.DisposeChildren = true;
            cloudVAO.Dispose();
        }

        /// <summary>
        /// Dispose of this cloud layer and all of its resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion

        #region ICameraBound
        public void UpdatePosition(Vector3 position)
        {
            modelMatrix = Matrix4.CreateTranslation(new Vector3(position.x, Height, position.z));
        }
        #endregion
    }
}

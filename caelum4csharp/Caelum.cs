using System;
using OpenGL;

namespace caelum4csharp
{
    public class Caelum : ICameraBound, IDisposable
    {
        private SkyDome skyDome;
        private Starfield starField;
        private Moon moon;
        private Sun sun;
        private Clouds cloudLayer;

        /// <summary>
        /// Sets the julian time of the Caelum system, which determines the 
        /// sun/moon position and phase as well as the time of day.
        /// </summary>
        public double JulianTime { get; set; }

        /// <summary>
        /// The current haze color (this is the color you should use
        /// for atmospheric blending of far away objects).
        /// </summary>
        public Vector4 HazeColor { get; private set; }

        /// <summary>
        /// The current sun color.
        /// </summary>
        public Vector3 SunColor { get; private set; }

        /// <summary>
        /// The current sun direction, which should be used by your lighting
        /// shaders.
        /// </summary>
        public Vector3 SunDirection { get; private set; }

        /// <summary>
        /// The moon direction, which should be used by your lighting
        /// shaders when the sun contribution is low enough.
        /// </summary>
        public Vector3 MoonDirection { get; private set; }

        /// <summary>
        /// Sets the height of the clouds.
        /// </summary>
        public float CloudHeight
        {
            get { return cloudLayer.Height; }
            set { cloudLayer.SetHeight(value); }
        }

        /// <summary>
        /// Sets the amount of cloud cover (from 0 to 1) where 0 is
        /// no cloud cover and 1 is full cloud cover.
        /// </summary>
        public float CloudCover
        {
            get { return cloudLayer.CloudCover; }
            set { cloudLayer.SetCloudCover(value); }
        }

        public Caelum(int screenx, int screeny, float znear, float zfar)
        {
            // load all of our shaders
            Shaders.InitShaders();

            // initialize the time system
            Time.Init();

            // create all of the sky components
            skyDome = new SkyDome();
            starField = new Starfield(screenx, screeny);
            moon = new Moon(zfar, znear);
            sun = new Sun(zfar, znear);
            cloudLayer = new Clouds();

            // set up the scaling of each of the billboards/domes
            float scale = (zfar + znear) / 2;
            starField.Scale = scale;
            skyDome.Scale = scale;
            moon.Radius = scale;
            sun.Radius = scale;

            // set up some default values for the clouds
            CloudHeight = 250f;
            CloudCover = 0.2f;
        }

        private float timeScale = 0.02f;

        public void Render()
        {
            Time.Update();

            JulianTime += timeScale * Time.DeltaTime;

            double relDayTime = Math.IEEERemainder(JulianTime, 1);
            Vector3 fogColor = skyDome.GetFogColor().ToVector3();
            Vector3 sunLightColor = skyDome.GetSunLightColor().ToVector3();
            Vector3 sunSphereColor = skyDome.GetSunSphereColor().ToVector3();
            Vector4 hazeColor = skyDome.GetFogColor().ToVector4();
            SunColor = fogColor;
            HazeColor = hazeColor;

            SunDirection = Astronomy.GetSunDirection(JulianTime);
            MoonDirection = Astronomy.GetMoonDirection(JulianTime);

            skyDome.UpdateSunDirection(SunDirection);
            moon.Direction = MoonDirection;
            moon.Phase = Astronomy.GetMoonPhase(JulianTime);
            sun.Direction = SunDirection;
            sun.Color = sunSphereColor;

            Gl.Enable(EnableCap.Blend);
            Gl.Disable(EnableCap.DepthTest);
            Gl.DepthMask(false);

            starField.Draw();
            moon.DrawBackground();
            skyDome.Draw();
            moon.Draw();
            sun.Draw();

            cloudLayer.Update(Time.DeltaTime * timeScale * 100, SunDirection, sunLightColor, fogColor, sunSphereColor);
            cloudLayer.Draw();

            Gl.DepthMask(true);
            Gl.Disable(EnableCap.Blend);
            Gl.Enable(EnableCap.DepthTest);
        }

        public void UpdatePosition(Vector3 position)
        {
            skyDome.UpdatePosition(position);
            starField.UpdatePosition(position);
            sun.UpdatePosition(position);
            cloudLayer.UpdatePosition(position);
        }

        public void Dispose()
        {
            skyDome.Dispose();
            starField.Dispose();
            moon.Dispose();
            sun.Dispose();
            cloudLayer.Dispose();

            Shaders.DisposeShaders();
        }
    }
}

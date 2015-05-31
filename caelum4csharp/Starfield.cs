using System;
using OpenGL;

namespace caelum4csharp
{
    public class Starfield : ICameraBound, IDisposable
    {
        #region Star Struc[kt]
        private struct Star
        {
            public float RightAscension;
            public float Declination;
            public float Magnitude;

            public Star(float ra, float dec, float mag)
            {
                RightAscension = ra;
                Declination = dec;
                Magnitude = mag;
            }
        }
        #endregion

        #region Fields
        private VAO starfield;
        private Vector3 position;
        private Matrix4 modelMatrix;
        private float angle = 0;
        private float mag0PixelSize = 16;
        private float minPixelSize = 4;
        private float maxPixelSize = 6;
        private float magnitudeScale = (float)Math.Pow(100, 0.2);
        #endregion

        #region Properties
        public float Scale { get; set; }
        #endregion

        #region Constructor
        /// <summary>
        /// Create a star field, which consists of 5000 stars by default.
        /// </summary>
        /// <param name="activeCamera">The camera to which this star field is bound.</param>
        /// <param name="numStars">The number of stars to place in the star field.</param>
        public Starfield(int screenWidth, int screenHeight, int numStars = 5000)
        {
            float pixFactor = 1.0f / screenWidth;
            float magScale = -(float)Math.Log(magnitudeScale) / 2;
            float mag0Size = mag0PixelSize * pixFactor;
            float minSize = minPixelSize * pixFactor;
            float maxSize = maxPixelSize * pixFactor;
            float aspectRatio = (float)screenWidth / screenHeight;

            Shaders.StarfieldShader.Use();
            Shaders.StarfieldShader["mag_scale"].SetValue(magScale);
            Shaders.StarfieldShader["mag0_size"].SetValue(mag0Size);
            Shaders.StarfieldShader["min_size"].SetValue(minSize);
            Shaders.StarfieldShader["max_size"].SetValue(maxSize);
            Shaders.StarfieldShader["aspect_ratio"].SetValue(aspectRatio);

            // add some stars to the scene
            AddRandomStars(numStars);
        }
        #endregion

        #region Random Star Placement
        private Random rand = new Random(Environment.TickCount);

        private float randReal()
        {
            return (float)rand.NextDouble();
        }

        private float randReal(float min, float max)
        {
            float f = randReal();
            return min * (1 - f) + max * f;
        }

        /// <summary>
        /// Add a random number of stars to the star field.
        /// </summary>
        /// <param name="count">The number of stars to add.</param>
        public void AddRandomStars(int count)
        {
            Star[] stars = new Star[count];

            for (int i = 0; i < stars.Length; i++)
            {
                Vector3 pos;
                do
                {
                    pos.x = randReal(-1, 1);
                    pos.y = randReal(-1, 1);
                    pos.z = randReal(-1, 1);
                } while (pos.SquaredLength >= 1);

                // Convert to rasc/decl angles
                double rasc, decl, dist;
                Astronomy.convertRectangularToSpherical(pos.x, pos.y, pos.z, out rasc, out decl, out dist);

                stars[i] = new Star((float)rasc, (float)decl, 6 * pos.SquaredLength + 1.5f);   // used to be a factor of 6...
            }

            if (starfield != null)
            {
                starfield.DisposeChildren = true;
                starfield.Dispose();
            }

            // and allocate all the space for the vertices/UV/faces
            Vector3[] vertices = new Vector3[6 * stars.Length];
            Vector3[] normals = new Vector3[6 * stars.Length];
            int[] elements = new int[6 * stars.Length];

            for (int i = 0; i < stars.Length; i++)
            {
                var star = stars[i];
                double azm, alt;

                // Determine position at J2000
                Astronomy.convertEquatorialToHorizontal(Astronomy.J2000, Astronomy.ObserverLatitude, Astronomy.ObserverLongitude, star.RightAscension, star.Declination, out azm, out alt);

                // Math function requires radians, so I make sure to convert from deg to rad.
                Vector3 pos = new Vector3(-Math.Cos(azm * Math.PI / 180) * Math.Cos(alt * Math.PI / 180), Math.Sin(azm * Math.PI / 180) * Math.Cos(alt * Math.PI / 180), Math.Sin(alt * Math.PI / 180));

                vertices[i * 6] = pos;
                normals[i * 6] = new Vector3(+1, -1, star.Magnitude);
                vertices[i * 6 + 1] = pos;
                normals[i * 6 + 1] = new Vector3(+1, +1, star.Magnitude);
                vertices[i * 6 + 2] = pos;
                normals[i * 6 + 2] = new Vector3(-1, -1, star.Magnitude);

                vertices[i * 6 + 3] = pos;
                normals[i * 6 + 3] = new Vector3(-1, -1, star.Magnitude);
                vertices[i * 6 + 4] = pos;
                normals[i * 6 + 4] = new Vector3(+1, +1, star.Magnitude);
                vertices[i * 6 + 5] = pos;
                normals[i * 6 + 5] = new Vector3(-1, +1, star.Magnitude);
            }

            for (int i = 0; i < elements.Length; i++) elements[i] = i;

            starfield = new VAO(Shaders.StarfieldShader, new VBO<Vector3>(vertices), new VBO<Vector3>(normals), new VBO<int>(elements, BufferTarget.ElementArrayBuffer, BufferUsageHint.StaticDraw));
        }
        #endregion

        #region Methods (Draw)
        public void Draw()
        {
            angle += Time.DeltaTime / 20f * 0.1f;

            // create the model matrix (replace with Matrix4.CreateTranslation * Matrix4.CreateRotation)
            modelMatrix = Utilities.FastMatrix4(position, Vector3.UnitScale * Scale, new Vector3(0.1, -0.9, 0.3).Normalize(), angle);

            Shaders.StarfieldShader.Use();
            Shaders.StarfieldShader["model_matrix"].SetValue(modelMatrix);
            starfield.Draw();
        }
        #endregion

        #region ICameraBound
        public void UpdatePosition(Vector3 position)
        {
            this.position = new Vector3(position.x, position.y, position.z);
        }
        #endregion

        #region IDisposable
        protected virtual void Dispose(bool disposing)
        {
            starfield.DisposeChildren = true;
            starfield.Dispose();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}

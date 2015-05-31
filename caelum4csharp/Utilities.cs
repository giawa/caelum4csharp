using System;
using System.Drawing;

using OpenGL;

namespace caelum4csharp
{
    public static class Utilities
    {
        /// <summary>
        /// Create a basic quad by storing two triangles into a VAO.
        /// This quad includes UV co-ordinates from 0,0 to 1,1.
        /// </summary>
        /// <param name="program">The ShaderProgram assigned to this quad.</param>
        /// <returns>The VAO object representing this quad.</returns>
        public static VAO CreateQuad(ShaderProgram program)//, Vector2 location, Vector2 size)
        {
            VBO<Vector3> vertices = new VBO<Vector3>(new Vector3[] { new Vector3(-1, -1, 0), new Vector3(1, -1, 0), new Vector3(1, 1, 0), new Vector3(-1, 1, 0) });
            VBO<Vector2> uvs = new VBO<Vector2>(new Vector2[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1) });
            VBO<int> indices = new VBO<int>(new int[] { 0, 1, 3, 1, 2, 3 }, BufferTarget.ElementArrayBuffer);

            return new VAO(program, vertices, uvs, indices);
        }

        /// <summary>
        /// Returns a translation and scaling matrix quickly.
        /// </summary>
        /// <param name="position">The translation to apply to this matrix.</param>
        /// <param name="scale">The scaling to apply to this matrix.</param>
        /// <returns>A matrix consisting of both translation and scaling.</returns>
        public static Matrix4 FastMatrix4(Vector3 position, Vector3 scale)
        {
            return new Matrix4(new Vector4(scale.x, 0, 0, 0), new Vector4(0, scale.y, 0, 0), new Vector4(0, 0, scale.z, 0), new Vector4(position.x, position.y, position.z, 1));
        }

        /// <summary>
        /// Returns a combined translation, rotation and scaling matrix quickly.
        /// </summary>
        /// <param name="position">The translation to apply to this matrix.</param>
        /// <param name="scale">The scaling to apply to this matrix.</param>
        /// <param name="axis">The axis angle to apply during the rotation.</param>
        /// <param name="angle">The angle (in radians) of the rotation.</param>
        /// <returns>A matrix consisting of translation, rotation and scaling.</returns>
        public static Matrix4 FastMatrix4(Vector3 position, Vector3 scale, Vector3 axis, float angle)
        {
            Matrix4 matrix = Matrix4.CreateRotation(axis, angle);
            matrix[0] *= scale.x;
            matrix[1] *= scale.y;
            matrix[2] *= scale.z;
            matrix[3] = new Vector4(position.x, position.y, position.z, 1);
            return matrix;
        }

        /// <summary>
        /// Get the interpolated color between pixels in a bitmap.
        /// </summary>
        /// <param name="fx">The location (between 0 and 1) of the pixel.</param>
        /// <param name="fy">The location (between 0 and 1) of the pixel.</param>
        /// <param name="img">The bitmap to perform the interpolation on.</param>
        /// <param name="wrapX">True if the x value should wrap around.</param>
        /// <returns>An interpolated color at position fx, fy.</returns>
        public static Color GetInterpolatedColor(float fx, float fy, Bitmap img, bool wrapX)
        {
            int imgWidth = img.Width;
            int imgHeight = img.Height;

            // Calculate pixel y coord.
            int py = (int)Math.Floor(Math.Abs(fy) * (imgHeight - 1));

            // Snap to py image bounds.
            py = (int)Math.Max(0, Math.Min(py, imgHeight - 1));

            // Get the two closest pixels on x.
            // px1 and px2 are the closest integer pixels to px.
            float px = fx * (imgWidth - 1);
            int px1 = (int)Math.Floor(px);
            int px2 = (int)Math.Ceiling(px);

            // Wrap x coords. The funny addition ensures that it does
            // "the right thing" for negative values.
            if (wrapX)
            {
                px1 = (px1 % imgWidth + imgWidth) % imgWidth;
                px2 = (px2 % imgWidth + imgWidth) % imgWidth;
            }
            else
            {
                px1 = (int)Math.Max(0, Math.Min(px1, imgWidth - 1));
                px2 = (int)Math.Max(0, Math.Min(px2, imgWidth - 1));
            }

            // Calculate the interpolated pixel
            Color c1 = img.GetPixel(px1, py);
            Color c2 = img.GetPixel(px2, py);

            // Blend the two pixels together
            float diff = px - px1;

            int r = (int)(c1.R * (1 - diff) + c2.R * diff);
            int g = (int)(c1.G * (1 - diff) + c2.G * diff);
            int b = (int)(c1.B * (1 - diff) + c2.B * diff);

            return Color.FromArgb(r, g, b);
        }

        /// <summary>
        /// Convert a standard color to a vector4 representation.
        /// The .w components contains the alpha channel.
        /// </summary>
        public static Vector4 ToVector4(this Color color)
        {
            return new Vector4(color.R / 255f, color.G / 255f, color.B / 255f, color.A / 255f);
        }

        /// <summary>
        /// Convert a standard color to a vector3 representation.
        /// </summary>
        public static Vector3 ToVector3(this Color color)
        {
            return new Vector3(color.R / 255f, color.G / 255f, color.B / 255f);
        }

        /// <summary>
        /// Builds a simple dome suitable for creating a smooth gradient to the horizon.
        /// </summary>
        /// <param name="program">The shader program that will be used with this dome.</param>
        /// <param name="segments">The number of segments in the dome.</param>
        /// <returns>A VAO that contains the vertices, uv co-ordinates and elements to draw the dome.</returns>
        public static VAO GradientDome(ShaderProgram program, int segments)
        {
            // allocate our vertex, uv and element arrays
            Vector3[] vertices = new Vector3[segments * (segments - 1) + 2];
            Vector2[] uvs = new Vector2[segments * (segments - 1) + 2];
            int[] elements = new int[2 * segments * (segments - 1) * 3];

            double deltaLatitude = Math.PI / segments;
            double deltaLongitude = Math.PI * 2.0 / segments;
            int index = 0;

            // create the rings of the dome using polar coordinates
            for (int i = 1; i < segments; i++)
            {
                double r0 = Math.Sin(i * deltaLatitude);
                double y0 = Math.Cos(i * deltaLatitude);

                for (int j = 0; j < segments; j++)
                {
                    double x0 = r0 * Math.Sin(j * deltaLongitude);
                    double z0 = r0 * Math.Cos(j * deltaLongitude);

                    vertices[index] = new Vector3(x0, y0, z0);
                    uvs[index++] = new Vector2(0, 1.0f - (float)y0);
                }
            }

            // create the top of the dome
            vertices[index] = new Vector3(0, 1, 0);
            uvs[index++] = new Vector2(0, 0);

            // create the bottom of the dome
            vertices[index] = new Vector3(0, -1, 0);
            uvs[index] = new Vector2(0, 2);

            // create the faces of the rings
            index = 0;
            for (int i = 0; i < segments - 2; i++)
            {
                for (int j = 0; j < segments; j++)
                {
                    elements[index++] = segments * i + j;
                    elements[index++] = segments * i + (j + 1) % segments;
                    elements[index++] = segments * (i + 1) + (j + 1) % segments;
                    elements[index++] = segments * i + j;
                    elements[index++] = segments * (i + 1) + (j + 1) % segments;
                    elements[index++] = segments * (i + 1) + j;
                }
            }

            // create the faces of the top of the dome
            for (int i = 0; i < segments; i++)
            {
                elements[index++] = segments * (segments - 1);
                elements[index++] = (i + 1) % segments;
                elements[index++] = i;
            }

            // create the faces of the bottom of the dome
            for (int i = 0; i < segments; i++)
            {
                elements[index++] = segments * (segments - 1) + 1;
                elements[index++] = segments * (segments - 2) + i;
                elements[index++] = segments * (segments - 2) + (i + 1) % segments;
            }

            Vector3[] normals = Geometry.CalculateNormals(vertices, elements);
            return new VAO(program, new VBO<Vector3>(vertices), new VBO<Vector3>(normals), new VBO<Vector2>(uvs), new VBO<int>(elements));
        }

        /// <summary>
        /// Builds a simple dome suitable for a point star field.
        /// </summary>
        /// <param name="program">The shader program that will be used with this dome.</param>
        /// <param name="segments">The number of segments in the dome.</param>
        /// <returns>A VAO that contains the vertices, uv co-ordinates and elements to draw the dome.</returns>
        public static VAO StarfieldDome(ShaderProgram program, int segments)
        {
            // Allocate the buffers
            Vector3[] vertices = new Vector3[(segments + 1) * (segments + 1)];
            Vector2[] uvs = new Vector2[(segments + 1) * (segments + 1)];
            int[] elements = new int[2 * (segments - 1) * segments * 3];

            double deltaLatitude = Math.PI / segments;
            double deltaLongitude = Math.PI * 2.0 / segments;
            int index = 0, vRowSize = segments + 1;

            // Generate the rings
            for (int i = 0; i <= segments; i++)
            {
                double r0 = Math.Sin(i * deltaLatitude);
                double y0 = Math.Cos(i * deltaLatitude);

                for (int j = 0; j <= segments; j++)
                {
                    double x0 = r0 * Math.Sin(j * deltaLongitude);
                    double z0 = r0 * Math.Cos(j * deltaLongitude);

                    vertices[index] = new Vector3(x0, y0, z0);
                    uvs[index++] = new Vector2((float)j / segments, 1.0f - (float)(y0 * 0.5 + 0.5));
                }
            }

            // Generate the mid segments
            index = 0;
            for (int i = 1; i < segments; i++)
            {
                for (int j = 0; j < segments; j++)
                {
                    int baseIdx = vRowSize * i + j;
                    elements[index++] = baseIdx;
                    elements[index++] = baseIdx + 1;
                    elements[index++] = baseIdx + vRowSize + 1;
                    elements[index++] = baseIdx + 1;
                    elements[index++] = baseIdx;
                    elements[index++] = baseIdx - vRowSize;
                }
            }

            Vector3[] normals = Geometry.CalculateNormals(vertices, elements);
            return new VAO(program, new VBO<Vector3>(vertices), new VBO<Vector3>(normals), new VBO<Vector2>(uvs), new VBO<int>(elements));
        }

        /// <summary>
        /// Create a flat mesh (or plane) with a defined number of segments and UV tiling.
        /// </summary>
        /// <param name="program">The shader program that will be used with this mesh.</param>
        /// <param name="width">The width of the mesh.</param>
        /// <param name="height">The depth of the mesh.</param>
        /// <param name="xSegments">The number of segments in the x direction (width).</param>
        /// <param name="ySegments">The number of segments in the z direction (depth)</param>
        /// <param name="xTile">The amount to tile the UV coordinates in the x direction.</param>
        /// <param name="zTile">The amount to tile the UV coordinates in the z direction.</param>
        /// <returns>A VAO containing the mesh (vertex, UV and index) as defined by the input parameters.</returns>
        public static VAO CreateMesh(ShaderProgram program, float width, float height, int xSegments, int zSegments, float xTile, float zTile)
        {
            Vector3[] vertices = new Vector3[xSegments * zSegments * 2 * 3];
            Vector2[] uvs = new Vector2[xSegments * zSegments * 2 * 3];
            int[] indices = new int[xSegments * zSegments * 2 * 3];

            float UVdelX = xTile / xSegments, UVdelY = zTile / zSegments;
            float delX = width / xSegments, delY = height / zSegments;
            float px = -width / 2, py = -height / 2, puvx = 0, puvy = 0;

            for (int z = 0, index = 0; z < zSegments; z++)
            {
                for (int x = 0; x < xSegments; x++)
                {
                    // First triangle
                    vertices[index] = new Vector3(px, 0, py);
                    uvs[index++] = new Vector2(puvx, puvy);
                    vertices[index] = new Vector3(px + delX, 0, py);
                    uvs[index++] = new Vector2(puvx + UVdelX, puvy);
                    vertices[index] = new Vector3(px, 0, py + delY);
                    uvs[index++] = new Vector2(puvx, puvy + UVdelY);

                    // Second triangle
                    vertices[index] = new Vector3(px + delX, 0, py);
                    uvs[index++] = new Vector2(puvx + UVdelX, puvy);
                    vertices[index] = new Vector3(px + delX, 0, py + delY);
                    uvs[index++] = new Vector2(puvx + UVdelX, puvy + UVdelY);
                    vertices[index] = new Vector3(px, 0, py + delY);
                    uvs[index++] = new Vector2(puvx, puvy + UVdelY);

                    px += delX; puvx += UVdelX;     // increment the x values
                }

                px = -width / 2; puvx = 0;          // reset the x values
                py += delY; puvy += UVdelY;         // increment the y values
            }

            for (int i = 0; i < indices.Length; i++) indices[i] = i;

            return new VAO(program, new VBO<Vector3>(vertices), new VBO<Vector2>(uvs), new VBO<int>(indices, BufferTarget.ElementArrayBuffer));
        }
    }

    public static class Time
    {
        #region Static Fields and Properties
        private static System.Diagnostics.Stopwatch Timer;
        private static int deltaTimeIntegrator;

        /// <summary>
        /// Gets the amount of time in seconds that the previous frame took to render.
        /// </summary>
        public static float DeltaTime { get; private set; }

        /// <summary>
        /// Gets the total number of frames since the game started;
        /// </summary>
        public static uint FrameCount { get; private set; }

        /// <summary>
        /// Gets a smoothed version of time (the average delta time for the last 256 frames or so).
        /// </summary>
        public static float SmoothDeltaTime { get; private set; }

        /// <summary>
        /// Scales all of the time values so that time appears to 'warp'.
        /// Defaults to a value of 1.0f;
        /// </summary>
        public static float TimeScale { get; set; }

        /// <summary>
        /// The number of physics steps that should occur given the previous frame time and current frame time.
        /// Note:  Physics steps occur at a rate of 20fps
        /// </summary>
        public static int PhysicsSteps { get; private set; }

        /// <summary>
        /// Gets the number of physics steps multiplied by the time between physics time steps.
        /// ex)  With physics time steps of 0.05s this value will be PhysicsSteps * 0.05
        /// </summary>
        public static float PhysicsTimeStep { get; private set; }

        private const float physicsTimeStep = 0.025f;    // equates to 20fps
        private const float physicsFrameRate = 1f / physicsTimeStep;
        private static float physicsAccumulator = 0f;
        #endregion

        #region Static Methods
        /// <summary>
        /// Initializes and begins a Stopwatch for the Time static class.
        /// </summary>
        public static void Init()
        {
            Timer = System.Diagnostics.Stopwatch.StartNew();
            FrameCount = 0;
            deltaTimeIntegrator = 0;
            TimeScale = 1.0f;
        }

        /// <summary>
        /// Updates the privately updated static properties and resets the Stopwatch.
        /// </summary>
        public static float Update()
        {
            float frequencyInverse = TimeScale / System.Diagnostics.Stopwatch.Frequency;

            DeltaTime = (float)Timer.ElapsedTicks * frequencyInverse + float.Epsilon;

            if (FrameCount == int.MaxValue)
            {
                //Logger.Instance.WriteLine(LogFlags.Message, "You have been playing for a long time!  Impressive ...");
                FrameCount = (int.MaxValue >> 1);
            }
            FrameCount++;

            deltaTimeIntegrator += (int)(Timer.ElapsedTicks - (deltaTimeIntegrator >> 4));
            SmoothDeltaTime = (deltaTimeIntegrator >> 4) * frequencyInverse;

            physicsAccumulator += DeltaTime;
            PhysicsSteps = (int)(physicsAccumulator * physicsFrameRate);
            PhysicsTimeStep = PhysicsSteps * physicsTimeStep;
            physicsAccumulator -= PhysicsTimeStep;

            Timer.Restart();

            return DeltaTime;
        }
        #endregion
    }
}

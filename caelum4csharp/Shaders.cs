using System;
using System.Text;
using System.Collections.Generic;

using OpenGL;

namespace caelum4csharp
{
    public static class Shaders
    {
        public static ShaderProgram MoonBackgroundShader;
        public static ShaderProgram MoonShader;
        public static ShaderProgram SkydomeShader;
        public static ShaderProgram StarfieldShader;
        public static ShaderProgram SunShader;
        public static ShaderProgram CloudShader;

        private static List<ShaderProgram> LoadedPrograms = new List<ShaderProgram>();

        /// <summary>
        /// Initialize the shaders necessary for caelum to operate.
        /// </summary>
        public static void InitShaders()
        {
            StarfieldShader = InitShader(starFieldVertexShader, starFieldFragmentShader);
            SkydomeShader = InitShader(skyDomeVertexShader, skyDomeFragmentShader);
            MoonBackgroundShader = InitShader(moonVertexShader, moonBackgroundFragmentShader);
            MoonShader = InitShader(moonVertexShader, moonFragmentShader);
            SunShader = InitShader(moonVertexShader, sunFragmentShader);
            CloudShader = InitShader(cloudVertexShader, cloudFragmentShader);
        }

        public static void DisposeShaders()
        {
            foreach (var program in LoadedPrograms) program.Dispose();
        }

        public static void UpdateViewMatrix(Matrix4 viewMatrix)
        {
            foreach (var program in LoadedPrograms)
            {
                for (int i = 0; i < program.VertexShader.ShaderParams.Length; i++)
                {
                    if (program.VertexShader.ShaderParams[i].Name == "view_matrix")
                    {
                        program.Use();
                        program["view_matrix"].SetValue(viewMatrix);
                    }
                }
            }
        }

        public static void UpdateProjectionMatrix(Matrix4 projectionMatrix)
        {
            foreach (var program in LoadedPrograms)
            {
                for (int i = 0; i < program.VertexShader.ShaderParams.Length; i++)
                {
                    if (program.VertexShader.ShaderParams[i].Name == "projection_matrix")
                    {
                        program.Use();
                        program["projection_matrix"].SetValue(projectionMatrix);
                    }
                }
            }
        }

        #region Convert Shaders
        private static char[] newlineChar = new char[] { '\n' };
        private static char[] unixNewlineChar = new char[] { '\r' };

        public static string ConvertShader(string shader, bool vertexShader)
        {
            // there are a few rules to convert a shader from 140 to 120
            // the first is to remove the keywords 'in' and 'out' and replace with 'attribute'
            // the next is to remove camera uniform blocks
            StringBuilder sb = new StringBuilder();

            string[] lines = shader.Split(newlineChar);

            for (int i = 0; i < lines.Length; i++)
            {
                lines[i] = lines[i].Trim(unixNewlineChar);
                if (lines[i].StartsWith("uniform Camera"))
                {
                    i += 3;

                    sb.AppendLine("uniform mat4 projection_matrix;");
                    sb.AppendLine("uniform mat4 view_matrix;");
                }
                else if (lines[i].StartsWith("#version 140")) sb.AppendLine("#version 130");
                else if (lines[i].StartsWith("in ")) sb.AppendLine((vertexShader ? "attribute " : "varying ") + lines[i].Substring(3));
                else if (lines[i].StartsWith("out ") && vertexShader) sb.AppendLine("varying " + lines[i].Substring(4));
                else sb.AppendLine(lines[i]);
            }

            return sb.ToString();
        }

        public static ShaderProgram InitShader(string vertexSource, string fragmentSource)
        {
            if (Version == ShaderVersion.GLSL120)
            {
                vertexSource = ConvertShader(vertexSource, true);
                fragmentSource = ConvertShader(fragmentSource, false);
            }

            var program = new ShaderProgram(vertexSource, fragmentSource);
            LoadedPrograms.Add(program);
            return program;
        }

        public enum ShaderVersion
        {
            GLSL120,
            GLSL140
        }

        public static ShaderVersion Version = ShaderVersion.GLSL120;
        #endregion

        #region Shader Source
        private static string starFieldVertexShader = @"
#version 140

uniform Camera {
   mat4 projection_matrix;
   mat4 view_matrix;
};
uniform mat4 model_matrix;

uniform float mag_scale;
uniform float mag0_size;
uniform float min_size;
uniform float max_size;
uniform float aspect_ratio; // width/height

in vec3 in_position;
in vec3 in_normal;

out vec2 star_uv;
out vec4 star_color;

void main(void)
{
   vec4 out_position = projection_matrix * view_matrix * model_matrix * vec4(in_position, 1);
   star_uv = in_normal.xy;

   float magnitude = in_normal.z;
   float size = exp(mag_scale * magnitude) * mag0_size;
   float fade = clamp(size / min_size, 0, 1);

   star_color = vec4(1, 1, 1, fade * fade);

   size = clamp(size, min_size, max_size);

   out_position.xy += out_position.w * in_normal.xy * vec2(size, size * aspect_ratio);
   gl_Position = out_position;
}";

        private static string starFieldFragmentShader = @"
#version 140

in vec2 star_uv;
in vec4 star_color;

void main(void)
{
   float sqlen = dot(star_uv, star_uv);

   gl_FragColor = vec4(star_color.xyz, star_color.a * 1.5 * exp(-(sqlen * 8.0)));
}";

        private static string skyDomeVertexShader = @"
#version 140

uniform Camera {
   mat4 projection_matrix;
   mat4 view_matrix;
};
uniform mat4 model_matrix;
uniform vec3 sun_direction;

in vec3 in_position;
in vec3 in_normal;
in vec2 in_uv;

out vec3 light_direction;
out vec3 vertex_normal;
out float incident_angle;
out float light_y;

void main(void)
{
   light_direction = normalize(sun_direction);
   vertex_normal = in_normal;
   gl_Position = projection_matrix * view_matrix * model_matrix * vec4(in_position, 1);

   float cosine = dot(-light_direction, vertex_normal);
   incident_angle = -cosine * 0.9;

   light_y = -sun_direction.y;

   vertex_normal = -vertex_normal.xyz;
}";

        private static string skyDomeFragmentShader = @"
#version 140

uniform sampler2D gradientMap;
uniform sampler2D atmRelativeDepth;
uniform vec4 hazeColor;
uniform float invHazeHeight = 15;

in vec3 light_direction;
in vec3 vertex_normal;
in float incident_angle;
in float light_y;

const float fogDensity = 15;
const float sunlightScatteringFactor = 0.05;
const float sunlightScatteringLossFactor = 0.1;
const float atmLightAbsorptionFactor = 0.1;

float fogExp(float z, float density)
{
    return 1 - clamp(pow(2.71828, -z * density), 0, 1);
}

void main(void)
{
   float elevation = dot(light_direction, vec3(0, 1, 0)) * 0.5 + 0.5;
   vec4 color = texture2D(gradientMap, vec2(elevation, 0));

   // sunlight scatter
   if (incident_angle > 0)
   {
      float clampedAngle = clamp(incident_angle, 0, 1);
      float scatteredSunlight = pow(clampedAngle, 5.32192807f);

      float atmDepth = clamp(atmLightAbsorptionFactor * (1 - texture2D(atmRelativeDepth, vec2(light_y, 0)).x), 0, 1);
      vec4 sunColor = vec4(3, 3, 3, 1) * (1.0 - atmDepth) * vec4 (0.9, 0.5, 0.09, 1) * scatteredSunlight;
      color.xyz += sunColor.xyz * (1.0 - sunlightScatteringLossFactor);
   }

   float haze = fogExp(pow(clamp(1.0 - vertex_normal.y, 0, 1), invHazeHeight), fogDensity);
   color = color * (1.0 - haze) + hazeColor * haze;
   float alpha = clamp((color.x + color.y + color.z) * 0.5, 0, 1);
   gl_FragColor = vec4(color.xyz, alpha);
}";

        private static string moonVertexShader = @"
#version 140

in vec3 in_position;
in vec2 in_uv;

out vec2 uv;

uniform mat4 projection_matrix;
uniform mat4 view_matrix;
uniform mat4 model_matrix;

void main(void)
{
    uv = in_uv;

    gl_Position = projection_matrix * (view_matrix * model_matrix * vec4(0, 0, 0, 1) + model_matrix * vec4(in_position.x, in_position.y, in_position.z, 0));
}";

        private static string moonBackgroundFragmentShader = @"
#version 140

in vec2 uv;

uniform sampler2D moonDisc;

void main()
{
    vec4 outcol = texture2D(moonDisc, uv.xy);
    
    gl_FragColor = vec4(0, 0, 0, outcol.a);
}";

        private static string moonFragmentShader = @"
#version 140

in vec2 uv;

uniform float phase;
uniform sampler2D moonDisc;

void main()
{
    float _lum;

    vec4 outcol = texture2D(moonDisc, uv.xy);
    
    float alpha = 1.0;
    float srefx = uv.x - 0.5;
    float refx = abs(uv.x - 0.5);
    float refy = abs(uv.y - 0.5);
    float refxfory = sqrt(0.25 - refy * refy);
    float xmin = -refxfory;
    float xmax = refxfory;
    float tphase = (phase > 2 ? phase - 2 : phase);
    float xmin1 = (xmax - xmin) * (tphase / 2) + xmin;
    float xmin2 = (xmax - xmin) * tphase + xmin;
    if (srefx < xmin1) alpha = 0;
    else if (srefx < xmin2 && xmin1 != xmin2) alpha = (srefx - xmin1) / (xmin2 - xmin1);
    
    float lum = dot(outcol.xyz, vec3(0.3333, 0.3333, 0.3333));
    alpha = lum * (phase > 2 ? 1 - alpha : alpha);
    outcol.w = min(outcol.a, (phase > 2 ? 1 - alpha : alpha));
    outcol.xyz = outcol.xyz / vec3(lum, lum, lum);
    
    gl_FragColor = outcol;
}";

        private static string sunFragmentShader = @"
#version 140

in vec2 uv;

uniform sampler2D active_texture;
uniform vec3 color;

void main(void)
{
  gl_FragColor = vec4(color, 1) * texture(active_texture, uv);
}";

        private static string cloudVertexShader = @"
#version 140

in vec3 in_position;
in vec2 in_uv;

out vec2 uv;
out float sunGlow;
out vec4 worldPosition;

uniform mat4 projection_matrix;
uniform mat4 view_matrix;
uniform mat4 model_matrix;

uniform vec3 vSunDirection;

void main(void)
{
    uv = in_uv;

    // This is the relative position, or view direction.
    vec3 relPosition = normalize(in_position.xyz);

    // Calculate the angle between the direction of the sun and the current
    // view direction. This we call 'glow' and ranges from 1 next to the sun
    // to -1 in the opposite direction.
    sunGlow = dot(relPosition, normalize(-vSunDirection));

    worldPosition = view_matrix * model_matrix * vec4(in_position, 1);
    gl_Position = projection_matrix * worldPosition;
}";

        private static string cloudFragmentShader = @"
#version 140

// Global cloud textures
uniform sampler2D cloud_shape1;
uniform sampler2D cloud_shape2;
uniform sampler2D cloud_detail;

// Cloud fragment program.
uniform vec3 sunDirection;

in vec2 uv;
in float sunGlow;
in vec4 worldPosition;

uniform float cloudMassInvScale;
uniform float cloudDetailInvScale;
uniform vec2 cloudMassOffset;
uniform vec2 cloudDetailOffset;
uniform float cloudMassBlend;
uniform float cloudDetailBlend;

uniform float cloudCoverageThreshold;

uniform vec3 sunLightColour;
uniform vec3 sunSphereColour;
uniform vec3 fogColour;

uniform float cloudSharpness;
uniform float cloudThickness;
uniform vec3 camera_position;

uniform float layerHeight;
uniform float cloudUVFactor;
uniform float heightRedFactor;

float LayeredClouds_intensity(vec2 pos)
{
    vec2 finalMassOffset = cloudMassOffset + pos;
    float aCloud = mix( texture2D(cloud_shape1, finalMassOffset * cloudMassInvScale).x,
                        texture2D(cloud_shape2, finalMassOffset * cloudMassInvScale).x,
                        cloudMassBlend);
    float aDetail = texture2D(cloud_detail, (cloudDetailOffset + pos) * cloudDetailInvScale).x;
    aCloud = (aCloud + aDetail * cloudDetailBlend) / (1.0 + cloudDetailBlend);
    return max(0.0, aCloud - cloudCoverageThreshold);
}

vec4 OldCloudColor(vec2 uvf, float sGlow)
{
    vec4 oCol = vec4(1.0, 1.0, 1.0, 0.0);
    float intensity = LayeredClouds_intensity(uvf);
    float aCloud = clamp(exp(cloudSharpness * intensity) - 1.0, 0.0, 1.0);
    
    float shine = pow(clamp(sGlow, 0.0, 1.0), 8.0) / 4;
    //sunLightColour.xyz = sunLightColour.xyz * 1.5;
    vec3 cloudColour = fogColour.xyz * (1 - intensity / 3);
    float thickness = clamp(0.8 - exp(-cloudThickness * (intensity + 0.2 - shine)), 0.0, 1.0);
    
    oCol.xyz = cloudColour.xyz * 1.1;//mix(sunLightColour.xyz, cloudColour.xyz, 0.5);
    oCol.a = aCloud;
    
    return oCol;
}

//Converts a color from RGB to YUV color space
//the rgb color is in [0,1] [0,1] [0,1] range
//the yuv color is in [0,1] [-0.436,0.436] [-0.615,0.615] range
vec3 YUVfromRGB(vec3 col)
{
    return vec3( dot(col, vec3(0.299, 0.587, 0.114)),
                 dot(col, vec3(-0.14713, -0.28886, 0.436)),
                 dot(col, vec3(0.615, -0.51499, -0.10001)) );
}

vec3 RGBfromYUV(vec3 col)
{
    return vec3( dot(col, vec3(1.0, 0.0, 1.13983)),
                 dot(col, vec3(1.0, -0.39465, -0.58060)),
                 dot(col, vec3(1.0, 2.03211, 0.0)) );
}

// Creates a color that has the intensity of col1 and the chrominance of col2
vec3 MagicColorMix(vec3 col1, vec3 col2)
{
    return clamp(RGBfromYUV(vec3(YUVfromRGB(col1).x, YUVfromRGB(col2).yz)), 0.0, 1.0);
}

void main()
{
    // Transform texture coordinates.
    vec2 uvf = uv * cloudUVFactor;

    vec4 oCol = OldCloudColor(uvf, sunGlow);

    // Modify the red intensity of the fragment.
    oCol.r += layerHeight / heightRedFactor;

    // Some constants
    float D1 = 1000.0;
    float D2 = 10000.0;
    float aMod = 1.0;

    // Calculate the distance from the current vertex to the camera.
    float Distance = distance(worldPosition.xyz, camera_position);

    // Adjust the fragments alpha based upon the 'Distance'
    if (Distance > D1)
    {
        // If the vertex is more than 10,000 units away then modify
        // the alpha so that clouds in the distance appear more dense.
        aMod = mix(0.0, 1.0, (D2 - Distance) / (D2 - D1));
    }
    float alfa = oCol.a * aMod;

    // Find the direction of the last vertex relative to the camera.
    vec3 cloudDir = normalize( vec3(worldPosition.x, layerHeight, worldPosition.y) - camera_position);

    // Calculate the difference in the angle between the cloud and the Sun.
    float angleDiff = clamp( dot(cloudDir, normalize(sunDirection)), 0.0, 1.0);

    // Do some magical color mixing based upon the cloud and Sun angles.
    vec3 lCol = mix(oCol.rgb, MagicColorMix(oCol.rgb, sunSphereColour.rgb), angleDiff);

    // Determine the final fragment color.
    gl_FragColor.rgb = mix(lCol, oCol.rgb, alfa + mod(layerHeight, 1.0));

    // Set the final fragment alpha.
    gl_FragColor.a = alfa;
}";
        #endregion
    }
}

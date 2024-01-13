using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using System;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace OrbitalSimOpenGL
{
    public class Reticle
    {
        /// <summary>
        /// Draw reticle
        /// </summary>
        /// <remarks>
        /// Recticle draws using its own shaders, matrices (View and Projection), and
        /// its own geometry (sphere just now).
        /// Whenver window aspect ratio changes be sure to set Reticle's AspectRatio property.
        /// </remarks>
        #region Properties
        public bool DrawReticle { get; set; } = true;
        public Double DistFromCamera { get; set; } = 100D; // meters from camera, U coords
        public Double ReticleSize { get; set; } = 25D; // recticle sphere diameter in meters, U coords

        // Shared sphere
        Single[] ReticleSphereMesh;
        UInt16[] ReticleSphereIndices;

        private readonly Shader ReticleShader;
        private static String ReticleVertexShader = @"
#version 330 core
layout (location = 0) in vec3 aPosition;
uniform mat4 MVP;
uniform vec4 objColor; // we set this variable in the OpenGL code.
out vec4 vertexColor;
void main(void)
{
    gl_Position = MVP * vec4(aPosition, 1.0);
    vertexColor = objColor;
}
";
        private static String ReticleFragmentShader = @"
#version 330 core
in vec4 vertexColor;  // the input variable from the vertex shader (same name and same type)
out vec4 FragColor;
void main()
{
    FragColor = vertexColor;
}
";

        Color4 ReticleColor { get; } = Color4.Red;
        private int ReticleColorUniform { get; set; }
        private int MVP_Uniform { get; set; }

        Matrix4 SizeMatrix = Matrix4.Identity;
        Matrix4 LocationMatrix = Matrix4.Identity;
        Matrix4 ViewMatrix = Matrix4.Identity;
        Matrix4 ProjectionMatrix = Matrix4.Identity;
        Matrix4 MVP = Matrix4.Identity;

        private float _aspectRatio = 1.0f;
        public float AspectRatio
        {
            get { return _aspectRatio; }
            set
            {
                _aspectRatio = value;
                // Projection matrix changes when aspect ratio changes
                ProjectionMatrix = Matrix4.CreatePerspectiveFieldOfView(45.0f * (MathHelper.Pi / 180f), _aspectRatio, 0.1f, 10f);
                // And so does MVP
                MVP = SizeMatrix * LocationMatrix * ViewMatrix * ProjectionMatrix;
            }
        }
        #endregion

        public Reticle()
        {
            ReticleShader = new(ReticleVertexShader, ReticleFragmentShader);
            ReticleColorUniform = GL.GetUniformLocation(ReticleShader.ShaderHandle, "objColor");
            MVP_Uniform = GL.GetUniformLocation(ReticleShader.ShaderHandle, "MVP");

            Util.MakeUnitSphere(ref ReticleSphereMesh, ref ReticleSphereIndices);

            Vector3 eye = new(0f, 0f, 1f);
            Vector3 target = new(0f, 0f, 0f);
            Vector3 up = new(0f, 1f, 0f);
            ViewMatrix = Matrix4.LookAt(eye, target, up);

            LocationMatrix.M41 = LocationMatrix.M42 = LocationMatrix.M43 = 0f;
            SizeMatrix.M11 =  SizeMatrix.M22 =  SizeMatrix.M33 = .01f;

            MVP = SizeMatrix * LocationMatrix * ViewMatrix * ProjectionMatrix;
        }

        internal void Render(SimCamera simCamera)
        {
            if (DrawReticle)
            {
                ReticleShader.Use();

                GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), 0 /*GL.GetAttribLocation(Shader.ShaderHandle, "aPosition")*/);

                // Upload ReticleSphereMesh
                GL.BufferData(BufferTarget.ArrayBuffer, ReticleSphereMesh.Length * sizeof(Single), ReticleSphereMesh, BufferUsageHint.StaticDraw);
                GL.BufferData(BufferTarget.ElementArrayBuffer, ReticleSphereIndices.Length * sizeof(UInt16), ReticleSphereIndices, BufferUsageHint.StaticDraw);

                // Render
                GL.Uniform4(ReticleColorUniform, ReticleColor);
                GL.UniformMatrix4(MVP_Uniform, false, ref MVP);
                GL.DrawElements(PrimitiveType.Triangles, ReticleSphereIndices.Length, DrawElementsType.UnsignedShort, 0);
            }
        }
    }
}

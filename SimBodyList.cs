using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Windows.Media.Media3D;

namespace OrbitalSimOpenGL
{
    public class SimBodyList
    {
        #region Properties

        // Shared sphere
        Single[] SharedSphereMesh;
        UInt16[] SharedSphereIndices;

        private Shader BodyShader;
        public static String VertexShader = @"
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
        public static String FragmentShader = @"
#version 330 core
in vec4 vertexColor; // the input variable from the vertex shader (same name and same type)
out vec4 FragColor;
void main()
{
    FragColor = vertexColor;
}
";
        int BodyColorUniform { get; set; }
        int MVP_Uniform { get; set; }

        Matrix4 SizeMatrix4 = Matrix4.Identity;
        Matrix4 LocationMatrix4 = Matrix4.Identity;
        public List<SimBody> BodyList { get; }
        private String AppDataFolder { get; set; }
        #endregion 

        public SimBodyList(EphemerisBodyList ephemerismBodyList, String appDataFolder)
        {
            AppDataFolder = appDataFolder;

            Util.MakeUnitSphere(ref SharedSphereMesh, ref SharedSphereIndices);

            BodyList = new List<SimBody>();

            foreach (EphemerisBody eB in ephemerismBodyList.Bodies)
                BodyList.Add(new SimBody(eB, AppDataFolder));

            BodyShader = new(VertexShader, FragmentShader);

            BodyColorUniform = GL.GetUniformLocation(BodyShader.ShaderHandle, "objColor");
            MVP_Uniform = GL.GetUniformLocation(BodyShader.ShaderHandle, "MVP");
        }

        public void Render(SimCamera simCamera)
        {
            BodyShader.Use();

            // Model (Scale * Trans) * View * Projection

            //LocationMatrix4.M41 = 0.0f; // X
            //LocationMatrix4.M42 = 0.0f; // Y
            //LocationMatrix4.M43 = 0.0f; // Z

            //SizeMatrix4.M11 = 50f; // X
            //SizeMatrix4.M22 = 50f; // Y
            //SizeMatrix4.M33 = 50f; // Z

            //Matrix4 mvp = SizeMatrix4 * LocationMatrix4 * vp;

            //GL.UniformMatrix4(GL.GetUniformLocation(BodyShader.ShaderHandle, "MVP"), false, ref mvp);


            // Upload and render 2D cube
            //GL.BufferData(BufferTarget.ArrayBuffer, CubeVertices2D.Length * sizeof(float), CubeVertices2D, BufferUsageHint.StaticDraw);
            //GL.BufferData(BufferTarget.ElementArrayBuffer, CubeIndices2D.Length * sizeof(uint), CubeIndices2D, BufferUsageHint.StaticDraw);
            //GL.DrawElements(PrimitiveType.Triangles, CubeIndices2D.Length, DrawElementsType.UnsignedInt, 0);

            // Upload unit sphere
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), 0 /*GL.GetAttribLocation(Shader.ShaderHandle, "aPosition")*/);
            GL.BufferData(BufferTarget.ArrayBuffer, SharedSphereMesh.Length * sizeof(Single), SharedSphereMesh, BufferUsageHint.StaticDraw);
            GL.BufferData(BufferTarget.ElementArrayBuffer, SharedSphereIndices.Length * sizeof(UInt16), SharedSphereIndices, BufferUsageHint.StaticDraw);

            //GL.DrawElements(PrimitiveType.Triangles, SharedSphereIndices.Length, DrawElementsType.UnsignedShort, 0);

            foreach (SimBody sB in BodyList)
                sB.RenderSphericalBody(SharedSphereIndices.Length, BodyColorUniform, MVP_Uniform, ref simCamera.VP_Matrix, ref LocationMatrix4, ref SizeMatrix4);
        }

        /// <summary>
        /// Get current position of a body in the model
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public void GetPosition(String name, ref Vector3d position)
        {
            position.X = position.Y = position.Z = 0D;

            foreach (SimBody sB in BodyList)
                if (name.Equals(sB.Name))
                    sB.GetPosition(ref position);
        }

        /// <summary>
        /// Location of a body, by name, in the list
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public int GetIndex(String name)
        {
            int index = 0;
            foreach (SimBody sB in BodyList)
            {
                if (name.Equals(sB.Name))
                    return index;
                index++;
            }
            return -1;
        }
    }
}

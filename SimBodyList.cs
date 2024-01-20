using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media.Media3D;

namespace OrbitalSimOpenGL
{
    public class SimBodyList
    {
        #region Properties
        // Shared sphere
        readonly Single[] SharedSphereMesh;
        readonly UInt16[] SharedSphereIndices;

        private Shader Shader { get; set; }
        private static String VertexShader = @"
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
        private static String FragmentShader = @"
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

        // Keep these here so they are allocated but once and reused
        Matrix4 SizeMatrix4 = Matrix4.Identity;
        Matrix4 LocationMatrix4 = Matrix4.Identity;
        public List<SimBody> BodyList { get; }
        private String AppDataFolder { get; set; }
        #endregion 

        public SimBodyList(EphemerisBodyList ephemerismBodyList, String appDataFolder)
        {
            AppDataFolder = appDataFolder;

            Util.MakeUnitSphere(out SharedSphereMesh, out SharedSphereIndices);

            BodyList = new List<SimBody>();

            foreach (EphemerisBody eB in ephemerismBodyList.Bodies)
                //                if (eB.Name.Equals("Sun"))
                BodyList.Add(new SimBody(eB, AppDataFolder));

            Shader = new(VertexShader, FragmentShader);

            BodyColorUniform = GL.GetUniformLocation(Shader.ShaderHandle, "objColor");
            MVP_Uniform = GL.GetUniformLocation(Shader.ShaderHandle, "MVP");
        }

        /// <summary>
        /// Render each body in the BodyList
        /// </summary>
        /// <param name="simCamera"></param>
        /// <param name="mousePosition">Current mouse cursor position, for hit-testing</param>
        /// <returns>
        /// simBody which mouse is over or null if no hit
        /// </returns>
        public SimBody Render(SimCamera simCamera, System.Windows.Point mousePosition)
        {
            SimBody? hitSB = null;
            Double lastHitDist = Double.MaxValue;

            Shader.Use();

            // Model (Scale * Trans) * View * Projection

            // Upload unit sphere
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), 0 /*GL.GetAttribLocation(Shader.ShaderHandle, "aPosition")*/);
            GL.BufferData(BufferTarget.ArrayBuffer, SharedSphereMesh.Length * sizeof(Single), SharedSphereMesh, BufferUsageHint.StaticDraw);
            GL.BufferData(BufferTarget.ElementArrayBuffer, SharedSphereIndices.Length * sizeof(UInt16), SharedSphereIndices, BufferUsageHint.StaticDraw);

            Vector3d halfNorm = simCamera.UpVector3d * 5e-1f;

            const Single minSize = 5;
            const Single minSizeSqared = 5 * 5;
            Vector3d center;
            FrustumCuller fC = simCamera.FrustumCuller;

            foreach (SimBody sB in BodyList)
            {
                // SimBody's center (U coords)
                center.X = sB.X;
                center.Y = sB.Y;
                center.Z = sB.Z;

                // If outsude frustum no need to render/process
                if (false == fC.SphereCulls(ref center, sB.EphemerisDiameter))
                {
                    // Sphere is visible in current frustum
                    // a. Keep its rendering to a minimum size (so something will be visible no matter how far from camera)
                    // b. Render it
                    Double dist = sB.KeepVisible(simCamera, ref halfNorm, minSize, minSizeSqared, ref mousePosition);
                    sB.Render(SharedSphereIndices.Length, BodyColorUniform, MVP_Uniform, ref simCamera._VP_Matrix, ref LocationMatrix4, ref SizeMatrix4);
                    if (-1D != dist)
                        if (dist < lastHitDist) // Keep only hit closest to camera
                        {
                            hitSB = sB;
                            lastHitDist = dist;
                        }
                }
            }
            return hitSB;
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
        /// <returns>Integer index or -1 if not found</returns>
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

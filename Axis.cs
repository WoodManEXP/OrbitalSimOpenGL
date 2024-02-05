using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace OrbitalSimOpenGL
{
    internal class Axis
    {
        /// <summary>
        /// Assembles and renders 3D axis
        /// </summary>
        /// 
        #region Properties
        private SimCamera SimCamera { get; set; }
        private Scale Scale { get; set; }
        private Single HalfAxisLength { get; set; }
        private static Color4 XAxisColor { get; } = Color4.Black;
        private static Color4 YAxisColor { get; } = Color4.Blue;
        private static Color4 ZAxisColor { get; } = Color4.Yellow;

        private Vector3[] XEndpoints, YEndpoints, ZEndpoints;
        private static readonly int Vector3Size = Marshal.SizeOf(typeof(Vector3));
        private static readonly int BSize = 2 * Vector3Size;

        private Shader AxisShader { get; set; }
        int AxisColorUniform { get; set; }
        int MVP_Uniform { get; set; }
        public float AxisLineWidth { get; private set; } = 15e-1f;

        public static String AxisVertexShader = @"
# version 330 core
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
        public static String AxisFragmentShader = @"
# version 330 core
in vec4 vertexColor;  // the input variable from the vertex shader (same name and same type)
out vec4 FragColor;
void main()
{
    FragColor = vertexColor;
}
";

        #endregion

        public Axis(SimCamera simCamera, Scale scale)
        {
            SimCamera = simCamera;
            Scale = scale;

            AxisShader = new(AxisVertexShader, AxisFragmentShader);
            AxisColorUniform = GL.GetUniformLocation(AxisShader.ShaderHandle, "objColor");
            MVP_Uniform = GL.GetUniformLocation(AxisShader.ShaderHandle, "MVP");

            // Value from Properties is U coords
            HalfAxisLength = Scale.ScaleU_ToW(Properties.Settings.Default.AxisLength * 5e-1d); // / 2

            XEndpoints = new Vector3[2];
            XEndpoints[0].X = -HalfAxisLength; XEndpoints[0].Y = 0f; XEndpoints[0].Z = 0f;
            XEndpoints[1].X = HalfAxisLength; XEndpoints[1].Y = 0f; XEndpoints[1].Z = 0f;

            YEndpoints = new Vector3[2];
            YEndpoints[0].X = 0f; YEndpoints[0].Y = -HalfAxisLength; YEndpoints[0].Z = 0f;
            YEndpoints[1].X = 0f; YEndpoints[1].Y = HalfAxisLength; YEndpoints[1].Z = 0f;

            ZEndpoints = new Vector3[2];
            ZEndpoints[0].X = 0f; ZEndpoints[0].Y = 0f; ZEndpoints[0].Z = -HalfAxisLength;
            ZEndpoints[1].X = 0f; ZEndpoints[1].Y = 0f; ZEndpoints[1].Z = HalfAxisLength;
        }

        public void Render()
        {
            AxisShader.Use();

            float currentLineWidth = GL.GetFloat(GetPName.LineWidth);
            GL.LineWidth(AxisLineWidth);

            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, Vector3Size, 0);
            GL.UniformMatrix4(MVP_Uniform, false, ref SimCamera._VP_Matrix);

            // X - Push into GPU and draw
            GL.BufferData(BufferTarget.ArrayBuffer, BSize, XEndpoints, BufferUsageHint.StaticDraw);
            GL.Uniform4(AxisColorUniform, XAxisColor);
            GL.DrawArrays(PrimitiveType.Lines, 0, 2);

            // Y - Push into GPU and draw
            GL.BufferData(BufferTarget.ArrayBuffer, BSize, YEndpoints, BufferUsageHint.StaticDraw);
            GL.Uniform4(AxisColorUniform, YAxisColor);
            GL.DrawArrays(PrimitiveType.Lines, 0, 2);

            // Z - Push into GPU and draw
            GL.BufferData(BufferTarget.ArrayBuffer, BSize, ZEndpoints, BufferUsageHint.StaticDraw);
            GL.Uniform4(AxisColorUniform, ZAxisColor);
            GL.DrawArrays(PrimitiveType.Lines, 0, 2);
            
            GL.LineWidth(currentLineWidth); // Restore
        }
    }
}

using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Media3D;

namespace OrbitalSimOpenGL
{
    public struct Vertex
    {
        public const int Size = (4 + 4) * 4; // size of struct in bytes

        private readonly Vector4 _position;
        private readonly Color4 _color;

        public Vertex(Vector4 position, Color4 color)
        {
            _position = position;
            _color = color;
        }
    }

    public class SimModel
    {
        /// <summary>
        /// https://opentk.net/learn
        /// https://opentk.net/learn/chapter1/2-hello-triangle.html?tabs=onload-opentk4%2Conrender-opentk4%2Cresize-opentk4
        /// https://www.geeks3d.com/20141201/how-to-rotate-a-vertex-by-a-quaternion-in-glsl/
        /// https://paroj.github.io/gltut/Positioning/Tut07%20The%20Perils%20of%20World%20Space.html
        /// https://paroj.github.io/gltut/index.html
        /// *** http://www.opengl-tutorial.org/beginners-tutorials/tutorial-3-matrices/#the-model-view-and-projection-matrices
        /// https://antongerdelan.net/opengl/raycasting.html
        /// </summary>
        /// 
        #region Properties
        public bool SceneReady { get; set; } = false;
        public SimBodyList? SimBodyList { get; set; }
        private NextPosition? NextPosition { get; set; }
        private Double GravConstantSetting { get; set; } = 0D; // Grav Constant starts unmodified

        private PathTrace PathTrace;
        public Scale Scale { get; set; } = new();
        public String? AppDataFolder { get; set; }
        public int IterationSeconds { get; set; } = 60; // Each frame iteration represents this many seconds of model simulation
        public int TimeCompression { get; set; } = 1; // Number of times to iterate per frame
        public Int64 ElapsedSeconds { get; set; } = 0;
        public bool IncludeAxis { get; set; } = true; // Render the three axis elements (X, Y, Z)
        public SimCamera? SimCamera { get; set; }
        private MassMass? MassMass { get; set; }
        private CollisionDetector? CollisionDetector { get; set; }

        // Closest approach between any two bodies captured here, for each iteration
        private Double _ClosestApproachDistSquared = -1D; // km-squared
        private int _ApproachBodyA, _ApproachBodyB;
        public Double ClosestApproachDistSquared // Distance between body surfaces
        { get { return _ClosestApproachDistSquared; } }
        public int ApproachBodyA
        { get { return _ApproachBodyA; } }
        public int ApproachBodyB
        { get { return _ApproachBodyB; } }

        public bool SimRunning { get; set; } = false;
        public SimBody? LastMouseOverSB { get; set; } = null;

        private bool _Wireframe;
        public bool Wireframe
        {
            get => _Wireframe;
            set
            {
                GL.PolygonMode(MaterialFace.FrontAndBack, value ? PolygonMode.Line : PolygonMode.Fill); // PolygonMode.Fill
                _Wireframe = value;
            }
        }
        int VertexBufferObject { get; set; }
        int VertexArrayObject { get; set; }
        int ElementBufferObject { get; set; }
        #endregion

        public SimModel()
        {
            GL.ClearColor(Color4.LightGray);

            // enable depth testing to ensure correct z-ordering of fragments
            GL.Enable(EnableCap.DepthTest);

            // Maximum number of vertex attributes supported
            //int nrAttributes = 0;
            //GL.GetInteger(GetPName.MaxVertexAttribs, out nrAttributes);
        }

        public void InitScene(EphemerisBodyList ephemerismBodyList)
        {
            InitAxes();
            InitBodies(ephemerismBodyList);

            VertexBufferObject = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, VertexBufferObject);

            VertexArrayObject = GL.GenVertexArray(); // Vertices
            GL.BindVertexArray(VertexArrayObject);
            GL.EnableVertexAttribArray(0);

            ElementBufferObject = GL.GenBuffer();   // Indices
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, ElementBufferObject);

            GL.Enable(EnableCap.DepthTest); // For z-ordering

            GL.Enable(EnableCap.CullFace);
            GL.CullFace(CullFaceMode.Back);
            GL.FrontFace(FrontFaceDirection.Cw);  // Tringles are wound CW

            Wireframe = true;
            SceneReady = true;
        }

        /// <summary>
        /// Initialize geometry for the indiviual bodies
        /// </summary>
        /// <param name="bodyList"></param>
        private void InitBodies(EphemerisBodyList ephemerisBodyList)
        {
            SimBodyList = new SimBodyList(ephemerisBodyList, AppDataFolder);

            // Process each body
            foreach (SimBody sB in SimBodyList.BodyList)
            {
                //                if (!sB.Name.Equals("Sun")) // Only Sol for now
                //                    continue;
                sB.InitBody(Scale);
            }

            PathTrace = new(Scale);
            MassMass = new(SimBodyList);
            CollisionDetector = new(SimBodyList, MassMass);
            NextPosition = new(SimBodyList, MassMass, GravConstantSetting);
        }

        /// <summary>
        /// Reset sim back to initial state
        /// </summary>
        /// <param name="ephemerismBodyList"></param>
        public void ResetScene(EphemerisBodyList ephemerismBodyList)
        {

            // Stop the sim, to avoid any timing and indetrerminate-state issues relative to
            // OpenTK's calling OrbitalSimWindow:OnRender.
            bool prevSimRunningState = SimRunning;
            SimRunning = false;




            SimRunning = prevSimRunningState;
        }

        /// <summary>
        /// This is called repeatedly by the OpenTK system
        /// </summary>
        /// <param name="ms"></param>
        /// <param name="frameRateMS"></param>
        /// <param name="mousePosition">Current mouse cursor position, for hit-testing</param>
        /// <remarks>Renders the current model state</remarks>
        public void Render(int ms, int frameRateMS, System.Windows.Point mousePosition)
        {
            if (!SceneReady)
                return;

            if (SimBodyList is null) // JIC
                return;

            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            Iterate(SimRunning, ms, frameRateMS); // Perform model calculations

            RenderAxis();

            // Render bodies
            // If render returns non-null sB then the mouse was over that body.
            SimBody sB;
            if (null != (sB = SimBodyList.Render(SimCamera, mousePosition)))
                LastMouseOverSB = sB;
        }

        /// <summary>
        /// Frame iteration
        /// </summary>
        /// <param name="ms">ms time of this call as supplied</param>
        /// <param name="movingAvgMS">Moving average frame rate. A frame is happening on average every movingAvgMS</param>
        public void Iterate(bool simRunning, int ms, int movingAvgMS)
        {
            if (simRunning)
            {
                for (int i = 0; i < TimeCompression; i++)
                {
                    NextPosition?.IterateOnce(IterationSeconds);

                    // Collision detection
                    CollisionDetector?.Detect(out _ClosestApproachDistSquared, out _ApproachBodyA, out _ApproachBodyB);

                    ElapsedSeconds += IterationSeconds;
                }

                // Process path traces (lines showing where bodies have been)
                //PathTrace.UpdateTracePaths(SimBodyList, NextPosition.IterationNumber);
            }
        }

        /// <summary>
        /// Alter the Gravational Constant
        /// </summary>
        /// <param name="v"><0 divides GC by that value, >0 multiplies GC by that value, 0 sets to std value</param>
        /// <exception cref="NotImplementedException"></exception>
        internal void GravConstant(int v)
        {
            GravConstantSetting = (Double)v;
            if (NextPosition is not null)
                NextPosition.UseReg_G = GravConstantSetting;
        }

        /// <summary>
        /// Remove a body from the sim
        /// </summary>
        /// <param name="bodyName"></param>
        /// <exception cref="NotImplementedException"></exception>
        internal void ExcludeBody(String bodyName)
        {
            if (!SceneReady)
                return;

            if (SimBodyList is null) // JIC
                return;

            int sBI = SimBodyList.GetIndex(bodyName);

            if (-1 != sBI)
            {
                SimBody sB = SimBodyList.BodyList[sBI];
                sB.ExcludeFromSim = true;
            }
        }

        /// <summary>
        /// Alter mass of a specific body
        /// </summary>
        /// <param name="bodyName"></param>
        /// <param name="multiplier"></param>
        /// <remarks>
        /// < 0 divides mass by that value, > 0 multiplies mas by that value, 0 sets to std value.
        /// Causes recalculation of MassMass table.
        /// </remarks>
        internal void SetMassMultiplier(String bodyName, int multiplier)
        {
            if (!SceneReady)
                return;

            if (SimBodyList is null) // JIC
                return;

            int sBI = SimBodyList.GetIndex(bodyName);
            SimBodyList.BodyList[sBI].MassMultiplier = (Double)multiplier;

            MassMass?.CalcMassMass(SimBodyList);
        }

        /// <summary>
        /// Alter current velocity of a specific body
        /// </summary>
        /// <param name="bodyName"></param>
        /// <param name="multiplier"></param>
        /// <remarks>
        /// < 0 divides current velocity by that value, > 0 multiplies current velocity by that value, 0 leaves it unchanged.
        /// </remarks>
        internal void SetVelocityMultiplier(String bodyName, int multiplier)
        {
            if (!SceneReady)
                return;

            if (SimBodyList is null) // JIC
                return;

            int sBI = SimBodyList.GetIndex(bodyName);
            SimBodyList.BodyList[sBI].AlterVelocity(multiplier);
        }

        #region Axes
        private Shader AxisShader;
        public static String AxisVertexShader = @"
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
        public static String AxisFragmentShader = @"
#version 330 core
in vec4 vertexColor;  // the input variable from the vertex shader (same name and same type)
out vec4 FragColor;
void main()
{
    FragColor = vertexColor;
}
";
        // Shared sphere
        float[]? SharedAxisMesh;
        ushort[]? SharedAxisIndices;

        Color4 X_AxisColor { get; } = Color4.Black;
        Color4 Y_AxisColor { get; } = Color4.Blue;
        Matrix4 Y_AxisRotationMatrix { get; set; }
        Color4 Z_AxisColor { get; } = Color4.Yellow;
        Matrix4 Z_AxisRotationMatrix { get; set; }
        int AxisColorUniform { get; set; }
        int MVP_Uniform { get; set; }

        /// <summary>
        /// Axes are long, rectangular prisims.
        /// refpoints are spheres on positive side of each axis
        /// </summary>
        private void InitAxes(bool refPoints = true)
        {
            AxisShader = new(AxisVertexShader, AxisFragmentShader);

            AxisColorUniform = GL.GetUniformLocation(AxisShader.ShaderHandle, "objColor");
            MVP_Uniform = GL.GetUniformLocation(AxisShader.ShaderHandle, "MVP");

            double axisLength = Properties.Settings.Default.AxisLength / 2D;
            double axisWidth = 1E6 / 10 / 2;

            // Y, Z scale factors
            Double zfactor = Math.Sqrt(.75D) / 2D;
            Double yFactor = .5D / 2D;

            // Build mesh/vertex and indices into form needed by OpenGL
            SharedAxisMesh = new Single[]
            {
                Scale.ScaleU_ToW(axisLength),  0f,                                   Scale.ScaleU_ToW(zfactor*axisWidth),   // 0 +X end
                Scale.ScaleU_ToW(axisLength),  Scale.ScaleU_ToW(-yFactor*axisWidth), Scale.ScaleU_ToW(-zfactor*axisWidth),  // 1
                Scale.ScaleU_ToW(axisLength),  Scale.ScaleU_ToW(yFactor*axisWidth),  Scale.ScaleU_ToW(-zfactor*axisWidth),  // 2
                Scale.ScaleU_ToW(-axisLength), 0f,                                   Scale.ScaleU_ToW(zfactor*axisWidth),   // 3 -X end
                Scale.ScaleU_ToW(-axisLength), Scale.ScaleU_ToW(-yFactor*axisWidth), Scale.ScaleU_ToW(-zfactor*axisWidth),  // 4 
                Scale.ScaleU_ToW(-axisLength), Scale.ScaleU_ToW(yFactor*axisWidth),  Scale.ScaleU_ToW(-zfactor*axisWidth)   // 5

            };

            SharedAxisIndices = new ushort[]
            {
                // +X end   -X end
                  0, 1, 2,   3, 5, 4
                // Sides
                , 0, 3, 4,   0, 4, 1
                , 3, 0, 2,   3, 2, 5
                , 1, 4, 5,   1, 5, 2
            };

            // X axis 

            // Y axis, rotate about Z
            OpenTK.Mathematics.Quaternion q = Util.MakeQuaternion(new Vector3(0f, 0f, 1f), MathHelper.DegreesToRadians(90f));
            Y_AxisRotationMatrix = Matrix4.CreateFromQuaternion(q);

            // Z axis. rotate about Y
            q = Util.MakeQuaternion(new Vector3(0f, 1f, 0f), MathHelper.DegreesToRadians(90f));
            Z_AxisRotationMatrix = Matrix4.CreateFromQuaternion(q);

        }

        private void RenderAxis()
        {
            if (!IncludeAxis)
                return;

            // Axes look best drawn as wireframes
            bool wireframe = Wireframe;
            Wireframe = true;

            AxisShader.Use();

            Matrix4 vp = SimCamera.ViewMatrix * SimCamera.ProjectionMatrix;

            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), 0 /*GL.GetAttribLocation(Shader.ShaderHandle, "aPosition")*/);

            // Upload axis mesh
            GL.BufferData(BufferTarget.ArrayBuffer, SharedAxisMesh.Length * sizeof(Single), SharedAxisMesh, BufferUsageHint.StaticDraw);
            GL.BufferData(BufferTarget.ElementArrayBuffer, SharedAxisIndices.Length * sizeof(UInt16), SharedAxisIndices, BufferUsageHint.StaticDraw);

            // X axis, no rotation needed
            GL.Uniform4(AxisColorUniform, X_AxisColor);
            GL.UniformMatrix4(MVP_Uniform, false, ref vp);
            GL.DrawElements(PrimitiveType.Triangles, SharedAxisIndices.Length, DrawElementsType.UnsignedShort, 0);

            // Y axis
            GL.Uniform4(AxisColorUniform, Y_AxisColor);
            Matrix4 mvp = Y_AxisRotationMatrix * vp;
            GL.UniformMatrix4(MVP_Uniform, false, ref mvp);
            GL.DrawElements(PrimitiveType.Triangles, SharedAxisIndices.Length, DrawElementsType.UnsignedShort, 0);

            // Z axis
            GL.Uniform4(AxisColorUniform, Z_AxisColor);
            mvp = Z_AxisRotationMatrix * vp;
            GL.UniformMatrix4(MVP_Uniform, false, ref mvp);
            GL.DrawElements(PrimitiveType.Triangles, SharedAxisIndices.Length, DrawElementsType.UnsignedShort, 0);

            Wireframe = wireframe; //Restore
        }
        #endregion

    }
}

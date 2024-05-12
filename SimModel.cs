using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Media3D;

namespace OrbitalSimOpenGL
{
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
        public String? AppDataFolder { get; set; }

        private Double _IterationSeconds;
        public Double IterationSeconds  // Each frame iteration represents this many seconds of model simulation
        {
            get
            {
                return _IterationSeconds;
            }
            set
            {
                _IterationSeconds = value;
            }
        }
        public int TimeCompression { get; set; } = 1; // Number of times to iterate per frame
        public Double ElapsedSeconds { get; set; } = 0D;
        private Axis Axis { get; set; }
        public bool ShowAxis { get; set; } = true; // Render the three axis elements (X, Y, Z)
        public OrbitalSimWindow OrbitalSimWindow { get; set; }
        public Scale? Scale { get; set; }
        public SimCamera? SimCamera { get; set; }
        internal MassMass? MassMass { get; set; }
        internal SparseArray? SparseArray { get; set; }
        private CollisionDetector? CollisionDetector { get; set; }
        private ApproachDistances? ApproachDistances { get; set; }
        private Double PrevClosestApproachDistSquared { get; set; } = Double.MaxValue;

        // Closest approach between any two bodies captured here, for each iteration
        private Double _ClosestApproachDistSquared = -1D; // km-squared
        public Double ClosestApproachDistSquared // Distance between body surfaces on closest approach, last iteration
        { get { return _ClosestApproachDistSquared; } }

        // From each iteration, the list of closest approach bodies. 
        private List<int> _ClosestApproachBodiesList = new(5);
        public List<int> ClosestApproachBodiesList { get { return _ClosestApproachBodiesList; } }
        public bool SimRunning { get; set; } = false;
        internal SimBody? ShowStatsForSB { get; set; } = null;

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
        internal Barycenter? Barycenter { get; set; }
        public bool ShowBarycenter { get; set; }
        int VertexBufferObject { get; set; }
        int VertexArrayObject { get; set; }
        int ElementBufferObject { get; set; }
        #endregion

        public SimModel(OrbitalSimWindow orbitalSimWindow)
        {
            OrbitalSimWindow = orbitalSimWindow;
            SimCamera = OrbitalSimWindow.SimCamera;
            Scale = OrbitalSimWindow.Scale;

            GL.ClearColor(Color4.DarkGray); // LightGray

            // enable depth testing to ensure correct z-ordering of fragments
            GL.Enable(EnableCap.DepthTest);

            // Maximum number of vertex attributes supported
            //int nrAttributes = 0;
            //GL.GetInteger(GetPName.MaxVertexAttribs, out nrAttributes);

            IterationSeconds = 60D;

            Axis = new(SimCamera, Scale);

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
        }

        /// <summary>
        /// Reset sim to initial state
        /// </summary>
        /// <param name="ephemerismBodyList"></param>
        public void ResetScene(EphemerisBodyList ephemerisBodyList)
        {
            // Stop the sim, to avoid any timing and indetrerminate-state issues relative to
            // OpenTK's calling OrbitalSimWindow:OnRender during reset.
            SceneReady = false;
            ElapsedSeconds = 0D;

            PrevClosestApproachDistSquared = Double.MaxValue;

            SimBodyList = new SimBodyList(Scale, ephemerisBodyList, AppDataFolder);

            Barycenter = new(Scale, SimBodyList);
            MassMass = new(SimBodyList);
            SparseArray = new(SimBodyList.BodyList.Count);

            CollisionDetector = new(this);
            ApproachDistances = new(SimBodyList.BodyList.Count, SparseArray);

            NextPosition = new(SimBodyList, SparseArray, MassMass, GravConstantSetting);

            Wireframe = true;
            SceneReady = true;
            ShowBarycenter = true;
        }

        /// <summary>
        /// Render current state of the model
        /// </summary>
        /// <param name="ms">ms time of this call since last call to render</param>
        /// <param name="frameRateMS"></param>
        /// <param name="mousePosition">Current mouse cursor position, for hit-testing</param>
        /// <remarks>
        /// Renders the current model state
        /// </remarks>
        public void Render(int ms, int frameRateMS, System.Windows.Point mousePosition)
        {
            if (!SceneReady)
                return;

            if (SimBodyList is null) // JIC
                return;

            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            Iterate(SimRunning, ms, frameRateMS); // Perform model calculations

            if (ShowAxis)
                Axis.Render();

            // Render bodies
            // If render returns non-null sB then the mouse was over that body.
            SimBody sB;
            if (null != (sB = SimBodyList.Render(ms, SimCamera, mousePosition)))
                ShowStatsForSB = sB;

            if (ShowBarycenter)
            {
                Barycenter?.Calc();
                // Render can use same shaders as available in SimBodyList
                Barycenter?.Render(ms, SimCamera, SimBodyList.BodyColorUniform, SimBodyList.MVP_Uniform);
            }
        }

        /// <summary>
        /// Frame iteration
        /// </summary>
        /// <param name="ms">ms time of this call since last call to render</param>
        /// <param name="movingAvgMS">Moving average frame rate. A frame is happening on average every movingAvgMS</param>
        public void Iterate(bool simRunning, int ms, int movingAvgMS)
        {
            if (simRunning)
            {
                for (int i = 0; i < TimeCompression; i++)
                {
                    Double seconds = IterationSeconds;
                    NextPosition?.IterateOnce(ref seconds);

                    // Closest approach and collision detection
                    CollisionDetector?.Detect(PrevClosestApproachDistSquared, out _ClosestApproachDistSquared, ref _ClosestApproachBodiesList);

                    // Keep PrevClosestApproachDistSquared around to supply CollisionDetector. Saves work.
                    PrevClosestApproachDistSquared = ClosestApproachDistSquared;

                    ElapsedSeconds += seconds;
                }
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
        /// (Dis/en)able collision detection
        /// </summary>
        /// <param name="detect"></param>
        internal void DetectCollisions(bool detect)
        {
            if (CollisionDetector is not null)
                CollisionDetector.DetectCollisions = detect;
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

                // Tell CommandControlWindow (posts to msg queue and so returns quickly)
                OrbitalSimWindow.CommandControlWindow.BodyExcluded(sBI);
            }

            Barycenter?.SystemMassChanged();
        }

        /// <summary>
        /// Body has been renamed by the sim. Let others know
        /// </summary>
        /// <param name="bodyIndex"></param>
        /// <param name="bodyName"></param>
        internal void BodyRenamed(int bodyIndex, String bodyName)
        {
            OrbitalSimWindow.CommandControlWindow.BodyRenamed(bodyIndex, bodyName);
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
            Barycenter?.SystemMassChanged();
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

        internal void TracePath(bool onOff, String bodyName)
        {
            if (!SceneReady)
                return;

            if (SimBodyList is null) // JIC
                return;

            int sBI = SimBodyList.GetIndex(bodyName);
            SimBodyList.BodyList[sBI].TracePath(onOff);
        }
    }
}

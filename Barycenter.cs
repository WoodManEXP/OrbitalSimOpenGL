using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using System;
using System.Collections.Specialized;
using System.Runtime.InteropServices;

namespace OrbitalSimOpenGL
{
    /// <summary>
    /// Calculates and renders system barycenter (center of mass)
    /// </summary>
    public class Barycenter
    {
        #region Properties
        private SimBodyList SimBodyList { get; set; }
        private Scale Scale { get; set; }
        private Double SystemMass { get; set; }

        private Vector3d R; // location of barycenter after last Calc

        private Vector3[] WorldPoint;
        private readonly int Vector3Size = Marshal.SizeOf(typeof(Vector3));
        private static readonly Single BaryPointSize = 8F;

        // Color cycling for barycenter symbol
        private Color4 StartC = Color4.Yellow;
        private Color4 EndC = Color4.Red;
        private Color4 LastColor = Color4.Yellow;
        private Vector3 StartCVec;
        private Single CDist { get; set; }
        private Vector3 CVec;
        private int MS_SoFar { get; set; } = 0;
        private static int NumColorSteps { get; set; } = 20;
        private static int ColorCycleTime { get; set; } = 3000; // MS
        private static int MS_PerStep { get; set; } = ColorCycleTime / NumColorSteps;
        private int LastColorStep { get; set; } = -1;
        private bool ColorGoingUp { get; set; } = true;
        #endregion

        public Barycenter(Scale scale, SimBodyList simBodyList)
        {
            this.SimBodyList = simBodyList;
            this.Scale = scale;

            WorldPoint = new Vector3[1];

            SystemMassChanged();

            // Setup for color cycling
            StartCVec.X = StartC.R; StartCVec.Y = StartC.G; StartCVec.Z = StartC.B;
            Vector3 eC = new(EndC.R, EndC.G, EndC.B);
            CVec = eC - StartCVec;
            CDist = CVec.Length / (Single)NumColorSteps;
            CVec.Normalize();
            CVec *= CDist;

            LastColor.A = 1F; // Always 1F
        }

        /// <summary>
        /// Calculate current system total mass
        /// </summary>
        public void SystemMassChanged()
        {
            SystemMass = 0D;
            // Total system mass, non-excluded bodies
            foreach (SimBody sB in SimBodyList.BodyList)
                if (!sB.ExcludeFromSim)
                    SystemMass += sB.Mass;
        }

        /// <summary>
        /// Calculate barycenter for all bodies in SimBodyList
        /// </summary>
        /// <remarks>
        /// Center of Mass of a Many-Body System
        /// See 2.7.3 at
        /// https://phys.libretexts.org/Bookshelves/Classical_Mechanics/Variational_Principles_in_Classical_Mechanics_(Cline)/02%3A_Review_of_Newtonian_Mechanics/2.07%3A_Center_of_Mass_of_a_Many-Body_System
        /// </remarks>
        internal void Calc()
        {
            Vector3d iVec;

            R.X = R.Y = R.Z = 0D;

            foreach (SimBody sB in SimBodyList.BodyList)
            {
                if (!sB.ExcludeFromSim)
                {
                    iVec.X = sB.X; iVec.Y = sB.Y; iVec.Z = sB.Z;
                    R += iVec * sB.Mass;
                }
            }
            R /= SystemMass;
        }

        /// <summary>
        /// Render barycenter symbol
        /// </summary>
        internal void Render(int ms, SimCamera simCamera, int bodyColorUniform, int mvp_Uniform)
        {
            FrustumCuller fC = simCamera.FrustumCuller;

            MS_SoFar += ms;

            int colorStep = (MS_SoFar % ColorCycleTime) / MS_PerStep;

            // Does color change?
            if (LastColorStep != colorStep)
            {
                if (colorStep < LastColorStep)
                    ColorGoingUp = !ColorGoingUp;
                LastColorStep = colorStep;

                // Color is changing, interpolate between the colors
                // Color moves back and forth between the two, pendulum style.
                if (!ColorGoingUp)
                    colorStep = NumColorSteps - colorStep;

                Vector3 sCVec = StartCVec + colorStep * CVec;
                LastColor.R = sCVec.X; LastColor.G = sCVec.Y; LastColor.B = sCVec.Z;
            }

            //System.Diagnostics.Debug.WriteLine("Barycenter:Render:"
            //        + " colorStep:" + colorStep.ToString()
            //        );

            if (!fC.SphereCulls(ref R, 0D))
            {
                Scale.ScaleU_ToW(out WorldPoint[0], R.X, R.Y, R.Z); // From Univ to OpenGL coords

                // Push it into the GPU and draw
                GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, Vector3Size, 0);
                GL.BufferData(BufferTarget.ArrayBuffer, Vector3Size, WorldPoint, BufferUsageHint.StaticDraw); // Just one point

                GL.Uniform4(bodyColorUniform, LastColor);
                GL.UniformMatrix4(mvp_Uniform, false, ref simCamera._VP_Matrix);

                Single ptSize = GL.GetFloat(GetPName.PointSize);
                GL.PointSize(BaryPointSize);
                GL.DrawArrays(PrimitiveType.Points, 0, 1);
                GL.PointSize(ptSize);
            }
        }
    }
}
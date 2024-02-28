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
    /// <summary>
    /// Renders collision highlights
    /// </summary>
    internal class CollisionHighlighter
    {

        #region Properties

        private readonly int Vector3Size = Marshal.SizeOf(typeof(Vector3));
        internal SimBody SimBody { get; set; }
        internal Scale? Scale { get; set; }
        private bool Highlighting { get; set; } = true;
        private int MS_SoFar { get; set; } = 0;
        static private Single HighlightDuration { get; } = 1000F * 0.6E1F; // 3.5 seconds

        private Vector3d[] ParticlePath { get; set; } // Unit vectors representing particle paths

        private Vector3d[] ParticlePosition; // Contains particles on a path, U Coords
        private Vector3[] WorldPoints;

        private Vector3d BodyLocation;

        private Double[] PathLength {get; set;}
        private Double[] NumParticles { get; set; }
        private static int NumPaths { get; } = 50;
        private static int MaxParticles { get; } = 50;
        private static Double PathMultiplier { get; } = 60D; // Times radius of body
        private static readonly Single ParticlePointSize = 2F;

        // Color fadeout
        private static Single FadeoutPct = 7e-1F; // 70%
        private static Color4 StartC = Color4.Red;
        private static Color4 EndC = Color4.DarkGray;
        private Vector3 StartCVec;
        private Vector3 CVec;
        private Single CDist;
        #endregion

        /// <summary>
        /// Renders collision highlights
        /// </summary>
        /// <param name="simBody">To be highlighted</param>
        /// <remarks>
        /// https://en.wikipedia.org/wiki/Spherical_coordinate_system
        /// </remarks>
        internal CollisionHighlighter(SimBody simBody)
        {
            SimBody = simBody;
            Scale = simBody.Scale;

            // Make a series of NumPaths random unit vectors and path lengths, each representing path of projectiles from the explosion
            ParticlePath = new Vector3d[NumPaths];
            ParticlePosition = new Vector3d[1 + MaxParticles];
            WorldPoints = new Vector3[1 + MaxParticles];
            PathLength = new Double[NumPaths];
            NumParticles = new Double[NumPaths];

            var rand = new Random();
            Double twoPi = 2D * Math.PI;
            Double maxPathLength = PathMultiplier * simBody.UseDiameter;
            Double twoDiameters = 2D * simBody.UseDiameter;

            for (int i=0;i<NumPaths;i++)
            {
                Double polarAngle = rand.NextDouble() * twoPi;
                Double azimuthAngle = rand.NextDouble() * twoPi;
                ParticlePath[i].X = Math.Sin(polarAngle) * Math.Cos(azimuthAngle);
                ParticlePath[i].Y = Math.Sin(polarAngle) * Math.Sin(azimuthAngle);
                ParticlePath[i].Z = Math.Cos(polarAngle);
                PathLength[i] = twoDiameters + rand.NextDouble() * maxPathLength;   // U Coords
                NumParticles[i] = MaxParticles * rand.NextDouble();
            }

            // Setup for color fadeout
            StartCVec.X = StartC.R; StartCVec.Y = StartC.G; StartCVec.Z = StartC.B;
            CVec.X = StartC.R; CVec.Y = StartC.G; CVec.Z = StartC.B;
            Vector3 eC = new(EndC.R, EndC.G, EndC.B);
            CVec = eC - CVec;
            CDist = CVec.Length;
            CVec.Normalize();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ms">ms time of this call since last call to render</param>
        /// <param name="simCamera"></param>
        /// <param name="bodyColorUniform"></param>
        /// <param name="mvp_Uniform"></param>
        /// Color4.DarkGray
        internal void Render(int ms, SimCamera simCamera, int bodyColorUniform, int mvp_Uniform)
        {
            if (!Highlighting)
                return;

            Color4 color;

            MS_SoFar += ms;

            // Where is body at this time?
            BodyLocation.X = SimBody.X;
            BodyLocation.Y = SimBody.Y;
            BodyLocation.Z = SimBody.Z;

            Single pct = MS_SoFar / HighlightDuration;

            // During last nn% of explosion fade particles to background color
            if (pct < FadeoutPct)
                color = StartC;
            else
            {
                // Fadeout
                Single colorPct = (pct - FadeoutPct) / (1F - FadeoutPct);
                Vector3 sCVec = StartCVec + (colorPct * CDist * CVec);
                color.R = sCVec.X; color.G = sCVec.Y; color.B = sCVec.Z;
                color.A = 1F; // Always 1F
            }

            Single ptSize = GL.GetFloat(GetPName.PointSize);
            GL.PointSize(ParticlePointSize);
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, Vector3Size, 0);

            // Construct and draw each of the projectile paths
            for (int i = 0; i < NumPaths; i++)
            {
                // Number of particles on this path so far
                int numParticles = 1 + (int)(pct * NumParticles[i]);

                // Distribute the particles evenly along the path, U Coords
                Double dist = pct * PathLength[i] / numParticles;
                for(int j=0; j<numParticles; j++) // First particle not drawn at (0,0,0)
                    ParticlePosition[j] = BodyLocation + ((1 + j) * dist * ParticlePath[i]);

                // Render particles on the path
                Scale?.ScaleU_ToW(ref WorldPoints, ref ParticlePosition); // Froim U to W coords

                GL.BufferData(BufferTarget.ArrayBuffer, Vector3Size, WorldPoints, BufferUsageHint.StaticDraw); // Just one point

                GL.Uniform4(bodyColorUniform, color);
                GL.UniformMatrix4(mvp_Uniform, false, ref simCamera._VP_Matrix);

                GL.DrawArrays(PrimitiveType.Points, 0, numParticles);
            }

            GL.PointSize(ptSize);

            // Set Highlighting to false when finished
            if (MS_SoFar >= HighlightDuration)
                Highlighting = false;
        }
    }
}

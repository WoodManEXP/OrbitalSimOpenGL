using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using System;
using System.Reflection;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;

namespace OrbitalSimOpenGL
{
    /// <summary>
    /// Each SimBody reoresents a body in the simulation
    /// </summary>
    internal class SimBody
    {
        #region Properties
        internal Scale? Scale { get; set; }

        // Current settings, U coords
        public Double X { get; set; } // km
        public Double Y { get; set; } // km
        public Double Z { get; set; } // km
        public Double PX { get; set; } // km
        public Double PY { get; set; } // km
        public Double PZ { get; set; } // km
        public Double VX { get; set; } // km/s
        public Double VY { get; set; } // km/s
        public Double VZ { get; set; } // km/s

        // Force vectors
        public Vector3d CurrIntervalFV;        // Force vectors acting on body at interval beginning
        public Vector3d PrevIntervalFV;    // Force vectors acting on body at previous interval beginning

        public bool CoastThisInterval { get; set; } = false;

        // Radius-squared of sphere encompassing travel from last iteration. Includes dist traveled + body radius.
        // Precalculated for collision rule-out.
        public Double LastPathRadiusSquared { get; set; } // On last iteration

        // Coordinate at middle of last iteration's travel path.
        // Precalculated for collision rule-out.
        public Vector3d MiddleOfPath;

        // Other settings from Ephemeris reading
        public Double LT { get; set; }
        public Double RG { get; set; }
        public Double RR { get; set; }
        public Double EphemerisDiameter { get; set; } // U coords
        public Double UseDiameter { get; set; } // U coords, gets adjusted for visability
        public Double EphemerisRaduis { get; set; }
        public Double EphemerisRaduisSquared { get; set; }

        private Double _Mass;
        public Double Mass
        {
            get { return _Mass * MassMultiplier; }
            set { _Mass = value; }
        }
        public Double GM { get; set; }
        public string ID { get; private set; }
        public string Name { get; set; }

        private Color4 _BodyColor;
        Color4 BodyColor { get { return _BodyColor; } set { _BodyColor = value; } }
        public bool ExcludeFromSim { get; set; } = false; // Body is to be or not be excluded from the sim
        public bool DisplayApproaches { get; set; } = false; // Stats monitors this
        private PathTracer? PathTracer { get; set; } = null;

        private Double _MassMultiplier = 1D;
        public Double MassMultiplier
        {
            get { return _MassMultiplier; }
            set // < 0 divides mass by that value, > 0 multiplies mass by that value, 0 sets to std value
            {
                if (value < 0D)
                    _MassMultiplier = -1D / value;
                else if (value > 0D)
                    _MassMultiplier = value;
                else
                    _MassMultiplier = 1D; // Std
            }
        }
        internal CollisionHighlighter? CollisionHighlighter { get; set; }
        #endregion

        public SimBody(Scale scale, EphemerisBody ephemerisBody)
        {
            Double dVal;

            Scale = scale; ;

            // Save ephemeris values into current settings (universe coords)
            X = Double.TryParse(ephemerisBody.X_Str, out dVal) ? dVal : -1D;
            Y = Double.TryParse(ephemerisBody.Y_Str, out dVal) ? dVal : -1D;
            Z = Double.TryParse(ephemerisBody.Z_Str, out dVal) ? dVal : -1D;
            VX = Double.TryParse(ephemerisBody.VX_Str, out dVal) ? dVal : -1D;
            VY = Double.TryParse(ephemerisBody.VY_Str, out dVal) ? dVal : -1D;
            VZ = Double.TryParse(ephemerisBody.VZ_Str, out dVal) ? dVal : -1D;
            LT = Double.TryParse(ephemerisBody.LT_Str, out dVal) ? dVal : -1D;
            RG = Double.TryParse(ephemerisBody.RG_Str, out dVal) ? dVal : -1D;
            RR = Double.TryParse(ephemerisBody.RR_Str, out dVal) ? dVal : -1D;
            Mass = Double.TryParse(ephemerisBody.MassStr, out dVal) ? dVal : -1D;
            GM = Double.TryParse(ephemerisBody.GM_Str, out dVal) ? dVal : -1D;

            // ID and Name are also useful
            ID = ephemerisBody.ID;
            Name = ephemerisBody.Name;

            // Body color
            Colors.GetColor4(ephemerisBody.ColorStr, out _BodyColor);

            EphemerisDiameter = Double.TryParse(ephemerisBody.DiameterStr, out dVal) ? dVal : -1D;
            if (0D == EphemerisDiameter)
            {
                // For bodies with 0 diameter instead calculate the Schwarzschild radius (https://en.wikipedia.org/wiki/Schwarzschild_radius)
                EphemerisRaduis = ((2D * Util.G_M * Mass) / Util.CSquared_M) / 1E3D;
                EphemerisDiameter = 2D * EphemerisRaduis;
            }
            else
                EphemerisRaduis = EphemerisDiameter / 2D;

            EphemerisRaduisSquared = EphemerisRaduis * EphemerisRaduis;
        }

        /// <summary>
        /// Render body using the shared sphere
        /// </summary>
        /// <param name="ms">ms time of this call since last call to render</param>
        /// <param name="indicesLength"></param>
        /// <param name="bodyColorUniform">OpenGL shader uniforn number for color</param>
        /// <param name="mvp_Uniform">OpenGL shader uniforn number for mvp matrix4</param>
        /// <param name="vp">View Projection matrix</param>
        /// <param name="locationMatrix4">Will be used/modified to set loc for this body</param>
        /// <param name="sizeMatrix4">Will be used/modified to set size for this body</param>
        /// <remarks>
        /// Assumes the shared unit sphere is what's currently loaded in OpenGL ArrayBuffer. So this
        /// is applying a set of transformations to that sphere to render this body.
        /// </remarks>
        internal void Render(int ms, SimCamera simCamera, int indicesLength, int bodyColorUniform, int mvp_Uniform, ref Matrix4 locationMatrix4, ref Matrix4 sizeMatrix4)
        {
            if (ExcludeFromSim)
                return;

            locationMatrix4.M41 = Scale.ScaleU_ToW(X); // X
            locationMatrix4.M42 = Scale.ScaleU_ToW(Y); // Y
            locationMatrix4.M43 = Scale.ScaleU_ToW(Z); // Z

            sizeMatrix4.M11 = sizeMatrix4.M22 = sizeMatrix4.M33 = Scale.ScaleU_ToW(UseDiameter);

            Matrix4 mvp = sizeMatrix4 * locationMatrix4 * simCamera.VP_Matrix;

            GL.Uniform4(bodyColorUniform, BodyColor);
            GL.UniformMatrix4(mvp_Uniform, false, ref mvp);

            GL.DrawElements(PrimitiveType.Triangles, indicesLength, DrawElementsType.UnsignedShort, 0);
        }

        #region Highlights/Collisions/Path
        public void HighlightCollision()
        {
            // These are never freed after collision highlight completes. But this event is most infrequent.
            CollisionHighlighter = new(this);
        }

        /// <summary>
        /// Render body path
        /// </summary>
        /// <param name="fC"></param>
        internal void RenderPath(FrustumCuller fC, int bodyColorUniform, int mvp_Uniform, ref Matrix4 vp)
        {
            PathTracer?.Render(fC, BodyColor, bodyColorUniform, mvp_Uniform, ref vp); // If PathTracer is available...
        }
        #endregion
        public void RenderHighlight(int ms, SimCamera simCamera, int bodyColorUniform, int mvp_Uniform)
        {
            // Collision highlighting
            CollisionHighlighter?.Render(ms, simCamera, bodyColorUniform, mvp_Uniform);
        }

        /// <summary>
        /// Get curreent position of a body (x, y, z)
        /// </summary>
        /// <returns>Point3D in universe coords</returns>
        public void GetPosition(ref Vector3d position)
        {
            position.X = X;
            position.Y = Y;
            position.Z = Z;
        }

        /// <summary>
        /// Update body's position and velocity vectors
        /// </summary>
        /// <param name="seconds">At this velocity</param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        /// <param name="vX"></param>
        /// <param name="vY"></param>
        /// <param name="vZ"></param>
        /// <remarks>
        /// Called for each iteration
        /// </remarks>
        public void SetPosAndVel(Double seconds, Double x, Double y, Double z, Double vX, Double vY, Double vZ)
        {
            Double d;

            // Calc radius-squared of sphere encompassing travel from last iteration.
            LastPathRadiusSquared = EphemerisRaduisSquared;
            d = X - x; d *= d;
            LastPathRadiusSquared += d;
            d = Y - y; d *= d;
            LastPathRadiusSquared += d;
            d = Z - z; d *= d;
            LastPathRadiusSquared += d;

            // Calc position at middle of travel path
            MiddleOfPath.X = (X + x) / 2D;
            MiddleOfPath.Y = (Y + y) / 2D;
            MiddleOfPath.Z = (Z + z) / 2D;

            PX = X; PY = Y; PZ = Z; // Previous position coords
            X = x; Y = y; Z = z;
            VX = vX; VY = vY; VZ = vZ;
            PathTracer?.AddLoc(seconds, x, y, z, vX, vY, vZ); // If there is a PathTracer
        }

        //        int iCtr = 0;

        /// <summary>
        /// If body has become too small in 3D to 2D projection adjust its size so it remains visible.
        /// And hit testing.
        /// </summary>
        /// <param name="simCamera">Camera</param>
        /// <param name="halfNorm">Camera's UpVector * .5 </param>
        /// <param name="minSize">min pixel diameter for rendered bodies</param>
        /// <param name="minSizeSquared">min pixel diameter squared for rendered bodies</param>
        /// <param name="mousePosition">Current mouse cursor position, for hit-testing</param>
        /// <returns>
        /// -1D if 2D mouseCursor position is NOT over the rendered body.
        /// Distance, in km, from cameraPosition to point on sphere 2D mouseCursor position is over.
        /// Can be used as a Z value to determine which hit to use (if there are multiples).
        /// renderingPrecision - some calculations are lost in single precision (e.g. small bodies + large distances). If
        /// that condition is detected renderingPrecision will be set to DoublePrecision, otherwise SinglePrecision.
        /// </returns>
        /// <remarks>
        /// This works only on spheres. The hit testing is a poor-man's RayCasting. Becasue each body visible in the 
        /// frustum is examined before being sent to OpenGL an inexpensive oppty avails to perfom hit testing.
        /// All calculations via double precision.
        /// </remarks>
        public Double KeepVisible(SimCamera simCamera, ref Vector3d halfNorm, Single minSize, Single minSizeSquared,
            ref System.Windows.Point mousePosition)
        {
            // Center of body in Normalized Device Coords
            Single distSquared;

            // Small bodies are lost in Single precision rounding errors.
            // In these cases jump the calculation to Double precision.

            Double distSquaredD;
            DistSquaredD(simCamera, EphemerisRaduis, ref halfNorm, minSize, out distSquaredD, out Vector2 ndcCenter);
            distSquared = (Single)distSquaredD;

            //                Single distSquared;
            //                DistSquared(simCamera, HalfEphemerisDiameter, ref halfNorm, minSize, out distSquared, out ndcCenter);

            if (distSquaredD < (Double)minSizeSquared)
                // Change the body's diameter such that distSquared will transform to minSizeSquared
                UseDiameter = (minSize * EphemerisDiameter) / Math.Sqrt(distSquaredD);
            else
                // Not too small, keep the diameter as the original value
                UseDiameter = EphemerisDiameter;

            // What is new distSquareD after diameter adjustment
            //Double newDistSquaredD;
            //DistSquaredD(simCamera, UseDiameter / 2D, ref halfNorm, minSize, out newDistSquaredD, out ndcCenter);

            //Single newDistSquared;
            //DistSquared(simCamera, UseDiameter / 2D, ref halfNorm, minSize, out newDistSquared, out ndcCenter);

            // Hit-test section
            // OpenGL's window-space is relative to the bottom-left of the window, not the top-left as in Windows.
            Single cX = ((ndcCenter.X + 1F) / 2F) * simCamera.ViewWidth;
            Single cY = ((1F - ndcCenter.Y) / 2F) * simCamera.ViewHeight;

            Single distSquared2 = (distSquared < minSizeSquared) ? minSizeSquared : distSquared;

            // distSquared from mouse cursor position to Center of sphere as projected onto 2D
            Single mX = (Single)mousePosition.X - cX; mX *= mX;
            Single mY = (Single)mousePosition.Y - cY; mY *= mY;
            Single mouseDistSquared = mX + mY;

            //GL.Viewport  GL_VIEWPORT

            //            int[] iArray = new int[4];
            //            GL.GetInteger(GetPName.Viewport, iArray);
#if false
            if (0 == ++Ctr % 100)
                System.Diagnostics.Debug.WriteLine("SimBody.KeepVisible - " + Name
                    + " distSquaredInt " + distSquared.ToString()
                    + " mouseDistSquared " + mouseDistSquared.ToString()
                    + " cX, cY " + cX.ToString() + ", " + cY.ToString()
                    + " mouseX, mouseY " + mousePosition.X.ToString() + ", " + mousePosition.Y.ToString()
                    + ((mouseDistSquared <= distSquared) ? " Hit" : " No hit")
                    );
#endif
            Double dist;
            if (mouseDistSquared <= distSquared2)
            {
                // A hit
                Vector3D distVector3D;
                distVector3D.X = this.X - simCamera.CameraPosition.X;
                distVector3D.Y = this.Y - simCamera.CameraPosition.Y;
                distVector3D.Z = this.Z - simCamera.CameraPosition.Z;

                // Dist from camera position to point on sphere where ray cast would intersect
                dist = distVector3D.Length - (EphemerisDiameter / 2);
            }
            else
                dist = -1;

            return dist;
        }

        /// <summary>
        /// Calculate distSquared in double precision
        /// </summary>
        /// <param name="simCamera"></param>
        /// <param name="diameter"></param>
        /// <param name="halfNorm"></param>
        /// <param name="minSize"></param>
        /// <param name="distSquared"></param>
        /// <param name="pixelDiameter"></param>
        /// <param name="ndcCenter"></param>
        private void DistSquaredD(SimCamera simCamera, Double halfD, ref Vector3d halfNorm, Single minSize, out Double distSquared,
                    out Vector2 ndcCenter)
        {
            // Calc Point3D coordinates for both ends of a vector parallel to camera's UpDirection,
            // centered at sB.X, sB.Y, sB.Z with length = sB's diameter. These represents widest
            // points of sphere as seen by the camera.
            Vector3d oneSideUniv = new(X + halfD * halfNorm.X, Y + halfD * halfNorm.Y, Z + halfD * halfNorm.Z);
            Vector3d otherSideUniv = new(X - halfD * halfNorm.X, Y - halfD * halfNorm.Y, Z - halfD * halfNorm.Z);

            // U coords to W coords
            Scale.ScaleVector3D(ref oneSideUniv);
            Scale.ScaleVector3D(ref otherSideUniv);

            // Make 3D to Homogenous 4D
            Vector4d oneSideV4 = new(oneSideUniv, 1d);
            Vector4d otherSideV4 = new(otherSideUniv, 1d);

            // To clip space (normalized device coordinates), -1 .. 1 (through View and projection matrices)
            Vector4d oneSide = oneSideV4 * simCamera.VP_MatrixD;
            Vector4d otherSide = otherSideV4 * simCamera.VP_MatrixD;
            oneSide /= oneSide.W;
            otherSide /= otherSide.W;

            // Retain center point ndc, normalized device coordinates, (for hit-testing below)
            ndcCenter = new((Single)((oneSide.X + otherSide.X) / 2D), (Single)((oneSide.Y + otherSide.Y) / 2D));

            // To pixel values
            // https://stackoverflow.com/questions/8491247/c-opengl-convert-world-coords-to-screen2d-coords
            Double oneSideX = ((oneSide.X + 1D) / 2D) * simCamera.ViewWidth;
            Double oneSideY = ((oneSide.Y + 1D) / 2D) * simCamera.ViewHeight;
            Double otherSideX = ((otherSide.X + 1D) / 2D) * simCamera.ViewWidth;
            Double otherSideY = ((otherSide.Y + 1D) / 2D) * simCamera.ViewHeight;

            Double dX = oneSideX - otherSideX;
            Double dY = oneSideY - otherSideY;
            distSquared = dX * dX + dY * dY;
        }

        /// <summary>
        /// Alter current velocity
        /// </summary>
        /// <param name="iMultiplier"></param>
        /// <remarks>
        /// < 0 divides current velocity by that value, > 0 multiplies current velocity by that value, 0 leaves it unchanged.
        /// </remarks>
        internal void AlterVelocity(int iMultiplier)
        {
            Double dMultiplier;

            if (0 == iMultiplier)
                return; // Mo change

            if (iMultiplier < 0)
                dMultiplier = -1D / (Double)iMultiplier;
            else
                dMultiplier = (Double)iMultiplier;

            VX *= dMultiplier;
            VY *= dMultiplier;
            VY *= dMultiplier;
        }

        /// <summary>
        /// Path trace on or off
        /// </summary>
        /// <param name="onOff"></param>
        /// <remarks>
        /// If turned off path history for the body is lost.
        /// </remarks>
        internal void TracePath(bool onOff)
        {
            if (onOff)
                PathTracer ??= new(Scale); // Start a PathTracer for this body, if not already started (the null-coalescing operators)
            else
                PathTracer = null; // Let go the resources
        }

        /// <summary>
        /// ApproachDistance on or off
        /// </summary>
        /// <param name="onOff"></param>
        /// <remarks>
        /// If turned off path history for the body is lost.
        /// </remarks>
        internal void ApproachDistance(bool onOff)
        {
            DisplayApproaches = onOff;
        }
    }
}

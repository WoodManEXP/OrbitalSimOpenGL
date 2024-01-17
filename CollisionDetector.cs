using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OrbitalSimOpenGL
{
    internal class CollisionDetector
    {
        /// <summary>
        /// Handles collisions beween bodies
        /// </summary>
        #region Properties
        private SimBodyList SimBodyList { get; set; }
        private MassMass MassMass { get; set; }
        #endregion

        public CollisionDetector(SimBodyList simBodyList, MassMass massMass)
        {
            MassMass = massMass;
            SimBodyList = simBodyList;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="approachDistSquared">Closest approach between two bodies detected, distance between surfaces</param>
        /// <param name="bA">Index of one body in closest approach</param>
        /// <param name="bB">Index of other body in closest approac</param>
        internal void Detect(out Double approachDistSquared, out int bA, out int bB)
        {

            int numBodies = SimBodyList.BodyList.Count;

            Vector3d lBodyPos, hBodyPos;
            SimBody lBody, hBody;

            approachDistSquared = Double.MaxValue;
            bA = bB = -1;

            // What about simultaneous collision of >2 bodies ?
            for (int bL = 0; bL < numBodies; bL++)       // bL - body low number
                for (int bH = 0; bH < numBodies; bH++)    // bH - body high number
                {
                    // Nothing for diagonal or entries below the diagonal
                    if (bH <= bL)
                        continue;

                    // Collision?
                    lBody = SimBodyList.BodyList[bL];
                    lBodyPos.X = lBody.X;
                    lBodyPos.Y = lBody.Y;
                    lBodyPos.Z = lBody.Z;

                    hBody = SimBodyList.BodyList[bH];
                    hBodyPos.X = hBody.X;
                    hBodyPos.Y = hBody.Y;
                    hBodyPos.Z = hBody.Z;

                    lBodyPos -= hBodyPos;

                    Double lRadius = lBody.HalfEphemerisDiameter;
                    Double hRadius = hBody.HalfEphemerisDiameter;
                    lRadius += hRadius;
                    lRadius *= lRadius;

                    Double lenSquared = lBodyPos.LengthSquared;

                    if (lenSquared < lRadius)
                    {
                        // Collision
                        System.Diagnostics.Debug.WriteLine("CollisionDetector:DetectCollision, bodies "
                            + lBody.Name + ", " + hBody.Name
                            );
                    }

                    // Closest approach
                    if (lenSquared < approachDistSquared)
                    {
                        approachDistSquared = lenSquared - lRadius; // Distance between surfaces
                        bA = bL;
                        bB = bH;
                    }
                }
        }
    }
}

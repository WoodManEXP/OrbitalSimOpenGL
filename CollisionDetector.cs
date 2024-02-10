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

        private List<int> _CollisionList = new(3);
        private List<int> CollisionList { get { return _CollisionList; } }

        private static readonly Double CollisionMassLoss = 9e-1D; // Body mass used for collisions, reduction repreents heat loss
        #endregion

        public CollisionDetector(SimBodyList simBodyList, MassMass massMass)
        {
            MassMass = massMass;
            SimBodyList = simBodyList;
        }

        /// <summary>
        /// Detect closest approach and process collisions
        /// </summary>
        /// <param name="onlyIfLessThanOrEqualToThisD2"> Process closest approach onlyIfLessThanOrEqualTo this value,
        /// an efficiency check. This is also a distSquared value. No need to gather approaches if none are less than any earlier encountered.
        /// </param>
        /// <param name="approachDistSquared">out, closest approach distance this pass</param>
        /// <param name="closestApproachBodies">ref, to List of bodies involved in closest approach</param>
        /// <remrks>
        /// </remrks>
        internal void Detect(Double onlyIfLessThanOrEqualToThisD2, out Double approachDistSquared, ref List<int> closestApproachBodies)
        {

            int numBodies = SimBodyList.BodyList.Count;

            Vector3d lBodyPos, hBodyPos;
            SimBody lBody, hBody;

            approachDistSquared = Double.MaxValue;
            closestApproachBodies.Clear();
            CollisionList.Clear();

            for (int bL = 0; bL < numBodies; bL++)       // bL - body low number
                for (int bH = 0; bH < numBodies; bH++)    // bH - body high number
                {
                    // Nothing for diagonal or entries below the diagonal
                    if (bH <= bL)
                        continue;

                    lBody = SimBodyList.BodyList[bL];
                    hBody = SimBodyList.BodyList[bH];

                    // If either previously excluded from Sim
                    if (lBody.ExcludeFromSim || hBody.ExcludeFromSim)
                        continue;

                    // Collision?
                    lBodyPos.X = lBody.X;
                    lBodyPos.Y = lBody.Y;
                    lBodyPos.Z = lBody.Z;

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

                        // Two colliding
                        if (!CollisionList.Contains(bL))
                            CollisionList.Add(bL);
                        if (!CollisionList.Contains(bH))
                            CollisionList.Add(bH);
                    }

                    // Collect the 2+ bodies that have lowest (possibly same) closest approach
                    if (lenSquared <= approachDistSquared && lenSquared <= onlyIfLessThanOrEqualToThisD2)
                    {
                        if (lenSquared < approachDistSquared)
                        {
                            // New closest approach
                            closestApproachBodies.Clear();
                            closestApproachBodies.Add(bL);
                            closestApproachBodies.Add(bH);
                        }
                        else
                        {
                            // Beyond 2 bodies at same closest approach
                            if (!closestApproachBodies.Contains(bL))
                                closestApproachBodies.Add(bL);
                            if (!closestApproachBodies.Contains(bH))
                                closestApproachBodies.Add(bH);
                        }
                        approachDistSquared = lenSquared - lRadius; // Distance between surfaces
                    }
                }

            if (CollisionList.Count > 0)
                ProcessCollisions();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <remarks>
        /// - Inelastic collisions
        /// - Bodies mass reduced to represent heat loss
        /// - Collapsed into single body, largest mass body stays, others excluded from sim
        /// - Name changes to largest-smaller-smaller ....
        /// - MassMass recalculation
        /// </remarks>
        private void ProcessCollisions()
        {
            Double massTotal = 0D, largestMass = 0D;
            Vector3d rVec = new(0D, 0d, 0D);
            Vector3d tVec;
            SimBody sB;
            int largestMassBodyIndex = -1 ;

            // Gather parts of the inelastic collision equation 
            for(int i=0; i<CollisionList.Count; i++)
            {
                int bodyIndex = CollisionList[i];

                Double tMass;
                sB = SimBodyList.BodyList[bodyIndex];
                tVec.X = sB.VX; tVec.Y = sB.VY; tVec.Z = sB.VZ;
                tMass = CollisionMassLoss * sB.Mass;
                rVec += tMass * tVec;
                massTotal += tMass;

                if (sB.Mass > largestMass)
                {
                    largestMassBodyIndex = bodyIndex;
                    largestMass = sB.Mass;
                }
            }

            // Resultant velocity vec
            rVec /= massTotal;

            // Build up new body name
            // Mark others in the collision as excluded
            String newBodyName = new(SimBodyList.BodyList[largestMassBodyIndex].Name);
            for (int i = 0; i < CollisionList.Count; i++)
            {
                int bodyIndex = CollisionList[i];
                sB = SimBodyList.BodyList[bodyIndex];

                if (largestMassBodyIndex != i)
                {
                    newBodyName += " - " + sB.Name;
                    sB.ExcludeFromSim = true;
                }
            }

            // Reset a few things in the remaining body
            sB = SimBodyList.BodyList[largestMassBodyIndex];
            sB.Name = newBodyName;
            sB.Mass = massTotal;
            sB.VX = rVec.X; sB.VY = rVec.Y; sB.VZ = rVec.Z;

            MassMass?.CalcMassMass(SimBodyList);
        }
    }
}

using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OrbitalSimOpenGL
{
    /// <summary>
    /// Calc approach distances and detect collisions
    /// </summary>
    /// <remarks>
    /// As the sim is incermental, bodies travel in increments, along a vector from their previous
    /// location to current location. As time increments increase and vectors lengthen  there is increasing probability of missing
    /// an ever smaller appoach distance or a collision as bodies are repositioned through the larger increments.
    /// An algorithm is used to calculate Closest Approach between Two Objects Moving Along 
    /// Straight Line Segments with Common Start Times and Common End Times. Essentially detecting if objects
    /// happened to pass closely or collide across the increment.
    /// https://www.kestrel.edu/people/fitzpatrick/pub/TechnicalNote-2019-ClosestApproach.pdf
    /// This is a computationally expensive, Double-precision calculation...
    /// </remarks>
    internal class CollisionDetector
    {
        /// <summary>
        /// Handles collisions beween bodies
        /// </summary>
        #region Properties
        private SimModel SimModel { get; set; }
        private SimBodyList? SimBodyList { get; set; }
        private MassMass? MassMass { get; set; }
        private Barycenter? Barycenter { get; set; }

        private List<int> _CollisionList = new(3);
        private List<int> CollisionList { get { return _CollisionList; } }

        private static readonly Double CollisionMassLoss = 9e-1D; // Body mass used for collisions, reduction represents heat loss

        private Vector3d DeltaS = new();
        private Vector3d DeltaE = new();
        #endregion

        public CollisionDetector(SimModel simModel)
        {
            SimModel = simModel;
            MassMass = SimModel.MassMass;
            Barycenter = SimModel.Barycenter;
            SimBodyList = SimModel.SimBodyList;
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

                    // No need to calc closest approach, looking for collision,
                    // if the two bodies could not have collided.

                    // Dist-squared between middle of each body's last path
                    Double d, distSquared;
                    d = lBody.MiddleOfPath.X - hBody.MiddleOfPath.X; distSquared = d * d;
                    d = lBody.MiddleOfPath.Y - hBody.MiddleOfPath.Y; distSquared += d * d;
                    d = lBody.MiddleOfPath.Z - hBody.MiddleOfPath.Z; distSquared += d * d;

                    // Could this possibly be a collision?
                    bool possibleCollision = (distSquared <= (lBody.LastPathRadiusSquared + hBody.LastPathRadiusSquared));

                    // Could this possibly represent a new closest approach?
                    bool possibleClosestApproach = (distSquared <= onlyIfLessThanOrEqualToThisD2);

                    if (!possibleCollision && !possibleClosestApproach)
                        continue; // No possible collision or closest approach, no need to continue calculations

                    // Closest approach of the two bodies along their most recent movement vectors.
                    // DeltaS - starting positions, DeltaE - ending positions
                    DeltaS.X = lBody.PX - hBody.PX;
                    DeltaS.Y = lBody.PY - hBody.PY;
                    DeltaS.Z = lBody.PZ - hBody.PZ;
                    DeltaE.X = lBody.X - hBody.X;
                    DeltaE.Y = lBody.Y - hBody.Y;
                    DeltaE.Z = lBody.Z - hBody.Z;
                    //Double k = Math.Max(0D, Math.Min(1D, Vector3d.Dot(DeltaS, DeltaS - DeltaE) / (DeltaS - DeltaE).LengthSquared));
                    Double k = Vector3d.Dot(DeltaS, DeltaS - DeltaE) / (DeltaS - DeltaE).LengthSquared;

                    // If (k < 0 or k > 1) the closest approach is beyond the current travel vectors. This test ignores fact that bodies
                    // have a radius > 0, so bodies could have begun a collision.
                    // Super low probability event and will anyway be picked up on next iteration.
                    if (0D > k || 1D < k)
                        continue;

                    // Where were the two bodies at K ?
                    lBodyPos.X = lBody.PX + k * (lBody.X - lBody.PX);
                    lBodyPos.Y = lBody.PY + k * (lBody.Y - lBody.PY);
                    lBodyPos.Z = lBody.PZ + k * (lBody.Z - lBody.PZ);

                    hBodyPos.X = hBody.PX + k * (hBody.X - hBody.PX);
                    hBodyPos.Y = hBody.PY + k * (hBody.Y - hBody.PY);
                    hBodyPos.Z = hBody.PZ + k * (hBody.Z - hBody.PZ);

                    // Vector representing distance between the two centers at k
                    lBodyPos -= hBodyPos;

                    Double radiSquared = lBody.EphemerisRaduisSquared + hBody.EphemerisRaduisSquared;
                    Double lenSquared = lBodyPos.LengthSquared;

                    if (lenSquared < radiSquared)
                    {
                        // Collision
#if false
                        System.Diagnostics.Debug.WriteLine("CollisionDetector:DetectCollision, ** Collision ** bodies "
                            + lBody.Name + ", " + hBody.Name
                            );
#endif
                        // Two colliding
                        if (!CollisionList.Contains(bL))
                            CollisionList.Add(bL);
                        if (!CollisionList.Contains(bH))
                            CollisionList.Add(bH);

                        lenSquared = 0D; // This is definitely a closest aproach
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
                            // More than 2 bodies at same closest approach
                            if (!closestApproachBodies.Contains(bL))
                                closestApproachBodies.Add(bL);
                            if (!closestApproachBodies.Contains(bH))
                                closestApproachBodies.Add(bH);
                        }
                        approachDistSquared = lenSquared - radiSquared; // Distance between surfaces
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
            int indexOfLargestMassBody = -1;

            // Gather parts of the inelastic collision equation 
            for (int i = 0; i < CollisionList.Count; i++)
            {
                int bodyIndex = CollisionList[i];

                Double tMass;
                sB = SimBodyList.BodyList[bodyIndex];
                tVec.X = sB.VX; tVec.Y = sB.VY; tVec.Z = sB.VZ;
                tMass = CollisionMassLoss * sB.Mass; // Simulate heat loss from collision
                rVec += tMass * tVec;
                massTotal += tMass;

                if (sB.Mass > largestMass)
                {
                    indexOfLargestMassBody = bodyIndex;
                    largestMass = sB.Mass;
                }
            }

            // Resultant velocity vec
            rVec /= massTotal;

            // Build up new body name
            // Mark others in the collision as excluded
            String newBodyName = new(SimBodyList.BodyList[indexOfLargestMassBody].Name);
            for (int i = 0; i < CollisionList.Count; i++)
            {
                int bodyIndex = CollisionList[i];
                sB = SimBodyList.BodyList[bodyIndex];

                if (indexOfLargestMassBody != bodyIndex)
                {
                    newBodyName += " - " + sB.Name;

                    // Broadcast the exclusion
                    SimModel.ExcludeBody(sB.Name);
                }
            }

            // Reset a few things in the remaining body
            sB = SimBodyList.BodyList[indexOfLargestMassBody];
            sB.Name = newBodyName;
            sB.Mass = massTotal;
            sB.VX = rVec.X; sB.VY = rVec.Y; sB.VZ = rVec.Z;

            // Broadcast the rename
            SimModel.BodyRenamed(indexOfLargestMassBody, newBodyName);

            // Barycenter calculations change
            Barycenter?.SystemMassChanged();

            // MassMass table changes
            MassMass?.CalcMassMass(SimBodyList);
        }
    }
}

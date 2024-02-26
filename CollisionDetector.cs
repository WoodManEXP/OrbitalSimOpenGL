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

                    if (distSquared > (lBody.LastPathRadiusSquared + hBody.LastPathRadiusSquared))
                        continue; // No possible collision

                    // Closest approach of the two bodies along their most recent movement vectors.
                    // DeltaS - starting positions, DeltaE - ending positions
                    DeltaS.X = lBody.PX - hBody.PX;
                    DeltaS.Y = lBody.PY - hBody.PY;
                    DeltaS.Z = lBody.PZ - hBody.PZ;
                    DeltaE.X = lBody.X - hBody.X;
                    DeltaE.Y = lBody.Y - hBody.Y;
                    DeltaE.Z = lBody.Z - hBody.Z;
                    Double k = Math.Max(0D, Math.Min(1D, Vector3d.Dot(DeltaS, DeltaS - DeltaE) / (DeltaS - DeltaE).LengthSquared));

                    // If (k < 0 or k > 1) the closet approach is beyond the current travel vectors. This test ignores fact that bodies
                    // have a radius > 0, so bodies could have collided. Super low probability event and will be picked up
                    // on next iteration.
                    if (0D > k || 1D < k)
                        continue; // No collision

                    // Where were the two bodies at K ?
                    lBodyPos.X = lBody.X + k * (lBody.X - lBody.PX);
                    lBodyPos.Y = lBody.Y + k * (lBody.Y - lBody.PY);
                    lBodyPos.Z = lBody.Z + k * (lBody.Z - lBody.PZ);

                    hBodyPos.X = hBody.X + k * (hBody.X - hBody.PX);
                    hBodyPos.Y = hBody.Y + k * (hBody.Y - hBody.PY);
                    hBodyPos.Z = hBody.Z + k * (hBody.Z - hBody.PZ);

                    // Vector representing distance between the two centers at k
                    lBodyPos -= hBodyPos;

                    Double radiSquared = lBody.EphemerisRaduisSquared + hBody.EphemerisRaduisSquared;
                    Double lenSquared = lBodyPos.LengthSquared;

#if false
                    String name1 = lBody.Name;
                    String name2 = hBody.Name;
                    if ((name1.Equals("Sun") || name2.Equals("Sun")) && (name1.Equals("PBH 1") || name2.Equals("PBH 1")))
                    {
                        System.Diagnostics.Debug.WriteLine("CollisionDetector:DetectCollision "
                            + lBody.Name + ", " + hBody.Name
                            + " lBodyPos: " + lBodyPos.X.ToString("N0") + "," + lBodyPos.Y.ToString("N0") + "," + lBodyPos.Z.ToString("N0")
                            + " hBodyPos: " + hBodyPos.X.ToString("N0") + "," + hBodyPos.Y.ToString("N0") + "," + hBodyPos.Z.ToString("N0")
                            + " lenSquared: " + lenSquared.ToString("N0")
                            + " len:" + Math.Sqrt(lenSquared).ToString("N0")
                            + " radiSquared: " + radiSquared.ToString("N0")
                            + " radi: " + Math.Sqrt(radiSquared).ToString("N0")
                            );
                    }
#endif

                    if (lenSquared < radiSquared)
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
                            // Beyond 2 bodies at same closest approach
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

            // MassMass table changes
            MassMass?.CalcMassMass(SimBodyList);
        }
    }
}

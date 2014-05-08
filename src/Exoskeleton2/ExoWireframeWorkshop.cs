using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Grasshopper.Kernel;
using Rhino;
using Rhino.Geometry;

namespace Exoskeleton
{
    public class execute
    {
        public static void Main()
        {

            List<Curve> L = new List<Curve>();

            int S = 0;
            List<double> Rs = new List<double>();
            List<double> Re = new List<double>();
            double D = 0;
            double ND = 0;
            int RP = 0;
            bool O = false;

            double Sides = (double)S;
            double Tol = RhinoDoc.ActiveDoc.ModelAbsoluteTolerance;

            List<ExoNode> ExoNodes = new List<ExoNode>();
            List<ExoStrut> ExoStruts = new List<ExoStrut>();

            //ExoNodes.Add(new ExoNode());
            Rhino.Collections.Point3dList NodeLookup = new Rhino.Collections.Point3dList();
            int IdxL = 0;

            ExoNodes.Add(new ExoNode(L[0].PointAtStart, -L[0].TangentAtStart));
            NodeLookup.Add(ExoNodes[0].Point3d);

            //cycle through each input curve to find unique nodes and assign struts to each node
            foreach (Curve StartL in L)
            {
                //strut as linecurve, and 
                LineCurve StartLC = new LineCurve(StartL.PointAtStart, StartL.PointAtEnd);
                
                //create paired struts for each input curve
                for (int I = 0; I <= 1; I++)
                {

                    Vector3d StrutNormal;
                    Point3d TestPoint;
                    double StrutRadius;
                    bool IsEnd;

                    //set variables based on whether the strut is the start or end strut
                    if (I == 0)
                    {
                        IsEnd = false;
                        TestPoint = StartL.PointAtStart;
                        StrutNormal = -StartL.TangentAtStart;
                        if (Rs.Count - 1 > IdxL) StrutRadius = Rs[IdxL];
                        else StrutRadius = Rs.Last();
                    }
                    else
                    {
                        IsEnd = true;
                        TestPoint = StartL.PointAtEnd;
                        StrutNormal = StartL.TangentAtStart;
                        if (Re.Count - 1 > IdxL) StrutRadius = Re[IdxL];
                        else StrutRadius = Re.Last();
                    }

                    //register new nodes
                    int TestIndex = NodeLookup.ClosestIndex(TestPoint);
                    int NodeIndex = -1;
                    if (ExoNodes[TestIndex].Point3d.DistanceTo(TestPoint) < Tol)
                    {
                        NodeIndex = TestIndex;
                        ExoNodes[TestIndex].StrutIndices.Add(ExoStruts.Count);
                        ExoNodes[TestIndex].NodeNormal += StrutNormal;
                    }
                    else
                    {
                        NodeIndex = ExoNodes.Count;
                        ExoNodes.Add(new ExoNode(TestPoint, StrutNormal));
                        ExoNodes.Last().StrutIndices.Add(ExoStruts.Count);
                    }

                    int LastStrut = ExoNodes[NodeIndex].StrutIndices.Last();
                    //register new struts
                    ExoStruts.Add(new ExoStrut(IsEnd, StrutRadius, StrutRadius / Math.Cos(Math.PI / Sides),
                        StartLC));

                    //test each new strut in a given node against all of the other struts in a given node to calculate both local
                    //and global vertex offsets, both for hulling operation and for relocating vertices post-hull
                    if (ExoNodes[NodeIndex].StrutIndices.Count > 1)
                    {
                        foreach (int StrutIndex in ExoNodes[NodeIndex].StrutIndices)
                        {
                            if (StrutIndex != LastStrut)
                            {
                                double Radius = Math.Max(ExoStruts[LastStrut].HullRadius, ExoStruts[StrutIndex].HullRadius);
                                double Theta = Vector3d.VectorAngle(ExoStruts[LastStrut].Normal, ExoStruts[StrutIndex].Normal);
                                double TestOffset = Radius * Math.Cos(Theta * 0.5) / Math.Sin(Theta * 0.5);
                                if (TestOffset > ExoNodes[NodeIndex].HullOffset) ExoNodes[NodeIndex].HullOffset = TestOffset;
                                if (ExoNodes[NodeIndex].MaxRadius < Radius) ExoNodes[NodeIndex].MaxRadius = Radius;

                                double Offset1 = 0;
                                double Offset2 = 0;

                                ExoTools.OffsetCalculator(Theta, ExoStruts[LastStrut].HullRadius,
                                    ExoStruts[StrutIndex].HullRadius, ref Offset1, ref Offset2);

                                Offset1 = Math.Max(ND, Offset1);
                                Offset2 = Math.Max(ND, Offset1);

                                if (ExoStruts[LastStrut].FixOffset < Offset1) ExoStruts[LastStrut].FixOffset = Offset1;
                                if (ExoStruts[StrutIndex].FixOffset < Offset1) ExoStruts[StrutIndex].FixOffset = Offset2;

                                double KnuckleMinSet = Math.Min(ExoStruts[LastStrut].Radius, ExoStruts[StrutIndex].Radius);
                                if (ExoNodes[NodeIndex].KnuckleMin < Tol || ExoNodes[NodeIndex].KnuckleMin > KnuckleMinSet) ExoNodes[NodeIndex].KnuckleMin = KnuckleMinSet;
                            }
                        }
                    }
                }
                IdxL += 1;
            }

            foreach (ExoNode HullNode in ExoNodes)
            {

                if (HullNode.StrutIndices.Count == 1)
                {
                    HullNode.HullOffset = 0;
                }
                else if (HullNode.StrutIndices.Count == 2)
                {
                    if (Vector3d.VectorAngle(ExoStruts[HullNode.StrutIndices[0]].Normal, ExoStruts[HullNode.StrutIndices[1]].Normal) - Math.PI < RhinoDoc.ActiveDoc.ModelAbsoluteTolerance)
                    { HullNode.HullOffset = HullNode.MaxRadius * 0.5; }
                }

                for (int HV = 0; HV < S; HV++)
                {
                    foreach (int StrutIdx in HullNode.StrutIndices)
                    {
                        if (HV == 0)
                        {
                            //set strut hulling plane
                            ExoStruts[StrutIdx].HullPlane.Origin = ExoStruts[StrutIdx].HullPlane.Origin + (ExoStruts[StrutIdx].Normal * HullNode.HullOffset);
                            //set strut knuckle
                            if (HullNode.StrutIndices.Count == 1) { HullNode.HullOffset = ND; }
                            HullNode.HullPoints.Add(HullNode.Point3d + (-ExoStruts[StrutIdx].Normal * (HullNode.HullOffset * 0.5)));
                            HullNode.HullVertexStrut.Add(-1);
                        }
                        HullNode.HullPoints.Add(ExoStruts[StrutIdx].HullPlane.PointAt(Math.Cos((HV / Sides) * Math.PI * 2) * ExoStruts[StrutIdx].Radius, Math.Sin((HV / Sides) * Math.PI * 2) * ExoStruts[StrutIdx].Radius));
                        HullNode.HullVertexStrut.Add(StrutIdx);
                    }
                }

                HullNode.HullVertexLookup.AddRange(HullNode.HullPoints);

            }

        }
    }

    public class ExoNode
    {

        public Point3d Point3d;
        public List<int> StrutIndices = new List<int>();
        public Vector3d NodeNormal;
        public double HullOffset = 0;
        public double MaxRadius = 0;
        public double KnuckleMin = 0.0;
        public List<Point3d> HullPoints = new List<Point3d>();
        public Rhino.Collections.Point3dList HullVertexLookup = new Rhino.Collections.Point3dList();
        public List<int> HullVertexStrut = new List<int>();

        public ExoNode(Point3d SetPoint3d, Vector3d SetNormal)
        {
            Point3d = SetPoint3d;
            NodeNormal = SetNormal;
        }
    }

    public class ExoStrut
    {

        public double Radius;
        public double HullRadius;
        public LineCurve Strut;
        public Plane HullPlane;
        public Vector3d Normal;
        public double FixOffset = 0;
        public Plane FixPlane;
        public bool StrutSolo = false;


        public ExoStrut(bool IsEnd, double SetRadius, double SetHullRadius, LineCurve SetStrut)
        {
            Radius = SetRadius;
            HullRadius = SetHullRadius;
            Strut = SetStrut;
            Strut.PerpendicularFrameAt(0.0, out HullPlane);
            Normal = HullPlane.ZAxis;
            if (IsEnd)
            {
                Strut.Reverse();
                HullPlane.Origin = Strut.PointAtStart;
                Normal *= -1;
            }
            FixPlane = HullPlane;
        }

    }
}
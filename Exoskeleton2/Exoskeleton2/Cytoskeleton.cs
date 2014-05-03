using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Grasshopper.Kernel;
using Rhino;
using Rhino.Geometry;
using Plankton;
using PlanktonGh;

namespace Cytoskeleton
{
    public class Cytoskeleton : GH_Component
    {
        public Cytoskeleton() : base("Cytoskeleton", "Cyto", "Thicken the edges of a mesh", "Mesh", "ExoTest") { }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddParameter(new PlanktonGh.GH_PlanktonMeshParam(), "PlanktonMesh", "PMesh", "The input PlanktonMesh to thicken the edges of", GH_ParamAccess.item);           
            pManager.AddNumberParameter("Radius", "R", "Strut thickness. Either one value to be applied across the whole mesh, or a list of values per vertex of the Plankton mesh(note - these are not ordered the same as the vertices of the grasshopper mesh!)", GH_ParamAccess.list, 0.2);
            pManager.AddBooleanParameter("Dual", "D", "If true, the edges of the dual will be thickened (NOT WORKING YET FOR OPEN MESHES!)", GH_ParamAccess.item, false);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddMeshParameter("Mesh", "M", "Thickened mesh wireframe", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            PlanktonMesh P1 = null;
            if (!DA.GetData(0, ref P1)) return;
            List<double> RL = new List<double>() ;
            if (!DA.GetDataList(1, RL)) { return; }
            bool D = false;
            if (!DA.GetData(2, ref D)) { return; }            

            if (D)
            {
                P1 = P1.Dual();
            }
            PlanktonMesh P2 = new PlanktonMesh();
            int vcount = P1.Vertices.Count;
            List<Vector3d> Normals = new List<Vector3d>();
            List<int> Outer = new List<int>();
            List<int> Inner = new List<int>();
            List<int> Elbow = new List<int>();

            for (int i = 0; i < vcount; i++)
            {
                Point3d Vertex = P1.Vertices[i].ToPoint3d();
                Vector3d Normal = new Vector3d();
                double AvgAngle = 0;

                double R = 0;
                if (RL.Count == 1)
                { R = RL[0]; }
                else
                { R = RL[i]; }
           
                int[] OutEdges = P1.Vertices.GetHalfedges(i);
                int[] Neighbours = P1.Vertices.GetVertexNeighbours(i);
                Vector3d[] OutVectors = new Vector3d[Neighbours.Length];
                int Valence = P1.Vertices.GetValence(i);

                for (int j = 0; j < Valence; j++)
                {
                    OutVectors[j] = P1.Vertices[Neighbours[j]].ToPoint3d() - Vertex;
                }

                for (int j = 0; j < Valence; j++)
                {
                    if (P1.Halfedges[OutEdges[(j + 1) % Valence]].AdjacentFace != -1)
                    {
                        Normal += (Vector3d.CrossProduct(OutVectors[(j + 1) % Valence], OutVectors[j]));
                    }
                }

                Normal.Unitize();
                Normals.Add(Normal);

                for (int j = 0; j < Valence; j++)
                {
                    AvgAngle += Vector3d.VectorAngle(Normal, OutVectors[j]);
                }
                AvgAngle = AvgAngle * (1.0 / Valence);

                double Offset = R / (Math.Sin(AvgAngle));

                Outer.Add(P2.Vertices.Add(Vertex + (Normal * Offset))); //this adds the actual point to the mesh, as well as its index to Outer
                Inner.Add(P2.Vertices.Add(Vertex - (Normal * Offset)));
            }

            for (int i = 0; i < P1.Halfedges.Count; i++)
            {
                //get the 3 points of the angle
                int Prev = P1.Halfedges[i].PrevHalfedge;
                int Next = P1.Halfedges[i].NextHalfedge;
                int PrevV = P1.Halfedges[Prev].StartVertex;
                int NextV = P1.Halfedges[Next].StartVertex;
                int ThisV = P1.Halfedges[i].StartVertex;

                double R = 0;
                if (RL.Count == 1)
                { R = RL[0]; }
                else
                { R = RL[ThisV]; }

                Point3d PrevPt = P1.Vertices[PrevV].ToPoint3d();
                Point3d NextPt = P1.Vertices[NextV].ToPoint3d();
                Point3d ThisPt = P1.Vertices[ThisV].ToPoint3d();
                //construct the point at the inside of the 'elbow'
                Vector3d Arm1 = PrevPt - ThisPt;
                Vector3d Arm2 = NextPt - ThisPt;
                Arm1.Unitize(); Arm2.Unitize();
                double alpha = Vector3d.VectorAngle(Arm1, Arm2);
                Point3d ThisElbow;
                Vector3d Bisect = Vector3d.CrossProduct(Normals[ThisV], -1.0 * Arm1) +
                  Vector3d.CrossProduct(Normals[ThisV], Arm2);
                Bisect.Unitize();
                ThisElbow = ThisPt + Bisect * (R / Math.Sin(alpha * 0.5));
                Elbow.Add(P2.Vertices.Add(ThisElbow));
            }

            for (int i = 0; i < P1.Halfedges.Count; i++)
            {
                int Next = P1.Halfedges[i].NextHalfedge;
                int NextV = P1.Halfedges[Next].StartVertex;
                int ThisV = P1.Halfedges[i].StartVertex;
                P2.Faces.AddFace(Outer[ThisV], Outer[NextV], Elbow[Next], Elbow[i]);
                P2.Faces.AddFace(Elbow[i], Elbow[Next], Inner[NextV], Inner[ThisV]);
            }

            Mesh OutputMesh = P2.ToRhinoMesh();
            DA.SetData(0, OutputMesh);

        }

        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return Exoskeleton.Properties.Resources.cyto;
            }
        }

        public override Guid ComponentGuid
        {
            get { return new Guid("3ad90f07-c1c9-4c7e-bb59-8f4343cfa0e6"); }
        }

    }
}

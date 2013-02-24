﻿using System;
using System.IO;
using BrawlLib.SSBB.ResourceNodes;
using BrawlLib.OpenGL;
using System.Windows.Forms;
using BrawlLib.Wii.Models;

namespace BrawlLib.Modeling
{
    public static class Wavefront
    {
        public static void Serialize(string outPath, params object[] assets)
        {
            using (FileStream stream = new FileStream(outPath, FileMode.Create, FileAccess.ReadWrite, FileShare.None))
            using (StreamWriter writer = new StreamWriter(stream))
            {
                writer.WriteLine("#Wavefront OBJ file, generated by BrawlBox");
                //writer.WriteLine();

                foreach (object o in assets)
                    if (o is MDL0VertexNode)
                        WriteVertexGroup(writer, (o as MDL0VertexNode).Vertices);
                    else if (o is MDL0NormalNode)
                        WriteNormalGroup(writer, (o as MDL0NormalNode));
                    else if (o is MDL0UVNode)
                        WriteUVGroup(writer, (o as MDL0UVNode));
                    else if (o is MDL0ObjectNode)
                        WritePolygon(writer, o as MDL0ObjectNode);

                writer.Flush();
            }
        }

        private static void WriteVertexGroup(StreamWriter writer, Vector3[] verts)
        {
            writer.WriteLine();
            writer.WriteLine("#Vertices");
            foreach (Vector3 v in verts)
                writer.WriteLine("v {0} {1} {2}", v._x, v._y, v._z);
        }
        private unsafe static void WriteVertexGroup(StreamWriter writer, UnsafeBuffer verts)
        {
            writer.WriteLine();
            writer.WriteLine("#Vertices");
            Vector3* pVert = (Vector3*)verts.Address;
            for (int i = 0; i < verts.Length / 12; i++)
            {
                writer.WriteLine("v {0} {1} {2}", pVert->_x, pVert->_y, pVert->_z);
                pVert++;
            }
        }
        private static void WriteNormalGroup(StreamWriter writer, MDL0NormalNode norm)
        {
            writer.WriteLine();
            writer.WriteLine("#Normals");
            foreach (Vector3 v in norm.Normals)
                writer.WriteLine("vn {0} {1} {2}", v._x, v._y, v._z);
        }
        private unsafe static void WriteNormalGroup(StreamWriter writer, UnsafeBuffer norms)
        {
            writer.WriteLine();
            writer.WriteLine("#Normals");
            Vector3* pVert = (Vector3*)norms.Address;
            for (int i = 0; i < norms.Length / 12; i++)
            {
                writer.WriteLine("vn {0} {1} {2}", pVert->_x, pVert->_y, pVert->_z);
                pVert++;
            }
        }
        private unsafe static void WriteNormalGroup(StreamWriter writer, PrimitiveManager p, bool weight)
        {
            ushort* pIndex = (ushort*)p._indices.Address;
            Vector3 v;

            writer.WriteLine();
            writer.WriteLine("#Normals");
            Vector3* pVert = (Vector3*)p._faceData[1].Address;
            for (int i = 0; i < p._faceData[1].Length / 12; i++)
            {
                //writer.WriteLine("vn {0} {1} {2}", pVert->_x, pVert->_y, (pVert++)->_z);
                if (weight && p._vertices[*pIndex]._matrixNode != null)
                    v = p._vertices[*pIndex++]._matrixNode.Matrix.GetRotationMatrix() * *pVert++;
                else
                    v = *pVert++;
                writer.WriteLine("vn {0} {1} {2}", v._x, v._y, v._z);
            }
        }
        private static void WriteUVGroup(StreamWriter writer, MDL0UVNode uv)
        {
            writer.WriteLine();
            writer.WriteLine("#UVs");
            foreach (Vector2 v in uv.Points)
                writer.WriteLine("vt {0} {1}", v._x, v._y);
        }
        private unsafe static void WriteUVGroup(StreamWriter writer, UnsafeBuffer uvs)
        {
            writer.WriteLine();
            writer.WriteLine("#UVs");
            Vector2* pVert = (Vector2*)uvs.Address;
            for (int i = 0; i < uvs.Length / 8; i++)
            {
                //Reverse T component to a top-down form
                writer.WriteLine("vt {0} {1}", pVert->_x, 1.0 - pVert->_y);
                pVert++;
            }
        }
        private static void WriteMaterial(StreamWriter writer, MDL0MaterialNode mat)
        {
            writer.WriteLine(String.Format("usemtl {0}", mat.Name));
        }
        private static void WritePolygon(StreamWriter writer, MDL0ObjectNode poly)
        {
            if (poly._manager._vertices != null)
            {
                int count = poly._manager._vertices.Count;
                Vector3[] Vertices = new Vector3[count];
                DialogResult result = MessageBox.Show("Do you want to export the weighted positions of the vertices?", "", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question, MessageBoxDefaultButton.Button1);
                if (result == DialogResult.Yes)
                {
                    //Weight vertices
                    foreach (Influence inf in (poly.Model)._influences._influences)
                        inf.CalcMatrix();
                    poly._manager.Weight();

                    //Set weighted positions
                    for (int i = 0; i < count; i++)
                        if (poly._manager._vertices[i].WeightedPosition.ToString() != "(0,0,0)")
                            Vertices[i] = poly._manager._vertices[i].WeightedPosition;
                        else
                            Vertices[i] = poly._manager._vertices[i].Position;
                }
                else if (result == DialogResult.No)
                {
                    //Set raw positions
                    for (int i = 0; i < count; i++)
                        Vertices[i] = poly._manager._vertices[i].Position;
                }
                if (result != DialogResult.Cancel) //Export
                    WriteVertexGroup(writer, Vertices);
            }
            if (poly._manager._faceData[1] != null)
                WriteNormalGroup(writer, poly._manager, true);

            for (int i = 0; i < 1; i++) //Obj only supports 1 uv coord set...
                if (poly._manager._faceData[i + 4] != null)
                    WriteUVGroup(writer, poly._manager._faceData[i + 4]);

            writer.WriteLine();
            writer.WriteLine(String.Format("g {0}", poly.Name));
            //if (poly._material != null)
            //    WriteMaterial(writer, poly._material);
            //if (poly.Primitives != null)
            //    foreach (Primitive p in poly.Primitives)
            //    {
            //        switch (p._type)
            //        {
            //            case GLPrimitiveType.TriangleFan:
            //                WriteTriFan(writer, p);
            //                break;
            //            case GLPrimitiveType.TriangleStrip:
            //                WriteTriStrip(writer, p);
            //                break;
            //            case GLPrimitiveType.Triangles:
            //                WriteTriList(writer, p);
            //                break;
            //            case GLPrimitiveType.Quads:
            //                WriteQuadList(writer, p);
            //                break;
            //        }
            //    }
            if (poly._manager != null)
            {
                if (poly._manager._triangles != null)
                    WriteTriList(writer, poly._manager);
                //if (poly._manager._lines != null)
                //{

                //}
                //if (poly._manager._points != null)
                //{

                //}
            }
        }

        //private static void WriteTriFan(StreamWriter writer, Primitive p)
        //{
        //    if ((p._vertexIndices == null) || (p._normalIndices == null))
        //        return;

        //    writer.WriteLine();
        //    writer.WriteLine("#Trifan");
        //    int count = p._elementCount - 2;
        //    if (p._uvIndices[0] != null)
        //        for (int i = 0; i < count; i++)
        //            writer.WriteLine(String.Format("f {0}/{1}/{2} {3}/{4}/{5} {6}/{7}/{8}",
        //                p._vertexIndices[0] + 1, p._uvIndices[0][0] + 1, p._normalIndices[0] + 1,
        //                p._vertexIndices[i + 1] + 1, p._uvIndices[0][i + 1] + 1, p._normalIndices[i + 1] + 1,
        //                p._vertexIndices[i + 2] + 1, p._uvIndices[0][i + 2] + 1, p._normalIndices[i + 2] + 1));
        //    else
        //        for (int i = 0; i < count; i++)
        //            writer.WriteLine(String.Format("f {0}//{1} {2}//{3} {4}//{5}",
        //                    p._vertexIndices[0] + 1, p._normalIndices[0] + 1,
        //                    p._vertexIndices[i + 1] + 1, p._normalIndices[i + 1] + 1,
        //                    p._vertexIndices[i + 2] + 1, p._normalIndices[i + 2] + 1));
        //}
        //private static void WriteTriStrip(StreamWriter writer, Primitive p)
        //{
        //    if ((p._vertexIndices == null) || (p._normalIndices == null))
        //        return;

        //    writer.WriteLine();
        //    writer.WriteLine("#Tristrip");
        //    int count = p._elementCount - 2;
        //    int l1 = 0, l2 = 2;
        //    if (p._uvIndices[0] != null)
        //        for (int i = 0; i < count; i++)
        //        {
        //            if ((i & 1) == 0)
        //            {
        //                l1 = i;
        //                l2 = i + 1;
        //            }
        //            else
        //            {
        //                l1 = i + 1;
        //                l2 = i;
        //            }
        //            writer.WriteLine(String.Format("f {0}/{1}/{2} {3}/{4}/{5} {6}/{7}/{8}",
        //                p._vertexIndices[l1] + 1, p._uvIndices[0][l1] + 1, p._normalIndices[l1] + 1,
        //                p._vertexIndices[l2] + 1, p._uvIndices[0][l2] + 1, p._normalIndices[l2] + 1,
        //                p._vertexIndices[i + 2] + 1, p._uvIndices[0][i + 2] + 1, p._normalIndices[i + 2] + 1));
        //        }
        //    else
        //        for (int i = 0; i < count; i++)
        //        {
        //            if ((i & 1) == 0)
        //            {
        //                l1 = i;
        //                l2 = i + 1;
        //            }
        //            else
        //            {
        //                l1 = i + 1;
        //                l2 = i;
        //            }
        //            writer.WriteLine(String.Format("f {0}//{1} {2}//{3} {4}//{5}",
        //                    p._vertexIndices[l1] + 1, p._normalIndices[l1] + 1,
        //                    p._vertexIndices[l2] + 1, p._normalIndices[l2] + 1,
        //                    p._vertexIndices[i + 2] + 1, p._normalIndices[i + 2] + 1));
        //        }
        //}
        //private static void WriteTriList(StreamWriter writer, Primitive p)
        //{
        //    if ((p._vertexIndices == null) || (p._normalIndices == null))
        //        return;

        //    writer.WriteLine();
        //    writer.WriteLine("#Trilist");
        //    int count = p._elementCount / 3;
        //    if (p._uvIndices[0] != null)
        //        for (int i = 0, x = 0; i < count; i++, x += 3)
        //            writer.WriteLine(String.Format("f {0}/{1}/{2} {3}/{4}/{5} {6}/{7}/{8}",
        //                p._vertexIndices[x] + 1, p._uvIndices[0][x] + 1, p._normalIndices[x] + 1,
        //                p._vertexIndices[x + 1] + 1, p._uvIndices[0][x + 1] + 1, p._normalIndices[x + 1] + 1,
        //                p._vertexIndices[x + 2] + 1, p._uvIndices[0][x + 2] + 1, p._normalIndices[x + 2] + 1));
        //    else
        //        for (int i = 0, x = 0; i < count; i++, x += 3)
        //            writer.WriteLine(String.Format("f {0}//{1} {2}//{3} {4}//{5}",
        //                p._vertexIndices[x] + 1, p._normalIndices[x] + 1,
        //                p._vertexIndices[x + 1] + 1, p._normalIndices[x + 1] + 1,
        //                p._vertexIndices[x + 2] + 1, p._normalIndices[x + 2] + 1));
        //}
        private unsafe static void WriteTriList(StreamWriter writer, PrimitiveManager p)
        {
            ushort* pData = (ushort*)p._triangles._indices.Address;
            ushort* pVert = (ushort*)p._indices.Address;

            bool hasUVs = p._faceData[4] != null;
            bool hasNorms = p._faceData[1] != null;
            
            writer.WriteLine();
            writer.WriteLine("#Trilist");
            
            int count = p._triangles._elementCount / 3;

            //Loop through triangles
            for (int tri = 0; tri < count; tri++)
            {
                //writer.WriteLine("s " + i); //Smoothing groups don't seem to do anything
                writer.Write("f "); //Face

                //Loop through triangle points
                for (int pt = 0; pt < 3; pt++)
                {
                    int index = *pData++;

                    //Loop through vertices, uvs, normals
                    for (int asset = 0; asset < 3; asset++)
                    {
                        if (asset != 0) //Not a point start
                            writer.Write("/"); //Asset divider

                        //If no UVs, there's a double slash
                        if (!hasUVs && asset == 1)
                            continue;

                        if (asset == 0) //Vertex index
                            writer.Write((pVert[index] + 1).ToString());
                        else
                            writer.Write((index + 1).ToString());

                        if (asset == 2) //Point is done
                            if (pt == 2) //Last point
                                writer.WriteLine();
                            else //Still more points to go
                                writer.Write(" ");
                    }
                }
            }
        }
        //private static void WriteQuadList(StreamWriter writer, Primitive p)
        //{
        //    if ((p._vertexIndices == null) || (p._normalIndices == null))
        //        return;

        //    writer.WriteLine();
        //    writer.WriteLine("#Quadlist");
        //    int count = p._elementCount / 4;
        //    if (p._uvIndices[0] != null)
        //        for (int i = 0, x = 0; i < count; i++, x += 4)
        //            writer.WriteLine(String.Format("f {0}/{1}/{2} {3}/{4}/{5} {6}/{7}/{8} {9}/{10}/{11}",
        //                p._vertexIndices[x] + 1, p._uvIndices[0][x] + 1, p._normalIndices[x] + 1,
        //                p._vertexIndices[x + 1] + 1, p._uvIndices[0][x + 1] + 1, p._normalIndices[x + 1] + 1,
        //                p._vertexIndices[x + 2] + 1, p._uvIndices[0][x + 2] + 1, p._normalIndices[x + 2] + 1,
        //                p._vertexIndices[x + 3] + 1, p._uvIndices[0][x + 3] + 1, p._normalIndices[x + 3] + 1));
        //    else
        //        for (int i = 0, x = 0; i < count; i++, x += 4)
        //            writer.WriteLine(String.Format("f {0}//{1} {2}//{3} {4}//{5} {6}//{7}",
        //                p._vertexIndices[x] + 1, p._normalIndices[x] + 1,
        //                p._vertexIndices[x + 1] + 1, p._normalIndices[x + 1] + 1,
        //                p._vertexIndices[x + 2] + 1, p._normalIndices[x + 2] + 1,
        //                p._vertexIndices[x + 3] + 1, p._normalIndices[x + 3] + 1));
        //}
    }
}

﻿// Copyright (c) 2019-2020 Faber Leonardo. All Rights Reserved. https://github.com/FaberSanZ
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)


/*=============================================================================
	GLTFLoader.cs
=============================================================================*/


using glTFLoader;
using glTFLoader.Schema;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Zeckoxe.Graphics;
using Schema = glTFLoader.Schema;


namespace Zeckoxe.GLTF
{
    public enum DescriptorBindingFlags
    {
        ImageBaseColor = 0x00000001,
        ImageNormalMap = 0x00000002
    }


    public enum VertexComponent
    {
        Position,
        Normal,
        UV,
        Color,
        Tangent,
        Joint0,
        Weight0
    }


    public enum FileLoadingFlags
    {
        None = 0x00000000,
        PreTransformVertices = 0x00000001,
        PreMultiplyVertexColors = 0x00000002,
        FlipY = 0x00000004,
        DontLoadImages = 0x00000008
    };

    public enum RenderFlags
    {
        BindImages = 0x00000001,
        RenderOpaqueNodes = 0x00000002,
        RenderAlphaMaskedNodes = 0x00000004,
        RenderAlphaBlendedNodes = 0x00000008
    };


    public unsafe class GLTFLoader<TVertex>
    {
        private struct Vertex
        {
            private Vector3 pos;
            private Vector3 normal;
            private Vector2 uv;
            private Vector4 color;
            private Vector4 joint0;
            private Vector4 weight0;
            private Vector4 tangent;
        };


        internal Gltf gltf;
        internal string baseDirectory;

        internal byte[][] loadedBuffers;
        internal GCHandle[] bufferHandles;


        internal Device _device;


        internal List<Node> nodes;
        internal List<Node> linearNodes;

        internal bool buffersBound = false;
        List<VertexPositionColor> vertexBuffer = new();

        List<int> indexBuffe = new();



        public GLTFLoader(Device device, string path)
        {
            _device = device;
            _path = path;

            baseDirectory = Path.GetDirectoryName(path);
            gltf = Interface.LoadModel(path);
            loadedBuffers = new byte[gltf.Buffers.Length][];
            bufferHandles = new GCHandle[gltf.Buffers.Length];

            GetVertexCount();

            //create_buffers();





            Meshes = new();
            foreach (Schema.Mesh mesh in gltf.Meshes)
            {
                Meshes.Add(loadNode(mesh, vertexBuffer, indexBuffe));
            }


            Scenes = LoadScenes();


            //for (int i = 0; i < vertexBuffer.Count; i++)
            //{
            //    VertexPositionColor vertex = vertexBuffer[i];


            //    vertex.Position.Y *= -1.0f;
            //    vertex.Color.Y *= -1.0f;

            //    vertexBuffer[i] = vertex;
            //}


            //foreach (var sc in Scenes)
            //{
            //    foreach (Node node in sc.Root.Children)
            //    {
            //        //PreTransform(node, sc.Root.LocalMatrix);
            //    }
            //}




            VertexBuffer = new(_device, new()
            {
                BufferFlags = BufferFlags.VertexBuffer,
                Usage = GraphicsResourceUsage.Dynamic,
                SizeInBytes = vertexBuffer.Count * Marshal.SizeOf<TVertex>(),
            });


            IndexBuffer = new(_device, new()
            {
                BufferFlags = BufferFlags.IndexBuffer,
                Usage = GraphicsResourceUsage.Dynamic,
                SizeInBytes = indexBuffe.Count * Marshal.SizeOf<int>(),
            });
            VertexBuffer.SetData<VertexPositionColor>(vertexBuffer.ToArray());
            IndexBuffer.SetData<int>(indexBuffe.ToArray());

            //Meshes = LoadMeshes();


            Console.WriteLine(IndexType);
        }

        private readonly int defaultSceneIndex = -1;
        private readonly List<Scene> Scenes;

        public TextureData TextureData { get; set; }

        public Graphics.Buffer VertexBuffer { get; private set; }
        public int VertexCount { get; private set; }

        public Graphics.Buffer IndexBuffer { get; private set; }
        public int IndexCount { get; private set; }

        public List<Mesh> Meshes { get; set; }

        public IndexType IndexType { get; set; }




        internal void create_buffers()
        {
            VertexBuffer = new(_device, new()
            {
                BufferFlags = BufferFlags.VertexBuffer,
                Usage = GraphicsResourceUsage.Dynamic,
                SizeInBytes = VertexCount * Marshal.SizeOf<TVertex>(),
            });


            IndexBuffer = new(_device, new()
            {
                BufferFlags = BufferFlags.IndexBuffer,
                Usage = GraphicsResourceUsage.Dynamic,
                SizeInBytes = IndexCount * (IndexType is IndexType.Uint16 ? 2 : 4),
            });
        }




        internal byte[] load_data_uri(Schema.Image img)
        {
            int idxComa = img.Uri.IndexOf(",", 5, StringComparison.Ordinal);
            return Convert.FromBase64String(img.Uri.Substring(idxComa + 1));
        }

        internal byte[] load_data_uri(Schema.Buffer buff)
        {
            int idxComa = buff.Uri.IndexOf(",", 5, StringComparison.Ordinal);
            return Convert.FromBase64String(buff.Uri.Substring(idxComa + 1));
        }


        internal void GetVertexCount()
        {
            VertexCount = 0;
            IndexCount = 0;
            IndexType = IndexType.Uint16;
            //compute size of stagging buf
            foreach (Schema.Mesh mesh in gltf.Meshes)
            {
                foreach (Schema.MeshPrimitive p in mesh.Primitives)
                {
                    if (p.Attributes.TryGetValue("POSITION", out int accessorIdx))
                    {
                        VertexCount += gltf.Accessors[accessorIdx].Count;
                    }

                    if (p.Indices != null)
                    {
                        IndexCount += gltf.Accessors[(int)p.Indices].Count;
                        if (gltf.Accessors[(int)p.Indices].ComponentType == Accessor.ComponentTypeEnum.UNSIGNED_INT)
                        {
                            IndexType = IndexType.Uint32;
                        }
                    }
                }
            }
        }

        public uint ImageCount => gltf.Images == null ? 0 : (uint)gltf.Images.Length;

        private readonly string _path;

        private void ensure_buffer_is_loaded(int bufferIdx)
        {
            if (loadedBuffers[bufferIdx] == null)
            {
                //load full buffer
                string uri = gltf.Buffers[bufferIdx].Uri;
                if (string.IsNullOrEmpty(uri))//glb
                {
                    loadedBuffers[bufferIdx] = gltf.LoadBinaryBuffer(bufferIdx, _path);
                }
                else if (uri.StartsWith("data", StringComparison.Ordinal))
                {
                    loadedBuffers[bufferIdx] = load_data_uri(gltf.Buffers[bufferIdx]); //TODO: check this func=>System.Buffers.Text.Base64.EncodeToUtf8InPlace
                }
                else
                {
                    loadedBuffers[bufferIdx] = File.ReadAllBytes(Path.Combine(baseDirectory, gltf.Buffers[bufferIdx].Uri));
                }

                bufferHandles[bufferIdx] = GCHandle.Alloc(loadedBuffers[bufferIdx], GCHandleType.Pinned);
            }
        }

        private unsafe void AttributeCopyData<T>(ref T[] array, int attributeByteLength, void* attributePointer) where T : unmanaged
        {
            array = new T[attributeByteLength / Unsafe.SizeOf<T>()];
            fixed (T* arrayPtr = &array[0])
            {
                Unsafe.CopyBlock(arrayPtr, attributePointer, (uint)attributeByteLength);
            }
        }

        internal Mesh loadNode(Schema.Mesh mesh, List<VertexPositionColor> vertexBuffer, List<int> indexBuffer)
        {
            Mesh m = new Mesh { Name = mesh.Name };

            foreach (Schema.MeshPrimitive p in mesh.Primitives)
            {
                ulong indexStart = (ulong)indexBuffer.Count;
                ulong vertexStart = (ulong)vertexBuffer.Count;
                int indexCount = 0;
                int vertexCount = 0;

                Schema.Accessor AccPos = null, AccNorm = null, AccUv = null, AccUv1 = null;

                if (p.Attributes.TryGetValue("POSITION", out int accessorIdx))
                {
                    AccPos = gltf.Accessors[accessorIdx];
                    ensure_buffer_is_loaded(gltf.BufferViews[(int)AccPos.BufferView].Buffer);
                }
                if (p.Attributes.TryGetValue("NORMAL", out accessorIdx))
                {
                    AccNorm = gltf.Accessors[accessorIdx];
                    ensure_buffer_is_loaded(gltf.BufferViews[(int)AccNorm.BufferView].Buffer);
                }
                if (p.Attributes.TryGetValue("TEXCOORD_0", out accessorIdx))
                {
                    AccUv = gltf.Accessors[accessorIdx];
                    ensure_buffer_is_loaded(gltf.BufferViews[(int)AccUv.BufferView].Buffer);
                }
                if (p.Attributes.TryGetValue("TEXCOORD_1", out accessorIdx))
                {
                    AccUv1 = gltf.Accessors[accessorIdx];
                    ensure_buffer_is_loaded(gltf.BufferViews[(int)AccUv1.BufferView].Buffer);
                }



                //prim.BoundingBox.Min.ImportFloatArray(AccPos.Min);
                //prim.BoundingBox.Max.ImportFloatArray(AccPos.Max);
                //prim.BoundingBox.IsValid = true;

                //Interleaving vertices
                byte* inPosPtr = null, inNormPtr = null, inUvPtr = null, inUv1Ptr = null;

                Vector3[] positions = new Vector3[0];
                Vector3[] normals = new Vector3[0];
                Vector2[] texcoords = new Vector2[0];
                Vector3[] tangents = new Vector3[0];

                Schema.BufferView bv = gltf.BufferViews[(int)AccPos.BufferView];
                inPosPtr = (byte*)bufferHandles[bv.Buffer].AddrOfPinnedObject().ToPointer();
                inPosPtr += AccPos.ByteOffset + bv.ByteOffset;
                this.AttributeCopyData(ref positions, AccPos.Count * Unsafe.SizeOf<Vector3>(), inPosPtr);


                if (AccNorm is not null)
                {
                    bv = gltf.BufferViews[(int)AccNorm.BufferView];
                    inNormPtr = (byte*)bufferHandles[bv.Buffer].AddrOfPinnedObject().ToPointer();
                    inNormPtr += AccNorm.ByteOffset + bv.ByteOffset;

                    AttributeCopyData(ref normals, AccPos.Count * Unsafe.SizeOf<Vector3>(), inNormPtr);
                }


                vertexCount = AccPos.Count;


                //TODO: use vertex attributes scan for copying data if they exists
                for (int i = 0; i < AccPos.Count; i++)
                {
                    Vector3 position = positions[i];
                    Vector3 normal = (normals.Length > i) ? normals[i] : Vector3.Zero;
                    vertexBuffer.Add(new(position, normal));
                }

                //indices loading
                if (p.Indices is not null)
                {
                    Schema.Accessor acc = gltf.Accessors[(int)p.Indices];
                    bv = gltf.BufferViews[(int)acc.BufferView];


                    indexCount = acc.Count;

                    byte* inIdxPtr = (byte*)bufferHandles[bv.Buffer].AddrOfPinnedObject().ToPointer();
                    inIdxPtr += acc.ByteOffset + bv.ByteOffset;

                    //TODO:double check this, I dont seems to increment stag pointer
                    if (acc.ComponentType is Schema.Accessor.ComponentTypeEnum.UNSIGNED_SHORT)
                    {
                        if (IndexType is IndexType.Uint16)
                        {
                            ushort* buf = stackalloc ushort[acc.Count];

                            Unsafe.CopyBlock(buf, inIdxPtr, (uint)acc.Count * 2);


                            for (int index = 0; index < acc.Count; index++)
                            {
                                indexBuffer.Add(buf[index] + (int)vertexStart);
                            }

                        }
                        else
                        {
                            ushort* buf = stackalloc ushort[acc.Count];

                            Unsafe.CopyBlock(buf, inIdxPtr, (uint)acc.Count * 4);


                            for (int index = 0; index < acc.Count; index++)
                            {
                                indexBuffer.Add(buf[index] + (int)vertexStart);
                            }
                        }
                    }
                    else if (acc.ComponentType is Schema.Accessor.ComponentTypeEnum.UNSIGNED_INT)
                    {
                        if (IndexType is IndexType.Uint16)
                        {
                            int* buf = stackalloc int[acc.Count];

                            Unsafe.CopyBlock(buf, inIdxPtr, (uint)acc.Count * 2);


                            for (int index = 0; index < acc.Count; index++)
                            {
                                indexBuffer.Add(buf[index] + (int)vertexStart);
                            }

                        }
                        else
                        {
                            int* buf = stackalloc int[acc.Count];

                            Unsafe.CopyBlock(buf, inIdxPtr, (uint)acc.Count * 4);


                            for (int index = 0; index < acc.Count; index++)
                            {
                                indexBuffer.Add(buf[index] + (int)vertexStart);
                            }
                        }
                    }
                    else if (acc.ComponentType is Schema.Accessor.ComponentTypeEnum.UNSIGNED_BYTE)
                    {
                        //convert
                        if (IndexType is IndexType.Uint16)
                        {
                            byte* buf = stackalloc byte[acc.Count];

                            Unsafe.CopyBlock(buf, inIdxPtr, (uint)acc.Count * 2);


                            for (int index = 0; index < acc.Count; index++)
                            {
                                indexBuffer.Add(buf[index] + (int)vertexStart);
                            }

                        }
                        else
                        {
                            byte* buf = stackalloc byte[acc.Count];

                            Unsafe.CopyBlock(buf, inIdxPtr, (uint)acc.Count * 4);


                            for (int index = 0; index < acc.Count; index++)
                            {
                                indexBuffer.Add(buf[index] + (int)vertexStart);
                            }
                        }
                    }
                    else
                    {
                        throw new NotImplementedException();
                    }

                }


                Primitive prim = new Primitive
                {
                    FirstIndex = (int)indexStart,
                    FirstVertex = (int)vertexStart,
                    VertexCount = vertexCount,
                    IndexCount = indexCount,
                };

                m.AddPrimitive(prim);

            }


            return m;
        }


        public List<Mesh> LoadMeshes()
        {

            int vertexByteSize = Marshal.SizeOf<TVertex>();
            ulong vertSize = (ulong)(VertexCount * vertexByteSize);
            ulong idxSize = (ulong)IndexCount * (IndexType is IndexType.Uint16 ? 2ul : 4ul);
            ulong size = vertSize + idxSize;

            //int vertexCount = 0, indexCount = 0;
            int autoNamedMesh = 1;


            VertexBuffer.Map();
            IndexBuffer.Map();

            {

                unsafe
                {
                    byte* stagVertPtrInit = (byte*)VertexBuffer.MappedData.ToPointer();
                    byte* stagIdxPtrInit = (byte*)(IndexBuffer.MappedData.ToPointer());
                    byte* stagVertPtr = stagVertPtrInit;
                    byte* stagIdxPtr = stagIdxPtrInit;

                    foreach (Schema.Mesh mesh in gltf.Meshes)
                    {

                        string meshName = mesh.Name;
                        if (string.IsNullOrEmpty(meshName))
                        {
                            meshName = "mesh_" + autoNamedMesh.ToString();
                            autoNamedMesh++;
                        }
                        Mesh m = new Mesh { Name = meshName };

                        foreach (Schema.MeshPrimitive p in mesh.Primitives)
                        {
                            ulong indexStart = vertSize;
                            ulong vertexStart = idxSize;
                            int indexCount = 0;
                            int vertexCount = 0;


                            Schema.Accessor AccPos = null, AccNorm = null, AccUv = null, AccUv1 = null;

                            if (p.Attributes.TryGetValue("POSITION", out int accessorIdx))
                            {
                                AccPos = gltf.Accessors[accessorIdx];
                                ensure_buffer_is_loaded(gltf.BufferViews[(int)AccPos.BufferView].Buffer);
                            }
                            if (p.Attributes.TryGetValue("NORMAL", out accessorIdx))
                            {
                                AccNorm = gltf.Accessors[accessorIdx];
                                ensure_buffer_is_loaded(gltf.BufferViews[(int)AccNorm.BufferView].Buffer);
                            }
                            if (p.Attributes.TryGetValue("TEXCOORD_0", out accessorIdx))
                            {
                                AccUv = gltf.Accessors[accessorIdx];
                                ensure_buffer_is_loaded(gltf.BufferViews[(int)AccUv.BufferView].Buffer);
                            }
                            if (p.Attributes.TryGetValue("TEXCOORD_1", out accessorIdx))
                            {
                                AccUv1 = gltf.Accessors[accessorIdx];
                                ensure_buffer_is_loaded(gltf.BufferViews[(int)AccUv1.BufferView].Buffer);
                            }



                            //prim.BoundingBox.Min.ImportFloatArray(AccPos.Min);
                            //prim.BoundingBox.Max.ImportFloatArray(AccPos.Max);
                            //prim.BoundingBox.IsValid = true;

                            //Interleaving vertices
                            byte* inPosPtr = null, inNormPtr = null, inUvPtr = null, inUv1Ptr = null;

                            Schema.BufferView bv = gltf.BufferViews[(int)AccPos.BufferView];
                            inPosPtr = (byte*)bufferHandles[bv.Buffer].AddrOfPinnedObject().ToPointer();
                            inPosPtr += AccPos.ByteOffset + bv.ByteOffset;

                            if (AccNorm is not null)
                            {
                                bv = gltf.BufferViews[(int)AccNorm.BufferView];
                                inNormPtr = (byte*)bufferHandles[bv.Buffer].AddrOfPinnedObject().ToPointer();
                                inNormPtr += AccNorm.ByteOffset + bv.ByteOffset;
                            }
                            if (AccUv is not null)
                            {
                                bv = gltf.BufferViews[(int)AccUv.BufferView];
                                inUvPtr = (byte*)bufferHandles[bv.Buffer].AddrOfPinnedObject().ToPointer();
                                inUvPtr += AccUv.ByteOffset + bv.ByteOffset;
                            }
                            if (AccUv1 is not null)
                            {
                                bv = gltf.BufferViews[(int)AccUv1.BufferView];
                                inUv1Ptr = (byte*)bufferHandles[bv.Buffer].AddrOfPinnedObject().ToPointer();
                                inUv1Ptr += AccUv1.ByteOffset + bv.ByteOffset;
                            }

                            vertexCount = AccPos.Count;


                            //TODO: use vertex attributes scan for copying data if they exists
                            for (int j = 0; j < AccPos.Count; j++)
                            {
                                System.Buffer.MemoryCopy(inPosPtr, stagVertPtr, 12, 12);
                                inPosPtr += 12;
                                if (inNormPtr is not null)
                                {
                                    System.Buffer.MemoryCopy(inNormPtr, stagVertPtr + 12, 12, 12);
                                    inNormPtr += 12;
                                }
                                if (inUvPtr is not null)
                                {
                                    System.Buffer.MemoryCopy(inUvPtr, stagVertPtr + 24, 8, 8);
                                    inUvPtr += 8;
                                }
                                if (inUv1Ptr is not null)
                                {
                                    System.Buffer.MemoryCopy(inUv1Ptr, stagVertPtr + 32, 8, 8);
                                    inUv1Ptr += 8;
                                }
                                stagVertPtr += vertexByteSize;
                            }

                            //indices loading
                            if (p.Indices is not null)
                            {
                                Schema.Accessor acc = gltf.Accessors[(int)p.Indices];
                                bv = gltf.BufferViews[(int)acc.BufferView];


                                indexCount = acc.Count;

                                byte* inIdxPtr = (byte*)bufferHandles[bv.Buffer].AddrOfPinnedObject().ToPointer();
                                inIdxPtr += acc.ByteOffset + bv.ByteOffset;

                                //TODO:double check this, I dont seems to increment stag pointer
                                if (acc.ComponentType is Schema.Accessor.ComponentTypeEnum.UNSIGNED_SHORT)
                                {
                                    if (IndexType is IndexType.Uint16)
                                    {
                                        System.Buffer.MemoryCopy(inIdxPtr, stagIdxPtr, (long)acc.Count * 2, (long)acc.Count * 2);
                                        stagIdxPtr += (long)acc.Count * 2;
                                    }
                                    else
                                    {
                                        uint* usPtr = (uint*)stagIdxPtr;
                                        ushort* inPtr = (ushort*)inIdxPtr;
                                        for (int i = 0; i < acc.Count; i++)
                                        {
                                            usPtr[i] = inPtr[i];
                                        }

                                        stagIdxPtr += (long)acc.Count * 4;
                                    }
                                }
                                else if (acc.ComponentType is Schema.Accessor.ComponentTypeEnum.UNSIGNED_INT)
                                {
                                    if (IndexType is IndexType.Uint32)
                                    {
                                        System.Buffer.MemoryCopy(inIdxPtr, stagIdxPtr, (long)acc.Count * 4, (long)acc.Count * 4);
                                        stagIdxPtr += (long)acc.Count * 4;
                                    }
                                    else
                                    {
                                        ushort* usPtr = (ushort*)stagIdxPtr;
                                        uint* inPtr = (uint*)inIdxPtr;
                                        for (int i = 0; i < acc.Count; i++)
                                        {
                                            usPtr[i] = (ushort)inPtr[i];
                                        }

                                        stagIdxPtr += (long)acc.Count * 2;
                                    }
                                }
                                else if (acc.ComponentType is Schema.Accessor.ComponentTypeEnum.UNSIGNED_BYTE)
                                {
                                    //convert
                                    if (IndexType is IndexType.Uint16)
                                    {
                                        ushort* usPtr = (ushort*)stagIdxPtr;
                                        for (int i = 0; i < acc.Count; i++)
                                        {
                                            usPtr[i] = inIdxPtr[i];
                                        }

                                        stagIdxPtr += (long)acc.Count * 2;
                                    }
                                    else
                                    {
                                        uint* usPtr = (uint*)stagIdxPtr;
                                        for (int i = 0; i < acc.Count; i++)
                                        {
                                            usPtr[i] = inIdxPtr[i];
                                        }

                                        stagIdxPtr += (long)acc.Count * 4;
                                    }
                                }
                                else
                                {
                                    throw new NotImplementedException();
                                }


                                Primitive prim = new Primitive
                                {
                                    FirstIndex = (int)indexStart,
                                    FirstVertex = (int)vertexStart,
                                    VertexCount = vertexCount,
                                    Material = p.Material ?? 0,
                                };



                                prim.IndexCount = indexCount;
                                m.AddPrimitive(prim);
                            }


                            vertexCount += AccPos.Count;
                        }
                        Meshes.Add(m);
                    }
                }

                VertexBuffer.Unmap();
                IndexBuffer.Unmap();

                //CommandBuffer cmd = new CommandBuffer(_device, CommandBufferType.AsyncTransfer);
                //cmd.Start();

                //stagging.CopyTo(cmd, vbo, vertSize, 0, vboOffset);
                //if (iCount > 0)
                //    stagging.CopyTo(cmd, ibo, idxSize, vertSize, iboOffset);

                //cmd.End();

                //transferQ.Submit(cmd);

                //dev.WaitIdle();
                //cmd.Free();

            }

            return Meshes;
        }


        public void PreTransform(Node node, Matrix4x4 currentTransform)
        {
            Matrix4x4 localMat = node.LocalMatrix * currentTransform;



            //vertexBuffer.Clear();
            if (node.Mesh != null)
            {
                foreach (Primitive p in node.Mesh.Primitives)
                {

                }
            }
            if (node.Children == null)
            {
                return;
            }

            foreach (Node child in node.Children)
            {
                PreTransform(child, localMat);
            }
        }

        public void RenderNode(CommandBuffer cmd, Node node, Matrix4x4 currentTransform, GraphicsPipelineState pipelineState)
        {
            Matrix4x4 localMat = node.LocalMatrix * currentTransform;

            cmd.PushConstant(pipelineState, ShaderStage.Vertex, localMat);


            if (node.Mesh != null)
            {
                foreach (Primitive p in node.Mesh.Primitives)
                {
                    cmd.DrawIndexed(p.IndexCount, 1, p.FirstIndex, 0, 0);
                }
            }
            if (node.Children == null)
            {
                return;
            }

            foreach (Node child in node.Children)
            {
                RenderNode(cmd, child, localMat, pipelineState);
            }
        }




        public void DrawNode2(CommandBuffer commandBuffer, Mesh m)
        {
            foreach (Primitive primitive in m.Primitives)
            {
                commandBuffer.DrawIndexed(primitive.IndexCount, 1, primitive.FirstIndex, 0, 0);

            }

        }



        public void Draw(CommandBuffer commandBuffer, GraphicsPipelineState pipelineState)
        {
            commandBuffer.SetVertexBuffers(new[] { VertexBuffer });
            commandBuffer.SetIndexBuffer(IndexBuffer, 0, IndexType.Uint32);


            //commandBuffer.BindDescriptorSets(new DescriptorSet);

            foreach (var sc in Scenes)
            {
                foreach (Node node in sc.Root.Children)
                {
                    RenderNode(commandBuffer, node, sc.Root.LocalMatrix, pipelineState);
                }
            }
        }



        public List<Scene> LoadScenes()
        {
            int defaultScene = -1;
            if (gltf.Scene == null)
            {
                return new();
            }

            List<Scene> scenes = new List<Scene>();
            defaultScene = (int)gltf.Scene;

            for (int i = 0; i < gltf.Scenes.Length; i++)
            {
                Schema.Scene scene = gltf.Scenes[i];
                //Debug.WriteLine("Loading Scene {0}", scene.Name);

                scenes.Add(new Scene
                {
                    Name = scene.Name,
                });

                if (scene.Nodes.Length == 0)
                {
                    continue;
                }

                scenes[i].Root = new Node
                {
                    LocalMatrix = Matrix4x4.Identity,
                    Children = new List<Node>()
                };

                foreach (int nodeIdx in scene.Nodes)
                {
                    LoadNode(scenes[i].Root, gltf.Nodes[nodeIdx]);
                }
            }
            return scenes;
        }




        public void LoadNode(Node parentNode, Schema.Node gltfNode)
        {
            //Debug.WriteLine("Loading node {0}", gltfNode.Name);

            Vector3 translation = new Vector3();
            System.Numerics.Quaternion rotation = System.Numerics.Quaternion.Identity;
            Vector3 scale = new Vector3(1);
            Matrix4x4 localTransform = Matrix4x4.Identity;

            if (gltfNode.Matrix != null)
            {
                float[] M = gltfNode.Matrix;
                localTransform = new Matrix4x4(
                    M[0], M[1], M[2], M[3],
                    M[4], M[5], M[6], M[7],
                    M[8], M[9], M[10], M[11],
                   M[12], M[13], M[14], M[15]);
            }

            if (gltfNode.Translation != null)
            {
                FromFloatArray(ref translation, gltfNode.Translation);
            }

            if (gltfNode.Translation != null)
            {
                FromFloatArray(ref rotation, gltfNode.Rotation);
            }

            if (gltfNode.Translation != null)
            {
                FromFloatArray(ref scale, gltfNode.Scale);
            }

            localTransform *=
                Matrix4x4.CreateScale(scale) *
                Matrix4x4.CreateFromQuaternion(rotation) *
                Matrix4x4.CreateTranslation(translation);

            //localTransform = Matrix4x4.Identity;

            Node node = new Node
            {
                LocalMatrix = localTransform,
                Parent = parentNode,
                Name = gltfNode.Name
            };
            parentNode.Children.Add(node);

            if (gltfNode.Children != null)
            {
                node.Children = new List<Node>();
                for (int i = 0; i < gltfNode.Children.Length; i++)
                {
                    LoadNode(node, gltf.Nodes[gltfNode.Children[i]]);
                }
            }

            if (gltfNode.Mesh != null)
            {
                node.Mesh = Meshes[(int)gltfNode.Mesh];
            }
        }

        public static void FromFloatArray(ref Vector3 v, float[] floats)
        {
            if (floats.Length > 0)
            {
                v.X = floats[0];
            }

            if (floats.Length > 1)
            {
                v.Y = floats[1];
            }

            if (floats.Length > 2)
            {
                v.Z = floats[2];
            }
        }


        public static void FromFloatArray(ref System.Numerics.Quaternion v, float[] floats)
        {
            if (floats.Length > 0)
            {
                v.X = floats[0];
            }

            if (floats.Length > 1)
            {
                v.Y = floats[1];
            }

            if (floats.Length > 2)
            {
                v.Z = floats[2];
            }

            if (floats.Length > 3)
            {
                v.W = floats[3];
            }
        }







        public void Update()
        {

        }



    }
}

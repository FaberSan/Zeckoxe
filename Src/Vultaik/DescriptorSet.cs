﻿// Copyright (c) 2019-2021 Faber Leonardo. All Rights Reserved. https://github.com/FaberSanZ
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)


using System;
using System.Collections.Generic;
using Vortice.Vulkan;
using static Vortice.Vulkan.Vulkan;

namespace Vultaik
{
    public unsafe class DescriptorSet : GraphicsResource
    {

        internal VkDescriptorPool _descriptorPool;
        internal VkDescriptorSet _descriptorSet;
        internal VkDescriptorSetLayout _descriptorSetLayout;
        internal VkPipelineLayout _pipelineLayout;


        internal List<ResourceData> Resources = new();
        internal VkPipelineBindPoint BindPoint;

        public DescriptorSet(GraphicsPipeline pipeline, DescriptorData data) : base(pipeline.NativeDevice)
        {

            VkDescriptorSetLayout descriptor_set_layout = pipeline._descriptorSetLayout;
            NativeDevice._descriptorPoolManager_0.Allocate(descriptor_set_layout);
            _descriptorSet = NativeDevice._descriptorPoolManager_0.handle;
            _pipelineLayout = pipeline._pipelineLayout;
            GraphicsPipeline = pipeline;
            DescriptorData = data;
            BindPoint = VkPipelineBindPoint.Graphics;
            Build();
        }


        public DescriptorSet(ComputePipeline pipeline, DescriptorData data) : base(pipeline.NativeDevice)
        {

            VkDescriptorSetLayout descriptor_set_layout = pipeline._descriptorSetLayout;
            NativeDevice._descriptorPoolManager_0.Allocate(descriptor_set_layout);
            _descriptorSet = NativeDevice._descriptorPoolManager_0.handle;
            _pipelineLayout = pipeline._pipelineLayout;
            ComputePipeline = pipeline;
            DescriptorData = data;
            BindPoint = VkPipelineBindPoint.Compute;
            Build();
        }

        public GraphicsPipeline GraphicsPipeline { get; }
        public ComputePipeline ComputePipeline  { get; }

        public DescriptorData DescriptorData { get; }

        public void Build()
        {
            Resources = DescriptorData.Data;


            int resources_count = Resources.Count;

            List<VkWriteDescriptorSet> writes = new();// stackalloc VkWriteDescriptorSet[resources_count + 12];
            VkDescriptorBufferInfo* bufferInfos = stackalloc VkDescriptorBufferInfo[resources_count];
            VkDescriptorImageInfo* imageInfos = stackalloc VkDescriptorImageInfo[resources_count];


            for (int i = 0; i < resources_count; i++)
            {
                ResourceData r = Resources[i];

                if (r.DescriptorType == VkDescriptorType.UniformBuffer)
                {

                    bufferInfos[i] = new()
                    {
                        buffer = r.Buffer.handle,
                        offset = (ulong)r.Offset,
                        range = (ulong)r.Buffer.SizeInBytes,
                    };

                    VkWriteDescriptorSet write_descriptor = new()
                    {
                        sType = VkStructureType.WriteDescriptorSet,
                        pNext = null,
                        dstSet = _descriptorSet,
                        descriptorCount = 1,
                        descriptorType = r.DescriptorType,
                        pBufferInfo = &bufferInfos[i],
                        dstBinding = (uint)r.Binding,
                    };


                    writes.Add(write_descriptor);
                }


                if (r.DescriptorType == VkDescriptorType.StorageBuffer)
                {

                    bufferInfos[i] = new()
                    {
                        buffer = r.Buffer.handle,
                        offset = (ulong)r.Offset,
                        range = (ulong)r.Buffer.SizeInBytes,
                    };

                    VkWriteDescriptorSet write_descriptor = new()
                    {
                        sType = VkStructureType.WriteDescriptorSet,
                        pNext = null,
                        dstSet = _descriptorSet,
                        descriptorCount = 1,
                        descriptorType = r.DescriptorType,
                        pBufferInfo = &bufferInfos[i],
                        dstBinding = (uint)r.Binding,
                    };


                    writes.Add(write_descriptor);
                }


                else if (r.DescriptorType == VkDescriptorType.UniformBufferDynamic)
                {

                    bufferInfos[i] = new()
                    {
                        buffer = r.Buffer.handle,
                        offset = (ulong)r.Offset,
                        range = (ulong)r.Buffer.SizeInBytes,
                    };

                    VkWriteDescriptorSet write_descriptor = new()
                    {
                        sType = VkStructureType.WriteDescriptorSet,
                        pNext = null,
                        dstSet = _descriptorSet,
                        descriptorCount = 1,
                        descriptorType = r.DescriptorType,
                        pBufferInfo = &bufferInfos[i],
                        dstBinding = (uint)r.Binding,
                    };


                    writes.Add(write_descriptor);

                }

                else if (r.DescriptorType == VkDescriptorType.CombinedImageSampler)
                {
                    imageInfos[i] = new()
                    {
                        imageLayout = VkImageLayout.ShaderReadOnlyOptimal,
                        imageView = r.Texture.image_view,
                        sampler = r.Sampler.handle,
                    };

                    VkWriteDescriptorSet write_descriptor = new()
                    {
                        sType = VkStructureType.WriteDescriptorSet,
                        dstSet = _descriptorSet,
                        dstBinding = (uint)r.Binding,
                        dstArrayElement = 0,
                        descriptorType = r.DescriptorType,
                        descriptorCount = 1,
                        pImageInfo = &imageInfos[i],

                    };

                    writes.Add(write_descriptor);


                }

                else if (r.DescriptorType == VkDescriptorType.SampledImage)
                {
                    imageInfos[i] = new()
                    {
                        imageLayout = VkImageLayout.ShaderReadOnlyOptimal,
                        imageView = r.Texture.image_view,
                    };

                    VkWriteDescriptorSet write_descriptor = new()
                    {
                        sType = VkStructureType.WriteDescriptorSet,
                        dstSet = _descriptorSet,
                        dstBinding = (uint)r.Binding,
                        dstArrayElement = 0,
                        descriptorType = r.DescriptorType,
                        descriptorCount = 1,
                        pImageInfo = &imageInfos[i],

                    };


                    writes.Add(write_descriptor);


                }


                else if (r.DescriptorType == VkDescriptorType.StorageImage)
                {
                    imageInfos[i] = new()
                    {
                        imageLayout = VkImageLayout.ShaderReadOnlyOptimal,
                        imageView = r.Texture.image_view,
                    };

                    VkWriteDescriptorSet write_descriptor = new()
                    {
                        sType = VkStructureType.WriteDescriptorSet,
                        dstSet = _descriptorSet,
                        dstBinding = (uint)r.Binding,
                        dstArrayElement = 0,
                        descriptorType = r.DescriptorType,
                        descriptorCount = 1,
                        pImageInfo = &imageInfos[i],

                    };


                    writes.Add(write_descriptor);


                }

                else if (r.DescriptorType == VkDescriptorType.Sampler)
                {
                    imageInfos[i] = new()
                    {
                        sampler = r.Sampler.handle,
                    };

                    VkWriteDescriptorSet write_descriptor = new()
                    {
                        sType = VkStructureType.WriteDescriptorSet,
                        dstSet = _descriptorSet,
                        dstBinding = (uint)r.Binding,
                        descriptorType = r.DescriptorType,
                        descriptorCount = 1,
                        pImageInfo = &imageInfos[i],

                    };


                    writes.Add(write_descriptor);

                }
            }

            var bind_resources = DescriptorData.DataBindless;

            for (int i = 0; i < bind_resources.Count; i++)
            {
                if (bind_resources[i].DescriptorType == VkDescriptorType.SampledImage)
                {
                    VkDescriptorImageInfo* image_infos = stackalloc VkDescriptorImageInfo[bind_resources[i].Images.Length];

                    for (int b = 0; b < bind_resources[i].Images.Length; b++)
                    {
                        image_infos[b] = new VkDescriptorImageInfo()
                        {
                            imageLayout = VkImageLayout.ShaderReadOnlyOptimal,
                            imageView = bind_resources[i].Images[b].image_view,
                        };
                    }
                    VkWriteDescriptorSet write_descriptor_bind = new()
                    {
                        sType = VkStructureType.WriteDescriptorSet,
                        dstSet = _descriptorSet,
                        dstBinding = (uint)bind_resources[i].Binding,
                        descriptorType = VkDescriptorType.SampledImage,
                        descriptorCount = (uint)bind_resources[i].Images.Length,
                        pImageInfo = image_infos,
                    };

                    writes.Add(write_descriptor_bind);

                }

            }



            ReadOnlySpan<VkWriteDescriptorSet> readOnlySpan = new ReadOnlySpan<VkWriteDescriptorSet>(writes.ToArray());

            vkUpdateDescriptorSets(NativeDevice.handle, readOnlySpan);
        }


        public void Free()
        {
            //vkFreeDescriptorSets()
        }

    }

}

﻿// Copyright (c) 2019-2020 Faber Leonardo. All Rights Reserved. https://github.com/FaberSanZ
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

/*=============================================================================
	HeapPool.cs
=============================================================================*/

using System;
using System.Collections.Generic;
using System.Linq;
using Vortice.Vulkan;
using static Vortice.Vulkan.Vulkan;

namespace Zeckoxe.Vulkan
{
    public unsafe class HeapPool : GraphicsResource, IDisposable
    {
        internal static uint MaxSets = 1024;


        internal readonly Dictionary<VkDescriptorType, uint> MaxDescriptorTypeCounts = new()
        {
            [VkDescriptorType.Sampler] = (MaxSets * 2),

            [VkDescriptorType.CombinedImageSampler] = (MaxSets * 3),

            [VkDescriptorType.SampledImage] = (MaxSets / 2),

            [VkDescriptorType.StorageImage] = (MaxSets / 64),

            [VkDescriptorType.UniformTexelBuffer] = (MaxSets * 2),

            [VkDescriptorType.StorageTexelBuffer] = (MaxSets / 64),

            [VkDescriptorType.UniformBuffer] = (MaxSets / 2),

            [VkDescriptorType.StorageBuffer] = (MaxSets / 64),

            [VkDescriptorType.UniformBufferDynamic] = (MaxSets / 64),

            [VkDescriptorType.StorageBufferDynamic] = (MaxSets / 64),

            [VkDescriptorType.InputAttachment] = (MaxSets / 64),
        };




        internal VkDescriptorPool handle;

        public HeapPool(Device device) : base(device)
        {
            Settings settings = NativeDevice.NativeParameters.Settings;

            if ((settings.OptionalDeviceExtensions & OptionalDeviceExtensions.RayTracing) != 0 && NativeDevice.RayTracingSupport) 
                MaxDescriptorTypeCounts[VkDescriptorType.AccelerationStructureKHR] = (MaxSets / 2);
        }

        internal void Create()
        {

            uint pool_size = (uint)MaxDescriptorTypeCounts.Count;

            VkDescriptorPoolSize* sizes = stackalloc VkDescriptorPoolSize[(int)pool_size];


            for (int index = 0; index < pool_size; index++)
            {
                KeyValuePair<VkDescriptorType, uint> item = MaxDescriptorTypeCounts.ElementAt(index);

                
                sizes[index] = new()
                {
                    type = item.Key,
                    descriptorCount = item.Value,
                };

            }


            VkDescriptorPoolCreateInfo pool_create_info = new VkDescriptorPoolCreateInfo
            {
                sType = VkStructureType.DescriptorPoolCreateInfo,
                pNext = null,
                flags = VkDescriptorPoolCreateFlags.None,
                poolSizeCount = pool_size,
                pPoolSizes = sizes,
                maxSets = MaxSets,
            };


            vkCreateDescriptorPool(NativeDevice.handle, &pool_create_info, null, out handle);
        }

        public void Reset()
        {
            vkResetDescriptorPool(NativeDevice.handle, handle, VkDescriptorPoolResetFlags.None);
        }

        public void Destroy()
        {
            vkDestroyDescriptorPool(NativeDevice.handle, handle, null);
        }

        public void Dispose()
        {
            // Destroy();
        }
    }
}

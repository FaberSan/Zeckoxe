﻿// Copyright (c) 2019-2020 Faber Leonardo. All Rights Reserved.

/*=============================================================================
	CommandList.cs
=============================================================================*/


using Vortice.Vulkan;
using Zeckoxe.Mathematics;
using static Vortice.Vulkan.Vulkan;

namespace Zeckoxe.Graphics
{
    public unsafe class CommandBuffer : GraphicsResource
    {

        internal uint imageIndex;
        internal VkCommandBuffer NativeCommandBuffer;





        public CommandBuffer(GraphicsDevice graphicsDevice) : base(graphicsDevice)
        {
            Recreate();
        }



        public void Recreate()
        {
            NativeCommandBuffer = NativeDevice.CreateCommandBufferPrimary();
        }


        public void Begin()
        {
            // By setting timeout to UINT64_MAX we will always wait until the next image has been acquired or an actual error is thrown
            // With that we don't have to handle VK_NOT_READY
            uint i = 0;
            vkAcquireNextImageKHR(NativeDevice.Device, NativeDevice.NativeSwapChain.SwapChain, ulong.MaxValue, NativeDevice.ImageAvailableSemaphore, new VkFence(), &i);
            imageIndex = i;


            VkCommandBufferBeginInfo beginInfo = new VkCommandBufferBeginInfo()
            {
                sType = VkStructureType.CommandBufferBeginInfo,
                flags = VkCommandBufferUsageFlags.RenderPassContinue,
            };

            vkBeginCommandBuffer(NativeCommandBuffer, &beginInfo);
        }


        public void BeginFramebuffer(Framebuffer framebuffer)
        {

            VkRenderPassBeginInfo renderPassInfo = new VkRenderPassBeginInfo()
            {
                sType = VkStructureType.RenderPassBeginInfo,
                renderPass = framebuffer.NativeRenderPass,
                framebuffer = framebuffer.SwapChainFramebuffers[imageIndex],
                renderArea = new VkRect2D()
                {
                    extent = new VkExtent2D((uint)NativeDevice.NativeParameters.BackBufferWidth, (uint)NativeDevice.NativeParameters.BackBufferHeight)
                },
            };

            vkCmdBeginRenderPass(NativeCommandBuffer, &renderPassInfo, VkSubpassContents.Inline);
        }


        public void Clear(float R, float G, float B, float A = 1.0f)
        {
            VkClearColorValue clearValue = new VkClearColorValue(R, G, B, A);

            VkImageSubresourceRange clearRange = new VkImageSubresourceRange()
            {
                aspectMask = VkImageAspectFlags.Color,
                baseMipLevel = 0,
                baseArrayLayer = 0,
                layerCount = 1,
                levelCount = 1
            };

            vkCmdClearColorImage(NativeCommandBuffer, NativeDevice.NativeSwapChain.Images[(int)imageIndex], VkImageLayout.ColorAttachmentOptimal, &clearValue, 1, &clearRange);
        }


        public void Clear(RawColor color)
        {
            VkClearColorValue clearValue = new VkClearColorValue(color.R, color.G, color.B, color.A);

            VkImageSubresourceRange clearRange = new VkImageSubresourceRange
            {
                aspectMask = VkImageAspectFlags.Color,
                baseArrayLayer = 1,
                baseMipLevel = 0
            };

            vkCmdClearColorImage(NativeCommandBuffer, NativeDevice.NativeSwapChain.Images[(int)imageIndex], VkImageLayout.ColorAttachmentOptimal, &clearValue, 1, &clearRange);
        }




        public void SetGraphicPipeline(PipelineState pipelineState)
        {
            vkCmdBindPipeline(NativeCommandBuffer, VkPipelineBindPoint.Graphics, pipelineState.graphicsPipeline);
        }


        public void SetComputePipeline(PipelineState pipelineState)
        {
            //vkCmdBindPipeline(NativeCommandBuffer, VkPipelineBindPoint.Compute, pipelineState.computesPipeline);
        }


        public void DrawIndexed(int indexCount, int instanceCount, int firstIndex, int vertexOffset, int firstInstance)
        {

        }

        public void SetScissor(int width, int height, int x, int y)
        {
            // Update dynamic scissor state
            VkRect2D scissor = new VkRect2D()
            {
                extent = new VkExtent2D()
                {
                    width = (uint)width,
                    height = (uint)height,
                },
                offset = new VkOffset2D()
                {
                    x = x,
                    y = y,
                },
            };
            

            vkCmdSetScissor(NativeCommandBuffer, 0, 1, &scissor);
        }

        public void SetViewport(float Width, float Height, float X, float Y, float MinDepth = 0.0f, float MaxDepth = 1.0f)
        {
            VkViewport Viewport = new VkViewport()
            {
                width = Width,

                height = Height,

                x = X,

                y = Y,

                minDepth = MinDepth,

                maxDepth = MaxDepth
            };

            vkCmdSetViewport(NativeCommandBuffer, 0, 1, &Viewport);
        }

        public void SetVertexBuffer(Buffer buffer, ulong offsets = 0)
        {
            fixed (VkBuffer* bufferptr = &buffer.Handle)
            {
                vkCmdBindVertexBuffers(NativeCommandBuffer, 0, 1, bufferptr, &offsets);
            }
        }

        public void SetVertexBuffers(Buffer[] buffers, ulong offsets = 0)
        {
            VkBuffer* buffer = stackalloc VkBuffer[buffers.Length];

            for (int i = 0; i < buffers.Length; i++)
            {
                buffer[i] = buffers[i].Handle;
            }

            vkCmdBindVertexBuffers(NativeCommandBuffer, 0, 1, buffer, &offsets);

        }

        public void SetIndexBuffer(Buffer buffer, ulong offsets = 0)
        {
            vkCmdBindIndexBuffer(NativeCommandBuffer, buffer.Handle, offsets, VkIndexType.Uint32);
        }

        public void Draw(int vertexCount, int instanceCount, int firstVertex, int firstInstance)
        {
            vkCmdDraw(NativeCommandBuffer, (uint)vertexCount, (uint)instanceCount, (uint)firstVertex, (uint)firstInstance);
        }




        public void Close()
        {
            CleanupRenderPass();
            vkEndCommandBuffer(NativeCommandBuffer);
        }


        internal unsafe void CleanupRenderPass()
        {
            vkCmdEndRenderPass(NativeCommandBuffer);
        }



        public void Submit()
        {
            VkSemaphore signalSemaphore = NativeDevice.RenderFinishedSemaphore;
            VkSemaphore waitSemaphore = NativeDevice.ImageAvailableSemaphore;
            VkPipelineStageFlags waitStages = VkPipelineStageFlags.ColorAttachmentOutput;
            VkCommandBuffer commandBuffer = NativeCommandBuffer;


            VkSubmitInfo submitInfo = new VkSubmitInfo()
            {
                sType = VkStructureType.SubmitInfo,
                waitSemaphoreCount = 1,
                pWaitSemaphores = &waitSemaphore,
                pWaitDstStageMask = &waitStages,
                commandBufferCount = 1,
                pCommandBuffers = &commandBuffer,
                signalSemaphoreCount = 1,
                pSignalSemaphores = &signalSemaphore,
            };

            vkQueueSubmit(NativeDevice.NativeCommandQueue, 1, &submitInfo, VkFence.Null);
        }


    }
}

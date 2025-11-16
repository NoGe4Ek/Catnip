using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace Catnip.Settings {
// Create a Scriptable Renderer Feature that implements a post-processing effect when the camera is inside a custom volume.
// For more information about creating scriptable renderer features, refer to https://docs.unity3d.com/Manual/urp/customizing-urp.html
public sealed class CustomPostProcessEffectRendererFeature : ScriptableRendererFeature {
    #region FEATURE_FIELDS

    // Declare the material used to render the post-processing effect.
    // Add a [SerializeField] attribute so Unity serializes the property and includes it in builds.
    [SerializeField] public Shader m_bloomShader;
    [SerializeField] public Shader m_compositeShader;

    private Material m_bloomMaterial;
    private Material m_compositeMaterial;

    // Declare the render pass that renders the effect.
    private CustomPostRenderPass m_customPass;

    #endregion

    #region FEATURE_METHODS

    // Override the Create method.
    // Unity calls this method when the Scriptable Renderer Feature loads for the first time, and when you change a property.
    public override void Create() {
        m_bloomMaterial = CoreUtils.CreateEngineMaterial(m_bloomShader);
        m_compositeMaterial = CoreUtils.CreateEngineMaterial(m_compositeShader);

        m_customPass = new CustomPostRenderPass(name, m_bloomMaterial, m_compositeMaterial);
    }

    public override void SetupRenderPasses(ScriptableRenderer renderer, in RenderingData renderingData) {
        if (renderingData.cameraData.cameraType == CameraType.Game) {
            m_customPass.ConfigureInput(ScriptableRenderPassInput.Depth);
            m_customPass.ConfigureInput(ScriptableRenderPassInput.Color);
            m_customPass.SetTarget(renderer.cameraColorTargetHandle, renderer.cameraDepthTargetHandle);
        }
    }

    // Override the AddRenderPasses method to inject passes into the renderer. Unity calls AddRenderPasses once per camera.
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData) {
        // Skip rendering if the pass instance are null.
        if (m_customPass == null) return; // todo remove?

        // Skip rendering if the target is a Reflection Probe or a preview camera.
        if (renderingData.cameraData.cameraType == CameraType.Preview ||
            renderingData.cameraData.cameraType == CameraType.Reflection)
            return; // todo remove?

        // Skip rendering if the camera is outside the custom volume.
        BenDayBloomPostProcessEffectVolumeComponent myVolume =
            VolumeManager.instance.stack?.GetComponent<BenDayBloomPostProcessEffectVolumeComponent>();
        if (myVolume == null || !myVolume.IsActive()) return; // todo remove?

        // Specify when the effect will execute during the frame.
        // For a post-processing effect, the injection point is usually BeforeRenderingTransparents, BeforeRenderingPostProcessing, or AfterRenderingPostProcessing.
        // For more information, refer to https://docs.unity3d.com/Manual/urp/customize/custom-pass-injection-points.html 
        // m_customPass.renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing; // todo remove?

        // Specify that the effect doesn't need scene depth, normals, motion vectors, or the color texture as input.
        // m_customPass.ConfigureInput(ScriptableRenderPassInput.None); // todo remove?

        // Add the render pass to the renderer.
        renderer.EnqueuePass(m_customPass);
    }

    protected override void Dispose(bool disposing) {
        // Free the resources the render pass uses.
        // m_customPass.Dispose(); // todo remove?
        CoreUtils.Destroy(m_bloomMaterial);
        CoreUtils.Destroy(m_compositeMaterial);
    }

    #endregion

    // Create the custom render pass.
    private class CustomPostRenderPass : ScriptableRenderPass {
        #region PASS_FIELDS

        // Declare the material used to render the post-processing effect.
        private Material m_bloomMaterial;
        private Material m_compositeMaterial;

        private const int k_maxPyramidSize = 16;
        private int[] _BloomMipUp;
        private int[] _BloomMipDown;
        private RTHandle tempSource;
        private int _tempSource;
        private RTHandle[] m_BloomMipUp;
        private RTHandle[] m_BloomMipDown;
        private GraphicsFormat hdrFormat;

        private RenderTextureDescriptor m_Descriptor;
        private RTHandle m_CameraColorTarget;
        private RTHandle m_CameraDepthTarget;
        private BenDayBloomPostProcessEffectVolumeComponent m_BloomEffect;
        
        private static readonly int kBlitTexturePropertyId = Shader.PropertyToID("_BlitTexture"); // todo as example

        #endregion

        public CustomPostRenderPass(string passName, Material bloomMaterial, Material compositeMaterial) {
            // Add a profiling sampler.
            profilingSampler = new ProfilingSampler(passName);

            // Assign the material to the render pass.
            m_bloomMaterial = bloomMaterial;
            m_compositeMaterial = compositeMaterial;

            renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;

            _BloomMipUp = new int[k_maxPyramidSize];
            _BloomMipDown = new int[k_maxPyramidSize];
            m_BloomMipUp = new RTHandle[k_maxPyramidSize];
            m_BloomMipDown = new RTHandle[k_maxPyramidSize];

            _tempSource = Shader.PropertyToID("_TempSource");
            tempSource = RTHandles.Alloc(_tempSource, name: "_TempSource");
            
            for (int i = 0; i < k_maxPyramidSize; i++) {
                _BloomMipUp[i] = Shader.PropertyToID("_BloomMipUp" + i);
                _BloomMipDown[i] = Shader.PropertyToID("_BloomMipDown" + i);
                // Get name, will get Allocated with descriptor later
                m_BloomMipUp[i] = RTHandles.Alloc(_BloomMipUp[i], name: "_BloomMipUp" + i);
                m_BloomMipDown[i] = RTHandles.Alloc(_BloomMipDown[i], name: "_BloomMipDown" + i);
            }

            const FormatUsage usage = FormatUsage.Linear | FormatUsage.Render;
            // HDR fallback
            if (SystemInfo.IsFormatSupported(GraphicsFormat.B10G11R11_UFloatPack32, usage)) {
                hdrFormat = GraphicsFormat.B10G11R11_UFloatPack32;
            } else {
                hdrFormat = QualitySettings.activeColorSpace == ColorSpace.Linear
                    ? GraphicsFormat.R8G8B8A8_SRGB
                    : GraphicsFormat.B8G8R8A8_UNorm;
            }

            // To make sure the render pass can sample the active color buffer, set URP to render to intermediate textures instead of directly to the backbuffer.
            // requiresIntermediateTexture = kSampleActiveColor; // todo remove?
        }

        #region PASS_NON_RENDER_GRAPH_PATH

        // Override the OnCameraSetup method to configure render targets and their clear states, and create temporary render target textures.
        // Unity calls this method before executing the render pass.
        // This method is used only in the Compatibility Mode path.
        // Use ConfigureTarget or ConfigureClear in this method. Don't use CommandBuffer.SetRenderTarget.
        [System.Obsolete(
            "This rendering path works in Compatibility Mode only, which is deprecated. Use the render graph API instead.",
            false)]
        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData) {
            // Reset the render target to default.
            // ResetTarget(); // todo remove?

            m_Descriptor = renderingData.cameraData.cameraTargetDescriptor;

            // Allocate a temporary texture, and reallocate it if there's a change to camera settings, for example resolution.
            // if (kSampleActiveColor) // todo remove?
            //     RenderingUtils.ReAllocateHandleIfNeeded(ref m_CopiedColor,
            //         GetCopyPassTextureDescriptor(renderingData.cameraData.cameraTargetDescriptor),
            //         name: "_CustomPostPassCopyColor");
        }

        public void SetTarget(RTHandle cameraColorTargetHandle, RTHandle cameraDepthTargetHandle) {
            m_CameraColorTarget = cameraColorTargetHandle;
            m_CameraDepthTarget = cameraDepthTargetHandle;
        }

        // Override the Execute method to implement the rendering logic. Use ScriptableRenderContext to issue drawing commands or execute command buffers.
        // You don't need to call ScriptableRenderContext.Submit.
        // This method is used only in the Compatibility Mode path.
        [System.Obsolete(
            "This rendering path works in Compatibility Mode only, which is deprecated. Use the render graph API instead.",
            false)]
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData) {
            if (renderingData.cameraData.cameraType == CameraType.Preview ||
                renderingData.cameraData.cameraType == CameraType.Reflection ||
                renderingData.cameraData.cameraType == CameraType.SceneView)
                return; // todo remove?
            
            VolumeStack stack = VolumeManager.instance.stack;
            m_BloomEffect = stack.GetComponent<BenDayBloomPostProcessEffectVolumeComponent>();

            CommandBuffer cmd = CommandBufferPool.Get();

            using (new ProfilingScope(cmd, profilingSampler)) {
                RenderTextureDescriptor desc = GetCompatibleDescriptor();
                RenderingUtils.ReAllocateIfNeeded(ref tempSource, desc, FilterMode.Bilinear, TextureWrapMode.Clamp,
                    name: tempSource.name);
                desc.width = Mathf.Max(1, desc.width >> 1);
                desc.height = Mathf.Max(1, desc.height >> 1);
                
        
                // Копируем исходное изображение
                Blitter.BlitCameraTexture(cmd, m_CameraColorTarget, tempSource);
                
                // Do the bloom pass here first
                SetupBloom(cmd, m_CameraColorTarget);

                // Setup composite values
                m_compositeMaterial.SetFloat("_Cutoff", m_BloomEffect.dotsCutoff.value);
                float animatedDensity = m_BloomEffect.dotsDensity.value + Mathf.Sin(Time.time * m_BloomEffect.pulseSpeed.value) * m_BloomEffect.pulseIntensity.value;
                m_compositeMaterial.SetFloat("_BlurAmount", m_BloomEffect.dotsBlur.value);
                m_compositeMaterial.SetFloat("_Density", animatedDensity);
                m_compositeMaterial.SetVector("_Direction", m_BloomEffect.scrollDirection.value);
                
                // ПЕРЕДАЕМ ТЕКСТУРЫ В ШЕЙДЕР:
                m_compositeMaterial.SetTexture("_Source_Texture", tempSource);     // Исходное изображение
                m_compositeMaterial.SetTexture("_Bloom_Texture", m_BloomMipUp[0]); // Bloom текстура
                
                // cmd.SetGlobalTexture("_Source_Texture", tempSource);

                Blitter.BlitCameraTexture(cmd, m_CameraColorTarget, m_CameraColorTarget, m_compositeMaterial, 0);
            }

            // Execute the command buffer.
            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();

            // Release the command buffer.
            CommandBufferPool.Release(cmd);
        }

        private void SetupBloom(CommandBuffer cmd, RTHandle source) {
            // Start at half-res
            int downres = 1;
            int tw = m_Descriptor.width >> downres;
            int th = m_Descriptor.height >> downres;

            // Determine the iteration count
            int maxSize = Mathf.Max(tw, th);
            int iterations = Mathf.FloorToInt(Mathf.Log(maxSize, 2f) - 1);
            int mipCount = Mathf.Clamp(iterations, 1, m_BloomEffect.maxIterations.value);

            // Pre-filtering parameters
            float clamp = m_BloomEffect.clamp.value;
            float threshold = Mathf.GammaToLinearSpace(m_BloomEffect.threshold.value);
            float thresholdKnee = threshold * 0.5f; // Hardcoded soft knee

            // Material setup
            float scatter = Mathf.Lerp(0.05f, 0.95f, m_BloomEffect.scatter.value);
            var bloomMaterial = m_bloomMaterial;

            bloomMaterial.SetVector("_Params", new Vector4(scatter, clamp, threshold, thresholdKnee));

            // Prefilter
            RenderTextureDescriptor desc = GetCompatibleDescriptor(tw, th, hdrFormat);
            for (int i = 0; i < mipCount; i++) {
                RenderingUtils.ReAllocateIfNeeded(ref m_BloomMipUp[i], desc, FilterMode.Bilinear, TextureWrapMode.Clamp,
                    name: m_BloomMipUp[i].name);
                RenderingUtils.ReAllocateIfNeeded(ref m_BloomMipDown[i], desc, FilterMode.Bilinear,
                    TextureWrapMode.Clamp, name: m_BloomMipDown[i].name);
                desc.width = Mathf.Max(1, desc.width >> 1);
                desc.height = Mathf.Max(1, desc.height >> 1);
            }

            Blitter.BlitCameraTexture(cmd, source, m_BloomMipDown[0], RenderBufferLoadAction.DontCare,
                RenderBufferStoreAction.Store, bloomMaterial, 0);

            // Downsample - gaussian pyramid
            var lastDown = m_BloomMipDown[0];
            for (int i = 0; i < mipCount; i++) {
                // Classic two pass gaussian blur - use mipUp as temporary target
                // First pass does 2x downsampling + 9-tap gaussian
                // Second pass does 9-tap gaussian using a 5-tap filter + bilinear filtering
                Blitter.BlitCameraTexture(cmd, lastDown, m_BloomMipUp[i], RenderBufferLoadAction.DontCare,
                    RenderBufferStoreAction.Store, bloomMaterial, 1);
                Blitter.BlitCameraTexture(cmd, m_BloomMipUp[i], m_BloomMipDown[i], RenderBufferLoadAction.DontCare,
                    RenderBufferStoreAction.Store, bloomMaterial, 2);

                lastDown = m_BloomMipDown[i];
            }

            // Upsample (bilinear by default, HQ filtering does bicubic instead
            for (int i = mipCount - 2; i >= 0; i--) {
                var lowMip = (i == mipCount - 2) ? m_BloomMipDown[i + 1] : m_BloomMipUp[i + 1];
                var highMip = m_BloomMipDown[i];
                var dst = m_BloomMipUp[i];

                cmd.SetGlobalTexture("_SourceTexLowMip", lowMip);
                Blitter.BlitCameraTexture(cmd, highMip, dst, RenderBufferLoadAction.DontCare,
                    RenderBufferStoreAction.Store, bloomMaterial, 3);
            }

            cmd.SetGlobalTexture("_Bloom_Texture", m_BloomMipUp[0]);
            cmd.SetGlobalFloat("_BloomIntensity", m_BloomEffect.intensity.value);
        }

        RenderTextureDescriptor GetCompatibleDescriptor()
            => GetCompatibleDescriptor(m_Descriptor.width, m_Descriptor.height, m_Descriptor.graphicsFormat);

        RenderTextureDescriptor GetCompatibleDescriptor(int width, int height, GraphicsFormat format,
            DepthBits depthBufferBits = DepthBits.None)
            => GetCompatibleDescriptor(m_Descriptor, width, height, format, depthBufferBits);

        internal static RenderTextureDescriptor GetCompatibleDescriptor(RenderTextureDescriptor desc, int width,
            int height, GraphicsFormat format, DepthBits depthBufferBits = DepthBits.None) {
            desc.depthBufferBits = (int)depthBufferBits;
            desc.msaaSamples = 1;
            desc.width = width;
            desc.height = height;
            desc.graphicsFormat = format;
            return desc;
        }

        // Free the resources the camera uses.
        // This method is used only in the Compatibility Mode path.
        public override void OnCameraCleanup(CommandBuffer cmd) { }

        #endregion

        /*#region PASS_RENDER_GRAPH_PATH

        // Declare the resource the copy render pass uses.
        // This method is used only in the render graph system path.
        private class CopyPassData {
            public TextureHandle inputTexture;
        }

        // Declare the resources the main render pass uses.
        // This method is used only in the render graph system path.
        private class MainPassData {
            public Material material;
            public TextureHandle inputTexture;
        }

        private static void ExecuteCopyColorPass(CopyPassData data, RasterGraphContext context) {
            ExecuteCopyColorPass(context.cmd, data.inputTexture);
        }

        private static void ExecuteMainPass(MainPassData data, RasterGraphContext context) {
            ExecuteMainPass(context.cmd, data.inputTexture.IsValid() ? data.inputTexture : null, data.material);
        }

        // Override the RecordRenderGraph method to implement the rendering logic.
        // This method is used only in the render graph system path.
        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData) {
            // Get the resources the pass uses.
            UniversalResourceData resourcesData = frameData.Get<UniversalResourceData>();
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();

            // Sample from the current color texture.
            using (var builder =
                   renderGraph.AddRasterRenderPass<MainPassData>(passName, out var passData, profilingSampler)) {
                passData.material = m_Material;

                TextureHandle destination;

                // Copy cameraColor to a temporary texture, if the kSampleActiveColor property is set to true.
                if (kSampleActiveColor) {
                    var cameraColorDesc = renderGraph.GetTextureDesc(resourcesData.cameraColor);
                    cameraColorDesc.name = "_CameraColorCustomPostProcessing";
                    cameraColorDesc.clearBuffer = false;

                    destination = renderGraph.CreateTexture(cameraColorDesc);
                    passData.inputTexture = resourcesData.cameraColor;

                    // If you use framebuffer fetch in your material, use builder.SetInputAttachment to reduce GPU bandwidth usage and power consumption.
                    builder.UseTexture(passData.inputTexture, AccessFlags.Read);
                } else {
                    destination = resourcesData.cameraColor;
                    passData.inputTexture = TextureHandle.nullHandle;
                }


                // Set the render graph to render to the temporary texture.
                builder.SetRenderAttachment(destination, 0, AccessFlags.Write);

                // Bind the depth-stencil buffer.
                // This is a demonstration. The code isn't used in the example.
                if (kBindDepthStencilAttachment)
                    builder.SetRenderAttachmentDepth(resourcesData.activeDepthTexture, AccessFlags.Write);

                // Set the render method.
                builder.SetRenderFunc((MainPassData data, RasterGraphContext context) =>
                    ExecuteMainPass(data, context));

                // Set cameraColor to the new temporary texture so the next render pass can use it. You don't need to blit to and from cameraColor if you use the render graph system.
                if (kSampleActiveColor) {
                    resourcesData.cameraColor = destination;
                }
            }
        }

        #endregion*/
    }
}
}
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Catnip.Settings {
// Defines a custom Volume Override component that controls the intensity of the URP Post-processing effect on a Scriptable Renderer Feature.
// For more information about the VolumeComponent API, refer to https://docs.unity3d.com/Packages/com.unity.render-pipelines.core@17.2/api/UnityEngine.Rendering.VolumeComponent.html

// Add the Volume Override to the list of available Volume Override components in the Volume Profile.
[VolumeComponentMenu("Post-processing Custom/Ben Day Bloom")]

// If the related Scriptable Renderer Feature doesn't exist, display a warning about adding it to the renderer.
[VolumeRequiresRendererFeatures(typeof(CustomPostProcessEffectRendererFeature))] // todo remove?

// Make the Volume Override active in the Universal Render Pipeline.
[SupportedOnRenderPipeline(typeof(UniversalRenderPipelineAsset))] // todo or UniversalRenderPipeline?

// Create the Volume Override by inheriting from VolumeComponent
public sealed class BenDayBloomPostProcessEffectVolumeComponent : VolumeComponent, IPostProcessComponent {
    // Set the name of the volume component in the list in the Volume Profile.
    public BenDayBloomPostProcessEffectVolumeComponent() {
        displayName = "Ben Day Bloom";
    }

    [Header("Bloom Settings")] //
    [Tooltip("Enter the description for the property that is shown when hovered")]
    public FloatParameter threshold = new FloatParameter(0.9f, true);

    // Create a property to control the intesity of the effect, with a tooltip description.
    // You can set the default value in the project-wide Graphics settings window. For more information, refer to https://docs.unity3d.com/Manual/urp/urp-global-settings.html
    // You can override the value in a local or global volume. For more information, refer to https://docs.unity3d.com/Manual/urp/volumes-landing-page.html
    // To access the value in a script, refer to the VolumeManager API: https://docs.unity3d.com/Packages/com.unity.render-pipelines.core@latest/index.html?subfolder=/api/UnityEngine.Rendering.VolumeManager.html 
    [Tooltip("Enter the description for the property that is shown when hovered")]
    public ClampedFloatParameter intensity = new ClampedFloatParameter(1f, 0f, 100f, true);

    [Tooltip("Enter the description for the property that is shown when hovered")]
    public FloatParameter scatter = new ClampedFloatParameter(0.7f, 0f, 1f, true);

    [Tooltip("Enter the description for the property that is shown when hovered")]
    public IntParameter clamp = new IntParameter(65472, true);

    [Tooltip("Enter the description for the property that is shown when hovered")]
    public ClampedIntParameter maxIterations = new ClampedIntParameter(6, 0, 10);

    [Tooltip("Enter the description for the property that is shown when hovered")]
    public NoInterpColorParameter tint = new NoInterpColorParameter(Color.white);

    [Header("Ben Day")] //
    public ClampedFloatParameter pulseSpeed = new ClampedFloatParameter(0f, 0f, 5f);
    public ClampedFloatParameter pulseIntensity = new ClampedFloatParameter(0.1f, 0f, 20f);
    [Tooltip("Softness of dot edges")]
    public ClampedFloatParameter dotsBlur = new ClampedFloatParameter(0.1f, 0f, 0.5f);
    
    [Tooltip("Enter the description for the property that is shown when hovered")]
    public IntParameter dotsDensity = new IntParameter(10, true);

    [Tooltip("Enter the description for the property that is shown when hovered")]
    public ClampedFloatParameter dotsCutoff = new ClampedFloatParameter(0.4f, 0, 1, true);

    [Tooltip("Enter the description for the property that is shown when hovered")]
    public Vector2Parameter scrollDirection = new Vector2Parameter(new Vector2());

    // Optional: Implement the IsActive() method of the IPostProcessComponent interface, and get the intensity value.
    public bool IsActive() {
        return intensity.GetValue<float>() > 0.0f;
    }

    public bool IsTileCompatible() {
        return true;
    }
}
}
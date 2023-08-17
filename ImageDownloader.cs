
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDK3.Image;
using VRC.SDK3.StringLoading;
using VRC.SDKBase;
using VRC.Udon.Common.Interfaces;

[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class ImageDownloader : UdonSharpBehaviour
{
    [SerializeField, Tooltip("URLs of images to load")]
    private VRCUrl[] imageUrls;

    [SerializeField, Tooltip("Renderer to show downloaded images on.")]
    private new Renderer renderer;

    [SerializeField, Tooltip("Duration in seconds until the next image is shown.")]
    private float slideDurationSeconds = 10f;

    private int _loadedIndex = -1;
    private VRCImageDownloader _imageDownloader;
    private IUdonEventReceiver _udonEventReceiver;
    private Texture2D[] _downloadedTextures;

    private void Start()
    {
        // Downloaded textures will be cached in a texture array.
        _downloadedTextures = new Texture2D[imageUrls.Length];

        // It's important to store the VRCImageDownloader as a variable, to stop it from being garbage collected!
        _imageDownloader = new VRCImageDownloader();

        // To receive Image and String loading events, 'this' is casted to the type needed
        _udonEventReceiver = (IUdonEventReceiver)this;

        // Load the next image. Then do it again, and again, and...
        LoadNextRecursive();
    }
    public void LoadNextRecursive()
    {
        _loadedIndex += 1;
        if (_loadedIndex >= _downloadedTextures.Length)
        {
            _loadedIndex = 0;
        }
        LoadNext();
        SendCustomEventDelayedSeconds(nameof(LoadNextRecursive), slideDurationSeconds);
    }
    private void LoadNext()
    {
        // All clients share the same server time. That's used to sync the currently displayed image.
        var nextTexture = _downloadedTextures[_loadedIndex];
        if (nextTexture != null)
        {
            // Image already downloaded! No need to download it again.
            renderer.sharedMaterial.mainTexture = nextTexture;
        }
        else
        {
            var rgbInfo = new TextureInfo();
            rgbInfo.GenerateMipMaps = true;
            _imageDownloader.DownloadImage(imageUrls[_loadedIndex], renderer.material, _udonEventReceiver, rgbInfo);
        }
    }

    public override void OnImageLoadSuccess(IVRCImageDownload result)
    {
        Debug.Log($"Image loaded: {result.SizeInMemoryBytes} bytes.");

        _downloadedTextures[_loadedIndex] = result.Result;
    }

    public override void OnImageLoadError(IVRCImageDownload result)
    {
        Debug.Log($"Image not loaded: {result.Error.ToString()}: {result.ErrorMessage}.");
    }

    private void OnDestroy()
    {
        _imageDownloader.Dispose();
    }
}

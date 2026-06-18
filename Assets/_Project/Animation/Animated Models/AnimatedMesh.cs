using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
public class AnimatedMesh : MonoBehaviour
{
    [SerializeField] private AnimatedMeshScriptableObject[] animationAssets;

    public Action OnAnimationFinished;

    private MeshFilter _meshFilter;

    // Flat lookup built once in Awake — no Dictionary allocations in Update.
    private struct ClipEntry
    {
        public List<Mesh> Frames;
        public float      FPS;
    }
    private Dictionary<string, ClipEntry> _clipLookup;

    // Playback state
    private List<Mesh> _currentFrames;
    private float      _currentFPS;
    private int        _currentFrame;
    private float      _timer;
    private float      _frameInterval;   // 1f / FPS — computed once per PlayAnimation call
    private float      _speedMultiplier; // multiplied into deltaTime, never causes a division
    private bool       _isLooping;
    private bool       _isPlaying;

    private void Awake()
    {
        _meshFilter = GetComponent<MeshFilter>();
        BuildLookup();
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>Switches to the named animation and restarts from frame 0.</summary>
    public void PlayAnimation(string animationName, bool isLooping, float speedMultiplier = 1f)
    {
        if (!_clipLookup.TryGetValue(animationName, out ClipEntry entry))
        {
            Debug.LogWarning($"[AnimatedMesh] Clip '{animationName}' not found.");
            return;
        }

        _currentFrames   = entry.Frames;
        _currentFPS      = entry.FPS;
        _currentFrame    = 0;
        _isLooping       = isLooping;
        _isPlaying       = true;
        _timer           = 0f;
        _frameInterval   = 1f / _currentFPS;                       // single division, never repeated
        _speedMultiplier = Mathf.Max(speedMultiplier, 0.01f);

        _meshFilter.sharedMesh = _currentFrames[0];
    }

    /// <summary>
    /// Updates playback speed without restarting the animation.
    /// Use to sync walk animation speed to NavMeshAgent velocity.
    /// </summary>
    public void SetSpeedMultiplier(float speedMultiplier)
    {
        _speedMultiplier = Mathf.Max(speedMultiplier, 0.01f);
    }

    // ── Update ────────────────────────────────────────────────────────────────

    private void Update()
    {
        if (!_isPlaying || _currentFrames == null) return;

        _timer += Time.deltaTime * _speedMultiplier;
        if (_timer < _frameInterval) return;

        _timer = Mathf.Min(_timer - _frameInterval, _frameInterval);

        _currentFrame++;

        if (_currentFrame >= _currentFrames.Count)
        {
            if (_isLooping)
            {
                _currentFrame = 0;
            }
            else
            {
                _currentFrame = _currentFrames.Count - 1;
                _isPlaying    = false;
                _meshFilter.sharedMesh = _currentFrames[_currentFrame];
                OnAnimationFinished?.Invoke();
                return;
            }
        }

        _meshFilter.sharedMesh = _currentFrames[_currentFrame];
    }

    // ── Initialisation ────────────────────────────────────────────────────────

    private void BuildLookup()
    {
        _clipLookup = new Dictionary<string, ClipEntry>();

        for (int i = 0; i < animationAssets.Length; i++)
        {
            AnimatedMeshScriptableObject so = animationAssets[i];
            if (so == null) continue;

            for (int j = 0; j < so.Animations.Count; j++)
            {
                AnimatedMeshScriptableObject.Animation anim = so.Animations[j];
                _clipLookup[anim.Name] = new ClipEntry
                {
                    Frames = anim.Meshes,
                    FPS    = so.AnimationFPS
                };
            }
        }
    }
}

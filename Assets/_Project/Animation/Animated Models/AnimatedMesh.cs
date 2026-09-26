using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
public class AnimatedMesh : MonoBehaviour
{
    [Tooltip("Default animation set, used when no set is supplied at runtime.")]
    [SerializeField] private AnimatedMeshScriptableObject[] animationAssets;

    public Action OnAnimationFinished;

    private MeshFilter _meshFilter;
    private AnimatedMeshScriptableObject[] _activeSet;

    // Playback state
    private List<Mesh> _currentFrames;
    private int        _currentFrame;
    private float      _timer;
    private float      _frameInterval;   // 1f / FPS — computed once per PlayAnimation call
    private float      _speedMultiplier; // multiplied into deltaTime, never causes a division
    private bool       _isLooping;
    private bool       _isPlaying;

    private void Awake()
    {
        _meshFilter = GetComponent<MeshFilter>();
        _activeSet  = animationAssets;
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>
    /// Swaps the animation set and stops playback. Passing null or an empty
    /// array restores the default set assigned in the Inspector.
    /// </summary>
    public void SetAnimationSet(AnimatedMeshScriptableObject[] set)
    {
        _activeSet     = set != null && set.Length > 0 ? set : animationAssets;
        _isPlaying     = false;
        _currentFrames = null;
    }

    /// <summary>Switches to the named animation and restarts from frame 0.</summary>
    public void PlayAnimation(string animationName, bool isLooping, float speedMultiplier = 1f)
    {
        if (!TryFindClip(animationName, out List<Mesh> frames, out int fps))
        {
            Debug.LogWarning($"[AnimatedMesh] Clip '{animationName}' not found.", this);
            return;
        }

        _currentFrames   = frames;
        _currentFrame    = 0;
        _isLooping       = isLooping;
        _isPlaying       = true;
        _timer           = 0f;
        _frameInterval   = 1f / fps;                               // single division, never repeated
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

    // ── Private ───────────────────────────────────────────────────────────────

    private bool TryFindClip(string animationName, out List<Mesh> frames, out int fps)
    {
        frames = null;
        fps    = 0;
        if (_activeSet == null) return false;

        for (int i = 0; i < _activeSet.Length; i++)
        {
            AnimatedMeshScriptableObject so = _activeSet[i];
            if (so == null || !so.TryGetClip(animationName, out frames)) continue;
            if (frames == null || frames.Count == 0) continue;

            fps = Mathf.Max(so.AnimationFPS, 1);
            return true;
        }

        frames = null;
        return false;
    }
}

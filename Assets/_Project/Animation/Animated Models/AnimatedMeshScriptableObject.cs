using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "BulletHeaven/Mesh Animation", fileName = "NewMeshAnimation")]
public class AnimatedMeshScriptableObject : ScriptableObject
{
    public int AnimationFPS;
    public List<Animation> Animations = new();

    [Serializable]
    public struct Animation
    {
        public string Name;
        public List<Mesh> Meshes;
    }

    // Built lazily and shared by every AnimatedMesh that uses this asset.
    [NonSerialized] private Dictionary<string, List<Mesh>> _lookup;

    public bool TryGetClip(string animationName, out List<Mesh> frames)
    {
        if (_lookup == null)
            BuildLookup();

        return _lookup.TryGetValue(animationName, out frames);
    }

    private void BuildLookup()
    {
        _lookup = new Dictionary<string, List<Mesh>>(Animations.Count);
        for (int i = 0; i < Animations.Count; i++)
            _lookup[Animations[i].Name] = Animations[i].Meshes;
    }

    private void OnValidate() => _lookup = null;
}

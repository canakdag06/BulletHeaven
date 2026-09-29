using System;
using System.Collections.Generic;
using UnityEngine;

public enum MeshAnimRole
{
    Walk = 0,
    Attack = 1,
    Death = 2,
}

[CreateAssetMenu(menuName = "BulletHeaven/Mesh Animation", fileName = "NewMeshAnimation")]
public class AnimatedMeshScriptableObject : ScriptableObject
{
    public int AnimationFPS;
    public List<Animation> Animations = new();

    [Serializable]
    public struct Animation
    {
        public string Name;
        public MeshAnimRole Role;
        public List<Mesh> Meshes;
    }

    // Built lazily and shared by every AnimatedMesh that uses this asset.
    [NonSerialized] private Dictionary<MeshAnimRole, List<Mesh>> _lookup;

    public bool TryGetClip(MeshAnimRole role, out List<Mesh> frames)
    {
        if (_lookup == null)
            BuildLookup();

        return _lookup.TryGetValue(role, out frames);
    }

    private void BuildLookup()
    {
        _lookup = new Dictionary<MeshAnimRole, List<Mesh>>(Animations.Count);
        for (int i = 0; i < Animations.Count; i++)
        {
            Animation clip = Animations[i];
            if (!_lookup.ContainsKey(clip.Role))
                _lookup[clip.Role] = clip.Meshes;
        }
    }

    private void OnValidate() => _lookup = null;
}

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
}

using Assets.PixelFantasy.Common.Scripts.CollectionScripts;
using UnityEngine;

namespace Assets.PixelFantasy.Common.Scripts.CharacterScripts
{
    public abstract class  CharacterBuilderBase : MonoBehaviour
    {
        public SpriteCollection SpriteCollection;
        public string Head = "Human";
        public string Ears = "Human";
        public string Eyes = "Human";
        public string Body = "Human";
        public string Hair;
        public string Armor;
        public string Helmet;
        public string Weapon;
        public string Firearm;
        public string Shield;
        public string Cape;
        public string Back;
        public string Mask;
        public string Horns;

        public Texture2D Texture { get; protected set; }
        
        public void Awake()
        {
            Rebuild();
        }

        public abstract void Rebuild(bool forceMerge = false);
    }
}
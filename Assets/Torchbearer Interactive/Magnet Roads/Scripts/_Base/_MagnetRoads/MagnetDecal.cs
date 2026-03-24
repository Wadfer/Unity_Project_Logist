// Copyright (C) Torchbearer Interactive, Ltd. - All Rights Reserved

// Handles Decal placment and managment of data for saving and loading

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace MagnetRoads
{
    [Serializable]
    [ExecuteInEditMode]
    [RequireComponent(typeof(SpriteRenderer))]
    public class MagnetDecal : MonoBehaviour
    {
        [HideInInspector]
        public string roadID;
        [HideInInspector]
        public float locationOnRoad;
        [HideInInspector]
        public Vector3 possitionOffset = new Vector3(0.0f, 0.01f, 0.0f);
        [HideInInspector]
        public Vector3 rotationOffset;
        [SerializeField] [HideInInspector]
        private Sprite decal;
        private SpriteRenderer spriteRenderer;
        public Sprite Decal { get { return decal; } set { SetSprite(value); } }
        //Fixes error with GUI interaction


        private void SetSprite(Sprite sprite)
        {
            if (!spriteRenderer)
            {
                try
                {
                    spriteRenderer = GetComponent<SpriteRenderer>();
                }
                catch (NullReferenceException)
                {
                    spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
                    Debug.LogWarning("Magnet Decal was missing a Sprite Render, One was added automaticly.");
                }
            }

            spriteRenderer.sprite = sprite;
            decal = sprite;
        }


    }
}
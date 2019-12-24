using System;
using Unity.UIWidgets.Movin;
using UnityEngine;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace Unity.UIWidgets.Movin {
    public struct MotionProps {
        public int key; // Current keyframe
        public int keys; // Total keyframes
        public float startFrame; // Frame current animation started
        public float endFrame; // Frame current animation ends
        public float percent; // Percentage to reach next key
        public bool completed; // Animation complete

        public Vector2 currentOutTangent; // Current keyframe out tangent
        public Vector2 nextInTangent; // Next keyframe in tangent

        public Vector3 startValue;
        public Vector3 endValue;
    }

    public class Movin {
        public GameObject container;
        public Transform transform;

        public BodymovinContent content;
        private MovinLayer[] layers;
        private MovinLayer[] layersByIndex;

        public float scale;
        public float frameRate = 0;
        public float totalFrames = 0;
        public float frame = 0; // Animation frame

        public float strokeWidth;
        public int sort;


        /* ---- BLENDING ---- */

        public bool blending = false;
        public BodymovinContent blendContent;
        public string blendPath;

        /* ---- EVENTS ---- */

        public Action OnComplete;


        public Movin(Transform parent, string path, int sort = 0, float scale = 1f, float strokeWidth = 0.5f, bool loop = true) {
            transform.SetParent(parent, false);

            container = new GameObject();
            container.transform.SetParent(transform, false);

            MovinInit(path, sort, scale, strokeWidth);
        }


        private void MovinInit(string path, int sort = 0, float scale = 1f, float strokeWidth = 0.5f) {
            scale *= 0.1f; // Reduce default scale

            container.name = "container - " + path;

            this.sort = sort;
            this.scale = scale;
            this.strokeWidth = strokeWidth;

            content = BodymovinContent.init(path);

            if (content.layers == null || content.layers.Length <= 0) {
                Debug.Log(">>>>  NO CONTENT LAYERS, ABORT!  <<<<");
                return;
            }

            container.transform.localScale = Vector3.one * this.scale;
            container.transform.localPosition -= new Vector3(content.w / 2, -(content.h / 2), 0) * scale;

            frameRate = content.fr;
            totalFrames = content.op;
            layers = new MovinLayer[content.layers.Length];


            /* ----- CREATE LAYERS ----- */

            layersByIndex = new MovinLayer[content.highestLayerIndex + 1];

            for (int i = 0; i < content.layers.Length; i++) {
                MovinLayer layer = new MovinLayer(this, content.layers[i], content.layers.Length - i);

                layers[i] = layer;
                layersByIndex[layer.content.ind] = layers[i];
            }


            /* ----- SET PARENTS ----- */

            for (int i = 0; i < layers.Length; i++) {
                MovinLayer layer = layers[i];
                int p = layer.content.parent;
                if (p <= 0) continue;

                layer.transform.SetParent(
                    layersByIndex[p].content.shapes.Length > 0
                        ? layersByIndex[p].transform.GetChild(0)
                        : layersByIndex[p].transform, false);
            }
        }


        private void Update(float time) {
            frame = time * frameRate;

            //Debug.Log("t:  " + time);

            if (frame >= totalFrames) {
                return;
            }

            UpdateLayers();
        }

        public void UpdateLayers() {
            for (int i = 0; i < layers.Length; i++) {
                float f = frame - layers[i].content.startTime;
                layers[i].Update(f);
            }
        }


        private void ResetKeyframes() {
            frame = 0;
            for (int i = 0; i < layers.Length; i++) layers[i].ResetKeyframes();
        }


        /* ------ PUBLIC METHODS ------ */


        public void SetColor(Color c, bool fill = true, bool stroke = false) {
            for (int i = 0; i < layers.Length; i++)
            for (int j = 0; j < layers[i].shapes.Length; j++) {
                MovinShape s = layers[i].shapes[j];

                if (fill)
                    s.UpdateFillColor(c);

                if (stroke)
                    s.UpdateStrokeColor(c);
            }
        }

        public Transform FindLayer(string n) {
            for (int i = 0; i < layers.Length; i++)
                if (n == layers[i].content.nm)
                    return layers[i].transform;
            return null;
        }
        
    }
}

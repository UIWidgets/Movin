﻿using UnityEngine;

namespace Unity.UIWidgets.Movin {
    public class MovinLayer {
        public Vector3 positionOffset = Vector3.zero;

        public int sort;
        public Movin movin;
        public BodymovinLayer content;
        public MovinShape[] shapes;
        public Transform transform;

        public MotionProps mpos;
        public MotionProps mscale;
        public MotionProps mrotx;
        public MotionProps mroty;
        public MotionProps mrotz;
        public MotionProps mopacity;
        public MotionProps manchor;

        public Vector3 finalRotation = Vector3.zero;

        public bool positionAnimated = false;
        public bool scaleAnimated = false;
        public bool rotationXAnimated = false;
        public bool rotationYAnimated = false;
        public bool rotationZAnimated = false;
        public bool opacityAnimated = false;
        public bool anchorAnimated = false;

        public float currentOpacity;
        public Vector3 currentAnchor;

        public MovinLayer(Movin movin, BodymovinLayer layer, int sort = 0) {
            this.movin = movin;
            content = layer;
            this.sort = sort;

            transform.SetParent(movin.container.transform, false);

            positionOffset = content.positionOffset;

            transform.localPosition = content.position + positionOffset;
            transform.localRotation = content.rotation;
            transform.localScale = content.scale;

            finalRotation = content.rotationEuler;


            /* ANIM SETUP */

            MotionSetup(ref positionAnimated, ref mpos, content.positionSets);
            MotionSetup(ref anchorAnimated, ref manchor, content.anchorSets);
            MotionSetup(ref scaleAnimated, ref mscale, content.scaleSets);
            MotionSetup(ref rotationXAnimated, ref mrotx, content.rotationXSets);
            MotionSetup(ref rotationYAnimated, ref mroty, content.rotationYSets);
            MotionSetup(ref rotationZAnimated, ref mrotz, content.rotationZSets);
            MotionSetup(ref opacityAnimated, ref mopacity, content.opacitySets);

            currentAnchor = content.anchorPoint;
            currentOpacity = content.opacity;


            /* SHAPES */

            shapes = new MovinShape[content.shapes.Length];

            int j = 0;
            for (int i = content.shapes.Length - 1; i >= 0; i--) {
                MovinShape shape = new MovinShape(this, content.shapes[i]);
                shape.UpdateOpacity(content.opacity);
                shapes[i] = shape;

                //shape.transform.localPosition += new Vector3(0, 0, -32 * j);
                j += 1;
            }
        }


        public void MotionSetup(ref bool b, ref MotionProps prop, BodymovinAnimatedProperties[] set) {
            b = set.Length > 0;
            if (b) {
                prop = new MotionProps {
                    keys = set.Length
                };

                SetKeyframe(ref prop, set, 0);
            }
        }


        public void SetKeyframe(ref MotionProps prop, BodymovinAnimatedProperties[] set, int k = 0) {
            prop.completed = false;
            if (prop.keys <= 0) return;
            if (k >= prop.keys) k = 0;

            prop.key = k;
            prop.startFrame = set[k].t;
            prop.endFrame = set.Length > k + 1 ? set[k + 1].t : prop.startFrame;
            prop.currentOutTangent = set[k].o;
            prop.nextInTangent = set[k].i;

            bool v3 = (set == content.positionSets || set == content.scaleSets || set == content.anchorSets);

            prop.startValue = v3 ? set[k].s : new Vector3(set[k].sf, 0, 0);
            prop.endValue = v3 ? set[k].e : new Vector3(set[k].ef, 0, 0);
        }


        public void Update(float frame) {
            if (frame >= content.outFrame || frame < content.inFrame)
                // ShapesActive(false);
                return;


            /* ----- SEND DOWN UPDATES ----- */

            for (int i = 0; i < shapes.Length; i++) shapes[i].Update(frame);


            /* ----- ANIM PROPS ----- */

            if (opacityAnimated && !mopacity.completed) UpdateProperty(frame, ref mopacity, content.opacitySets);
            if (positionAnimated && !mpos.completed) UpdateProperty(frame, ref mpos, content.positionSets);
            if (anchorAnimated && !manchor.completed) UpdateProperty(frame, ref manchor, content.anchorSets);
            if (scaleAnimated && !mscale.completed) UpdateProperty(frame, ref mscale, content.scaleSets);
            if (rotationXAnimated && !mrotx.completed) UpdateProperty(frame, ref mrotx, content.rotationXSets);
            if (rotationYAnimated && !mroty.completed) UpdateProperty(frame, ref mroty, content.rotationYSets);
            if (rotationZAnimated && !mrotz.completed) UpdateProperty(frame, ref mrotz, content.rotationZSets);

            if (rotationXAnimated || rotationYAnimated || rotationZAnimated)
                transform.localRotation = Quaternion.Euler(finalRotation);
        }


        public void UpdateProperty(float frame, ref MotionProps m, BodymovinAnimatedProperties[] set) {
            /* ----- CHECK FOR COMPLETE ----- */

            if (m.keys <= 0) {
                //Debug.Log(">>> NO PROP KEYS TO ANIMATE!");
                m.completed = true;
                return;
            }

            if (frame >= m.endFrame) {
                if (m.key + 1 == set.Length - 1) {
                    m.completed = true;
                    //Debug.Log("****** Prop Animation done! ******");
                    return;
                }

                while (frame >= m.endFrame) {
                    // Debug.Log("fr > end, eq:  " + frame + " - " + m.startFrame + " / (" + m.endFrame + " - " + m.startFrame + ")   keyframe:  " + m.key );
                    SetKeyframe(ref m, set, m.key + 1);
                    if (m.key == 0) break;
                }
            }


            /* ----- PERCENT KEYFRAME COMPLETE ----- */

            m.percent = (frame - m.startFrame) / (m.endFrame - m.startFrame);


            /* ----- CUBIC BEZIER EASE ----- */

            float ease = Ease.CubicBezier(Vector2.zero, m.currentOutTangent, m.nextInTangent, Vector2.one, m.percent);


            /* ----- UPDATE PROPERTY ----- */

            if (set == content.positionSets) {
                transform.localPosition = Value3(m, ease) + positionOffset;
            }
            else if (set == content.anchorSets) {
                Vector3 v = Value3(m, ease);
                currentAnchor = v;

                for (int i = 0; i < shapes.Length; i++) shapes[i].UpdateAnchor(v);
            }
            else if (set == content.scaleSets) {
                transform.localScale = Value3(m, ease);
            }
            else if (set == content.rotationXSets) {
                finalRotation.x = Value1(m, ease);
            }
            else if (set == content.rotationYSets) {
                finalRotation.y = Value1(m, ease);
            }
            else if (set == content.rotationZSets) {
                finalRotation.z = Value1(m, ease);
            }
            else if (set == content.opacitySets) {
                float v = Value1(m, ease);
                currentOpacity = v;

                for (int i = 0; i < shapes.Length; i++) shapes[i].UpdateOpacity(v);
            }
        }


        public Vector3 Value3(MotionProps m, float ease) {
            return (m.percent < 0 || m.percent > 1)
                ? m.startValue
                : m.startValue + ((m.endValue - m.startValue) * ease);
        }

        public float Value1(MotionProps m, float ease) {
            return (m.percent < 0 || m.percent > 1)
                ? m.startValue.x
                : m.startValue.x + ((m.endValue.x - m.startValue.x) * ease);
        }


        public void ResetKeyframes() {
            if (positionAnimated) SetKeyframe(ref mpos, content.positionSets, 0);
            if (anchorAnimated) SetKeyframe(ref manchor, content.anchorSets, 0);
            if (scaleAnimated) SetKeyframe(ref mscale, content.scaleSets, 0);
            if (rotationXAnimated) SetKeyframe(ref mrotx, content.rotationXSets, 0);
            if (rotationYAnimated) SetKeyframe(ref mroty, content.rotationYSets, 0);
            if (rotationZAnimated) SetKeyframe(ref mrotz, content.rotationZSets, 0);
            if (opacityAnimated) SetKeyframe(ref mopacity, content.opacitySets, 0);

            for (int i = 0; i < shapes.Length; i++) shapes[i].ResetKeyframes();
        }


        /* ---- BLENDING ---- */


        public void CreateBlendKeyframe(BodymovinLayer blendLayer, float duration, Vector2[] ease) {
            positionAnimated = true;
            CreateKeyframe(ref mpos, 0, duration, ease, transform.localPosition, blendLayer.position + positionOffset);

            anchorAnimated = true;
            CreateKeyframe(ref manchor, 0, duration, ease, currentAnchor, blendLayer.anchorPoint);

            scaleAnimated = true;
            CreateKeyframe(ref mscale, 0, duration, ease, transform.localScale, blendLayer.scale);

            rotationXAnimated = true;
            CreateKeyframe(ref mrotx, 0, duration, ease, new Vector3(finalRotation.x, 0, 0),
                new Vector3(blendLayer.rotationEuler.x, 0, 0));

            rotationYAnimated = true;
            CreateKeyframe(ref mroty, 0, duration, ease, new Vector3(finalRotation.y, 0, 0),
                new Vector3(blendLayer.rotationEuler.y, 0, 0));

            rotationZAnimated = true;
            CreateKeyframe(ref mrotz, 0, duration, ease, new Vector3(finalRotation.z, 0, 0),
                new Vector3(blendLayer.rotationEuler.z, 0, 0));

            opacityAnimated = true;
            CreateKeyframe(ref mopacity, 0, duration, ease, new Vector3(currentOpacity, 0, 0),
                new Vector3(blendLayer.opacity, 0, 0));

            for (int i = 0; i < shapes.Length; i++) shapes[i].CreateBlendKeyframe(blendLayer.shapes[i], duration, ease);
        }


        public void CreateKeyframe(ref MotionProps prop, float start, float end,
            Vector2[] ease, Vector3 startValue, Vector3 endValue, int k = 0) {
            prop.completed = false;
            prop.keys = 1;

            prop.key = k;
            prop.startFrame = start;
            prop.endFrame = end;
            prop.currentOutTangent = ease[0];
            prop.nextInTangent = ease[1];

            prop.startValue = startValue;
            prop.endValue = endValue;
        }


        public void UpdateLayersWithContent(BodymovinLayer l) {
            content = l;

            positionOffset = content.positionOffset;

            transform.localPosition = content.position + positionOffset;
            transform.localRotation = content.rotation;
            transform.localScale = content.scale;

            finalRotation = content.rotationEuler;

            MotionSetup(ref positionAnimated, ref mpos, content.positionSets);
            MotionSetup(ref anchorAnimated, ref manchor, content.anchorSets);
            MotionSetup(ref scaleAnimated, ref mscale, content.scaleSets);
            MotionSetup(ref rotationXAnimated, ref mrotx, content.rotationXSets);
            MotionSetup(ref rotationYAnimated, ref mroty, content.rotationYSets);
            MotionSetup(ref rotationZAnimated, ref mrotz, content.rotationZSets);
            MotionSetup(ref opacityAnimated, ref mopacity, content.opacitySets);

            for (int i = 0; i < shapes.Length; i++) shapes[i].UpdateLayersWithContent(l.shapes[i]);
        }
    }
}
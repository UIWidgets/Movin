using Unity.UIWidgets.ui;
using UnityEngine;
using UnityEngine.Rendering;

namespace Unity.UIWidgets.Movin {
    public class MovinShapeSlave : MovinShape {
        public MovinShape master;
        public BodymovinShapePath path;

        public MovinShapeSlave(MovinShape master, BodymovinShapePath path, float strokeWidth = 1f) {
            this.master = master;
            this.path = path;
            Transform parent = master.transform.parent;


            /* SHAPE PROPS */

            points = (BodyPoint[]) path.points.Clone();
            motionSet = path.animSets;
            closed = path.closed;


            /* ANIM SETUP */

            MotionSetup(ref animated, ref motion, motionSet);


            /* GAMEOBJECT */

            transform = new RectTransform();
            transform.SetParent(parent, false);
            transform.localPosition = master.transform.localPosition;


            /* SETUP VECTOR */

            fill = master.content.fillHidden || master.content.fillColor == null
                ? null
                : new SolidFill() {Color = master.fill.Color};
            stroke = master.content.strokeHidden || master.content.strokeColor == null
                ? null
                : new Stroke() {Color = master.stroke.Color, HalfThickness = master.content.strokeWidth * strokeWidth};
            props = new PathProperties() {Stroke = stroke};

            shape = new Shape() {
                Fill = fill,
                PathProps = props,
                FillTransform = Matrix3.I()
            };

            UpdateMesh();
        }
    }
}
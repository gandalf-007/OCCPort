﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;

namespace OCCPort
{
    public class V3d_View
    {
        public void Rotation(int X,
                          int Y)
        {
            if (rx == 0.0 || ry == 0.0)
            {
                StartRotation(X, Y);
                return;
            }
            double dx = 0.0, dy = 0.0, dz = 0.0;
            if (myZRotation)
            {
                dz = Math.Atan2((double)(X) - rx / 2.0, ry / 2.0 - (double)(Y)) -
                  Math.Atan2(sx - rx / 2.0, ry / 2.0 - sy);
            }
            else
            {
                dx = ((double)(X) - sx) * Math.PI / rx;
                dy = (sy - (double)(Y)) * Math.PI / ry;
            }

            Rotate(dx, dy, dz,
                    myRotateGravity.X(), myRotateGravity.Y(), myRotateGravity.Z(),
                    false);
        }

        public V3d_View()
        {
            //myView = theViewer->Driver()->CreateView(theViewer->StructureManager());
            myView = new Graphic3d_CView();
            //myView.SetBackground(theViewer->GetBackgroundColor());
            //  myView->SetGradientBackground(theViewer->GetGradientBackground());

            // ChangeRenderingParams() = theViewer->DefaultRenderingParams();

            // camera init
            var aCamera = new Graphic3d_Camera();
            /*aCamera.SetFOVy(45.0);
            aCamera.SetIOD(Graphic3d_Camera::IODType_Relative, 0.05);
            aCamera.SetZFocus(Graphic3d_Camera::FocusType_Relative, 1.0);
            aCamera.SetProjectionType((theType == V3d_ORTHOGRAPHIC)
              ? Graphic3d_Camera::Projection_Orthographic
              : Graphic3d_Camera::Projection_Perspective);*/

            myDefaultCamera = new Graphic3d_Camera();

            myImmediateUpdate = false;
            /*  SetAutoZFitMode(true, 1.0);
              SetBackFacingModel(V3d_TOBM_AUTOMATIC);*/
            SetCamera(aCamera);/*
            SetAxis(0., 0., 0., 1., 1., 1.);
            SetVisualization(theViewer->DefaultVisualization());
            SetTwist(0.);
            SetAt(0.0, 0.0, 0.0);
            SetProj(theViewer->DefaultViewProj());
            SetSize(theViewer->DefaultViewSize());
            Standard_Real zsize = theViewer->DefaultViewSize();
            SetZSize(2.* zsize);
            SetDepth(theViewer->DefaultViewSize() / 2.0);
            SetViewMappingDefault();
            SetViewOrientationDefault();
            theViewer->AddView(this);*/
            Init();
            myImmediateUpdate = true;
        }
        public void Pan(int theDXp,
                     int theDYp,
                     double theZoomFactor,
                     bool theToStart)
        {
            Panning(Convert(theDXp), Convert(theDYp), theZoomFactor, theToStart);
        }

        public void Panning(double theDXv,
                         double theDYv,
                         double theZoomFactor,
                         bool theToStart)
        {
            //Standard_ASSERT_RAISE(theZoomFactor > 0.0, "Bad zoom factor");

            var aCamera = Camera();

            if (theToStart)
            {
                myCamStartOpDir = aCamera.Direction();
                myCamStartOpEye = aCamera.Eye();
                myCamStartOpCenter = aCamera.Center();
            }

            bool wasUpdateEnabled = SetImmediateUpdate(false);

            var aViewDims = aCamera.ViewDimensions();

            aCamera.SetEyeAndCenter(myCamStartOpEye, myCamStartOpCenter);
            aCamera.SetDirectionFromEye(myCamStartOpDir);
            Translate(aCamera, -theDXv, -theDYv);
            Scale(aCamera, aViewDims.X() / theZoomFactor, aViewDims.Y() / theZoomFactor);

            SetImmediateUpdate(wasUpdateEnabled);

            ImmediateUpdate();
        }

        private void Scale(Graphic3d_Camera theCamera, double theSizeXv, double theSizeYv)
        {
            var anAspect = theCamera.Aspect();
            if (anAspect > 1.0)
            {
                theCamera.SetScale(Math.Max(theSizeXv / anAspect, theSizeYv));
            }
            else
            {
                theCamera.SetScale(Math.Max(theSizeXv, theSizeYv * anAspect));
            }
            Invalidate();

        }



        private void Translate(Graphic3d_Camera theCamera, double theDXv, double theDYv)
        {
            gp_Pnt aCenter = theCamera.Center();
            gp_Dir aDir = theCamera.Direction();
            gp_Dir anUp = theCamera.Up();
            gp_Ax3 aCameraCS = new gp_Ax3(aCenter, aDir.Reversed(), aDir ^ anUp);

            gp_Vec aCameraPanXv = new gp_Vec(aCameraCS.XDirection()) * theDXv;
            gp_Vec aCameraPanYv = new gp_Vec(aCameraCS.YDirection()) * theDYv;
            gp_Vec aCameraPan = aCameraPanXv + aCameraPanYv;
            gp_Trsf aPanTrsf = new gp_Trsf();
            aPanTrsf.SetTranslation(aCameraPan);

            theCamera.Transform(aPanTrsf);
            Invalidate();
        }

        private void Invalidate()
        {
            if (!myView.IsDefined())
            {
                return;
            }

            myView.Invalidate();
        }

        bool SetImmediateUpdate(bool theImmediateUpdate)
        {
            bool aPreviousMode = myImmediateUpdate;
            myImmediateUpdate = theImmediateUpdate;
            return aPreviousMode;
        }

        public void Zoom(int theXp1,
                      int theYp1,
                      int theXp2,
                      int theYp2)
        {
            int aDx = theXp2 - theXp1;
            int aDy = theYp2 - theYp1;
            if (aDx != 0 || aDy != 0)
            {
                double aCoeff = Math.Sqrt((double)(aDx * aDx + aDy * aDy)) / 100.0 + 1.0;
                aCoeff = (aDx > 0) ? aCoeff : 1.0 / aCoeff;
                SetZoom(aCoeff, true);
            }
        }

        private void SetZoom(double theCoef, bool theToStart)
        {
            //V3d_BadValue_Raise_if(theCoef <= 0., "V3d_View::SetZoom, bad coefficient");

            var aCamera = Camera();

            if (theToStart)
            {
                myCamStartOpEye = aCamera.Eye();
                myCamStartOpCenter = aCamera.Center();
            }

            var aViewWidth = aCamera.ViewDimensions().X();
            var aViewHeight = aCamera.ViewDimensions().Y();

            // ensure that zoom will not be too small or too big
            var aCoef = theCoef;
            if (aViewWidth < aCoef * Precision.Confusion())
            {
                aCoef = aViewWidth / Precision.Confusion();
            }
            else if (aViewWidth > aCoef * 1e12)
            {
                aCoef = aViewWidth / 1e12;
            }
            if (aViewHeight < aCoef * Precision.Confusion())
            {
                aCoef = aViewHeight / Precision.Confusion();
            }
            else if (aViewHeight > aCoef * 1e12)
            {
                aCoef = aViewHeight / 1e12;
            }

            aCamera.SetEye(myCamStartOpEye);
            aCamera.SetCenter(myCamStartOpCenter);
            aCamera.SetScale(aCamera.Scale() / aCoef);

            ImmediateUpdate();
        }

        private void SetCamera(Graphic3d_Camera aCamera)
        {
            _camera = aCamera;
        }

        public void Init()
        {
            myGravityReferencePoint = new Graphic3d_Vertex();
        }

        double myOldMouseX;
        double myOldMouseY;
        gp_Dir myCamStartOpUp;
        gp_Dir myCamStartOpDir;
        gp_Pnt myCamStartOpEye;
        gp_Pnt myCamStartOpCenter;
        Graphic3d_Camera myDefaultCamera;
        Graphic3d_CView myView;
        bool myImmediateUpdate;
        //mutable Standard_Boolean myIsInvalidatedImmediate;

        //! Returns camera object of the view.
        //! @return: handle to camera object, or NULL if 3D view does not use
        //! the camera approach.
        Graphic3d_Camera _camera;
        Graphic3d_Camera Camera()
        {
            return _camera;
        }

        gp_Vec myXscreenAxis;
        gp_Vec myYscreenAxis;
        gp_Vec myZscreenAxis;
        gp_Dir myViewAxis;
        Graphic3d_Vertex myGravityReferencePoint;
        bool myAutoZFitIsOn;
        double myAutoZFitScaleFactor;

        //  V3d_ListOfLight myActiveLights;
        //  gp_Dir myDefaultViewAxis;
        //gp_Pnt myDefaultViewPoint;
        AspectWindow MyWindow = new AspectWindow();
        int sx;
        int sy;
        double rx;
        double ry;
        gp_Pnt myRotateGravity;
        bool myComputedMode;
        bool SwitchSetFront;
        bool myZRotation;
        bool MyZoomAtPointX;
        bool MyZoomAtPointY;

        //! Converts the PIXEL value
        //! to a value in the projection plane.
        double Convert(double Vp)
        {
            int aDxw, aDyw;

            //V3d_UnMapped_Raise_if(!myView->IsDefined(), "view has no window");

            MyWindow.Size(out aDxw, out aDyw);
            double aValue;

            var aViewDims = Camera().ViewDimensions();
            aValue = aViewDims.X() * (float)Vp / (float)aDxw;

            return aValue;
        }
        double DEUXPI = (2.0 * Math.PI);
        //=============================================================================
        //function : Rotate
        //purpose  :
        //=============================================================================
        public void Rotate(double ax, double ay, double az,
                       double X, double Y, double Z, bool Start)
        {

            double Ax = ax;
            double Ay = ay;
            double Az = az;

            if (Ax > 0.0) while (Ax > DEUXPI) Ax -= DEUXPI;
            else if (Ax < 0.0) while (Ax < -DEUXPI) Ax += DEUXPI;
            if (Ay > 0.0) while (Ay > DEUXPI) Ay -= DEUXPI;
            else if (Ay < 0.0) while (Ay < -DEUXPI) Ay += DEUXPI;
            if (Az > 0.0) while (Az > DEUXPI) Az -= DEUXPI;
            else if (Az < 0.0) while (Az < -DEUXPI) Az += DEUXPI;

            var aCamera = Camera();

            if (Start)
            {
                myGravityReferencePoint.SetCoord(X, Y, Z);
                myCamStartOpUp = aCamera.Up();
                myCamStartOpDir = aCamera.Direction();
                myCamStartOpEye = aCamera.Eye();
                myCamStartOpCenter = aCamera.Center();
            }

            var aVref = myGravityReferencePoint;

            aCamera.SetUp(myCamStartOpUp);
            aCamera.SetEyeAndCenter(myCamStartOpEye, myCamStartOpCenter);
            aCamera.SetDirectionFromEye(myCamStartOpDir);

            // rotate camera around 3 initial axes
            gp_Pnt aRCenter = new gp_Pnt(aVref.X(), aVref.Y(), aVref.Z());

            gp_Dir aZAxis = new gp_Dir(aCamera.Direction().Reversed());
            gp_Dir aYAxis = new gp_Dir(aCamera.Up());
            gp_Dir aXAxis = new gp_Dir(aYAxis.Crossed(aZAxis));

            gp_Trsf[] aRot = new gp_Trsf[3];
            gp_Trsf aTrsf = new gp_Trsf();
            for (int i = 0; i < 3; i++)
            {
                aRot[i] = new gp_Trsf();
            }
            aRot[0].SetRotation(new gp_Ax1(aRCenter, aYAxis), -Ax);
            aRot[1].SetRotation(new gp_Ax1(aRCenter, aXAxis), Ay);
            aRot[2].SetRotation(new gp_Ax1(aRCenter, aZAxis), Az);
            aTrsf.Multiply(aRot[0]);
            aTrsf.Multiply(aRot[1]);
            aTrsf.Multiply(aRot[2]);

            aCamera.Transform(aTrsf);

            ImmediateUpdate();
        }

        /*private object gp_Ax1(gp_Pnt aRCenter, Func<aCamera.Up, (object, object), gpDir> aYAxis)
        {
            throw new NotImplementedException();
        }*/

        const int THE_NB_BOUND_POINTS = 8;
        //=======================================================================
        //function : GravityPoint
        //purpose  :
        //=======================================================================
        gp_Pnt GravityPoint()
        {
            Graphic3d_MapOfStructure[] aSetOfStructures;
            myView.DisplayedStructures(out aSetOfStructures);

            bool hasSelection = false;
            foreach (var aStructIter in aSetOfStructures)
            {
                if (aStructIter.Key().IsHighlighted()
                 && aStructIter.Key().IsVisible())
                {
                    hasSelection = true;
                    break;
                }
            }

            double Xmin, Ymin, Zmin, Xmax, Ymax, Zmax;
            int aNbPoints = 0;
            gp_XYZ aResult = new gp_XYZ(0.0, 0.0, 0.0);
            foreach (var aStructIter in aSetOfStructures)
            {
                var aStruct = aStructIter.Key();
                if (!aStruct.IsVisible()
                  || aStruct.IsInfinite()
                  || (hasSelection && !aStruct.IsHighlighted()))
                {
                    continue;
                }

                Graphic3d_BndBox3d aBox = aStruct.CStructure().BoundingBox();
                if (!aBox.IsValid())
                {
                    continue;
                }

                // skip transformation-persistent objects
                if (aStruct.TransformPersistence() != null)
                {
                    continue;
                }

                // use camera projection to find gravity point
                Xmin = aBox.CornerMin().x();
                Ymin = aBox.CornerMin().y();
                Zmin = aBox.CornerMin().z();
                Xmax = aBox.CornerMax().x();
                Ymax = aBox.CornerMax().y();
                Zmax = aBox.CornerMax().z();
                gp_Pnt[] aPnts = new gp_Pnt[THE_NB_BOUND_POINTS]
                {
             new gp_Pnt (Xmin, Ymin, Zmin),new  gp_Pnt (Xmin, Ymin, Zmax),
            new  gp_Pnt (Xmin, Ymax, Zmin), new gp_Pnt (Xmin, Ymax, Zmax),
             new gp_Pnt (Xmax, Ymin, Zmin), new gp_Pnt (Xmax, Ymin, Zmax),
             new gp_Pnt (Xmax, Ymax, Zmin), new gp_Pnt (Xmax, Ymax, Zmax)
    };

                for (int aPntIt = 0; aPntIt < THE_NB_BOUND_POINTS; ++aPntIt)
                {
                    gp_Pnt aBndPnt = aPnts[aPntIt];
                    gp_Pnt aProjected = Camera().Project(aBndPnt);
                    if (Math.Abs(aProjected.X()) <= 1.0
                     && Math.Abs(aProjected.Y()) <= 1.0)
                    {
                        aResult += aBndPnt.XYZ();
                        ++aNbPoints;
                    }
                }
            }

            if (aNbPoints == 0)
            {
                // fallback - just use bounding box of entire scene
                Bnd_Box aBox = myView.MinMaxValues();
                if (!aBox.IsVoid())
                {
                    aBox.Get(out Xmin, out Ymin, out Zmin,
                             out Xmax, out Ymax, out Zmax);
                    gp_Pnt[] aPnts = new gp_Pnt[THE_NB_BOUND_POINTS]
                    {
       new  gp_Pnt (Xmin, Ymin, Zmin), new gp_Pnt(Xmin, Ymin, Zmax),
       new  gp_Pnt (Xmin, Ymax, Zmin),new gp_Pnt (Xmin, Ymax, Zmax),
       new gp_Pnt (Xmax, Ymin, Zmin), new gp_Pnt(Xmax, Ymin, Zmax),
       new gp_Pnt (Xmax, Ymax, Zmin),new  gp_Pnt (Xmax, Ymax, Zmax)
      };

                    for (int aPntIt = 0; aPntIt < THE_NB_BOUND_POINTS; ++aPntIt)
                    {
                        gp_Pnt aBndPnt = aPnts[aPntIt];
                        aResult.Add(aBndPnt.XYZ());
                        ++aNbPoints;
                    }
                }
            }

            if (aNbPoints > 0)
            {
                aResult.Divide(aNbPoints);
            }

            return new gp_Pnt(aResult);
        }

        private void ImmediateUpdate()
        {

        }

        //=============================================================================
        //function : StartRotation
        //purpose  :
        //=============================================================================
        public void StartRotation(int X,
                             int Y,
                             double zRotationThreshold = 0)
        {
            sx = X; sy = Y;
            double x, y;
            Size(out x, out y);
            rx = Convert(x);
            ry = Convert(y);
            myRotateGravity = GravityPoint();
            Rotate(0.0, 0.0, 0.0,
                    myRotateGravity.X(), myRotateGravity.Y(), myRotateGravity.Z(),
                    true);
            myZRotation = false;
            if (zRotationThreshold > 0.0)
            {
                var dx = Math.Abs(sx - rx / 2.0);
                var dy = Math.Abs(sy - ry / 2.0);
                //  if( dx > rx/3. || dy > ry/3. ) myZRotation = Standard_True;
                var dd = zRotationThreshold * (rx + ry) / 2.0;
                if (dx > dd || dy > dd) myZRotation = true;
            }

        }

        private void Size(out double Width, out double Height)
        {
            var aViewDims = Camera().ViewDimensions();

            Width = aViewDims.X();
            Height = aViewDims.Y();
        }
    }

}

﻿using System;

namespace OCCPort.Tester
{
	public class BRepPrimAPI_MakeBox: BRepBuilderAPI_MakeShape
	{



		BRepPrim_Wedge myWedge;

		public gp_Pnt pmin(gp_Pnt p,
			double dx,
			double dy,
			double dz)
		{
			gp_Pnt P = p;
			if (dx < 0) P.SetX(P.X() + dx);
			if (dy < 0) P.SetY(P.Y() + dy);
			if (dz < 0) P.SetZ(P.Z() + dz);
			return P;
		}

		gp_Pnt pmin(gp_Pnt p1, gp_Pnt p2)
		{
			return new gp_Pnt(Math.Min(p1.X(), p2.X()), Math.Min(p1.Y(), p2.Y()), Math.Min(p1.Z(), p2.Z()));
		}


		public BRepPrimAPI_MakeBox(gp_Pnt P1, gp_Pnt P2)
		{
			myWedge = new BRepPrim_Wedge(new gp_Ax2(pmin(P1, P2), new gp_Dir(0, 0, 1), new gp_Dir(1, 0, 0)),
				Math.Abs(P2.X() - P1.X()),
				Math.Abs(P2.Y() - P1.Y()),
				Math.Abs(P2.Z() - P1.Z()));

		}

		public void Build()
		{
			Solid();
		}
		//=======================================================================
		//function : Solid
		//purpose  : 
		//=======================================================================

		public TopoDS_Solid Solid()
		{
			BRep_Builder B = new BRep_Builder();
			B.MakeSolid(TopoDS.Solid(myShape));
			B.Add(myShape, myWedge.Shell());
			Done();
			return TopoDS.Solid(myShape);
		}

		
	}
}
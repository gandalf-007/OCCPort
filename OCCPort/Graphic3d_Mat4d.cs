﻿using System;

namespace OCCPort
{
	internal class Graphic3d_Mat4d : NCollection_Mat4
	{


		//! Compute matrix multiplication product: A * B.
		//! @param theMatA [in] the matrix "A".
		//! @param theMatB [in] the matrix "B".
		static NCollection_Mat4 Multiply(NCollection_Mat4 theMatA,
									 NCollection_Mat4 theMatB)
		{
			NCollection_Mat4 aMatRes = new NCollection_Mat4();

			int aInputElem;



			for (int aResElem = 0; aResElem < 16; ++aResElem)

			{
				aMatRes.myMat[aResElem] = 0;
				for (aInputElem = 0; aInputElem < 4; ++aInputElem)
				{
					aMatRes.myMat[aResElem] += theMatA.GetValue(aResElem % 4, aInputElem)
											 * theMatB.GetValue(aInputElem, aResElem / 4);
				}
			}

			return aMatRes;
		}


		internal void Multiply(NCollection_Mat4 theMat)
		{

			var r = Multiply(this, theMat);
			Array.Copy(r.myMat, r.myMat, 16);

		}

		internal void Translate(NCollection_Vec3 theVec)
		{
			NCollection_Mat4 aTempMat = new NCollection_Mat4();
			aTempMat.SetColumn(3, theVec);
			Multiply(aTempMat);
		}
	}
}

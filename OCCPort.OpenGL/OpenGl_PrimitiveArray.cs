﻿using OpenTK.Graphics.OpenGL;
using System;
using System.Security.AccessControl;

namespace OCCPort.OpenGL
{
	public class OpenGl_PrimitiveArray : OpenGl_Element
	{
		public void InitBuffers(OpenGl_Context theContext,
										 Graphic3d_TypeOfPrimitiveArray theType,

										 Graphic3d_IndexBuffer theIndices,
										 Graphic3d_Buffer theAttribs,
										 Graphic3d_BoundBuffer theBounds)
		{
			// Release old graphic resources
			Release(theContext);

			myIndices = theIndices;
			myAttribs = theAttribs;
			myBounds = theBounds;
			if (theContext != null
			  && theContext.GraphicsLibrary() == Aspect_GraphicsLibrary.Aspect_GraphicsLibrary_OpenGLES)
			{
				processIndices(theContext);
			}

			setDrawMode(theType);
		}

		//=======================================================================
		public void setDrawMode(Graphic3d_TypeOfPrimitiveArray theType)
		{

			if (myAttribs == null)
			{
				myDrawMode = DRAW_MODE_NONE;
				myIsFillType = false;
				return;
			}

			switch (theType)
			{
				case Graphic3d_TypeOfPrimitiveArray.Graphic3d_TOPA_POINTS:
					myDrawMode = GLConstants.GL_POINTS;
					myIsFillType = false;
					break;
				case Graphic3d_TypeOfPrimitiveArray.Graphic3d_TOPA_SEGMENTS:
					myDrawMode = GLConstants.GL_LINES;
					myIsFillType = false;
					break;
				case Graphic3d_TypeOfPrimitiveArray.Graphic3d_TOPA_POLYLINES:
					myDrawMode = GLConstants.GL_LINE_STRIP;
					myIsFillType = false;
					break;
				case Graphic3d_TypeOfPrimitiveArray.Graphic3d_TOPA_TRIANGLES:
					myDrawMode = GLConstants.GL_TRIANGLES;
					myIsFillType = true;
					break;
				case Graphic3d_TypeOfPrimitiveArray.Graphic3d_TOPA_TRIANGLESTRIPS:
					myDrawMode = GLConstants.GL_TRIANGLE_STRIP;
					myIsFillType = true;
					break;
				case Graphic3d_TypeOfPrimitiveArray.Graphic3d_TOPA_TRIANGLEFANS:
					myDrawMode = GLConstants.GL_TRIANGLE_FAN;
					myIsFillType = true;
					break;

				case Graphic3d_TypeOfPrimitiveArray.Graphic3d_TOPA_LINES_ADJACENCY:
					myDrawMode = GLConstants.GL_LINES_ADJACENCY;
					myIsFillType = false;
					break;
				case Graphic3d_TypeOfPrimitiveArray.Graphic3d_TOPA_LINE_STRIP_ADJACENCY:
					myDrawMode = GLConstants.GL_LINE_STRIP_ADJACENCY;
					myIsFillType = false;
					break;
				case Graphic3d_TypeOfPrimitiveArray.Graphic3d_TOPA_TRIANGLES_ADJACENCY:
					myDrawMode = GLConstants.GL_TRIANGLES_ADJACENCY;
					myIsFillType = true;
					break;
				case Graphic3d_TypeOfPrimitiveArray.Graphic3d_TOPA_TRIANGLE_STRIP_ADJACENCY:
					myDrawMode = GLConstants.GL_TRIANGLE_STRIP_ADJACENCY;
					myIsFillType = true;
					break;


				case Graphic3d_TypeOfPrimitiveArray.Graphic3d_TOPA_QUADRANGLES:
					myDrawMode = GLConstants.GL_QUADS;
					myIsFillType = true;
					break;
				case Graphic3d_TypeOfPrimitiveArray.Graphic3d_TOPA_QUADRANGLESTRIPS:
					myDrawMode = GLConstants.GL_QUAD_STRIP;
					myIsFillType = true;
					break;
				case Graphic3d_TypeOfPrimitiveArray.Graphic3d_TOPA_POLYGONS:
					myDrawMode = GLConstants.GL_POLYGON;
					myIsFillType = true;
					break;
				case Graphic3d_TypeOfPrimitiveArray.Graphic3d_TOPA_UNDEFINED:
					myDrawMode = DRAW_MODE_NONE;
					myIsFillType = false;
					break;

			}
		}


		private void Release(OpenGl_Context theContext)
		{
			throw new NotImplementedException();
		}

		OpenGl_IndexBuffer myVboIndices;
		protected OpenGl_VertexBuffer myVboAttribs;

		protected Graphic3d_IndexBuffer myIndices;
		protected Graphic3d_Buffer myAttribs;
		Graphic3d_BoundBuffer myBounds;
		short myDrawMode;
		bool myIsFillType;
		bool myIsVboInit;

		int myUID; //!< Unique ID of primitive array. 

		public OpenGl_PrimitiveArray(OpenGl_GraphicDriver theDriver,
			Graphic3d_TypeOfPrimitiveArray theType,
			Graphic3d_IndexBuffer theIndices,
			Graphic3d_Buffer theAttribs,
			Graphic3d_BoundBuffer theBounds)
		{
			myIndices = (theIndices);
			myAttribs = (theAttribs);
			myBounds = (theBounds);
			//myDrawMode(DRAW_MODE_NONE),
			myIsFillType = (false);
			myIsVboInit = (false);

			if (myIndices != null && myIndices.NbElements < 1)
			{
				// dummy index buffer?
				myIndices = null;
			}

			if (theDriver != null)
			{
				myUID = theDriver.GetNextPrimitiveArrayUID();
				OpenGl_Context aCtx = theDriver.GetSharedContext();
				if (aCtx != null
				  && aCtx.GraphicsLibrary() == Aspect_GraphicsLibrary.Aspect_GraphicsLibrary_OpenGLES)
				{
					processIndices(aCtx);
				}
			}

			setDrawMode(theType);
		}

		//! OpenGL does not provide a constant for "none" draw mode.
		//! So we define our own one that does not conflict with GL constants and utilizes common GL invalid value.

		const int DRAW_MODE_NONE = -1;


		public override void Render(OpenGl_Workspace theWorkspace)
		{
			if (myDrawMode == DRAW_MODE_NONE)
			{
				return;
			}

			OpenGl_Aspects anAspectFace = theWorkspace.Aspects();
			OpenGl_Context aCtx = theWorkspace.GetGlContext();

			bool toDrawArray = true, toSetLinePolygMode = false;
			int toDrawInteriorEdges = 0; // 0 - no edges, 1 - glsl edges, 2 - polygonMode

			// create VBOs on first render call
			if (!myIsVboInit)
			{
				// compatibility - keep data to draw markers using display lists
				bool toKeepData = myDrawMode == (int)All.Points
										   && anAspectFace.IsDisplayListSprite(aCtx);
				if (aCtx.GraphicsLibrary() == Aspect_GraphicsLibrary.Aspect_GraphicsLibrary_OpenGLES)
				{
					processIndices(aCtx);
				}
				buildVBO(aCtx, toKeepData);
				myIsVboInit = true;
			}
			else if ((myAttribs != null
				   && myAttribs.IsMutable())
				  || (myIndices != null
				   && myIndices.IsMutable()))
			{
				updateVBO(aCtx);
			}

			Graphic3d_TypeOfShadingModel aShadingModel = Graphic3d_TypeOfShadingModel.Graphic3d_TypeOfShadingModel_Unlit;

			bool hasVertNorm = myVboAttribs != null && myVboAttribs.HasNormalAttribute();
			switch (myDrawMode)
			{
				default:
					{
						aShadingModel = aCtx.ShaderManager().ChooseFaceShadingModel(anAspectFace.ShadingModel(), hasVertNorm);
						//aCtx.ShaderManager().BindFaceProgram(aTextureSet,
						//                                        aShadingModel,
						//                                        aCtx.ShaderManager().MaterialState().HasAlphaCutoff() ? Graphic3d_AlphaMode_Mask : Graphic3d_AlphaMode_Opaque,
						//                                        toDrawInteriorEdges == 1 ? anAspectFace.Aspect().InteriorStyle() : Aspect_IS_SOLID,
						//                                        hasVertColor,
						//                                        toEnableEnvMap,
						//                                        toDrawInteriorEdges == 1,
						//                                        anAspectFace.ShaderProgramRes(aCtx));
						if (toDrawInteriorEdges == 1)
						{
							//aCtx.ShaderManager().PushInteriorState(aCtx.ActiveProgram(), anAspectFace.Aspect());
						}
						else if (toSetLinePolygMode)
						{
							aCtx.SetPolygonMode((int)PolygonMode.Line);
						}
						break;
					}
			}

		}

		private void processIndices(OpenGl_Context aCtx)
		{
			throw new NotImplementedException();
		}

		private void updateVBO(OpenGl_Context aCtx)
		{
			throw new NotImplementedException();
		}


		// =======================================================================
		// function : buildVBO
		// purpose  :
		// =======================================================================
		public bool buildVBO(OpenGl_Context theCtx,
												   bool theToKeepData)
		{
			bool isNormalMode = theCtx.ToUseVbo();
			clearMemoryGL(theCtx);
			if (myAttribs == null
			 || myAttribs.IsEmpty()
			 || myAttribs.NbElements < 1
			 || myAttribs.NbAttributes < 1
			 || myAttribs.NbAttributes > 10)
			{
				// vertices should be always defined - others are optional
				return false;
			}

			if (isNormalMode
			 && initNormalVbo(theCtx))
			{
				if (!theCtx.caps.keepArrayData
				 && !theToKeepData
				 && !myAttribs.IsMutable())
				{
					myIndices = null; ;
					myAttribs = null; ;
				}
				else
				{
					myAttribs.Validate();
				}
				return true;
			}

			OpenGl_VertexBufferCompat aVboAttribs = new OpenGl_VertexBufferCompat();
			switch (myAttribs.NbAttributes)
			{
				case 1: aVboAttribs = new OpenGl_VertexBufferT<OpenGl_VertexBufferCompat>(1, myAttribs); break;
				case 2: aVboAttribs = new OpenGl_VertexBufferT<OpenGl_VertexBufferCompat>(2, myAttribs); break;
				case 3: aVboAttribs = new OpenGl_VertexBufferT<OpenGl_VertexBufferCompat>(3, myAttribs); break;
				case 4: aVboAttribs = new OpenGl_VertexBufferT<OpenGl_VertexBufferCompat>(4, myAttribs); break;
				case 5: aVboAttribs = new OpenGl_VertexBufferT<OpenGl_VertexBufferCompat>(5, myAttribs); break;
				case 6: aVboAttribs = new OpenGl_VertexBufferT<OpenGl_VertexBufferCompat>(6, myAttribs); break;
				case 7: aVboAttribs = new OpenGl_VertexBufferT<OpenGl_VertexBufferCompat>(7, myAttribs); break;
				case 8: aVboAttribs = new OpenGl_VertexBufferT<OpenGl_VertexBufferCompat>(8, myAttribs); break;
				case 9: aVboAttribs = new OpenGl_VertexBufferT<OpenGl_VertexBufferCompat>(9, myAttribs); break;
				case 10: aVboAttribs = new OpenGl_VertexBufferT<OpenGl_VertexBufferCompat>(10, myAttribs); break;
			}
			aVboAttribs.initLink(myAttribs, 0, myAttribs.NbElements, (int)All.None);
			if (myIndices != null)
			{
				OpenGl_IndexBufferCompat aVboIndices = new OpenGl_IndexBufferCompat();
				switch (myIndices.Stride)
				{
					case 2:
						{
							aVboIndices.initLink(myIndices, 1, myIndices.NbElements, (int)All.UnsignedShort);
							break;
						}
					case 4:
						{
							aVboIndices.initLink(myIndices, 1, myIndices.NbElements, (int)All.UnsignedInt);
							break;
						}
					default:
						{
							return false;
						}
				}
				//todo!!myVboIndices = aVboIndices;
			}
			//todo!!myVboAttribs = aVboAttribs;
			if (!theCtx.caps.keepArrayData
			 && !theToKeepData)
			{
				// does not make sense for compatibility mode
				//myIndices.Nullify();
				//myAttribs.Nullify();
			}

			return true;
		}

		private void clearMemoryGL(OpenGl_Context theCtx)
		{
			throw new NotImplementedException();
		}

		private bool initNormalVbo(OpenGl_Context theCtx)
		{
			throw new NotImplementedException();
		}

		internal int DrawMode()
		{
			return myDrawMode;
		}
	}
}

﻿using System.Collections.Generic;

namespace OCCPort
{
    internal class TopExp_Stack: TopoDS_Iterator
    {
        public List<TopoDS_Iterator> list = new List<TopoDS_Iterator>();
        public TopoDS_Shape this[int i]
        {
            get { return myShapes[i]; }
            set { myShapes[i] = value; }
        }
    }
}
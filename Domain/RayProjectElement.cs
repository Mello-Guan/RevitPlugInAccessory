using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RevitPlugInAccessory
{
    public class RayProjectElement //通过射线相交法得到的实例类
    {
        //穿过的实例
        public Element Elem { get; set; }

        //穿过实例上的点坐标
        public XYZ RayProjectPoint { get; set; }

        //穿过的对象引用（可能为face，element等）
        public Reference Ref { get; set; }



        public RayProjectElement(Element elem, XYZ rayProjectPoint, Reference rref)
        {
            this.Elem = elem;
            this.RayProjectPoint = rayProjectPoint;
            this.Ref = rref;
        }
    }
}

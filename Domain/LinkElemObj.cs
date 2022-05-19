using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RevitPlugInAccessory
{
    ///链接文档中的对象类
    public class LinkElemObj
    {
        //链接对象转化为链接文档中的Element对象，可以直接 as 为各种类型对象
        public Element Link_Elem { get; set; }

        //链接对象所在链接文档
        public Document Link_Doc { get; set; }

        //链接对象的转化矩阵
        public Transform Link_Transform { get; set; }

        public LinkElemObj(Element link_Elem, Document link_Doc, Transform link_Transform)
        {
            this.Link_Elem = link_Elem;
            this.Link_Doc = link_Doc;
            this.Link_Transform = link_Transform;
        }

    }
}

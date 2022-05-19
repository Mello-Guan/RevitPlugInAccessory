using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RevitPlugInAccessory.Domain
{
    ///自建尺寸标注类
    public class RvtDimension
    {
        //表示尺寸标注的Id
        public string Id { get; set; }

        //表示尺寸标注所在视图
        public View LocView { get; set; }

        //表示尺寸标注的位置线
        public Line LocLine { get; set; }

        //表示尺寸标注的引用集合
        public ReferenceArray RefArray { get; set; }

        //表示尺寸标注的类型
        public DimensionType DimType { get; set; }




        //构造函数
        public RvtDimension(string id, View locView, Line locLine, ReferenceArray refArray, DimensionType dimType)
        {
            this.Id = id;
            this.LocView = locView;
            this.LocLine = locLine;
            this.RefArray = refArray;
            this.DimType = dimType;
        }
    }
}


using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RevitPlugInAccessory.Domain
{
    /// 标注段类
    public class DimSegment //标注段类
    {
        //表示标段的序号
        public int Num { get; set; }


        //表示标注段文字总宽度
        public double Width { get; set; }


        //表示标注段文字底部边缘中心坐标
        public XYZ TextBotMidPosition { get; set; }


        //表示与前一个标注文字的重合距离
        public double OverDisToBefore1 { get; set; }


        //表示与前一个的前一个标注文字的重合距离
        public double OverDisToBefore2 { get; set; }


        //表示与后一个标注文字的重合距离（主要用于判断第一个标段）
        public double OverDisToAfter1 { get; set; }


        //记录移动方式（会影响后面一个的移动方式）(包括：“前”“后”“上”“下”)
        public string MoveWay { get; set; }



        //构造函数
        public DimSegment(int num, double width, XYZ textBotMidPosition, double overDisToBefore1, double overDisToBefore2, double overDisToAfter1, string moveWay)
        {
            this.Num = num;
            this.Width = width;
            this.TextBotMidPosition = textBotMidPosition;
            this.OverDisToBefore1 = overDisToBefore1;
            this.OverDisToBefore2 = overDisToBefore2;
            this.OverDisToAfter1 = overDisToAfter1;
            this.MoveWay = moveWay;
        }

    }
}


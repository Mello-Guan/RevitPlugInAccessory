using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RevitPlugInAccessory
{
    /// 数字字体类
    public class DigitFont
    {
        //表示文字字体的名称(属性中设置的“文字字体”mm)
        public string Name { get; set; }


        //表示文字字体的大小(属性中设置的“文字大小”mm)
        public double Size { get; set; }


        //表示文字宽度系数(属性中设置的“宽度系数”)
        public double WidthFactor { get; set; }


        //表示字体在1:100的视图比例下一个数字的实际测量宽度（mm）
        public double Width { get; set; }


        //表示字体在1:100的视图比例下一个数字的实际测量高度（mm）
        public double Hight { get; set; }


        //表示在1:100的视图比例下,文字底部边缘和标注文本位置点（TextPosition坐标）之间的距离
        public double OffsetToTextPosition { get; set; }



        //构造函数
        public DigitFont(string name, double size, double widthFactor, double width, double hight, double offsetToTextPosition)
        {
            this.Name = name;
            this.Size = size;
            this.WidthFactor = widthFactor;
            this.Width = width;
            this.Hight = hight;
            this.OffsetToTextPosition = offsetToTextPosition;
        }

    }
}

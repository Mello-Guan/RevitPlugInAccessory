using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RevitPlugInAccessory
{
    public class PmethodTagSetFormPara //Form_PmethodTagSet窗体的传入参数类
    {
        //以下数值是否跟随视图比例变化(默认随视图比例变化)
        public bool ChangeByViewScale { get; set; }


        //文本偏移值（视图比例为100下）
        public double TextOffset { get; set; }


        //文本绝对偏移还是相对偏移
        public TextOffsetKind OffsetKind { get; set; }


        //文本偏移方向
        public XYZ OffsetOri { get; set; }


        //是否设置引线
        public bool HasLeader { get; set; }




        public PmethodTagSetFormPara(bool changeByViewScale, double textOffset, TextOffsetKind offsetKind, XYZ offsetOri, bool hasLeader)
        {
            this.ChangeByViewScale = changeByViewScale;
            this.TextOffset = textOffset;
            this.OffsetKind = offsetKind;
            this.OffsetOri = offsetOri;
            this.HasLeader = hasLeader;
        }

        public enum TextOffsetKind
        {
            Relative,
            Absolute,
            Null,
        }

    }
}
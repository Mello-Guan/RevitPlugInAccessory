using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RevitPlugInAccessory
{
    public class TagSetFormPara  //Form_TagSet窗体的传入参数类
    {

        //标记族关键字
        public string FNkey { get; set; }

        //标记类型关键字
        public string FSNkey { get; set; }

        public TagMode Tagmode { get; set; }

        public TagOrientation TagOri { get; set; }

        //是否带引线
        public bool HasLeader { get; set; }


        //视图X方向定位距离（不随视图比例变化）（相对方向的时候是沿着构件方向）（标记在右边的时候是右正左负，标记在左边的时候是左正右负）
        public double XDirectionOffset { get; set; }


        //视图Y方向定位距离（不随视图比例变化）（相对偏移的时候是垂直构件方向）（标记在上边的时候是上正下负，标记在下边的时候是下正上负）
        public double YDirectionOffset { get; set; }


        //文本按比例右侧X方向偏移（随视图比例变化）（相对方向的的时候是沿着构件方向）
        public double XrTextOffset { get; set; }

        //文本按比例左侧X方向偏移（随视图比例变化）（相对方向的的时候是沿着构件方向）（只针对带引线对象）
        public double XlTextOffset { get; set; }


        //文本按比例上侧Y方向偏移（随视图比例变化）（相对方向的的时候是垂直构件方向）
        public double YtTextOffset { get; set; }

        //文本按比例下侧Y方向偏移（随视图比例变化）（相对方向的的时候是垂直构件方向）（只针对带引线对象）
        public double YbTextOffset { get; set; }


        //水平段长度
        public double HoriLeaderLength { get; set; }


        //是否旋转
        public bool IsRotate { get; set; }


        //旋转角度(角度值)
        public double RotateAngle { get; set; }



        public TagSetFormPara(string fNkey, string fSNkey, TagMode tagmode, TagOrientation tagOri, bool hasLeader, double xDirectionOffset, double yDirectionOffset,
                              double xrTextOffset, double xlTextOffset, double ytTextOffset, double ybTextOffset, double horiLeaderLength, bool isRotate, double rotateAngle)
        {
            this.FNkey = FNkey;
            this.FSNkey = fSNkey;
            this.Tagmode = tagmode;
            this.TagOri = tagOri;
            this.HasLeader = hasLeader;
            this.XDirectionOffset = xDirectionOffset;
            this.YDirectionOffset = yDirectionOffset;
            this.XrTextOffset = xrTextOffset;
            this.XlTextOffset = xlTextOffset;
            this.YtTextOffset = ytTextOffset;
            this.YbTextOffset = ybTextOffset;
            this.HoriLeaderLength = horiLeaderLength;
            this.IsRotate = isRotate;
            this.RotateAngle = rotateAngle;
        }

    }
}

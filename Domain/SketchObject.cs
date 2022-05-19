using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RevitPlugInAccessory
{
    public class SketchObject //草图对象（布置辅助线）
    {
        //辅助线对象
        public FamilyInstance Ins { get; set; }


        #region 点辅助线
        public XYZ LocPoint { get; set; }


        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="ins">辅助参考实例</param>
        /// <param name="locPoint">实例参考位置点</param>
        public SketchObject(FamilyInstance ins, XYZ locPoint)
        {
            this.Ins = ins;
            this.LocPoint = locPoint;
        }

        #endregion //点辅助线

        #region 直线辅助线

        public Line LocLine { get; set; }

        public XYZ StartXYZ { get; set; }

        public XYZ EndXYZ { get; set; }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="ins">辅助参考实例</param>
        /// <param name="locLine">实例参考位置线</param>
        /// <param name="startXYZ">位置线起点</param>
        /// <param name="endXYZ">位置线终点</param>
        public SketchObject(FamilyInstance ins, Line locLine, XYZ startXYZ, XYZ endXYZ)
        {
            this.Ins = ins;
            this.LocLine = locLine;
            this.StartXYZ = startXYZ;
            this.EndXYZ = endXYZ;
        }

        #endregion //直线辅助线

        #region 直线集辅助线

        public IList<Line> LocLineList { get; set; }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="ins">辅助参考实例</param>
        /// <param name="locLineList">实例参考直线集</param>
        public SketchObject(FamilyInstance ins, IList<Line> locLineList)
        {
            this.Ins = ins;
            this.LocLineList = locLineList;
        }

        #endregion //直线集辅助线

        #region 曲线辅助线

        public Arc LocArc { get; set; }

        public XYZ Pxyz_s { get; set; }

        public XYZ Pxyz_e { get; set; }

        public XYZ Pxyz_3 { get; set; }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="ins">辅助参考实例</param>
        /// <param name="locArc">实例参考位置曲线</param>
        /// <param name="pxyz_s">曲线起点</param>
        /// <param name="pxyz_e">曲线终点</param>
        /// <param name="pxyz_3">曲线任一点</param>
        public SketchObject(FamilyInstance ins, Arc locArc, XYZ pxyz_s, XYZ pxyz_e, XYZ pxyz_3)
        {
            this.Ins = ins;
            this.LocArc = locArc;
            this.Pxyz_s = pxyz_s;
            this.Pxyz_e = pxyz_e;
            this.Pxyz_3 = pxyz_3;
        }

        #endregion //曲线辅助线

        #region 圆形辅助线

        public Arc LocCircle { get; set; }

        public XYZ Center { get; set; }

        public double Radius { get; set; }


        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="ins">辅助参考实例</param>
        /// <param name="locCircle">实例参考位置圆心曲线</param>
        /// <param name="center">圆形中点</param>
        /// <param name="radius">圆形半径</param>
        public SketchObject(FamilyInstance ins, Arc locCircle, XYZ center, double radius)
        {
            this.Ins = ins;
            this.LocCircle = locCircle;
            this.Center = center;
            this.Radius = radius;
        }

        #endregion //圆形辅助线

        #region 矩形辅助线

        public IList<Line> LocRectangle { get; set; }

        public XYZ TlXYZ { get; set; }

        public XYZ TrXYZ { get; set; }

        public XYZ BlXYZ { get; set; }

        public XYZ BrXYZ { get; set; }

        public double Width { get; set; }

        public double Height { get; set; }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="ins">辅助参考实例</param>
        /// <param name="locRectangle">实例参考矩形直线集</param>
        /// <param name="tlXYZ">左上点坐标</param>
        /// <param name="trXYZ">右上点坐标</param>
        /// <param name="blXYZ">左下点坐标</param>
        /// <param name="brXYZ">右下点坐标</param>
        /// <param name="width">矩形宽度</param>
        /// <param name="height">矩形高度</param>
        public SketchObject(FamilyInstance ins, IList<Line> locRectangle, XYZ tlXYZ, XYZ trXYZ, XYZ blXYZ, XYZ brXYZ, double width, double height)
        {
            this.Ins = ins;
            this.LocRectangle = locRectangle;
            this.TlXYZ = tlXYZ;
            this.TrXYZ = trXYZ;
            this.BlXYZ = blXYZ;
            this.BrXYZ = brXYZ;
            this.Width = width;
            this.Height = height;
        }

        #endregion //矩形辅助线

        #region PickBox辅助线

        public XYZ MidXYZ { get; set; }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="midXYZ">PickBox中心点</param>
        /// <param name="tlXYZ">左上点坐标</param>
        /// <param name="trXYZ">右上点坐标</param>
        /// <param name="blXYZ">左下点坐标</param>
        /// <param name="brXYZ">右下点坐标</param>
        /// <param name="width">矩形宽度</param>
        /// <param name="height">矩形高度</param>
        public SketchObject(XYZ midXYZ, XYZ tlXYZ, XYZ trXYZ, XYZ blXYZ, XYZ brXYZ, double width, double height)
        {
            this.MidXYZ = midXYZ;
            this.TlXYZ = tlXYZ;
            this.TrXYZ = trXYZ;
            this.BlXYZ = blXYZ;
            this.BrXYZ = brXYZ;
            this.Width = width;
            this.Height = height;
        }

        #endregion //PickBox辅助线

        #region DetailCurve辅助线

        //辅助线对象
        public Element Elem { get; set; }

        public Curve DetailCurve { get; set; }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="elem">DetailCurve 的Elem形式对象</param>
        /// <param name="detailCurve">DetailCurve 对应的Curve</param>
        public SketchObject(Element elem, Curve detailCurve)
        {
            this.Elem = elem;
            this.DetailCurve = detailCurve;
        }

        #endregion //DetailCurve辅助线

    }
}


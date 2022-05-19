using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace RevitPlugInAccessory
{
    /// 数学计算相关方法
    public class MathsCalculation
    {


        #region 相关枚举

        /// 取值方式枚举
        public enum ExtremeWay
        {
            //最大值
            Max,
            //最小值
            Min,
        }

        #endregion //相关枚举




        #region 向量相关方法

        /// <summary>
        /// 返回两个点的中点
        /// </summary>
        /// <param name="p1">第一个点</param>
        /// <param name="p2">第二个点</param>
        /// <returns></returns>
        public XYZ GetMidPoint(XYZ p1, XYZ p2)
        {
            XYZ rP = new XYZ((p1.X + p2.X) / 2, (p1.Y + p2.Y) / 2, (p1.Z + p2.Z) / 2);

            return rP;
        }

        /// <summary>
        /// 将点按照某一方向排序
        /// </summary>
        /// <param name="PointList">点的集合</param>
        /// <param name="Ori">排序方向</param>
        /// <returns></returns>
        public List<XYZ> OrderPointsOnOri(IList<XYZ> PointList, XYZ Ori)
        {
            if (PointList.Distinct().Count() == 1) { return PointList.ToList(); }

            //方向线
            Line l = Line.CreateUnbound(XYZ.Zero, Ori);

            //得到点在方向线上的投影点(key为原来的点，value为投影后的点)
            IDictionary<XYZ, XYZ> PointOnLine = PointList.Distinct().ToDictionary(o => o, o => l.Project(o).XYZPoint);

            if (PointOnLine.Values.Distinct().Count() == 1) { return PointList.ToList(); }

            try
            {
                //将点排序
                List<XYZ> orderList = new List<XYZ>();

                //得到第一个点
                XYZ first = GetExtremePointOnOri(PointList, Ori, ExtremeWay.Min);

                //将点按照与第一个点的在直线上的投影距离排序
                orderList = PointOnLine.OrderBy(o => o.Value.DistanceTo(l.Project(first).XYZPoint)).Select(o => o.Key).ToList();

                return orderList;

            }
            catch
            {
                return PointList.ToList();
            }
        }



        /// <summary>
        /// 返回点的集合中在某一个向量方向上面的极值点坐标
        /// </summary>
        /// <param name="PointList">点的坐标</param>
        /// <param name="Ori">方向向量</param>
        /// <param name="Way">取得的极值方式：Max，Min</param>
        /// <returns></returns>
        public XYZ GetExtremePointOnOri(IList<XYZ> PointList, XYZ Ori, ExtremeWay Way)
        {

            if (PointList.Distinct().Count() == 1) { return PointList.First(); }

            //方向线
            Line l = Line.CreateUnbound(XYZ.Zero, Ori);

            //得到点在方向线上的投影点(key为原来的点，value为投影后的点)
            IDictionary<XYZ, XYZ> PointOnLine = PointList.Distinct().ToDictionary(o => o, o => l.Project(o).XYZPoint);

            if (PointOnLine.Values.Distinct().Count() == 1) { return PointList.First(); }

            //得到极值点的投影点
            XYZ max = PointOnLine.Values.First();
            XYZ min = PointOnLine.Values.First();

            foreach (XYZ p in PointOnLine.Values)
            {
                //得到极大值点
                max = GetExtremePointOnOri(p, max, Ori, ExtremeWay.Max);
                min = GetExtremePointOnOri(p, min, Ori, ExtremeWay.Min);
            }

            //返回极值点
            if (Way == ExtremeWay.Max)
            {
                return PointOnLine.Where(o => o.Value == max).First().Key;
            }
            else
            {
                return PointOnLine.Where(o => o.Value == min).First().Key;
            }

        }


        /// <summary>
        /// 比较两个点在某一方向上的大小关系，并返回满足条件的那个点
        /// </summary>
        /// <param name="p1">第一个比较点p1</param>
        /// <param name="p2">第二个比较点p2</param>
        /// <param name="Ori">方向向量</param>
        /// <param name="Way">取得的极值方式：Max，Min</param>
        /// <returns></returns>
        public XYZ GetExtremePointOnOri(XYZ p1, XYZ p2, XYZ Ori, ExtremeWay Way)
        {
            //将两个点组成向量
            XYZ n = (p2 - p1).Normalize();

            //求上面的向量和方向向量的夹角大小，大于90度则p1>p2,小于则p1<p2
            double angle = n.AngleTo(Ori);

            //p1>p2
            if (angle > Math.PI / 2)
            {
                if (Way == ExtremeWay.Max)
                {
                    return p1;
                }
                else
                {
                    return p2;
                }
            }
            //p1<p2
            else
            {
                if (Way == ExtremeWay.Max)
                {
                    return p2;
                }
                else
                {
                    return p1;
                }
            }
        }



        /// <summary>
        /// 两点形成直线在已知方向上的投影直线
        /// </summary>
        /// <param name="l">两点形成直线</param>
        /// <param name="Ori">已知方向向量</param>
        /// <returns></returns>
        public Line ProjectionLineOnOri(Line l, XYZ Ori)
        {
            XYZ p1 = l.GetEndPoint(0);
            XYZ p2 = l.GetEndPoint(1);
            Line OriLine = Line.CreateUnbound(p1, Ori);
            XYZ p1_ = OriLine.Project(p1).XYZPoint;
            XYZ p2_ = OriLine.Project(p2).XYZPoint;
            if (p1_.DistanceTo(p2) < 1e-6) { return null; }
            Line rL = Line.CreateBound(p1_, p2_);
            return rL;
        }


        /// <summary>
        /// 计算两个点在某一方向上的距离偏移（带正负方向，p2相对于p1在该方向上的偏移）
        /// </summary>
        /// <param name="p1">第一个点</param>
        /// <param name="p2">第二个点</param>
        /// <param name="ori">方向</param>
        /// <returns></returns>
        public double PointsDistanceOnOri(XYZ p1, XYZ p2, XYZ ori)
        {
            if (p1.IsAlmostEqualTo(p2, 1e-6)) { return 0; }

            try
            {
                Line l = Line.CreateBound(p1, p2);
                Line ll = ProjectionLineOnOri(l, ori);
                if (GetExtremePointOnOri(p1, p2, ori, ExtremeWay.Max).IsAlmostEqualTo(p2)) { return ll.Length; }
                else { return (-ll.Length); }
            }
            catch
            { return 0; }
        }


        /// <summary>
        /// 得到一个方向向量在一个已知平面上面的垂直方向向量（看向平面顺时针方向）
        /// </summary>
        /// <param name="ori">已知方向向量</param>
        /// <param name="PlaneRightOri">已知平面的右方向（X方向）</param>
        /// <param name="PlaneUpOri">已知平面的上方向（Y方向）</param>
        /// <param name="PlaneDirection">已知平面的法向量(Z方向)</param>
        /// <returns></returns>
        public XYZ GetNormalOnPlane(XYZ ori, XYZ PlaneRightOri, XYZ PlaneUpOri, XYZ PlaneDirection)
        {
            //double a = 1;
            //double b = 1;
            //double c = 1;

            //根据两个垂直向量点积为零 a * ori.X + b * ori.Y + c * ori.Z = 0 和 a * PlaneDirection.X + b * PlaneDirection.Y + c * PlaneDirection.Z = 0 ，求得：

            //a = ori.Y * PlaneDirection.Z - PlaneDirection.Y * ori.Z;
            //b = PlaneDirection.X * ori.Z - ori.X * PlaneDirection.Z;
            //c = ori.X * PlaneDirection.Y - PlaneDirection.X * ori.Y;
            //结果为：
            XYZ normal = new XYZ(ori.Y * PlaneDirection.Z - PlaneDirection.Y * ori.Z,
                                PlaneDirection.X * ori.Z - ori.X * PlaneDirection.Z,
                                ori.X * PlaneDirection.Y - PlaneDirection.X * ori.Y
                                ).Normalize();

            #region 根据向量在平面中的方向讨论其顺时针垂直向量方向

            if (ori.AngleTo(PlaneRightOri) <= Math.PI / 2 && ori.AngleTo(PlaneUpOri) <= Math.PI / 2) //第一象限
            {
                if (!(normal.AngleTo(PlaneRightOri) <= Math.PI / 2 && normal.AngleTo(PlaneUpOri) >= Math.PI / 2)) //垂直向量不在第二象限
                {
                    normal = normal.Negate();//垂直向量要反向
                }
            }
            else if (ori.AngleTo(PlaneRightOri) <= Math.PI / 2 && ori.AngleTo(PlaneUpOri) >= Math.PI / 2)//第二象限
            {
                if (!(normal.AngleTo(PlaneRightOri) >= Math.PI / 2 && normal.AngleTo(PlaneUpOri) >= Math.PI / 2)) //垂直向量不在第三象限
                {
                    normal = normal.Negate();//垂直向量要反向
                }
            }
            else if (ori.AngleTo(PlaneRightOri) >= Math.PI / 2 && ori.AngleTo(PlaneUpOri) >= Math.PI / 2)//第三象限
            {
                if (!(normal.AngleTo(PlaneRightOri) >= Math.PI / 2 && normal.AngleTo(PlaneUpOri) <= Math.PI / 2)) //垂直向量不在第四象限
                {
                    normal = normal.Negate();//垂直向量要反向
                }
            }
            else if (ori.AngleTo(PlaneRightOri) >= Math.PI / 2 && ori.AngleTo(PlaneUpOri) <= Math.PI / 2)//第四象限
            {
                if (!(normal.AngleTo(PlaneRightOri) <= Math.PI / 2 && normal.AngleTo(PlaneUpOri) <= Math.PI / 2)) //垂直向量不在第一象限
                {
                    normal = normal.Negate();//垂直向量要反向
                }
            }

            #endregion //根据向量在平面中的方向讨论其顺时针垂直向量方向

            return normal;
        }



        /// <summary>
        /// 得到一个向量在一个已知平面上的垂直方向向量，且这个方向向量是顺时针还是逆时针取决于这个向量在平面中的方向，总是冲着平面中上侧(向量竖直时候冲着右侧)
        /// </summary>
        /// <param name="ori">已知方向向量</param>
        /// <param name="PlaneRightOri">已知平面的右方向（X方向）</param>
        /// <param name="PlaneUpOri">已知平面的上方向（Y方向）</param>
        /// <param name="PlaneDirection">已知平面的法向量(Z方向)</param>
        /// <returns></returns>
        public XYZ GetFnormalOfVectorOnPlane(XYZ ori, XYZ PlaneRightOri, XYZ PlaneUpOri, XYZ PlaneDirection)
        {
            //double a = 1;
            //double b = 1;
            //double c = 1;

            //根据两个垂直向量点积为零 a * ori.X + b * ori.Y + c * ori.Z = 0 和 a * PlaneDirection.X + b * PlaneDirection.Y + c * PlaneDirection.Z = 0 ，求得：

            //a = ori.Y * PlaneDirection.Z - PlaneDirection.Y * ori.Z;
            //b = PlaneDirection.X * ori.Z - ori.X * PlaneDirection.Z;
            //c = ori.X * PlaneDirection.Y - PlaneDirection.X * ori.Y;
            //结果为：
            XYZ normal = new XYZ(ori.Y * PlaneDirection.Z - PlaneDirection.Y * ori.Z,
                                PlaneDirection.X * ori.Z - ori.X * PlaneDirection.Z,
                                ori.X * PlaneDirection.Y - PlaneDirection.X * ori.Y
                                ).Normalize();



            #region 根据向量在平面中的方向讨论其选择冲向上面和右边的那个垂直向量

            if (ori.IsAlmostEqualTo(PlaneRightOri, 1e-6) || ori.IsAlmostEqualTo(PlaneRightOri.Negate(), 1e-6)) //视图水平方向
            {
                if (!normal.IsAlmostEqualTo(PlaneUpOri, 1e-6)) //垂直向量不在视图向上方
                {
                    normal = normal.Negate();//垂直向量要反向
                }
            }
            else if (ori.IsAlmostEqualTo(PlaneUpOri, 1e-6) || ori.IsAlmostEqualTo(PlaneUpOri.Negate(), 1e-6)) //视图垂直方向
            {
                if (!normal.IsAlmostEqualTo(PlaneRightOri, 1e-6)) //垂直向量不在视图向右方
                {
                    normal = normal.Negate();//垂直向量要反向
                }
            }

            else if (ori.AngleTo(PlaneRightOri) < Math.PI / 2 && ori.AngleTo(PlaneUpOri) < Math.PI / 2) //第一象限
            {
                if (!(normal.AngleTo(PlaneRightOri) > Math.PI / 2 && normal.AngleTo(PlaneUpOri) < Math.PI / 2)) //垂直向量不在第四象限
                {
                    normal = normal.Negate();//垂直向量要反向
                }
            }
            else if (ori.AngleTo(PlaneRightOri) < Math.PI / 2 && ori.AngleTo(PlaneUpOri) > Math.PI / 2)//第二象限
            {
                if (!(normal.AngleTo(PlaneRightOri) < Math.PI / 2 && normal.AngleTo(PlaneUpOri) < Math.PI / 2)) //垂直向量不在第一象限
                {
                    normal = normal.Negate();//垂直向量要反向
                }
            }
            else if (ori.AngleTo(PlaneRightOri) > Math.PI / 2 && ori.AngleTo(PlaneUpOri) > Math.PI / 2)//第三象限
            {
                if (!(normal.AngleTo(PlaneRightOri) > Math.PI / 2 && normal.AngleTo(PlaneUpOri) < Math.PI / 2)) //垂直向量不在第四象限
                {
                    normal = normal.Negate();//垂直向量要反向
                }
            }
            else if (ori.AngleTo(PlaneRightOri) > Math.PI / 2 && ori.AngleTo(PlaneUpOri) < Math.PI / 2)//第四象限
            {
                if (!(normal.AngleTo(PlaneRightOri) < Math.PI / 2 && normal.AngleTo(PlaneUpOri) < Math.PI / 2)) //垂直向量不在第一象限
                {
                    normal = normal.Negate();//垂直向量要反向
                }
            }


            #endregion //根据向量在平面中的方向讨论其选择冲向上面和右边的那个垂直向量

            return normal;
        }


        /// <summary>
        /// 返回一个点在平面上的投影点坐标
        /// </summary>
        /// <param name="xyz">平面外任一点坐标</param>
        /// <param name="planeP">平面上的任一点坐标</param>
        /// <param name="planeNormal">平面的法向量</param>
        /// <returns></returns>
        public XYZ GetPointOnPlane(XYZ xyz, XYZ planeP, XYZ planeNormal)
        {
            double x = 0;
            double y = 0;
            double z = 0;

            x = (planeNormal.X * planeNormal.X * planeP.X + planeNormal.Y * planeNormal.Y * xyz.X - planeNormal.Y * xyz.Y * planeNormal.X + planeNormal.Y * planeP.Y * planeNormal.X + planeNormal.Z * planeNormal.Z * xyz.X - planeNormal.Z * xyz.Z * planeNormal.X + planeNormal.Z * planeP.Z * planeNormal.X) / (planeNormal.X * planeNormal.X + planeNormal.Y * planeNormal.Y + planeNormal.Z * planeNormal.Z);
            y = (planeNormal.Y * planeNormal.Z * planeP.Z + planeNormal.Z * planeNormal.Z * xyz.Y - planeNormal.Y * planeNormal.Z * xyz.Z + planeNormal.Y * planeNormal.X * planeP.X + planeNormal.X * planeNormal.X * xyz.Y - planeNormal.X * planeNormal.Y * xyz.X + planeNormal.Y * planeNormal.Y * planeP.Y) / (planeNormal.X * planeNormal.X + planeNormal.Y * planeNormal.Y + planeNormal.Z * planeNormal.Z);
            z = (planeNormal.X * planeP.X * planeNormal.Z + planeNormal.X * planeNormal.X * xyz.Z - planeNormal.X * xyz.X * planeNormal.Z + planeNormal.Y * planeP.Y * planeNormal.Z + planeNormal.Y * planeNormal.Y * xyz.Z - planeNormal.Y * xyz.Y * planeNormal.Z + planeNormal.Z * planeNormal.Z * planeP.Z) / (planeNormal.X * planeNormal.X + planeNormal.Y * planeNormal.Y + planeNormal.Z * planeNormal.Z);

            return new XYZ(x, y, z);
        }


        /// <summary>
        /// 返回直线在平面上的投影直线
        /// </summary>
        /// <param name="line">平面外的一条直线</param>
        /// <param name="planeP">平面上的任一点坐标</param>
        /// <param name="planeNormal">平面的法向量</param>
        /// <returns></returns>
        public Line GetLineOnPlane(Line line, XYZ planeP, XYZ planeNormal)
        {
            if (line.IsBound)
            {
                Line newl = Line.CreateBound(GetPointOnPlane(line.GetEndPoint(0), planeP, planeNormal), GetPointOnPlane(line.GetEndPoint(1), planeP, planeNormal));
                return newl;
            }
            else
            {
                XYZ newOri = (GetPointOnPlane(line.Origin + line.Direction * 100, planeP, planeNormal) - GetPointOnPlane(line.Origin, planeP, planeNormal)).Normalize();
                Line newl = Line.CreateUnbound(GetPointOnPlane(line.Origin, planeP, planeNormal), newOri);
                return newl;
            }
        }


        /// <summary>
        /// 得到点在某一方向上在同一直线上的所有点（点的双侧方向共线）（不包含当前点）
        /// </summary>
        /// <param name="point">已知点坐标</param>
        /// <param name="ori">已知方向</param>
        /// <param name="points">需要筛选的点的集合</param>
        /// <param name="tolerance">误差精度</param>
        /// <returns></returns>
        public List<XYZ> GetOnLinePoints(XYZ point, XYZ ori, List<XYZ> points, double tolerance)
        {
            List<XYZ> getPoints = new List<XYZ>();

            Line l = Line.CreateBound(point - ori * 1e20, point + ori * 1e20);

            getPoints.AddRange(points.Where(o => l.Distance(o) < tolerance).ToList());

            return getPoints;
        }

        /// <summary>
        /// 得到点在视图中某一方向上，所有视图投影点在一条直线上的点的集合（点的双侧方向共线）（不包含当前点）
        /// </summary>
        /// <param name="point">基准点</param>
        /// <param name="view">判断视图</param>
        /// <param name="ori">共线方向</param>
        /// <param name="points">需要判断的点的集合</param>
        /// <param name="tolerance">误差精度</param>
        /// <returns></returns>
        public List<XYZ> GetOnLinePointsOnView(XYZ point, Autodesk.Revit.DB.View view, XYZ ori, List<XYZ> points, double tolerance)
        {
            MathsCalculation mc = new MathsCalculation();
            List<XYZ> getPoints = new List<XYZ>();
            XYZ pointOnView = mc.GetPointOnPlane(point, view.Origin, view.ViewDirection);

            Line l = Line.CreateBound(pointOnView - ori * 1e20, pointOnView + ori * 1e20);

            getPoints.AddRange(points.Where(o => l.Distance(mc.GetPointOnPlane(o, view.Origin, view.ViewDirection)) < tolerance).ToList());

            return getPoints;
        }


        /// <summary>
        /// 得到点在某一方向上在同一射线上的所有点（点的单侧方向共线）（不包含当前点）
        /// </summary>
        /// <param name="point">已知点坐标</param>
        /// <param name="ori">已知方向</param>
        /// <param name="points">需要筛选的点的集合</param>
        /// <param name="tolerance">误差精度</param>
        /// <returns></returns>
        public List<XYZ> GetOnRayPoints(XYZ point, XYZ ori, List<XYZ> points, double tolerance)
        {
            List<XYZ> getPoints = new List<XYZ>();

            Line l = Line.CreateBound(point, point + ori * 1e20);

            getPoints.AddRange(points.Where(o => l.Distance(o) < tolerance).ToList());

            return getPoints;
        }


        /// <summary>
        /// 返回两条直线交点方法
        /// </summary>
        /// <param name="line_1">第一条直线</param>
        /// <param name="line_2">第二条直线</param>
        /// <returns></returns>
        public XYZ Line_IEresult(Line line_1, Line line_2)
        {
            XYZ xyz = null;
            IntersectionResultArray result;
            SetComparisonResult scr = line_1.Intersect(line_2, out result);
            if (scr == SetComparisonResult.Overlap)//不重合
            {
                if (SetComparisonResult.Disjoint != scr)//相交
                {
                    xyz = result.get_Item(0).XYZPoint;
                }
            }
            return xyz;
        }



        /// <summary>
        /// 返回两个向量定向旋转的夹角，第二个向量为基准向量, rotateOri 为 ViewDirection 为顺时针，rotateOri为 ViewDirection.Negate() 为逆时针
        /// </summary>
        /// <param name="a">第一个向量</param>
        /// <param name="b">第二个向量（基准向量）</param>
        /// <param name="rotateOri">旋转方向，ViewDirection 为顺时针，ViewDirection.Negate() 为逆时针</param>
        /// <returns></returns>
        public double SignedAngleBetween(XYZ a, XYZ b, XYZ rotateOri)
        {
            double angle = a.AngleTo(b);

            int sign = Math.Sign(rotateOri.DotProduct(a.CrossProduct(b)));

            double signed_angle = angle * sign;

            if (signed_angle == 0)
            {
                if (a.IsAlmostEqualTo(b, 1e-6)) { signed_angle = 0; }
                else { signed_angle = Math.PI; }
            }

            else if (signed_angle < 0)
            {
                signed_angle = Math.PI * 2 + signed_angle;
            }

            return signed_angle;
        }


        /// <summary>
        /// 弧度值转化为角度值
        /// </summary>
        /// <param name="radian">已知弧度值</param>
        /// <returns></returns>
        public double RadianToAngle(double radian)
        {
            double angle = radian * 180 / Math.PI;
            return angle;
        }


        /// <summary>
        /// 角度值转化为弧度值
        /// </summary>
        /// <param name="angle">已知角度值</param>
        /// <returns></returns>
        public double AngleToRadian(double angle)
        {
            double radian = Math.PI / 180 * angle;
            return radian;
        }


        /// <summary>
        /// 获得线与面的交点
        /// </summary>
        /// <param name="face"></param>
        /// <param name="line"></param>
        /// <returns></returns>
        public XYZ CurveIntersectFace(Face face, Line line)
        {

            XYZ returnXYZ = null;

            IntersectionResultArray ira = new IntersectionResultArray();

            SetComparisonResult scr = face.Intersect(line, out ira);

            if (scr != SetComparisonResult.Disjoint)
            {

                if (ira == null)
                {
                    return returnXYZ;
                }

                if (!ira.IsEmpty)
                {
                    returnXYZ = ira.get_Item(0).XYZPoint;
                }
            }

            return returnXYZ;
        }


        #endregion //向量相关方法



        #region 几何判定

        /// <summary>
        /// 判断点是否在区域内(区域边界线均为直线)
        /// </summary>
        /// <param name="array">区域边界线集合</param>
        /// <param name="point">需要判定的点</param>
        /// <param name="view">需要判定的视图</param>
        /// <returns></returns>
        public bool PointInRegion(CurveArray array, XYZ point, Autodesk.Revit.DB.View view)
        {
            XYZ xyz = GetPointOnPlane(point, view.Origin, view.ViewDirection);
            Line line = Line.CreateBound(xyz + view.RightDirection * 1e6, xyz - view.RightDirection * 1e6);

            List<XYZ> xyzlist = new List<XYZ>();
            xyzlist.Add(xyz);

            foreach (Curve curve in array)
            {
                Line line_1 = Line.CreateBound(GetPointOnPlane(curve.GetEndPoint(0), view.Origin, view.ViewDirection), GetPointOnPlane(curve.GetEndPoint(1), view.Origin, view.ViewDirection));
                XYZ xyz_1 = Line_IEresult(line, line_1);
                if (xyz_1 != null)
                {
                    bool buer = false;

                    if (xyzlist.Where(o => o.IsAlmostEqualTo(xyz_1)).Count() > 0) { buer = true; }

                    if (buer == false)
                    {
                        xyzlist.Add(xyz_1);
                    }

                }
            }

            List<XYZ> listing = OrderPointsOnOri(xyzlist, view.RightDirection);
            int d = 0;
            for (int c = 0; c < listing.Count; c++)
            {
                if (listing[c].IsAlmostEqualTo(xyz))
                {
                    d = c;
                    break;
                }
            }
            if (d % 2 == 1 && (listing.Count - 1 - d) % 2 == 1)
            {
                return true;
            }

            return false;
        }



        #endregion //几何判定

    }
}

using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Events;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using RevitPlugInAccessory.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;
using static RevitPlugInAccessory.MathsCalculation;
using static RevitPlugInAccessory.PmethodTagSetFormPara;
using static RevitPlugInAccessory.RevDocInfo;
using static RevitPlugInAccessory.RvtParameter;

namespace RevitPlugInAccessory
{
    /// <summary>
    /// 插件辅助功能
    /// </summary>
    public class AccessoryFunction
    {
        #region 基本属性
        /// <summary>
        /// Revit版本名称（关系到引用的API版本）
        /// </summary>
        public RevitName RvtName { get; set; }

        /// <summary>
        /// UIApplication
        /// </summary>
        public UIApplication UIapp { get; set; }

        /// <summary>
        /// UIDocument
        /// </summary>
        public UIDocument UIdoc { get; set; }

        /// <summary>
        /// Document
        /// </summary>
        public Autodesk.Revit.DB.Document Doc { get; set; }

        #region 属性相关枚举

        /// Revit版本
        public enum RevitName
        {
            /// Revit2016
            Revit2016,
            /// Revit2018
            Revit2018,
            /// Revit2020
            Revit2020,
            /// UnKnown
            UnKnown
        }

        #endregion //属性相关枚举

        #endregion //基本属性




        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="rvtName">Revit版本</param>
        /// <param name="uIapp">UIApplication</param>
        /// <param name="uIdoc">UIDocument</param>
        /// <param name="doc">Document</param>
        public AccessoryFunction(RevitName rvtName, UIApplication uIapp, UIDocument uIdoc, Autodesk.Revit.DB.Document doc)
        {
            this.RvtName = rvtName;
            this.UIapp = uIapp;
            this.UIdoc = uIdoc;
            this.Doc = doc;
        }








        #region 操作辅助线相关方法

        #region 使用布置自建辅助线族命令，绘制各种样式的辅助线

        /// <summary>
        /// 使用布置自建辅助线族的方式，调用绘制辅助线命令(注意辅助线目前只能为FamilyInstance)
        /// </summary>
        /// <param name="FN">辅助线族名称</param>
        /// <param name="FSN">辅助线族类型</param>
        /// <param name="FamilyFilePath">辅助线族文件路径</param>
        /// <param name="category">辅助线族的类型</param>
        /// <param name="sketchType">辅助线类型</param>
        /// <returns></returns>
        public IList<SketchObject> SketchLineCommand(string FN, string FSN, string FamilyFilePath, BuiltInCategory category, SketchType sketchType)
        {
            //初始化返回的辅助线对象集合
            IList<SketchObject> sketchObjects = new List<SketchObject>();

            #region 载入本地辅助线族

            //实例化自建类库类
            DownLoadFam dlf = new DownLoadFam();

            //载入族判定
            bool success = false;
            //载入辅助线族
            Family fam = dlf.LoadGetFamily(UIdoc.Document, FN, FamilyFilePath, ref success, category);

            if (success == false)
            {
                MessageBox.Show("找不到辅助线族");
                return sketchObjects;
            }

            #endregion //载入本地辅助线族

            Transaction tr = new Transaction(UIdoc.Document);

            try
            {

                #region 激活辅助线族类型

                //得到族类型
                FamilySymbol fs = new FilteredElementCollector(UIdoc.Document).OfClass(typeof(FamilySymbol)).OfCategory(category)
                                  .Cast<FamilySymbol>().ToList().Where(o => o.FamilyName == FN && o.Name == FSN).FirstOrDefault();

                if (fs == null)
                {
                    MessageBox.Show("找不到辅助线族类型");
                    return sketchObjects;
                }
                if (fs.IsActive == false)
                {
                    tr.Start("激活辅助线族类型");

                    fs.Activate();

                    tr.Commit();
                }

                #endregion //激活辅助线族类型



                #region 绘制辅助线并得到辅助线信息           

                //绘制前实例集合
                IList<FamilyInstance> oldList = new FilteredElementCollector(UIdoc.Document).OfClass(typeof(FamilyInstance)).OfCategory(category).Cast<FamilyInstance>().ToList();
                try
                {
                    //绘制辅助线
                    UIdoc.PromptForFamilyInstancePlacement(fs);
                }
                catch { }

                //绘制后实例集合
                IList<FamilyInstance> newList = new FilteredElementCollector(UIdoc.Document).OfClass(typeof(FamilyInstance)).OfCategory(category).Cast<FamilyInstance>().ToList();

                //得到辅助线FamilyInstance对象集合
                IList<FamilyInstance> sketchInsList = newList.Except(oldList).Where(o => o.Symbol.Name == FSN && o.Symbol.FamilyName == FN).ToList();

                foreach (FamilyInstance ins in sketchInsList)
                {

                    switch (sketchType)
                    {
                        case SketchType.None:
                            {
                                MessageBox.Show("未设置辅助线类型");
                                return sketchObjects;
                            }
                        case SketchType.Point:
                            {
                                LocationPoint loc = ins.Location as LocationPoint;
                                XYZ p = loc.Point;
                                sketchObjects.Add(new SketchObject(ins, p));
                                break;
                            }
                        case SketchType.Line:
                            {
                                LocationCurve loc = ins.Location as LocationCurve;
                                Line l = loc.Curve as Line;
                                XYZ startXYZ = l.GetEndPoint(0);
                                XYZ endXYZ = l.GetEndPoint(1);
                                sketchObjects.Add(new SketchObject(ins, l, startXYZ, endXYZ));
                                break;
                            }
                        case SketchType.LineList:
                            {
                                LocationCurve loc = ins.Location as LocationCurve;
                                Line l = loc.Curve as Line;
                                IList<Line> lineList = new List<Line>();
                                lineList.Add(l);
                                //需要根据族计算出来别的线
                                //暂留......
                                sketchObjects.Add(new SketchObject(ins, lineList));
                                break;
                            }
                        case SketchType.Arc:
                            {
                                LocationCurve loc = ins.Location as LocationCurve;
                                Arc arc = loc.Curve as Arc;
                                XYZ xyz_s = arc.GetEndPoint(0);
                                XYZ xyz_e = arc.GetEndPoint(1);
                                XYZ xyz_3 = arc.Tessellate().ElementAt(3);//第三点可随意取
                                sketchObjects.Add(new SketchObject(ins, arc, xyz_s, xyz_e, xyz_3));
                                break;
                            }
                        case SketchType.Circle:
                            {
                                LocationCurve loc = ins.Location as LocationCurve;
                                Arc arc = loc.Curve as Arc;
                                XYZ center = arc.Center;
                                double r = arc.Radius;
                                sketchObjects.Add(new SketchObject(ins, arc, center, r));
                                break;
                            }
                        default:
                            {
                                break;
                            }
                    }

                }

                #endregion //绘制辅助线并得到辅助线信息



                #region 删除辅助线

                tr.Start("删除辅助线");

                List<ElementId> delList = new List<ElementId>();
                delList.AddRange(sketchInsList.Select(o => o.Id).ToList());

                UIdoc.Document.Delete(delList);

                tr.Commit();

                #endregion //删除辅助线


            }
            catch
            {
                if (tr.HasStarted() == true)
                {
                    tr.Commit();//关闭已开启事务
                }

            }
            return sketchObjects;
        }


        /// <summary>
        /// 辅助线类型枚举
        /// </summary>
        public enum SketchType
        {
            /// 基于点的辅助线族
            Point,
            /// 基于线的辅助线族
            Line,
            /// 基于线的辅助线族，族中包含多条辅助线
            LineList,
            /// 基于线的辅助线族，族中位置线为曲线
            Arc,
            /// 基于线的辅助线族，族中位置线为圆
            Circle,
            /// 无
            None
        }

        #endregion //使用布置自建辅助线族命令，绘制各种样式的辅助线

        #region 调用RVT内置绘制 DetailCurve 命令绘制各种样式的辅助线

        /// <summary>
        /// （绘制详图线辅助线第一步方法，需要在非模式窗体命令按钮下直接使用）使用Revit内置的绘制详图线命令绘制各种样式的辅助线并返回绘制前项目中的详图线集合
        /// </summary>
        /// <param name="drawKind">辅助详图线样式</param>
        /// <param name="oldDetailCurveIds">绘制前项目中的详图线集合</param>
        public void SketchCommandByDetailCurve_1(DrawKind drawKind, ref List<ElementId> oldDetailCurveIds)
        {
            //实例化自建类库类
            RevDocInfo rvtDocInfo = new RevDocInfo(UIdoc, Doc);

            //判断视图是否为非三维视图
            if (rvtDocInfo.CheckView(CheckViewSet.NoView3D, "在三维视图中无法操作！") == false) { return; }

            //得到视图中已有的 DetailCurve
            CurveElementFilter filter = new CurveElementFilter(CurveElementType.DetailCurve);
            oldDetailCurveIds.Clear();
            oldDetailCurveIds = new FilteredElementCollector(Doc, UIdoc.ActiveView.Id).WherePasses(filter).ToElementIds().ToList();

            Transaction tr = new Transaction(Doc);

            try
            {
                #region 如果没有草图平面创建草图平面

                //tr.Start("创建草图平面");

                ////如果剖面中无草图平面，需要创建草图平面
                //Plane plane = Plane.CreateByNormalAndOrigin(Doc.ActiveView.ViewDirection, Doc.ActiveView.Origin);

                //SketchPlane sp = SketchPlane.Create(Doc, plane);

                //Doc.ActiveView.SketchPlane = sp;
                ////doc.ActiveView.ShowActiveWorkPlane();
                ///

                //tr.Commit();

                #endregion //如果没有草图平面创建草图平面

                //绘制详图线
                UIFrameworkServices.CommandHandlerService.invokeCommandHandler("ID_OBJECTS_DETAIL_CURVES");
                //UIFrameworkServices.CommandHandlerService.invokeCommandHandler("ID_SNAP_OVERRIDE_NO_SNAP"); //取消捕捉

                //调用绘制方式
                switch (drawKind)
                {
                    case DrawKind.LINE:
                        break;
                    case DrawKind.RECT:
                        UIFrameworkServices.CommandHandlerService.invokeCommandHandler("IDC_RADIO_RECT");
                        break;
                    case DrawKind.INSCRIBED_POLYGON:
                        UIFrameworkServices.CommandHandlerService.invokeCommandHandler("IDC_RADIO_INSCRIBED_POLYGON");
                        break;
                    case DrawKind.CIRCUMSCRIBED_POLYGON:
                        UIFrameworkServices.CommandHandlerService.invokeCommandHandler("IDC_RADIO_CIRCUMSCRIBED_POLYGON");
                        break;
                    case DrawKind.CIRCLE:
                        UIFrameworkServices.CommandHandlerService.invokeCommandHandler("IDC_RADIO_CIRCLE");
                        break;
                    case DrawKind.ARC_FILLET:
                        UIFrameworkServices.CommandHandlerService.invokeCommandHandler("IDC_RADIO_ARC_FILLET");
                        break;
                    case DrawKind.ARC_CENTER:
                        UIFrameworkServices.CommandHandlerService.invokeCommandHandler("IDC_RADIO_ARC_CENTER");
                        break;
                    case DrawKind.ARC_3_PNT:
                        UIFrameworkServices.CommandHandlerService.invokeCommandHandler("IDC_RADIO_ARC_3_PNT");
                        break;
                    case DrawKind.ARC_TAN_END:
                        UIFrameworkServices.CommandHandlerService.invokeCommandHandler("IDC_RADIO_ARC_TAN_END");
                        break;
                    case DrawKind.FULL_ELLIPSE:
                        UIFrameworkServices.CommandHandlerService.invokeCommandHandler("IDC_RADIO_FULL_ELLIPSE");
                        break;
                    case DrawKind.PARTIAL_ELLIPSE:
                        UIFrameworkServices.CommandHandlerService.invokeCommandHandler("IDC_RADIO_PARTIAL_ELLIPSE");
                        break;
                    case DrawKind.SPLINE:
                        UIFrameworkServices.CommandHandlerService.invokeCommandHandler("IDC_RADIO_SPLINE");
                        break;
                    case DrawKind.COPY_CURVE:
                        UIFrameworkServices.CommandHandlerService.invokeCommandHandler("IDC_RADIO_COPY_CURVE");
                        break;
                    default:
                        break;
                }
            }
            catch
            {
                if (tr.HasStarted() == true)
                {
                    tr.Commit();//关闭已开启事务
                }
            }
        }

        /// <summary>
        /// （绘制详图线辅助线第二步方法，需要在 EventCommand 外部类中使用）得到上一步绘制的详图线信息并删除所绘制的详图线
        /// </summary>
        /// <param name="drawKind">辅助详图线样式</param>
        /// <param name="oldDetailCurveIds">绘制前项目中的详图线集合</param>
        /// <returns>返回绘制的详图线信息集合</returns>
        public IList<SketchObject> SketchCommandByDetailCurve_2(DrawKind drawKind, List<ElementId> oldDetailCurveIds)
        {
            //初始化返回的辅助线对象集合
            IList<SketchObject> sketchObjects = new List<SketchObject>();
            sketchObjects.Clear();

            //得到视图中的 DetailCurve
            CurveElementFilter filter = new CurveElementFilter(CurveElementType.DetailCurve);
            List<ElementId> newDetailCurveIds = new FilteredElementCollector(Doc, UIdoc.ActiveView.Id).WherePasses(filter).ToElementIds().ToList();

            Transaction tr = new Transaction(UIdoc.Document);

            try
            {
                #region 得到所绘制的详图线信息集合 sketchObjects

                //所绘制详图线集合
                IList<Element> sketchElemList = newDetailCurveIds.Except(oldDetailCurveIds).Select(o => Doc.GetElement(o)).ToList();
                sketchObjects.Clear();

                foreach (Element elem in sketchElemList)
                {
                    Curve cur = (elem as DetailCurve).GeometryCurve;
                    sketchObjects.Add(new SketchObject(elem, cur));
                }

                #endregion //得到所绘制的详图线信息集合 sketchObjects

                #region 删除详图线

                tr.Start("删除详图线");

                UIdoc.Document.Delete(sketchObjects.Select(o => o.Elem.Id).ToList());

                tr.Commit();

                #endregion //删除详图线
            }
            catch
            {
                if (tr.HasStarted() == true)
                {
                    tr.Commit();//关闭已开启事务
                }
            }
            return sketchObjects;
        }

        /// <summary>
        /// 绘制详图线辅助线绘制方式枚举
        /// </summary>
        public enum DrawKind
        {
            /// <summary>
            /// 直线
            /// </summary>
            LINE,
            /// <summary>
            /// 矩形
            /// </summary>
            RECT,

            /// <summary>
            /// 内接多边形
            /// </summary>
            INSCRIBED_POLYGON,
            /// <summary>
            /// 内接多边形
            /// </summary>
            CIRCUMSCRIBED_POLYGON,
            /// <summary>
            /// 圆
            /// </summary>
            CIRCLE,
            /// <summary>
            /// 圆角弧
            /// </summary>
            ARC_FILLET,


            /// <summary>
            /// 圆心端点弧
            /// </summary>
            ARC_CENTER,
            /// <summary>
            /// 起点终点半径弧
            /// </summary>
            ARC_3_PNT,
            /// <summary>
            /// 相切端点弧
            /// </summary>
            ARC_TAN_END,

            /// <summary>
            /// 椭圆
            /// </summary>
            FULL_ELLIPSE,
            /// <summary>
            /// 半椭圆
            /// </summary>
            PARTIAL_ELLIPSE,


            /// <summary>
            /// 样条曲线
            /// </summary>
            SPLINE,


            /// <summary>
            /// 拾取线
            /// </summary>
            COPY_CURVE,
        }

        #endregion //调用RVT内置绘制 DetailCurve 命令绘制各种样式的辅助线




        /// <summary>
        /// 使用PickBox实现绘制矩形辅助线命令
        /// </summary>
        /// <param name="desc">操作提示描述</param>
        /// <returns></returns>
        public IList<SketchObject> SketchCommandByPickBox(string desc)
        {
            //初始化返回的辅助线对象集合
            IList<SketchObject> sketchObjects = new List<SketchObject>();

            //实例化自建类库类
            MathsCalculation mc = new MathsCalculation();
            RevDocInfo rvtDocInfo = new RevDocInfo(UIdoc, Doc);

            //判断视图是否为非三维视图
            if (rvtDocInfo.CheckView(CheckViewSet.NoView3D, "在三维视图中无法操作！") == false) { return sketchObjects; }

            try
            {
                PickedBox pickedbox = UIdoc.Selection.PickBox(PickBoxStyle.Crossing, desc);

                XYZ min = pickedbox.Min;
                XYZ max = pickedbox.Max;

                IList<XYZ> PointList = new List<XYZ>() { };
                PointList.Add(min);
                PointList.Add(max);

                XYZ xyz_l = mc.GetExtremePointOnOri(PointList, UIdoc.ActiveView.RightDirection, ExtremeWay.Min);
                XYZ xyz_r = mc.GetExtremePointOnOri(PointList, UIdoc.ActiveView.RightDirection, ExtremeWay.Max);

                XYZ xyz_mid = new XYZ((min.X + max.X) / 2, (min.Y + max.Y) / 2, (min.Z + max.Z) / 2);

                double width = mc.ProjectionLineOnOri(Line.CreateBound(min, max), UIdoc.ActiveView.RightDirection).Length;
                double height = mc.ProjectionLineOnOri(Line.CreateBound(min, max), UIdoc.ActiveView.UpDirection).Length;

                XYZ xyz_tl = xyz_mid - UIdoc.ActiveView.RightDirection * width / 2 + UIdoc.ActiveView.UpDirection * height / 2;
                XYZ xyz_tr = xyz_mid + UIdoc.ActiveView.RightDirection * width / 2 + UIdoc.ActiveView.UpDirection * height / 2;
                XYZ xyz_bl = xyz_mid - UIdoc.ActiveView.RightDirection * width / 2 - UIdoc.ActiveView.UpDirection * height / 2;
                XYZ xyz_br = xyz_mid + UIdoc.ActiveView.RightDirection * width / 2 - UIdoc.ActiveView.UpDirection * height / 2;

                sketchObjects.Add(new SketchObject(xyz_mid, xyz_tl, xyz_tr, xyz_bl, xyz_br, width, height));
            }
            catch { }

            return sketchObjects;
        }

        /// <summary>
        ///  使用PickBox实现绘制矩形辅助线命令
        /// </summary>
        /// <param name="desc">操作提示描述</param>
        /// <param name="pickBoxStyle">PickBox类型</param>
        /// <returns></returns>
        public IList<SketchObject> SketchCommandByPickBox(string desc, PickBoxStyle pickBoxStyle)
        {
            //初始化返回的辅助线对象集合
            IList<SketchObject> sketchObjects = new List<SketchObject>();

            //实例化自建类库类
            MathsCalculation mc = new MathsCalculation();
            RevDocInfo rvtDocInfo = new RevDocInfo(UIdoc, Doc);

            //判断视图是否为非三维视图
            if (rvtDocInfo.CheckView(CheckViewSet.NoView3D, "在三维视图中无法操作！") == false) { return sketchObjects; }

            try
            {
                PickedBox pickedbox = UIdoc.Selection.PickBox(pickBoxStyle, desc);

                XYZ min = pickedbox.Min;
                XYZ max = pickedbox.Max;

                IList<XYZ> PointList = new List<XYZ>() { };
                PointList.Add(min);
                PointList.Add(max);

                XYZ xyz_l = mc.GetExtremePointOnOri(PointList, UIdoc.ActiveView.RightDirection, ExtremeWay.Min);
                XYZ xyz_r = mc.GetExtremePointOnOri(PointList, UIdoc.ActiveView.RightDirection, ExtremeWay.Max);

                XYZ xyz_mid = new XYZ((min.X + max.X) / 2, (min.Y + max.Y) / 2, (min.Z + max.Z) / 2);

                double width = mc.ProjectionLineOnOri(Line.CreateBound(min, max), UIdoc.ActiveView.RightDirection).Length;
                double height = mc.ProjectionLineOnOri(Line.CreateBound(min, max), UIdoc.ActiveView.UpDirection).Length;

                XYZ xyz_tl = xyz_mid - UIdoc.ActiveView.RightDirection * width / 2 + UIdoc.ActiveView.UpDirection * height / 2;
                XYZ xyz_tr = xyz_mid + UIdoc.ActiveView.RightDirection * width / 2 + UIdoc.ActiveView.UpDirection * height / 2;
                XYZ xyz_bl = xyz_mid - UIdoc.ActiveView.RightDirection * width / 2 - UIdoc.ActiveView.UpDirection * height / 2;
                XYZ xyz_br = xyz_mid + UIdoc.ActiveView.RightDirection * width / 2 - UIdoc.ActiveView.UpDirection * height / 2;

                sketchObjects.Add(new SketchObject(xyz_mid, xyz_tl, xyz_tr, xyz_bl, xyz_br, width, height));
            }
            catch { }

            return sketchObjects;
        }


        /// <summary>
        /// 使用PostableCommand.DetailLine实现绘制直线辅助线命令
        /// </summary>
        /// <param name="desc">操作提示描述</param>
        /// <returns></returns>
        public IList<SketchObject> SketchCommandByDetailLine(string desc)
        {
            //初始化返回的辅助线对象集合
            IList<SketchObject> sketchObjects = new List<SketchObject>();

            //实例化自建类库类
            RevDocInfo rvtDocInfo = new RevDocInfo(UIdoc, Doc);

            //判断视图是否为非三维视图
            if (rvtDocInfo.CheckView(CheckViewSet.NoView3D, "在三维视图中无法操作！") == false) { return sketchObjects; }

            try
            {
                #region 绘制详图线并得到详图线信息

                //绘制前详图线集合
                IList<Element> oldList = new FilteredElementCollector(UIdoc.Document, UIdoc.ActiveView.Id)
                                        .OfCategory(BuiltInCategory.OST_Lines).ToElements().Where(o => o.Name.ToString() == "详图线").ToList();

                //调用绘制详图线的内置命令
                UIapp.PostCommand(RevitCommandId.LookupPostableCommandId(PostableCommand.DetailLine));

                //绘制后详图线集合
                IList<Element> newList = new FilteredElementCollector(UIdoc.Document, UIdoc.ActiveView.Id)
                                        .OfCategory(BuiltInCategory.OST_Lines).ToElements().Where(o => o.Name.ToString() == "详图线").ToList();

                //所绘制详图线集合
                IList<Element> sketchElemList = newList.Except(oldList).ToList();

                foreach (Element elem in sketchElemList)
                {
                    Curve cur = (elem as DetailCurve).GeometryCurve;

                    sketchObjects.Add(new SketchObject(elem, cur));
                }

                #endregion //绘制详图线并得到详图线信息
            }
            catch { }

            Transaction tr = new Transaction(UIdoc.Document);

            #region 删除详图线

            try
            {
                tr.Start("删除详图线");

                List<ElementId> delList = new List<ElementId>();
                delList.AddRange(sketchObjects.Select(o => o.Elem.Id).ToList());

                UIdoc.Document.Delete(delList);

                tr.Commit();
            }
            catch
            {
                if (tr.HasStarted() == true)
                {
                    tr.Commit();//关闭已开启事务
                }
            }

            #endregion //删除详图线


            return sketchObjects;

        }

        #endregion //操作辅助线相关方法






        #region 尺寸标注相关方法


        /// <summary>
        /// 根据重复性和用户选择判断可以创建的尺寸标记集合（根据参考信息集合判断是否有重复标记,如果重复了，提示用户是否删除并重建，如果用户否，则不删除原标记且不创建新标记）,并返回需要创建的标记集合
        /// </summary>
        /// <param name="rvtDimList"></param>
        /// <returns></returns>
        public IList<RvtDimension> DimensionsCreateJudge(IList<RvtDimension> rvtDimList)
        {

            //初始化需要创建的尺寸标注集合
            IList<RvtDimension> creatList = new List<RvtDimension>();

            try
            {
                //记录用户选择提示是否已经出现，为了避免重复出现
                bool chooseTipHasShow = false;

                //记录用户的第一次选择
                UserChoose userChoose = UserChoose.Null;

                //记录需要删除的尺寸标注集合
                List<Dimension> delectDimList = new List<Dimension>();


                //得到当前视图下的所有线性尺寸标注
                List<Dimension> exsistDimList = new FilteredElementCollector(Doc).OfCategory(BuiltInCategory.OST_Dimensions)
                                               .Cast<Dimension>().Where(o => o.DimensionType.StyleType == DimensionStyleType.Linear).ToList();

                foreach (RvtDimension rvtDim in rvtDimList)
                {
                    //得到重复的对象
                    List<Dimension> RepeatDims = exsistDimList.Where(o => SameReferenceDim(new RvtDimension(o.Id.ToString(), o.View, o.Curve as Line, o.References, o.DimensionType), rvtDim)).ToList();

                    //存在重复对象
                    if (RepeatDims.Count > 0)
                    {
                        if (chooseTipHasShow == false)
                        {
                            if (MessageBox.Show("存在重复尺寸标注，是否覆盖?\n\n【是】: 删除原尺寸标注，并创建新的尺寸标注。\n【否】: 保留原尺寸标注，且不再重复创建。", "提示", MessageBoxButtons.YesNo) == DialogResult.Yes)
                            {
                                //用户选择覆盖 ，则 将已有重复对象加入删除集合、将该尺寸标注加入需要创建集合、记录用户的选择
                                delectDimList.AddRange(RepeatDims);

                                creatList.Add(rvtDim);

                                userChoose = UserChoose.Yes;

                            }
                            else
                            {
                                //用户选择覆盖，则保留原尺寸标注不删除，新的标注不加入创建集合、记录用户的选择
                                userChoose = UserChoose.No;
                            }

                            //记录窗体已经弹出，避免二次弹出
                            chooseTipHasShow = true;
                        }
                        else
                        {
                            if (userChoose == UserChoose.Yes)
                            {
                                //再次出现重复情况，根据用户上一次的选择，是则 ，将已有重复对象加入删除集合、将该尺寸标注加入需要创建集合
                                delectDimList.AddRange(RepeatDims);

                                creatList.Add(rvtDim);
                            }
                        }
                    }
                    else
                    {
                        //不存在重复对象直接加入需要创建集合
                        creatList.Add(rvtDim);
                    }
                }

                if (delectDimList.Count() > 0 && userChoose == UserChoose.Yes)
                {
                    Transaction tran = new Transaction(Doc);
                    tran.Start("删除重复尺寸标注");
                    Doc.Delete(delectDimList.Select(o => o.Id).ToList());
                    tran.Commit();
                }
            }
            catch
            {
                //MessageBox.Show("尺寸标注重复性判断出错！");
                return rvtDimList;
            }

            return creatList;
        }


        /// <summary>
        /// 创建单个尺寸标注对象(包含 标注去除0标段、标注文字自动避让)
        /// </summary>
        /// <param name="doc">文档</param>
        /// <param name="rvtDim">尺寸标注类对象</param>
        /// <param name="logFilePath">后台记录文档路径（文字避让使用）</param>
        /// <param name="logFileName">后台记录文档名称（文字避让使用）</param>
        /// <param name="transactionTip">事务组名称</param>
        /// <returns></returns>
        public Dimension CreateDimension(Document doc, RvtDimension rvtDim, string logFilePath, string logFileName, string transactionTip)
        {
            //创建事务和事务组
            TransactionGroup tsGrp = new TransactionGroup(doc, transactionTip);
            Transaction tr = new Transaction(doc);
            try
            {
                tsGrp.Start();

                tr.Start(transactionTip);

                Dimension dim = doc.Create.NewDimension(rvtDim.LocView, rvtDim.LocLine, rvtDim.RefArray, rvtDim.DimType);

                tr.Commit();

                //标注除0
                Dimension dim2 = RemoveDim0Seg(doc, dim);

                //标注文字自动避让
                DimensionLocAutoAvoid(dim2, logFilePath, logFileName);

                tsGrp.Assimilate();

                return dim2;
            }
            catch
            {
                if (tr.HasStarted()) { tr.Commit(); }
                if (tsGrp.HasStarted()) { tsGrp.Assimilate(); }
                //MessageBox.Show("尺寸标注创建失败！");
                return null;
            }

        }


        /// <summary>
        /// 创建多个尺寸标注对象(包含 标注去除0标段、标注文字自动避让)
        /// </summary>
        /// <param name="doc">文档</param>
        /// <param name="rvtDimList">尺寸标注类对象集合</param>
        /// <param name="logFilePath">后台记录文档路径（文字避让使用）</param>
        /// <param name="logFileName">后台记录文档名称（文字避让使用）</param>
        /// <param name="transactionTip">事务组名称</param>
        /// <returns></returns>
        public List<Dimension> CreateDimensions(Document doc, IList<RvtDimension> rvtDimList, string logFilePath, string logFileName, string transactionTip)
        {
            //创建事务和事务组
            TransactionGroup tsGrp = new TransactionGroup(doc, transactionTip);
            Transaction tr = new Transaction(doc);

            //创建返回集合
            List<Dimension> dimList = new List<Dimension>();

            try
            {
                tsGrp.Start();

                foreach (RvtDimension rvtDim in rvtDimList)
                {
                    tr.Start(transactionTip);

                    Dimension dim = doc.Create.NewDimension(rvtDim.LocView, rvtDim.LocLine, rvtDim.RefArray, rvtDim.DimType);

                    tr.Commit();

                    //标注除0
                    Dimension dim2 = RemoveDim0Seg(doc, dim);

                    dimList.Add(dim2);
                }

                //标注文字自动避让
                if (dimList.Count > 0) { DimensionLocAutoAvoid(dimList.Select(o => o as Element).ToList(), logFilePath, logFileName); }

                tsGrp.Assimilate();
            }
            catch/* (Exception ex)*/
            {
                if (tr.HasStarted()) { tr.Commit(); }
                if (tsGrp.HasStarted()) { tsGrp.Assimilate(); }
                MessageBox.Show("尺寸标注创建失败！");
                //MessageBox.Show(ex.ToString());
            }

            return dimList;
        }


        /// <summary>
        /// 尺寸标注去除0标段
        /// </summary>
        /// <param name="doc">文档</param>
        /// <param name="dim">尺寸标注对象</param>
        /// <returns></returns>
        public Dimension RemoveDim0Seg(Document doc, Dimension dim)
        {
            Transaction tr = new Transaction(doc);

            try
            {
                if (!(dim.Curve is Line)) { return dim; }
                if (dim.References.Size < 3) { return dim; }

                Line dimL = dim.Curve as Line;
                //初始化新的参考集合
                ReferenceArray newRefArray = new ReferenceArray();
                //加入第一个参考
                newRefArray.Append(dim.References.get_Item(0));
                for (int i = 1; i < dim.References.Size; i++)
                {
                    if (dim.Segments.get_Item(i - 1).ValueString != "0")
                    {
                        //加入非0标段参考
                        newRefArray.Append(dim.References.get_Item(i));
                    }
                }
                //不存在0标段，直接返回原尺寸标注
                if (newRefArray.Size == dim.References.Size)
                {
                    return dim;
                }
                //存在0标段，创建新的标注并删除原标注，返回新标注
                else
                {

                    tr.Start("尺寸标注去除0标段");

                    Dimension newdim = doc.Create.NewDimension(dim.View, dimL, newRefArray, dim.DimensionType);
                    doc.Delete(dim.Id);

                    tr.Commit();

                    return newdim;
                }
            }
            catch
            {
                if (tr.HasStarted()) { tr.Commit(); }
                //MessageBox.Show("尺寸标注去除0标段出错！");
                return dim;
            }
        }


        /// <summary>
        /// 多个尺寸标注文字自动避让(两段的侧位避让，多段的上下避让)包含对后台记录文档中标注字体信息的读取
        /// </summary>
        /// <param name="Dimensionlist"> 选取需要避让的尺寸标注对象集合</param>
        /// <param name="LogFilePath">后台记录xml文档路径</param>
        /// <param name="LogFileName">后台记录xml文档名称</param>
        public void DimensionLocAutoAvoid(IList<Element> Dimensionlist, string LogFilePath, string LogFileName)
        {
            //参数设置
            double AddDis = -999; //在1:100的视图比例下，移动开的两个文字之间的间隔距离,通过读取后台文档获得

            //字体类集合
            IList<DigitFont> DigitFontList = new List<DigitFont>(); //通过读取后台文档获得

            //读取后台文档并更新上述参数
            GetDimensionAvoidLog(LogFilePath, LogFileName, ref DigitFontList, ref AddDis);

            //创建事务
            Transaction ts = new Transaction(Doc, "尺寸标注文字避让");

            try
            {
                if (DigitFontList.Count() > 0 && AddDis != -999)
                {

                    //得到当前视图比例
                    int scale = UIdoc.ActiveView.get_Parameter(BuiltInParameter.VIEW_SCALE).AsInteger();

                    //根据视图比例调整移动开的两个文字之间的间隔距离
                    AddDis = AddDis / 100 * scale;

                    if (Dimensionlist.Count > 0)
                    {
                        //开启事务               
                        ts.Start();

                        foreach (Element e in Dimensionlist)
                        {
                            Dimension d = e as Dimension;

                            //标注自动避让
                            DimensionLocAvoid(scale, d, DigitFontList, AddDis);

                        }
                        //关闭
                        ts.Commit();
                    }
                }
                else
                {
                    MessageBox.Show("后台文件损坏，请联系相关开发人员！", "提示");
                }
            }
            catch
            {
                if (ts.HasStarted()) { ts.Commit(); }
                //MessageBox.Show("尺寸标注文字自调出错！");
            }
        }


        /// <summary>
        /// 单个尺寸标注文字自动避让(两段的侧位避让，多段的上下避让)包含对后台记录文档中标注字体信息的读取
        /// </summary>
        /// <param name="dimension"> 需要避让的尺寸标注对象</param>
        /// <param name="LogFilePath">后台记录xml文档路径</param>
        /// <param name="LogFileName">后台记录xml文档名称</param>
        public void DimensionLocAutoAvoid(Dimension dimension, string LogFilePath, string LogFileName)
        {
            //参数设置
            double AddDis = -999; //在1:100的视图比例下，移动开的两个文字之间的间隔距离,通过读取后台文档获得

            //字体类集合
            IList<DigitFont> DigitFontList = new List<DigitFont>(); //通过读取后台文档获得

            //读取后台文档并更新上述参数
            GetDimensionAvoidLog(LogFilePath, LogFileName, ref DigitFontList, ref AddDis);

            //创建事务
            Transaction ts = new Transaction(Doc, "尺寸标注文字避让");

            try
            {
                if (DigitFontList.Count() > 0 && AddDis != -999)
                {

                    //得到当前视图比例
                    int scale = UIdoc.ActiveView.get_Parameter(BuiltInParameter.VIEW_SCALE).AsInteger();

                    //根据视图比例调整移动开的两个文字之间的间隔距离
                    AddDis = AddDis / 100 * scale;

                    //开启事务               
                    ts.Start();

                    //标注自动避让
                    DimensionLocAvoid(scale, dimension, DigitFontList, AddDis);

                    //关闭
                    ts.Commit();

                }
                else
                {
                    MessageBox.Show("后台文件损坏，请联系相关开发人员！", "提示");
                }
            }
            catch
            {
                if (ts.HasStarted()) { ts.Commit(); }
                //MessageBox.Show("尺寸标注文字自调出错！");
            }
        }



        /// <summary>
        /// 循环操作选取，尺寸标注文字自动避让(两段的侧位避让，多段的上下避让)包含对后台记录文档中标注字体信息的读取
        /// </summary>
        /// <param name="LogFilePath">后台记录xml文档路径</param>
        /// <param name="LogFileName">后台记录xml文档名称</param>
        /// <param name="filter">选取对象过滤器</param>
        /// <param name="selDesc">选取描述</param>
        public void DimensionLocAutoAvoid(string LogFilePath, string LogFileName, ISelectionFilter filter, string selDesc)
        {
            //参数设置
            double AddDis = -999; //在1:100的视图比例下，移动开的两个文字之间的间隔距离,通过读取后台文档获得

            //字体类集合
            IList<DigitFont> DigitFontList = new List<DigitFont>(); //通过读取后台文档获得

            //读取后台文档并更新上述参数
            GetDimensionAvoidLog(LogFilePath, LogFileName, ref DigitFontList, ref AddDis);

            //创建事务
            Transaction ts = new Transaction(Doc, "尺寸标注文字避让");

            if (DigitFontList.Count() > 0 && AddDis != -999)
            {
                //得到当前视图比例
                int scale = UIdoc.ActiveView.get_Parameter(BuiltInParameter.VIEW_SCALE).AsInteger();

                //根据视图比例调整移动开的两个文字之间的间隔距离
                AddDis = AddDis / 100 * scale;

                bool cyle = true;//循环控制变量

                while (cyle)
                {
                    try
                    {
                        //框选需要操作的对象
                        IList<Element> Dimensionlist = UIdoc.Selection.PickElementsByRectangle(filter, selDesc);

                        if (Dimensionlist.Count > 0)
                        {
                            //开启事务               
                            ts.Start();

                            foreach (Element e in Dimensionlist)
                            {
                                Dimension d = e as Dimension;

                                //标注自动避让
                                DimensionLocAvoid(scale, d, DigitFontList, AddDis);
                            }

                            ts.Commit();
                        }
                    }
                    catch
                    //catch (Exception ex)
                    {
                        //MessageBox.Show(ex.ToString());
                        if (ts.HasStarted() == true)
                        {
                            ts.Commit();
                        }
                        cyle = false;
                    }
                }

                return;
            }
            else
            {
                MessageBox.Show("后台文件损坏，请联系相关开发人员！", "提示");
                return;
            }
        }



        #region 尺寸标注避让辅助方法

        /// <summary>
        /// 读取后台记录文件并更新所需参数 AddDis 和 DigitFontList
        /// </summary>
        /// <param name="LogFilePath">后台记录xml文档路径</param>
        /// <param name="LogFileName">后台记录xml文档名称</param>
        /// <param name="DigitFontList">返回的字体类集合</param>
        /// <param name="AddDis">返回的移动开的两个文字之间的间隔距离（在1:100的视图比例下）</param>
        private void GetDimensionAvoidLog(string LogFilePath, string LogFileName, ref IList<DigitFont> DigitFontList, ref double AddDis)
        {
            try
            {
                //读取XML中 PlugInLog/DimensionLocAutoAvoid 节点下名称为 AddDis 的节点的 InnerText
                AddDis = Convert.ToDouble(GetAllChildNode(LogFilePath, LogFileName, "PlugInLog/DimensionLocAutoAvoid")
                         .Where(o => o.Name == "AddDis").First().InnerText) / 304.8;

                //读取XML中所有 PlugInLog/DimensionFont 下的节点
                List<XmlNode> readList = GetAllChildNode(LogFilePath, LogFileName, "PlugInLog/DimensionFont");
                foreach (XmlNode xn in readList)
                {
                    if (xn.Name == "Font")
                    {
                        //读取该节点下的所有带 InnerText 的子节点名称及其 InnerText
                        Dictionary<string, string> list = ReadChildElement(xn);
                        string name = list.First(o => o.Key == "Name").Value;
                        double size = Convert.ToDouble(list.First(o => o.Key == "Size").Value);
                        double widthFactor = Convert.ToDouble(list.First(o => o.Key == "WidthFactor").Value);
                        double width = Convert.ToDouble(list.First(o => o.Key == "Width").Value);
                        double hight = Convert.ToDouble(list.First(o => o.Key == "Hight").Value);
                        double offsetToTextPosition = Convert.ToDouble(list.First(o => o.Key == "OffsetToTextPosition").Value);

                        DigitFontList.Add(new DigitFont(name, size, widthFactor, width, hight, offsetToTextPosition));
                    }
                }
            }
            catch
            {
                MessageBox.Show("后台文件已损坏，请联系相关开发人员！", "提示");
                return;
            }
        }


        /// <summary>
        /// 标注自动避让(两段的侧位避让，多段的上下避让)
        /// </summary>
        /// <param name="scale"></param>
        /// <param name="d"></param>
        /// <param name="DigitFontList"></param>
        /// <param name="AddDis"></param>
        private void DimensionLocAvoid(double scale, Dimension d, IList<DigitFont> DigitFontList, double AddDis)
        {
            #region 得到标注字体名称  string FontName ,字体大小 double FontSize ， 宽度系数 double WidthFactor

            string FontName = null;
            double FontSize = 0;
            double WidthFactor = 0;

            foreach (Parameter p in (Doc.GetElement(d.GetTypeId()) as DimensionType).Parameters)
            {
                if (p.Definition.Name == "文字字体")
                {
                    FontName = p.AsString();
                }
                if (p.Definition.Name == "文字大小")
                {
                    try
                    {
                        FontSize = Convert.ToDouble(p.AsValueString().Split('m').First());
                    }
                    catch
                    {
                        FontSize = 0;
                    }
                }
                if (p.Definition.Name == "宽度系数")
                {
                    WidthFactor = p.AsDouble();
                }
            }

            if (FontName == null) { return; }

            #endregion //得到标注字体名称  string FontName ,字体大小 double FontSize 和 宽度系数 double WidthFactor

            #region 得到标注方向 XYZ Ori

            //得到标注方向
            XYZ Ori = XYZ.BasisX;
            Line line = d.Curve as Line;
            if (line.IsBound == false)
            {
                Ori = line.Direction;
            }
            else
            {
                Ori = (line.GetEndPoint(1) - line.GetEndPoint(0)).Normalize();
            }

            #endregion//得到标注方向 XYZ Ori

            #region 去掉引线

            Parameter para = d.get_Parameter(BuiltInParameter.DIM_LEADER);
            if (para != null)
            {
                para.Set(0);
            }

            #endregion //去掉引线

            //得到对应的数字字体类对象
            DigitFont df = DigitFontList.Where(o => o.Name == FontName && o.Size == FontSize && o.WidthFactor == WidthFactor).FirstOrDefault();

            #region 重置文本位置

            if (d.Segments.Size > 1)
            {
                foreach (DimensionSegment s in d.Segments)
                {
                    s.ResetTextPosition();
                }
            }
            else
            {
                d.ResetTextPosition();
            }

            #endregion //重置文本位置

            if (df != null)
            {
                #region 调整重合文字

                if (d.Segments.Size == 2)
                {
                    #region 两段的尺寸标注

                    DimensionSegment seg1 = d.Segments.get_Item(0);
                    DimensionSegment seg2 = d.Segments.get_Item(1);

                    //得到标段字符串
                    string s1 = seg1.ValueString;
                    string s2 = seg2.ValueString;

                    //得到每个字符串宽度
                    double w1 = s1.Length * df.Width;
                    double w2 = s2.Length * df.Width;

                    //得到当前两个字符串的重合长度
                    double preDis = ((w1 + w2) / 2 / 304.8 / 100 * scale) - seg1.TextPosition.DistanceTo(seg2.TextPosition);


                    if (preDis > 0)
                    {
                        //移动文字
                        seg1.TextPosition = seg1.TextPosition - Ori * (preDis + AddDis) / 2;
                        seg2.TextPosition = seg2.TextPosition + Ori * (preDis + AddDis) / 2;

                    }

                    #endregion //两段的尺寸标注
                }
                else if (d.Segments.Size > 2)
                {
                    #region 多段尺寸标注

                    //得到文字高度
                    double h = df.Hight / 304.8 / 100 * scale;

                    //得到文字底部边缘距离文字位置点(TextPosition)的距离偏移
                    double offsetToTextPosition = df.OffsetToTextPosition / 304.8 / 100 * scale;

                    //得到文字底部边缘距离标注线的距离偏移
                    double offsetToLine = offsetToTextPosition + d.Curve.Distance(d.Segments.get_Item(0).TextPosition);

                    //得到标注指向标注文字的方向
                    XYZ nor = (d.Segments.get_Item(0).TextPosition - d.Curve.Project(d.Segments.get_Item(0).TextPosition).XYZPoint).Normalize();

                    #region 记录标段信息和重合情况  IList<Segment> SegmentList

                    //记录每段标注的信息和重合情况
                    IList<DimSegment> SegmentList = new List<DimSegment>();

                    for (int i = 0; i < d.Segments.Size; i++)
                    {
                        DimensionSegment seg = d.Segments.get_Item(i);

                        if (i == 0)
                        {
                            #region 第一段要讨论与它后一段的文字是否有重叠,并记录标段信息和重合情况

                            DimensionSegment seg2 = d.Segments.get_Item(i + 1);

                            //得到标段字符串
                            string s1 = seg.ValueString;
                            string s2 = seg2.ValueString;

                            //得到每个字符串宽度
                            double w1 = s1.Length * df.Width / 304.8 / 100 * scale;
                            double w2 = s2.Length * df.Width / 304.8 / 100 * scale;

                            //得到当前两个字符串的重合长度
                            double overDisToAfter1 = (w1 + w2) / 2 - seg.TextPosition.DistanceTo(seg2.TextPosition);

                            SegmentList.Add(new DimSegment(i,
                                                          w1,
                                                          seg.TextPosition + nor * (df.OffsetToTextPosition / 304.8 / 100 * scale),
                                                          -999,
                                                          -999,
                                                          overDisToAfter1,
                                                          null));

                            #endregion //第一段要讨论与它后一段的文字是否有重叠,并记录标段信息和重合情况
                        }
                        else
                        {
                            #region 非第一段要讨论与它前两段的文字是否有重叠,并记录标段信息和重合情况

                            DimensionSegment seg2 = d.Segments.get_Item(i - 1);

                            //得到标段字符串
                            string s1 = seg.ValueString;
                            string s2 = seg2.ValueString;

                            //得到每个字符串宽度
                            double w1 = s1.Length * df.Width / 304.8 / 100 * scale;
                            double w2 = s2.Length * df.Width / 304.8 / 100 * scale;

                            //得到与前一个字符串的重合长度
                            double overDisToAfter1 = (w1 + w2) / 2 - seg.TextPosition.DistanceTo(seg2.TextPosition);

                            //记录与前一个的前一个字符串的重合长度
                            double overDisToAfter2 = -999;
                            if (i > 1)
                            {
                                DimensionSegment seg3 = d.Segments.get_Item(i - 2);
                                //得到标段字符串
                                string s3 = seg3.ValueString;
                                //得到字符串宽度
                                double w3 = s3.Length * df.Width / 304.8 / 100 * scale;

                                //得到与前一个的前一个字符串的重合长度
                                overDisToAfter2 = (w1 + w3) / 2 - seg.TextPosition.DistanceTo(seg3.TextPosition);
                            }

                            SegmentList.Add(new DimSegment(i,
                                                          w1,
                                                          seg.TextPosition + nor * (df.OffsetToTextPosition / 304.8 / 100 * scale),
                                                          overDisToAfter1,
                                                          overDisToAfter2,
                                                          -999,
                                                          null));

                            #endregion //非第一段要讨论与它前两段的文字是否有重叠,并记录标段信息和重合情况
                        }

                    }

                    #endregion //记录标段信息和重合情况  IList<Segment> SegmentList

                    #region 根据上述集合 SegmentList 中记录的情况，对每段文字进行移动处理

                    for (int i = 0; i < d.Segments.Size; i++)
                    {
                        DimensionSegment seg = d.Segments.get_Item(i);

                        if (i == 0)
                        {
                            #region 第一段要横向侧位偏移

                            if (SegmentList.Where(o => o.Num == i).First().OverDisToAfter1 > 0)
                            {

                                //得到需要侧位移动的距离
                                double offset = SegmentList.Where(o => o.Num == i).First().OverDisToAfter1 + AddDis;

                                //向标注起始端侧位移动
                                seg.TextPosition = seg.TextPosition - Ori * offset;

                                //记录移动方式
                                SegmentList.Where(o => o.Num == i).First().MoveWay = "前";

                            }

                            #endregion //第一段要横向侧位偏移
                        }
                        else if (i == 1)
                        {
                            #region 第二段一般不移动，因为与第一段重合时候是第一段向前移动，与第一段不重合也不需要移动

                            //记录移动方式
                            SegmentList.Where(o => o.Num == i).First().MoveWay = null;

                            #endregion //第二段一般不移动，因为与第一段重合时候是第一段向前移动，与第一段不重合也不需要移动
                        }
                        else if (i != d.Segments.Size - 1)
                        {
                            #region 第二段以后的中间段直接向标注方向垂直方向偏移(可能上移也可能下移，分多种情况讨论)

                            //只有在与前一个有重合的情况下才选择性移动
                            if (SegmentList.Where(o => o.Num == i).First().OverDisToBefore1 > 0)
                            {
                                if (SegmentList.Where(o => o.Num == i - 1).First().MoveWay == null)
                                {
                                    #region 前一个不移动

                                    if (SegmentList.Where(o => o.Num == i).First().OverDisToBefore2 > 0)
                                    {
                                        #region 与前前个有重合

                                        //前前个不移动
                                        if (SegmentList.Where(o => o.Num == i - 2).First().MoveWay == null)
                                        {
                                            //这种情况应该是不存在
                                            //上移
                                            d.Segments.get_Item(i).TextPosition = seg.TextPosition + nor * (h + AddDis);
                                            //记录移动方式
                                            SegmentList.Where(o => o.Num == i).First().MoveWay = "上";

                                        }
                                        //前前个上移
                                        else if (SegmentList.Where(o => o.Num == i - 2).First().MoveWay == "上")
                                        {
                                            //下移
                                            d.Segments.get_Item(i).TextPosition = seg.TextPosition - nor * (h + offsetToLine + AddDis);
                                            //记录移动方式
                                            SegmentList.Where(o => o.Num == i).First().MoveWay = "下";
                                        }
                                        //前前个下移
                                        else if (SegmentList.Where(o => o.Num == i - 2).First().MoveWay == "下")
                                        {
                                            //上移
                                            d.Segments.get_Item(i).TextPosition = seg.TextPosition + nor * (h + AddDis);
                                            //记录移动方式
                                            SegmentList.Where(o => o.Num == i).First().MoveWay = "上";
                                        }

                                        //前前个前移
                                        else if (SegmentList.Where(o => o.Num == i - 2).First().MoveWay == "前")
                                        {
                                            //上移
                                            d.Segments.get_Item(i).TextPosition = seg.TextPosition + nor * (h + AddDis);
                                            //记录移动方式
                                            SegmentList.Where(o => o.Num == i).First().MoveWay = "上";
                                        }

                                        #endregion //与前前个有重合
                                    }
                                    else
                                    {
                                        #region 与前前个无重合

                                        //上移
                                        d.Segments.get_Item(i).TextPosition = seg.TextPosition + nor * (h + AddDis);
                                        //记录移动方式
                                        SegmentList.Where(o => o.Num == i).First().MoveWay = "上";

                                        #endregion //与前前个无重合
                                    }

                                    #endregion //前一个不移动
                                }
                                else if (SegmentList.Where(o => o.Num == i - 1).First().MoveWay == "上")
                                {
                                    #region 前一个上移

                                    if (SegmentList.Where(o => o.Num == i).First().OverDisToBefore2 > 0)
                                    {
                                        #region 与前前个有重合

                                        //前前个不移动
                                        if (SegmentList.Where(o => o.Num == i - 2).First().MoveWay == null)
                                        {
                                            //下移
                                            d.Segments.get_Item(i).TextPosition = seg.TextPosition - nor * (h + offsetToLine + AddDis);
                                            //记录移动方式
                                            SegmentList.Where(o => o.Num == i).First().MoveWay = "下";
                                        }
                                        //前前个上移
                                        else if (SegmentList.Where(o => o.Num == i - 2).First().MoveWay == "上")
                                        {
                                            //这种情况应该是不存在
                                            //不移动
                                            //记录移动方式
                                            SegmentList.Where(o => o.Num == i).First().MoveWay = null;
                                        }
                                        //前前个下移
                                        else if (SegmentList.Where(o => o.Num == i - 2).First().MoveWay == "下")
                                        {
                                            //不移动
                                            //记录移动方式
                                            SegmentList.Where(o => o.Num == i).First().MoveWay = null;
                                        }

                                        #endregion //与前前个有重合
                                    }
                                    else
                                    {
                                        #region 与前前个无重合

                                        //不移动
                                        //记录移动方式
                                        SegmentList.Where(o => o.Num == i).First().MoveWay = null;

                                        #endregion //与前前个无重合
                                    }

                                    #endregion //前一个上移
                                }
                                else if (SegmentList.Where(o => o.Num == i - 1).First().MoveWay == "下")
                                {
                                    #region 前一个下移

                                    if (SegmentList.Where(o => o.Num == i).First().OverDisToBefore2 > 0)
                                    {
                                        #region 与前前个有重合

                                        //前前个不移动
                                        if (SegmentList.Where(o => o.Num == i - 2).First().MoveWay == null)
                                        {
                                            //上移
                                            d.Segments.get_Item(i).TextPosition = seg.TextPosition + nor * (h + AddDis);
                                            //记录移动方式
                                            SegmentList.Where(o => o.Num == i).First().MoveWay = "上";
                                        }
                                        //前前个上移
                                        else if (SegmentList.Where(o => o.Num == i - 2).First().MoveWay == "上")
                                        {
                                            //不移动
                                            //记录移动方式
                                            SegmentList.Where(o => o.Num == i).First().MoveWay = null;
                                        }
                                        //前前个下移
                                        else if (SegmentList.Where(o => o.Num == i - 2).First().MoveWay == "下")
                                        {
                                            //这种情况应该是不存在
                                            //不移动
                                            //记录移动方式
                                            SegmentList.Where(o => o.Num == i).First().MoveWay = null;
                                        }

                                        #endregion //与前前个有重合
                                    }
                                    else
                                    {
                                        #region 与前前个无重合

                                        //不移动
                                        //记录移动方式
                                        SegmentList.Where(o => o.Num == i).First().MoveWay = null;

                                        #endregion //与前前个无重合
                                    }

                                    #endregion //前一个下移
                                }
                            }

                            #endregion //第二段以后的中间段直接向标注方向垂直方向偏移(可能上移也可能下移，分多种情况讨论)
                        }
                        else
                        {
                            #region 最后一段要横向侧位偏移,且只有在前一个没有移动并有重合的情况下才需要偏移

                            if (SegmentList.Where(o => o.Num == i).First().OverDisToBefore1 > 0 && SegmentList.Where(o => o.Num == i - 1).First().MoveWay == null)
                            {
                                //得到需要侧位移动的距离
                                double offset = SegmentList.Where(o => o.Num == i).First().OverDisToBefore1 + AddDis;

                                //向标注结尾端侧位移动
                                seg.TextPosition = seg.TextPosition + Ori * offset;

                                //记录移动方式
                                SegmentList.Where(o => o.Num == i).First().MoveWay = "后";
                            }

                            #endregion //最后一段要横向侧位偏移,且只有在前一个没有移动并有重合的情况下才需要偏移
                        }
                    }

                    #endregion //根据上述集合 SegmentList 中记录的情况，对每段文字进行移动处理

                    #endregion //多段尺寸标注
                }

                #endregion //调整重合文字
            }
            else
            {
                MessageBox.Show("缺少该尺寸标注中字体(" + FontName + ")的信息,请联系相关开发人员进行补充！", "提示");
                return;
            }
        }

        #endregion //尺寸标注避让辅助方法



        /// <summary>
        /// 判断尺寸标注是否等同(位置线和类型可以不相同)，用来判断尺寸标注是否重复
        /// </summary>
        /// <param name="dim1">RvtDimension尺寸标注1</param>
        /// <param name="dim2">RvtDimension尺寸标注2</param>
        /// <returns></returns>
        public bool SameReferenceDim(RvtDimension dim1, RvtDimension dim2)
        {
            List<string> RefStrList1 = new List<string>();
            List<string> RefStrList2 = new List<string>();
            foreach (Reference r in dim1.RefArray)
            {
                RefStrList1.Add(r.ConvertToStableRepresentation(Doc));
            }
            foreach (Reference r in dim2.RefArray)
            {
                RefStrList2.Add(r.ConvertToStableRepresentation(Doc));
            }

            if (RefStrList1.Count != 0 && RefStrList2.Count != 0)
            {
                if (RefStrList1.Count == RefStrList2.Count &&
                    dim1.LocView.Id.ToString() == dim2.LocView.Id.ToString() &&
                    (dim1.LocLine.Direction.IsAlmostEqualTo(dim1.LocLine.Direction, 1e-6) || dim1.LocLine.Direction.IsAlmostEqualTo(dim1.LocLine.Direction.Negate(), 1e-6)))
                {
                    if (RefStrList1.All(o => RefStrList2.Contains(o)) && RefStrList2.All(o => RefStrList1.Contains(o)))
                    {
                        return true;
                    }
                }
            }
            return false;
        }


        /// <summary>
        /// 从点的引用字典集合中得到两个极值边界点的引用（上下或者左右），根据尺寸标注布置位置（上、下、左、右）,适用于所有非三维视图
        /// </summary>
        /// <param name="pointRefDic">已知点的引用字典集合</param>
        /// <param name="dimLocOri">尺寸标注布置位置（上、下、左、右）</param>
        /// <param name="ref_1">左极值点引用（布置位置为上或者下）或者 上极值点引用（布置位置为左或者右）</param>
        /// <param name="ref_2">右极值点引用（布置位置为上或者下）或者 下极值点引用（布置位置为左或者右）</param>
        public void Get2ExPointsRef(IDictionary<XYZ, Reference> pointRefDic, DimLocOri dimLocOri, ref Reference ref_1, ref Reference ref_2)
        {
            MathsCalculation mc = new MathsCalculation();

            try
            {
                if (pointRefDic.Count > 0)
                {
                    //得到视图方向
                    XYZ view_r = Doc.ActiveView.RightDirection;
                    XYZ view_u = Doc.ActiveView.UpDirection;

                    //如果布置标注在上或者下侧，则取得左右极值点引用
                    if (dimLocOri == DimLocOri.Top || dimLocOri == DimLocOri.Bottom)
                    {
                        //得到左右极值点坐标
                        XYZ exP_l = mc.GetExtremePointOnOri(pointRefDic.Keys.ToList(), view_r, ExtremeWay.Min);
                        XYZ exP_r = mc.GetExtremePointOnOri(pointRefDic.Keys.ToList(), view_r, ExtremeWay.Max);

                        //得到所有左极值点集合
                        IList<XYZ> onLinePonits_l = mc.GetOnLinePointsOnView(exP_l, Doc.ActiveView, view_u, pointRefDic.Keys.ToList(), 1e-6);
                        onLinePonits_l.Add(exP_l);

                        //得到所有右极值点集合
                        IList<XYZ> onLinePonits_r = mc.GetOnLinePointsOnView(exP_r, Doc.ActiveView, view_u, pointRefDic.Keys.ToList(), 1e-6);
                        onLinePonits_r.Add(exP_r);

                        //如果标注在上侧，则参考的左右边界点应为最上侧点，即视图中纵坐标最大
                        if (dimLocOri == DimLocOri.Top)
                        {
                            //最左点对应的引用（视图最上侧）                            
                            ref_1 = pointRefDic[mc.GetExtremePointOnOri(onLinePonits_l, view_u, ExtremeWay.Max)];
                            //最右点对应的引用（视图最上侧）
                            ref_2 = pointRefDic[mc.GetExtremePointOnOri(onLinePonits_r, view_u, ExtremeWay.Max)];
                        }
                        //如果标注在下侧，则参考的左右边界点应为最下侧点，即视图中纵坐标最小
                        else
                        {
                            //最左点对应的引用（视图最下侧）
                            ref_1 = pointRefDic[mc.GetExtremePointOnOri(onLinePonits_l, view_u, ExtremeWay.Min)];
                            //最右点对应的引用（视图最下侧）
                            ref_2 = pointRefDic[mc.GetExtremePointOnOri(onLinePonits_r, view_u, ExtremeWay.Min)];
                        }
                    }
                    //如果布置标注在左或者右侧，则取得上下极值点引用
                    else if (dimLocOri == DimLocOri.Left || dimLocOri == DimLocOri.Right)
                    {
                        //得到上下极值点坐标
                        XYZ exP_t = mc.GetExtremePointOnOri(pointRefDic.Keys.ToList(), view_u, ExtremeWay.Max);
                        XYZ exP_b = mc.GetExtremePointOnOri(pointRefDic.Keys.ToList(), view_u, ExtremeWay.Min);

                        //得到所有上极值点集合
                        IList<XYZ> onLinePonits_t = mc.GetOnLinePointsOnView(exP_t, Doc.ActiveView, view_r, pointRefDic.Keys.ToList(), 1e-6);
                        onLinePonits_t.Add(exP_t);

                        //得到所有下极值点集合
                        IList<XYZ> onLinePonits_b = mc.GetOnLinePointsOnView(exP_b, Doc.ActiveView, view_r, pointRefDic.Keys.ToList(), 1e-6);
                        onLinePonits_b.Add(exP_b);

                        //如果标注在左侧，则参考的上下边界点应为最左侧点，即视图中横坐标最小
                        if (dimLocOri == DimLocOri.Left)
                        {
                            //最上点对应的引用（视图最左侧）
                            ref_1 = pointRefDic[mc.GetExtremePointOnOri(onLinePonits_t, view_r, ExtremeWay.Min)];
                            //最下点对应的引用（视图最左侧）
                            ref_2 = pointRefDic[mc.GetExtremePointOnOri(onLinePonits_b, view_r, ExtremeWay.Min)];
                        }
                        //如果标注在右侧，则参考的上下边界点应为最右侧点，即视图中横坐标最大
                        else
                        {
                            //最上点对应的引用（视图最右侧）
                            ref_1 = pointRefDic[mc.GetExtremePointOnOri(onLinePonits_t, view_r, ExtremeWay.Max)];
                            //最下点对应的引用（视图最右侧）
                            ref_2 = pointRefDic[mc.GetExtremePointOnOri(onLinePonits_b, view_r, ExtremeWay.Max)];
                        }
                    }
                    else { }
                }
            }
            catch
            //catch (Exception ex)
            {
                //MessageBox.Show(ex.ToString());
            }
        }


        //得到上下边界点的引用


        #region 相关枚举

        /// <summary>
        /// 在判断尺寸标注是否重复并返回创建集合的方法（DimensionsCreateJudge）中，记录用户在弹出提示窗体后的选择
        /// </summary>
        public enum UserChoose
        {
            //未选择
            Null,
            //覆盖并删除重复对象
            Yes,
            //忽略重复对象且不创建相应的新对象
            No
        }

        /// <summary>
        /// 尺寸标注方位
        /// </summary>
        public enum DimLocOri
        {
            Top,
            Bottom,
            Left,
            Right
        }

        #endregion //相关枚举



        #endregion //尺寸标注相关方法







        #region 构件标记相关方法

        #region 根据标记设置窗体布置各种类型构件标记


        /// <summary>
        /// 批量布置构件标记(直线引线，折线引线或者无引线)，根据 exsistSet 讨论处理已经存在的标记，读取后台记录的标记设置信息（基于点和基于线主体都布置在主体当前视图下的几何中心点）
        /// </summary>
        /// <param name="collection">输入集合（对象可以为IndependentTag 或者标记主体）</param>
        /// <param name="oldTagCategory">已有标记类型（用于记录原始标记）</param>
        /// <param name="fs">标记族类型</param>
        /// <param name="exsistSet">标记设置：忽略原始标记、重置位置不变、完全重置</param>
        /// <param name="arrangeWay">引线方向</param>
        /// <param name="Xori">布置右方向（相对方向的时候是沿着构件方向）</param>
        /// <param name="Yori">布置左方向（相对方向的时候是沿着构件方向）</param>
        /// <param name="filePath">后台文档路径</param>
        /// <param name="fileName">后台文档名称</param>
        /// <returns></returns>
        public IList<IndependentTag> CreateIndependentTags(IList<Element> collection, BuiltInCategory oldTagCategory, FamilySymbol fs, ExsistTagSet exsistSet,
                                                           ArrangeWay arrangeWay, XYZ Xori, XYZ Yori, string filePath, string fileName)
        {
            //设置事物
            Transaction tr = new Transaction(Doc);

            //初始化返回的标记集合
            IList<IndependentTag> returnTags = new List<IndependentTag>();

            //从后台文档中得到该族类型的标记设置
            TagSetFormPara tagSetFrmPara = GetTagSetParaFromXML(filePath, fileName, fs);

            //得到当前视图比例
            int scale = UIdoc.ActiveView.get_Parameter(BuiltInParameter.VIEW_SCALE).AsInteger();

            try
            {
                //记录已有标记集合
                List<OldTag> oldTags = new List<OldTag>();


                //布置不带引线的标记，得到新创建的标记集合 newTags 和 已有标记集合 oldTags
                bool addleader = true;
                if (arrangeWay == ArrangeWay.NoLeader || tagSetFrmPara.HasLeader == false) { addleader = false; }
                IList<IndependentTag> newTags = CreateIndependentTags(collection, oldTagCategory, tagSetFrmPara.Tagmode, tagSetFrmPara.TagOri, addleader, fs, exsistSet, ref oldTags);


                //得到重建的标记集合
                IList<IndependentTag> ReSetTags = newTags.Where(o => oldTags.Select(t => t.HostId).ToList().Contains(o.TaggedLocalElementId)).ToList();

                //得到纯新建的标记集合
                IList<IndependentTag> creatTags = newTags.Except(ReSetTags).ToList();

                #region 操作新建对象

                foreach (IndependentTag idt in creatTags)
                {
                    //设置标记引线
                    ChangeTagLeader(idt, scale, arrangeWay, Xori, Yori, tagSetFrmPara);
                    //加入返回集合
                    returnTags.Add(idt);
                }

                #endregion //操作新建对象

                #region 操作重建对象   

                if (exsistSet == ExsistTagSet.CompletelyReset)
                {
                    foreach (IndependentTag idt in ReSetTags)
                    {
                        //设置标记引线
                        ChangeTagLeader(idt, scale, arrangeWay, Xori, Yori, tagSetFrmPara);
                        //加入返回集合
                        returnTags.Add(idt);
                    }
                }
                else if (exsistSet == ExsistTagSet.KeepLoc)
                {
                    foreach (IndependentTag idt in ReSetTags)
                    {
                        //得到原有标记
                        OldTag oldTag = oldTags.Where(o => o.HostId == idt.TaggedLocalElementId).First();
                        //根据原标记设置标记引线
                        if (addleader == true && oldTag.HasLeader) { ChangeTagLeader(idt, oldTag.TagHeadPosition, oldTag.LeaderEnd, oldTag.LeaderElbow); }
                        //根据原标记不设置引线
                        else
                        {
                            tr.Start("按照原位置重设标记位置");
                            idt.HasLeader = false; idt.TagHeadPosition = oldTag.TagHeadPosition;
                            tr.Commit();
                        }

                        //加入返回集合
                        returnTags.Add(idt);
                    }
                }
                else { }

                #endregion //操作重建对象

            }
            catch/* (Exception ex)*/
            {
                if (tr.HasStarted()) { tr.Commit(); }
                //MessageBox.Show(ex.ToString());
            }

            return returnTags;

        }


        /// <summary>
        /// 批量布置平法标记(直线引线，或者无引线)，根据 exsistSet 讨论处理已经存在的标记（基于点和基于线主体都布置在主体当前视图下的几何中心点）
        /// </summary>
        /// <param name="collection">输入集合（对象可以为IndependentTag 或者标记主体）</param>
        /// <param name="oldTagCategory">已有标记类型（用于记录原始标记）</param>
        /// <param name="tagMode">标记模式</param>
        /// <param name="tagorn">标记方向：横向/竖向</param>
        /// <param name="fs">标记族类型</param>
        /// <param name="exsistSet">标记设置：忽略原始标记、重置位置不变、完全重置</param>
        /// <param name="arrangeWay">引线方向</param>
        /// <param name="pmethodTagSetFrmPara">平法标记设置类</param>
        /// <returns></returns>
        public IList<IndependentTag> CreatePmethodTags(IList<Element> collection, BuiltInCategory oldTagCategory, TagMode tagMode, TagOrientation tagorn,
                                                       FamilySymbol fs, ExsistTagSet exsistSet, PmethodTagSetFormPara pmethodTagSetFrmPara,
                                                       string leaderFN, string leaderFSN, string leaderFamilyFilePath, BuiltInCategory leaderCategory)
        {
            //设置事物
            Transaction tr = new Transaction(Doc);

            //初始化返回的标记集合
            IList<IndependentTag> returnTags = new List<IndependentTag>();

            #region 载入本地引线族并得到引线族类型 leaderFs

            //实例化自建类库类
            DownLoadFam dlf = new DownLoadFam();

            //载入族判定
            bool success = false;
            //载入辅助线族
            Family fam = dlf.LoadGetFamily(UIdoc.Document, leaderFN, leaderFamilyFilePath, ref success, leaderCategory);

            if (success == false)
            {
                MessageBox.Show("找不到引线族");
                return returnTags;
            }

            #region 激活辅助线族类型

            //得到族类型
            FamilySymbol leaderFs = new FilteredElementCollector(UIdoc.Document).OfClass(typeof(FamilySymbol)).OfCategory(leaderCategory)
                                    .Cast<FamilySymbol>().ToList().Where(o => o.FamilyName == leaderFN && o.Name == leaderFSN).FirstOrDefault();

            if (leaderFs == null)
            {
                MessageBox.Show("找不到引线族类型");
                return returnTags;
            }
            if (leaderFs.IsActive == false)
            {
                tr.Start("激活引线族类型");

                leaderFs.Activate();

                tr.Commit();
            }

            #endregion //激活辅助线族类型

            #endregion //载入本地引线族并得到引线族类型 leaderFs

            try
            {
                //记录已有标记集合
                List<OldTag> oldTags = new List<OldTag>();

                //布置不带引线的标记，得到新创建的标记集合 newTags 和 已有标记集合 oldTags
                IList<IndependentTag> newTags = CreateIndependentTags(collection, oldTagCategory, tagMode, tagorn, false, fs, exsistSet, ref oldTags);

                //得到重建的标记集合
                IList<IndependentTag> ReSetTags = newTags.Where(o => oldTags.Select(t => t.HostId).ToList().Contains(o.TaggedLocalElementId)).ToList();

                //得到纯新建的标记集合
                IList<IndependentTag> creatTags = newTags.Except(ReSetTags).ToList();

                #region 操作新建对象

                foreach (IndependentTag idt in creatTags)
                {
                    //设置标记引线
                    ChangePmethodTagLeader(idt, pmethodTagSetFrmPara, leaderFs);
                    //加入返回集合
                    returnTags.Add(idt);
                }

                #endregion //操作新建对象

                #region 操作重建对象   

                if (exsistSet == ExsistTagSet.CompletelyReset)
                {
                    foreach (IndependentTag idt in ReSetTags)
                    {
                        //设置标记引线
                        ChangePmethodTagLeader(idt, pmethodTagSetFrmPara, leaderFs);
                        //加入返回集合
                        returnTags.Add(idt);
                    }
                }
                else if (exsistSet == ExsistTagSet.KeepLoc)
                {
                    foreach (IndependentTag idt in ReSetTags)
                    {
                        //得到原有标记
                        OldTag oldTag = oldTags.Where(o => o.HostId == idt.TaggedLocalElementId).First();

                        tr.Start("按照原位置重设标记位置");
                        idt.HasLeader = false; idt.TagHeadPosition = oldTag.TagHeadPosition;
                        tr.Commit();

                        //根据原标记设置标记引线
                        if (pmethodTagSetFrmPara.HasLeader)
                        {
                            ///////////////////////////////////////////
                        }

                        //加入返回集合
                        returnTags.Add(idt);
                    }
                }
                else
                {

                }

                #endregion //操作重建对象

            }
            catch (Exception ex) { if (tr.HasStarted()) { tr.Commit(); } MessageBox.Show(ex.ToString()); }

            return returnTags;

        }

        #endregion //根据标记设置窗体布置各种类型构件标记


        /// <summary>
        /// 在非三维视图中布置构件标记，返回新布置的标记集合并记录已有标记集合（输入对象可以为IndependentTag 或者标记主体）（不带引线，基于点和基于线主体都布置在主体当前视图下的几何中心点）
        /// </summary>
        /// <param name="collection">输入集合（对象可以为IndependentTag 或者标记主体）</param>
        /// <param name="oldTagCategory">已有标记类型（用于记录原始标记）</param>
        /// <param name="tagMode">标记模式</param>
        /// <param name="tagorn">标记方向：横向/竖向</param>
        /// <param name="addLeader">是否添加引线</param>
        /// <param name="fs">标记族类型</param>
        /// <param name="exsistSet">标记设置：忽略原始标记、重置位置不变、完全重置</param>
        /// <param name="refOldTags">返回的已有标记集合</param>
        /// <returns></returns>
        public IList<IndependentTag> CreateIndependentTags(IList<Element> collection, BuiltInCategory oldTagCategory, TagMode tagMode, TagOrientation tagorn, bool addLeader,
                                                           FamilySymbol fs, ExsistTagSet exsistSet, ref List<OldTag> refOldTags)
        {
            //初始化返回的标记集合
            IList<IndependentTag> newTags = new List<IndependentTag>();

            //需要记录下来的原标记集合
            List<OldTag> oldTags = new List<OldTag>();


            //事务
            Transaction tr = new Transaction(Doc);

            //当前视图
            Autodesk.Revit.DB.View view = Doc.ActiveView;

            //实例化自建类库类
            RevDocInfo rvtDocInfo = new RevDocInfo(UIdoc, Doc);
            MathsCalculation mc = new MathsCalculation();


            #region 错误判断

            //判断视图是否为非三维视图
            if (rvtDocInfo.CheckView(CheckViewSet.NoView3D, "请将视图转换至非三维视图中操作！") == false) { return newTags; }

            //判断对象集合是否为空
            if (collection.Count == 0)
            {
                return newTags;
            }

            //判断标记类型是否为空
            if (fs == null)
            {
                MessageBox.Show("标记类型不能为空!");
                return newTags;
            }

            #endregion //错误判断

            try
            {
                #region 根据 exsistSet 设置记录原标记集合 oldTags 和新生成标记集合 newTags

                if (collection.First() is IndependentTag)
                {
                    #region  输入对象为 标记 集合

                    //记录原有标记
                    foreach (IndependentTag tag in collection.Select(o => o as IndependentTag).ToList())
                    {
                        if (tag.HasLeader)
                        {
                            if (tag.HasElbow == true)
                            {
                                oldTags.Add(new OldTag(tag.Id, tag.TaggedLocalElementId, tag.GetTypeId(), tag.HasLeader, tag.TagHeadPosition, tag.LeaderEnd, tag.LeaderElbow));
                            }
                            else
                            {
                                oldTags.Add(new OldTag(tag.Id, tag.TaggedLocalElementId, tag.GetTypeId(), tag.HasLeader, tag.TagHeadPosition, tag.LeaderEnd));
                            }
                        }
                        else { oldTags.Add(new OldTag(tag.Id, tag.TaggedLocalElementId, tag.GetTypeId(), tag.HasLeader, tag.TagHeadPosition)); }
                    }


                    //只有标记重置但位置不变或者标记完全重置的情况下才创建新标记
                    if (exsistSet == ExsistTagSet.CompletelyReset || exsistSet == ExsistTagSet.KeepLoc)
                    {
                        foreach (OldTag idt in oldTags)
                        {
                            tr.Start("创建新标记");

                            XYZ xyz = idt.TagHeadPosition;
                            if (idt.HasLeader)
                            {
                                xyz = idt.LeaderEnd;
                            }

                            //创建新的标记
                            IndependentTag newTag = IndependentTag.Create(Doc, view.Id, new Reference(Doc.GetElement(idt.HostId)), addLeader, tagMode, tagorn, xyz);

                            //有引线的标记要将引线端点位置调整到布置基点位置
                            if (addLeader == true) { newTag.LeaderEndCondition = LeaderEndCondition.Free; newTag.LeaderEnd = xyz; }

                            //将新生成的标记加入集合
                            newTags.Add(newTag);

                            tr.Commit();
                        }
                    }

                    #endregion //输入对象为 标记 集合
                }
                else
                {
                    #region 输入对象为 标记主体 集合

                    #region 记录原有标记

                    //得到所有标记
                    IList<IndependentTag> AllTags = new FilteredElementCollector(Doc, Doc.ActiveView.Id).OfCategory(oldTagCategory).Where(o => o is IndependentTag).Cast<IndependentTag>().ToList();
                    if (AllTags.Select(o => o.TaggedLocalElementId).Intersect(collection.Select(o => o.Id)).Count() > 0)
                    {
                        //记录原有标记
                        foreach (IndependentTag tag in AllTags.Where(o => collection.Select(e => e.Id).ToList().Contains(o.TaggedLocalElementId) && o.OwnerViewId == Doc.ActiveView.Id))
                        {
                            try
                            {
                                if (tag.HasLeader)
                                {
                                    if (tag.HasElbow == true)
                                    {
                                        oldTags.Add(new OldTag(tag.Id, tag.TaggedLocalElementId, tag.GetTypeId(), tag.HasLeader, tag.TagHeadPosition, tag.LeaderEnd, tag.LeaderElbow));
                                    }
                                    else
                                    {
                                        oldTags.Add(new OldTag(tag.Id, tag.TaggedLocalElementId, tag.GetTypeId(), tag.HasLeader, tag.TagHeadPosition, tag.LeaderEnd));
                                    }
                                }
                                else
                                {
                                    oldTags.Add(new OldTag(tag.Id, tag.TaggedLocalElementId, tag.GetTypeId(), tag.HasLeader, tag.TagHeadPosition));
                                }
                            }
                            catch { }
                        }
                    }

                    #endregion //记录原有标记

                    //选择忽略原标记情况下排除原标记对象
                    if (exsistSet == ExsistTagSet.Ignore && oldTags.Count > 0)
                    {
                        List<Element> newCollection = collection.Where(o => !oldTags.Select(t => t.HostId).ToList().Contains(o.Id)).ToList();
                        collection.Clear();
                        collection = newCollection;
                    }

                    #region 创建新的标记

                    foreach (Element e in collection)
                    {
                        #region 得到 Tag 布置点 XYZ xyz（基于点和基于线主体都布置在主体当前视图下的几何中心点）

                        XYZ xyz = XYZ.Zero;

                        BoundingBoxXYZ box = e.get_BoundingBox(view);
                        XYZ max = mc.GetPointOnPlane(box.Max, view.Origin, view.ViewDirection);
                        XYZ min = mc.GetPointOnPlane(box.Min, view.Origin, view.ViewDirection);
                        xyz = new XYZ((min.X + max.X) / 2, (min.Y + max.Y) / 2, (min.Z + max.Z) / 2);

                        #endregion //得到 Tag 布置点 XYZ xyz（基于点和基于线主体都布置在主体当前视图下的几何中心点）

                        #region 创建标记

                        tr.Start("创建新标记");

                        //得到创建的标记
                        IndependentTag newTag = IndependentTag.Create(Doc, view.Id, new Reference(e), addLeader, tagMode, tagorn, xyz);

                        //有引线的标记要将引线端点位置调整到布置基点位置
                        if (addLeader == true) { newTag.LeaderEndCondition = LeaderEndCondition.Free; newTag.LeaderEnd = xyz; }

                        //将新生成的标记加入集合
                        newTags.Add(newTag);

                        tr.Commit();

                        #endregion //创建标记
                    }

                    #endregion //创建新的标记

                    #endregion //输入对象为 标记主体 集合
                }

                #region 设置新标记类型

                tr.Start("设置标记类型");

                foreach (IndependentTag newTag in newTags)
                {
                    //设置标注类型
                    newTag.ChangeTypeId(fs.Id);
                }

                tr.Commit();

                #endregion //设置新标记类型

                #endregion //根据 exsistSet 设置记录原标记集合 oldTags 和新生成标记集合 newTags

                #region 根据 exsistSet 设置删除原标记 oldTags

                //只有在重置位置不变和全部重置的情况下才删除原标记
                if (exsistSet == ExsistTagSet.CompletelyReset || exsistSet == ExsistTagSet.KeepLoc)
                {
                    tr.Start("删除原标记");

                    Doc.Delete(oldTags.Select(o => o.Id).ToList());

                    tr.Commit();
                }

                #endregion //根据 exsistSet 设置删除原标记 oldTags                
            }
            catch/* (Exception ex)*/
            {
                //MessageBox.Show(ex.ToString());
                if (tr.HasStarted())
                {
                    tr.Commit();
                }
            }

            //返回记录的已有标记集合
            refOldTags = oldTags;

            return newTags;
        }


        /// <summary>
        /// 标记位置偏移（根据视图比例变化）（标记主体基于线，偏移方向为当前视图平面垂直主体位置线方向，总是冲着上侧，位置线竖直时候冲着右侧）
        /// </summary>
        /// <param name="tag">标记对象</param>
        /// <param name="offset">偏移距离(在1:100的视图比例下)</param>
        public void ChangeTagLoc(IndependentTag tag, double offset)
        {
            //得到当前视图
            Autodesk.Revit.DB.View view = Doc.ActiveView;
            ////得到当前视图比例
            //int scale = view.get_Parameter(BuiltInParameter.VIEW_SCALE).AsInteger();

            //得到标记主体
            Element e = Doc.GetElement(tag.TaggedLocalElementId);

            //事务
            Transaction tr = new Transaction(Doc);

            try
            {
                if (e.Location is LocationCurve)
                {
                    MathsCalculation mc = new MathsCalculation();

                    LocationCurve location = e.Location as LocationCurve;
                    Curve cur = location.Curve;

                    //得到中心线投影在当前视图平面上的方向向量
                    XYZ ori = (mc.GetPointOnPlane(cur.GetEndPoint(1), view.Origin, view.ViewDirection) - mc.GetPointOnPlane(cur.GetEndPoint(0), view.Origin, view.ViewDirection)).Normalize();

                    //得到中心线投影在当前视图平面上的方向向量的垂直方向向量（总是冲着上侧，竖直时候冲着右侧）
                    XYZ ver = mc.GetFnormalOfVectorOnPlane(ori, view.RightDirection, view.UpDirection, view.ViewDirection);

                    //得到原来的基点
                    XYZ xyz = tag.TagHeadPosition;
                    if (tag.HasLeader)
                    {
                        xyz = tag.LeaderEnd;
                    }
                    //得到最终的标记位置(偏移量与当前视图比例相关)
                    xyz = xyz + ver * offset;

                    //改变标记位置
                    tr.Start("改变标记位置");

                    if (tag.HasLeader)
                    {
                        tag.LeaderEnd = xyz;
                    }
                    else
                    {
                        tag.TagHeadPosition = xyz;
                    }

                    tr.Commit();
                }
                else
                {
                    return;
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
                if (tr.HasStarted())
                {
                    tr.Commit();
                }
            }
        }


        /// <summary>
        /// 标记位置偏移（根据视图比例变化）（自定义方向，主体无限制）
        /// </summary>
        /// <param name="tag">标记对象</param>
        /// <param name="offset">偏移距离(实际距离)</param>
        /// <param name="ori">偏移方向</param>
        public void ChangeTagLoc(IndependentTag tag, double offset, XYZ ori)
        {
            //得到当前视图
            Autodesk.Revit.DB.View view = Doc.ActiveView;

            ////得到当前视图比例
            //int scale = view.get_Parameter(BuiltInParameter.VIEW_SCALE).AsInteger();

            //事务
            Transaction tr = new Transaction(Doc);

            try
            {
                //得到原来的基点
                XYZ xyz = tag.TagHeadPosition;
                if (tag.HasLeader)
                {
                    xyz = tag.LeaderEnd;
                }
                //得到新的标记位置(偏移量与当前视图比例相关)
                xyz = xyz + ori * offset;

                //改变标记位置
                tr.Start("改变标记位置");

                if (tag.HasLeader)
                {
                    tag.LeaderEnd = xyz;
                }
                else
                {
                    tag.TagHeadPosition = xyz;
                }

                tr.Commit();

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
                if (tr.HasStarted())
                {
                    tr.Commit();
                }
            }
        }



        /// <summary>
        /// 根据引线长度和引线角度设置标记引线（直线引线）（根据视图比例变化）
        /// </summary>
        /// <param name="tag">操作标记对象</param>
        /// <param name="arrangeWay">引线方向</param>
        /// <param name="leaderLength">引线长度</param>
        /// <param name="leaderAngle">引线角度</param>
        public void ChangeTagLeader(IndependentTag tag, ArrangeWay arrangeWay, double leaderLength, double leaderAngle)
        {
            //得到当前视图
            Autodesk.Revit.DB.View view = Doc.ActiveView;

            //得到当前视图比例
            int scale = view.get_Parameter(BuiltInParameter.VIEW_SCALE).AsInteger();

            //事务
            Transaction tr = new Transaction(Doc);
            try
            {
                tr.Start("改变标注引线");

                //设置自由端点
                tag.LeaderEndCondition = LeaderEndCondition.Free;


                #region 计算相关数值 ： double x（引线水平长度）， double y（引线竖直长度），double d（文本端点距离引线端点长度）

                double x = Math.Sin(leaderAngle) * leaderLength / 304.8;//引线水平长度
                double y = Math.Cos(leaderAngle) * leaderLength / 304.8;//引线竖直长度
                double d = 15 / 304.8 * scale;//文本端点距离引线端点长度

                if (arrangeWay == ArrangeWay.LeftTop || arrangeWay == ArrangeWay.LeftBottom)
                {
                    d = 5 / 304.8 * scale;
                }

                #endregion //计算相关数值 ： double x（引线水平长度）， double y（引线竖直长度），double d（文本端点距离引线端点长度）


                #region 根据方位设置标注引线

                //得到原来的基点
                XYZ xyz = tag.TagHeadPosition;
                if (tag.HasLeader)
                {
                    xyz = tag.LeaderEnd;
                }

                switch (arrangeWay)
                {
                    case ArrangeWay.RightTop:

                        //设置引线端点            
                        tag.LeaderEnd = xyz;

                        ////设置引线中点（将引线中点设置在引线端点，这样拖动文字，引线将不会弯折）
                        //newTag.LeaderElbow = xyz + v.RightDirection * x + v.UpDirection * y;

                        //设置文本端点
                        tag.TagHeadPosition = xyz + view.RightDirection * (x + d) + view.UpDirection * y;

                        break;

                    case ArrangeWay.RightBottom:

                        //设置引线端点              
                        tag.LeaderEnd = xyz;

                        ////设置引线中点（将引线中点设置在引线端点，这样拖动文字，引线将不会弯折）
                        //newTag.LeaderElbow = xyz + v.RightDirection * x - v.UpDirection * y;

                        //设置文本端点
                        tag.TagHeadPosition = xyz + view.RightDirection * (x + d) - view.UpDirection * y;

                        break;

                    case ArrangeWay.LeftTop:

                        //设置引线端点 
                        tag.LeaderEnd = xyz;

                        ////设置引线中点（将引线中点设置在引线端点，这样拖动文字，引线将不会弯折）
                        //newTag.LeaderElbow = xyz - v.RightDirection * x + v.UpDirection * y;

                        //设置文本端点
                        tag.TagHeadPosition = xyz - view.RightDirection * (x + d) + view.UpDirection * y;

                        break;

                    case ArrangeWay.LeftBottom:

                        //设置引线端点     
                        tag.LeaderEnd = xyz;

                        ////设置引线中点（将引线中点设置在引线端点，这样拖动文字，引线将不会弯折）
                        //newTag.LeaderElbow = xyz - v.RightDirection * x - v.UpDirection * y;

                        //设置文本端点
                        tag.TagHeadPosition = xyz - view.RightDirection * (x + d) - view.UpDirection * y;

                        break;

                    default:

                        break;

                }

                #endregion //根据方位设置标注引线

                tr.Commit();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());

                if (tr.HasStarted()) { tr.Commit(); }
            }

        }


        /// <summary>
        /// 根据三点设置标记引线（LeaderElbow 可为空）
        /// </summary>
        /// <param name="tag">操作标记对象</param>
        /// <param name="tagHeadPosition">标记头坐标</param>
        /// <param name="leaderEnd">引线端点坐标</param>
        /// <param name="leaderElbow">引线中点坐标</param>
        public void ChangeTagLeader(IndependentTag tag, XYZ tagHeadPosition, XYZ leaderEnd, XYZ leaderElbow)
        {
            //事务
            Transaction tr = new Transaction(Doc);
            try
            {
                tr.Start("改变标注引线");

                //设置自由端点
                tag.LeaderEndCondition = LeaderEndCondition.Free;


                //设置引线端点            
                tag.LeaderEnd = leaderEnd;

                if (leaderElbow != null)
                {
                    //设置引线中点（将引线中点设置在引线端点，这样拖动文字，引线将不会弯折）
                    tag.LeaderElbow = leaderElbow;
                }

                //设置文本端点
                tag.TagHeadPosition = tagHeadPosition;


                tr.Commit();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
                if (tr.HasStarted())
                {
                    tr.Commit();
                }
            }

        }



        /// <summary>
        /// 根据 TagSetFormPara 标记设置类对象 设置标记引线（带方向折线或者直线，或者无引线）
        /// </summary>
        /// <param name="tag">操作标记对象</param>
        /// <param name="scale">视图比例</param>
        /// <param name="arrangeWay">布置方式</param>
        /// <param name="Xori">布置右方向（相对方向的时候是沿着构件方向）</param>
        /// <param name="Yori">布置左方向（相对方向的时候是沿着构件方向）</param>
        /// <param name="tagSetFrmPara">TagSetFormPara标记设置类对象</param>
        public void ChangeTagLeader(IndependentTag tag, int scale, ArrangeWay arrangeWay, XYZ Xori, XYZ Yori, TagSetFormPara tagSetFrmPara)
        {

            RevDocInfo rvtinfo = new RevDocInfo(UIdoc, Doc);
            MathsCalculation mc = new MathsCalculation();

            //事务
            Transaction tr = new Transaction(Doc);

            try
            {
                //视图X方向定位距离（不随视图比例变化）（相对方向的时候是沿着构件方向）（标记在右边的时候是右正左负，标记在左边的时候是左正右负）
                double xDirectionOffset = tagSetFrmPara.XDirectionOffset / 304.8;
                //视图Y方向定位距离（不随视图比例变化）（相对方向的时候是沿着构件方向）（标记在右边的时候是右正左负，标记在左边的时候是左正右负）
                double yDirectionOffset = tagSetFrmPara.YDirectionOffset / 304.8;
                //文本按比例右侧X方向偏移（随视图比例变化）（相对方向的的时候是沿着构件方向）
                double xrTextOffset = tagSetFrmPara.XrTextOffset / 304.8 * scale;
                //文本按比例左侧X方向偏移（随视图比例变化）（相对方向的的时候是沿着构件方向）（只针对带引线对象）
                double xlTextOffset = tagSetFrmPara.XlTextOffset / 304.8 * scale;
                //文本按比例上侧Y方向偏移（随视图比例变化）（相对方向的的时候是垂直构件方向）
                double ytTextOffset = tagSetFrmPara.YtTextOffset / 304.8 * scale;
                //文本按比例下侧Y方向偏移（随视图比例变化）（相对方向的的时候是垂直构件方向）（只针对带引线对象）
                double ybTextOffset = tagSetFrmPara.YbTextOffset / 304.8 * scale;

                #region 旋转标记

                if (tagSetFrmPara.IsRotate == true && tagSetFrmPara.RotateAngle != 0)
                {
                    rvtinfo.RotateInstance(tag as Element, mc.AngleToRadian(tagSetFrmPara.RotateAngle));
                }

                #endregion //旋转标记


                if (arrangeWay == ArrangeWay.NoLeader || tagSetFrmPara.HasLeader == false)
                {
                    #region 不带引线标记

                    if ((xDirectionOffset + xrTextOffset) != 0) { ChangeTagLoc(tag, (xDirectionOffset + xrTextOffset), Xori); }
                    if ((yDirectionOffset + ytTextOffset) != 0) { ChangeTagLoc(tag, (yDirectionOffset + ytTextOffset), Yori); }

                    #endregion //不带引线标记
                }
                else
                {
                    #region 带引线标记

                    tr.Start("改变标注引线");

                    //设置自由端点
                    tag.LeaderEndCondition = LeaderEndCondition.Free;

                    #region 根据方位设置标注引线

                    #region 得到原来的基点 xyz

                    //得到原来的基点
                    XYZ xyz = tag.TagHeadPosition;
                    if (tag.HasLeader)
                    {
                        xyz = tag.LeaderEnd;
                    }

                    #endregion //得到原来的基点 xyz

                    if (tagSetFrmPara.HoriLeaderLength == 0)
                    {
                        #region 直线引线

                        switch (arrangeWay)
                        {
                            case ArrangeWay.RightTop:

                                //设置引线端点            
                                tag.LeaderEnd = xyz;

                                //设置文本端点
                                tag.TagHeadPosition = xyz + Xori * (xDirectionOffset + xrTextOffset) + Yori * (yDirectionOffset + ytTextOffset);

                                break;

                            case ArrangeWay.RightBottom:

                                //设置引线端点              
                                tag.LeaderEnd = xyz;

                                //设置文本端点
                                tag.TagHeadPosition = xyz + Xori * (xDirectionOffset + xrTextOffset) - Yori * (yDirectionOffset + ybTextOffset);

                                break;

                            case ArrangeWay.LeftTop:

                                //设置引线端点 
                                tag.LeaderEnd = xyz;

                                //设置文本端点
                                tag.TagHeadPosition = xyz - Xori * (xDirectionOffset + xlTextOffset) + Yori * (yDirectionOffset + ytTextOffset);

                                break;

                            case ArrangeWay.LeftBottom:

                                //设置引线端点     
                                tag.LeaderEnd = xyz;

                                //设置文本端点
                                tag.TagHeadPosition = xyz - Xori * (xDirectionOffset + xlTextOffset) - Yori * (yDirectionOffset + ybTextOffset);

                                break;

                            default:

                                break;

                        }

                        #endregion //直线引线
                    }
                    else
                    {
                        //计算水平段引线长度
                        double horiLeaderLength = tagSetFrmPara.HoriLeaderLength / 304.8;

                        #region 折线引线

                        switch (arrangeWay)
                        {
                            case ArrangeWay.RightTop:

                                //设置引线端点            
                                tag.LeaderEnd = xyz;

                                //设置引线中点（将引线中点设置在引线端点，这样拖动文字，引线将不会弯折）
                                tag.LeaderElbow = xyz + Xori * xDirectionOffset + Yori * (yDirectionOffset + ytTextOffset);

                                //设置文本端点
                                tag.TagHeadPosition = tag.LeaderElbow + Xori * (horiLeaderLength + xrTextOffset);

                                break;

                            case ArrangeWay.RightBottom:

                                //设置引线端点              
                                tag.LeaderEnd = xyz;

                                //设置引线中点（将引线中点设置在引线端点，这样拖动文字，引线将不会弯折）
                                tag.LeaderElbow = xyz + Xori * xDirectionOffset - Yori * (yDirectionOffset + ybTextOffset);

                                //设置文本端点
                                tag.TagHeadPosition = tag.LeaderElbow + Xori * (horiLeaderLength + xrTextOffset);

                                break;

                            case ArrangeWay.LeftTop:

                                //设置引线端点 
                                tag.LeaderEnd = xyz;

                                //设置引线中点（将引线中点设置在引线端点，这样拖动文字，引线将不会弯折）
                                tag.LeaderElbow = xyz - Xori * xDirectionOffset + Yori * (yDirectionOffset + ytTextOffset);

                                //设置文本端点
                                tag.TagHeadPosition = tag.LeaderElbow - Xori * (horiLeaderLength + xlTextOffset);

                                break;

                            case ArrangeWay.LeftBottom:

                                //设置引线端点     
                                tag.LeaderEnd = xyz;

                                //设置引线中点（将引线中点设置在引线端点，这样拖动文字，引线将不会弯折）
                                tag.LeaderElbow = xyz - Xori * xDirectionOffset - Yori * (yDirectionOffset + ybTextOffset);

                                //设置文本端点
                                tag.TagHeadPosition = tag.LeaderElbow - Xori * (horiLeaderLength + xlTextOffset);

                                break;

                            default:

                                break;

                        }

                        #endregion //折线引线
                    }

                    #endregion //根据方位设置标注引线

                    tr.Commit();

                    #endregion //带引线标记
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());

                if (tr.HasStarted()) { tr.Commit(); }
            }



        }



        /// <summary>
        /// 根据 PmethodTagSetFormPara 平法标记设置类对象 设置标记引线（直线，或者无引线）
        /// </summary>
        /// <param name="tag">操作标记对象</param>
        /// <param name="pmethodTagSetFrmPara">PmethodTagSetFormPara平法标记设置类对象</param>
        public void ChangePmethodTagLeader(IndependentTag tag, PmethodTagSetFormPara pmethodTagSetFrmPara, FamilySymbol leaderFs)
        {
            MathsCalculation mc = new MathsCalculation();
            RevDocInfo rvtDocInfo = new RevDocInfo(UIdoc, Doc);

            //得到当前视图
            Autodesk.Revit.DB.View view = Doc.ActiveView;

            //得到当前视图比例
            int scale = view.get_Parameter(BuiltInParameter.VIEW_SCALE).AsInteger();
            //根据视图比例设置转化率
            double s = scale / 100;

            //标记间隙
            double dis = 104 / 304.8 * s;

            //事务
            Transaction tr = new Transaction(Doc);
            try
            {
                Element hostElem = Doc.GetElement(tag.TaggedLocalElementId);

                //文本绝对偏移方向
                XYZ offsetOri = XYZ.Zero + view.RightDirection * pmethodTagSetFrmPara.OffsetOri.X +
                                           view.UpDirection * pmethodTagSetFrmPara.OffsetOri.Y +
                                           view.ViewDirection * pmethodTagSetFrmPara.OffsetOri.Z;

                #region 计算对象位置垂直方向 hostLocFOri（总是朝着上侧和右侧）

                XYZ hostLocFOri = view.UpDirection;

                if (pmethodTagSetFrmPara.OffsetKind == TextOffsetKind.Relative)
                {
                    if (hostElem.Location is LocationCurve)
                    {
                        Curve hostLocCur = (hostElem.Location as LocationCurve).Curve;
                        XYZ hostLocS = hostLocCur.GetEndPoint(0);
                        XYZ hostLocE = hostLocCur.GetEndPoint(1);
                        //基于线实例的位置线垂直方向(逆时针方向)
                        hostLocFOri = mc.GetNormalOnPlane((hostLocE - hostLocS), view.RightDirection, view.UpDirection, view.ViewDirection).Negate().Normalize();

                        //第三四象限的梁方向要反向
                        if ((hostLocE - hostLocS).Normalize().AngleTo(view.RightDirection.Negate()) <= Math.PI / 2 &&
                           !(hostLocE - hostLocS).Normalize().IsAlmostEqualTo(view.UpDirection.Negate(), 1e-6))
                        { hostLocFOri = mc.GetNormalOnPlane((hostLocS - hostLocE), view.RightDirection, view.UpDirection, view.ViewDirection).Negate().Normalize(); }
                    }
                }

                #endregion //计算梁位置垂直方向 hostLocFOri（总是朝着上侧和右侧）

                #region 计算文本偏移距离 textOffset 和文本相对方向 offsetOri

                double textOffset = pmethodTagSetFrmPara.TextOffset / 304.8;
                if (pmethodTagSetFrmPara.ChangeByViewScale == true) { textOffset = textOffset * s; }

                if (pmethodTagSetFrmPara.OffsetKind == TextOffsetKind.Relative)
                {
                    if (hostElem.Location is LocationCurve)
                    {
                        #region 计算相对偏移方向 offsetOri

                        Transform transform = Transform.CreateRotation(view.ViewDirection, mc.SignedAngleBetween(hostLocFOri, view.UpDirection, view.ViewDirection.Negate()));
                        offsetOri = transform.OfVector(offsetOri);

                        #endregion //计算相对偏移方向 offsetOri
                    }
                }

                #endregion //计算文本偏移距离 textOffset 和文本相对方向 offsetOri

                #region 设置平法标注文字

                tr.Start("设置平法标注文字");

                //得到原来的基点
                XYZ xyz = tag.TagHeadPosition;

                //设置文本端点
                tag.TagHeadPosition = xyz + offsetOri * textOffset;

                tr.Commit();

                #endregion //设置平法标注文字

                if (pmethodTagSetFrmPara.HasLeader == true)
                {
                    #region 设置平法标注引线                   

                    #region 得到当前视图下tag的BoundingBox各个边界线

                    Dictionary<PointLocKey, XYZ> boundingBoxPoints = rvtDocInfo.GetElementBoundingBoxPoint(tag, view);

                    XYZ lt = boundingBoxPoints[PointLocKey.LeftTop];
                    XYZ rt = boundingBoxPoints[PointLocKey.RightTop];
                    XYZ lb = boundingBoxPoints[PointLocKey.LeftBottom];
                    XYZ rb = boundingBoxPoints[PointLocKey.RightBottom];
                    XYZ mid = boundingBoxPoints[PointLocKey.Centre];

                    Line lineT = Line.CreateBound(lt, rt);
                    Line lineR = Line.CreateBound(rt, rb);
                    Line lineB = Line.CreateBound(lb, rb);
                    Line lineL = Line.CreateBound(lt, lb);

                    #region 测试

                    //Line l5 = Line.CreateBound(lt, mid);
                    //Line l6 = Line.CreateBound(lb, mid);


                    CreateTestDetailCurve(lineT);
                    CreateTestDetailCurve(lineR);
                    CreateTestDetailCurve(lineB);
                    CreateTestDetailCurve(lineL);
                    //CreateTestDetailCurve(l5);
                    //CreateTestDetailCurve(l6);

                    #endregion //测试

                    #endregion  //得到当前视图下tag的BoundingBox各个边界线

                    //设置引线起点
                    XYZ leaderStart = mc.GetPointOnPlane(xyz, view.Origin, view.ViewDirection);

                    #region 根据文本位置计算引线方向 leaderOri

                    XYZ leaderOri = hostLocFOri;
                    //文字中心点在梁相对下方向，引线方向需要反向
                    if (mc.ProjectionLineOnOri(Line.CreateBound(leaderStart, mc.GetPointOnPlane(mid, view.Origin, view.ViewDirection)), hostLocFOri).Direction.Normalize().IsAlmostEqualTo(hostLocFOri.Negate(), 1e-6))
                    {
                        leaderOri = leaderOri.Negate();
                    }

                    #endregion //根据文本位置计算引线方向 leaderOri

                    #region 计算引线长度 leaderL

                    double leaderL = 0;

                    //辅助引线起点和端点（将需要布置的引线平移文字和引线间隙的距离并和tag的BoundingBox求交点，远交点即为辅助引线的终点，由辅助引线得到需要布置的引线长度）
                    XYZ calcuLeaderS = leaderStart;
                    XYZ calcuLeaderE = leaderStart;

                    #region 计算辅助引线起点坐标 calcuLeaderS

                    //辅助引线偏移方向
                    XYZ calcuLeaderOffsetOri = XYZ.Zero;

                    if (pmethodTagSetFrmPara.OffsetKind == TextOffsetKind.Relative)
                    {
                        if (hostElem.Location is LocationCurve)
                        {
                            //得到对象垂直方向的顺时针垂直方向，即梁方向（总是朝着右侧和下侧），也是需要测试交点的直线的平移方向（距离为平法标注文字和引线的间隙长度）
                            calcuLeaderOffsetOri = mc.GetNormalOnPlane(hostLocFOri, view.RightDirection, view.UpDirection, view.ViewDirection);

                            //当梁为竖直的时候，上述方向需要反向
                            if (hostLocFOri.IsAlmostEqualTo(view.RightDirection, 1e-6)) { calcuLeaderOffsetOri = calcuLeaderOffsetOri.Negate(); }

                            //得到平移文字和引线间隙距离后的引线起点
                            if (!(offsetOri.IsAlmostEqualTo(leaderOri, 1e-6) || offsetOri.IsAlmostEqualTo(leaderOri.Negate(), 1e-6)))
                            {
                                //得到文字相对偏移方向在梁方向上面的投影向量
                                Line l = mc.ProjectionLineOnOri(Line.CreateBound(xyz, xyz + offsetOri * textOffset), calcuLeaderOffsetOri);
                                calcuLeaderS = calcuLeaderS + calcuLeaderOffsetOri * dis + l.Direction * l.Length;
                            }
                            else
                            {
                                calcuLeaderS = calcuLeaderS + calcuLeaderOffsetOri * dis;
                            }
                        }
                        else
                        {

                        }
                    }

                    #endregion //计算辅助引线起点坐标 calcuLeaderS

                    #region 计算辅助引线终点坐标 calcuLeaderE，根据交点找到

                    //得到辅助引线起点和方向决定的射线
                    Line calcuLeaderll = Line.CreateBound(calcuLeaderS, calcuLeaderS + leaderOri * 1e6);

                    //测试
                    CreateTestDetailCurve(calcuLeaderll);

                    //得到射线与tag的BoundingBox外边的交点
                    IDictionary<string, XYZ> IeDic = new Dictionary<string, XYZ>();
                    IeDic.Add("T", mc.Line_IEresult(lineT, calcuLeaderll));
                    IeDic.Add("R", mc.Line_IEresult(lineR, calcuLeaderll));
                    IeDic.Add("B", mc.Line_IEresult(lineB, calcuLeaderll));
                    IeDic.Add("L", mc.Line_IEresult(lineL, calcuLeaderll));

                    KeyValuePair<string, XYZ> ieP = IeDic.Where(o => o.Value != null).OrderBy(o => o.Value.DistanceTo(leaderStart)).Last();

                    calcuLeaderE = ieP.Value;

                    #endregion //计算辅助引线终点坐标 calcuLeaderE，根据交点找到

                    leaderL = calcuLeaderS.DistanceTo(calcuLeaderE);

                    #region 当文字中心点在梁的相对下方向时候（且梁非水平竖直方向）引线的长度需要减掉间隙造成的误差

                    //文字中心点在梁相对下方向
                    if (mc.ProjectionLineOnOri(Line.CreateBound(leaderStart, mc.GetPointOnPlane(mid, view.Origin, view.ViewDirection)), hostLocFOri).Direction.Normalize().IsAlmostEqualTo(hostLocFOri.Negate(), 1e-6))
                    {
                        if (!(leaderOri.IsAlmostEqualTo(view.UpDirection) || leaderOri.IsAlmostEqualTo(view.UpDirection.Negate()) ||
                           leaderOri.IsAlmostEqualTo(view.RightDirection) || leaderOri.IsAlmostEqualTo(view.RightDirection.Negate())))
                        {

                            if (ieP.Key == "B") { leaderL = leaderL - Math.Tan(calcuLeaderOffsetOri.AngleTo(view.RightDirection)) * dis; }
                            else { leaderL = leaderL - Math.Tan(calcuLeaderOffsetOri.AngleTo(view.UpDirection.Negate())) * dis; }
                        }
                    }

                    #endregion //当文字中心点在梁的相对下方向时候（且梁非水平竖直方向）引线的长度需要减掉间隙造成的误差

                    #endregion //计算引线长度 leaderL

                    tr.Start("设置平法标注引线");

                    //设置引线
                    FamilyInstance leader = Doc.Create.NewFamilyInstance(Line.CreateBound(leaderStart, leaderStart + leaderOri * leaderL), leaderFs, view);

                    tr.Commit();

                    #endregion //设置平法标注引线
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());

                if (tr.HasStarted()) { tr.Commit(); }
            }

        }





        /// <summary>
        /// 判断标记主体中某一参数值是否为空,如果单一主体参数为空的时候弹出窗体让用户输入相应参数值，如果多个主体参数为空的时候仅提示用户是否继续操作
        /// </summary>
        /// <param name="collection">主体对象</param>
        /// <param name="paraName">需要判断的参数名</param>
        /// <returns></returns>
        public bool ParaIsEmpty(IList<Element> collection, string paraName)
        {
            if (String.IsNullOrWhiteSpace(paraName)) { return true; }
            RevDocInfo rvtDocInfo = new RevDocInfo(UIdoc, Doc);

            try
            {
                if (collection.Count == 1)
                {
                    Element e = collection.First();
                    foreach (Parameter p in e.Parameters)
                    {
                        if (p.Definition.Name.Contains(paraName))
                        {
                            //如果参数值为空
                            if (String.IsNullOrWhiteSpace(p.AsString()))
                            {
                                //显示参数输入窗体
                                Form_ParaEmpty frm = new Form_ParaEmpty(paraName);
                                frm.ShowDialog();

                                if (frm.DialogResult == DialogResult.OK)
                                {
                                    Transaction tr = new Transaction(Doc, "改变参数");
                                    tr.Start();

                                    rvtDocInfo.SetParameterValueByStrValue(Doc, p, frm.ParaValue);

                                    tr.Commit();
                                }
                            }
                        }
                    }
                    return true;
                }
                if (collection.Count > 1)
                {
                    bool isEmpty = false;

                    #region 判断对象集合中是否存在当前参数值为空的对象

                    foreach (Element e in collection)
                    {
                        foreach (Parameter p in e.Parameters)
                        {
                            if (p.Definition.Name.Contains(paraName) && String.IsNullOrWhiteSpace(p.AsString()))
                            {
                                isEmpty = true;
                                break;
                            }
                        }
                        if (isEmpty == true)
                        {
                            break;
                        }
                    }

                    #endregion //判断对象集合中是否存在当前参数值为空的对象

                    if (isEmpty == true)
                    {
                        if (MessageBox.Show("所选集合中存在关联构件参数[" + paraName + "]的值为空的对象！是否继续标记？", "提示", MessageBoxButtons.OKCancel, MessageBoxIcon.Question) == DialogResult.OK)
                        {
                            return true;
                        }
                        else
                        {
                            return false;
                        }
                    }

                }
            }
            catch
            //catch (Exception ex)
            {
                //MessageBox.Show(ex.ToString());
            }

            return true;
        }


        /// <summary>
        /// 从后台XML文档中读取TagSetPara节点信息，并创建TagSetFormPara类
        /// </summary>
        /// <param name="filePath">后台文档路径</param>
        /// <param name="fileName">后台文档名称</param>
        /// <param name="Tagfs">标记族类型</param>
        /// <returns></returns>
        public TagSetFormPara GetTagSetParaFromXML(string filePath, string fileName, FamilySymbol Tagfs)
        {
            //得到标记族名称和族类型名称
            string FN = Tagfs.FamilyName;
            string FSN = Tagfs.Name;

            TagSetFormPara tsfp = GetTagSetParaFromXML(filePath, fileName, FN, FSN);
            return tsfp;
        }


        /// <summary>
        /// 从后台XML文档中读取TagSetPara节点信息，并创建TagSetFormPara类
        /// </summary>
        /// <param name="filePath">后台文档路径</param>
        /// <param name="fileName">后台文档名称</param>
        /// <param name="fn">所选族名称</param>
        /// <param name="fsn">所选类型名称</param>
        /// <returns></returns>
        public TagSetFormPara GetTagSetParaFromXML(string filePath, string fileName, string fn, string fsn)
        {

            //初始化返回类属性
            TagSetFormPara tagSetFormPara = new TagSetFormPara(null, null, TagMode.TM_ADDBY_CATEGORY, TagOrientation.Horizontal, false, 0, 0, 0, 0, 0, 0, 0, false, 0);

            if (fn == null) { MessageBox.Show("读取后台标记设置信息出错!", "提示"); return tagSetFormPara; }


            List<XmlNode> list = new List<XmlNode>();

            try
            {
                #region 从后台文档中得到 TagSetPara 节点下的信息并将所有节点加入集合 list

                XmlDocument xmldoc = new XmlDocument();
                xmldoc.Load(filePath + fileName + ".xml");

                XmlNode n = xmldoc.SelectSingleNode("PlugInLog/TagSetPara");

                if (n == null) { MessageBox.Show("XML文档 [ " + fileName + ".xml ] 中的节点 [TagSetPara] 不存在，导致节点读取失败！"); return tagSetFormPara; }

                foreach (XmlNode x in n.ChildNodes)
                {
                    if (x.Name == "TagSet" && x.Attributes.GetNamedItem("FNkey") != null && x.Attributes.GetNamedItem("FSNkey") != null
                        && !String.IsNullOrWhiteSpace(x.Attributes.GetNamedItem("FNkey").Value) && !String.IsNullOrWhiteSpace(x.Attributes.GetNamedItem("FSNkey").Value))
                    {
                        list.Add(x);
                    }
                }

                if (list.Count == 0) { MessageBox.Show("XML文档 [ " + fileName + ".xml ] 中的节点 [TagSetPara] 不存在，导致节点读取失败！"); return tagSetFormPara; }

                #endregion //从后台文档中得到 TagSetPara 节点下的信息并将所有节点加入集合 list

                #region 得到满足条件的 TagSet 节点下的信息 并 创建类 tagSetFormPara

                XmlNode xn = list.Where(o => fn.Contains(o.Attributes.GetNamedItem("FNkey").Value.ToString())
                                          && fsn == o.Attributes.GetNamedItem("FSNkey").Value.ToString()).FirstOrDefault();

                if (xn == null)
                {
                    xn = list.Where(o => fn.Contains(o.Attributes.GetNamedItem("FNkey").Value.ToString())).FirstOrDefault();
                }
                if (xn == null)
                {
                    MessageBox.Show("查找不到族[" + fn + "]的类型[" + fsn + "]的标记设置信息！", "提示"); return tagSetFormPara;
                }

                #region 获取满足条件的节点下的类属性并更新类信息

                tagSetFormPara.FNkey = xn.Attributes.GetNamedItem("FNkey").Value;
                tagSetFormPara.FSNkey = xn.Attributes.GetNamedItem("FSNkey").Value;

                foreach (XmlNode xxn in xn.ChildNodes)
                {
                    switch (xxn.Name)
                    {
                        case "TagMode":
                            if (!String.IsNullOrWhiteSpace(xxn.InnerText)) { tagSetFormPara.Tagmode = (TagMode)System.Enum.Parse(typeof(TagMode), xxn.InnerText); }
                            break;

                        case "TagOri":
                            if (!String.IsNullOrWhiteSpace(xxn.InnerText)) { tagSetFormPara.TagOri = (TagOrientation)System.Enum.Parse(typeof(TagOrientation), xxn.InnerText); }
                            break;

                        case "HasLeader":
                            if (!String.IsNullOrWhiteSpace(xxn.InnerText)) { tagSetFormPara.HasLeader = Boolean.Parse(xxn.InnerText); }
                            break;

                        case "XDirectionOffset":
                            if (!String.IsNullOrWhiteSpace(xxn.InnerText)) { tagSetFormPara.XDirectionOffset = Convert.ToDouble(xxn.InnerText); }
                            break;

                        case "YDirectionOffset":
                            if (!String.IsNullOrWhiteSpace(xxn.InnerText)) { tagSetFormPara.YDirectionOffset = Convert.ToDouble(xxn.InnerText); }
                            break;

                        case "XrTextOffset":
                            if (!String.IsNullOrWhiteSpace(xxn.InnerText)) { tagSetFormPara.XrTextOffset = Convert.ToDouble(xxn.InnerText); }
                            break;

                        case "XlTextOffset":
                            if (!String.IsNullOrWhiteSpace(xxn.InnerText)) { tagSetFormPara.XlTextOffset = Convert.ToDouble(xxn.InnerText); }
                            break;

                        case "YtTextOffset":
                            if (!String.IsNullOrWhiteSpace(xxn.InnerText)) { tagSetFormPara.YtTextOffset = Convert.ToDouble(xxn.InnerText); }
                            break;

                        case "YbTextOffset":
                            if (!String.IsNullOrWhiteSpace(xxn.InnerText)) { tagSetFormPara.YbTextOffset = Convert.ToDouble(xxn.InnerText); }
                            break;

                        case "HoriLeaderLength":
                            if (!String.IsNullOrWhiteSpace(xxn.InnerText)) { tagSetFormPara.HoriLeaderLength = Convert.ToDouble(xxn.InnerText); }
                            break;

                        case "IsRotate":
                            if (!String.IsNullOrWhiteSpace(xxn.InnerText)) { tagSetFormPara.IsRotate = Boolean.Parse(xxn.InnerText); }
                            break;

                        case "RotateAngle":
                            if (!String.IsNullOrWhiteSpace(xxn.InnerText)) { tagSetFormPara.RotateAngle = Convert.ToDouble(xxn.InnerText); }
                            break;

                    }
                }

                #endregion //获取满足条件的节点下的类属性并更新类信息


                #endregion //得到满足条件的 TagSet 节点下的信息 并 创建类 tagSetFormPara
            }
            catch/* (Exception ex)*/
            {
                //MessageBox.Show(ex.ToString());
                MessageBox.Show("XML文档 [ " + fileName + ".xml ] 读取失败！");
            }
            return tagSetFormPara;
        }

        /// <summary>
        /// 重写后台XML文档中TagSetPara节点信息
        /// </summary>
        /// <param name="filePath">后台文档路径</param>
        /// <param name="fileName">后台文档名称</param>
        /// <param name="tagSetFormPara">需要记录的类</param>
        /// <returns></returns>
        public bool SetTagSetParaFromXML(string filePath, string fileName, TagSetFormPara tagSetFormPara)
        {
            try
            {
                #region 从后台文档中得到 TagSetPara 节点下的信息 并 修改相应节点信息

                XmlDocument xmldoc = new XmlDocument();
                xmldoc.Load(filePath + fileName + ".xml");

                XmlNode n = xmldoc.SelectSingleNode("PlugInLog/TagSetPara");

                if (n == null) { MessageBox.Show("XML文档 [ " + fileName + ".xml ] 中的节点 [TagSetPara] 不存在，导致节点写入失败！"); return false; }

                foreach (XmlNode x in n.ChildNodes)
                {
                    if (x.Name == "TagSet" && x.Attributes.GetNamedItem("FNkey") != null && x.Attributes.GetNamedItem("FSNkey") != null
                        && !String.IsNullOrWhiteSpace(x.Attributes.GetNamedItem("FNkey").Value) && !String.IsNullOrWhiteSpace(x.Attributes.GetNamedItem("FSNkey").Value))
                    {
                        //得到满足条件的 TagSet 节点下的信息
                        if (tagSetFormPara.FNkey.Contains(x.Attributes.GetNamedItem("FNkey").Value.ToString()) && tagSetFormPara.FSNkey.Contains(x.Attributes.GetNamedItem("FSNkey").Value.ToString()))
                        {
                            #region 修改满足条件的节点下的类属性

                            x.Attributes.GetNamedItem("FNkey").Value = tagSetFormPara.FNkey;
                            x.Attributes.GetNamedItem("FSNkey").Value = tagSetFormPara.FSNkey;

                            foreach (XmlNode xxn in x.ChildNodes)
                            {
                                switch (xxn.Name)
                                {
                                    case "TagMode":
                                        xxn.InnerText = tagSetFormPara.Tagmode.ToString();
                                        break;

                                    case "TagOri":
                                        xxn.InnerText = tagSetFormPara.TagOri.ToString();
                                        break;

                                    case "HasLeader":
                                        xxn.InnerText = tagSetFormPara.HasLeader.ToString();
                                        break;

                                    case "XDirectionOffset":
                                        xxn.InnerText = tagSetFormPara.XDirectionOffset.ToString();
                                        break;

                                    case "YDirectionOffset":
                                        xxn.InnerText = tagSetFormPara.YDirectionOffset.ToString();
                                        break;

                                    case "XrTextOffset":
                                        xxn.InnerText = tagSetFormPara.XrTextOffset.ToString();
                                        break;

                                    case "XlTextOffset":
                                        xxn.InnerText = tagSetFormPara.XlTextOffset.ToString();
                                        break;

                                    case "YtTextOffset":
                                        xxn.InnerText = tagSetFormPara.YtTextOffset.ToString();
                                        break;

                                    case "YbTextOffset":
                                        xxn.InnerText = tagSetFormPara.YbTextOffset.ToString();
                                        break;

                                    case "HoriLeaderLength":
                                        xxn.InnerText = tagSetFormPara.HoriLeaderLength.ToString();
                                        break;

                                    case "IsRotate":
                                        xxn.InnerText = tagSetFormPara.IsRotate.ToString();
                                        break;

                                    case "RotateAngle":
                                        xxn.InnerText = tagSetFormPara.RotateAngle.ToString();
                                        break;

                                }
                            }

                            #endregion //修改满足条件的节点下的类属性
                        }
                    }
                }

                #endregion //从后台文档中得到 TagSetPara 节点下的信息 并 修改相应节点信息

                //保存文档
                xmldoc.Save(filePath + fileName + ".xml");
            }
            catch/* (Exception ex)*/
            {
                //MessageBox.Show(ex.ToString());              
                MessageBox.Show("XML文档 [ " + fileName + ".xml ] 写入失败！");
                return false;
            }

            return true;
        }


        #region 相关枚举

        public enum ArrangeWay
        {
            LeftTop,
            RightTop,
            LeftBottom,
            RightBottom,
            PmethodLeader,
            NoLeader,
        }

        public enum ExsistTagSet
        {
            //忽略原始标记（不操作原标记）
            Ignore,
            //重置位置不变(删除后新建对象保留原位置)
            KeepLoc,
            //完全重置
            CompletelyReset,
        }

        #endregion //相关枚举

        #endregion //构件标记相关方法







        #region XML文档操作相关方法



        #region 创建文档

        /// <summary>
        /// 创建并保存一个只有一个根节点的XML文档
        /// </summary>
        /// <param name="filePath">文档所在路径(结尾带\) </param>
        /// <param name="fileName">文档名称（不包含.xml）</param>
        /// <param name="genNodeName">根节点名称</param>
        /// <returns>返回创建的 XmlDocument 文档</returns>
        public XmlDocument CreateXMLFlie(string filePath, string fileName, string genNodeName)
        {
            XmlDocument xmldoc = new XmlDocument();
            try
            {
                XmlNode node;
                node = xmldoc.CreateXmlDeclaration("1.0", "utf-8", null);
                xmldoc.AppendChild(node);

                XmlElement n1 = xmldoc.CreateElement(genNodeName);//根节点
                xmldoc.AppendChild(n1);

                xmldoc.Save(filePath + fileName + ".xml");
            }
            catch
            {
                MessageBox.Show("XML文档[" + fileName + ".xml]创建失败");
            }
            return xmldoc;
        }



        #endregion //创建文档



        #region 文档写入

        /// <summary>
        /// 写入xml子节点，如果写入字段 key （子节点名称）存在，则重写子节点的 InnerText，否则，添加名字为 key 的子节点及其 InnerText
        /// </summary>
        /// <param name="filePath">文档所在路径(结尾带\)</param>
        /// <param name="fileName">文档名称（不包含.xml）</param>
        /// <param name="genNodeName">需要重写的根节点名称</param>
        /// <param name="InnerTextList">需要重写的子节点名称及其InnerText的集合</param>
        public void ReWriteChildElement(string filePath, string fileName, string genNodeName, Dictionary<string, string> InnerTextList)
        {
            try
            {
                XmlDocument xmldoc = new XmlDocument();
                xmldoc.Load(filePath + fileName + ".xml");

                XmlNode n = xmldoc.SelectSingleNode(genNodeName);

                if (n == null)
                {
                    string[] s = genNodeName.ToString().Split('/');
                    if (s.Count() == 2)
                    {
                        if (!String.IsNullOrWhiteSpace(s[0]) && !String.IsNullOrWhiteSpace(s[1]))
                        {
                            XmlNode nn = xmldoc.SelectSingleNode(s[0]);
                            if (nn != null)
                            {
                                XmlElement xmle = CreateChildElement(xmldoc, nn as XmlElement, s[1]);

                                if (xmle != null)
                                { n = xmle as XmlNode; }
                            }
                        }
                    }
                }

                if (n != null)
                {
                    //得到当前节点所有子节点集合
                    List<XmlNode> list = new List<XmlNode>();
                    foreach (XmlNode xn in n.ChildNodes)
                    {
                        list.Add(xn);
                    }

                    foreach (var kvp in InnerTextList)
                    {
                        //重复子节点重写
                        if (list.Select(o => o.Name).ToList().Contains(kvp.Key))
                        {
                            ChangeChildElement(xmldoc, n as XmlElement, kvp.Key, kvp.Value);
                        }
                        //非重复子节点创建
                        else
                        {
                            CreateChildElement(xmldoc, n as XmlElement, kvp.Key, kvp.Value);
                        }

                    }
                    xmldoc.Save(filePath + fileName + ".xml");
                }
                else
                {
                    MessageBox.Show("XML文档 [ " + fileName + ".xml ] 中的节点 [ " + genNodeName + " ] 不存在，导致文档写入失败！");
                    return;
                }
            }
            catch /*(Exception ex)*/
            {
                //MessageBox.Show(ex.ToString());
                MessageBox.Show("XML文档 [ " + fileName + ".xml ] 写入失败！");
            }
        }

        #endregion //文档写入



        #region 文档读取


        /// <summary>
        /// 读取XML文档下某一节点下带 InnerText 的子节点名称及其 InnerText
        /// </summary>
        /// <param name="filePath">文档所在路径(结尾带\)</param>
        /// <param name="fileName">文档名称（不包含.xml）</param>
        /// <param name="genNodeName">需要读取的根节点名称</param>
        /// <returns>读取的子节点名称及其InnerText的集合</returns>
        public Dictionary<string, string> ReadChildElement(string filePath, string fileName, string genNodeName)
        {
            Dictionary<string, string> dic = new Dictionary<string, string>();
            try
            {
                XmlDocument xmldoc = new XmlDocument();
                xmldoc.Load(filePath + fileName + ".xml");

                XmlNode n = xmldoc.SelectSingleNode(genNodeName);

                if (n != null)
                {
                    foreach (XmlNode xn in n.ChildNodes)
                    {
                        //名称重复的节点只加一个
                        if (!dic.Keys.Contains(xn.Name))
                        {
                            dic.Add(xn.Name, xn.InnerText);
                        }
                    }
                }
                else
                {
                    MessageBox.Show("XML文档 [ " + fileName + ".xml ] 中的节点 [ " + genNodeName + " ] 不存在，导致文档读取失败！");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
                MessageBox.Show("XML文档 [ " + fileName + ".xml ] 读取失败！");
            }
            return dic;
        }


        /// <summary>
        /// 读取某一节点下的所有带 InnerText 的子节点名称及其 InnerText
        /// </summary>
        /// <param name="genNode">已经的根节点</param>
        /// <returns>读取的子节点名称及其InnerText的集合</returns>
        public Dictionary<string, string> ReadChildElement(XmlNode genNode)
        {
            Dictionary<string, string> dic = new Dictionary<string, string>();
            try
            {
                if (genNode != null)
                {
                    foreach (XmlNode xn in genNode.ChildNodes)
                    {
                        //名称重复的节点只加一个
                        if (!dic.Keys.Contains(xn.Name))
                        {
                            dic.Add(xn.Name, xn.InnerText);
                        }
                    }
                }
                else
                {
                    MessageBox.Show("节点 [ " + genNode.Name + " ] 不存在，导致文档读取失败！");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
                MessageBox.Show("节点 [ " + genNode.Name + " ] 读取失败！");
            }
            return dic;
        }



        /// <summary>
        /// 读取XML文档某一节点下的所有子节点
        /// </summary>
        /// <param name="filePath">文档所在路径(结尾带\)</param>
        /// <param name="fileName">文档名称（不包含.xml）</param>
        /// <param name="genNodeName">需要读取的根节点名称</param>
        /// <returns>返回该节点下的所有字节点集合</returns>
        public List<XmlNode> GetAllChildNode(string filePath, string fileName, string genNodeName)
        {
            List<XmlNode> list = new List<XmlNode>();
            try
            {
                XmlDocument xmldoc = new XmlDocument();
                xmldoc.Load(filePath + fileName + ".xml");

                XmlNode n = xmldoc.SelectSingleNode(genNodeName);

                if (n != null)
                {
                    foreach (XmlNode xn in n.ChildNodes)
                    {
                        list.Add(xn);
                    }
                }
                else
                {
                    MessageBox.Show("XML文档 [ " + fileName + ".xml ] 中的节点 [ " + genNodeName + " ] 不存在，导致节点读取失败！");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
                MessageBox.Show("XML文档 [ " + fileName + ".xml ] 读取失败！");
            }
            return list;
        }



        /// <summary>
        /// 读XML文件，并输出各个节点和其深度值
        /// </summary>
        /// <param name="filePath">文档所在路径(结尾带\)</param>
        /// <param name="fileName">文档名称（不包含.xml）</param>
        public void ReadXMLFile(string filePath, string fileName)
        {
            try
            {
                XmlReader textReader = XmlReader.Create(filePath + fileName + ".xml");
                while (textReader.Read())
                {
                    MessageBox.Show("节点深度: [ " + textReader.Depth + " ] " + ",节点名称：[ " + textReader.LocalName + " ] " + ",节点类型：[ " + textReader.NodeType + " ]");
                }
            }
            catch
            {
                MessageBox.Show("XML文档 [ " + fileName + ".xml ] 读取失败！");
            }
        }


        #endregion //文档读取



        #region 节点操作


        /// <summary>
        /// 添加带多个属性的子节点
        /// </summary>
        /// <param name="xmlDoc">XmlDocument</param>
        /// <param name="fatherElem">父节点</param>
        /// <param name="sonElemName">子节点名称</param>
        /// <param name="attributeDic">子节点属性字典集合（属性名称，属性值）</param>
        /// <returns>返回添加完成的子节点</returns>

        public XmlElement CreateChildElement(XmlDocument xmlDoc, XmlElement fatherElem, string sonElemName, Dictionary<string, string> attributeDic)
        {
            XmlElement childelem = xmlDoc.CreateElement(sonElemName);
            foreach (var kvp in attributeDic)
            {
                childelem.SetAttribute(kvp.Key, kvp.Value);
            }

            fatherElem.AppendChild(childelem);
            return childelem;
        }

        /// <summary>
        /// 添加带一个属性的子节点
        /// </summary>
        /// <param name="xmlDoc">XmlDocument</param>
        /// <param name="fatherElem">父节点</param>
        /// <param name="sonElemName">子节点名称</param>
        /// <param name="attributeKey">子节点属性名称</param>
        /// <param name="attributeValue">子节点属性值</param>
        /// <returns>返回添加完成的子节点</returns>
        public XmlElement CreateChildElement(XmlDocument xmlDoc, XmlElement fatherElem, string sonElemName, string attributeKey, string attributeValue)
        {
            XmlElement childelem = xmlDoc.CreateElement(sonElemName);
            childelem.SetAttribute(attributeKey, attributeValue);

            fatherElem.AppendChild(childelem);
            return childelem;
        }

        /// <summary>
        /// 添加不带属性的子节点
        /// </summary>
        /// <param name="xmlDoc">XmlDocument</param>
        /// <param name="fatherElem">父节点</param>
        /// <param name="sonElemName">子节点名称</param>
        /// <returns>返回添加完成的子节点</returns>
        public XmlElement CreateChildElement(XmlDocument xmlDoc, XmlElement fatherElem, string sonElemName)
        {
            XmlElement childelem = xmlDoc.CreateElement(sonElemName);

            fatherElem.AppendChild(childelem);
            return childelem;
        }





        /// <summary>
        /// 添加一个带 InnerText 的子节点
        /// </summary>
        /// <param name="xmldoc">XmlDocument</param>
        /// <param name="fatherElem">父节点</param>
        /// <param name="sonElemName">子节点名称</param>
        /// <param name="InnerText">子节点 InnerText </param>
        public void CreateChildElement(XmlDocument xmldoc, XmlElement fatherElem, string sonElemName, string InnerText)
        {
            XmlNode childnode = xmldoc.CreateNode(XmlNodeType.Element, sonElemName, null);
            childnode.InnerText = InnerText;
            fatherElem.AppendChild(childnode);
        }


        /// <summary>
        /// 改变一个带 InnerText 的子节点的 InnerText
        /// </summary>
        /// <param name="xmldoc">XmlDocument</param>
        /// <param name="fatherElem">父节点</param>
        /// <param name="sonElemName">子节点名称</param>
        /// <param name="InnerText">改后子节点 InnerText </param>
        public void ChangeChildElement(XmlDocument xmldoc, XmlElement fatherElem, string sonElemName, string InnerText)
        {
            foreach (XmlNode xn in fatherElem.ChildNodes)
            {
                if (xn.Name == sonElemName)
                {
                    xn.InnerText = InnerText;
                    break;
                }
            }
        }


        #endregion //节点操作



        #endregion //XML文档操作相关方法







        #region Txt文档写入相关方法

        #region 文档写入

        /// <summary>
        /// 通过重写Txt文档的方式记录信息，每次写入将刷新文档内容，文档格式：key1：value1|key2：value3|...|keyn：valuen|
        /// </summary>
        /// <param name="filePath">txt文档所在路径(结尾带\)</param>
        /// <param name="fileName">txt文档名称（不包含.xml）</param>
        /// <param name="InfoDic">需要记录的信息字典集合，key为字段名称，value为字段string值 </param>
        public void ReWriteTxt(string filePath, string fileName, Dictionary<string, string> InfoDic)
        {
            string s = null;

            foreach (var kvp in InfoDic)
            {
                s = s + kvp.Key + ":" + kvp.Value + "|";
            }

            //记录用户设置
            System.IO.File.WriteAllText(filePath + fileName + ".txt", s, Encoding.UTF8);
        }

        #endregion //文档写入


        #region 文档读取

        /// <summary>
        /// 读取Txt文档中的记录，并得到记录信息的字典集合，key为字段名称，value为字段string值，文档格式：key1：value1|key2：value3|...|keyn：valuen|
        /// </summary>
        /// <param name="filePath">txt文档所在路径(结尾带\)</param>
        /// <param name="fileName">txt文档名称（不包含.xml）</param>
        /// <returns>返回txt中读取的记录信息的字典集合，key为字段名称，value为字段string值 </returns>
        public Dictionary<string, string> ReadTxt(string filePath, string fileName)
        {
            //初始化得到的集合
            Dictionary<string, string> InfoDic = new Dictionary<string, string>();

            string text = null;
            try
            {
                //直接读取出字符串
                text = System.IO.File.ReadAllText(filePath + fileName + ".txt");
            }
            catch { }//拦截文档不存在情况

            if (!String.IsNullOrWhiteSpace(text))
            {
                try
                {
                    string[] Ary_s = text.Split('|');

                    foreach (string s in Ary_s)
                    {
                        if (!String.IsNullOrWhiteSpace(s) && s.Contains(':'))
                        {
                            string[] pair_s = s.Split(':');
                            string k = pair_s[0];
                            string v = pair_s[1];

                            if (!String.IsNullOrWhiteSpace(k) && !InfoDic.Keys.Contains(k))
                            {
                                InfoDic.Add(k, v);
                            }
                        }
                    }
                }
                catch { }
            }

            return InfoDic;
        }

        #endregion //文档读取


        #endregion //Txt文档写入相关方法






        #region 一切文档相关操作


        /// <summary>
        /// 判断文档是否存在
        /// </summary>
        /// <param name="filePath">文档根路径</param>
        /// <param name="fileName">文档名称，包含拓展名</param>
        /// <returns></returns>
        public bool FileIsExist(string filePath, string fileName)
        {
            string[] files = System.IO.Directory.GetFiles(filePath);

            foreach (string file in files)
            {
                if (System.IO.Path.GetFileName(file) == fileName)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 文档复制
        /// </summary>
        /// <param name="filePath">需要复制的文档根路径</param>
        /// <param name="fileName">需要复制的文档名称，包含拓展名</param>
        /// <param name="fileFinalPath">需要复制到的根文件夹</param>
        public void CopyFile(string filePath, string fileName, string fileFinalPath)
        {
            try
            {
                //得到原文件根目录下的所有文件
                string[] files = System.IO.Directory.GetFiles(filePath);

                foreach (string file in files)
                {
                    if (System.IO.Path.GetFileName(file) == fileName)
                    {
                        string name = System.IO.Path.GetFileName(file);
                        string dest = System.IO.Path.Combine(fileFinalPath, name);
                        System.IO.File.Copy(file, dest);//复制文件
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("文件复制失败！");
            }
        }

        #endregion //一切文档相关操作




        #region 功能辅助方法

        /// <summary>
        /// 手动放置FamilyInstance对象并返回放置后的对象集合
        /// </summary>
        /// <param name="symbol">对象族类型</param>
        /// <param name="category">对象类别</param>
        /// <returns></returns>

        public IList<Element> ManualPlaceElement(FamilySymbol symbol, BuiltInCategory category)
        {
            //布置前实例集合
            IList<Element> oldList = new FilteredElementCollector(Doc).OfClass(typeof(FamilyInstance)).OfCategory(category).ToList();

            //放置实例
            try { UIdoc.PromptForFamilyInstancePlacement(symbol); } catch { }

            //布置后实例集合
            IList<Element> newList = new FilteredElementCollector(Doc).OfClass(typeof(FamilyInstance)).OfCategory(category).ToList();

            //得到放置的实例对象
            IList<Element> insList = newList.Where(o => !oldList.Select(q => q.Id).ToList().Contains(o.Id)).ToList();

            return insList;

        }


        /// <summary>
        /// 创建三维视图
        /// </summary>
        /// <param name="vdLevel">详细程度</param>
        /// <param name="displaySy">视觉样式</param>
        /// <param name="activeView">是否激活视图</param>
        /// <returns></returns>
        public View3D CreateView3D(ViewDetailLevel vdLevel, DisplayStyle displaySy, bool activeView)
        {
            Transaction tr = new Transaction(Doc, "创建三维视图");
            try { tr.Start(); } catch { }

            ElementId View3dId = new FilteredElementCollector(Doc).OfClass(typeof(ViewFamilyType)).Cast<ViewFamilyType>()
                                 .Where(o => o != null && o.ViewFamily == ViewFamily.ThreeDimensional).First().Id;

            View3D newView = View3D.CreateIsometric(Doc, View3dId);//生成一个新的等轴3D视图

            Parameter viewTemplet = newView.get_Parameter(BuiltInParameter.VIEW_TEMPLATE);
            viewTemplet.Set(ElementId.InvalidElementId);

            newView.DetailLevel = vdLevel;
            newView.DisplayStyle = displaySy;


            if (tr.HasStarted()) { tr.Commit(); }

            if (activeView == true) { UIdoc.ActiveView = newView; }

            return newView;
        }

        /// <summary>
        /// 调节视图透明度
        /// </summary>
        /// <param name="doc">文档</param>
        /// <param name="v">需要调节透明度的三维视图</param>
        /// <param name="dt">视图详细程度</param>
        /// <param name="transparency">视图透明度</param>
        public void SetViewTransparency(Document doc, View3D v, ViewDetailLevel dt, int transparency)
        {
            Transaction tr = new Transaction(doc, "调节视图透明度");

            try { tr.Start(); } catch { }

            v.DisplayStyle = DisplayStyle.Shading;
            v.DetailLevel = dt;

            //调节视图透明度

            Categories categories = doc.Settings.Categories;

            foreach (Category c in categories)
            {
                if (c.get_AllowsVisibilityControl(v) == true)
                {
                    var overrideGraphicSettings = v.GetCategoryOverrides(c.Id);
                    overrideGraphicSettings.SetSurfaceTransparency(transparency);
                    v.SetCategoryOverrides(c.Id, overrideGraphicSettings);
                }

            }

            if (tr.HasStarted()) { tr.Commit(); }
        }

        /// <summary>
        /// 删除单一对象
        /// </summary>
        /// <param name="doc">文档</param>
        /// <param name="id">对象id</param>
        public void DeleteElement(Document doc, ElementId id)
        {
            Transaction tr = new Transaction(doc, "删除单一对象");

            try { tr.Start(); } catch { }

            try { doc.Delete(id); } catch { }

            if (tr.HasStarted()) { tr.Commit(); }
        }

        /// <summary>
        /// 批量删除对象
        /// </summary>
        /// <param name="doc">文档</param>
        /// <param name="ids">删除id集合</param>
        public void DeleteElements(Document doc, List<ElementId> ids)
        {
            Transaction tr = new Transaction(doc, "删除多个对象");

            if (ids.Count() > 0)
            {
                try { tr.Start(); } catch { }

                foreach (ElementId id in ids)
                {
                    DeleteElement(doc, id);
                }

                if (tr.HasStarted()) { tr.Commit(); }
            }
        }

        /// <summary>
        /// 创建线样式
        /// </summary>
        /// <param name="doc">文档</param>
        /// <param name="name">名称</param>
        /// <param name="lineWidth">线宽</param>
        /// <param name="color">线颜色</param>
        /// <param name="gst">样式类型</param>
        /// <returns></returns>
        public Category CreateLineStyleCategory(Document doc, string name, int lineWidth, Color color, GraphicsStyleType gst)
        {
            Category c;

            Transaction tr = new Transaction(doc, "创建线样式");
            try { tr.Start(); } catch { }


            Category lineC = doc.Settings.Categories.get_Item(BuiltInCategory.OST_Lines);
            CategoryNameMap cnm = lineC.SubCategories;

            if (cnm.Contains(name))
            {
                c = cnm.get_Item(name);
            }
            else
            {
                c = doc.Settings.Categories.NewSubcategory(lineC, name);
            }

            c.LineColor = color;
            c.SetLineWeight(lineWidth, gst);

            if (tr.HasStarted()) { tr.Commit(); }

            return c;
        }




        #endregion //功能辅助方法




        #region 插件测试辅助方法



        /// <summary>
        /// 创建单一测试详图线
        /// </summary>
        /// <param name="curve">需要创建的线</param>
        public DetailCurve CreateTestDetailCurve(Curve curve)
        {
            Transaction tr = new Transaction(Doc, "创建测试详图线");
            try { tr.Start(); } catch { }

            //如果当前平面无草图平面，需要创建草图平面
            if (Doc.ActiveView.SketchPlane == null)
            {
                Plane plane = Plane.CreateByNormalAndOrigin(Doc.ActiveView.ViewDirection, Doc.ActiveView.Origin);
                SketchPlane sp = SketchPlane.Create(Doc, plane);
                Doc.ActiveView.SketchPlane = sp;
            }

            //doc.ActiveView.ShowActiveWorkPlane();

            DetailCurve cur = Doc.Create.NewDetailCurve(Doc.ActiveView, curve) as DetailCurve;

            if (tr.HasStarted()) { tr.Commit(); }

            return cur;
        }


        /// <summary>
        /// 创建单一测试详图线（带线样式）
        /// </summary>
        /// <param name="curve">需要创建的线</param>
        /// <param name="lineStyleCategory">线样式类别</param>
        /// <param name="gst">样式类型</param>
        public DetailCurve CreateTestDetailCurve(Curve curve, Category lineStyleCategory, GraphicsStyleType gst)
        {
            Transaction tr = new Transaction(Doc, "创建带线样式的测试详图线");
            try { tr.Start(); } catch { }

            //如果当前平面无草图平面，需要创建草图平面
            if (Doc.ActiveView.SketchPlane == null)
            {
                Plane plane = Plane.CreateByNormalAndOrigin(Doc.ActiveView.ViewDirection, Doc.ActiveView.Origin);
                SketchPlane sp = SketchPlane.Create(Doc, plane);
                Doc.ActiveView.SketchPlane = sp;
            }

            //doc.ActiveView.ShowActiveWorkPlane();

            DetailCurve cur = Doc.Create.NewDetailCurve(Doc.ActiveView, curve) as DetailCurve;
            cur.LineStyle = lineStyleCategory.GetGraphicsStyle(gst);

            if (tr.HasStarted()) { tr.Commit(); }

            return cur;
        }



        /// <summary>
        /// 创建多个测试详图线
        /// </summary>
        /// <param name="curveList">需要创建的线集合</param>
        public List<DetailCurve> CreateTestDetailCurves(List<Curve> curveList)
        {
            List<DetailCurve> list = new List<DetailCurve>();

            Transaction tr = new Transaction(Doc, "创建测试详图线");
            try { tr.Start(); } catch { }

            //如果当前平面无草图平面，需要创建草图平面
            if (Doc.ActiveView.SketchPlane == null)
            {
                Plane plane = Plane.CreateByNormalAndOrigin(Doc.ActiveView.ViewDirection, Doc.ActiveView.Origin);
                SketchPlane sp = SketchPlane.Create(Doc, plane);
                Doc.ActiveView.SketchPlane = sp;
            }

            //doc.ActiveView.ShowActiveWorkPlane();

            foreach (Curve cur in curveList)
            {
                DetailCurve curve = Doc.Create.NewDetailCurve(Doc.ActiveView, cur) as DetailCurve;
                list.Add(curve);
            }

            if (tr.HasStarted()) { tr.Commit(); }

            return list;
        }

        /// <summary>
        /// 创建多个测试详图线（带线样式）
        /// </summary>
        /// <param name="curveList">需要创建的线集合</param>
        /// <param name="lineStyleCategory">线样式类别</param>
        /// <param name="gst">样式类型</param>
        public List<DetailCurve> CreateTestDetailCurves(List<Curve> curveList, Category lineStyleCategory, GraphicsStyleType gst)
        {
            List<DetailCurve> list = new List<DetailCurve>();

            Transaction tr = new Transaction(Doc, "创建带线样式的测试详图线");
            try { tr.Start(); } catch { }

            //如果当前平面无草图平面，需要创建草图平面
            if (Doc.ActiveView.SketchPlane == null)
            {
                Plane plane = Plane.CreateByNormalAndOrigin(Doc.ActiveView.ViewDirection, Doc.ActiveView.Origin);
                SketchPlane sp = SketchPlane.Create(Doc, plane);
                Doc.ActiveView.SketchPlane = sp;
            }

            //doc.ActiveView.ShowActiveWorkPlane();

            foreach (Curve cur in curveList)
            {
                DetailCurve curve = Doc.Create.NewDetailCurve(Doc.ActiveView, cur) as DetailCurve;
                curve.LineStyle = lineStyleCategory.GetGraphicsStyle(gst);
                list.Add(curve);
            }

            if (tr.HasStarted()) { tr.Commit(); }

            return list;
        }



        /// <summary>
        /// 创建单一测试模型线
        /// </summary>
        /// <param name="curve"></param>
        /// <param name="sketchPlaneDirection"></param>
        public ModelCurve CreateTestModelCurves(Curve curve, XYZ sketchPlaneDirection)
        {

            Transaction tr = new Transaction(Doc, "创建测试模型线");
            try { tr.Start(); } catch { }

            Plane plane = Plane.CreateByNormalAndOrigin(sketchPlaneDirection, curve.GetEndPoint(0));
            SketchPlane sp = SketchPlane.Create(Doc, plane);
            Doc.ActiveView.SketchPlane = sp;

            //创建模型线
            ModelCurve mc = Doc.Create.NewModelCurve(curve, sp);
            if (tr.HasStarted()) { tr.Commit(); }

            return mc;
        }


        /// <summary>
        /// 创建单一测试模型线（带线样式）
        /// </summary>
        /// <param name="curve"></param>
        /// <param name="sketchPlaneDirection"></param>
        /// <param name="lineStyleCategory">线样式类别</param>
        /// <param name="gst">样式类型</param>
        public ModelCurve CreateTestModelCurves(Curve curve, XYZ sketchPlaneDirection, Category lineStyleCategory, GraphicsStyleType gst)
        {

            Transaction tr = new Transaction(Doc, "创建带线样式的测试模型线");
            try { tr.Start(); } catch { }

            Plane plane = Plane.CreateByNormalAndOrigin(sketchPlaneDirection, curve.GetEndPoint(0));
            SketchPlane sp = SketchPlane.Create(Doc, plane);
            Doc.ActiveView.SketchPlane = sp;

            //创建模型线
            ModelCurve mc = Doc.Create.NewModelCurve(curve, sp);
            mc.LineStyle = lineStyleCategory.GetGraphicsStyle(gst);

            if (tr.HasStarted()) { tr.Commit(); }

            return mc;
        }





        /// <summary>
        /// 显示测试点（用强调定位点的辅助族布置在点的位置突出点）
        /// </summary>
        /// <param name="point">测试点坐标</param>
        /// <param name="view">测试点视图</param>
        /// <param name="FN">辅助族名称</param>
        /// <param name="FSN">辅助族类型名称</param>
        /// <param name="FamilyFilePath">辅助族路径</param>
        /// <param name="category">辅助族类别</param>
        /// <param name="Show">是否移动视图以显示点</param>
        /// <param name="Select">是否选中点辅助族实例</param>
        /// <param name="Isolate">是否隔离显示点辅助族实例</param>
        public void ShowTestPointInView(XYZ point, Autodesk.Revit.DB.View view, string FN, string FSN, string FamilyFilePath, BuiltInCategory category, bool Show, bool Select, bool Isolate)
        {
            RevDocInfo rvtDocInfo = new RevDocInfo(UIdoc, Doc);
            MathsCalculation mc = new MathsCalculation();

            #region 载入本地辅助线族

            //实例化自建类库类
            DownLoadFam dlf = new DownLoadFam();

            //载入族判定
            bool success = false;
            //载入辅助线族
            Family fam = dlf.LoadGetFamily(UIdoc.Document, FN, FamilyFilePath, ref success, category);

            if (success == false)
            {
                MessageBox.Show("找不到辅助族" + FN);
                return;
            }

            #endregion //载入本地辅助线族

            Transaction tr = new Transaction(UIdoc.Document, "显示测试点并放置辅助族");

            try
            {

                string fsn = FSN;

                //得到族类型
                FamilySymbol fs = rvtDocInfo.GetFamilySymbol(category, FN, ref fsn, true);

                //得到放置点
                XYZ loc = point;

                if (!(view is View3D))
                {
                    loc = mc.GetPointOnPlane(point, view.Origin, view.ViewDirection);
                }

                try { tr.Start(); } catch { }

                //放置族
                FamilyInstance ins = Doc.Create.NewFamilyInstance(loc, fs, Autodesk.Revit.DB.Structure.StructuralType.UnknownFraming);

                if (ins != null)
                {
                    List<ElementId> Id = new List<ElementId>() { ins.Id };

                    if (Show) { UIdoc.ShowElements(Id); }
                    if (Select) { UIdoc.Selection.SetElementIds(Id); }
                    if (Isolate) { view.IsolateElementTemporary(ins.Id); }
                }

                if (tr.HasStarted()) { tr.Commit(); }
            }
            catch
            {
                if (tr.HasStarted()) { tr.Commit(); }
            }

        }



        /// <summary>
        /// 显示测试点（用详图圈和模型十字线的方式显示和突出点的位置）
        /// </summary>
        /// <param name="point">点</param>
        /// <param name="view">视图</param>
        /// <param name="r">显示的圈或者十字的半径</param>
        /// <param name="Show">是否移动视图并显示</param>
        /// <param name="Select">是否选中</param>
        /// <param name="Isolate">是否隔离</param>
        public void ShowTestPointInView(XYZ point, Autodesk.Revit.DB.View view, double r, bool Show, bool Select, bool Isolate)
        {
            RevDocInfo rvtDocInfo = new RevDocInfo(UIdoc, Doc);
            MathsCalculation mc = new MathsCalculation();
            Transaction tr = new Transaction(Doc, "显示测试点示意");

            try
            {
                //得到放置点
                XYZ loc = point;

                //生成对象Id集合
                List<ElementId> ids = new List<ElementId>();

                if (!(view is View3D))
                {
                    loc = mc.GetPointOnPlane(point, view.Origin, view.ViewDirection);

                    Curve cur1 = Arc.Create(loc, r, 0, 360, view.RightDirection, view.UpDirection);
                    Curve cur2 = Line.CreateBound(loc - view.RightDirection * r / 2, loc + view.RightDirection * r / 2);
                    Curve cur3 = Line.CreateBound(loc - view.UpDirection * r / 2, loc + view.UpDirection * r / 2);
                    DetailCurve dc1 = CreateTestDetailCurve(cur1);
                    DetailCurve dc2 = CreateTestDetailCurve(cur2);
                    DetailCurve dc3 = CreateTestDetailCurve(cur3);

                    ids.Add(dc1.Id);
                    ids.Add(dc2.Id);
                    ids.Add(dc3.Id);
                }
                else
                {
                    Curve cur1 = Line.CreateBound(loc + view.UpDirection * r, loc - view.UpDirection * r);
                    ModelCurve mc1 = CreateTestModelCurves(cur1, view.RightDirection);

                    Curve cur2 = Line.CreateBound(loc + view.RightDirection * r, loc - view.RightDirection * r);
                    ModelCurve mc2 = CreateTestModelCurves(cur2, view.UpDirection);

                    Curve cur3 = Line.CreateBound(loc + view.ViewDirection * r, loc - view.ViewDirection * r);
                    ModelCurve mc3 = CreateTestModelCurves(cur3, view.UpDirection);


                    ids.Add(mc1.Id);
                    ids.Add(mc2.Id);
                    ids.Add(mc3.Id);
                }

                if (ids.Count() > 0)
                {
                    try { tr.Start(); } catch { }

                    if (Show) { UIdoc.ShowElements(ids); }
                    if (Select) { UIdoc.Selection.SetElementIds(ids); }
                    if (Isolate) { view.HideElementsTemporary(ids); }

                    if (tr.HasStarted()) { tr.Commit(); }
                }
            }
            catch/*(Exception ex)*/
            {
                if (tr.HasStarted()) { tr.Commit(); }
                //MessageBox.Show(ex.ToString());
            }

        }




        #endregion //插件测试辅助方法
    }
}


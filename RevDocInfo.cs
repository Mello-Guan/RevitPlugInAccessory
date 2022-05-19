using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;
using static RevitPlugInAccessory.AccessoryFunction;
using static RevitPlugInAccessory.RvtParameter;

namespace RevitPlugInAccessory
{
    ///获取文档信息相关方法
    public class RevDocInfo
    {

        #region 基本属性

        //Revit项目UI文档
        public UIDocument UIdoc { get; set; }

        //Revit项目文档
        public Autodesk.Revit.DB.Document Doc { get; set; }

        #endregion //基本属性


        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="uIdoc">Revit项目UI文档</param>
        /// <param name="doc">Revit项目文档</param>
        public RevDocInfo(UIDocument uIdoc, Autodesk.Revit.DB.Document doc)
        {
            this.UIdoc = uIdoc;
            this.Doc = doc;
        }






        #region 方法


        #region 获取Revit版本

        /// <summary>
        /// 从后台文档中获取Revit版本名称
        /// </summary>
        /// <param name="filePath">后台文档路径</param>
        /// <param name="fileName">后台文档名称</param>
        /// <returns></returns>
        public RevitName GetRevitName(string filePath, string fileName)
        {
            RevitName rn = RevitName.Revit2018;

            try
            {
                #region 从后台文档中得到 RevitName 节点信息

                XmlDocument xmldoc = new XmlDocument();
                xmldoc.Load(filePath + fileName + ".xml");

                XmlNode n = xmldoc.SelectSingleNode("PlugInLog/RevitName");

                if (n != null)
                {
                    if (!String.IsNullOrWhiteSpace(n.InnerText)) { rn = (RevitName)System.Enum.Parse(typeof(RevitName), n.InnerText); }
                }
                else
                {
                    MessageBox.Show("XML文档 [ " + fileName + ".xml ] 中的节点 [ RevitName ] 不存在，导致节点读取失败！");
                }

                #endregion // 从后台文档中得到 RevitName 节点信息
            }
            catch
            {
                MessageBox.Show("XML文档 [ " + fileName + ".xml ] 读取失败！");
            }
            return rn;
        }


        #endregion //获取Revit版本





        #region 得到文档信息相关方法

        /// <summary>
        /// 判断当前视图
        /// </summary>
        /// <param name="checkViewSet">判断设置</param>
        /// <param name="falseTip">判断为否时候的信息提示</param>
        /// <returns></returns>
        public bool CheckView(CheckViewSet checkViewSet, string falseTip)
        {
            bool bur = false;
            Autodesk.Revit.DB.View view = Doc.ActiveView;

            switch (checkViewSet)
            {
                case CheckViewSet.IsView3D:
                    if (view is View3D) { bur = true; }
                    else { bur = false; }
                    break;

                case CheckViewSet.IsViewPlan:
                    if (view is ViewPlan) { bur = true; }
                    else { bur = false; }
                    break;

                case CheckViewSet.IsViewSection:
                    if (view is ViewSection) { bur = true; }
                    else { bur = false; }
                    break;

                case CheckViewSet.NoView3D:
                    if (view is View3D) { bur = false; }
                    else { bur = true; }
                    break;

                default:
                    break;
            }

            if (bur == false)
            {
                MessageBox.Show(falseTip, "提示");
            }

            return bur;
        }



        /// <summary>
        /// 得到族类型,并根据需要激活族类型，FSN为空的时候赋值
        /// </summary>
        /// <param name="builtInCategory">族类型类别</param>
        /// <param name="FN">族名称</param>
        /// <param name="FSN">族类型名称</param>
        /// <param name="active">是否需要激活</param>
        /// <returns></returns>
        public FamilySymbol GetFamilySymbol(BuiltInCategory builtInCategory, string FN, ref string FSN, bool active)
        {

            FamilySymbol symbol = null;

            Transaction tr = new Transaction(Doc);

            try
            {
                //遍历类型集合
                IList<FamilySymbol> FsList = new FilteredElementCollector(Doc).OfClass(typeof(FamilySymbol)).OfCategory(builtInCategory).Cast<FamilySymbol>().ToList();

                foreach (Element elem in FsList)
                {
                    FamilySymbol fs = elem as FamilySymbol;

                    if (String.IsNullOrWhiteSpace(FSN))
                    {
                        if (fs.Family.Name == FN)
                        {
                            symbol = fs;
                            FSN = symbol.Name.ToString();
                            break;
                        }
                    }
                    else
                    {
                        if (fs.Family.Name == FN && fs.Name == FSN)
                        {
                            symbol = fs;
                            break;
                        }
                    }
                }

                if (symbol == null)
                {
                    if (FSN == null)
                    {
                        MessageBox.Show("查找不到族类型:" + FSN);
                    }
                    else
                    {
                        if (FsList.Where(o => o.FamilyName == FN).Count() > 0)
                        { MessageBox.Show("族 [" + FN + "] 下不存在族类型:" + FSN); }
                        else
                        { MessageBox.Show("查到不到族 [" + FN + "]"); }
                    }
                }
                if (active == true && symbol != null && symbol.IsActive == false)
                {
                    tr.Start("激活族类型：" + symbol.Name);
                    symbol.Activate();
                    tr.Commit();
                }
            }
            catch (Exception ex)
            {
                if (tr.HasStarted())
                {
                    tr.Commit();
                }
                MessageBox.Show(ex.ToString());
            }

            return symbol;
        }

        /// <summary>
        /// 得到视图顶部高程
        /// </summary>
        /// <param name="view">视图</param>
        /// <returns></returns>
        public double TopClipPlan(Autodesk.Revit.DB.View view)
        {
            double max_z = 1500 / 304.8;
            try
            {
                if (view is ViewPlan)
                {
                    ViewPlan viewplan = view as ViewPlan;
                    PlanViewRange viewRange = viewplan.GetViewRange(); //得到视图范围
                    ElementId topclipPlane = viewRange.GetLevelId(PlanViewPlane.TopClipPlane); //得到视图顶部标高Id
                    double doffset = viewRange.GetOffset(PlanViewPlane.TopClipPlane); //得到视图顶部偏移量
                    if (topclipPlane.IntegerValue > 0)
                    {
                        Level levelabove = Doc.GetElement(topclipPlane) as Level;
                        double level = levelabove.ProjectElevation;
                        max_z = level + doffset; //得到视图顶部高度
                    }
                    else
                    {
                        max_z = 1500 / 304.8;
                    }

                }
            }
            catch { }

            return max_z;
        }


        /// <summary>
        /// 得到视图底部高程
        /// </summary>
        /// <param name="view">视图</param>
        /// <returns></returns>
        public double BottomCLipPlan(Autodesk.Revit.DB.View view)
        {
            double min_z = -1500 / 304.8;
            try
            {
                if (view is ViewPlan)
                {
                    ViewPlan viewplan = view as ViewPlan;
                    PlanViewRange viewRange = viewplan.GetViewRange();//得到视图范围
                    ElementId bottomclipPlane = viewRange.GetLevelId(PlanViewPlane.BottomClipPlane);//得到视图底部标高Id
                    double Boffset = viewRange.GetOffset(PlanViewPlane.BottomClipPlane);//得到视图底部偏移量
                    if (bottomclipPlane.IntegerValue > 0)
                    {
                        Level levelBottom = Doc.GetElement(bottomclipPlane) as Level;
                        double level = levelBottom.ProjectElevation;
                        min_z = level + Boffset;//得到视图底部高度
                    }
                    else
                    {
                        min_z = -1500 / 304.8;
                    }

                }
            }
            catch { }

            return min_z;
        }


        /// <summary>
        /// 得到墙体构造层信息
        /// </summary>
        /// <param name="wall">所选墙体</param>
        /// <returns></returns>
        public List<WallFacing> GetWallCompoundStructureLayers(Wall wall)
        {
            WallType wty = wall.WallType;

            return GetWallCompoundStructureLayers(wty);
        }


        /// <summary>
        /// 得到墙体构造层信息
        /// </summary>
        /// <param name="wallty">墙体类型</param>
        /// <returns></returns>
        public List<WallFacing> GetWallCompoundStructureLayers(WallType wallty)
        {
            List<WallFacing> wallfacingList = new List<WallFacing>();

            CompoundStructure cs = wallty.GetCompoundStructure();

            IList<CompoundStructureLayer> layerList = cs.GetLayers();
            string materialName = "空";//害怕材质为空 处理替换层时 要判断一下 如果为空 则用默认的材质替换

            for (int i = 0; i < layerList.Count; i++)
            {
                CompoundStructureLayer layer = layerList[i];

                Material material = Doc.GetElement(layer.MaterialId) as Material;

                if (material == null)
                {
                    materialName = "空";
                }
                else
                {
                    materialName = material.Name;
                }
                wallfacingList.Add(new WallFacing(i, layer.Function, materialName, layer.Width * 304.8));
            }
            return wallfacingList;
        }






        #endregion //得到文档信息相关方法






        #region 轴网相关


        /// <summary>
        /// 返回所有横轴集合或者纵轴集合
        /// </summary>
        /// <param name="gridKind">轴网类别（横轴或纵轴）</param>
        /// <returns></returns>
        public List<Grid> GetGridList(GridKind gridKind)
        {
            List<Grid> returnList = new List<Grid>();

            List<Grid> gridList = new FilteredElementCollector(Doc).OfClass(typeof(Grid)).ToElements().Where(o => o is Grid).Cast<Grid>().ToList();

            if (gridKind == GridKind.HGrid)
            {
                returnList = gridList.Where(o => (o.Curve.GetEndPoint(1) - o.Curve.GetEndPoint(0)).Normalize().IsAlmostEqualTo(XYZ.BasisX, 1e-6)
                                              || (o.Curve.GetEndPoint(1) - o.Curve.GetEndPoint(0)).Normalize().IsAlmostEqualTo(XYZ.BasisX.Negate(), 1e-6)).ToList();
            }
            else if (gridKind == GridKind.VGrid)
            {
                returnList = gridList.Where(o => (o.Curve.GetEndPoint(1) - o.Curve.GetEndPoint(0)).Normalize().IsAlmostEqualTo(XYZ.BasisY, 1e-6)
                                              || (o.Curve.GetEndPoint(1) - o.Curve.GetEndPoint(0)).Normalize().IsAlmostEqualTo(XYZ.BasisY.Negate(), 1e-6)).ToList();
            }

            return returnList;
        }


        /// <summary>
        /// 返回所有轴网排序集合（横轴或者纵轴）
        /// </summary>
        /// <param name="gridKind">轴网类型</param>
        /// <returns></returns>
        public SortedDictionary<double, Grid> GetSortGridDic(GridKind gridKind)
        {
            SortedDictionary<double, Grid> returnDic = new SortedDictionary<double, Grid>();

            List<Grid> gridList = GetGridList(gridKind);

            if (gridKind == GridKind.HGrid)
            {
                foreach (Grid grid in gridList)
                {
                    XYZ start = grid.Curve.GetEndPoint(0);
                    XYZ end = grid.Curve.GetEndPoint(1);

                    if (!returnDic.Keys.Contains(start.Y))
                    {
                        returnDic.Add(start.Y, grid);
                    }
                    else
                    {
                        MessageBox.Show("存在重复轴网：" + grid.Name.ToString(), "提示");
                    }
                }
            }
            else if (gridKind == GridKind.VGrid)
            {
                foreach (Grid grid in gridList)
                {
                    XYZ start = grid.Curve.GetEndPoint(0);
                    XYZ end = grid.Curve.GetEndPoint(1);

                    if (!returnDic.Keys.Contains(start.X))
                    {
                        returnDic.Add(start.X, grid);
                    }
                    else
                    {
                        MessageBox.Show("存在重复轴网：" + grid.Name.ToString(), "提示");
                    }
                }

            }

            return returnDic;
        }


        /// <summary>
        /// 返回所有轴网排序集合（横轴或者纵轴）
        /// </summary>
        /// <param name="gridList">输入轴网集合</param>
        /// <param name="gridKind">轴网类型</param>
        /// <returns></returns>
        public SortedDictionary<double, Grid> GetSortGridDic(List<Grid> gridList, GridKind gridKind)
        {
            SortedDictionary<double, Grid> returnDic = new SortedDictionary<double, Grid>();

            if (gridKind == GridKind.HGrid)
            {
                foreach (Grid grid in gridList)
                {
                    XYZ start = grid.Curve.GetEndPoint(0);
                    XYZ end = grid.Curve.GetEndPoint(1);

                    if (!returnDic.Keys.Contains(start.Y))
                    {
                        returnDic.Add(start.Y, grid);
                    }
                    else
                    {
                        MessageBox.Show("存在重复轴网：" + grid.Name.ToString(), "提示");
                    }
                }
            }
            else if (gridKind == GridKind.VGrid)
            {
                foreach (Grid grid in gridList)
                {
                    XYZ start = grid.Curve.GetEndPoint(0);
                    XYZ end = grid.Curve.GetEndPoint(1);

                    if (!returnDic.Keys.Contains(start.X))
                    {
                        returnDic.Add(start.X, grid);
                    }
                    else
                    {
                        MessageBox.Show("存在重复轴网：" + grid.Name.ToString(), "提示");
                    }
                }

            }

            return returnDic;
        }


        /// <summary>
        /// 返回最近的横轴及其参考信息
        /// </summary>
        /// <param name="Hlist">所有横轴集合</param>
        /// <param name="y">已知纵坐标</param>
        /// <returns></returns>
        public KeyValuePair<double, Reference> HGrid_Close(SortedDictionary<double, Grid> Hlist, double y)
        {
            //控制变量
            KeyValuePair<double, Grid> min_G = new KeyValuePair<double, Grid>();//y下最近横轴
            KeyValuePair<double, Grid> max_G = new KeyValuePair<double, Grid>();//y上最近横轴

            KeyValuePair<double, Reference> ref_return_min = new KeyValuePair<double, Reference>();//y下最近参考
            KeyValuePair<double, Reference> ref_return_max = new KeyValuePair<double, Reference>();//y上最近参考

            foreach (KeyValuePair<double, Grid> item in Hlist)
            {
                if (item.Key <= y)
                {
                    min_G = item;
                }
                if (item.Key > y)
                {
                    max_G = item;
                    break;
                }
            }
            //排除边界情况
            if (min_G.Value == null)//说明对象在最左侧轴网外
            {
                ref_return_max = new KeyValuePair<double, Reference>(max_G.Key, new Reference(max_G.Value));
                return ref_return_max;
            }
            else if (max_G.Value == null)//说明对象在最右侧轴网外
            {
                ref_return_min = new KeyValuePair<double, Reference>(min_G.Key, new Reference(min_G.Value));
                return ref_return_min;
            }
            else
            {
                //得到下边界参考
                ref_return_min = new KeyValuePair<double, Reference>(min_G.Key, new Reference(min_G.Value));

                //得到上边界参考
                ref_return_max = new KeyValuePair<double, Reference>(max_G.Key, new Reference(max_G.Value));



                //对比上下那个更近一些

                if (Math.Abs(ref_return_max.Key - y) < Math.Abs(ref_return_min.Key - y))
                {
                    return ref_return_max;
                }
                else
                {
                    return ref_return_min;
                }
            }


        }


        /// <summary>
        /// 返回最近的纵轴及其参考信息
        /// </summary>
        /// <param name="Vlist">所有纵轴集合</param>
        /// <param name="x">已知横坐标</param>
        /// <returns></returns>
        public KeyValuePair<double, Reference> VGrid_Close(SortedDictionary<double, Grid> Vlist, double x)
        {
            //控制变量
            KeyValuePair<double, Grid> min_G = new KeyValuePair<double, Grid>();//x左最近横轴
            KeyValuePair<double, Grid> max_G = new KeyValuePair<double, Grid>();//x右最近横轴

            KeyValuePair<double, Reference> ref_return_min = new KeyValuePair<double, Reference>();//x左最近参考
            KeyValuePair<double, Reference> ref_return_max = new KeyValuePair<double, Reference>();//x右最近参考

            foreach (KeyValuePair<double, Grid> item in Vlist)
            {
                if (item.Key <= x)
                {
                    min_G = item;
                }
                if (item.Key > x)
                {
                    max_G = item;
                    break;
                }
            }

            //排除边界情况
            if (min_G.Value == null)//说明对象在最左侧轴网外
            {
                ref_return_max = new KeyValuePair<double, Reference>(max_G.Key, new Reference(max_G.Value));
                return ref_return_max;
            }
            else if (max_G.Value == null)//说明对象在最右侧轴网外
            {
                ref_return_min = new KeyValuePair<double, Reference>(min_G.Key, new Reference(min_G.Value));
                return ref_return_min;
            }
            else
            {
                //得到左边界参考

                ref_return_min = new KeyValuePair<double, Reference>(min_G.Key, new Reference(min_G.Value));

                //得到右边界参考

                ref_return_max = new KeyValuePair<double, Reference>(max_G.Key, new Reference(max_G.Value));



                //对比上下那个更近一些

                if (Math.Abs(ref_return_max.Key - x) < Math.Abs(ref_return_min.Key - x))
                {
                    return ref_return_max;
                }
                else
                {
                    return ref_return_min;
                }
            }

        }



        #region 相关枚举

        public enum CheckViewSet
        {
            IsViewPlan,
            IsView3D,
            IsViewSection,
            NoView3D
        }

        public enum GridKind
        {
            HGrid,
            VGrid
        }

        #endregion //相关枚举


        #endregion //轴网相关






        #region Element 参数获得相关方法

        /// <summary>
        /// 创建自建参数类实体，属性均为空
        /// </summary>
        /// <returns></returns>
        public RvtParameter NewRvtParameter()
        {
            #region 类属性初始化

            string name = null;
            string showName = null;
            ParaBelongs belongs = ParaBelongs.NULL;
            ParaKind kind = ParaKind.NULL;
            StorageType storType = StorageType.None;
            string value = null;
            string strValue = null;
            string code = null;
            bool isReadOnly = false;
            bool isShow = false;

            #endregion //类属性初始化

            //创建类
            RvtParameter rvtPara = new RvtParameter(name, showName, belongs, kind, storType, value, strValue, code, isReadOnly, isShow);

            return rvtPara;
        }


        /// <summary>
        /// 根据Revit对象参数，创建自建参数类实体,赋值属性：Name,StorageType,Value,StrValue,IsReadOnly
        /// </summary>
        /// <param name="p">已知Revit参数 Parameter p</param>
        /// <returns></returns>
        public RvtParameter NewRvtParameterFromParameter(Parameter p)
        {
            RvtParameter rvtPara = NewRvtParameter();

            #region 类属性赋值

            rvtPara.Name = p.Definition.Name.ToString();

            #region 参数存储类型 storageType 赋值、参数值 value 和 strValue 赋值

            rvtPara.StorType = p.StorageType;

            switch (p.StorageType)
            {
                case StorageType.Integer:

                    rvtPara.Value = p.AsInteger().ToString();
                    rvtPara.StrValue = p.AsValueString();
                    break;

                case StorageType.ElementId:

                    rvtPara.Value = p.AsElementId().ToString();
                    rvtPara.StrValue = p.AsValueString();
                    break;

                case StorageType.Double:

                    rvtPara.Value = p.AsDouble().ToString();
                    rvtPara.StrValue = p.AsValueString();
                    break;

                case StorageType.String:

                    rvtPara.Value = p.AsString().ToString();
                    rvtPara.StrValue = p.AsValueString();
                    break;

                default:
                    break;

            }

            #endregion //参数存储类型 storageType 赋值、参数值 value 和 strValue 赋值

            #region 参数是否只读 isReadOnly 赋值

            if (p.IsReadOnly == true)
            {
                rvtPara.IsReadOnly = true;
            }
            else
            {
                rvtPara.IsReadOnly = false;
            }

            #endregion //参数是否只读 isReadOnly 赋值

            //以上未赋值属性：showName，belongs，code，isShow

            #endregion //类属性赋值    

            return rvtPara;
        }


        /// <summary>
        /// 根据Element对象及参数名称关键字，创建自建参数类实体,赋值属性：Name,StorageType,Value,StrValue,IsReadOnly,Kind
        /// </summary>
        /// <param name="ele">Element对象</param>
        /// <param name="paraNameKeyWord">要遍历的参数名称关键字</param>
        /// <returns></returns>
        public RvtParameter GetRvtParameter(Element ele, string paraNameKeyWord)
        {
            // 创建自建参数类实体，属性均为空
            RvtParameter rvtPara = NewRvtParameter();

            foreach (Parameter p in ele.Parameters)
            {
                if (p.Definition.Name.Contains(paraNameKeyWord))
                {
                    //根据Revit对象参数，创建自建参数类实体,赋值属性：Name,StorageType,Value,StrValue,IsReadOnly
                    rvtPara = NewRvtParameterFromParameter(p);
                    break;
                }
            }

            #region 参数来源类型 kind 赋值

            if (ele is FamilyInstance)
            {
                rvtPara.Kind = ParaKind.FamInsPara;
            }
            else if (ele is FamilySymbol)
            {
                rvtPara.Kind = ParaKind.FamSymbolPara;
            }
            else if (ele is Wall)
            {
                rvtPara.Kind = ParaKind.WallPara;
            }
            else if (ele is Floor)
            {
                rvtPara.Kind = ParaKind.FloorPara;
            }
            else if (ele is RoofBase)
            {
                rvtPara.Kind = ParaKind.RoofBasePara;
            }
            else if (ele is Ceiling)
            {
                rvtPara.Kind = ParaKind.CeilingPara;
            }
            else if (ele is Opening)
            {
                rvtPara.Kind = ParaKind.OpeningPara;
            }
            else if (ele is Railing)
            {
                rvtPara.Kind = ParaKind.RailingPara;
            }
            else if (ele is Dimension)
            {
                rvtPara.Kind = ParaKind.DimensionPara;
            }
            else if (ele is IndependentTag)
            {
                rvtPara.Kind = ParaKind.IndependentTagPara;
            }
            else if (ele is ViewSheet)
            {
                rvtPara.Kind = ParaKind.ViewSheetPara;
            }
            else if (ele is ViewSchedule)
            {
                rvtPara.Kind = ParaKind.ViewSchedulePara;
            }

            #endregion //参数来源类型 kind 赋值

            return rvtPara;
        }


        /// <summary>
        /// 根据Element对象及参数名称关键字，创建自建参数类实体,返回所有满足关键字条件的参数类实体集合，赋值属性：Name,StorageType,Value,StrValue,IsReadOnly,Kind
        /// </summary>
        /// <param name="ele">Element对象</param>
        /// <param name="paraNameKeyWord">要遍历的参数名称关键字</param>
        /// <returns></returns>
        public List<RvtParameter> GetRvtParameters(Element ele, string paraNameKeyWord)
        {
            List<RvtParameter> rvtPlist = new List<RvtParameter>();

            foreach (Parameter p in ele.Parameters)
            {
                if (p.Definition.Name.Contains(paraNameKeyWord))
                {
                    //根据Revit对象参数，创建自建参数类实体,赋值属性：Name,StorageType,Value,StrValue,IsReadOnly
                    RvtParameter rvtPara = NewRvtParameterFromParameter(p);

                    #region 参数来源类型 kind 赋值

                    if (ele is FamilyInstance)
                    {
                        rvtPara.Kind = ParaKind.FamInsPara;
                    }
                    else if (ele is FamilySymbol)
                    {
                        rvtPara.Kind = ParaKind.FamSymbolPara;
                    }
                    else if (ele is Wall)
                    {
                        rvtPara.Kind = ParaKind.WallPara;
                    }
                    else if (ele is Floor)
                    {
                        rvtPara.Kind = ParaKind.FloorPara;
                    }
                    else if (ele is RoofBase)
                    {
                        rvtPara.Kind = ParaKind.RoofBasePara;
                    }
                    else if (ele is Ceiling)
                    {
                        rvtPara.Kind = ParaKind.CeilingPara;
                    }
                    else if (ele is Opening)
                    {
                        rvtPara.Kind = ParaKind.OpeningPara;
                    }
                    else if (ele is Railing)
                    {
                        rvtPara.Kind = ParaKind.RailingPara;
                    }
                    else if (ele is Dimension)
                    {
                        rvtPara.Kind = ParaKind.DimensionPara;
                    }
                    else if (ele is IndependentTag)
                    {
                        rvtPara.Kind = ParaKind.IndependentTagPara;
                    }
                    else if (ele is ViewSheet)
                    {
                        rvtPara.Kind = ParaKind.ViewSheetPara;
                    }
                    else if (ele is ViewSchedule)
                    {
                        rvtPara.Kind = ParaKind.ViewSchedulePara;
                    }

                    #endregion //参数来源类型 kind 赋值

                    rvtPlist.Add(rvtPara);
                }
            }

            return rvtPlist;
        }


        /// <summary>
        /// 得到Element多个参数并生成其自建参数类集合
        /// </summary>
        /// <param name="ele">Element对象</param>
        /// <param name="paraNameKeyWordList">要遍历的参数名称关键字集合</param>
        /// <returns></returns>
        public List<RvtParameter> GetRvtParameters(Element ele, List<string> paraNameKeyWordList)
        {
            List<RvtParameter> rvtParaList = new List<RvtParameter>();

            foreach (string paraNameKeyWord in paraNameKeyWordList)
            {
                rvtParaList.Add(GetRvtParameter(ele, paraNameKeyWord));
            }

            return rvtParaList;
        }

        #endregion //Element 参数获得相关方法






        #region 参数赋值相关方法

        /// <summary>
        /// 单个参数赋值方法（RvtParameter参数类方式）
        /// </summary>
        /// <param name="doc">RVT文档</param>
        /// <param name="p">需要赋值的参数</param>
        /// <param name="rvtPara">给参数赋值的类对象，用Value属性赋值</param>
        /// <returns></returns>
        public bool SetParameterValue(Document doc, Parameter p, RvtParameter rvtPara)
        {
            Transaction tr = new Transaction(doc);

            if (rvtPara.Name != p.Definition.Name.ToString())
            {
                MessageBox.Show("参数[" + p.Definition.Name + "]赋值参数名称不匹配！");
                return false;
            }

            if (p.IsReadOnly == true)
            {
                MessageBox.Show("参数[" + p.Definition.Name + "]为只读参数，无法赋值！");
                return false;
            }

            try
            {
                switch (p.StorageType)
                {
                    case StorageType.Integer:

                        if (String.IsNullOrWhiteSpace(rvtPara.Value) && !String.IsNullOrWhiteSpace(rvtPara.StrValue))
                        {
                            p.SetValueString(rvtPara.StrValue);
                        }
                        else
                        {
                            try
                            {
                                p.Set(Convert.ToInt16(rvtPara.Value));
                            }
                            catch
                            {
                                MessageBox.Show("为参数[" + p.Definition.Name + "]赋值的参数Value值必须可以转化为 Int16 形式！");
                                return false;
                            }
                        }

                        break;

                    case StorageType.ElementId:

                        if (String.IsNullOrWhiteSpace(rvtPara.Value) && !String.IsNullOrWhiteSpace(rvtPara.StrValue))
                        {
                            p.Set(new ElementId(Convert.ToInt16(rvtPara.StrValue)));
                        }
                        else
                        {
                            try
                            {
                                p.Set(new ElementId(Convert.ToInt16(rvtPara.Value)));
                            }
                            catch
                            {
                                MessageBox.Show("为参数[" + p.Definition.Name + "]赋值的参数Value值必须可以转化为 Int16（ElementId）形式！");
                                return false;
                            }
                        }

                        break;

                    case StorageType.Double:

                        if (String.IsNullOrWhiteSpace(rvtPara.Value) && !String.IsNullOrWhiteSpace(rvtPara.StrValue))
                        {
                            p.SetValueString(rvtPara.StrValue);
                        }
                        else
                        {
                            try
                            {
                                p.Set(Convert.ToDouble(rvtPara.Value));
                            }
                            catch
                            {
                                MessageBox.Show("为参数[" + p.Definition.Name + "]赋值的参数Value值必须可以转化为 Double 形式！");
                                return false;
                            }
                        }

                        break;

                    case StorageType.String:

                        if (String.IsNullOrWhiteSpace(rvtPara.Value) && !String.IsNullOrWhiteSpace(rvtPara.StrValue))
                        {
                            p.Set(rvtPara.StrValue);
                        }
                        else
                        {
                            p.Set(rvtPara.Value);
                        }
                        break;

                    default:
                        break;
                }
            }
            catch
            {
                MessageBox.Show("参数[" + p.Definition.Name + "]赋值失败！");
                return false;
            }

            return true;
        }


        /// <summary>
        /// 单个参数赋值方法（StrValue值方式）
        /// </summary>
        /// <param name="doc">文档</param>
        /// <param name="p">需要赋值的参数</param>
        /// <param name="strValue">StrValue值</param>
        /// <returns></returns>
        public bool SetParameterValueByStrValue(Document doc, Parameter p, string strValue)
        {
            Transaction tr = new Transaction(doc);

            if (p.IsReadOnly == true)
            {
                MessageBox.Show("参数[" + p.Definition.Name + "]为只读参数，无法赋值！");
                return false;
            }

            try
            {
                switch (p.StorageType)
                {
                    case StorageType.Integer:

                        if (strValue == "True")
                        {
                            p.Set(1);
                        }
                        else if (strValue == "False")
                        {
                            p.Set(0);
                        }
                        else
                        {
                            p.Set(Convert.ToInt16(strValue));
                        }
                        break;

                    case StorageType.ElementId:

                        try
                        {
                            p.Set(new ElementId(Convert.ToInt16(strValue)));
                        }
                        catch
                        {
                            MessageBox.Show("为参数[" + p.Definition.Name + "]赋值的参数Value值必须可以转化为 Int16（ElementId）形式！");
                            return false;
                        }
                        break;

                    case StorageType.Double:

                        p.SetValueString(strValue);
                        break;

                    case StorageType.String:

                        p.Set(strValue);
                        break;

                    default:
                        break;
                }
            }
            catch
            {
                MessageBox.Show("参数[" + p.Definition.Name + "]赋值失败！");
                return false;
            }

            return true;
        }

        /// <summary>
        /// 单个参数赋值方法(直接Set(double)方式）
        /// </summary>
        /// <param name="doc">文档</param>
        /// <param name="p">需要赋值的参数</param>
        /// <param name="doubleValue">Value值</param>
        /// <returns></returns>
        public bool SetParameterValueByValue(Document doc, Parameter p, double doubleValue)
        {
            Transaction tr = new Transaction(doc);

            if (p.IsReadOnly == true)
            {
                MessageBox.Show("参数[" + p.Definition.Name + "]为只读参数，无法赋值！");
                return false;
            }

            try
            {
                if (p.StorageType == StorageType.Double)
                {
                    p.Set(doubleValue);
                }
            }
            catch
            {
                MessageBox.Show("参数[" + p.Definition.Name + "]赋值失败！");
                return false;
            }

            return true;
        }



        /// <summary>
        /// 多个参数赋值方法（RvtParameter参数类方式）
        /// </summary>
        /// <param name="doc">RVT文档</param>
        /// <param name="pSet">需要赋值参数的集合</param>
        /// <param name="rvtParaList">给参数赋值的类对象集合，用Value属性赋值</param>
        public void SetParameterValues(Document doc, ParameterSet pSet, List<RvtParameter> rvtParaList)
        {
            //先除去rvtParaList中的Name为空的
            List<RvtParameter> rPList = rvtParaList.Where(o => !String.IsNullOrWhiteSpace(o.Name)).ToList();

            foreach (Parameter p in pSet)
            {
                if (p.IsReadOnly == false && rPList.Select(o => o.Name).ToList().Contains(p.Definition.Name.ToString()))
                {
                    //得到要赋值的对象
                    RvtParameter rvtP = rPList.Where(o => o.Name == p.Definition.Name.ToString()).FirstOrDefault();

                    if (rvtP != null)
                    {
                        bool succes = SetParameterValue(doc, p, rvtP);

                        if (succes == true)
                        {
                            //赋值完的对象要除去，提高效率也能避免重复
                            rPList.Remove(rvtP);
                        }
                    }

                }
            }
        }

        /// <summary>
        /// 多个参数赋值方法（StrValue值方式）
        /// </summary>
        /// <param name="doc">RVT文档</param>
        /// <param name="pSet">需要赋值参数的集合</param>
        /// <param name="strValueList">参数StrValue值的字典集合</param>
        public void SetParameterValuesByStrValue(Document doc, ParameterSet pSet, IDictionary<string, string> strValueList)
        {
            foreach (Parameter p in pSet)
            {
                if (p.IsReadOnly == false && strValueList.Keys.Contains(p.Definition.Name.ToString()))
                {
                    //得到要赋的参数值
                    string strValue = strValueList[p.Definition.Name.ToString()];

                    bool succes = SetParameterValueByStrValue(doc, p, strValue);
                }
            }
        }


        /// <summary>
        /// 多个参数赋值方法 (直接Set(double)方式）
        /// </summary>
        /// <param name="doc">RVT文档</param>
        /// <param name="pSet">需要赋值参数的集合</param>
        /// <param name="doubleValueList">参数Value值的字典集合</param>
        public void SetParameterValuesByValue(Document doc, ParameterSet pSet, IDictionary<string, double> doubleValueList)
        {
            foreach (Parameter p in pSet)
            {
                if (p.IsReadOnly == false && doubleValueList.Keys.Contains(p.Definition.Name.ToString()))
                {
                    //得到要赋的参数值
                    double doubleValue = doubleValueList[p.Definition.Name.ToString()];

                    bool succes = SetParameterValueByValue(doc, p, doubleValue);
                }
            }
        }

        #endregion //参数赋值相关方法




        #region 得到实例信息


        /// <summary>
        /// 得到基于线的实例位置线方向（如果为基于点或者位置线非直线则返回 XYZ.Zero）
        /// </summary>
        /// <param name="ins">已知实例</param>
        /// <returns></returns>
        public XYZ GetInsLineLocOri(FamilyInstance ins)
        {
            XYZ LocOri = XYZ.Zero;
            Location loc = ins.Location;

            if (loc is LocationCurve)
            {
                LocationCurve locCur = loc as LocationCurve;
                if (locCur.Curve is Line)
                {
                    Line l = locCur.Curve as Line;
                    LocOri = l.Direction;
                }
            }
            return LocOri;
        }








        /// <summary>
        /// 射线相交法得到所穿过的对象Id集合(FamilyInstance对象)
        /// </summary>
        /// <param name="doc">Rvt文档</param>
        /// <param name="view3d">需要应用射线相交的三维视图</param>
        /// <param name="origin">射线的起点</param>
        /// <param name="normal">射线的方向</param>
        /// <returns></returns>
        public IList<ElementId> ReferenceIntersectElement(Document doc, View3D view3d, XYZ origin, XYZ normal)
        {
            IList<ElementId> listId = new List<ElementId>();
            ElementClassFilter filter = new ElementClassFilter(typeof(FamilyInstance));

            ReferenceIntersector refInter = new ReferenceIntersector(filter, FindReferenceTarget.Element, view3d);
            IList<ReferenceWithContext> listContext = refInter.Find(origin, normal);
            foreach (ReferenceWithContext reference in listContext)
            {
                Reference refer = reference.GetReference();
                ElementId id = refer.ElementId;
                listId.Add(id);
            }
            return listId;
        }


        /// <summary>
        /// 通过射线相交法，得到已知直线（可以为端点直线或者射线）穿过的实例类集合
        /// </summary>
        /// <param name="doc">文档</param>
        /// <param name="v">应用射线相交法的三维视图，可以为空，为空的时候自动创建一个临时视图，运行后删除</param>
        /// <param name="l">已知直线（可以为端点直线或者射线）</param>
        /// <param name="filter">实例过滤器</param>
        /// <param name="frt">查到目标对象类型</param>
        /// <returns></returns>
        public List<RayProjectElement> RayProjectElements(Document doc, View3D v, Line l, ElementClassFilter filter, FindReferenceTarget frt)
        {
            Transaction tr = new Transaction(doc);
            List<RayProjectElement> rList = new List<RayProjectElement>();

            View3D view3D = v;

            try
            {
                if (v == null)
                {
                    #region 创建三维视图

                    tr.Start("创建三维视图");

                    //获取视图类型对象
                    ViewFamilyType vft = new FilteredElementCollector(doc).OfClass(typeof(ViewFamilyType)).Cast<ViewFamilyType>()
                                         .FirstOrDefault<ViewFamilyType>(x => ViewFamily.ThreeDimensional == x.ViewFamily);

                    view3D = View3D.CreateIsometric(doc, vft.Id);//生成一个新的等轴3D视图

                    tr.Commit();

                    #endregion //创建三维视图
                }

                ReferenceIntersector refInter = new ReferenceIntersector(filter, frt, view3D);
                refInter.FindReferencesInRevitLinks = false;
                IList<ReferenceWithContext> rwcList = refInter.Find(l.Origin, l.Direction);

                foreach (ReferenceWithContext rwc in rwcList)
                {
                    Reference refer = rwc.GetReference();

                    ElementId id = refer.ElementId;
                    Element elem = doc.GetElement(id);
                    XYZ xyz = refer.GlobalPoint;

                    if (l.IsBound)
                    {
                        if (xyz.DistanceTo(l.Origin) <= l.Length)
                        {
                            rList.Add(new RayProjectElement(elem, xyz, refer));
                        }
                    }
                    else
                    {
                        rList.Add(new RayProjectElement(elem, xyz, refer));
                    }
                }

                if (v == null)
                {
                    #region 删除三维视图

                    tr.Start("删除三维视图");

                    doc.Delete(view3D.Id);

                    tr.Commit();

                    #endregion //删除三维视图
                }

            }
            catch/* (Exception ex)*/
            {
                //MessageBox.Show(ex.ToString());
                if (tr.HasStarted()) { tr.Commit(); }
            }

            return rList;
        }



        /// <summary>
        /// 通过射线相交法，得到已知直线（可以为端点直线或者射线）与实例的交点集合
        /// </summary>
        /// <param name="doc">文档</param>
        /// <param name="e">所求实例</param>
        /// <param name="frt">查到目标对象类型</param>
        /// <param name="l">已知直线（可以为端点直线或者射线）</param>
        /// <returns></returns>
        public List<XYZ> RayProjectElementPoint(Document doc, Element e, FindReferenceTarget frt, Line l)
        {
            Transaction tr = new Transaction(doc);
            List<XYZ> rList = new List<XYZ>();

            try
            {
                #region 创建三维视图

                tr.Start("创建三维视图");

                //获取视图类型对象
                ViewFamilyType vft = new FilteredElementCollector(doc).OfClass(typeof(ViewFamilyType)).Cast<ViewFamilyType>()
                                     .FirstOrDefault<ViewFamilyType>(x => ViewFamily.ThreeDimensional == x.ViewFamily);

                View3D view3D = View3D.CreateIsometric(doc, vft.Id);//生成一个新的等轴3D视图

                tr.Commit();

                #endregion //创建三维视图

                string hostElemUniqueId = e.UniqueId.ToString();
                ReferenceIntersector refInter = new ReferenceIntersector(e.Id, frt, view3D);
                refInter.FindReferencesInRevitLinks = false;
                IList<ReferenceWithContext> rwcList = refInter.Find(l.Origin, l.Direction);

                foreach (ReferenceWithContext rwc in rwcList)
                {
                    XYZ xyz = rwc.GetReference().GlobalPoint;
                    string uid = rwc.GetReference().ConvertToStableRepresentation(doc);

                    if (l.IsBound)
                    {
                        if (xyz.DistanceTo(l.Origin) <= l.Length)
                        {
                            if (uid.Contains(hostElemUniqueId)) { rList.Add(xyz); }
                        }
                    }
                    else
                    {
                        if (uid.Contains(hostElemUniqueId)) { rList.Add(xyz); }
                    }
                }

                #region 删除三维视图

                tr.Start("删除三维视图");

                doc.Delete(view3D.Id);

                tr.Commit();

                #endregion //删除三维视图

            }
            catch/* (Exception ex)*/
            {
                //MessageBox.Show(ex.ToString());
                if (tr.HasStarted()) { tr.Commit(); }
            }

            return rList;
        }


        /// <summary>
        /// 通过射线相交法，得到已知直线（可以为端点直线或者射线）与链接文档中的实例的交点集合
        /// </summary>
        /// <param name="doc">当前文档</param>
        /// <param name="linkElemRef">链接实例引用信息</param>
        /// <param name="onlyHostElem">true：只得到选中链接实例的交点信息，false：得到整个链接文档中所有实例的交点信息</param>
        /// <param name="frt">查到目标对象类型</param>
        /// <param name="l">已知直线（可以为端点直线或者射线）</param>
        /// <returns></returns>
        public List<XYZ> RayProjectLinkElementPoint(Document doc, Reference linkElemRef, bool onlyHostElem, FindReferenceTarget frt, Line l)
        {
            Transaction tr = new Transaction(doc);
            List<XYZ> rList = new List<XYZ>();

            //得到链接对象信息
            Element linkElem = doc.GetElement(linkElemRef);
            Document linkDoc = (linkElem as RevitLinkInstance).GetLinkDocument();

            //获得链接文件转换矩阵
            //Transform linkTrans = linkIns.GetTotalTransform();

            //获得标准的族实例
            Element hostElem = linkDoc.GetElement(linkElemRef.LinkedElementId);
            string hostElemId = hostElem.Id.ToString();

            try
            {
                #region 创建三维视图

                tr.Start("创建视图");

                //获取视图类型对象
                ViewFamilyType vft = new FilteredElementCollector(doc).OfClass(typeof(ViewFamilyType)).Cast<ViewFamilyType>()
                                     .FirstOrDefault<ViewFamilyType>(x => ViewFamily.ThreeDimensional == x.ViewFamily);

                View3D view3D = View3D.CreateIsometric(doc, vft.Id);//生成一个新的等轴3D视图

                tr.Commit();

                #endregion //创建三维视图

                ReferenceIntersector refInter = new ReferenceIntersector(linkElem.Id, frt, view3D);
                refInter.FindReferencesInRevitLinks = true;
                IList<ReferenceWithContext> rwcList = refInter.Find(l.Origin, l.Direction);

                foreach (ReferenceWithContext rwc in rwcList)
                {
                    XYZ xyz = rwc.GetReference().GlobalPoint;
                    string uid = rwc.GetReference().ConvertToStableRepresentation(doc);

                    if (l.IsBound && xyz.DistanceTo(l.Origin) <= l.Length)
                    {
                        if (xyz.DistanceTo(l.Origin) <= l.Length)
                        {
                            if (onlyHostElem)
                            {
                                if (hostElemId == null || uid.Contains(hostElemId)) { rList.Add(xyz); }
                            }
                            else
                            {
                                rList.Add(xyz);
                            }
                        }
                    }
                    else
                    {
                        if (onlyHostElem)
                        {
                            if (hostElemId == null || uid.Contains(hostElemId)) { rList.Add(xyz); }
                        }
                        else
                        {
                            rList.Add(xyz);
                        }
                    }
                }

                #region 删除三维视图

                tr.Start("删除视图");

                doc.Delete(view3D.Id);

                tr.Commit();

                #endregion //删除三维视图
            }
            catch/* (Exception ex)*/
            {
                //MessageBox.Show(ex.ToString());
                if (tr.HasStarted()) { tr.Commit(); }
            }


            return rList;
        }







        /// <summary>
        /// 得到 Element 对象，在当前视图下，BoundingBox得到的各个方位点坐标集合
        /// </summary>
        /// <param name="e">需要操作的对象</param>
        /// <param name="view">需要操作的视图</param>
        /// <returns></returns>
        public Dictionary<PointLocKey, XYZ> GetElementBoundingBoxPoint(Element e, Autodesk.Revit.DB.View view)
        {

            Dictionary<PointLocKey, XYZ> dic = new Dictionary<PointLocKey, XYZ>();

            MathsCalculation mc = new MathsCalculation();
            BoundingBoxXYZ box = e.get_BoundingBox(view);
            XYZ max = mc.GetPointOnPlane(box.Max, view.Origin, view.ViewDirection);
            XYZ min = mc.GetPointOnPlane(box.Min, view.Origin, view.ViewDirection);
            XYZ mid = new XYZ((min.X + max.X) / 2, (min.Y + max.Y) / 2, (min.Z + max.Z) / 2);
            double h = mc.ProjectionLineOnOri(Line.CreateBound(min, max), view.UpDirection).Length;
            double w = mc.ProjectionLineOnOri(Line.CreateBound(min, max), view.RightDirection).Length;

            XYZ lt = mid - view.RightDirection * w / 2 + view.UpDirection * h / 2;
            XYZ rt = mid + view.RightDirection * w / 2 + view.UpDirection * h / 2;
            XYZ lb = mid - view.RightDirection * w / 2 - view.UpDirection * h / 2;
            XYZ rb = mid + view.RightDirection * w / 2 - view.UpDirection * h / 2;

            dic.Add(PointLocKey.Centre, mid);
            dic.Add(PointLocKey.LeftTop, lt);
            dic.Add(PointLocKey.RightTop, rt);
            dic.Add(PointLocKey.LeftBottom, lb);
            dic.Add(PointLocKey.RightBottom, rb);

            return dic;

        }



        /// <summary>
        /// 得到基于线构件在当前视图的相对方向(Y方向总是向着右侧，水平构件Y方向向着上侧)（X方向为顺着主体方向，Y方向为垂直主体方向）
        /// </summary>
        /// <param name="ele">构件对象</param>
        /// <param name="Xori">构件的右方向（总是沿着构件方向）</param>
        /// <param name="Yori">构件的上方向（总是垂直构件方向且冲着右侧，水平构件冲着上侧）</param>
        public void GetCurveElementViewRelativeOri(Element ele, ref XYZ Xori, ref XYZ Yori)
        {
            MathsCalculation mc = new MathsCalculation();
            Autodesk.Revit.DB.View view = UIdoc.ActiveView;

            XYZ locFOri = view.UpDirection;

            if (ele.Location is LocationCurve)
            {
                Curve hostLocCur = (ele.Location as LocationCurve).Curve;
                XYZ hostLocS = hostLocCur.GetEndPoint(0);
                XYZ hostLocE = hostLocCur.GetEndPoint(1);
                //基于线实例的位置线垂直方向(逆时针方向)
                locFOri = mc.GetNormalOnPlane((hostLocE - hostLocS), view.RightDirection, view.UpDirection, view.ViewDirection).Negate().Normalize();

                //第三四象限的主体方向要反向
                if ((hostLocE - hostLocS).Normalize().AngleTo(view.RightDirection.Negate()) <= Math.PI / 2 &&
                   !(hostLocE - hostLocS).Normalize().IsAlmostEqualTo(view.UpDirection.Negate(), 1e-6))
                { locFOri = mc.GetNormalOnPlane((hostLocS - hostLocE), view.RightDirection, view.UpDirection, view.ViewDirection).Negate().Normalize(); }

                Yori = locFOri;
                Xori = mc.GetNormalOnPlane(Yori, view.RightDirection, view.UpDirection, view.ViewDirection);
            }
        }


        /// <summary>
        /// 得到基于线构件的标记在构件垂直方向上的调整方向（基于线构件的标记放置初始位置：非竖直构件放置在下侧，竖直构件放置在右侧）（标记调整方向：非竖直垂直构件上侧，竖直构件左侧）（非基于线对象向着视图上侧）
        /// </summary>
        /// <param name="ele">构件对象</param>
        /// <returns></returns>
        public XYZ GetCurveElementTagMoveOri(Element ele)
        {
            MathsCalculation mc = new MathsCalculation();
            Autodesk.Revit.DB.View view = UIdoc.ActiveView;

            XYZ moveOri = view.UpDirection;

            if (ele.Location is LocationCurve)
            {
                //得到基于线实例在当前视图中的位置线投影方向
                XYZ LocOri = GetCurveElementViewOri(ele, view);

                //得到位置线投影方向的垂直方向(总是向着构件垂直方向上侧，构件竖直的时候向着视图右侧)
                XYZ LocFori = mc.GetFnormalOfVectorOnPlane(LocOri, view.RightDirection, view.UpDirection, view.ViewDirection);

                //将向着右侧的时候改为向着左侧
                if (LocFori.IsAlmostEqualTo(view.RightDirection, 1e-6))
                {
                    LocFori = LocFori.Negate();
                }
                moveOri = LocFori;
            }

            return moveOri;
        }


        /// <summary>
        /// 得到基于线构件在已知视图的投影方向
        /// </summary>
        /// <param name="ele">构件对象</param>
        /// <param name="view">已知视图</param>
        public XYZ GetCurveElementViewOri(Element ele, Autodesk.Revit.DB.View view)
        {
            MathsCalculation mc = new MathsCalculation();
            Curve hostLocCur = (ele.Location as LocationCurve).Curve;
            XYZ hostLocS = hostLocCur.GetEndPoint(0);
            XYZ hostLocE = hostLocCur.GetEndPoint(1);
            if (mc.GetPointOnPlane(hostLocS, view.Origin, view.ViewDirection).DistanceTo(mc.GetPointOnPlane(hostLocE, view.Origin, view.ViewDirection)) < 1e-6) { return XYZ.Zero; }
            XYZ viewOri = mc.GetLineOnPlane(Line.CreateBound(hostLocS, hostLocE), view.Origin, view.ViewDirection).Direction;
            return viewOri;
        }


        /// <summary>
        /// 得到标记IndependentTag的宽度和高度
        /// </summary>
        /// <param name="tag">标记对象</param>
        /// <param name="w">标记对象的宽度</param>
        /// <param name="h">标记对象的高度</param>
        public void GetIndependentTagWidthHeight(IndependentTag tag, ref double w, ref double h)
        {
            if (String.IsNullOrWhiteSpace(tag.TagText)) { w = 0; h = 0; return; }
            MathsCalculation mc = new MathsCalculation();
            int scale = UIdoc.ActiveView.get_Parameter(BuiltInParameter.VIEW_SCALE).AsInteger();

            Element hostE = tag.GetTaggedLocalElement();
            Autodesk.Revit.DB.View view = UIdoc.ActiveView;
            IDictionary<PointLocKey, XYZ> tagBoxPList = GetElementBoundingBoxPoint(tag, view);

            if (hostE.Location is LocationCurve)
            {
                //标记主体在当前视图投影线方向
                XYZ hostOri = GetCurveElementViewOri(hostE, view);
                if (hostOri.IsAlmostEqualTo(XYZ.Zero)) { w = 0; h = 0; return; }


                //标记主体水平
                if (hostOri.IsAlmostEqualTo(view.RightDirection, 1e-4) || hostOri.IsAlmostEqualTo(view.RightDirection.Negate(), 1e-4))
                {
                    w = tagBoxPList[PointLocKey.LeftTop].DistanceTo(tagBoxPList[PointLocKey.RightTop]);
                    h = tagBoxPList[PointLocKey.LeftTop].DistanceTo(tagBoxPList[PointLocKey.LeftBottom]);
                }

                //标记主体垂直
                else if (hostOri.IsAlmostEqualTo(view.UpDirection, 1e-4) || hostOri.IsAlmostEqualTo(view.UpDirection.Negate(), 1e-4))
                {
                    w = tagBoxPList[PointLocKey.LeftTop].DistanceTo(tagBoxPList[PointLocKey.LeftBottom]);
                    h = tagBoxPList[PointLocKey.LeftTop].DistanceTo(tagBoxPList[PointLocKey.RightTop]);
                }
                else
                {
                    #region 标记主体不是水平或者竖直的时候，无法计算标记的宽高，因为box的内接矩形有无数多个，所以先计算个大概的数值吧

                    ////获取两个对角线
                    //Line l1 = Line.CreateBound(tagBoxPList[PointLocKey.LeftTop], tagBoxPList[PointLocKey.RightBottom]);
                    //Line l2 = Line.CreateBound(tagBoxPList[PointLocKey.LeftBottom], tagBoxPList[PointLocKey.RightTop]);

                    ////得到标记主体的垂直投影方向
                    //XYZ hostFOri = mc.GetNormalOnPlane(hostOri, view.RightDirection, view.UpDirection, view.ViewDirection);

                    //if (mc.ProjectionLineOnOri(l1, hostOri) == null) { w = mc.ProjectionLineOnOri(l2, hostOri).Length; }
                    //else if (mc.ProjectionLineOnOri(l2, hostOri) == null) { w = mc.ProjectionLineOnOri(l1, hostOri).Length; }
                    //else { w = Math.Max(mc.ProjectionLineOnOri(l1, hostOri).Length, mc.ProjectionLineOnOri(l2, hostOri).Length); }

                    //if (mc.ProjectionLineOnOri(l1, hostFOri) == null) { w = mc.ProjectionLineOnOri(l2, hostFOri).Length; }
                    //else if (mc.ProjectionLineOnOri(l2, hostFOri) == null) { w = mc.ProjectionLineOnOri(l1, hostFOri).Length; }
                    //else { h = Math.Max(mc.ProjectionLineOnOri(l1, hostFOri).Length, mc.ProjectionLineOnOri(l2, hostFOri).Length); }                    

                    #endregion  //标记主体不是水平或者竖直的时候，无法计算标记的宽高，因为box的内接矩形有无数多个，所以先计算个大概的数值吧

                    #region 标记主体不是水平或者竖直的时候，无法计算标记的宽高，因为box的内接矩形有无数多个，所以高先按照2.5*视图比例计算吧


                    //获取两个对角线
                    Line l1 = Line.CreateBound(tagBoxPList[PointLocKey.LeftTop], tagBoxPList[PointLocKey.RightBottom]);
                    Line l2 = Line.CreateBound(tagBoxPList[PointLocKey.LeftBottom], tagBoxPList[PointLocKey.RightTop]);

                    //得到标记主体的垂直投影方向
                    XYZ hostFOri = mc.GetNormalOnPlane(hostOri, view.RightDirection, view.UpDirection, view.ViewDirection);


                    if (l1.Direction.IsAlmostEqualTo(hostOri) || l1.Direction.IsAlmostEqualTo(hostOri.Negate()) || mc.ProjectionLineOnOri(l1, hostOri) == null) { w = mc.ProjectionLineOnOri(l2, hostOri).Length; }
                    else if (l2.Direction.IsAlmostEqualTo(hostOri) || l2.Direction.IsAlmostEqualTo(hostOri.Negate()) || mc.ProjectionLineOnOri(l2, hostOri) == null) { w = mc.ProjectionLineOnOri(l1, hostOri).Length; }
                    else { w = Math.Max(mc.ProjectionLineOnOri(l1, hostOri).Length, mc.ProjectionLineOnOri(l2, hostOri).Length); }

                    h = 2.5 / 304.8 * scale;

                    #endregion  //标记主体不是水平或者竖直的时候，无法计算标记的宽高，因为box的内接矩形有无数多个，所以高先按照2.5*视图比例计算吧
                }
            }
            else
            {
                w = tagBoxPList[PointLocKey.LeftTop].DistanceTo(tagBoxPList[PointLocKey.RightTop]);
                h = tagBoxPList[PointLocKey.LeftTop].DistanceTo(tagBoxPList[PointLocKey.LeftBottom]);
            }
        }



        #endregion //得到实例信息






        #region 得到实例几何信息及参照相关方法

        /// <summary>
        /// 得到Element对象的几何对象
        /// </summary>
        /// <param name="e">element对象</param>
        /// <param name="viewDetailLevel">视图详细程度</param>
        /// <param name="IncludeNonVisibleObjects">是否包含不可见对象</param>
        /// <returns></returns>
        public GeometryElement GetGeomElem(Element e, ViewDetailLevel viewDetailLevel, bool IncludeNonVisibleObjects)
        {
            //创建新的包含几何信息的几何对象
            Options option = new Options();
            option.ComputeReferences = true;//获取几何对象的引用，获取的点线面对象带有参考信息

            option.DetailLevel = viewDetailLevel;//设置所提取的对象的集合表达详细程度

            option.IncludeNonVisibleObjects = IncludeNonVisibleObjects;//得到不可见的几何对象
            GeometryElement geomElement = e.get_Geometry(option);//提取选取对象中包含的所有几何对象option到集合geomElement中

            return geomElement;
        }


        /// <summary>
        /// 得到Element对象在某一视图中的几何对象
        /// </summary>
        /// <param name="e">element对象</param>
        /// <param name="view">相应视图</param>
        /// <param name="IncludeNonVisibleObjects">是否包含不可见对象</param>
        /// <returns></returns>
        public GeometryElement GetGeomElemInView(Element e, Autodesk.Revit.DB.View view, bool IncludeNonVisibleObjects)
        {
            //创建新的包含几何信息的几何对象
            Options option = new Options();
            option.ComputeReferences = true;//获取几何对象的引用，获取的点线面对象带有参考信息

            option.View = view;

            option.IncludeNonVisibleObjects = IncludeNonVisibleObjects;//得到不可见的几何对象
            GeometryElement geomElement = e.get_Geometry(option);//提取选取对象中包含的所有几何对象option到集合geomElement中

            return geomElement;
        }



        /// <summary>
        /// 得到几何对象的转换矩阵
        /// </summary>
        /// <param name="geomElem">几何对象</param>
        /// <returns></returns>
        public Transform GetGeomElemTransform(GeometryElement geomElem)
        {
            Transform trans = null;

            //如果对象为标准族，或者被切割了的系统族,则需要进行坐标的转换
            foreach (GeometryObject geom in geomElem)
            {
                GeometryInstance geom_Ins = geom as GeometryInstance;
                if (null != geom_Ins && geom_Ins.GetSymbolGeometry().Count() != 0)
                {
                    trans = geom_Ins.Transform;
                }
            }

            return trans;
        }



        /// <summary>
        /// 得到几何对象中的所有几何体
        /// </summary>
        /// <param name="geomElem">几何对象</param>
        /// <returns></returns>
        public IList<Solid> GetGeomElemSolides(GeometryElement geomElem)
        {
            IList<Solid> solids = new List<Solid>();//创建所有几何体的集合
            AddSolids(geomElem, solids);

            return solids;
        }

        /// <summary>
        /// 得到几何对象中的所有边
        /// </summary>
        /// <param name="solids">几何对象中的所有几何体</param>
        /// <returns></returns>
        public IList<Edge> GetGeomElemEdges(IList<Solid> solids)
        {
            IList<Edge> edges = new List<Edge>();//创建几何体所有边的集合
            if (solids.Count > 0)
            {
                AddEdges(solids, edges);
            }

            return edges;
        }

        /// <summary>
        /// 得到几何对象中的所有面
        /// </summary>
        /// <param name="solids">几何对象中的所有几何体</param>
        /// <returns></returns>
        public IList<Face> GetGeomElemFaces(IList<Solid> solids)
        {
            IList<Face> faces = new List<Face>();//创建几何体所有面的集合

            if (solids.Count > 0)
            {
                AddFaces(solids, faces);
            }

            return faces;
        }

        /// <summary>
        /// 得到几何对象中的所有曲线
        /// </summary>
        /// <param name="geomElem">几何对象</param>
        /// <returns></returns>
        public IList<Curve> GetGeomElemCurves(GeometryElement geomElem)
        {
            IList<Curve> curves = new List<Curve>();//创建所有线的集合
            AddCurves(geomElem, curves);

            return curves;
        }

        /// <summary>
        /// 得到几何对象中的所有直线
        /// </summary>
        /// <param name="geomElem">几何对象</param>
        /// <returns></returns>
        public IList<Line> GetGeomElemLines(GeometryElement geomElem)
        {
            IList<Line> lines = new List<Line>();//创建所有直线的集合
            AddLines(geomElem, lines);

            return lines;
        }


        /// <summary>
        /// 得到几何对象中的所有曲线
        /// </summary>
        /// <param name="geomElem">几何对象</param>
        /// <returns></returns>
        public IList<Arc> GetGeomElemArcs(GeometryElement geomElem)
        {
            IList<Arc> arcs = new List<Arc>();//创建所有直线的集合
            AddArcs(geomElem, arcs);

            return arcs;
        }


        /// <summary>
        /// 从几何体的边中得到所有点和点的引用
        /// </summary>
        /// <param name="edges">几何体中的边集合</param>
        /// <param name="tf">几何体的转换矩阵</param>
        /// <returns></returns>
        public IDictionary<XYZ, Reference> GetPointRefDic(IList<Edge> edges, Transform tf)
        {
            //创建点和点的引用集合
            IDictionary<XYZ, Reference> pointRefDic = new Dictionary<XYZ, Reference>();

            if (edges.Count > 0)
            {
                foreach (Edge e in edges)
                {
                    Curve cur = e.AsCurve();

                    //注意，每个点的坐标要进行坐标转换
                    if (tf != null)
                    {
                        if (!pointRefDic.Keys.Contains(tf.OfPoint(cur.GetEndPoint(0))))
                        {
                            pointRefDic.Add(tf.OfPoint(cur.GetEndPoint(0)), e.GetEndPointReference(0));
                        }
                        if (!pointRefDic.Keys.Contains(tf.OfPoint(cur.GetEndPoint(1))))
                        {
                            pointRefDic.Add(tf.OfPoint(cur.GetEndPoint(1)), e.GetEndPointReference(1));
                        }
                    }
                    else
                    {
                        if (!pointRefDic.Keys.Contains(cur.GetEndPoint(0)))
                        {
                            pointRefDic.Add(cur.GetEndPoint(0), e.GetEndPointReference(0));
                        }
                        if (!pointRefDic.Keys.Contains(cur.GetEndPoint(1)))
                        {
                            pointRefDic.Add(cur.GetEndPoint(1), e.GetEndPointReference(1));
                        }
                    }
                }
            }

            return pointRefDic;
        }


        /// <summary>
        /// 从几何体的直线中得到所有点和点的引用
        /// </summary>
        /// <param name="lines">几何体中的直线集合</param>
        /// <param name="tf">几何体的转换矩阵</param>
        /// <returns></returns>
        public IDictionary<XYZ, Reference> GetPointRefDic(IList<Line> lines, Transform tf)
        {
            //创建点和点的引用集合
            IDictionary<XYZ, Reference> pointRefDic = new Dictionary<XYZ, Reference>();

            if (lines.Count > 0)
            {
                foreach (Line l in lines)
                {
                    //注意，每个点的坐标要进行坐标转换
                    if (tf != null)
                    {
                        if (!pointRefDic.Keys.Contains(tf.OfPoint(l.GetEndPoint(0))))
                        {
                            pointRefDic.Add(tf.OfPoint(l.GetEndPoint(0)), l.GetEndPointReference(0));
                        }
                        if (!pointRefDic.Keys.Contains(tf.OfPoint(l.GetEndPoint(1))))
                        {
                            pointRefDic.Add(tf.OfPoint(l.GetEndPoint(1)), l.GetEndPointReference(1));
                        }
                    }
                    else
                    {
                        if (!pointRefDic.Keys.Contains(l.GetEndPoint(0)))
                        {
                            pointRefDic.Add(l.GetEndPoint(0), l.GetEndPointReference(0));
                        }
                        if (!pointRefDic.Keys.Contains(l.GetEndPoint(1)))
                        {
                            pointRefDic.Add(l.GetEndPoint(1), l.GetEndPointReference(1));
                        }
                    }
                }
            }

            return pointRefDic;
        }


        /// <summary>
        /// 通过替换参考中的字符串方式得到新的参考，已知参考用":"分割并替换相应位置的字符串
        /// </summary>
        /// <param name="refer">原参考</param>
        /// <param name="replaceStrDic">替换字符串字典（替换位置序号，替换字符串）</param>
        /// <returns></returns>
        public Reference GetReplaceStrRef(Reference refer, IDictionary<int, string> replaceStrDic)
        {
            Reference newrefer = null;
            if (refer != null)
            {
                String[] refTokens = refer.ConvertToStableRepresentation(Doc).Split(':');

                String newStableRef = null;

                for (int i = 0; i < refTokens.Count(); i++)
                {
                    if (replaceStrDic.Keys.Contains(i))
                    {
                        if (replaceStrDic.Where(o => o.Key == i).First().Value != null)
                        { newStableRef = newStableRef + replaceStrDic.Where(o => o.Key == i).First().Value + ":"; }
                    }
                    else
                    {
                        newStableRef = newStableRef + refTokens[i] + ":";
                    }
                }

                if (newStableRef.EndsWith(":")) { newStableRef = newStableRef.Remove(newStableRef.LastIndexOf(':')); }

                newrefer = Reference.ParseFromStableRepresentation(Doc, newStableRef);
            }
            else
            {
                MessageBox.Show("提供参考不能为空!");
            }
            return newrefer;
        }


        /// <summary>
        /// 得到几何对象在已知视图下所有投影点集合
        /// </summary>
        /// <param name="e">实例对象</param>
        /// <param name="view">已知视图</param>
        /// <returns></returns>
        public List<XYZ> GetGeomElemPointsOnView(Element e, Autodesk.Revit.DB.View view)
        {
            MathsCalculation mc = new MathsCalculation();
            //得到对象在某一视图中的几何对象
            GeometryElement geomElem = GetGeomElemInView(e, view, true);


            //得到几何对象的转换矩阵
            Transform tf = GetGeomElemTransform(geomElem);
            //if (e.Location is LocationCurve) { tf = null; }

            //得到几何对象中的所有几何体
            IList<Solid> solids = GetGeomElemSolides(geomElem);

            //得到几何对象中的所有边
            IList<Edge> edges = GetGeomElemEdges(solids);

            //从几何体的边中得到所有点和点的引用
            IDictionary<XYZ, Reference> pointRefDic = GetPointRefDic(edges, tf);
            if (e.Location is LocationCurve)
            {
                XYZ start = (e.Location as LocationCurve).Curve.GetEndPoint(0);
                XYZ end = (e.Location as LocationCurve).Curve.GetEndPoint(1);

                if (pointRefDic.Where(o => o.Key.DistanceTo(start) > start.DistanceTo(end) * 5).Count() > 0)
                {
                    pointRefDic.Clear();
                    pointRefDic = GetPointRefDic(edges, null);
                }
            }

            //得到所有点在当前视图中的投影点集合
            List<XYZ> viewPList = pointRefDic.Keys.Select(o => mc.GetPointOnPlane(o, view.Origin, view.ViewDirection)).ToList();

            return viewPList;
        }




        /// <summary>
        /// 得到几何对象在当前视图下的所有几何直线的投影线集合
        /// </summary>
        /// <param name="e">对象</param>
        /// <param name="view">视图</param>
        /// <returns></returns>
        public IList<Line> GetElemOnViewLines(Element e, Autodesk.Revit.DB.View view)
        {
            MathsCalculation mc = new MathsCalculation();
            IList<Line> lines = new List<Line>();

            try
            {

                //得到对象位置基准，为了判断是否坐标转化
                XYZ loc = XYZ.Zero;
                if (e.Location is LocationPoint) { loc = (e.Location as LocationPoint).Point; }
                else if (e.Location is LocationCurve) { loc = (e.Location as LocationCurve).Curve.GetEndPoint(0); }


                GeometryElement geomElem = GetGeomElemInView(e, view, false);
                //得到几何对象的转换矩阵
                Transform tf = GetGeomElemTransform(geomElem);
                //得到几何对象中的所有几何体
                IList<Solid> solids = GetGeomElemSolides(geomElem);
                //得到几何对象中的所有边
                IList<Edge> edges = GetGeomElemEdges(solids);
                //得到所有投影的直线集合
                lines = edges.Where(o => o.AsCurve() is Line)
                        .Where(o => !((o.AsCurve() as Line).Direction.IsAlmostEqualTo(view.ViewDirection)
                        || (o.AsCurve() as Line).Direction.IsAlmostEqualTo(view.ViewDirection.Negate()))).
                        Select(o => o.AsCurve() as Line).ToList();

                //判断点是否需要坐标转化
                if (tf != null && tf.OfPoint(lines.First().GetEndPoint(0)).DistanceTo(loc) < 100000 / 304.8)
                {
                    IList<Line> llist = lines.ToList();
                    lines.Clear();
                    lines = llist.Select(o => mc.GetLineOnPlane(Line.CreateBound(tf.OfPoint(o.GetEndPoint(0)), tf.OfPoint(o.GetEndPoint(1))), view.Origin, view.ViewDirection)).ToList();
                }
                else
                {
                    IList<Line> llist = lines.ToList();
                    lines.Clear();
                    lines = llist.Select(o => mc.GetLineOnPlane(o, view.Origin, view.ViewDirection)).ToList();
                }

            }
            catch { }

            return lines;
        }


        /// <summary>
        /// 得到基于线对象在已知视图中的投影宽度
        /// </summary>
        /// <param name="e"></param>
        /// <param name="view">已知视图</param>
        /// <returns></returns>
        public double GetCurveElementWidth(Element e, Autodesk.Revit.DB.View view)
        {
            double maxW = 0;

            MathsCalculation mc = new MathsCalculation();

            //得到所有几何体中的点在当前视图中的投影点集合
            List<XYZ> viewPList = GetGeomElemPointsOnView(e, view).Distinct().ToList();

            Curve cur = (e.Location as LocationCurve).Curve;
            XYZ FnorOri = view.UpDirection;
            //与视图方向一致的对象情况
            if (mc.GetPointOnPlane(cur.GetEndPoint(0), view.Origin, view.ViewDirection).IsAlmostEqualTo(mc.GetPointOnPlane(cur.GetEndPoint(1), view.Origin, view.ViewDirection), 1e-6))
            {
                FnorOri = view.UpDirection;
            }
            else
            {
                //得到基于线对象在已知视图的投影方向
                XYZ LocOri = GetCurveElementViewOri(e, view);

                //得到基于线对象在已知视图的垂直投影方向
                FnorOri = mc.GetNormalOnPlane(LocOri, view.RightDirection, view.UpDirection, view.ViewDirection);
            }

            //得到投影点在垂直方向上面的最大距离

            if (viewPList.Count > 0)
            {
                XYZ x1 = mc.GetExtremePointOnOri(viewPList, FnorOri, MathsCalculation.ExtremeWay.Max);
                XYZ x2 = mc.GetExtremePointOnOri(viewPList, FnorOri, MathsCalculation.ExtremeWay.Min);
                if (x1.DistanceTo(x2) > 0)
                {
                    Line l = Line.CreateBound(x1, x2);
                    maxW = mc.ProjectionLineOnOri(l, FnorOri).Length;
                }
            }

            return maxW;
        }



        /// <summary>
        /// 得到对象在已知视图中的投影中心点坐标，及其几何宽度和几何高度
        /// </summary>
        /// <param name="e">对象</param>
        /// <param name="w">其几何宽度</param>
        /// <param name="h">其几何高度</param>
        /// <param name="view">视图</param>
        /// <returns></returns>
        public XYZ GetPointElementCenter(Element e, ref double w, ref double h, Autodesk.Revit.DB.View view)
        {
            MathsCalculation mc = new MathsCalculation();
            XYZ mid = XYZ.Zero;
            if (e.Location is LocationPoint locP)
            {
                mid = mc.GetPointOnPlane(locP.Point, view.Origin, view.ViewDirection);
            }
            else if (e.Location is LocationCurve locC)
            {
                mid = mc.GetPointOnPlane(mc.GetMidPoint(locC.Curve.GetEndPoint(0), locC.Curve.GetEndPoint(1)), view.Origin, view.ViewDirection);
            }


            try
            {
                //得到所有几何体中的点在当前视图中的投影点集合
                List<XYZ> viewPList = GetGeomElemPointsOnView(e, view).Distinct().ToList();

                //得到视图方向
                XYZ viewR = view.RightDirection;
                XYZ viewU = view.UpDirection;

                //得到投影点在水平方向上面的最大距离
                double maxW = 0;
                if (viewPList.Count > 0)
                {
                    XYZ max = mc.GetExtremePointOnOri(viewPList, viewR, MathsCalculation.ExtremeWay.Max);
                    XYZ min = mc.GetExtremePointOnOri(viewPList, viewR, MathsCalculation.ExtremeWay.Min);
                    if (!max.IsAlmostEqualTo(min))
                    {
                        Line l = Line.CreateBound(min, max);
                        if (!(l.Direction.IsAlmostEqualTo(viewU) || l.Direction.IsAlmostEqualTo(viewU.Negate())))
                        {
                            maxW = mc.ProjectionLineOnOri(l, viewR).Length;
                        }

                    }
                }
                w = maxW;

                //得到投影点在垂直方向上面的最大距离
                double maxH = 0;
                if (viewPList.Count > 0)
                {
                    XYZ max = mc.GetExtremePointOnOri(viewPList, viewU, MathsCalculation.ExtremeWay.Max);
                    XYZ min = mc.GetExtremePointOnOri(viewPList, viewU, MathsCalculation.ExtremeWay.Min);
                    if (!max.IsAlmostEqualTo(min))
                    {
                        Line l = Line.CreateBound(min, max);
                        if (!(l.Direction.IsAlmostEqualTo(viewR) || l.Direction.IsAlmostEqualTo(viewR.Negate())))
                        {
                            maxH = mc.ProjectionLineOnOri(l, viewU).Length;
                        }

                    }
                }

                h = maxH;

                //得到中心点
                XYZ midU = mc.GetExtremePointOnOri(viewPList, viewU, MathsCalculation.ExtremeWay.Min) + viewU * h / 2;
                XYZ midR = mc.GetExtremePointOnOri(viewPList, viewR, MathsCalculation.ExtremeWay.Min) + viewR * w / 2;
                Line l1 = Line.CreateBound(midU + viewR * 10000, midU - viewR * 10000);
                Line l2 = Line.CreateBound(midR + viewU * 10000, midR - viewU * 10000);
                mid = mc.Line_IEresult(l1, l2);
            }
            catch { }

            return mid;
        }


        /// <summary>
        /// 得到对象在已知视图中的投影中心点坐标
        /// </summary>
        /// <param name="e">对象</param>
        /// <param name="view">已知视图</param>
        /// <returns></returns>
        public XYZ GetElementCenter(Element e, Autodesk.Revit.DB.View view)
        {
            XYZ mid = XYZ.Zero;
            double w = 0;
            double h = 0;

            mid = GetPointElementCenter(e, ref w, ref h, view);

            return mid;
        }



        #region 几何辅助方法


        private void AddSolids(GeometryElement geomElem, IList<Solid> solids)
        {
            foreach (GeometryObject geomObject in geomElem)
            {
                Solid solid = geomObject as Solid;
                if (null != solid)
                {
                    solids.Add(solid);
                    continue;
                }
                //如果GeometryObject是几何实例，则进行二次遍历
                GeometryInstance geomInst = geomObject as GeometryInstance;
                if (null != geomInst)
                {
                    AddSolids(geomInst.GetSymbolGeometry(), solids);
                }
            }
        }

        private void AddEdges(IList<Solid> solids, IList<Edge> Edges)
        {
            foreach (Solid solid in solids)
            {
                //遍历几何体中的所有边对象
                if (null != solid.Edges)
                {
                    foreach (Edge edge in solid.Edges)
                    {
                        Edges.Add(edge);
                    }
                }
            }
        }
        private void AddFaces(IList<Solid> solids, IList<Face> Faces)
        {
            foreach (Solid solid in solids)
            {
                //遍历几何体中的所有面对象
                if (null != solid.Faces)
                {
                    foreach (Face face in solid.Faces)
                    {
                        Faces.Add(face);
                    }
                }
            }
        }

        private void AddCurves(GeometryElement geomElem, IList<Curve> curves)
        {
            foreach (GeometryObject geomObject in geomElem)
            {
                Curve curve = geomObject as Curve;
                if (null != curve)
                {
                    curves.Add(curve);
                    continue;
                }
                //如果GeometryObject是几何实例，则进行二次遍历
                GeometryInstance geomInst = geomObject as GeometryInstance;
                if (null != geomInst)
                {
                    AddCurves(geomInst.GetSymbolGeometry(), curves);
                }
            }

        }

        private void AddLines(GeometryElement geomElem, IList<Line> lines)
        {
            foreach (GeometryObject geomObject in geomElem)
            {
                Line line = geomObject as Line;
                if (null != line)
                {
                    lines.Add(line);
                    continue;
                }
                //如果GeometryObject是几何实例，则进行二次遍历
                GeometryInstance geomInst = geomObject as GeometryInstance;
                if (null != geomInst)
                {
                    AddLines(geomInst.GetSymbolGeometry(), lines);
                }
            }

        }

        private void AddArcs(GeometryElement geomElem, IList<Arc> arcs)
        {
            foreach (GeometryObject geomObject in geomElem)
            {
                Arc arc = geomObject as Arc;
                if (null != arc)
                {
                    arcs.Add(arc);
                    continue;
                }
                //如果GeometryObject是几何实例，则进行二次遍历
                GeometryInstance geomInst = geomObject as GeometryInstance;
                if (null != geomInst)
                {
                    AddArcs(geomInst.GetSymbolGeometry(), arcs);
                }
            }

        }



        #endregion //几何辅助方法


        #endregion //得到实例几何信息及参照相关方法





        #region 实例变换相关方法

        /// <summary>
        /// 移动实例
        /// </summary>
        /// <param name="ele">Element 实例</param>
        /// <param name="MoveTran"> 移动方式，包含方向和距离 </param>
        public void MoveElement(Element ele, XYZ MoveTran)
        {
            Transaction tr = new Transaction(Doc);

            try
            {
                tr.Start("实例移动");

                ElementTransformUtils.MoveElement(Doc, ele.Id, MoveTran);

                tr.Commit();
            }
            catch
            {
                if (tr.HasStarted())
                {
                    tr.Commit();
                }
                MessageBox.Show("实例移动失败！");
            }
        }




        /// <summary>
        /// 原地旋转实例，三维视图中旋转轴为Z轴，其他视图中旋转轴为当前视图的 ViewDirection
        /// </summary>
        /// <param name="ins">需要旋转的 FamilyInstance 实例对象</param>
        /// <param name="RotateAngle"> 旋转角度（弧度值）</param>
        public void RotateInstance(FamilyInstance ins, double RotateAngle)
        {
            LocationPoint location = ins.Location as LocationPoint;
            XYZ LocP = location.Point;

            //定义旋转轴方向
            XYZ axisNormal = XYZ.BasisZ;

            //三维视图中默认沿着Z方向旋转，其他视图沿着当前视图方向旋转
            if (!(UIdoc.ActiveView is View3D))
            {
                axisNormal = UIdoc.ActiveView.ViewDirection;
            }

            Transaction tr = new Transaction(Doc);

            try
            {
                tr.Start("实例旋转");

                Line axsi = Line.CreateBound(LocP, LocP + axisNormal * 1);
                location.Rotate(axsi, RotateAngle);
                location.Point = LocP;

                tr.Commit();
            }
            catch
            {
                if (tr.HasStarted())
                {
                    tr.Commit();
                }
                MessageBox.Show("实例旋转失败！");
            }

        }


        /// <summary>
        /// 原地旋转实例，三维视图中旋转轴为Z轴，其他视图中旋转轴为当前视图的 ViewDirection
        /// </summary>
        /// <param name="ins">需要旋转的 FamilyInstance 实例对象</param>
        /// <param name="RotateAngle"> 旋转角度（弧度值）</param>
        public void RotateInstance(Element ele, double RotateAngle)
        {
            LocationPoint location = ele.Location as LocationPoint;
            XYZ LocP = location.Point;

            //定义旋转轴方向
            XYZ axisNormal = XYZ.BasisZ;

            //三维视图中默认沿着Z方向旋转，其他视图沿着当前视图方向旋转
            if (!(UIdoc.ActiveView is View3D))
            {
                axisNormal = UIdoc.ActiveView.ViewDirection;
            }

            Transaction tr = new Transaction(Doc);

            try
            {
                tr.Start("实例旋转");

                Line axsi = Line.CreateBound(LocP, LocP + axisNormal * 1);
                location.Rotate(axsi, RotateAngle);
                location.Point = LocP;

                tr.Commit();
            }
            catch
            {
                if (tr.HasStarted())
                {
                    tr.Commit();
                }
                MessageBox.Show("实例旋转失败！");
            }

        }



        /// <summary>
        /// 实例水平镜像（镜像面方向三维视图中默认为X方向，其他视图中为视图右方向）
        /// </summary>
        /// <param name="ins">需要镜像的 FamilyInstance 实例对象</param>
        /// <param name="DeleOriginal">是否删除镜像之前的对象</param>
        public void RightOriMirrorIns(FamilyInstance ins, bool DeleOriginal)
        {
            LocationPoint locPoint = ins.Location as LocationPoint;

            //定义镜像方向
            XYZ axisNormal = XYZ.BasisX;

            //三维视图中默认沿着X方向镜像，其他视图沿着当前视图右方向镜像
            if (!(UIdoc.ActiveView is View3D))
            {
                axisNormal = UIdoc.ActiveView.RightDirection;
            }

            //设置镜像面
            Plane MirrorPlan = Plane.CreateByNormalAndOrigin(axisNormal, locPoint.Point);


            Transaction tr = new Transaction(Doc);

            try
            {
                tr.Start("实例镜像");

                ElementTransformUtils.MirrorElement(UIdoc.Document, ins.Id, MirrorPlan);

                if (DeleOriginal == true)
                {
                    //删除原来的对象
                    UIdoc.Document.Delete(ins.Id);
                }

                tr.Commit();

            }
            catch
            {
                if (tr.HasStarted())
                {
                    tr.Commit();
                }
                MessageBox.Show("实例镜像失败！");
            }

        }


        /// <summary>
        /// 实例竖直镜像（镜像面方向三维视图中默认为Y方向，其他视图中为视图上方向）
        /// </summary>
        /// <param name="ins">需要镜像的 FamilyInstance 实例对象</param>
        /// <param name="DeleOriginal">是否删除镜像之前的对象</param>
        public void UpOriMirrorIns(FamilyInstance ins, bool DeleOriginal)
        {
            LocationPoint locPoint = ins.Location as LocationPoint;

            //定义镜像方向
            XYZ axisNormal = XYZ.BasisY;

            //三维视图中默认沿着Y方向镜像，其他视图沿着当前视图上方向镜像
            if (!(UIdoc.ActiveView is View3D))
            {
                axisNormal = UIdoc.ActiveView.UpDirection;
            }

            //设置镜像面
            Plane MirrorPlan = Plane.CreateByNormalAndOrigin(axisNormal, locPoint.Point);


            Transaction tr = new Transaction(Doc);

            try
            {
                tr.Start("实例镜像");

                ElementTransformUtils.MirrorElement(UIdoc.Document, ins.Id, MirrorPlan);

                if (DeleOriginal == true)
                {
                    //删除原来的对象
                    UIdoc.Document.Delete(ins.Id);
                }

                tr.Commit();

            }
            catch
            {
                if (tr.HasStarted())
                {
                    tr.Commit();
                }
                MessageBox.Show("实例镜像失败！");
            }

        }


        /// <summary>
        /// 实例自定义方向镜像
        /// </summary>
        /// <param name="ins">需要镜像的 FamilyInstance 实例对象</param>
        /// <param name="axisNormal">自定义镜像面的方向</param>
        /// <param name="DeleOriginal">是否删除镜像之前的对象</param> 
        public void AnyOriMirrorIns(FamilyInstance ins, XYZ axisNormal, bool DeleOriginal)
        {
            LocationPoint locPoint = ins.Location as LocationPoint;

            //设置镜像面
            Plane MirrorPlan = Plane.CreateByNormalAndOrigin(axisNormal, locPoint.Point);


            Transaction tr = new Transaction(Doc);

            try
            {
                tr.Start("实例镜像");

                ElementTransformUtils.MirrorElement(UIdoc.Document, ins.Id, MirrorPlan);

                if (DeleOriginal == true)
                {
                    //删除原来的对象
                    UIdoc.Document.Delete(ins.Id);
                }

                tr.Commit();

            }
            catch
            {
                if (tr.HasStarted())
                {
                    tr.Commit();
                }
                MessageBox.Show("实例镜像失败！");
            }

        }





        /// <summary>
        /// 多个实例线性阵列
        /// </summary>
        /// <param name="ElemIds">需要操作的实例Id集合</param>
        /// <param name="transXYZ">第一次阵列变化向量（具有长度和方向的向量）</param>
        /// <param name="CopyCount">阵列次数</param>
        public void CopyInsByLine(IList<ElementId> ElemIds, XYZ transXYZ, int CopyCount)
        {
            Transaction tr = new Transaction(Doc);

            try
            {
                tr.Start("实例阵列");

                for (int i = 0; i < CopyCount; i++)
                {
                    ElementTransformUtils.CopyElements(Doc, ElemIds, transXYZ * i);
                }

                tr.Commit();

            }
            catch
            {
                if (tr.HasStarted())
                {
                    tr.Commit();
                }
                MessageBox.Show("实例阵列失败！");
            }

        }


        /// <summary>
        /// 多个实例水平矩形阵列，已知阵列矩形总宽度、总高度、行数、列数
        /// </summary>
        /// <param name="ElemIds">需要操作的实例Id集合</param>
        /// <param name="Width">阵列矩形宽度（两端实例位置点水平方向距离）</param>
        /// <param name="Height">阵列矩形高度（两端实例位置点竖直方向距离）</param>
        /// <param name="RowNum">阵列行数</param>
        /// <param name="ColumnNum">阵列列数</param>
        public void CopyInsByRect(IList<ElementId> ElemIds, double Width, double Height, int RowNum, int ColumnNum)
        {
            Transaction tr = new Transaction(Doc);

            XYZ RowOri = XYZ.BasisX * Width / (ColumnNum - 1);
            XYZ ColumnOri = XYZ.BasisY * Height / (RowNum - 1);


            //三维视图中默认沿着X和Y方向，其他视图沿着当前视图右和上方向
            if (!(UIdoc.ActiveView is View3D))
            {
                RowOri = UIdoc.ActiveView.RightDirection * Width / (ColumnNum - 1);
                ColumnOri = UIdoc.ActiveView.UpDirection * Height / (RowNum - 1);
            }


            try
            {
                tr.Start("实例阵列");

                for (int i = 0; i < ColumnNum - 1; i++)
                {
                    ElementTransformUtils.CopyElements(Doc, ElemIds, RowOri * i);
                }

                for (int i = 0; i < RowNum - 1; i++)
                {
                    ElementTransformUtils.CopyElements(Doc, ElemIds, ColumnOri * i);
                }

                tr.Commit();

            }
            catch
            {
                if (tr.HasStarted())
                {
                    tr.Commit();
                }
                MessageBox.Show("实例阵列失败！");
            }

        }


        /// <summary>
        /// 多个实例水平矩形阵列，已知阵列行数、列数、行间距、列间距
        /// </summary>
        /// <param name="ElemIds">需要操作的实例Id集合</param>
        /// <param name="RowNum">阵列行数</param>
        /// <param name="ColumnNum">阵列列数</param>
        /// <param name="RowDis">阵列行间距</param>
        /// <param name="ColumnDis">阵列列间距</param>
        public void CopyInsByRect(IList<ElementId> ElemIds, int RowNum, int ColumnNum, double RowDis, double ColumnDis)
        {
            Transaction tr = new Transaction(Doc);

            XYZ RowOri = XYZ.BasisX * ColumnDis;
            XYZ ColumnOri = XYZ.BasisY * RowDis;


            //三维视图中默认沿着X和Y方向，其他视图沿着当前视图右和上方向
            if (!(UIdoc.ActiveView is View3D))
            {
                RowOri = UIdoc.ActiveView.RightDirection * ColumnDis;
                ColumnOri = UIdoc.ActiveView.UpDirection * RowDis;
            }


            try
            {
                tr.Start("实例阵列");

                for (int i = 0; i < ColumnNum - 1; i++)
                {
                    ElementTransformUtils.CopyElements(Doc, ElemIds, RowOri * i);
                }

                for (int i = 0; i < RowNum - 1; i++)
                {
                    ElementTransformUtils.CopyElements(Doc, ElemIds, ColumnOri * i);
                }

                tr.Commit();

            }
            catch
            {
                if (tr.HasStarted())
                {
                    tr.Commit();
                }
                MessageBox.Show("实例阵列失败！");
            }

        }


        /// <summary>
        /// 多个实例自定义方向矩形阵列，已知阵列矩形总宽度、总高度、行数、列数，以及阵列矩阵行方向
        /// </summary>
        /// <param name="ElemIds">需要操作的实例Id集合</param>
        /// <param name="Width">阵列矩形宽度（两端实例位置点水平方向距离）</param>
        /// <param name="Height">阵列矩形高度（两端实例位置点竖直方向距离）</param>
        /// <param name="RowNum">阵列行数</param>
        /// <param name="ColumnNum">阵列列数</param>
        /// <param name="RowOriNormal">阵列矩阵行方向</param>
        public void CopyInsByRect(IList<ElementId> ElemIds, double Width, double Height, int RowNum, int ColumnNum, XYZ RowOriNormal)
        {
            Transaction tr = new Transaction(Doc);

            MathsCalculation mc = new MathsCalculation();

            XYZ RowOri = RowOriNormal * Width / (ColumnNum - 1);
            XYZ ColumnOri = mc.GetNormalOnPlane(RowOriNormal, XYZ.BasisX, XYZ.BasisY, XYZ.BasisZ) * Height / (RowNum - 1);

            //三维视图中默认按照平面视图方向，其他视图沿着当前视图方向
            if (!(UIdoc.ActiveView is View3D))
            {
                ColumnOri = mc.GetNormalOnPlane(RowOriNormal, UIdoc.ActiveView.RightDirection, UIdoc.ActiveView.UpDirection, UIdoc.ActiveView.ViewDirection) * Height / (RowNum - 1);
            }

            try
            {
                tr.Start("实例阵列");

                for (int i = 0; i < ColumnNum - 1; i++)
                {
                    ElementTransformUtils.CopyElements(Doc, ElemIds, RowOri * i);
                }

                for (int i = 0; i < RowNum - 1; i++)
                {
                    ElementTransformUtils.CopyElements(Doc, ElemIds, ColumnOri * i);
                }

                tr.Commit();

            }
            catch
            {
                if (tr.HasStarted())
                {
                    tr.Commit();
                }
                MessageBox.Show("实例阵列失败！");
            }

        }


        /// <summary>
        /// 多个实例自定义方向矩形阵列，已知阵列行数、列数、行间距、列间距，以及阵列矩阵行方向
        /// </summary>
        /// <param name="ElemIds">需要操作的实例Id集合</param>
        /// <param name="RowNum">阵列行数</param>
        /// <param name="ColumnNum">阵列列数</param>
        /// <param name="RowDis">阵列行间距</param>
        /// <param name="ColumnDis">阵列列间距</param>
        /// <param name="RowOriNormal">阵列矩阵行方向</param>
        public void CopyInsByRect(IList<ElementId> ElemIds, int RowNum, int ColumnNum, double RowDis, double ColumnDis, XYZ RowOriNormal)
        {
            Transaction tr = new Transaction(Doc);

            MathsCalculation mc = new MathsCalculation();

            XYZ RowOri = RowOriNormal * ColumnDis;
            XYZ ColumnOri = mc.GetNormalOnPlane(RowOriNormal, XYZ.BasisX, XYZ.BasisY, XYZ.BasisZ) * RowDis;


            //三维视图中默认按照平面视图方向，其他视图沿着当前视图方向
            if (!(UIdoc.ActiveView is View3D))
            {
                ColumnOri = mc.GetNormalOnPlane(RowOriNormal, UIdoc.ActiveView.RightDirection, UIdoc.ActiveView.UpDirection, UIdoc.ActiveView.ViewDirection) * RowDis;
            }


            try
            {
                tr.Start("实例阵列");

                for (int i = 0; i < ColumnNum - 1; i++)
                {
                    ElementTransformUtils.CopyElements(Doc, ElemIds, RowOri * i);
                }

                for (int i = 0; i < RowNum - 1; i++)
                {
                    ElementTransformUtils.CopyElements(Doc, ElemIds, ColumnOri * i);
                }

                tr.Commit();

            }
            catch
            {
                if (tr.HasStarted())
                {
                    tr.Commit();
                }
                MessageBox.Show("实例阵列失败！");
            }

        }





        #endregion //实例变换相关方法






        /// <summary>
        /// 多选链接文档中的对象
        /// </summary>
        /// <param name="linkElementFilter">链接文档中的对象过滤器</param>
        /// <param name="desc">选取描述</param>
        /// <returns></returns>
        public IList<LinkElemObj> SelLinkElements(ISelectionFilter linkElementFilter, string desc)
        {
            IList<LinkElemObj> collection = new List<LinkElemObj>();

            //框选
            IList<Reference> sel_list = UIdoc.Selection.PickObjects(ObjectType.LinkedElement, linkElementFilter, desc);

            foreach (Reference refr in sel_list)
            {
                //首先转化为RevitLinkInstance 就是为了获取连接文件的Document，当然还可以通过其他方式获取比如过滤项目的所有文档,如果IsLinked==true就是链接文档，这个只针对单链接文档 如果是多链接文档则要增加一些筛选条件了
                RevitLinkInstance linkInstance = Doc.GetElement(refr) as RevitLinkInstance;

                if (linkInstance != null)
                {
                    Autodesk.Revit.DB.Document link_doc = linkInstance.GetLinkDocument();
                    Transform link_transform = linkInstance.GetTotalTransform();

                    //获得标准的族实例
                    Element ele = link_doc.GetElement(refr.LinkedElementId);

                    if (!collection.Select(o => o.Link_Elem.Id).Contains(ele.Id))
                    {
                        collection.Add(new LinkElemObj(ele, link_doc, link_transform));
                    }
                }
            }

            return collection;

        }





        #endregion //方法


        #region 相关枚举

        //方向点标识
        public enum PointLocKey
        {
            LeftTop,
            RightTop,
            LeftBottom,
            RightBottom,
            Centre,
        }


        #endregion //相关枚举

    }
}


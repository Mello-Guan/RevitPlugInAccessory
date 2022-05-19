using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.UI.Selection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RevitPlugInAccessory
{
    ///各种对象过滤器
    public class RvtFilters
    {



        /// <summary>
        /// 过滤选取楼板
        /// </summary>
        public class FloorFilter : ISelectionFilter
        {
            public bool AllowElement(Element elem)
            {
                return elem is Floor;
            }

            public bool AllowReference(Reference reference, XYZ position)
            {
                return false;
            }
        }

        /// <summary>
        /// 过滤选取楼板，排除重复选取
        /// </summary>
        /// <param name="elemId">需要除去的元素id</param>
        public class FloorDistinctFilter : ISelectionFilter
        {
            ElementId id = null;
            public FloorDistinctFilter(ElementId elemId)
            {
                id = elemId;
            }
            public bool AllowElement(Element elem)
            {
                if (elem is Floor && elem.Id != id)
                {
                    return true;
                }
                return false;
            }

            public bool AllowReference(Reference reference, XYZ position)
            {
                return false;
            }
        }






        /// <summary>
        /// 尺寸标注过滤器
        /// </summary>
        public class DimensionFilter : ISelectionFilter
        {
            public bool AllowElement(Element elem)
            {
                if (elem is Dimension)
                {
                    return true;
                }
                return false;
            }

            public bool AllowReference(Reference reference, XYZ position)
            {
                return false;
            }
        }

        /// <summary>
        /// 尺寸标注过滤器，排除重复选取
        /// </summary>
        /// <param name="elemId">需要除去的元素id</param>
        public class DimensionDistinctFilter : ISelectionFilter
        {
            ElementId id = null;
            public DimensionDistinctFilter(ElementId elemId)
            {
                id = elemId;
            }
            public bool AllowElement(Element elem)
            {
                if (elem is Dimension && elem.Id != id)
                {
                    return true;
                }
                return false;
            }

            public bool AllowReference(Reference reference, XYZ position)
            {
                return false;
            }
        }




        /// <summary>
        /// 线性尺寸标注过滤器
        /// </summary>
        public class LinearDimensionFilter : ISelectionFilter
        {
            public bool AllowElement(Element elem)
            {
                if (elem is Dimension)
                {
                    Dimension di = elem as Dimension;
                    if (di.DimensionType.StyleType == DimensionStyleType.Linear)
                    {
                        return true;
                    }
                }

                return false;
            }

            public bool AllowReference(Reference reference, XYZ position)
            {
                return false;
            }
        }


        /// <summary>
        /// 过滤选取两个引用的线性尺寸标注
        /// </summary>
        public class Linear2RefDimensionFilter : ISelectionFilter
        {
            public bool AllowElement(Element elem)
            {
                if (elem is Dimension)
                {
                    Dimension di = elem as Dimension;
                    if (di.DimensionType.StyleType == DimensionStyleType.Linear)
                    {
                        if (di.Segments.Size == 2)
                        {
                            return true;
                        }
                    }
                }

                return false;
            }

            public bool AllowReference(Reference reference, XYZ position)
            {
                return false;
            }
        }



        /// <summary>
        /// 多类别过滤器
        /// </summary>
        public class MultiCategoryFilter : ISelectionFilter
        {
            public bool AllowElement(Element elem)
            {
                if (!(elem is Grid || elem is Level || elem is DetailCurve || elem is IndependentTag || elem is Dimension || elem is DetailCurve) &&
                    !((elem.Category.Name == "视图") || (elem.Category.Name == "详图项目") || (elem.Category.Name == "填充图案") || (elem.Category.Name == "注释符号") || (elem.Category.Name == "轮廓")))

                {
                    return true;
                }

                return false;
            }

            public bool AllowReference(Reference reference, XYZ position)
            {
                return false;
            }
        }


        /// <summary>
        /// 过滤选取房间
        /// </summary>
        public class RoomFilter : ISelectionFilter
        {
            public bool AllowElement(Element elem)
            {
                return elem is Room;
            }

            public bool AllowReference(Reference reference, XYZ position)
            {
                return false;
            }
        }






        /// <summary>
        /// 过滤选取详图线
        /// </summary>
        public class DetailCurFilter : ISelectionFilter
        {
            public bool AllowElement(Element elem)
            {

                if (elem is DetailCurve)
                {
                    return true;
                }
                return false;
            }

            public bool AllowReference(Reference reference, XYZ position)
            {
                return false;
            }
        }






        /// <summary>
        /// 过滤选取标高
        /// </summary>
        public class LevelFilter : ISelectionFilter
        {
            public bool AllowElement(Element elem)
            {
                if (elem is Level)
                {
                    return true;
                }
                return false;
            }

            public bool AllowReference(Reference reference, XYZ position)
            {
                return false;
            }
        }








        /// <summary>
        /// 过滤选取某一方向的墙体
        /// </summary>
        /// <param name="ori">墙体平行方向</param>
        public class DirectedWallFilter : ISelectionFilter
        {
            //墙体方向
            XYZ Ori = XYZ.Zero;
            public DirectedWallFilter(XYZ ori)
            {
                Ori = ori;
            }

            public bool AllowElement(Element elem)
            {
                if (elem is Wall)
                {
                    Wall wall = elem as Wall;
                    XYZ wallOri = wall.Orientation;

                    if (wallOri.Normalize().IsAlmostEqualTo(Ori.Normalize(), 1e-6) || wallOri.Normalize().IsAlmostEqualTo(Ori.Normalize().Negate(), 1e-6))
                    {
                        return true;
                    }
                }
                return false;
            }

            public bool AllowReference(Reference reference, XYZ position)
            {
                return false;
            }
        }






        /// <summary>
        /// 过滤选取轴网
        /// </summary>
        public class GridFilter : ISelectionFilter
        {
            public bool AllowElement(Element elem)
            {
                if (elem is Grid)
                {
                    return true;
                }
                return false;
            }

            public bool AllowReference(Reference reference, XYZ position)
            {
                return false;
            }
        }

        /// <summary>
        /// 过滤选取某一方向的轴网
        /// </summary>
        /// <param name="ori">轴网平行方向</param>
        public class DirectedGridFilter : ISelectionFilter
        {
            //轴网方向
            XYZ Ori = XYZ.Zero;
            public DirectedGridFilter(XYZ ori)
            {
                Ori = ori;
            }

            public bool AllowElement(Element elem)
            {
                if (elem is Grid)
                {
                    Grid grid = elem as Grid;
                    XYZ gridOri = (grid.Curve.GetEndPoint(0) - grid.Curve.GetEndPoint(1)).Normalize();

                    if (gridOri.Normalize().IsAlmostEqualTo(Ori.Normalize(), 1e-6) || gridOri.Normalize().IsAlmostEqualTo(Ori.Normalize().Negate(), 1e-6))
                    {
                        return true;
                    }
                }
                return false;
            }

            public bool AllowReference(Reference reference, XYZ position)
            {
                return false;
            }
        }











        /// <summary>
        /// 过滤选取单个特定 CategoryName 的 Element 对象
        /// </summary>
        /// <param name="categoryname">元素 Category 全称</param>
        public class ElementFilter : ISelectionFilter
        {
            string CategoryName = null;

            public ElementFilter(string categoryname)
            {
                CategoryName = categoryname;
            }

            public bool AllowElement(Element elem)
            {
                if (elem.Category.Name == CategoryName)
                {
                    return true;
                }
                return false;
            }

            public bool AllowReference(Reference reference, XYZ position)
            {
                return false;
            }
        }


        /// <summary>
        /// 过滤选取多个特定 CategoryName 的 Element 对象
        /// </summary>
        /// <param name="categorynames">元素 Category 全称的集合</param>
        public class ElementsFilter : ISelectionFilter
        {
            List<string> CategoryNames = new List<string>();

            public ElementsFilter(List<string> categorynames)
            {
                CategoryNames = categorynames;
            }

            public bool AllowElement(Element elem)
            {
                if (CategoryNames.Contains(elem.Category.Name))
                {
                    return true;
                }
                return false;
            }

            public bool AllowReference(Reference reference, XYZ position)
            {
                return false;
            }
        }


        /// <summary>
        /// 根据 BuiltInCategory 集合过滤选取 element 对象
        /// </summary>
        /// <param name="doc">项目文档</param>
        /// <param name="bicList">需要过滤对象的BuiltInCategory集合</param>

        public class ElementBICFilter : ISelectionFilter
        {
            Document Doc;
            List<BuiltInCategory> BICList = new List<BuiltInCategory>();

            public ElementBICFilter(Document doc, List<BuiltInCategory> bicList)
            {
                BICList = bicList;
                Doc = doc;
            }

            public bool AllowElement(Element elem)
            {
                List<ElementId> categoryIds = new List<ElementId>();
                categoryIds = BICList.Select(o => Category.GetCategory(Doc, o).Id).ToList();

                if (categoryIds.Contains(elem.Category.Id))
                {
                    return true;
                }
                return false;
            }

            public bool AllowReference(Reference reference, XYZ position)
            {
                return false;
            }
        }


        /// <summary>
        /// 过滤选取特定 CategoryName 的 FamilyInstance 对象
        /// </summary>
        /// <param name="categoryname">元素 Category 全称</param>
        public class FamInsFilter : ISelectionFilter
        {
            string CategoryName = null;

            public FamInsFilter(string categoryname)
            {
                CategoryName = categoryname;
            }

            public bool AllowElement(Element elem)
            {
                if (elem is FamilyInstance && elem.Category.Name == CategoryName)
                {
                    return true;
                }

                return false;
            }

            public bool AllowReference(Reference reference, XYZ position)
            {
                return false;
            }
        }


        /// <summary>
        /// 过滤选取特定 CategoryName 的 FamilyInstance 对象，排除重复选取
        /// </summary>
        /// <param name="categoryname">元素 Category 全称</param>
        /// <param name="elemId">需要除去的元素id</param>
        public class FamInsDistinctFilter : ISelectionFilter
        {
            ElementId id = null;
            string CategoryName = null;

            public FamInsDistinctFilter(string categoryname, ElementId elemId)
            {
                CategoryName = categoryname;
                id = elemId;
            }

            public bool AllowElement(Element elem)
            {
                if (elem is FamilyInstance && elem.Category.Name == CategoryName && elem.Id != id)
                {
                    return true;
                }

                return false;
            }

            public bool AllowReference(Reference reference, XYZ position)
            {
                return false;
            }
        }






        /// <summary>
        /// 过滤选取特定 CategoryName，且族名称带关键字判断的 FamilyInstance 对象
        /// </summary>
        /// <param name="categoryname">元素 Category 全称</param>
        /// <param name="famNameKeyWords">族名称关键字字符串数组集合（集合中的字符串数组之间是或存在，每个字符串数组内的字符串是与存在）</param>
        public class FamInsKeyWordFilter : ISelectionFilter
        {
            string CategoryName = null;
            IList<string[]> FamNameKeyWords = new List<string[]>();


            public FamInsKeyWordFilter(string categoryname, IList<string[]> famNameKeyWords)
            {
                CategoryName = categoryname;
                FamNameKeyWords = famNameKeyWords;
            }

            public bool AllowElement(Element elem)
            {
                if (elem is FamilyInstance && elem.Category.Name == CategoryName)
                {
                    FamilyInstance ins = elem as FamilyInstance;

                    foreach (string[] strArray in FamNameKeyWords)
                    {
                        bool elemOk = true;
                        for (int i = 0; i < strArray.Count(); i++)
                        {
                            if (!ins.Symbol.FamilyName.Contains(strArray[i]))
                            {
                                elemOk = false;
                            }
                        }
                        if (elemOk == true)
                        {
                            return true;
                        }
                    }
                }

                return false;
            }

            public bool AllowReference(Reference reference, XYZ position)
            {
                return false;
            }
        }






        /// <summary>
        /// 过滤选取特定 CategoryName，且族名称带关键字判断的 FamilyInstance 对象，且具有一定方向的基于线对象
        /// </summary>
        /// <param name="categoryname">元素 Category 全称</param>
        /// <param name="famNameKeyWords">族名称关键字字符串数组集合（集合中的字符串数组之间是或存在，每个字符串数组内的字符串是与存在）</param>
        /// <param name="ori">基于线对象判定方向</param>
        public class FamInsOriKeyWordFilter : ISelectionFilter
        {
            string CategoryName = null;
            IList<string[]> FamNameKeyWords = new List<string[]>();
            XYZ Ori = XYZ.Zero;


            public FamInsOriKeyWordFilter(string categoryname, IList<string[]> famNameKeyWords, XYZ ori)
            {
                CategoryName = categoryname;
                FamNameKeyWords = famNameKeyWords;
                Ori = ori;
            }

            public bool AllowElement(Element elem)
            {
                if (elem is FamilyInstance && elem.Category.Name == CategoryName)
                {
                    FamilyInstance ins = elem as FamilyInstance;

                    if (ins.Location is LocationCurve)
                    {
                        Curve cur = (ins.Location as LocationCurve).Curve;
                        if (cur is Line)
                        {
                            Line line = cur as Line;
                            if (line.Direction.IsAlmostEqualTo(Ori, 1e-6) || line.Direction.IsAlmostEqualTo(Ori.Negate(), 1e-6))
                            {
                                foreach (string[] strArray in FamNameKeyWords)
                                {
                                    bool elemOk = true;
                                    for (int i = 0; i < strArray.Count(); i++)
                                    {
                                        if (!ins.Symbol.FamilyName.Contains(strArray[i]))
                                        {
                                            elemOk = false;
                                        }
                                    }
                                    if (elemOk == true)
                                    {
                                        return true;
                                    }
                                }
                            }
                        }
                    }
                }

                return false;
            }

            public bool AllowReference(Reference reference, XYZ position)
            {
                return false;
            }
        }






        /// <summary>
        /// 过滤选取特定 CategoryName，且族名称带关键字判断的 FamilyInstance 对象，排除重复选取
        /// </summary>
        /// <param name="categoryname">元素 Category 全称</param>
        /// <param name="famNameKeyWords">族名称关键字字符串数组集合（集合中的字符串数组之间是或存在，每个字符串数组内的字符串是与存在）</param>
        /// <param name="elemId">需要除去的元素id</param> 
        public class FamInsKeyWordDistinctFilter : ISelectionFilter
        {
            ElementId id = null;
            string CategoryName = null;
            IList<string[]> FamNameKeyWords = new List<string[]>();


            public FamInsKeyWordDistinctFilter(string categoryname, IList<string[]> famNameKeyWords, ElementId elemId)
            {
                CategoryName = categoryname;
                FamNameKeyWords = famNameKeyWords;
                id = elemId;
            }

            public bool AllowElement(Element elem)
            {
                if (elem is FamilyInstance && elem.Category.Name == CategoryName && elem.Id != id)
                {
                    FamilyInstance ins = elem as FamilyInstance;

                    foreach (string[] strArray in FamNameKeyWords)
                    {
                        bool elemOk = true;
                        for (int i = 0; i < strArray.Count(); i++)
                        {
                            if (!ins.Symbol.FamilyName.Contains(strArray[i]))
                            {
                                elemOk = false;
                            }
                        }
                        if (elemOk == true)
                        {
                            return true;
                        }
                    }
                }

                return false;
            }

            public bool AllowReference(Reference reference, XYZ position)
            {
                return false;
            }
        }










        /// <summary>
        /// 过滤选取链接文件中，特定 CategoryName 的 FamilyInstance 对象
        /// </summary>
        /// <param name="categoryname">元素 Category 全称</param>
        public class LinkFamInsFiler : ISelectionFilter
        {
            string CategoryName = null;

            public LinkFamInsFiler(string categoryname)
            {
                CategoryName = categoryname;
            }

            public RevitLinkInstance ins = null;

            public bool AllowElement(Element elem)
            {
                ins = elem as RevitLinkInstance;
                if (ins != null)
                {
                    return true;
                }
                return false;
            }

            public bool AllowReference(Reference reference, XYZ position)
            {
                if (ins == null)
                {
                    return false;
                }

                else
                {
                    Document linkdocument = ins.GetLinkDocument();
                    Element Elem = linkdocument.GetElement(reference.LinkedElementId);

                    if (Elem != null && Elem.Category.Name == CategoryName)
                    {
                        FamilyInstance instance = Elem as FamilyInstance;

                        return true;
                    }
                    return false;
                }
            }
        }






        /// <summary>
        /// 过滤选取链接文件中的常规模型洞口族和洞口符号族实例
        /// </summary>
        /// <param name="famNameContainsList">可以选择的洞口实例对象的族名称可能包含的字符串集合</param>
        /// <param name="fsnNameContainsList">可以选择的洞口符号对象的族类型名称可能包含的字符串集合</param>
        public class LinkHoleFiler : ISelectionFilter
        {
            //初始化可以选择的洞口实例对象的族名称可能包含的字符串集合
            IList<string> FamNameContainsList = new List<string>();

            //初始化可以选择的洞口符号对象的族类型名称可能包含的字符串集合
            IList<string> FsnNameContainsList = new List<string>();

            public LinkHoleFiler(IList<string> famNameContainsList, IList<string> fsnNameContainsList)
            {
                FamNameContainsList = famNameContainsList.ToList();
                FsnNameContainsList = fsnNameContainsList.ToList();
            }

            public RevitLinkInstance ins = null;

            public bool AllowElement(Element elem)
            {
                ins = elem as RevitLinkInstance;
                if (ins != null)
                {
                    return true;
                }
                return false;
            }

            public bool AllowReference(Reference reference, XYZ position)
            {
                if (ins == null)
                {
                    return false;
                }

                else
                {
                    Document linkdocument = ins.GetLinkDocument();
                    Element Elem = linkdocument.GetElement(reference.LinkedElementId);

                    if (Elem != null && Elem.Category.Name == "常规模型")
                    {
                        FamilyInstance instance = Elem as FamilyInstance;

                        if (FamNameContainsList.Any(o => instance.Symbol.Family.Name.Contains(o)))
                        {
                            return true;
                        }
                    }
                    else if (Elem != null && Elem.Category.Name == "详图项目")
                    {
                        FamilyInstance instance = Elem as FamilyInstance;

                        if (FsnNameContainsList.Any(o => instance.Symbol.Name.ToString().Contains(o)))
                        {
                            return true;
                        }
                    }
                    return false;
                }
            }
        }



    }



}

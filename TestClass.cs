using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static RevitPlugInAccessory.AccessoryFunction;

namespace RevitPlugInAccessory
{
    [Transaction(TransactionMode.Manual)]
    class TestClass : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIDocument uidoc = commandData.Application.ActiveUIDocument;
            Document doc = uidoc.Document;

            AccessoryFunction accFcn = new AccessoryFunction(AccessoryFunction.RevitName.Revit2018, commandData.Application, uidoc, doc);
            RevDocInfo rvtDocInfo = new RevDocInfo(uidoc, doc);

            try
            {
                
            }
            catch (Exception ex)
            {

                MessageBox.Show(ex.ToString());
                return Result.Cancelled;
            }


            return Result.Succeeded;
        }
    }









    #region 过滤器

    //过滤选取结构支撑（屋面支撑，柱间支撑，系杆）
    public class StructuralSupportFiler : ISelectionFilter
    {
        bool sel_WM = true;
        bool sel_ZJ = true;
        bool sel_XG = true;
        public StructuralSupportFiler(bool Sel_WM, bool Sel_ZJ, bool Sel_XG)
        {
            sel_WM = Sel_WM;
            sel_ZJ = Sel_ZJ;
            sel_XG = Sel_XG;
        }

        public bool AllowElement(Element elem)
        {
            FamilyInstance ins = elem as FamilyInstance;

            if (elem.Category.Name == "结构框架" && ins.Symbol.FamilyName.ToString().Contains("屋面支撑") && sel_WM == true)
            {
                return true;
            }

            else if (elem.Category.Name == "结构框架" && ins.Symbol.FamilyName.ToString().Contains("柱间支撑") && sel_ZJ == true)
            {
                return true;
            }
            else
            {
                foreach (Parameter p in ins.Parameters)
                {
                    if (p.Definition.Name.ToString().Contains("构件名称"))
                    {
                        string n = p.AsString();
                        if (n.Contains("系杆") && sel_XG == true)
                        {
                            return true;
                        }
                        break;
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


    //过滤选取常规模型洞口族和洞口符号族实例
    public class GenericHoleFiler : ISelectionFilter
    {
        //初始化可以选择的洞口实例对象的族名称可能包含的字符串集合
        IList<string> FamNameContainsList = new List<string>();

        //初始化可以选择的洞口符号对象的族类型名称可能包含的字符串集合
        IList<string> FsnNameContainsList = new List<string>();

        public GenericHoleFiler(IList<string> famNameContainsList, IList<string> fsnNameContainsList)
        {
            FamNameContainsList = famNameContainsList.ToList();
            FsnNameContainsList = fsnNameContainsList.ToList();
        }

        public bool AllowElement(Element elem)
        {
            if (elem.Category.Name == "常规模型")
            {
                FamilyInstance instance = elem as FamilyInstance;

                if (FamNameContainsList.Any(o => instance.Symbol.Family.Name.Contains(o)))
                {
                    return true;
                }
            }
            else if (elem.Category.Name == "详图项目")
            {
                FamilyInstance instance = elem as FamilyInstance;

                if (FsnNameContainsList.Any(o => instance.Symbol.Name.ToString().Contains(o)))
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



    #endregion //过滤器

}

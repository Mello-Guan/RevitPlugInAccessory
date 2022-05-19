using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;

namespace RevitPlugInAccessory
{
    /// 族下载相关方法
    public class DownLoadFam
    {


        #region 方法

        /// <summary>
        /// 从本地路径载入单个族
        /// </summary>
        /// <param name="doc">revit文档</param>
        /// <param name="FN">族全称（不带.rfa）</param>
        /// <param name="FamilyFilePath">族所在的本地路径</param>
        /// <param name="success">返回是否载入成功</param>
        /// <param name="category">族 BuiltInCategory 类型</param>
        /// <returns></returns>
        public Family LoadGetFamily(Document doc, string FN, string FamilyFilePath, ref bool success, BuiltInCategory category) //文档，族全称，族路径，返回是否成功，族类别枚举
        {
            Family Fam = null;//初始化一个

            if (FN == null) { success = false; return Fam; }

            //族类型在视图中可见性判断
            if (doc.Settings.Categories.get_Item(category).get_Visible(doc.ActiveView) == false)
            {
                MessageBox.Show("族类型[" + doc.Settings.Categories.get_Item(category).Name + "]在当前视图中不可见。");
                success = false;
                return Fam;
            }

            //得到族类型集合
            IList<FamilySymbol> SymbolList = new FilteredElementCollector(doc).OfClass(typeof(FamilySymbol)).OfCategory(category).Cast<FamilySymbol>().ToList();


            //当族未载入时将其载入到项目中
            if (SymbolList.Where(o => o.Family.Name == FN).Count() == 0)
            {
                //TaskDialog.Show("bbbb", "当前不存在 " + FamilyName + "族");//测试
                Transaction lf = new Transaction(doc, "载入" + FN + "族");
                try
                {
                    lf.Start();

                    doc.LoadFamily(FamilyFilePath + FN + ".rfa");

                    lf.Commit();

                    IList<FamilySymbol> NewSymbolList = new FilteredElementCollector(doc).OfClass(typeof(FamilySymbol)).OfCategory(category).Cast<FamilySymbol>().ToList();

                    //得到新载入后的族
                    Fam = NewSymbolList.Where(o => o.Family.Name == FN).First().Family;

                }
                catch
                {
                    if (lf.HasStarted()) { lf.Commit(); }
                    MessageBox.Show("载入族[" + FN + "]出错！");
                    success = false;
                }
            }

            success = true;
            return Fam;
        }

        /// <summary>
        /// 从本地路径载入多个族
        /// </summary>
        /// <param name="doc">revit文档</param>
        /// <param name="DLFamilyList">族下载类集合</param>
        /// <param name="success">全部载入成功判定</param>
        /// <returns></returns>

        public List<Family> LoadGetFamilys(Document doc, List<DLFamily> DLFamilyList, ref bool success)
        {
            List<Family> FamilyList = new List<Family>();

            //载入族判定
            success = true;

            foreach (DLFamily dlf in DLFamilyList)
            {
                if (dlf.FN != null)
                {
                    bool succ = false;

                    Family fam = LoadGetFamily(doc, dlf.FN, dlf.FamilyFilePath, ref succ, dlf.BICategory);

                    if (succ == true) { FamilyList.Add(fam); }
                    else { success = false; break; }
                }
            }

            return FamilyList;
        }




        /// <summary>
        /// 删除单个本地缓存族文件
        /// </summary>
        /// <param name="FN">族全称（不带.rfa）</param>
        /// <param name="FamilyFilePath">族所在的本地路径</param>
        public void DeleteFamilyFile(string FN, string FamilyFilePath)
        {
            //删除族文件
            FileInfo file = new FileInfo(FamilyFilePath + FN + ".rfa");
            if (file.Exists)
            {
                file.Delete(); //删除单个文件
            }
        }



        /// <summary>
        /// 删除多个本地缓存族文件
        /// </summary>
        /// <param name="DLFamilyList">族下载类集合</param>
        public void DeleteFamilyFiles(List<DLFamily> DLFamilyList)
        {
            foreach (DLFamily dlf in DLFamilyList)
            {
                //删除族文件
                FileInfo file = new FileInfo(dlf.FamilyFilePath + dlf.FN + ".rfa");
                if (file.Exists)
                {
                    file.Delete(); //删除单个文件
                }
            }
        }



        #endregion //方法

    }
}


using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RevitPlugInAccessory
{
    public class DLFamily  //用于族的下载族类
    {
        //族名称关键字
        public string FnKey { get; set; }

        //族全称
        public string FN { get; set; }

        //族类型名称
        public string FSN { get; set; }

        //本地下载后的族路径
        public string FamilyFilePath { get; set; }

        //族类别
        public BuiltInCategory BICategory { get; set; }

        public DLFamily(string fnKey, string fN, string fSN, string familyFilePath, BuiltInCategory bICategory)
        {
            this.FnKey = fnKey;
            this.FN = fN;
            this.FSN = fSN;
            this.FamilyFilePath = familyFilePath;
            this.BICategory = bICategory;
        }

    }
}

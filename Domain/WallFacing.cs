using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RevitPlugInAccessory
{
    public class WallFacing
    {
        //序号（从外到内）
        public int Num { get; set; }

        //功能（涂膜层、面层1、面层2、保温层、空气层、衬底、结构）
        public MaterialFunctionAssignment Function { get; set; }

        //材质
        public string MaterialName { get; set; }

        //厚度
        public double LayerWidth { get; set; }



        //构造函数
        public WallFacing(int num, MaterialFunctionAssignment function, string materialName, double layerWidth)
        {
            this.Num = num;
            this.Function = function;
            this.MaterialName = materialName;
            this.LayerWidth = layerWidth;
        }

    }


}

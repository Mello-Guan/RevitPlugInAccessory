using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RevitPlugInAccessory
{
    ///自建Revit各种参数类
    public class RvtParameter
    {
        #region 属性

        //参数名称（Revit的族和族类型中显示的参数实际名称）
        public string Name { get; set; }

        //参数在自建窗体中显示的名称
        public string ShowName { get; set; }

        //参数所属分类（值为一个枚举值 ParaBelongs）
        public ParaBelongs Belongs { get; set; }

        //参数来源类型
        public ParaKind Kind { get; set; }

        //参数存储类型
        public StorageType StorType { get; set; }

        //参数实际值
        public string Value { get; set; }

        //转字符串后的参数值（AsValueString）
        public string StrValue { get; set; }

        //参数编码
        public string Code { get; set; }

        //参数是否只读
        public bool IsReadOnly { get; set; }

        //参数是否显示
        public bool IsShow { get; set; }

        #endregion //属性

        #region 属性相关枚举

        //参数所属分类可选枚举
        public enum ParaBelongs
        {
            TypePara, //类别参数
            LocPara, //位置参数
            GeomPara, //几何参数
            SpeciPara,//规格参数
            CalcuPara, //计算辅助参数
            NULL //未赋值
        }

        //参数来源类型可选枚举
        public enum ParaKind
        {
            FamInsPara, //族实例参数
            FamSymbolPara, //族类型参数


            WallPara, //墙实例参数
            FloorPara, //楼板实例参数
            RoofBasePara, //屋顶实例参数
            CeilingPara, //天花板实例参数
            OpeningPara, //洞口实例参数
            RailingPara, //栏杆扶手实例参数
            DimensionPara, //尺寸标注实例参数
            IndependentTagPara, //标记实例参数
            ViewSheetPara,
            ViewSchedulePara,

            SelfCreatPara, //自建参数
            NULL //未赋值
        }


        #endregion //属性相关枚举


        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="name">参数名称（Revit的族和族类型中显示的参数实际名称）</param>
        /// <param name="showName">参数在自建窗体中显示的名称</param>
        /// <param name="belongs">参数所属分类（值为一个枚举值 ParaBelongs）</param>
        /// <param name="kind">参数来源类型</param>
        /// <param name="storType">参数存储类型</param>
        /// <param name="value">参数实际值</param>
        /// <param name="strValue">转字符串后的参数值（AsValueString）</param>
        /// <param name="code">参数编码</param>
        /// <param name="isReadOnly">IsReadOnly</param>
        /// <param name="isShow">IsShow</param>
        public RvtParameter(string name, string showName, ParaBelongs belongs, ParaKind kind, StorageType storType, string value,
                            string strValue, string code, bool isReadOnly, bool isShow)
        {
            this.Name = name;
            this.ShowName = showName;
            this.Belongs = belongs;
            this.Kind = kind;
            this.StorType = storType;
            this.Value = value;
            this.StrValue = strValue;
            this.Code = code;
            this.IsReadOnly = isReadOnly;
            this.IsShow = IsShow;
        }


    }
}


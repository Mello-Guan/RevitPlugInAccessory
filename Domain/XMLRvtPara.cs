using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace RevitPlugInAccessory.Domain
{
    public class XMLRvtPara //从后台记录XML文档中获取的单一参数类
    {
        //参数名称（从示例或者族类型中获得的参数名称全称）
        public string Name { get; set; }

        //参数来源
        public RvtParaSource Source { get; set; }

        //参数在表格中显示的名称
        public string ShowName { get; set; }

        //参数在表格中的控件形式
        public RowKind ControlKind { get; set; }

        //下拉列表样式
        public ComboBoxStyle ComBoxStyle { get; set; }


        //参数在表格中选项列表
        public List<string> ValueItems { get; set; }


        //参数在表格中默认值
        public string ParaValue { get; set; }


        public XMLRvtPara(string name, RvtParaSource source, string showName, RowKind controlKind, string paraValue)
        {
            this.Name = name;
            this.Source = source;
            this.ShowName = showName;
            this.ControlKind = controlKind;
            this.ParaValue = paraValue;
        }

        public XMLRvtPara(string name, RvtParaSource source, string showName, RowKind controlKind, ComboBoxStyle comBoxStyle, List<string> valueItems, string paraValue)
        {
            this.Name = name;
            this.Source = source;
            this.ShowName = showName;
            this.ControlKind = controlKind;
            this.ComBoxStyle = comBoxStyle;
            this.ValueItems = valueItems;
            this.ParaValue = paraValue;
        }
    }


    //参数来源枚举
    public enum RvtParaSource
    {
        //从元素对象中获得参数
        Element,
        //从族类型中获得参数
        FamilySymbol,
        //未赋值
        NULL,
    }

}




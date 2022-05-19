using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace RevitPlugInAccessory
{
    public class DGVParaRow //DataGridView承载Rvt参数行类
    {
        //行名称（参数名）
        public string RowName { get; set; }

        //行操作控件类型
        public RowKind Kind { get; set; }

        //参数值
        public string Value { get; set; }

        public TextBox TB { get; set; }

        public ComboBox CbB { get; set; }

        public CheckBox CkB { get; set; }

        public Button But { get; set; }




        //普通行 CommonRow 构造函数
        public DGVParaRow(string rowName, RowKind kind, string value)
        {
            this.RowName = rowName;
            this.Kind = kind;
            this.Value = value;
        }

        //TextBox行 构造函数
        public DGVParaRow(string rowName, RowKind kind, ref TextBox tB)
        {
            this.RowName = rowName;
            this.Kind = kind;
            this.TB = tB;
        }

        //CombolBox行 构造函数
        public DGVParaRow(string rowName, RowKind kind, ref ComboBox cbB)
        {
            this.RowName = rowName;
            this.Kind = kind;
            this.CbB = cbB;
        }
        //CheckBox行 构造函数
        public DGVParaRow(string rowName, RowKind kind, ref CheckBox ckB)
        {
            this.RowName = rowName;
            this.Kind = kind;
            this.CkB = ckB;
        }

        //Button行 构造函数
        public DGVParaRow(string rowName, RowKind kind, ref Button but)
        {
            this.RowName = rowName;
            this.Kind = kind;
            this.But = but;
        }

    }


    /// <summary>
    /// 行操作控件类型枚举
    /// </summary>
    public enum RowKind
    {
        //普通行
        CommonRow,
        TextBox,
        ComboBox,
        CheckBox,
        Button,
        NULL,
    }
}

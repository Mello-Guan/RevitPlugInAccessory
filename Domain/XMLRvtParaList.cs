using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RevitPlugInAccessory.Domain
{
    public class XMLRvtParaList //从后台XML文档中获得的参数类集合（用于显示在窗体参数表格中）
    {

        //对应族名称
        public string FN { get; set; }

        //对应族类型名称
        public string FSN { get; set; }

        //参数类集合
        public List<XMLRvtPara> ParaList { get; set; }



        public XMLRvtParaList(string fN, string fSN, List<XMLRvtPara> paraList)
        {
            this.FN = fN;
            this.FSN = fSN;
            this.ParaList = paraList;
        }
    }
}

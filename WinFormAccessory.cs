

using Autodesk.Revit.UI;
using RevitPlugInAccessory.Domain;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;

namespace RevitPlugInAccessory
{
    public class WinFormAccessory
    {
        #region 属性  

        public string revitName { get; set; }


        #endregion //属性




        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="rn">Revit版本</param>
        public WinFormAccessory(RevitName rn)
        {
            if (rn == RevitName.Revit2016)
            {
                revitName = "Autodesk Revit 2016";
            }
            else if (rn == RevitName.Revit2018)
            {
                revitName = "Autodesk Revit 2018";
            }
            else if (rn == RevitName.Revit2020)
            {
                revitName = "Autodesk Revit 2020";
            }
        }

        /// <summary>
        /// 无参数构造函数
        /// </summary>
        public WinFormAccessory()
        {

        }




        #region 方法


        /// <summary>
        /// 设置弹窗初始方位
        /// </summary>
        /// <param name="uidoc">UIDocument</param>
        /// <param name="frmWidth">窗体宽度像素</param>
        /// <param name="frmHeight">窗体高度像素</param>
        /// <param name="locWay">窗体方位选择</param>
        /// <returns></returns>
        public System.Drawing.Point FormStartLoc(UIDocument uidoc, int frmWidth, int frmHeight, LocWay locWay)
        {
            System.Drawing.Point point = new System.Drawing.Point();
            try
            {
                Autodesk.Revit.DB.View view = uidoc.ActiveView;
                UIView uiview = null;
                IList<UIView> uiviews = uidoc.GetOpenUIViews();
                foreach (UIView uv in uiviews)
                {
                    if (uv.ViewId.Equals(view.Id))
                    {
                        uiview = uv;
                        break;
                    }
                }
                Autodesk.Revit.DB.Rectangle rect = rect = uiview.GetWindowRectangle();

                switch (locWay)
                {
                    case LocWay.TopMid:
                        {
                            point = new System.Drawing.Point((rect.Left + rect.Right - frmWidth) / 2, rect.Top);
                            break;
                        }
                    case LocWay.TopLeft:
                        {
                            point = new System.Drawing.Point(rect.Left, rect.Top);
                            break;
                        }
                    case LocWay.TopRight:
                        {
                            point = new System.Drawing.Point(rect.Right - frmWidth, rect.Top);
                            break;
                        }
                    case LocWay.BotMid:
                        {
                            point = new System.Drawing.Point((rect.Left + rect.Right - frmWidth) / 2, rect.Bottom - frmHeight);
                            break;
                        }
                    case LocWay.BotLeft:
                        {
                            point = new System.Drawing.Point(rect.Left, rect.Bottom);
                            break;
                        }
                    case LocWay.BotRight:
                        {
                            point = new System.Drawing.Point(rect.Right - frmWidth, rect.Bottom);
                            break;
                        }
                    default:
                        {
                            break;
                        }
                }
            }
            catch { }


            return point;
        }


        /// <summary>
        /// 自动调整ComboBox下拉列表宽度大小使列表内容可以显示完整
        /// </summary>
        /// <param name="cB">需要设置的ComboBox</param>
        public void AdjustComboBoxDropDownListWidth(System.Windows.Forms.ComboBox cB)
        {
            Graphics g = null;
            Font font = null;
            try
            {
                int width = cB.Width;
                g = cB.CreateGraphics();
                font = cB.Font;

                //检查是否显示滚动条,如果是，则获取它的宽度来调整下拉列表的大小。
                int vertScrollBarWidth = (cB.Items.Count > cB.MaxDropDownItems) ? SystemInformation.VerticalScrollBarWidth : 0;

                int newWidth;
                for (int i = 0; i < cB.Items.Count; i++)
                {
                    newWidth = (int)g.MeasureString(cB.Items[i].ToString(), font).Width + vertScrollBarWidth;
                    if (width < newWidth)
                    {
                        width = newWidth;
                    }
                }

                cB.DropDownWidth = width;
            }
            catch
            { }
            finally
            {
                if (g != null)
                    g.Dispose();
            }
        }


        /// <summary>
        /// 设置ComboBox列表及首选项,如果传入首选项为空或者不存在于列表中，则直接默认为列表第一项
        /// </summary>
        /// <param name="cB">需要操作的ComboBox</param>
        /// <param name="comboBoxStyle">下拉列表下拉样式</param>
        /// <param name="ItemList">需要添加的列表项集合，除去重复</param>
        /// <param name="defaultItem">默认首选项，为空或者列表不存在时，默认首选项为列表首相</param>
        public void ComboBoxAddList(System.Windows.Forms.ComboBox cB, ComboBoxStyle comboBoxStyle, List<string> ItemList, string defaultItem)
        {
            //设置下拉列表样式
            cB.DropDownStyle = comboBoxStyle;

            //清空列表项目
            cB.Items.Clear();
            cB.Text = null;

            if (ItemList.Count > 0)
            {
                foreach (string n in ItemList)
                {
                    if (!cB.Items.Contains(n))
                    {
                        cB.Items.Add(n);
                    }
                }

                //设置首选项             
                if (defaultItem != null && ItemList.Contains(defaultItem))
                {
                    cB.Text = defaultItem;
                }
                else if (ItemList.Contains("线性尺寸标注样式") && ItemList.Count() > 1)
                {
                    if (ItemList.Where(o => o.Contains("2.5")).Count() > 0)
                    {
                        cB.Text = ItemList.Where(o => o.Contains("2.5")).First();
                    }
                    else
                    {
                        cB.Text = ItemList.Where(o => !o.Contains("线性尺寸标注样式")).First();
                    }
                }
                else
                {
                    cB.Text = cB.Items[0].ToString();
                }
            }

            //调整combox列表宽度
            AdjustComboBoxDropDownListWidth(cB);
        }


        #region 控件数据限定

        /// <summary>
        /// 为textbox设置输入限制（整数)
        /// </summary>
        /// <param name="tB">需要加限制的textBox</param>
        /// <param name="defText">默认文本</param>
        /// <param name="canInputNegativeSign">是否可输入负号</param>
        public void TextBoxInputLimit_Int(System.Windows.Forms.TextBox tB, int defText, bool canInputNegativeSign)
        {
            tB.Text = defText.ToString();
            if (canInputNegativeSign == false)
            {
                tB.KeyPress += new KeyPressEventHandler(tBox_KeyPress1); //添加输入设定事件，只允许输入数字，退格键
            }
            else
            {
                tB.KeyPress += new KeyPressEventHandler(tBox_KeyPress2); //添加输入设定事件，只允许输入数字，退格键，负号
            }

        }

        /// <summary>
        /// 为textbox设置输入限制（小数点后1位)
        /// </summary>
        /// <param name="tB">需要加限制的textBox</param>
        /// <param name="defText">默认文本</param>
        /// <param name="canInputNegativeSign">是否可输入负号</param>
        public void TextBoxInputLimit_Double1(System.Windows.Forms.TextBox tB, double defText, bool canInputNegativeSign)
        {
            tB.Text = string.Format("{0:###0.0}", defText); //默认值精确到小数点后一位
            if (canInputNegativeSign == false)
            {
                tB.KeyPress += new KeyPressEventHandler(tBox_KeyPress3); //添加输入设定事件，只允许输入数字、退格键、小数点
            }
            else
            {
                tB.KeyPress += new KeyPressEventHandler(tBox_KeyPress4); //添加输入设定事件，只允许输入数字、退格键、负号和小数点
            }


            tB.Leave += new System.EventHandler(tBox_Leave_n1);       //控件不再是焦点的时候将控件内的数字精确到小数点后一位
        }


        /// <summary>
        /// 为textbox设置输入限制（小数点后2位)
        /// </summary>
        /// <param name="tB">需要加限制的textBox</param>
        /// <param name="defText">默认文本</param>
        /// <param name="canInputNegativeSign">是否可输入负号</param>
        public void TextBoxInputLimit_Double2(System.Windows.Forms.TextBox tB, double defText, bool canInputNegativeSign)
        {
            tB.Text = string.Format("{0:###0.00}", defText); //默认值精确到小数点后2位
            if (canInputNegativeSign == false)
            {
                tB.KeyPress += new KeyPressEventHandler(tBox_KeyPress3); //添加输入设定事件，只允许输入数字、退格键、小数点
            }
            else
            {
                tB.KeyPress += new KeyPressEventHandler(tBox_KeyPress4); //添加输入设定事件，只允许输入数字、退格键、负号和小数点
            }


            tB.Leave += new System.EventHandler(tBox_Leave_n2);       //控件不再是焦点的时候将控件内的数字精确到小数点后2位
        }


        #region 限定事件方法


        //textBox输入设定：只允许输入数字、退格键
        private void tBox_KeyPress1(object sender, System.Windows.Forms.KeyPressEventArgs e)
        {
            if (!((e.KeyChar >= 48 && e.KeyChar <= 57) || e.KeyChar == '\b'))
            {
                e.Handled = true;
            }
        }

        //textBox输入设定：只允许输入数字、退格键、负号
        private void tBox_KeyPress2(object sender, System.Windows.Forms.KeyPressEventArgs e)
        {
            if (!((e.KeyChar >= 48 && e.KeyChar <= 57) || e.KeyChar == '\b' || e.KeyChar == '-'))
            {
                e.Handled = true;
            }
        }

        //textBox输入设定：只允许输入数字、退格键、小数点
        private void tBox_KeyPress3(object sender, System.Windows.Forms.KeyPressEventArgs e)
        {
            if (!((e.KeyChar >= 48 && e.KeyChar <= 57) || e.KeyChar == '\b' || e.KeyChar == '.'))
            {
                e.Handled = true;
            }
        }

        //textBox输入设定：只允许输入数字、退格键、负号和小数点
        private void tBox_KeyPress4(object sender, System.Windows.Forms.KeyPressEventArgs e)
        {
            if (!((e.KeyChar >= 48 && e.KeyChar <= 57) || e.KeyChar == '\b' || e.KeyChar == '-' || e.KeyChar == '.'))
            {
                e.Handled = true;
            }
        }


        //控件不再是焦点的时候将控件内的数字精确到小数点后1位
        private void tBox_Leave_n1(object sender, EventArgs e)
        {
            System.Windows.Forms.TextBox tBox = sender as System.Windows.Forms.TextBox;
            try
            {
                double input = Convert.ToDouble(tBox.Text);//记录用户输入的值
                tBox.Text = string.Format("{0:###0.0}", input);//将输入的值转换为正确格式,精确到小数点后一位
            }
            catch
            {
                MessageBox.Show("请输入有效值!", "提示");

                tBox.Text = "null";

            }
        }

        //控件不再是焦点的时候将控件内的数字精确到小数点后2位
        private void tBox_Leave_n2(object sender, EventArgs e)
        {
            System.Windows.Forms.TextBox tBox = sender as System.Windows.Forms.TextBox;
            try
            {
                double input = Convert.ToDouble(tBox.Text);//记录用户输入的值
                tBox.Text = string.Format("{0:###0.00}", input);//将输入的值转换为正确格式,精确到小数点后2位
            }
            catch
            {
                MessageBox.Show("请输入有效值!", "提示");

                tBox.Text = "null";

            }
        }


        #endregion //限定事件方法

        #endregion //控件数据限定



        #region 将PickBox作为自定义Button设置选中效果相关方法


        /// <summary>
        /// 将PickBox作为自定义Button设置选中效果，更新PictureBox背景图片来改变选中状态
        /// </summary>
        /// <param name="pb">操作的PictureBox</param>
        /// <param name="isSel">当前状态是否为选中</param>
        /// <param name="selImage">选中状态背景图片</param>
        /// <param name="unselImage">非选中状态背景图片</param>
        public void PictureBoxChangeSel(PictureBox pb, ref bool isSel, Image selImage, Image unselImage)
        {
            pb.BackgroundImage.Dispose();

            if (isSel == true) //由选中变为不选中
            {
                pb.BackgroundImage = unselImage;
                isSel = false;
                return;
            }
            if (isSel == false) //由不选中变为选中
            {
                pb.BackgroundImage = selImage;
                isSel = true;
                return;
            }
        }



        /// <summary>
        /// 将PickBox作为自定义Button设置选中效果
        /// </summary>
        /// <param name="pb">操作的PictureBox</param>
        /// <param name="isSel">当前状态是否为选中</param>
        /// <param name="selImage">选中状态背景图片</param>
        /// <param name="unselImage">非选中状态背景图片</param>
        public void PictureBoxSetSel(PictureBox pb, ref bool isSel, Image selImage, Image unselImage)
        {
            pb.BackgroundImage.Dispose();

            if (isSel == true)
            {
                //设置选中样式
                pb.BackgroundImage = selImage;
                return;
            }
            if (isSel == false) //由不选中变为选中
            {
                //设置非选中样式
                pb.BackgroundImage = unselImage;
                return;
            }
        }



        #endregion //将PickBox作为自定义Button设置选中效果相关方法





        #region 参数列表窗体相关


        /// <summary>
        /// 参数表格样式初始化
        /// </summary>
        /// <param name="dgv">DataGridView参数列表</param>
        /// <param name="paraNameWidth">参数名称列宽度(为了适应不同分辨率，改为比例方式均分DGV宽度)</param>
        /// <param name="paraValueWidth">参数值列宽度(为了适应不同分辨率，改为比例方式均分DGV宽度)</param>
        public void SetDGV(DataGridView dgv, int paraNameWidth, int paraValueWidth)
        {

            dgv.BackgroundColor = System.Drawing.Color.White;
            dgv.BorderStyle = BorderStyle.FixedSingle;
            dgv.CellBorderStyle = DataGridViewCellBorderStyle.Single;
            dgv.ClipboardCopyMode = DataGridViewClipboardCopyMode.EnableAlwaysIncludeHeaderText;
            dgv.EnableHeadersVisualStyles = false;
            dgv.GridColor = System.Drawing.Color.LightGray;
            dgv.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgv.MultiSelect = false;
            dgv.ReadOnly = false;
            dgv.EditMode = System.Windows.Forms.DataGridViewEditMode.EditOnEnter;

            //列相关设置
            dgv.ColumnHeadersVisible = true;
            dgv.AllowUserToOrderColumns = false;
            dgv.AllowUserToResizeColumns = false;
            dgv.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single;
            dgv.ColumnHeadersHeight = 25;
            dgv.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;


            //行相关设置
            dgv.RowHeadersVisible = false;
            dgv.AllowUserToAddRows = false;
            dgv.AllowUserToResizeRows = false;
            dgv.RowHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single;

            //设置单元格样式
            DataGridViewCellStyle cellstyle = new DataGridViewCellStyle();
            cellstyle.SelectionBackColor = System.Drawing.Color.LightSkyBlue;
            cellstyle.SelectionForeColor = System.Drawing.Color.Black;
            dgv.DefaultCellStyle = cellstyle;

            //列标头样式
            DataGridViewCellStyle columnStyle = new DataGridViewCellStyle();
            columnStyle.SelectionBackColor = System.Drawing.Color.LightSkyBlue;
            columnStyle.SelectionForeColor = System.Drawing.Color.Black;
            columnStyle.BackColor = System.Drawing.Color.Gainsboro;
            columnStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dgv.ColumnHeadersDefaultCellStyle = columnStyle;

            //计算宽度比值
            float s1 = (float)paraNameWidth / (float)(paraNameWidth + paraValueWidth);
            float s2 = (float)paraValueWidth / (float)(paraNameWidth + paraValueWidth);
            paraNameWidth = (int)((dgv.Width - (dgv.Width / 80)) * s1);
            paraValueWidth = (int)((dgv.Width - (dgv.Width / 80)) * s2);

            //设置列
            dgv.ColumnCount = 2;
            dgv.Columns[0].Name = "参数名称";
            dgv.Columns[0].Width = paraNameWidth;
            dgv.Columns[0].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dgv.Columns[0].ReadOnly = true;
            dgv.Columns[1].Name = "参数值";
            dgv.Columns[1].Width = paraValueWidth;
            dgv.Columns[1].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;

        }



        /// <summary>
        /// 为参数表格添加参数数据行
        /// </summary>
        /// <param name="dgv">DataGridView参数列表</param>
        /// <param name="dgvRowList">数据行集合（承载Rvt参数行类）</param>
        public void DGVAddRow(DataGridView dgv, List<DGVParaRow> dgvRowList)
        {
            foreach (DGVParaRow dgvr in dgvRowList)
            {
                //添加新行           
                dgv.Rows.Add(new DataGridViewRow());
                dgv.Rows[dgv.RowCount - 1].Cells[0].Value = dgvr.RowName;

                switch (dgvr.Kind)
                {
                    case RowKind.CommonRow:
                        dgv.Rows[dgv.RowCount - 1].Cells[1].Value = dgvr.Value;
                        break;

                    case RowKind.TextBox:
                        Control tB = dgvr.TB as Control;
                        MakeControlInDGVCell(tB, dgv.Rows[dgv.RowCount - 1].Cells[1]);
                        dgvr.TB = tB as System.Windows.Forms.TextBox;
                        break;

                    case RowKind.ComboBox:
                        Control cbB = dgvr.CbB as Control;
                        MakeControlInDGVCell(cbB, dgv.Rows[dgv.RowCount - 1].Cells[1]);
                        dgvr.CbB = cbB as System.Windows.Forms.ComboBox;
                        break;

                    case RowKind.CheckBox:
                        Control ckB = dgvr.CkB as Control;
                        MakeControlInDGVCell(ckB, dgv.Rows[dgv.RowCount - 1].Cells[1]);
                        dgvr.CkB = ckB as CheckBox;
                        break;

                    case RowKind.Button:
                        MakeControlInDGVCell(dgvr.But, dgv.Rows[dgv.RowCount - 1].Cells[1]);
                        break;

                    default:
                        break;
                }
            }

            if (dgv.Rows.Count > 0)
            {
                dgv.Rows[0].Cells[0].Selected = false;
            }
        }



        /// <summary>
        /// DataGridView 间隔行颜色区分
        /// </summary>
        /// <param name="dgv">DataGridView</param>
        /// <param name="color1">单数行背景色</param>
        /// <param name="color2">双数行背景色</param>
        public void SetDataGridViewZebraLine(DataGridView dgv, Color color1, Color color2)
        {
            for (int i = 0; i < dgv.Rows.Count; i++)
            {
                if (i % 2 == 1)
                {
                    dgv.Rows[i].DefaultCellStyle.BackColor = color1;
                }
                else
                {
                    dgv.Rows[i].DefaultCellStyle.BackColor = color2;
                }
            }
        }





        /// <summary>
        /// 将自定义控件放置进DataGridView的单元格中
        /// </summary>
        /// <param name="c">自定义控件</param>
        /// <param name="cell">需要放置的DataGridView单元格</param>
        public void MakeControlInDGVCell(Control c, DataGridViewCell cell)
        {
            cell.DataGridView.Controls.Add(c);
            //得到控件所在单元格的矩形
            Rectangle rect = cell.DataGridView.GetCellDisplayRectangle(cell.ColumnIndex, cell.RowIndex, false);
            //将控件放置在单元格中
            c.Left = rect.Left;
            c.Top = rect.Top;
            c.Width = rect.Width;
            c.Height = rect.Height;
            c.Visible = true;
        }






        /// <summary>
        /// 从后台XML文档中得到需要显示在参数表格中的 族类型对应参数列表
        /// </summary>
        /// <param name="filePath">XML文档路径</param>
        /// <param name="fileName">XML文档名称</param>
        /// <param name="genNodeName">根节点名称</param>
        /// <returns></returns>

        public List<XMLRvtParaList> GetXMLRvtParaList(string filePath, string fileName, string genNodeName)
        {
            List<XMLRvtParaList> returnList = new List<XMLRvtParaList>();

            List<XmlNode> list = new List<XmlNode>();
            try
            {
                #region 从后台文档中得到 ParaLog 节点下的子集合 List<XmlNode> list

                XmlDocument xmldoc = new XmlDocument();
                xmldoc.Load(filePath + fileName + ".xml");

                XmlNode n = xmldoc.SelectSingleNode(genNodeName);

                if (n != null)
                {
                    foreach (XmlNode xn in n.ChildNodes)
                    {
                        if (xn.Name == "ParaLog")
                        {
                            foreach (XmlNode xxn in xn.ChildNodes)
                            {
                                if (xxn.Name == "ParaList")
                                {
                                    list.Add(xxn);
                                }
                            }
                        }
                    }
                }
                else
                {
                    MessageBox.Show("XML文档 [ " + fileName + ".xml ] 中的节点 [ " + genNodeName + " ] 不存在，导致节点读取失败！");
                }

                #endregion // 从后台文档中得到 ParaLog 节点下的子集合 List<XmlNode> list             

                #region 从 ParaList 节点中获取所需要的信息,并加入 returnList

                if (list.Count > 0)
                {
                    foreach (XmlNode xn in list)
                    {
                        string fN = null;
                        if (xn.Attributes.GetNamedItem("FN") != null) { fN = xn.Attributes.GetNamedItem("FN").Value; }
                        string fSN = null;
                        if (xn.Attributes.GetNamedItem("FSN") != null) { fSN = xn.Attributes.GetNamedItem("FSN").Value; }

                        List<XMLRvtPara> paraList = new List<XMLRvtPara>();

                        foreach (XmlNode xxn in xn.ChildNodes)
                        {
                            if (xxn.Name == "Para")
                            {
                                string name = null;
                                if (xxn.Attributes.GetNamedItem("Name") != null) { name = xxn.Attributes.GetNamedItem("Name").Value; }

                                RvtParaSource source = RvtParaSource.NULL;
                                string showName = null;
                                RowKind controlKind = RowKind.NULL;
                                ComboBoxStyle comBoxStyle = ComboBoxStyle.DropDownList;
                                List<string> valueItems = new List<string>();
                                string paraValue = null;

                                foreach (XmlNode xxxn in xxn.ChildNodes)
                                {
                                    switch (xxxn.Name)
                                    {
                                        case "Source":
                                            if (!String.IsNullOrWhiteSpace(xxxn.InnerText)) { source = (RvtParaSource)System.Enum.Parse(typeof(RvtParaSource), xxxn.InnerText); }
                                            break;

                                        case "ShowName":
                                            if (!String.IsNullOrWhiteSpace(xxxn.InnerText)) { showName = xxxn.InnerText; } else { showName = name; }
                                            break;

                                        case "ControlKind":
                                            if (!String.IsNullOrWhiteSpace(xxxn.InnerText)) { controlKind = (RowKind)System.Enum.Parse(typeof(RowKind), xxxn.InnerText); }
                                            break;

                                        case "ComBoxStyle":
                                            if (!String.IsNullOrWhiteSpace(xxxn.InnerText)) { comBoxStyle = (ComboBoxStyle)System.Enum.Parse(typeof(ComboBoxStyle), xxxn.InnerText); }
                                            break;

                                        case "ValueItems":
                                            if (!String.IsNullOrWhiteSpace(xxxn.InnerText))
                                            {
                                                string[] sa = xxxn.InnerText.Split('|');
                                                for (int i = 0; i < sa.Count(); i++) { valueItems.Add(sa[i]); }
                                            }
                                            break;

                                        case "ParaValue":
                                            if (!String.IsNullOrWhiteSpace(xxxn.InnerText)) { paraValue = xxxn.InnerText; }
                                            break;

                                        default:
                                            break;
                                    }
                                }

                                switch (controlKind)
                                {
                                    case RowKind.NULL:
                                        break;

                                    case RowKind.CommonRow:
                                        if (name != null && source != RvtParaSource.NULL && showName != null && controlKind != RowKind.NULL)
                                        { paraList.Add(new XMLRvtPara(name, source, showName, controlKind, paraValue)); }
                                        break;

                                    case RowKind.TextBox:
                                        if (name != null && source != RvtParaSource.NULL && showName != null && controlKind != RowKind.NULL)
                                        { paraList.Add(new XMLRvtPara(name, source, showName, controlKind, paraValue)); }
                                        break;

                                    case RowKind.ComboBox:
                                        if (name != null && source != RvtParaSource.NULL && showName != null && controlKind != RowKind.NULL && valueItems.Count > 0)
                                        { paraList.Add(new XMLRvtPara(name, source, showName, controlKind, comBoxStyle, valueItems, paraValue)); }
                                        break;

                                    case RowKind.CheckBox:
                                        if (name != null && source != RvtParaSource.NULL && showName != null && controlKind != RowKind.NULL)
                                        { paraList.Add(new XMLRvtPara(name, source, showName, controlKind, paraValue)); }
                                        break;

                                    case RowKind.Button:
                                        if (name != null && source != RvtParaSource.NULL && showName != null && controlKind != RowKind.NULL)
                                        { paraList.Add(new XMLRvtPara(name, source, showName, controlKind, paraValue)); }
                                        break;

                                    default:
                                        break;
                                }

                            }
                        }

                        if (fN != null && fSN != null)
                        { returnList.Add(new XMLRvtParaList(fN, fSN, paraList)); }
                    }
                }

                #endregion //从 ParaList 节点中获取所需要的信息,并加入 returnList
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
                MessageBox.Show("XML文档 [ " + fileName + ".xml ] 读取失败！");
            }

            return returnList;
        }


        /// <summary>
        /// 补全从后台XML文档中读取的参数列表中的族全称和族类型全称
        /// </summary>
        /// <param name="xrplList">从后台XML文档中读取的参数列表</param>
        /// <param name="Fn">补全的族名称</param>
        /// <param name="Fsn">补全的族类型名称</param>
        public void RefreshXMLRvtParaListFnFsn(ref List<XMLRvtParaList> xrplList, string Fn, string Fsn)
        {
            XMLRvtParaList xmlRPList = xrplList.Where(o => Fn.Contains(o.FN) && (String.IsNullOrWhiteSpace(o.FSN) || Fsn.Contains(o.FSN))).FirstOrDefault();
            if (xmlRPList != null)
            {
                xrplList.Where(o => o.FN == xmlRPList.FN && o.FSN == xmlRPList.FSN).First().FN = Fn;
                xrplList.Where(o => o.FN == xmlRPList.FN && o.FSN == xmlRPList.FSN).First().FSN = Fsn;
            }
        }


        /// <summary>
        /// 在初始化好的DataGridView中，显示当前所选族类型的参数列表，并返回相应的参数控件字典集合
        /// </summary>
        /// <param name="dgv">初始化好的DataGridView</param>
        /// <param name="xrpList">XMLRvtPara类集合</param>
        /// <returns></returns>
        public Dictionary<XMLRvtPara, System.Windows.Forms.Control> ShowParaListInDGV(DataGridView dgv, List<XMLRvtPara> xrpList)
        {
            Dictionary<XMLRvtPara, System.Windows.Forms.Control> dic = new Dictionary<XMLRvtPara, System.Windows.Forms.Control>();
            List<DGVParaRow> dgvRowList = new List<DGVParaRow>();

            foreach (XMLRvtPara xrp in xrpList)
            {
                switch (xrp.ControlKind)
                {
                    case RowKind.CommonRow:
                        dgvRowList.Add(new DGVParaRow(xrp.ShowName, xrp.ControlKind, xrp.ParaValue)); //添加普通行
                        break;

                    case RowKind.TextBox:
                        System.Windows.Forms.TextBox tB1 = new System.Windows.Forms.TextBox();
                        tB1.Text = xrp.ParaValue;
                        dgvRowList.Add(new DGVParaRow(xrp.ShowName, xrp.ControlKind, ref tB1));//添加TextBox行
                        dic.Add(xrp, tB1);//记录返回的控件
                        break;

                    case RowKind.ComboBox:
                        System.Windows.Forms.ComboBox cbB1 = new System.Windows.Forms.ComboBox();
                        ComboBoxAddList(cbB1, xrp.ComBoxStyle, xrp.ValueItems, xrp.ParaValue);
                        dgvRowList.Add(new DGVParaRow(xrp.ShowName, xrp.ControlKind, ref cbB1));//添加ComboBox行
                        dic.Add(xrp, cbB1);//记录返回的控件
                        break;

                    case RowKind.CheckBox:
                        CheckBox ckB = new CheckBox();
                        ckB.Checked = Boolean.Parse(xrp.ParaValue);
                        dgvRowList.Add(new DGVParaRow(xrp.ShowName, xrp.ControlKind, ref ckB));//添加CheckBox行
                        dic.Add(xrp, ckB);//记录返回的控件
                        break;

                    case RowKind.Button:
                        Button but = new Button();
                        but.Text = xrp.ParaValue;
                        dgvRowList.Add(new DGVParaRow(xrp.ShowName, xrp.ControlKind, ref but));//添加Button行
                        dic.Add(xrp, but);//记录返回的控件
                        break;

                    default:
                        break;
                }
            }

            DGVAddRow(dgv, dgvRowList);

            return dic;
        }






        /// <summary>
        /// 得到参数表格内的内容
        /// </summary>
        /// <param name="dgv">DataGridView参数列表</param>
        /// <returns></returns>
        public Dictionary<string, string> GetDGVInfoDic(DataGridView dgv)
        {
            Dictionary<string, string> dic = new Dictionary<string, string>();
            Control.ControlCollection cts = dgv.Controls;

            try
            {
                for (int i = 0; i < dgv.RowCount; i++)
                {
                    string rowName = dgv.Rows[i].Cells[0].Value.ToString();
                    string rowValue = null;

                    if (cts.Count > 0)
                    {
                        //得到控件所在单元格的矩形
                        Rectangle rect = dgv.GetCellDisplayRectangle(1, i, false);
                        foreach (Control c in cts)
                        {
                            if (c.Left >= rect.Left && c.Left < rect.Left + rect.Width && c.Top >= rect.Top && c.Top < rect.Top + rect.Height)
                            {
                                if (c is System.Windows.Forms.TextBox) { rowValue = (c as System.Windows.Forms.TextBox).Text.ToString(); }
                                else if (c is System.Windows.Forms.ComboBox) { rowValue = (c as System.Windows.Forms.ComboBox).Text.ToString(); }
                                else if (c is CheckBox) { rowValue = (c as CheckBox).Checked.ToString(); }
                                else { }
                                break;
                            }
                        }

                    }

                    if (rowValue == null)
                    {
                        if (dgv.Rows[i].Cells[1].Value != null)
                        {
                            rowValue = dgv.Rows[i].Cells[1].Value.ToString();
                        }
                    }
                    dic.Add(rowName, rowValue);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }

            return dic;
        }


        /// <summary>
        /// 获取表格信息，并重新赋值 List<XMLRvtParaList> xrplList,刷新参数对象赋值
        /// </summary>
        /// <param name="dgv">DataGridView</param>
        /// <param name="Fn">当前选中的族名称</param>
        /// <param name="Fsn">当前选中的族类型名称</param>
        /// <param name="xrplList">需要刷新的列表</param>
        public void GetDGVInfo(DataGridView dgv, string Fn, string Fsn, ref List<XMLRvtParaList> xrplList)
        {
            //获取表格内信息
            Dictionary<string, string> dic = GetDGVInfoDic(dgv);

            List<XMLRvtParaList> oldList = xrplList;
            foreach (XMLRvtPara xmlp in oldList.Where(o => o.FN.Contains(Fn) && o.FSN.Contains(Fsn)).First().ParaList)
            {
                if (dic.Keys.Contains(xmlp.ShowName))
                {
                    xrplList.Where(o => o.FN.Contains(Fn) && o.FSN.Contains(Fsn)).First()
                                      .ParaList.Where(o => o.ShowName == xmlp.ShowName).First().ParaValue = dic[xmlp.ShowName];
                }
            }
        }


        /// <summary>
        /// 重写后台XML文档中的ParaLog记录中的参数信息
        /// </summary>
        /// <param name="filePath">后台记录文档路径</param>
        /// <param name="fileName">后台记录文档名称</param>
        /// <param name="genNodeName">根节点名称</param>
        /// <param name="xrplList">传入的 XMLRvtParaList 集合</param>
        public void RewriteXMLRvtParaListLog(string filePath, string fileName, string genNodeName, List<XMLRvtParaList> xrplList)
        {
            try
            {
                XmlDocument xmldoc = new XmlDocument();
                xmldoc.Load(filePath + fileName + ".xml");

                XmlNode n = xmldoc.SelectSingleNode(genNodeName);

                #region  重写 ParaList 节点中的信息 

                if (n != null)
                {
                    foreach (XmlNode xn in n.ChildNodes)
                    {
                        if (xn.Name == "ParaLog")
                        {
                            foreach (XmlNode xxn in xn.ChildNodes)
                            {
                                if (xxn.Name == "ParaList")
                                {
                                    //获取节点对应的 XMLRvtParaList
                                    XMLRvtParaList xrpl = xrplList.Where(o => o.FN.Contains(xxn.Attributes.GetNamedItem("FN").Value) &&
                                                         (xxn.Attributes.GetNamedItem("FSN") == null || o.FSN.Contains(xxn.Attributes.GetNamedItem("FSN").Value))).FirstOrDefault();

                                    //更新ParaList属性
                                    xxn.Attributes.GetNamedItem("FN").Value = xrpl.FN;
                                    xxn.Attributes.GetNamedItem("FSN").Value = xrpl.FSN;

                                    foreach (XmlNode xxxn in xxn)
                                    {
                                        if (xxxn.Name == "Para")
                                        {
                                            XMLRvtPara xrp = xrpl.ParaList.Where(o => o.Name.Contains(xxxn.Attributes.GetNamedItem("Name").Value)).FirstOrDefault();

                                            if (xrp != null)
                                            {
                                                foreach (XmlNode xxxxn in xxxn.ChildNodes)
                                                {
                                                    if (xxxxn.Name == "ParaValue") { xxxxn.InnerText = xrp.ParaValue; }
                                                }
                                            }

                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                else
                {
                    MessageBox.Show("XML文档 [ " + fileName + ".xml ] 中的节点 [ " + genNodeName + " ] 不存在，导致节点读取失败！");
                }

                #endregion //重写 ParaList 节点中的信息 

                xmldoc.Save(filePath + fileName + ".xml");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
                MessageBox.Show("XML文档 [ " + fileName + ".xml ] 写入失败！");
            }
        }




        #endregion //参数列表窗体相关



        #endregion //方法






        /// <summary>
        /// Revit版本枚举
        /// </summary>
        public enum RevitName
        {
            Revit2016,
            Revit2018,
            Revit2020
        }

        /// <summary>
        /// 窗体位置枚举
        /// </summary>
        public enum LocWay
        {
            TopMid,
            TopLeft,
            TopRight,
            BotMid,
            BotLeft,
            BotRight
        }


    }
}

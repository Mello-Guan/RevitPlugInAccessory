using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using static RevitPlugInAccessory.AccessoryFunction;

namespace RevitPlugInAccessory
{
    /// 
    public class AETask
    {

        #region 引用AETask

        [DllImport("user32.dll", EntryPoint = "FindWindow")]
        internal static extern IntPtr FindWindow(string ClassName, string WindowNamw);

        [DllImport("user32.dll")]
        internal static extern int EnumWindows(CALLBACK call, int lParam);//遍历到revit软件的句柄
        internal delegate bool CALLBACK(int hwnd, int lparam);

        [DllImport("user32.dll")]
        internal static extern int SendMessage(IntPtr hWnd, int Msg, IntPtr wParam, int lParam);
        internal const int WM_GETTEXT = 0x000D;
        internal const int WM_SETTEXT = 0X000C;
        internal const int WM_CLICK = 0x00f5;

        [DllImport("user32.dll")]
        internal static extern int GetWindowText(int hWnd, StringBuilder lpText, int nCount);
        [DllImport("user32")]
        internal static extern bool EnumChildWindows(IntPtr window, EnumWindowProc callback, IntPtr lparam);
        [DllImport("user32.dll", EntryPoint = "FindWindowEx")]
        internal static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string lpszClass, string lpszWindow);
        internal delegate bool EnumWindowProc(IntPtr hWnd, IntPtr parameter);
        internal static IntPtr Revit;
        internal static IntPtr Edit;
        internal static IntPtr finish;

        //双击取消相关
        //SetCancel
        [DllImport("user32.dll")]
        internal static extern bool SetForegroundWindow(IntPtr hWnd);  //设置窗体获得焦点
        [DllImport("user32.dll")]
        internal static extern void keybd_event(byte bVk, byte bScan, int dwFlags, int dwExtraInfo);

        #endregion //引用AETask



        #region 属性  

        /// <summary>
        /// Revit版本名称
        /// </summary>

        public string revitName { get; set; }

        #endregion //属性




        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="rn">Revit版本</param>
        public AETask(RevitName rn)
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







        #region 方法


        /// <summary>
        /// 相当于双击ESC取消所有命令
        /// </summary>
        public void SetTwoCancel()
        {
            CALLBACK cb = BackCallHwnd;
            EnumWindows(cb, 0);
            if (Revit != IntPtr.Zero)
            {
                SetForegroundWindow(Revit);
                keybd_event(0x1B, 0, 0, 0);
                keybd_event(0x1B, 0, 2, 0);
                keybd_event(0x1B, 0, 0, 0);
                keybd_event(0x1B, 0, 2, 0);

            }

        }


        /// <summary>
        /// 相当于多选完成按钮
        /// </summary>
        public void SetOk()
        {
            CALLBACK cb = BackCallHwnd;
            EnumWindows(cb, 0);
            if (Revit != IntPtr.Zero)
            {
                FindChildClassHwnd(Revit, IntPtr.Zero);
                SendMessage(finish, WM_CLICK, IntPtr.Zero, 0);
            }
        }

        /// <summary>
        /// 相当于多选取消按钮
        /// </summary>
        public void SetCancel()
        {
            CALLBACK cb = BackCallHwnd;
            EnumWindows(cb, 0);
            if (Revit != IntPtr.Zero)
            {
                SetForegroundWindow(Revit);
                keybd_event(0x1B, 0, 0, 0);
                keybd_event(0x1B, 0, 2, 0);

            }
        }




        /// <summary>
        /// 相当于打开编辑类型对话框
        /// </summary>
        /// <returns></returns>
        public int SetSymbolEdit()
        {
            CALLBACK cb = BackCallHwnd;
            EnumWindows(cb, 0);
            if (Revit != IntPtr.Zero)
            {
                FindChildEditHwnd(Revit, IntPtr.Zero);
                SendMessage(Edit, WM_CLICK, IntPtr.Zero, 0);
            }
            return Edit.ToInt32();
        }


        #endregion //方法





        #region 辅助方法       

        internal bool BackCallHwnd(int hwnd, int lparam)//遍历桌面窗体得到Revit 获得句柄
        {

            StringBuilder sb = new StringBuilder(256);
            GetWindowText(hwnd, sb, sb.Capacity);
            if (sb.ToString().Contains(revitName))
            {
                Revit = FindWindow(null, sb.ToString());
                return false;
            }

            return true;
        }

        internal static bool FindChildClassHwnd(IntPtr hwndParent, IntPtr lParam)
        {
            EnumWindowProc childProc = new EnumWindowProc(FindChildClassHwnd);
            IntPtr hwnd = FindWindowEx(hwndParent, IntPtr.Zero, "Button", "完成");
            if (hwnd != IntPtr.Zero)
            {
                finish = hwnd;
                return false;
            }
            EnumChildWindows(hwndParent, childProc, IntPtr.Zero);
            return true;
        }

        internal static bool FindChildEditHwnd(IntPtr hwndParent, IntPtr lParam)
        {
            EnumWindowProc childProc = new EnumWindowProc(FindChildEditHwnd);
            IntPtr hwnd = FindWindowEx(hwndParent, IntPtr.Zero, "Button", "编辑类型");
            if (hwnd != IntPtr.Zero)
            {
                Edit = hwnd;
                return false;
            }
            EnumChildWindows(hwndParent, childProc, IntPtr.Zero);
            return true;
        }

        #endregion //辅助方法




    }
}

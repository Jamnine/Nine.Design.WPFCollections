using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using WinInterop = System.Windows.Interop;

namespace Nine.Design.Client
{
    public static class Win32Interactive
    {
        #region 最大化不遮挡任务栏
        public static System.IntPtr WindowProc(
              System.IntPtr hwnd,
              int msg,
              System.IntPtr wParam,
              System.IntPtr lParam,
              ref bool handled)
        {
            switch (msg)
            {
                case 0x0024:
                    WmGetMinMaxInfo(hwnd, lParam);
                    handled = true;
                    break;
            }

            return (System.IntPtr)0;
        }

        private static void WmGetMinMaxInfo(System.IntPtr hwnd, System.IntPtr lParam)
        {

            MINMAXINFO mmi = (MINMAXINFO)Marshal.PtrToStructure(lParam, typeof(MINMAXINFO));

            // Adjust the maximized size and position to fit the work area of the correct monitor
            int MONITOR_DEFAULTTONEAREST = 0x00000002;
            System.IntPtr monitor = MonitorFromWindow(hwnd, MONITOR_DEFAULTTONEAREST);

            if (monitor != System.IntPtr.Zero)
            {

                MONITORINFO monitorInfo = new MONITORINFO();
                GetMonitorInfo(monitor, monitorInfo);
                RECT rcWorkArea = monitorInfo.rcWork;
                RECT rcMonitorArea = monitorInfo.rcMonitor;
                mmi.ptMaxPosition.x = Math.Abs(rcWorkArea.left - rcMonitorArea.left);
                mmi.ptMaxPosition.y = Math.Abs(rcWorkArea.top - rcMonitorArea.top);
                mmi.ptMaxSize.x = Math.Abs(rcWorkArea.right - rcWorkArea.left);
                mmi.ptMaxSize.y = Math.Abs(rcWorkArea.bottom - rcWorkArea.top);
            }

            Marshal.StructureToPtr(mmi, lParam, true);
        }

        /// <summary>
        /// POINT aka POINTAPI
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            /// <summary>
            /// x coordinate of point.
            /// </summary>
            public int x;
            /// <summary>
            /// y coordinate of point.
            /// </summary>
            public int y;

            /// <summary>
            /// Construct a point of coordinates (x,y).
            /// </summary>
            public POINT(int x, int y)
            {
                this.x = x;
                this.y = y;
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MINMAXINFO
        {
            public POINT ptReserved;
            public POINT ptMaxSize;
            public POINT ptMaxPosition;
            public POINT ptMinTrackSize;
            public POINT ptMaxTrackSize;
        };

        /// <summary>
        /// </summary>
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public class MONITORINFO
        {
            /// <summary>
            /// </summary>            
            public int cbSize = Marshal.SizeOf(typeof(MONITORINFO));

            /// <summary>
            /// </summary>            
            public RECT rcMonitor = new RECT();

            /// <summary>
            /// </summary>            
            public RECT rcWork = new RECT();

            /// <summary>
            /// </summary>            
            public int dwFlags = 0;
        }

        /// <summary> Win32 </summary>
        [StructLayout(LayoutKind.Sequential, Pack = 0)]
        public struct RECT
        {
            /// <summary> Win32 </summary>
            public int left;
            /// <summary> Win32 </summary>
            public int top;
            /// <summary> Win32 </summary>
            public int right;
            /// <summary> Win32 </summary>
            public int bottom;

            /// <summary> Win32 </summary>
            public static readonly RECT Empty = new RECT();

            /// <summary> Win32 </summary>
            public int Width
            {
                get { return Math.Abs(right - left); }  // Abs needed for BIDI OS
            }
            /// <summary> Win32 </summary>
            public int Height
            {
                get { return bottom - top; }
            }

            /// <summary> Win32 </summary>
            public RECT(int left, int top, int right, int bottom)
            {
                this.left = left;
                this.top = top;
                this.right = right;
                this.bottom = bottom;
            }


            /// <summary> Win32 </summary>
            public RECT(RECT rcSrc)
            {
                this.left = rcSrc.left;
                this.top = rcSrc.top;
                this.right = rcSrc.right;
                this.bottom = rcSrc.bottom;
            }

            /// <summary> Win32 </summary>
            public bool IsEmpty
            {
                get
                {
                    // BUGBUG : On Bidi OS (hebrew arabic) left > right
                    return left >= right || top >= bottom;
                }
            }
            /// <summary> Return a user friendly representation of this struct </summary>
            public override string ToString()
            {
                if (this == RECT.Empty) { return "RECT {Empty}"; }
                return "RECT { left : " + left + " / top : " + top + " / right : " + right + " / bottom : " + bottom + " }";
            }

            /// <summary> Determine if 2 RECT are equal (deep compare) </summary>
            public override bool Equals(object obj)
            {
                if (!(obj is Rect)) { return false; }
                return (this == (RECT)obj);
            }

            /// <summary>Return the HashCode for this struct (not garanteed to be unique)</summary>
            public override int GetHashCode()
            {
                return left.GetHashCode() + top.GetHashCode() + right.GetHashCode() + bottom.GetHashCode();
            }


            /// <summary> Determine if 2 RECT are equal (deep compare)</summary>
            public static bool operator ==(RECT rect1, RECT rect2)
            {
                return (rect1.left == rect2.left && rect1.top == rect2.top && rect1.right == rect2.right && rect1.bottom == rect2.bottom);
            }

            /// <summary> Determine if 2 RECT are different(deep compare)</summary>
            public static bool operator !=(RECT rect1, RECT rect2)
            {
                return !(rect1 == rect2);
            }
        }

        [DllImport("user32")]
        internal static extern bool GetMonitorInfo(IntPtr hMonitor, MONITORINFO lpmi);

        /// <summary>
        /// 
        /// </summary>
        [DllImport("User32")]
        internal static extern IntPtr MonitorFromWindow(IntPtr handle, int flags);
        #endregion

        #region 隐藏任务栏图标
        [DllImport("user32.dll")]
        public static extern int SetWindowLong(IntPtr hWnd, int nindex, IntPtr dwNewLong);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern UInt32 GetWindowLong(IntPtr hWnd, int index);
        #endregion

        #region DPI转换
        [DllImport("User32.dll")] private static extern IntPtr GetDC(HandleRef hWnd);
        [DllImport("User32.dll")] private static extern int ReleaseDC(HandleRef hWnd, HandleRef hDC);
        [DllImport("GDI32.dll")] private static extern int GetDeviceCaps(HandleRef hDC, int nIndex);
        private static int _dpi = 0;
        public static int DPI
        {
            get
            {
                if (_dpi == 0)
                {
                    HandleRef desktopHwnd = new HandleRef(null, IntPtr.Zero);
                    HandleRef desktopDC = new HandleRef(null, GetDC(desktopHwnd));
                    _dpi = GetDeviceCaps(desktopDC, 88 /*LOGPIXELSX*/);
                    ReleaseDC(desktopHwnd, desktopDC);
                }
                return _dpi;
            }
        }

        public static double 转换为DPI参数(double pixels)
        {
            return (double)pixels * 96 / DPI;
        }

        public static double 转换为实际参数(double pixels)
        {
            return pixels * DPI / 96;
        }

        public static Point 转换为DPI坐标(Point pixels)
        {
            return new Point((double)pixels.X * 96 / DPI, (double)pixels.Y * 96 / DPI);
        }
        public static Point 转换为实际坐标(Point pixels)
        {

            return new Point((double)pixels.X * DPI / 96, (double)pixels.Y * DPI / 96);
        }

        public static System.Windows.Size 转换为DPI尺寸(System.Windows.Size pixels, double dpi = 0)
        {
            if (dpi == 0)
            {
                dpi = DPI;
            }
            return new System.Windows.Size((double)pixels.Width * 96 / dpi, (double)pixels.Height * 96 / dpi);
        }
        public static System.Windows.Size 转换为实际尺寸(System.Windows.Size pixels, double dpi = 0)
        {
            if (dpi == 0)
            {
                dpi = DPI;
            }
            return new System.Windows.Size((double)pixels.Width * dpi / 96, (double)pixels.Height * dpi / 96);
        }

        public static T? 实际参数转为DPI参数<T>(T 泛型参数)
        {
            if (泛型参数 is double)
            {
                double 实例参数 = (dynamic)泛型参数;
                实例参数 = 实例参数 * 96 / DPI;//计算
                T 返回结果 = (dynamic)实例参数; //转泛型            
                return 返回结果;
            }
            else if (泛型参数 is System.Windows.Point)
            {
                System.Windows.Point 实例参数 = (dynamic)泛型参数;//拆箱
                实例参数 = new System.Windows.Point(实例参数.X * 96 / DPI, 实例参数.Y * 96 / DPI);//计算
                T 返回结果 = (dynamic)实例参数; //转泛型        
                return 返回结果;
            }
            else if (泛型参数 is System.Windows.Size)
            {
                System.Windows.Size 实例参数 = (dynamic)泛型参数;//拆箱
                实例参数 = new System.Windows.Size(实例参数.Width * 96 / DPI, 实例参数.Height * 96 / DPI);//计算
                T 返回结果 = (dynamic)实例参数; //转泛型        
                return 返回结果;
            }
            else if (泛型参数 is Point)
            {
                Point 实例参数 = (dynamic)泛型参数;//拆箱
                实例参数 = new Point(实例参数.X * 96 / DPI, 实例参数.Y * 96 / DPI);//计算
                T 返回结果 = (dynamic)实例参数; //转泛型        
                return 返回结果;
            }
            else if (泛型参数 is Thickness)
            {
                Thickness 实例参数 = (dynamic)泛型参数;//拆箱
                实例参数 = new Thickness(实例参数.Left * 96 / DPI, 实例参数.Top * 96 / DPI, 实例参数.Right * 96 / DPI, 实例参数.Bottom * 96 / DPI);//计算
                T 返回结果 = (dynamic)实例参数; //转泛型        
                return 返回结果;
            }
            else
            {
                return default(T);
            }
        }
        public static T? DPI参数转为实际参数<T>(T 泛型参数)
        {
            if (泛型参数 is double)
            {
                T 返回结果 = 转换为实际参数((dynamic)泛型参数); //转泛型            
                return 返回结果;
            }
            else if (泛型参数 is System.Windows.Point)
            {
                System.Windows.Point 实例参数 = (dynamic)泛型参数;//拆箱
                实例参数 = new System.Windows.Point(转换为实际参数(实例参数.X), 转换为实际参数(实例参数.Y));//计算
                T 返回结果 = (dynamic)实例参数; //转泛型        
                return 返回结果;
            }
            else if (泛型参数 is System.Windows.Size)
            {
                System.Windows.Size 实例参数 = (dynamic)泛型参数;//拆箱
                实例参数 = new System.Windows.Size(转换为实际参数(实例参数.Width), 转换为实际参数(实例参数.Height));//计算
                T 返回结果 = (dynamic)实例参数; //转泛型        
                return 返回结果;
            }
            else if (泛型参数 is Point)
            {
                Point 实例参数 = (dynamic)泛型参数;//拆箱
                实例参数 = new Point(转换为实际参数(实例参数.X), 转换为实际参数(实例参数.Y));//计算
                T 返回结果 = (dynamic)实例参数; //转泛型        
                return 返回结果;
            }
            else if (泛型参数 is Thickness)
            {
                Thickness 实例参数 = (dynamic)泛型参数;//拆箱
                实例参数 = new Thickness(转换为实际参数(实例参数.Left), 转换为实际参数(实例参数.Top), 转换为实际参数(实例参数.Right), 转换为实际参数(实例参数.Bottom));//计算
                T 返回结果 = (dynamic)实例参数; //转泛型        
                return 返回结果;
            }
            else
            {
                return default(T);
            }
        }
        #endregion
    }
}

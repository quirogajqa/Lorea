// SPDX-License-Identifier: GPL-3.0-only
// Copyright (c) 2026 Lorea contributors
using System;
using System.Runtime.InteropServices;
class ShellThumbnailSmoke {
    [StructLayout(LayoutKind.Sequential)] struct Size { public int X,Y; }
    [ComImport,Guid("BCC18B79-BA16-442F-80C4-8A59C30C463B"),InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface IShellItemImageFactory {
        [PreserveSig] int GetImage(Size size,uint flags,out IntPtr bitmap);
    }
    [DllImport("shell32.dll",CharSet=CharSet.Unicode,PreserveSig=false)]
    static extern void SHCreateItemFromParsingName(string path,IntPtr context,ref Guid iid,[MarshalAs(UnmanagedType.Interface)] out IShellItemImageFactory item);
    [DllImport("gdi32.dll")] static extern bool DeleteObject(IntPtr bitmap);
    [STAThread] static int Main(string[] args) {
        foreach(string path in args) {
            IShellItemImageFactory item;var iid=typeof(IShellItemImageFactory).GUID;
            SHCreateItemFromParsingName(System.IO.Path.GetFullPath(path),IntPtr.Zero,ref iid,out item);
            IntPtr bitmap;int result=item.GetImage(new Size { X=128,Y=128 },8,out bitmap);
            Console.WriteLine(path+": Shell thumbnail HRESULT="+result.ToString("X8")+" bitmap="+bitmap);
            if(bitmap!=IntPtr.Zero) DeleteObject(bitmap);Marshal.ReleaseComObject(item);
            if(result!=0 || bitmap==IntPtr.Zero) return 1;
        }
        return 0;
    }
}

// SPDX-License-Identifier: GPL-3.0-only
// Copyright (c) 2026 Lorea contributors
using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using Piccasa;
class ThumbnailSmoke {
    [DllImport("shlwapi.dll",CharSet=CharSet.Unicode,PreserveSig=false)]
    static extern void SHCreateStreamOnFileEx(string path,uint mode,uint attributes,bool create,IntPtr reserved,out IStream stream);
    [DllImport("gdi32.dll")] static extern bool DeleteObject(IntPtr bitmap);
    [STAThread] static int Main(string[] args) {
        int failures=0;
        foreach(string argument in args) {
            bool reject=argument.StartsWith("reject:");string path=reject ? argument.Substring(7) : argument;
            IStream stream;SHCreateStreamOnFileEx(System.IO.Path.GetFullPath(path),0x20,0,false,IntPtr.Zero,out stream);
            var provider=new SvgThumbnails();IntPtr bitmap=IntPtr.Zero;uint alpha=0;
            try {
                int init=provider.Initialize(stream,0);
                int result=init==0 ? provider.GetThumbnail(128,out bitmap,out alpha) : init;
                bool passed=reject ? result!=0 && bitmap==IntPtr.Zero : result==0 && bitmap!=IntPtr.Zero && alpha==2;
                Console.WriteLine((passed ? "PASS " : "FAIL ")+path+" HRESULT="+result.ToString("X8"));
                if(!passed) failures++;
            } finally { if(bitmap!=IntPtr.Zero) DeleteObject(bitmap);Marshal.ReleaseComObject(stream); }
        }
        return failures;
    }
}

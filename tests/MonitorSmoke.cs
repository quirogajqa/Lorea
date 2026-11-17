// SPDX-License-Identifier: GPL-3.0-only
// Copyright (c) 2026 Lorea contributors
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;
class MonitorSmoke {
    [StructLayout(LayoutKind.Sequential)] struct Rect { public int Left,Top,Right,Bottom; }
    delegate bool Callback(IntPtr monitor,IntPtr dc,ref Rect rectangle,IntPtr data);
    [DllImport("user32.dll")] static extern bool EnumDisplayMonitors(IntPtr dc,IntPtr clip,Callback callback,IntPtr data);
    [DllImport("user32.dll")] static extern IntPtr MonitorFromWindow(IntPtr window,uint flags);
    [DllImport("user32.dll")] static extern bool SetWindowPos(IntPtr window,IntPtr after,int x,int y,int width,int height,uint flags);
    [STAThread] static int Main() {
        var monitors=new List<Tuple<IntPtr,Rect>>();
        EnumDisplayMonitors(IntPtr.Zero,IntPtr.Zero,delegate(IntPtr m,IntPtr dc,ref Rect r,IntPtr data) { monitors.Add(Tuple.Create(m,r));return true; },IntPtr.Zero);
        Console.WriteLine("Displays: "+monitors.Count);
        int result=1,stage=0;
        var app=new Application();
        var constructor=typeof(Piccasa.Viewer).GetConstructor(BindingFlags.Instance|BindingFlags.NonPublic,null,new[]{typeof(bool)},null);
        var viewer=(Window)constructor.Invoke(new object[]{true});
        var target=monitors[monitors.Count-1];
        var timer=new DispatcherTimer { Interval=TimeSpan.FromMilliseconds(400) };
        viewer.Loaded+=delegate { timer.Start(); };
        timer.Tick+=delegate {
            try {
                var current=app.MainWindow;
                if(stage==0) {
                    SetWindowPos(new WindowInteropHelper(current).Handle,IntPtr.Zero,target.Item2.Left+40,target.Item2.Top+40,500,400,0x14);
                    stage++;return;
                }
                if(stage==1) { current.WindowState=WindowState.Maximized;stage++;return; }
                IntPtr monitor=MonitorFromWindow(new WindowInteropHelper(current).Handle,2);
                if(monitor!=target.Item1) throw new Exception("Viewer moved to a different monitor at stage "+stage);
                if(stage==2) {
                    typeof(Piccasa.Viewer).GetMethod("EnterWindowedMode",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(current,null);
                    stage++;return;
                }
                Console.WriteLine("PASS: maximized and restored modes retained monitor "+target.Item1);
                result=0;timer.Stop();current.Close();
            } catch(Exception ex) { Console.WriteLine(ex);timer.Stop();app.Shutdown(); }
        };
        app.Run(viewer);return result;
    }
}

// SPDX-License-Identifier: GPL-3.0-only
// Copyright (c) 2026 Lorea contributors
using System;
using System.IO;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Xml;

[assembly: ComVisible(false)]
namespace Piccasa {
    [ComVisible(true), Guid("B824B49D-22AC-4161-AC8A-9916E8FA3F7F"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IInitializeWithStream {
        [PreserveSig] int Initialize(IStream stream,uint mode);
    }
    [ComVisible(true), Guid("E357FCCD-A995-4576-B01F-234630154E96"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IThumbnailProvider {
        [PreserveSig] int GetThumbnail(uint size,out IntPtr bitmap,out uint alphaType);
    }
    // Windows loads this COM component in its thumbnail surrogate; isolation is not disabled.
    [ComVisible(true), Guid("D7903169-9D1C-4CB8-9CA1-884652133E71"), ClassInterface(ClassInterfaceType.None)]
    public sealed class SvgThumbnails : IInitializeWithStream,IThumbnailProvider {
        byte[] contents;
        const int Fail=unchecked((int)0x80004005);
        public int Initialize(IStream stream,uint mode) {
            if(contents!=null) return unchecked((int)0x800704DF);
            if(stream==null) return unchecked((int)0x80070057);
            try {
                using(var data=new MemoryStream()) {
                    byte[] buffer=new byte[65536];IntPtr count=Marshal.AllocHGlobal(4);
                    try {
                        while(true) {
                            stream.Read(buffer,buffer.Length,count);int length=Marshal.ReadInt32(count);
                            if(length<=0) break;
                            if(data.Length+length>8*1024*1024) return Fail;
                            data.Write(buffer,0,length);
                        }
                    } finally { Marshal.FreeHGlobal(count); }
                    contents=data.ToArray();
                }
                return 0;
            } catch { return Fail; }
        }
        public int GetThumbnail(uint size,out IntPtr bitmap,out uint alphaType) {
            bitmap=IntPtr.Zero;alphaType=0;
            if(contents==null || size==0) return Fail;
            string directory=Path.Combine(Path.GetTempPath(),"Lorea-thumb-"+Guid.NewGuid().ToString("N"));
            string input=Path.Combine(directory,"input.svg"),normalized=Path.Combine(directory,"normalized.svg"),output=Path.Combine(directory,"thumb.png");
            try {
                Directory.CreateDirectory(directory);File.WriteAllBytes(input,contents);
                // No external resources, scripts or XML entities during background Explorer rendering.
                ValidateSvg(input);
                if(Viewer.NormalizeAndroidVector(input,normalized)) input=normalized;
                string root=Path.GetDirectoryName(typeof(SvgThumbnails).Assembly.Location);
                string codec=Path.Combine(root,"codecs","magick.exe");
                int edge=(int)Math.Min(size,1024);
                var info=new ProcessStartInfo(codec) {
                    Arguments="-limit memory 64MiB -limit map 128MiB -limit disk 128MiB -limit time 6 -background none -density 96 \"MSVG:"+input+"\" -thumbnail "+edge+"x"+edge+" -strip \"PNG:"+output+"\"",
                    UseShellExecute=false,CreateNoWindow=true,RedirectStandardError=true
                };
                info.EnvironmentVariables["MAGICK_CONFIGURE_PATH"]=Path.Combine(root,"thumbnail-policy");
                using(var process=Process.Start(info)) {
                    var errors=process.StandardError.ReadToEndAsync();
                    if(!process.WaitForExit(8000)) { process.Kill(); return Fail; }
                    if(process.ExitCode!=0 || !File.Exists(output)) return Fail;
                }
                BitmapSource frame;
                using(var stream=File.OpenRead(output)) frame=BitmapDecoder.Create(stream,BitmapCreateOptions.None,BitmapCacheOption.OnLoad).Frames[0];
                var converted=new FormatConvertedBitmap(frame,PixelFormats.Pbgra32,null,0);
                int stride=converted.PixelWidth*4;
                byte[] pixels=new byte[stride*converted.PixelHeight];converted.CopyPixels(pixels,stride,0);
                var header=new BitmapInfo { Size=40,Width=converted.PixelWidth,Height=-converted.PixelHeight,Planes=1,Bits=32,ImageSize=(uint)pixels.Length };
                IntPtr bits;bitmap=CreateDIBSection(IntPtr.Zero,ref header,0,out bits,IntPtr.Zero,0);
                if(bitmap==IntPtr.Zero) return Fail;
                Marshal.Copy(pixels,0,bits,pixels.Length);alphaType=2;return 0;
            } catch {
                if(bitmap!=IntPtr.Zero) DeleteObject(bitmap);bitmap=IntPtr.Zero;return Fail;
            } finally {
                foreach(string path in new[]{Path.Combine(directory,"input.svg"),normalized,output}) try { if(File.Exists(path)) File.Delete(path); } catch { }
                try { if(Directory.Exists(directory)) Directory.Delete(directory,false); } catch { }
            }
        }
        static void ValidateSvg(string path) {
            using(var reader=XmlReader.Create(path,new XmlReaderSettings { DtdProcessing=DtdProcessing.Ignore,XmlResolver=null,MaxCharactersInDocument=8*1024*1024 })) {
                while(reader.Read()) {
                    if(reader.NodeType!=XmlNodeType.Element) continue;
                    if(reader.LocalName=="script" || reader.LocalName=="foreignObject" || reader.LocalName=="image" || reader.LocalName=="use") throw new InvalidDataException();
                    if(reader.HasAttributes) {
                        while(reader.MoveToNextAttribute()) {
                            string value=reader.Value;
                            if(reader.LocalName=="href" || reader.LocalName.StartsWith("on",StringComparison.OrdinalIgnoreCase) || value.IndexOf("url(",StringComparison.OrdinalIgnoreCase)>=0) throw new InvalidDataException();
                        }
                        reader.MoveToElement();
                    }
                }
            }
        }
        [StructLayout(LayoutKind.Sequential)] struct BitmapInfo {
            public uint Size;public int Width,Height;public ushort Planes,Bits;
            public uint Compression,ImageSize;public int X,Y;public uint Used,Important;
        }
        [DllImport("gdi32.dll")] static extern IntPtr CreateDIBSection(IntPtr dc,ref BitmapInfo info,uint usage,out IntPtr bits,IntPtr section,uint offset);
        [DllImport("gdi32.dll")] static extern bool DeleteObject(IntPtr bitmap);
    }
}

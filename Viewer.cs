// SPDX-License-Identifier: GPL-3.0-only
// Copyright (c) 2026 Lorea contributors
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Xml;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using System.Windows.Markup;
using Microsoft.Win32;

namespace Piccasa
{
    public class Viewer : Window
    {
        readonly Image photo = new Image { Stretch = Stretch.Uniform, Margin = new Thickness(12,8,12,4), MinWidth = 0, MinHeight = 0 };
        readonly Image previousPhoto = new Image { Stretch=Stretch.Uniform, Margin=new Thickness(12,8,12,4), IsHitTestVisible=false, RenderTransformOrigin=new Point(.5,.5) };
        readonly DispatcherTimer slideshowTimer = new DispatcherTimer { Interval=TimeSpan.FromSeconds(3) };
        Button playButton;
        readonly DispatcherTimer gifTimer=new DispatcherTimer();
        System.Drawing.Image gifImage;
        MemoryStream gifStream;
        int gifFrame, gifFrameCount;
        int[] gifDelays;
        readonly TextBlock caption = new TextBlock { Foreground = Brushes.White, FontSize = 11, Padding = new Thickness(8,4,8,4), TextTrimming = TextTrimming.CharacterEllipsis, TextAlignment = TextAlignment.Center };
        readonly StackPanel thumbnails = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center };
        int thumbnailGeneration;
        readonly ScaleTransform scale = new ScaleTransform(1,1);
        readonly TranslateTransform offset = new TranslateTransform();
        double zoomTarget = 1;
        int originalPixelWidth;
        readonly TextBlock zoomLabel = new TextBlock { Foreground=Brushes.White, FontSize=16 };
        readonly Border zoomBadge = new Border { Background=new SolidColorBrush(Color.FromArgb(170,55,55,55)), CornerRadius=new CornerRadius(4), Padding=new Thickness(9,4,9,4), HorizontalAlignment=HorizontalAlignment.Center, VerticalAlignment=VerticalAlignment.Center, Visibility=Visibility.Collapsed, IsHitTestVisible=false };
        readonly DispatcherTimer zoomBadgeTimer = new DispatcherTimer { Interval=TimeSpan.FromMilliseconds(900) };
        string[] files = new string[0];
        int index;
        class PhotoView { public double Zoom, X, Y, Angle; }
        Dictionary<string, PhotoView> photoViews = new Dictionary<string, PhotoView>(StringComparer.OrdinalIgnoreCase);
        class CachedPhoto { public BitmapSource Bitmap; public int Width; public long Stamp, Used; public bool Invalid; }
        const long ImageCacheBudget=128L*1024*1024;
        bool prefetchBusy, detailBusy;
        readonly DispatcherTimer detailTimer=new DispatcherTimer { Interval=TimeSpan.FromMilliseconds(200) };
        Dictionary<string,CachedPhoto> imageCache = new Dictionary<string,CachedPhoto>(StringComparer.OrdinalIgnoreCase);
        Dictionary<string,BitmapImage> thumbnailCache = new Dictionary<string,BitmapImage>(StringComparer.OrdinalIgnoreCase);
        Point anchor;
        bool dragging;
        readonly bool windowed;
        readonly DispatcherTimer controlsTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
        static readonly string[] nativeExtensions = { ".jpg", ".jpeg", ".png", ".bmp", ".gif", ".tif", ".tiff" };
        static readonly string[] extensions = (".jpg .jpeg .jpe .jfif .png .bmp .dib .gif .tif .tiff .webp .heic .heif .avif .svg .svgz .raw .dng .cr2 .cr3 .crw .nef .nrw .arw .sr2 .srf .orf .rw2 .raf .pef .ptx .srw .3fr .fff .iiq .kdc .dcr .mrw .mos .mef .erf .x3f .rwl .psd .psb .ico .cur .tga .pcx .dds .exr .hdr .jp2 .j2k .jxl .jng .mng .pbm .pgm .ppm .pnm .pam .pfm .qoi .sgi .rgb .rgba .ras .xbm .xpm .xcf .fits .fit .fts .wbmp .apng").Split(' ').Where(x=>x.StartsWith(".")).ToArray();
        static readonly object conversionLock=new object();

        [STAThread]
        public static void Main(string[] args)
        {
            var app = new Application();
            Task.Run((Action)CleanConversionCache);
            var viewer = new Viewer();
            if (args.Length > 0) viewer.Loaded += delegate { viewer.LoadPath(args[0]); };
            app.Run(viewer);
        }

        public Viewer() : this(false) { }

        Viewer(bool windowed)
        {
            this.windowed = windowed;
            Title = "Lorea — Visor";
            using(var icon=typeof(Viewer).Assembly.GetManifestResourceStream("Lorea.Icon")) {
                if(icon!=null) {
                    var decoder=BitmapDecoder.Create(icon,BitmapCreateOptions.None,BitmapCacheOption.OnLoad);
                    Icon=decoder.Frames.OrderByDescending(frame=>frame.PixelWidth).First();
                }
            }
            WindowStyle = windowed ? WindowStyle.SingleBorderWindow : WindowStyle.None;
            ResizeMode = windowed ? ResizeMode.CanResize : ResizeMode.NoResize;
            AllowsTransparency = !windowed;
            Background = windowed ? new SolidColorBrush(Color.FromRgb(125,125,125)) : new SolidColorBrush(Color.FromArgb(155,0,0,0));
            if (windowed) {
                Width = Math.Min(760, SystemParameters.WorkArea.Width * .8);
                Height = Math.Min(720, SystemParameters.WorkArea.Height * .9);
                MinWidth = 420; MinHeight = 300;
                WindowStartupLocation = WindowStartupLocation.CenterScreen;
            } else WindowState = WindowState.Maximized;
            if (windowed) StateChanged += delegate {
                if (WindowState == WindowState.Maximized) SwitchViewerMode(false);
            };
            AllowDrop = true;
            var root = new Grid { ClipToBounds = true };
            Content = root;
            var transforms = new TransformGroup();
            transforms.Children.Add(scale);
            transforms.Children.Add(offset);
            photo.RenderTransform = transforms;
            photo.RenderTransformOrigin = new Point(.5,.5);
            var photoArea = new Grid { ClipToBounds = true, Background = Brushes.Transparent };
            photoArea.Children.Add(photo);
            photoArea.Children.Add(previousPhoto);
            photoArea.MouseLeftButtonDown += delegate(object sender, MouseButtonEventArgs e) {
                // Image occupies a layout rectangle larger than its Uniform-rendered bitmap.
                // Test the actual visible bitmap, including the current pan and zoom.
                if (photo.Source != null && photo.ActualWidth > 0 && photo.ActualHeight > 0) {
                    Point point = e.GetPosition(photo);
                    double fit = Math.Min(photo.ActualWidth / photo.Source.Width, photo.ActualHeight / photo.Source.Height);
                    double width = photo.Source.Width * fit;
                    double height = photo.Source.Height * fit;
                    var bounds = new Rect((photo.ActualWidth-width)/2, (photo.ActualHeight-height)/2, width, height);
                    if (bounds.Contains(point)) return;
                }
                EnterWindowedMode();
            };
            root.Children.Add(photoArea);
            var dock = new StackPanel { VerticalAlignment = VerticalAlignment.Bottom, Background = new SolidColorBrush(Color.FromArgb(215,24,24,24)) };
            root.Children.Add(dock);
            // Keep a transparent hit area when hidden so hovering restores the controls.
            var controlsArea = new Border { Background = Brushes.Transparent, VerticalAlignment = VerticalAlignment.Bottom };
            root.Children.Remove(dock);
            controlsArea.Child = dock;
            root.Children.Add(controlsArea);
            var closeButton = new Button {
                Content = ControlIcon("close"), Foreground = Brushes.White,
                Width = 34, Height = 34, Template = MakeButtonTemplate(17),
                Background = new SolidColorBrush(Color.FromArgb(150,35,35,35)),
                BorderThickness = new Thickness(0), Focusable = false, Cursor = Cursors.Hand,
                ToolTip = "Cerrar visor (Esc)"
            };
            var closeArea = new Border {
                Width = 44, Height = 44, Margin = new Thickness(0,4,4,0), HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Top, Background = Brushes.Transparent,
                Visibility = windowed ? Visibility.Collapsed : Visibility.Visible, Child = closeButton
            };
            closeButton.Click += delegate { Close(); };
            root.Children.Add(closeArea);
            zoomBadge.Child=zoomLabel;
            root.Children.Add(zoomBadge);
            zoomBadgeTimer.Tick += delegate { zoomBadgeTimer.Stop(); zoomBadge.Visibility=Visibility.Collapsed; };
            controlsTimer.Tick += delegate {
                controlsTimer.Stop();
                if (!controlsArea.IsMouseOver && !closeArea.IsMouseOver) {
                    dock.Opacity = 0;
                    dock.IsHitTestVisible = false;
                    closeButton.Opacity = 0;
                    closeButton.IsHitTestVisible = false;
                }
            };
            controlsArea.MouseEnter += delegate {
                controlsTimer.Stop();
                dock.Opacity = 1;
                dock.IsHitTestVisible = true;
                closeButton.Opacity = 1;
                closeButton.IsHitTestVisible = true;
            };
            controlsArea.MouseLeave += delegate {
                controlsTimer.Stop();
                controlsTimer.Start();
            };
            closeArea.MouseEnter += delegate {
                controlsTimer.Stop();
                dock.Opacity = closeButton.Opacity = 1;
                dock.IsHitTestVisible = closeButton.IsHitTestVisible = true;
            };
            closeArea.MouseLeave += delegate {
                controlsTimer.Stop(); controlsTimer.Start();
            };
            gifTimer.Tick += delegate { AdvanceGif(); };
            detailTimer.Tick += delegate { detailTimer.Stop(); LoadZoomDetail(); };
            Loaded += delegate { RestorePhotoView(); if(files.Length>0) { StartGif(files[index]); RefreshThumbnails(); } controlsTimer.Start(); CompositionTarget.Rendering += UpdateZoomIndicator; };
            Closed += delegate { detailTimer.Stop(); StopGif(); controlsTimer.Stop(); zoomBadgeTimer.Stop(); slideshowTimer.Stop(); CompositionTarget.Rendering -= UpdateZoomIndicator; ++thumbnailGeneration; };
            dock.Children.Add(caption);
            var strip = new ScrollViewer { Content = thumbnails, HorizontalScrollBarVisibility = ScrollBarVisibility.Hidden, VerticalScrollBarVisibility = ScrollBarVisibility.Disabled, Height = 30 };
            dock.Children.Add(strip);
            strip.PreviewMouseWheel += delegate(object s, MouseWheelEventArgs e) { strip.ScrollToHorizontalOffset(strip.HorizontalOffset-e.Delta); e.Handled=true; };
            var bar = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center };
            var toolbar = new Border { Child=bar, HorizontalAlignment=HorizontalAlignment.Center, CornerRadius=new CornerRadius(18), Padding=new Thickness(9,3,9,3), Margin=new Thickness(4,2,4,4), Background=new LinearGradientBrush(Color.FromRgb(65,65,65),Color.FromRgb(30,30,30),90), BorderBrush=new SolidColorBrush(Color.FromRgb(95,95,95)), BorderThickness=new Thickness(1) };
            dock.Children.Add(toolbar);
            var open=AddButton(bar,"Abrir",delegate { Open(); });
            open.ToolTip="Abrir archivo (Ctrl+O)"; open.Margin=new Thickness(0,0,10,0);
            AddButton(bar,"+", delegate { Zoom(1.2); }).ToolTip="Acercar";
            AddButton(bar,"−", delegate { Zoom(1/1.2); }).ToolTip="Alejar";
            AddButton(bar,"1:1", delegate { OriginalSize(); }).ToolTip="Tamaño original";
            var back=AddButton(bar,"◀", delegate { Navigate(-1); });
            back.Margin=new Thickness(14,0,2,0); back.ToolTip="Foto anterior";
            playButton=AddButton(bar,"▶", delegate { SetPlaying(!slideshowTimer.IsEnabled); });
            playButton.Width=36; playButton.Height=36;
            playButton.Content=PlayIcon(false);
            playButton.Background=new LinearGradientBrush(Color.FromRgb(245,245,245),Color.FromRgb(140,140,140),90);
            playButton.Foreground=new SolidColorBrush(Color.FromRgb(40,40,40));
            playButton.BorderBrush=Brushes.Silver;
            playButton.Template=MakeButtonTemplate(18);
            playButton.ToolTip="Reproducir / Pausar";
            slideshowTimer.Tick += delegate {
                if(files.Length<2) { SetPlaying(false); return; }
                try { Navigate(1,true); }
                catch(Exception ex) { SetPlaying(false); ShowError(ex); }
            };
            AddButton(bar,"▶", delegate { Navigate(1); }).ToolTip="Foto siguiente";
            var rotateLeft=AddButton(bar,"↶", delegate { RotatePhoto(-90); });
            rotateLeft.Margin=new Thickness(14,0,2,0); rotateLeft.ToolTip="Girar a la izquierda (solo vista)";
            AddButton(bar,"↷", delegate { RotatePhoto(90); }).ToolTip="Girar a la derecha (solo vista)";
            caption.Text = "Abre una foto o arrástrala aquí";
            KeyDown += OnKey;
            MouseWheel += delegate(object s, MouseWheelEventArgs e) { Zoom(e.Delta > 0 ? 1.2 : 1/1.2); };
            photo.MouseLeftButtonDown += delegate(object s, MouseButtonEventArgs e) {
                if (e.ClickCount == 2) { Reset(); return; }
                anchor = e.GetPosition(this); dragging = true; photo.CaptureMouse();
            };
            photo.MouseMove += delegate(object s, MouseEventArgs e) {
                if (!dragging) return;
                var next = e.GetPosition(this);
                offset.X += next.X-anchor.X; offset.Y += next.Y-anchor.Y; anchor=next;
            };
            photo.MouseLeftButtonUp += delegate { dragging=false; photo.ReleaseMouseCapture(); };
            Drop += delegate(object s, DragEventArgs e) {
                var paths = e.Data.GetData(DataFormats.FileDrop) as string[];
                if (paths != null && paths.Length > 0) LoadPath(paths[0]);
            };
        }

        void EnterWindowedMode()
        {
            if (windowed) return;
            SwitchViewerMode(true);
        }

        void SwitchViewerMode(bool useWindow)
        {
            SavePhotoView();
            dragging = false;
            photo.ReleaseMouseCapture();
            // WPF transparency cannot change after a window is shown.
            // Create a normal native window and retain the loaded image and folder.
            var next = new Viewer(useWindow);
            next.photoViews = photoViews;
            next.imageCache = imageCache;
            next.thumbnailCache = thumbnailCache;
            next.files = files;
            next.index = index;
            next.photo.Source = photo.Source;
            next.originalPixelWidth = originalPixelWidth;
            next.caption.Text = caption.Text;
            next.Title = Title;
            if (files.Length > 0) next.RefreshThumbnails();
            next.Show();
            next.SetPlaying(slideshowTimer.IsEnabled);
            Application.Current.MainWindow = next;
            Close();
        }

        Button AddButton(Panel panel, string text, Action action)
        {
            var button = new Button { Content=ControlIcon(text), FontFamily=new FontFamily("Segoe UI"), FontSize=11, MinWidth=28, Height=25, VerticalAlignment=VerticalAlignment.Center, Focusable=false, Cursor=Cursors.Hand, Margin=new Thickness(2,0,2,0), Padding=new Thickness(6,0,6,0), Background=new LinearGradientBrush(Color.FromRgb(64,64,64),Color.FromRgb(39,39,39),90), Foreground=Brushes.WhiteSmoke, BorderBrush=new SolidColorBrush(Color.FromRgb(105,105,105)), BorderThickness=new Thickness(1), Template=MakeButtonTemplate(8) };
            button.Click += delegate { action(); };
            panel.Children.Add(button);
            return button;
        }
        static object ControlIcon(string name)
        {
            string geometry=null;
            bool filled=false;
            switch(name) {
                case "close": geometry="M 3,3 L 13,13 M 13,3 L 3,13"; break;
                case "+": geometry="M 2,8 L 14,8 M 8,2 L 8,14"; break;
                case "−": geometry="M 2,8 L 14,8"; break;
                case "◀": geometry="M 12,2 L 3,8 L 12,14 Z"; filled=true; break;
                case "▶": geometry="M 4,2 L 13,8 L 4,14 Z"; filled=true; break;
                case "Ⅱ": geometry="M 4,2 L 6,2 L 6,14 L 4,14 Z M 10,2 L 12,2 L 12,14 L 10,14 Z"; filled=true; break;
                case "↶": geometry="M 4,5 A 6,6 0 1 1 3,11 M 4,1 L 4,5 L 0,5"; break;
                case "↷": geometry="M 12,5 A 6,6 0 1 0 13,11 M 12,1 L 12,5 L 16,5"; break;
                case "☆": case "★": geometry="M 8,1 L 10,5.5 L 15,6 L 11.3,9.5 L 12.3,15 L 8,12.3 L 3.7,15 L 4.7,9.5 L 1,6 L 6,5.5 Z"; filled=name=="★"; break;
            }
            if(geometry==null) return name;
            var path=new System.Windows.Shapes.Path { Data=Geometry.Parse(geometry), Width=14, Height=14, Stretch=Stretch.Uniform, StrokeThickness=1.5, StrokeStartLineCap=PenLineCap.Round, StrokeEndLineCap=PenLineCap.Round, StrokeLineJoin=PenLineJoin.Round };
            path.SetBinding(System.Windows.Shapes.Shape.StrokeProperty,new System.Windows.Data.Binding("Foreground") { RelativeSource=new System.Windows.Data.RelativeSource(System.Windows.Data.RelativeSourceMode.FindAncestor,typeof(Button),1) });
            if(filled) path.SetBinding(System.Windows.Shapes.Shape.FillProperty,new System.Windows.Data.Binding("Foreground") { RelativeSource=new System.Windows.Data.RelativeSource(System.Windows.Data.RelativeSourceMode.FindAncestor,typeof(Button),1) });
            return path;
        }
        static object PlayIcon(bool paused)
        {
            var icon=(FrameworkElement)ControlIcon(paused ? "Ⅱ" : "▶");
            // A triangle's visual weight sits left of its bounding-box center.
            if(!paused) icon.RenderTransform=new TranslateTransform(2,0);
            return icon;
        }
        static ControlTemplate MakeButtonTemplate(int radius)
        {
            return (ControlTemplate)XamlReader.Parse("<ControlTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' TargetType='Button'><Border x:Name='edge' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' CornerRadius='"+radius+"' Background='{TemplateBinding Background}' BorderBrush='{TemplateBinding BorderBrush}' BorderThickness='{TemplateBinding BorderThickness}' Padding='{TemplateBinding Padding}'><ContentPresenter HorizontalAlignment='Center' VerticalAlignment='Center'/></Border><ControlTemplate.Triggers><Trigger Property='IsMouseOver' Value='True'><Setter TargetName='edge' Property='Background' Value='#606060'/><Setter Property='Foreground' Value='White'/></Trigger><Trigger Property='IsPressed' Value='True'><Setter TargetName='edge' Property='Background' Value='#808080'/></Trigger></ControlTemplate.Triggers></ControlTemplate>");
        }
        void OriginalSize() {
            Reset();
            if(photo.Source==null || originalPixelWidth<=0) return;
            double fit=Math.Min(photo.ActualWidth/photo.Source.Width,photo.ActualHeight/photo.Source.Height);
            if(fit<=0) return;
            zoomTarget=originalPixelWidth/(photo.Source.Width*fit);
            scale.ScaleX=scale.ScaleY=zoomTarget;
            detailTimer.Stop();detailTimer.Start();
        }
        void RotatePhoto(double angle) {
            if(photo.Source==null) return;
            var existing=photo.LayoutTransform as RotateTransform;
            photo.LayoutTransform=new RotateTransform(((existing==null ? 0 : existing.Angle)+angle)%360);
        }

        void SetPlaying(bool playing)
        {
            slideshowTimer.Stop();
            if(playing && files.Length>1) slideshowTimer.Start();
            playButton.Content=PlayIcon(slideshowTimer.IsEnabled);
        }

        void Open()
        {
            var dialog = new OpenFileDialog { Filter="Imágenes|"+string.Join(";",extensions.Select(x=>"*"+x)), CheckFileExists=true };
            if (dialog.ShowDialog(this)==true) LoadPath(dialog.FileName);
        }

        static string DecodePath(string path)
        {
            string extension=Path.GetExtension(path).ToLowerInvariant();
            if(nativeExtensions.Contains(extension)) return path;
            if(!extensions.Contains(extension)) throw new IOException("Solo se admiten imágenes.");
            string codec=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"codecs","magick.exe");
            if(!File.Exists(codec)) throw new IOException("Falta la carpeta codecs junto al visor.");
            string key;
            using(var hash=SHA256.Create()) key=BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes("v2"+path+File.GetLastWriteTimeUtc(path).Ticks))).Replace("-","");
            string folder=Path.Combine(Path.GetTempPath(),"Piccasa-image-cache");
            Directory.CreateDirectory(folder);
            string output=Path.Combine(folder,key+".png");
            lock(conversionLock) {
                if(File.Exists(output)) return output;
                string temporary=Path.Combine(folder,key+"-"+Guid.NewGuid().ToString("N")+".png");
                string normalized=temporary+".svg";
                try {
                    string input=path;
                    if(extension==".svg" && NormalizeAndroidVector(path,normalized)) input=normalized;
                    var info=new ProcessStartInfo(codec) {
                        Arguments="-limit memory 256MiB -limit map 512MiB -limit time 30 \""+input+"[0]\" -auto-orient \"PNG:"+temporary+"\"",
                        UseShellExecute=false, CreateNoWindow=true, RedirectStandardError=true
                    };
                    using(var process=Process.Start(info)) {
                        var errors=process.StandardError.ReadToEndAsync();
                        if(!process.WaitForExit(35000)) { process.Kill(); throw new IOException("La imagen tardó demasiado en decodificarse."); }
                        if(process.ExitCode!=0 || !File.Exists(temporary)) throw new IOException("No se pudo abrir «"+Path.GetFileName(path)+"». El archivo puede estar dañado o contener una variante no compatible.");
                    }
                    File.Move(temporary,output);
                } finally { if(File.Exists(temporary)) File.Delete(temporary); if(File.Exists(normalized)) File.Delete(normalized); }
            }
            return output;
        }
        static void CleanConversionCache()
        {
            try {
                string folder=Path.Combine(Path.GetTempPath(),"Piccasa-image-cache");
                if(!Directory.Exists(folder)) return;
                lock(conversionLock) {
                    var entries=new DirectoryInfo(folder).GetFiles("*.png").OrderByDescending(x=>x.LastWriteTimeUtc).ToArray();
                    long retained=0;
                    foreach(var file in entries) {
                        // Only files created by this viewer, inside its dedicated cache directory.
                        if(!System.Text.RegularExpressions.Regex.IsMatch(file.Name,"^[A-F0-9]{64}(?:-[a-f0-9]{32})?\\.png$")) continue;
                        retained+=file.Length;
                        if(retained>512L*1024*1024 || file.LastWriteTimeUtc<DateTime.UtcNow.AddDays(-7)) {
                            try { file.Delete(); } catch(IOException) { } catch(UnauthorizedAccessException) { }
                        }
                    }
                }
            } catch { /* Cache housekeeping must never prevent launching the viewer. */ }
        }

        static bool NormalizeAndroidVector(string input,string output)
        {
            var document=new XmlDocument { XmlResolver=null };
            using(var reader=XmlReader.Create(input,new XmlReaderSettings { DtdProcessing=DtdProcessing.Ignore, XmlResolver=null })) {
                reader.MoveToContent();
                // Ordinary SVGs go directly to the renderer; only Android vectors need conversion.
                if(reader.LocalName!="vector") return false;
                document.Load(reader);
            }
            var root=document.DocumentElement;
            if(root==null || root.LocalName!="vector") return false;
            const string android="http://schemas.android.com/apk/res/android";
            string width=root.GetAttribute("viewportWidth",android),height=root.GetAttribute("viewportHeight",android);
            double w,h;
            if(!double.TryParse(width,System.Globalization.NumberStyles.Float,System.Globalization.CultureInfo.InvariantCulture,out w) || !double.TryParse(height,System.Globalization.NumberStyles.Float,System.Globalization.CultureInfo.InvariantCulture,out h) || w<=0 || h<=0) throw new IOException("El vector Android no tiene dimensiones válidas.");
            using(var writer=XmlWriter.Create(output,new XmlWriterSettings { Encoding=Encoding.UTF8 })) {
                writer.WriteStartElement("svg","http://www.w3.org/2000/svg");
                writer.WriteAttributeString("width",width); writer.WriteAttributeString("height",height);
                writer.WriteAttributeString("viewBox","0 0 "+width+" "+height);
                WriteAndroidPaths(root,writer);
                writer.WriteEndElement();
            }
            return true;
        }
        static void WriteAndroidPaths(XmlElement parent,XmlWriter writer)
        {
            const string ns="http://schemas.android.com/apk/res/android";
            foreach(XmlNode node in parent.ChildNodes) {
                var element=node as XmlElement;
                if(element==null) continue;
                if(element.LocalName=="group") {
                    writer.WriteStartElement("g");
                    Func<string,string,string> value=(name,fallback)=>element.HasAttribute(name,ns) ? element.GetAttribute(name,ns) : fallback;
                    string px=value("pivotX","0"),py=value("pivotY","0");
                    writer.WriteAttributeString("transform","translate("+value("translateX","0")+","+value("translateY","0")+") translate("+px+","+py+") rotate("+value("rotation","0")+") scale("+value("scaleX","1")+","+value("scaleY","1")+") translate(-"+px+",-"+py+")");
                    WriteAndroidPaths(element,writer);writer.WriteEndElement();
                } else if(element.LocalName=="path") {
                    writer.WriteStartElement("path");writer.WriteAttributeString("d",element.GetAttribute("pathData",ns));
                    foreach(var pair in new[]{new[]{"fillColor","fill"},new[]{"strokeColor","stroke"}}) {
                        string color=element.GetAttribute(pair[0],ns);
                        if(color=="") color=pair[1]=="fill" ? "none" : "none";
                        if(color.StartsWith("@") || color.StartsWith("?")) throw new IOException("El vector usa colores externos de Android.");
                        if(color.Length==9 && color[0]=='#') {
                            writer.WriteAttributeString(pair[1]+"-opacity",(Convert.ToInt32(color.Substring(1,2),16)/255.0).ToString(System.Globalization.CultureInfo.InvariantCulture));
                            color="#"+color.Substring(3);
                        }
                        writer.WriteAttributeString(pair[1],color);
                    }
                    foreach(var pair in new[]{new[]{"strokeWidth","stroke-width"},new[]{"strokeLineCap","stroke-linecap"},new[]{"strokeLineJoin","stroke-linejoin"}}) if(element.HasAttribute(pair[0],ns)) writer.WriteAttributeString(pair[1],element.GetAttribute(pair[0],ns));
                    if(element.GetAttribute("fillType",ns)=="evenOdd") writer.WriteAttributeString("fill-rule","evenodd");
                    writer.WriteEndElement();
                } else throw new IOException("Este vector Android contiene elementos aún no compatibles: "+element.LocalName);
            }
        }

        void LoadPath(string path)
        {
            try {
                path=Path.GetFullPath(path);
                if (!File.Exists(path)) throw new IOException("El archivo no existe o no está disponible.");
                var candidates=Directory.GetFiles(Path.GetDirectoryName(path)).Where(p => extensions.Contains(Path.GetExtension(p).ToLowerInvariant())).OrderBy(p => p,StringComparer.OrdinalIgnoreCase).ToArray();
                int selected=Array.FindIndex(candidates,p => string.Equals(p,path,StringComparison.OrdinalIgnoreCase));
                if (selected < 0) throw new IOException("Formato de imagen no compatible. PDF y videos no se admiten.");
                ShowImage(candidates,selected);
            } catch (Exception ex) { ShowError(ex); }
        }

        void ShowImage(string[] candidates, int selected, bool fade=false)
        {
            // Cache fully, then close the stream: the viewer never locks or edits the original.
            string selectedPath=candidates[selected];
            long stamp=File.GetLastWriteTimeUtc(selectedPath).Ticks;
            CachedPhoto cached;
            if (!imageCache.TryGetValue(selectedPath,out cached) || cached.Stamp!=stamp) {
                try { cached=ReadPhoto(selectedPath,1920); }
                catch(Exception) { cached=new CachedPhoto { Bitmap=InvalidImage(), Width=640, Stamp=stamp, Invalid=true }; }
                StorePhoto(selectedPath,cached);
            }
            cached.Used=DateTime.UtcNow.Ticks;
            SavePhotoView();
            StopGif();
            previousPhoto.BeginAnimation(UIElement.OpacityProperty,null);
            photo.BeginAnimation(UIElement.OpacityProperty,null);
            previousPhoto.Source=fade ? photo.Source : null;
            previousPhoto.RenderTransform=photo.RenderTransform.CloneCurrentValue();
            previousPhoto.LayoutTransform=photo.LayoutTransform.CloneCurrentValue();
            previousPhoto.Opacity=1;
            originalPixelWidth=cached.Width;
            photo.Source=cached.Bitmap; files=candidates; index=selected;
            if (IsLoaded) { UpdateLayout(); RestorePhotoView(); }
            else Reset();
            if(fade && previousPhoto.Source!=null) {
                var outgoing=new DoubleAnimation(1,0,TimeSpan.FromMilliseconds(450));
                outgoing.Completed += delegate { previousPhoto.Source=null; };
                previousPhoto.BeginAnimation(UIElement.OpacityProperty,outgoing);
                photo.BeginAnimation(UIElement.OpacityProperty,new DoubleAnimation(0,1,TimeSpan.FromMilliseconds(450)));
            }
            if(slideshowTimer.IsEnabled) { slideshowTimer.Stop(); slideshowTimer.Start(); }
            caption.Text=Path.GetFileName(files[index])+"   "+(index+1)+" / "+files.Length;
            caption.ToolTip=Path.GetFileName(files[index]);
            Title=Path.GetFileName(files[index])+" — Visor de fotos Lorea";
            RefreshThumbnails();
            if(IsLoaded) { StartGif(selectedPath); PrefetchNeighbors(); detailTimer.Stop(); detailTimer.Start(); }
        }

        static CachedPhoto ReadPhoto(string path,int desiredWidth)
        {
            long stamp=File.GetLastWriteTimeUtc(path).Ticks;
            using(var stream=File.OpenRead(DecodePath(path))) {
                var decoder=BitmapDecoder.Create(stream,BitmapCreateOptions.DelayCreation,BitmapCacheOption.None);
                int width=decoder.Frames[0].PixelWidth,height=decoder.Frames[0].PixelHeight;
                // Bound any one bitmap to 24 megapixels, including unusually tall images.
                int bounded=(int)Math.Max(1,Math.Min(width,Math.Sqrt(24000000.0*width/height)));
                stream.Position=0;
                var bitmap=new BitmapImage();bitmap.BeginInit();bitmap.CacheOption=BitmapCacheOption.OnLoad;
                bitmap.DecodePixelWidth=Math.Max(1,Math.Min(bounded,desiredWidth));
                bitmap.StreamSource=stream;bitmap.EndInit();bitmap.Freeze();
                return new CachedPhoto { Bitmap=bitmap,Width=width,Stamp=stamp,Used=DateTime.UtcNow.Ticks };
            }
        }
        void StorePhoto(string path,CachedPhoto cached)
        {
            cached.Used=DateTime.UtcNow.Ticks;imageCache[path]=cached;
            while(imageCache.Count>1 && (imageCache.Count>8 || imageCache.Values.Sum(x=>(long)x.Bitmap.PixelWidth*x.Bitmap.PixelHeight*4)>ImageCacheBudget)) {
                string oldest=imageCache.Where(x=>!string.Equals(x.Key,path,StringComparison.OrdinalIgnoreCase)).OrderBy(x=>x.Value.Used).First().Key;
                imageCache.Remove(oldest);
            }
        }
        async void PrefetchNeighbors()
        {
            if(prefetchBusy || files.Length<2) return;
            prefetchBusy=true;
            var snapshot=files;int selected=index;
            try {
                foreach(int direction in new[]{1,-1}) {
                    string path=snapshot[(selected+direction+snapshot.Length)%snapshot.Length];
                    CachedPhoto cached;
                    if(imageCache.TryGetValue(path,out cached) && cached.Stamp==File.GetLastWriteTimeUtc(path).Ticks) continue;
                    try {
                        cached=await Task.Run(()=>ReadPhoto(path,1920));
                        if(!IsLoaded) return;
                        StorePhoto(path,cached);
                    } catch { /* Invalid neighbors are handled when displayed. */ }
                }
            } finally {
                prefetchBusy=false;
                if(IsLoaded && (snapshot!=files || selected!=index)) PrefetchNeighbors();
            }
        }
        async void LoadZoomDetail()
        {
            if(detailBusy || files.Length==0 || gifImage!=null || photo.Source==null) return;
            string path=files[index];CachedPhoto cached;
            if(!imageCache.TryGetValue(path,out cached) || cached.Invalid) return;
            double fit=Math.Min(photo.ActualWidth/photo.Source.Width,photo.ActualHeight/photo.Source.Height);
            var dpi=PresentationSource.FromVisual(this);
            double density=dpi==null ? 1 : dpi.CompositionTarget.TransformToDevice.M11;
            int needed=(int)Math.Min(cached.Width,Math.Max(1,photo.Source.Width*fit*zoomTarget*density));
            int maxWidth=(int)Math.Max(1,Math.Min(cached.Width,Math.Sqrt(24000000.0*cached.Bitmap.PixelWidth/cached.Bitmap.PixelHeight)));
            needed=Math.Min(needed,maxWidth);
            if(needed<=cached.Bitmap.PixelWidth*1.15) return;
            detailBusy=true;
            try {
                var detail=await Task.Run(()=>ReadPhoto(path,needed));
                if(!IsLoaded || files.Length==0 || !string.Equals(files[index],path,StringComparison.OrdinalIgnoreCase)) return;
                StorePhoto(path,detail);photo.Source=detail.Bitmap;
            } catch { /* Retain the available preview if the detail decode fails. */ }
            finally { detailBusy=false; }
        }

        static BitmapSource InvalidImage()
        {
            var visual=new DrawingVisual();
            using(var drawing=visual.RenderOpen()) {
                drawing.DrawRoundedRectangle(new SolidColorBrush(Color.FromRgb(38,38,38)),null,new Rect(0,0,640,360),16,16);
                var title=new FormattedText("Imagen no válida",System.Globalization.CultureInfo.CurrentCulture,FlowDirection.LeftToRight,new Typeface("Segoe UI"),30,Brushes.White);
                var hint=new FormattedText("No se pudo leer este archivo. Puedes seguir a la siguiente foto.",System.Globalization.CultureInfo.CurrentCulture,FlowDirection.LeftToRight,new Typeface("Segoe UI"),16,Brushes.LightGray);
                drawing.DrawText(title,new Point((640-title.Width)/2,140));
                drawing.DrawText(hint,new Point((640-hint.Width)/2,195));
            }
            var bitmap=new RenderTargetBitmap(640,360,96,96,PixelFormats.Pbgra32);bitmap.Render(visual);bitmap.Freeze();return bitmap;
        }

        void StopGif()
        {
            gifTimer.Stop();
            if(gifImage!=null) { gifImage.Dispose(); gifImage=null; }
            if(gifStream!=null) { gifStream.Dispose(); gifStream=null; }
        }
        void StartGif(string path)
        {
            StopGif();
            if(!string.Equals(Path.GetExtension(path),".gif",StringComparison.OrdinalIgnoreCase)) return;
            try {
                gifStream=new MemoryStream(File.ReadAllBytes(path));
                gifImage=System.Drawing.Image.FromStream(gifStream);
                gifFrameCount=gifImage.GetFrameCount(System.Drawing.Imaging.FrameDimension.Time);
                if(gifFrameCount<2) { StopGif(); return; }
                gifDelays=new int[gifFrameCount];
                byte[] timing=null;
                try { timing=gifImage.GetPropertyItem(0x5100).Value; } catch(ArgumentException) { }
                for(int i=0;i<gifFrameCount;i++) {
                    int delay=timing!=null && timing.Length>=(i+1)*4 ? BitConverter.ToInt32(timing,i*4) : 10;
                    gifDelays[i]=delay<=1 ? 100 : Math.Min(delay,60000)*10;
                }
                gifFrame=0;
                gifTimer.Interval=TimeSpan.FromMilliseconds(gifDelays[0]);gifTimer.Start();
            } catch { StopGif(); } // A still GIF remains viewable if animation metadata is invalid.
        }
        void AdvanceGif()
        {
            if(gifImage==null) return;
            try {
                gifFrame=(gifFrame+1)%gifFrameCount;
                gifImage.SelectActiveFrame(System.Drawing.Imaging.FrameDimension.Time,gifFrame);
                using(var buffer=new MemoryStream()) {
                    gifImage.Save(buffer,System.Drawing.Imaging.ImageFormat.Png); buffer.Position=0;
                    var frame=new BitmapImage();frame.BeginInit();frame.CacheOption=BitmapCacheOption.OnLoad;
                    frame.DecodePixelWidth=Math.Min(3840,gifImage.Width);frame.StreamSource=buffer;frame.EndInit();frame.Freeze();
                    photo.Source=frame;
                }
                gifTimer.Interval=TimeSpan.FromMilliseconds(gifDelays[gifFrame]);
            } catch { StopGif(); }
        }

        void RefreshThumbnails()
        {
            if(!IsLoaded) return;
            // Always start under WPF's dispatcher synchronization context, including file launches.
            Dispatcher.BeginInvoke(new Action(RefreshThumbnailsAsync));
        }

        async void RefreshThumbnailsAsync()
        {
            int generation=++thumbnailGeneration;
            thumbnails.Children.Clear();
            // A bounded neighborhood keeps large photo folders inexpensive to browse.
            var snapshot=files;
            int start=Math.Max(0,index-10), end=Math.Min(files.Length,index+11);
            for (int i=start;i<end;i++) {
                int selected=i;
                var image=new Image { Width=30, Height=24, Stretch=Stretch.Uniform };
                var button=new Button { Content=image, Width=34, Height=28, Focusable=false, Cursor=Cursors.Hand, ToolTip=Path.GetFileName(snapshot[i]), Margin=new Thickness(1,0,1,0), Padding=new Thickness(1), BorderThickness=new Thickness(1), BorderBrush=i==index ? Brushes.White : Brushes.Transparent, Background=new SolidColorBrush(Color.FromRgb(45,45,45)) };
                button.Click += delegate { try { ShowImage(snapshot,selected); } catch(Exception ex) { ShowError(ex); } };
                thumbnails.Children.Add(button);
            }
            for (int i=start;i<end;i++) {
                string path=snapshot[i];
                BitmapImage bitmap;
                if (!thumbnailCache.TryGetValue(path,out bitmap)) {
                bitmap=await Task.Run(() => {
                    try {
                        var thumb=new BitmapImage();
                        using(var stream=File.OpenRead(DecodePath(path))) {
                            thumb.BeginInit(); thumb.CacheOption=BitmapCacheOption.OnLoad; thumb.DecodePixelWidth=100;
                            thumb.StreamSource=stream; thumb.EndInit(); thumb.Freeze();
                        }
                        return thumb;
                    } catch { return null; }
                });
                if (generation!=thumbnailGeneration) return;
                if (bitmap!=null) {
                    if (thumbnailCache.Count>=256) thumbnailCache.Clear();
                    thumbnailCache[path]=bitmap;
                }
                }
                var button=(Button)thumbnails.Children[i-start];
                if(bitmap!=null) ((Image)button.Content).Source=bitmap;
                else button.Content="Sin vista";
            }
        }

        void Navigate(int step, bool fade=false)
        {
            if (files.Length==0) return;
            int start=index;
            for(int attempt=1;attempt<files.Length;attempt++) {
                int candidate=((start+step*attempt)%files.Length+files.Length)%files.Length;
                try { ShowImage(files,candidate,fade); return; }
                catch(Exception) { /* Keep browsing past unreadable files without interrupting navigation. */ }
            }
            if(slideshowTimer.IsEnabled) SetPlaying(false);
        }
        void Reset() {
            scale.BeginAnimation(ScaleTransform.ScaleXProperty,null);
            scale.BeginAnimation(ScaleTransform.ScaleYProperty,null);
            zoomTarget=1; scale.ScaleX=scale.ScaleY=1; offset.X=offset.Y=0;
            zoomBadgeTimer.Stop(); zoomBadge.Visibility=Visibility.Collapsed;
        }
        void ResetOpeningSize()
        {
            Reset();
            if (photo.Source==null || originalPixelWidth<=0 || photo.ActualWidth<=0 || photo.ActualHeight<=0) return;
            double fit=Math.Min(photo.ActualWidth/photo.Source.Width,photo.ActualHeight/photo.Source.Height);
            if (fit<=0) return;
            // Do not enlarge small originals; keep large images fitted to the viewport.
            zoomTarget=Math.Min(1,originalPixelWidth/(photo.Source.Width*fit));
            scale.ScaleX=scale.ScaleY=zoomTarget;
        }
        void SavePhotoView()
        {
            if (photo.Source==null || files.Length==0) return;
            var rotation=photo.LayoutTransform as RotateTransform;
            photoViews[files[index]]=new PhotoView { Zoom=scale.ScaleX, X=offset.X, Y=offset.Y, Angle=rotation==null ? 0 : rotation.Angle };
        }
        void RestorePhotoView()
        {
            PhotoView saved;
            photo.LayoutTransform=Transform.Identity;
            if (files.Length==0 || !photoViews.TryGetValue(files[index],out saved)) {
                ResetOpeningSize(); return;
            }
            Reset();
            photo.LayoutTransform=new RotateTransform(saved.Angle);
            zoomTarget=saved.Zoom;
            scale.ScaleX=scale.ScaleY=saved.Zoom;
            offset.X=saved.X; offset.Y=saved.Y;
        }
        void Zoom(double factor) {
            detailTimer.Stop();detailTimer.Start();
            zoomTarget=Math.Max(.1,Math.Min(20,zoomTarget*factor));
            double current=scale.ScaleX;
            var animation=new DoubleAnimation(current,zoomTarget,TimeSpan.FromMilliseconds(320)) {
                EasingFunction=new QuadraticEase { EasingMode=EasingMode.EaseOut }
            };
            scale.BeginAnimation(ScaleTransform.ScaleXProperty,animation);
            scale.BeginAnimation(ScaleTransform.ScaleYProperty,animation);
            if (photo.Source!=null && originalPixelWidth>0) {
                zoomBadge.Visibility=Visibility.Visible;
                UpdateZoomIndicator(null,EventArgs.Empty);
                zoomBadgeTimer.Stop(); zoomBadgeTimer.Start();
            }
        }
        void UpdateZoomIndicator(object sender, EventArgs e)
        {
            if (zoomBadge.Visibility!=Visibility.Visible || photo.Source==null || originalPixelWidth<=0) return;
            double fit=Math.Min(photo.ActualWidth/photo.Source.Width,photo.ActualHeight/photo.Source.Height);
            double percent=100*photo.Source.Width*fit*scale.ScaleX/originalPixelWidth;
            zoomLabel.Text=Math.Round(percent)+"%";
        }
        void ShowError(Exception ex) { MessageBox.Show(this,ex.Message,"No se pudo abrir la imagen",MessageBoxButton.OK,MessageBoxImage.Information); }
        void OnKey(object sender, KeyEventArgs e)
        {
            if (e.Key==Key.Escape) Close();
            else if (e.Key==Key.Right) Navigate(1);
            else if (e.Key==Key.Left) Navigate(-1);
            else if (e.Key==Key.Up) { Zoom(1.10); e.Handled=true; }
            else if (e.Key==Key.Down) { Zoom(1/1.10); e.Handled=true; }
            else if (e.Key==Key.D0 || e.Key==Key.Home) Reset();
            else if (e.Key==Key.O && Keyboard.Modifiers==ModifierKeys.Control) Open();
        }
    }
}

